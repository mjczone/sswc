echo off
set msbuildit="C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe"
call %msbuildit% ..\src\sswc\sswc.csproj /p:Configuration=Debug
call %msbuildit% .\ServiceStackExampleApp\ServiceStackExampleApp.csproj /p:Configuration=Debug
call start "" http://localhost:2027
call ..\src\sswc\bin\debug\sswc.exe ServiceStackExampleApp\bin\Debug\ServiceStackExampleApp.dll /port=2027