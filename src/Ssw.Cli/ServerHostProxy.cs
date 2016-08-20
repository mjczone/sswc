using System;
using System.Linq;
using System.Reflection;

namespace Ssw.Cli
{
    [Serializable]
    public class ServerHostProxy : MarshalByRefObject
    {
        private object _appHost;
        private MethodInfo _appHostStopMethod;

        public string Start(string assemblyFile, string appHostTypeName, int port)
        {
            try
            {
                StartAppHostContainer(assemblyFile, appHostTypeName, port);
                return null;
            }
            catch (Exception ex)
            {
                return ex.GetType().Name + ": " + ex.Message;
            }
        }

        public void Stop()
        {
            if (_appHost == null) return;

            try
            {
                _appHostStopMethod?.Invoke(_appHost, null);
            }
            catch
            {
                // ignored
            }
        }

        private void StartAppHostContainer(string assemblyFile, string appHostTypeName, int port)
        {
            var assemblyWithAppHost = Assembly.LoadFrom(assemblyFile);

            var appHostType = GetAppHostType(assemblyWithAppHost, appHostTypeName);

            try
            {
                _appHost = Activator.CreateInstance(appHostType);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(nameof(appHostType), "An error occurred when trying to activate the app host '" + appHostTypeName + "' : " + ex.Message);
            }

            var appHostInitMethod = appHostType.GetMethods().FirstOrDefault(m => !m.IsStatic && m.IsPublic && m.Name.Equals("Init", StringComparison.Ordinal));
            var appHostStartMethod = appHostType.GetMethods().FirstOrDefault(m => !m.IsStatic && m.IsPublic && m.Name.Equals("Start", StringComparison.Ordinal));
            _appHostStopMethod = appHostType.GetMethods().FirstOrDefault(m => !m.IsStatic && m.IsPublic && m.Name.Equals("Stop", StringComparison.Ordinal));

            appHostInitMethod?.Invoke(_appHost, null);
            if (appHostStartMethod != null)
            {
                appHostStartMethod.Invoke(_appHost,
                    appHostStartMethod.GetParameters().Length == 1 ? new object[] {$"http://*:{port}/"} : null);
            }
            
        }

        private static Type GetAppHostType(Assembly assemblyWithAppHost, string appHostTypeName)
        {
            var appHostType = string.IsNullOrWhiteSpace(appHostTypeName) ? null :
                assemblyWithAppHost.GetType(appHostTypeName);

            if (appHostType == null && !string.IsNullOrWhiteSpace(appHostTypeName))
            {
                throw new ArgumentException("Unable to locate type " + appHostTypeName + " in assembly " + assemblyWithAppHost.FullName);
            }

            if (appHostType != null && appHostType.GetConstructor(Type.EmptyTypes) == null)
            {
                // apphost type must either be a valid ServiceStack AppHost, or contain either an 'Init()' method and/or a 'Start(params string[] / string)' method;
                throw new ArgumentException("Type " + appHostTypeName + " is not a valid server host implementation. Make sure it has an empty constructor.");
            }

            if (appHostType != null)
                return appHostType;

            var appHostTypes = assemblyWithAppHost.GetTypes()
                .Where(f => f.IsClass && f.IsPublic && !f.IsAbstract && IsAppHost(f))
                .ToArray();

            if (appHostTypes.Length == 0)
            {
                throw new ArgumentException("Unable to locate a valid server host implementation inside " + assemblyWithAppHost.GetName().Name);
            }

            if (appHostTypes.Length > 1)
            {
                throw new ArgumentException("Found multiple apphost types:\n\t- " + string.Join("\n\t- ", appHostTypes.Select(t => t.AssemblyQualifiedName)) +
                                            "\n\nUse the /type= argument to specify the type to use");
            }

            return appHostTypes[0];
        }

        private static bool IsAppHost(Type t)
        {
            Type cur = t.BaseType;

            while (cur != null)
            {
                if (cur.Name.Equals("AppSelfHostBase"))
                {
                    return t.GetConstructor(Type.EmptyTypes) != null;
                }

                cur = cur.BaseType;
            }

            return false;
        }
    }
}