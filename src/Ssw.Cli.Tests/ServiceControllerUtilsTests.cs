using NUnit.Framework;
using Ssw.Cli;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Ssw.Cli.Tests
{
    public class ServiceControllerUtilsTests: TestFixtureBase
    {
        internal static volatile int StartCount = 0;
        internal static volatile string[] StartArgs = new string[0];

        internal static readonly string LogFileName = Path.Combine(CurrentDir, "ServiceControllerUtilsTests.txt");

        [Test()]
        public void InstallServiceTests()
        {
            UninstallServiceTests();

            if (File.Exists(LogFileName)) File.Delete(LogFileName);

            for (var i = 1; i < 5; i++)
            {
                var svcName = "bogus_123_" + i;
                var svcDisplayName = "Bogus 123_" + i;
                var svcDescription = "Bogus 123_" + i + " Description";

                ServiceControllerUtils.InstallService(typeof(BogusService).Assembly, svcName, svcDisplayName,
                    svcDescription, "/a=a1", "/b=b1", "/c=\"blah blah blah\"");
                Assert.That(ServiceControllerUtils.ServiceExists(svcName), Is.True);

                var sc = ServiceController.GetServices(".").FirstOrDefault(f => f.ServiceName == svcName);
                Assert.IsNotNull(sc);
                Assert.That(sc.DisplayName, Is.EqualTo(svcDisplayName));

                var reg = Registry.LocalMachine;
                try
                {
                    // make sure the path has the config parameters
                    RegistryKey hklm = null;
                    try
                    {
                        hklm = reg.OpenSubKey(@"System\CurrentControlSet\Services\" + svcName);
                        Assert.IsNotNull(hklm);

                        //var displayName = hklm.GetValue("DisplayName");
                        //Assert.IsNotNull(displayName);
                        //Assert.That(displayName, Is.EqualTo(svcDisplayName));

                        //var description = hklm.GetValue("Description");
                        //Assert.IsNotNull(description);
                        //Assert.That(description, Is.EqualTo(svcDisplayName + " Description"));

                        var path = hklm.GetValue("ImagePath");
                        Assert.IsNotNull(path);
                        Assert.That(path, Contains.Substring("/a=a1"));
                        Assert.That(path, Contains.Substring("/b=b1"));
                        Assert.That(path, Contains.Substring("/c=\"blah blah blah\""));
                    }
                    finally
                    {
                        hklm?.Dispose();
                    }
                }
                finally
                {
                    reg?.Dispose();
                }

                Assert.That(ServiceControllerUtils.ServiceStatus(svcName), Is.EqualTo(ServiceControllerStatus.Stopped));
            }

            try
            {
                // attempt to start and stop the services
                var svcName2 = "bogus_123_" + 1;
                ServiceControllerUtils.StartService(svcName2);
                Assert.That(ServiceControllerUtils.ServiceStatus(svcName2), Is.EqualTo(ServiceControllerStatus.Running));
                ServiceControllerUtils.StopService(svcName2);
                Assert.That(ServiceControllerUtils.ServiceStatus(svcName2), Is.EqualTo(ServiceControllerStatus.Stopped));
            }
            finally
            {
                UninstallServiceTests();

                if (File.Exists(LogFileName)) File.Delete(LogFileName);
            }
        }

        [Test()]
        public void UninstallServiceTests()
        {
            // for some reason you can't stop the service:
            // do: 
            // sc queryex bogus_123_1
            // taskkill /f /pid [PID]  <-- get the PID from the first command
            for (var i = 1; i < 5; i++)
            {
                var svcName = "bogus_123_" + i;
                ServiceControllerUtils.UninstallService(typeof(BogusService).Assembly, svcName);
            }

            for (var i = 1; i < 5; i++)
            {
                var svcName = "bogus_123_" + i;
                Assert.That(ServiceControllerUtils.ServiceExists(svcName), Is.False);
            }
        }
    }
    public class Program
    {
        private string[] args;

        public Program(string[] args)
        {
            this.args = args;
        }

        public static void Main(string[] args)
        {
            args = args ?? new string[0];
            File.AppendAllText(ServiceControllerUtilsTests.LogFileName, "Program.ctr: " + string.Join(" :: ", args) + "\n");
            var p = new Program(args);
            p.Start();
        }

        private void Start()
        {
            // running as service   
            ServiceBase[] services = { new BogusService() };
            ServiceBase.Run(services);
        }
    }


    public class BogusService: ServiceBase
    {
        protected override void OnStart(string[] args)
        {
            args = args ?? new string[0];
            File.AppendAllText(ServiceControllerUtilsTests.LogFileName, "BogusService.OnStart (" + ServiceName + "): " + string.Join(" :: ", args) + "\n");
        }

        protected override void OnStop()
        {
            try
            {
                File.AppendAllText(ServiceControllerUtilsTests.LogFileName, "BogusService.OnStop (" + ServiceName + ")\n");
            } catch { }
        }
    }

    [RunInstaller(true)]
    public class BogusServiceInstaller : ServiceInstallerBase
    {
        public BogusServiceInstaller() : base(
            "XYZService",
            "XYZService DisplayName",
            "XYZService Description", 
            true)
        {
        }
    }

}