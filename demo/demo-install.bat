call ..\src\sswc\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2024 /install /serviceName="DemoApi_1"
call ..\src\sswc\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2025 /install /serviceName="DemoApi_2"
call ..\src\sswc\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2026 /install /serviceName="DemoApi_3"
call start "" http://localhost:2024
call start "" http://localhost:2025
call start "" http://localhost:2026
