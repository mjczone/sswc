@echo off
for %%i in ("%~dp0.") do SET "spath=%%~fi"
set dist=%spath%\dist
mkdir "%dist%"
del "%dist%\sswc.zip"
"C:\Program Files (x86)\MSBuild\14.0\Bin\MsBuild.exe" src\sswc\sswc.csproj /p:Configuration=Release /p:OutputPath="%dist%"
lib\7za.exe a dist\sswc.zip "%dist%\*"
cd "%dist%"
for /f %%F in ('dir /b /a-d ^| findstr /vile ".zip .md"') do del "%%F"
cd ..
