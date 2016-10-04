using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Ssw.Cli
{
    public abstract class ServiceInstallerBase: Installer
    {
        public const string ServiceNameParameterKey = "ServiceName";
        public const string ServiceDisplayNameParameterKey = "ServiceDisplayName";
        public const string ServiceDescriptionParameterKey = "ServiceDescription";

        protected readonly ServiceProcessInstaller InstallProcess;
        protected readonly ServiceInstaller InstallService;

        /// <summary>
        /// The base constructor allows you to configure how the service installer will
        /// name the service, allow overriding the name information for the service, and
        /// allow you to determine how any command line information should be generated 
        /// to add to the service assembly path.
        /// </summary>
        /// <param name="defaultServiceName">The default service name to use if not overriden</param>
        /// <param name="defaultServiceDisplayName">The default service display name to use if not overriden</param>
        /// <param name="defaultServiceDescription">The default service description to use if not overriden</param>
        /// <param name="allowOverwritingServiceName">Specify if the service name can be overriden via either Context.Parameters (e.g.: /ServiceName= parameter using InstallUtil) or via 'savedState' in a custom installer. Also applies to 'ServiceDisplayName' and 'ServiceDescription'</param>
        /// <param name="defaultServiceAccount">The default service account to use (can be changed later after the service is installed)</param>
        /// <param name="defaultServiceStartMode">The default service start mode to use (can be changed later after the service is installed)</param>
        protected ServiceInstallerBase(string defaultServiceName, string defaultServiceDisplayName,
            string defaultServiceDescription, bool allowOverwritingServiceName = false,
            ServiceAccount defaultServiceAccount = ServiceAccount.LocalService,
            ServiceStartMode defaultServiceStartMode = ServiceStartMode.Automatic)
        {
            DefaultServiceName = defaultServiceName;
            DefaultServiceDisplayName = defaultServiceDisplayName;
            DefaultServiceDescription = defaultServiceDescription;
            AllowOverwritingServiceName = allowOverwritingServiceName;

            InstallProcess = new ServiceProcessInstaller {Account = defaultServiceAccount};
            InstallService = new ServiceInstaller {StartType = defaultServiceStartMode};

            Installers.Add(InstallProcess);
            Installers.Add(InstallService);
        }

        public string DefaultServiceName { get; set; }
        public string DefaultServiceDisplayName { get; set; }
        public string DefaultServiceDescription { get; set; }
        public bool AllowOverwritingServiceName { get; set; }

        protected override void OnBeforeInstall(IDictionary savedState)
        {
#if DEBUG
            Log("OnBeforeInstall Context.Parameters:");
            foreach (var key in Context.Parameters.Keys.Cast<string>())
                Log($"- {key}: {Context.Parameters[key]}");
#endif
            // Format the command line here
            var assemblyPathArgs = GetServiceExeArgs(Context.Parameters);
            if (!string.IsNullOrWhiteSpace(assemblyPathArgs))
            {
                var assemblyPath = Path.GetFullPath(Context.Parameters["assemblypath"].Trim(' ', '\'', '"'));
                Context.Parameters["assemblypath"] = $"\"{assemblyPath}\" {assemblyPathArgs}";
            }

            InstallService.ServiceName = AllowOverwritingServiceName ? Context.Parameters[ServiceNameParameterKey] ?? DefaultServiceName : DefaultServiceName;
            InstallService.DisplayName = AllowOverwritingServiceName ? Context.Parameters[ServiceDisplayNameParameterKey] ?? DefaultServiceDisplayName : DefaultServiceDisplayName;
            InstallService.Description = AllowOverwritingServiceName ? Context.Parameters[ServiceDescriptionParameterKey] ?? DefaultServiceDescription : DefaultServiceDescription;

            base.OnBeforeInstall(savedState);
        }

        protected override void OnBeforeUninstall(IDictionary savedState)
        {
#if DEBUG
            Log("OnBeforeUninstall Context.Parameters:");
            foreach (var key in Context.Parameters.Keys.Cast<string>())
                Log($"- {key}: {Context.Parameters[key]}");
#endif

            InstallService.ServiceName = AllowOverwritingServiceName ? Context.Parameters[ServiceNameParameterKey] ?? DefaultServiceName : DefaultServiceName;
            InstallService.DisplayName = AllowOverwritingServiceName ? Context.Parameters[ServiceDisplayNameParameterKey] ?? DefaultServiceDisplayName : DefaultServiceDisplayName;
            InstallService.Description = AllowOverwritingServiceName ? Context.Parameters[ServiceDescriptionParameterKey] ?? DefaultServiceDescription : DefaultServiceDescription;

            base.OnBeforeUninstall(savedState);
        }

        /// <summary>
        /// A custom delegate that let's the caller define if and how to set the command-line arguments provided to the service executable.
        /// </summary>
        /// <param name="parameters">The arguments passed in when using the InstallUtil command</param>
        protected virtual string GetServiceExeArgs(StringDictionary parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return null;

            var sb = new StringBuilder();
            foreach (var key in parameters.Keys.Cast<string>())
            {
                if (key.StartsWith("_") ||
                    key.Equals(ServiceNameParameterKey, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(ServiceDisplayNameParameterKey, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals(ServiceDescriptionParameterKey, StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("install", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("uninstall", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("assemblypath", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("logfile", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = (parameters[key] ?? string.Empty).Trim(' ', '\'', '"');
                if (string.IsNullOrWhiteSpace(value))
                    sb.Append(" /" + key);
                else
                {
                    sb.Append(" /" + key + "=" + (value.Contains(" ") ? "\"" + value + "\"" : value));
                }
            }
            return sb.ToString().Trim(' ');
        }

        protected virtual void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}