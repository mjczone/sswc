using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using ServiceStack;

namespace DemoApi
{
    public class AppHost: AppSelfHostBase
    {
        public static readonly Guid RuntimeId = Guid.NewGuid();
        public static readonly DateTime StartTime = DateTime.Now;

        public AppHost(): base("Demo API", typeof(AppHost).Assembly) { }
        public override void Configure(Container container)
        {
        }
    }

    public class PingService: Service
    {
        public Pong Any(Ping ping)
        {
            return new Pong();
        }
        public async Task<bool> Any(Restart ping)
        {
            var restartFile = Path.Combine(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath), "system.restart");
            var restartBody = DateTime.Now.ToString("u");
#pragma warning disable 4014
            WriteToFileAsync(restartFile, restartBody, 20);
#pragma warning restore 4014

            return await Task.FromResult(true);
        }

        // see: http://www.infoworld.com/article/2995387/application-architecture/how-to-perform-asynchronous-file-operations-in-c.html
        static async Task WriteToFileAsync(string filePath, string text, int delay)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            if(delay > 0)   
                await Task.Delay(delay);

            var buffer = Encoding.Unicode.GetBytes(text);
            var offset = 0;
            var sizeOfBuffer = 4096;
            FileStream fileStream = null;

            try
            {
                fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write,

                FileShare.None, bufferSize: sizeOfBuffer, useAsync: true);
                await fileStream.WriteAsync(buffer, offset, buffer.Length);
            }
            catch
            {
                //Write code here to handle exceptions.
            }
            finally
            {
                fileStream?.Dispose();
            }
        }
    }

    [Route("/restart")]
    public class Restart
    {

    }

    [Route("/ping")]
    public class Ping : IReturn<Pong>
    {
        
    }

    public class Pong
    {
        public string RuntimeId { get; } = AppHost.RuntimeId.ToString("N") + " started at " + AppHost.StartTime.ToString("u");
    }
}
