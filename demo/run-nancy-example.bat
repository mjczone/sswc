echo off
set msbuildit="C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe"
call %msbuildit% ..\src\sswc\sswc.csproj /p:Configuration=Debug
call %msbuildit% .\NancyExampleApp\NancyExampleApp.csproj /p:Configuration=Debug
call start "" http://localhost:2025
call ..\src\sswc\bin\debug\sswc.exe NancyExampleApp.dll /bin=NancyExampleApp\bin\Debug /type=NancyExampleApp.NancyHostWrapper /port=2025