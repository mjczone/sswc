using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Funq;
using ServiceStack;
using ServiceStack.Text;

namespace ServiceStackExampleApp
{
    public class AppHost: AppSelfHostBase
    {
        public AppHost() : base("ServiceStack API", typeof(AppHost).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            JsConfig.EmitCamelCaseNames = true;
        }
    }

    public class AppService : Service
    {
        static readonly Guid RuntimeId = Guid.NewGuid();
        public object Any(Ping request)
        {
            return new[] { "Hello", "World", RuntimeId.ToString("N") };
        }
        public object Any(Demo request)
        {
            return "You are at path: " + request.Path;
        }
    }

    [Route("/ping")]
    public class Ping
    {
    }

    [FallbackRoute("/{Path*}")]
    public class Demo
    {   
        public string Path { get; set; }
    }
}
