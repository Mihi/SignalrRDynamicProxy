using LinFu.DynamicProxy;
using Microsoft.AspNet.SignalR.Client;
using SignalRDynamicProxy;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace Client
{
    class Program
    {
        static void Main()
        {
            var proxy = SignalRClientFactory.Create<ISampleHubClient>("http://localhost:44250", "SampleHub");
            proxy.Received.Subscribe(Console.WriteLine);

            for (; ; )
            {
                var message = Console.ReadLine();
                proxy.Send(message);
            }
        }
    }

    public static class SignalRClientFactory
    {
        public static T Create<T>(string url, string hubname)
        {
            var factory = new ProxyFactory();
            var wrappedProxy = new HubClient(url, hubname);
            var interceptor = new SignalRInterceptor(wrappedProxy);
            var proxy = factory.CreateProxy<T>(interceptor);
            return proxy;
        }
    }

    internal class SignalRInterceptor : IInterceptor
    {
        private readonly HubClient _interceptedProxy;

        private readonly Dictionary<string, dynamic> _subjects; 

        public SignalRInterceptor(HubClient interceptedProxy)
        {
            _interceptedProxy = interceptedProxy;
            _subjects = new Dictionary<string, dynamic>();
        }

        public object Intercept(InvocationInfo info)
        {
            if (info.TargetMethod.ReturnType.Name.StartsWith("IObservable"))
            {
                var eventName = GetEventName(info);
                dynamic subject;
                if (!_subjects.TryGetValue(eventName, out subject))
                {
                    var genericType = info.TargetMethod.ReturnType.GenericTypeArguments[0];
                    var subjectType = typeof(Subject<>).MakeGenericType(new[] { genericType });
                    subject = Activator.CreateInstance(subjectType);

                    _interceptedProxy.HubProxy.On(eventName, p => subject.OnNext(p));
                    _subjects.Add(eventName, subject);
                    return subject;
                }

                return subject;
            }
            
            return _interceptedProxy.HubProxy.Invoke(info.TargetMethod.Name, info.Arguments);
        }

        private string GetEventName(InvocationInfo info)
        {
            return info.TargetMethod.Name.Remove(0, 4);
        }
    }

    internal class HubClient
    {
        private HubConnection _hubConnection;
        public IHubProxy HubProxy { get; private set; }
        
        public HubClient(string url, string hub)
        {
            _hubConnection = new HubConnection(url);
            _hubConnection.StateChanged += _hubConnection_StateChanged;
            HubProxy = _hubConnection.CreateHubProxy(hub);

            _hubConnection.Start();
        }

        void _hubConnection_StateChanged(StateChange obj)
        {
            Console.WriteLine(obj.NewState);
        }
    }
}
