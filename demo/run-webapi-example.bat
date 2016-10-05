echo off
set msbuildit="C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe"
call %msbuildit% ..\src\sswc\sswc.csproj /p:Configuration=Debug
call %msbuildit% .\WebApiExampleApp\WebApiExampleApp.csproj /p:Configuration=Debug
call start "" http://localhost:2026
call ..\src\sswc\bin\debug\sswc.exe WebApiExampleApp.dll /bin=WebApiExampleApp\bin\Debug /type=WebApiExampleApp.WebApiHostWrapper /port=2026