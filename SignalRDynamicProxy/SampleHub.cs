using Microsoft.AspNet.SignalR;
using System;

namespace SignalRDynamicProxy
{
    public class SampleHub : Hub, ISampleHub
    {
        public void Send(string message)
        {
            Clients.All.Received(message);
        }
    }

    public interface ISampleHubClient : ISampleHub, ISampleCallback
    {
    }

    public interface ISampleCallback
    {
        IObservable<string> Received { get; }
    }

    public interface ISampleHub
    {
        void Send(string message);
    }
}