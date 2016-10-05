using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
using Owin;

namespace WebApiExampleApp
{
    /// <summary>
    /// Run WebApi example with sswc using:
    /// 
    /// sswc .\WebApiExampleApp\bin\Debug\WebApiExampleApp.dll /type=WebApiExampleApp.WebApiHostWrapper /port=2026
    /// </summary>
    public class WebApiHostWrapper
    {
        private IDisposable _host = null;

        public void Init()
        {
            // Setup a FIX for an OWIN binding redirect error
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new AssemblyName(args.Name);
                return name.Name == "Microsoft.Owin" ? typeof(Microsoft.Owin.IOwinContext).Assembly : null;
            };
        }

        // 'urlBase' will look like this: 'http://*:2020', or whatever port you're using
        public void Start(string urlBase)
        {
            try
            {
                _host = WebApp.Start<Startup>(urlBase);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Stop()
        {
            _host?.Dispose();
            _host = null;
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Web API for self-host. 
            var config = new HttpConfiguration();

            // Set JSON as the default
            config.Formatters.XmlFormatter.MediaTypeMappings.Add(new QueryStringMapping("xml", "true", "application/xml"));
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));

            // Create routes
            config.Routes.MapHttpRoute( "DefaultApi", "api/{controller}/{id}", new { id = RouteParameter.Optional } );

            app.UseWebApi(config);
            app.UseWelcomePage("/");
        }
    }
    public class DemoController : ApiController
    {
        static readonly Guid RuntimeId = Guid.NewGuid();

        public IEnumerable<string> Get()
        {
            return new [] { "Hello", "World", RuntimeId.ToString("N") };
        }
    }
}
