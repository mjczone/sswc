using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

namespace Ssw.Cli
{
    public static class ServiceControllerUtils
    {
        public static bool ServiceExists(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));
            
            return ServiceController.GetServices().Any(s => s.ServiceName == serviceName);

            //OLD WAY
            //ServiceController controller = null;
            //try
            //{
            //    controller = new ServiceController(serviceName);
            //    var status = controller.Status;
            //    return true;
            //}
            //catch (InvalidOperationException)
            //{
            //    return false;
            //}
            //finally
            //{
            //    controller?.Dispose();
            //}
        }

        public static ServiceControllerStatus? ServiceStatus(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));
            
            return ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName)?.Status;
        }

        public static void InstallService(Assembly serviceAssembly, string serviceName, string serviceDisplayName = null, string serviceDescription = null, params string[] commandLine)
        {
            if(serviceAssembly == null)
                throw new ArgumentNullException(nameof(serviceAssembly));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            Console.WriteLine("Installing service " + serviceName + " with assembly " + serviceAssembly.FullName);

            if (ServiceExists(serviceName))
            {
                Console.WriteLine("Service " + serviceName + " already exists");
                return;
            }

            // another way
            // ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });

            // The commandLine args can be processed using Context.Parameters (in the OnBeforeInstall method of the installer)
            commandLine = GetCommandLineForInstaller(serviceName, serviceDisplayName, serviceDescription, commandLine);
            Console.WriteLine("AssemblyPath: " + serviceAssembly.GetName().Name + " " + string.Join(" ", commandLine ?? new string[0]));
            using (var installer = GetInstaller(serviceAssembly, commandLine))
            {
                var state = new Hashtable();
                try
                {
                    installer.Install(state);
                    installer.Commit(state);
                }
                catch
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch
                    {
                        // ignored
                    }
                    throw;
                }
            }
        }

        public static void UninstallService(Assembly serviceAssembly, string serviceName, params string[] commandLine)
        {
            if (serviceAssembly == null)
                throw new ArgumentNullException(nameof(serviceAssembly));

            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            Console.WriteLine("Uninstalling service " + serviceName + " with assembly " + serviceAssembly.FullName);

            if (!ServiceExists(serviceName))
            {
                Console.WriteLine("Service " + serviceName + " does not exist");
                return;
            }

            StopService(serviceName);

            // another way
            // ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });

            // The commandLine args can be processed using Context.Parameters (in the OnBeforeUninstall method of the installer)
            commandLine = GetCommandLineForInstaller(serviceName, null, null, commandLine);
            Console.WriteLine("AssemblyPath: " + serviceAssembly.GetName().Name + " " + string.Join(" ", commandLine ?? new string[0]));
            using (var installer = GetInstaller(serviceAssembly, commandLine))
            {
                var state = new Hashtable();
                try
                {
                    installer.Uninstall(state);
                }
                catch
                {
                    try
                    {
                        installer.Rollback(state);
                    }
                    catch
                    {
                        // ignore the rollback exception
                    }
                    throw;
                }
            }
        }

        private static string[] GetCommandLineForInstaller(string serviceName, string serviceDisplayName, string serviceDescription, string[] commandLine)
        {
            var commandLineArgs = new List<string>();
            if (commandLine != null) commandLineArgs.AddRange(commandLine);
            commandLineArgs.Add("/" + ServiceInstallerBase.ServiceNameParameterKey + "=" + serviceName);
            if (!string.IsNullOrWhiteSpace(serviceDisplayName)) commandLineArgs.Add("/" + ServiceInstallerBase.ServiceDisplayNameParameterKey + "=" + serviceDisplayName);
            if (!string.IsNullOrWhiteSpace(serviceDescription)) commandLineArgs.Add("/" + ServiceInstallerBase.ServiceDescriptionParameterKey + "=" + serviceDescription);
            commandLine = commandLineArgs.ToArray();
            return commandLine;
        }

        public static void StartService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            if (!ServiceExists(serviceName))
                return;

            using (var controller = new ServiceController(serviceName))
            {
                if (controller.Status == ServiceControllerStatus.Running)
                    return;

                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            }
        }

        public static void StopService(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentNullException(nameof(serviceName));

            if (!ServiceExists(serviceName))
                return;

            using (var controller = new ServiceController(serviceName))
            {
                if (controller.Status == ServiceControllerStatus.Stopped)
                    return;

                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            }
        }

        private static AssemblyInstaller GetInstaller(Assembly assembly, string[] commandLine = null)
        {
            return new AssemblyInstaller(assembly, commandLine) { UseNewContext = true };
        }
    }
}