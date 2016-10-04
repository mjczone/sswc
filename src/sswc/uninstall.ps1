param($installPath, $toolsPath, $package, $project)

$solution = $project.DTE.Solution
$solutionDir = Split-Path $solution.FullName -parent
$batFile = Join-Path -Path $solutionDir -ChildPath "ssw-serve.bat"
$batFileInstall = Join-Path -Path $solutionDir -ChildPath "ssw-install.bat"
$batFileUninstall = Join-Path -Path $solutionDir -ChildPath "ssw-uninstall.bat"

$sfName = "Solution Items"
$sf = $solution.Projects | ?{ $_.Kind -eq [EnvDTE80.ProjectKinds]::vsProjectKindSolutionFolder -and $_.Name -eq $sfName }
if($sf) {
  $sfItem = $sf.ProjectItems | where { ($_.Name -eq "ssw-serve.bat") }
  if($sfItem) { $sfItem.Remove() }
  $sfItem = $sf.ProjectItems | where { ($_.Name -eq "ssw-install.bat") }
  if($sfItem) { $sfItem.Remove() }
  $sfItem = $sf.ProjectItems | where { ($_.Name -eq "ssw-uninstall.bat") }
  if($sfItem) { $sfItem.Remove() }
}

If (Test-Path $batFile){ Remove-Item $batFile }
If (Test-Path $batFileInstall){ Remove-Item $batFileInstall }
If (Test-Path $batFileUninstall){ Remove-Item $batFileUninstall }