call ..\src\Ssw.Cli\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2024 /install /serviceName="DemoApi 1"
call ..\src\Ssw.Cli\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2025 /install /serviceName="DemoApi 2"
call ..\src\Ssw.Cli\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2026 /install /serviceName="DemoApi 3"
call start "" http://localhost:2024
call start "" http://localhost:2025
call start "" http://localhost:2026
