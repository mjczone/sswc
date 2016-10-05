# sswc

## Background

Ever built a self-hosted api and gone through the pain of having to constantly restart it to re-compile your code, for simple updates?
Do you build SPAs with .NET server backends, and don't want to kill your dev flow as you go back and forth between client / server code?

'sswc' is a simple command line interface that can hopefully ease that pain. Use it to run an http server for development purposes, or use it to install your class library as a windows service, and never lock your assemblies again, allowing you to re-compile your code
into the same bin directory you are serving your server from, and 'sswc' will restart the server for you.

The http server automatically restarts when files are updated in the bin directory based on a watch 'regex' pattern. All you need is a class with an Init and/or Start method in it. Use the /type={classname} argument and you're good to go.

We love "ServiceStack", so we simplified our usage of ServiceStack by removing the need for the /type flag if you intend to use ServiceStack. Simply [create a class](https://github.com/mjczone/sswc/blob/master/demo/demo/AppHost.cs#L13) that derives from [ServiceStack.AppSelfHostBase](https://github.com/ServiceStack/ServiceStack/wiki/Self-hosting), and 'sswc' will locate it in your assembly for you.

## Getting Started

Download the 'sswc.zip' file and unzip it to your directory of choice ...

```
curl -L -o sswc.zip https://github.com/mjczone/sswc/raw/master/dist/sswc.zip
```

Run the cli app from a command prompt

```
sswc.exe /help
```

## Usage

Simplest usage:

```
sswc .\MyProject\bin\Debug\MyProject.dll /type=MyProject.MyHost 
```

If the MyHost class above derives from the ServiceStack.AppSelfHostBase class, it's even simpler:

```
sswc .\MyProject\bin\Debug\MyProject.dll
```

Specify the port (uses 2020 by default):

```
sswc .\MyProject\bin\Debug\MyProject.dll /type=MyProject.MyHost /port=4000
```

Use polling instead of relying on file system events for bin directory change notifications (useful in certain situations when using UNC paths and/or mapped drives):

```
sswc .\MyProject\bin\Debug\MyProject.dll /type=MyProject.MyHost /poll=3000
```

If you only care to watch for *.dll changes, specify the watch pattern as a Regex:

```
sswc .\MyProject\bin\Debug\MyProject.dll /type=MyProject.MyHost /watch=\*.dll
```

## Windows Service Commands

You can use 'sswc' to install your Host class library as a Windows Service.

To install the windows service, use:

```
sswc .\MyProject\bin\Debug\MyProject.dll /type=MyProject.MyHost /port=8081 /install /serviceName="MyProject_Host" /serviceDisplayName="MyProject Host" /serviceDescription="API for MyProject served on port 8081"
```

To uninstall the windows service, use:

```
sswc /uninstall /serviceName="MyProject_Host"
```

## Examples

### Sample using Nancy

The following example would respond to the following command:

```
sswc NancyHostAssembly.dll /type=NancyHostAssembly.NancyHostWrapper
```

Nancy class:

```
namespace NancyHostAssembly 
{
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
             Console.WriteLine("Running on " + urlBase);
             Console.ReadLine();
        }

        public void Stop()
        {
            if(_host != null) {
                _host.Stop();
                _host.Dispose();
            }
        }
    }
}
```

### Sample using ServiceStack

The following example would respond to the following command:

```
sswc ServiceStackHostAssembly.dll /type=ServiceStackHostAssembly.MyServiceStackHost
```

ServiceStack class:

```
namespace ServiceStackHostAssembly 
{
    public class MyServiceStackHost: AppSelfHostBase
    {
        public AppHost(): base("Sample API", typeof(MyServiceStackHost).Assembly) { }
        
        public override ServiceStackHost Init() // optional
        {
            return base.Init();
        }

        public override ServiceStackHost Start(string urlBase) // optional
        {
            return base.Start(urlBase);
        }

        public override void Stop() // optional
        {
            base.Stop();
        }

        public override void Configure(Container container)
        {
        }
    }
}
```

## Issues/questions

[Create an issue](https://github.com/mjczone/sswc/issues) ...

## Collaboration

Pull requests are welcome.

## License

MIT - 2016


