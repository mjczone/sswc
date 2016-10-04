param($installPath, $toolsPath, $package, $project)

#IF IN CONSOLE
#$projectName = "MyProject"
#$project = Get-Project $projectName
#$package = Get-Package -Filter Ssw.Cli
#$solution = Get-Interface $dte.Solution ([EnvDTE80.Solution2])

$solution = $project.DTE.Solution
$solutionDir = Split-Path $solution.FullName -parent
$batFile = Join-Path -Path $solutionDir -ChildPath "ssw-serve.bat"
$batFileInstall = Join-Path -Path $solutionDir -ChildPath "ssw-install.bat"
$batFileUninstall = Join-Path -Path $solutionDir -ChildPath "ssw-uninstall.bat"

$batExists = Test-Path $batFile
if($batExists){ 
    return #throw "serve.bat file already exists in solution directory" 
}

$projectDir = $project.Properties.Item("FullPath").Value #OR: Split-Path $project.FullName -parent
$binDir = Join-Path -Path $project.Properties.Item("FullPath").Value -ChildPath $project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value
$outputExeName = $project.Properties.Item("OutputFileName").Value
$outputExePath = Join-Path -Path $binDir -ChildPath $outputExeName

$batc = @"
@echo off
set /P uport=Port to run the api on (default: 2020):
if "%uport%"=="" GOTO usedefaultport
goto start
:usedefaultport
set uport=2020
:start
start "" http://localhost:%uport%/
:restart
$toolsPath\sswc.exe /assembly=$outputExeName /port=%uport% /bin=$binDir
cls
goto restart
"@
$batc | Set-Content $batFile -Encoding Ascii

$batc = @"
@echo off
set /P uport=Port to run the api on (default: 2020):
if "%uport%"=="" GOTO usedefaultport
goto start
:usedefaultport
set uport=2020
:start
$toolsPath\sswc.exe /assembly=$outputExeName /port=%uport% /bin=$binDir /install
"@
$batc | Set-Content $batFileInstall -Encoding Ascii

$batc = @"
@echo off
set /P uport=Port to run the api on (default: 2020):
if "%uport%"=="" GOTO usedefaultport
goto start
:usedefaultport
set uport=2020
:start
$toolsPath\sswc.exe /assembly=$outputExeName /port=%uport% /bin=$binDir /uninstall
"@
$batc | Set-Content $batFileUninstall -Encoding Ascii

$sfName = "Solution Items"
$sf = $solution.Projects | ?{ $_.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder -and $_.Name -eq $sfName }
if(!$sf) { $sf = $solution.AddSolutionFolder($sfName) }
$sf.ProjectItems.AddFromFile($batFile)
$sf.ProjectItems.AddFromFile($batFileInstall)
$sf.ProjectItems.AddFromFile($batFileUninstall)
