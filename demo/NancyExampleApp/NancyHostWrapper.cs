using System;
using Nancy;
using Nancy.Hosting.Self;

namespace NancyExampleApp
{
    /// <summary>
    /// Run Nancy example with sswc using:
    /// 
    /// sswc .\NancyExampleApp\bin\Debug\NancyExampleApp.dll /type=NancyExampleApp.NancyHostWrapper /port=2025
    /// </summary>
    public class NancyHostWrapper
    {
        private NancyHost _host = null;

        public void Init()
        {
            // optional call that is fired before the Start method
        }

        // 'urlBase' will look like this: 'http://*:2020', or whatever port you're using
        public void Start(string urlBase)
        {
            _host = new NancyHost(new Uri(urlBase.Replace("*", "localhost")));
            _host.Start();
        }

        public void Stop()
        {
            _host?.Stop();
            _host?.Dispose();
            _host = null;
        }
    }

    public class SampleModule : Nancy.NancyModule
    {
        static readonly Guid RuntimeId = Guid.NewGuid();

        public SampleModule()
        {
            Get["/"] = _ => Response.AsJson(new [] { "Hello", "World", RuntimeId.ToString("N") });
        }
    }
}