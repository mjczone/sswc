using System;
using System.Collections;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Management.Instrumentation;
using System.ServiceProcess;
using System.Text;

namespace Ssw.Cli
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // Normalize args
            args = NormalizeArgs(args);

            // Parse the arguments
            ProgramArgs pArgs;
            if (!TryGetProgramArgs(args, out pArgs))
                return;

            if (pArgs.Install)
            {
                ServiceControllerUtils.InstallService(typeof(SswService).Assembly, 
                    pArgs.ServiceName, 
                    pArgs.ServiceDisplayName, 
                    pArgs.ServiceDescription, 
                    pArgs.GetServiceInstallerArgs());
                ServiceControllerUtils.StartService(pArgs.ServiceName);
            } 
            else if (pArgs.Uninstall)
            {
                ServiceControllerUtils.UninstallService(typeof(SswService).Assembly,
                    pArgs.ServiceName,
                    pArgs.GetServiceInstallerArgs());
            }
            else if (Environment.UserInteractive)
            {
                using (var service = new SswService())
                {
                    service.Start(pArgs);
                    if (!Console.IsInputRedirected)
                    {
                        Console.WriteLine("System running; press any key to stop");
                        Console.ReadKey(true);
                    }
                }
            }
            else
            {
                // running as service   
                ServiceBase[] services = { new SswService(pArgs) };
                ServiceBase.Run(services);
            }
        }

        private static string[] NormalizeArgs(string[] args)
        {
            if (args == null)
                return new string[0];

            if (args.Length == 0 || args[0].Length == 0)
                return args;

            var firstChar = args[0].First();

            if (!(new[] { '/', '-' }.Contains(firstChar)))
            {
                args[0] = "/assembly=" + args[0];
            }

            return args;
        }
        
        private static bool TryGetProgramArgs(string[] args, out ProgramArgs pArgs)
        {
            pArgs = null;

            try
            {
                if (args == null || args.Length == 0 || args.As<HelpArgs>().Help)
                {
                    throw new ArgumentException(nameof(args));
                }
            }
            catch (ArgumentException)
            {
                Console.WriteLine(ConsoleArgs.HelpFor<ProgramArgs>(false));
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine(ConsoleArgs.HelpFor<ProgramArgs>(true));
                return false;
            }

            try
            {
                pArgs = args.As<ProgramArgs>();

                pArgs.ServiceName = string.IsNullOrWhiteSpace(pArgs.ServiceName) ? null: pArgs.ServiceName.Replace(" ", "_").Trim(' ', '\"', '\'');
                pArgs.ServiceDisplayName = string.IsNullOrWhiteSpace(pArgs.ServiceDisplayName) ? pArgs.ServiceName: pArgs.ServiceDisplayName.Trim(' ', '\"', '\'');
                pArgs.ServiceDescription = string.IsNullOrWhiteSpace(pArgs.ServiceDescription) ? null : pArgs.ServiceDescription.Trim(' ', '\"', '\'');
                pArgs.Watch = string.IsNullOrWhiteSpace(pArgs.Watch) ? null : pArgs.Watch.Trim(' ', '\"', '\'');
                pArgs.AppHostAssembly = string.IsNullOrWhiteSpace(pArgs.AppHostAssembly) ? null : pArgs.AppHostAssembly.Trim(' ', '\"', '\'');
                pArgs.AppHostType = string.IsNullOrWhiteSpace(pArgs.AppHostType) ? null : pArgs.AppHostType.Trim(' ', '\"', '\'');
                pArgs.BinDirectory = string.IsNullOrWhiteSpace(pArgs.BinDirectory) ? null : pArgs.BinDirectory.Trim(' ', '\"', '\'');

                if (!string.IsNullOrEmpty(pArgs.AppHostAssembly) && string.IsNullOrEmpty(pArgs.BinDirectory))
                    pArgs.AppHostAssembly = new FileInfo(pArgs.AppHostAssembly).FullName;

                if (!string.IsNullOrEmpty(pArgs.BinDirectory))
                    pArgs.BinDirectory = new DirectoryInfo(pArgs.BinDirectory).FullName;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("There are problems with the command line arguments.");
                Console.WriteLine();
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine(ConsoleArgs.HelpFor<ProgramArgs>());
                Console.ResetColor();
                return false;
            }

            return true;
        }
    }
}