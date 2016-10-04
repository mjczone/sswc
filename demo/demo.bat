echo off
set msbuildit="C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe"
call %msbuildit% ..\src\sswc\sswc.csproj /p:Configuration=Debug
call %msbuildit% .\demo\demo.csproj /p:Configuration=Debug
cls
call start "" http://localhost:2024
call ..\src\sswc\bin\debug\sswc.exe .\demo\bin\Debug\DemoApi.dll /port=2024
