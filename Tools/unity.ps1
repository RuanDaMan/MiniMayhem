# Helper for running Unity in batch mode against this project.
#   ./Tools/unity.ps1 build          -> regenerate assets/prefabs/scenes (Mini Mayhem/Rebuild Project Assets)
#   ./Tools/unity.ps1 compile        -> just import + compile
#   ./Tools/unity.ps1 player         -> build the Windows player into Builds/Windows
#   ./Tools/unity.ps1 test EditMode  -> run edit-mode tests
#   ./Tools/unity.ps1 test PlayMode  -> run play-mode tests
param([string]$mode = "build", [string]$platform = "EditMode", [string]$filter = "")

$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe"
$project = Split-Path -Parent $PSScriptRoot
$logDir = Join-Path $project "Logs"
New-Item -ItemType Directory -Force $logDir | Out-Null
$log = Join-Path $logDir "batch_$mode.log"

$uargs = @("-batchmode", "-projectPath", $project, "-logFile", $log)
switch ($mode) {
    "build"   { $uargs += @("-quit", "-executeMethod", "MiniMayhem.EditorTools.ProjectBuilder.BuildAll") }
    "compile" { $uargs += @("-quit") }
    "player"  { $uargs += @("-quit", "-executeMethod", "MiniMayhem.EditorTools.MiniMayhemMenu.BuildWindowsPlayer") }
    "test"    {
        $results = Join-Path $logDir "TestResults_$platform.xml"
        $uargs += @("-runTests", "-testPlatform", $platform, "-testResults", $results)
        if ($filter) { $uargs += @("-testFilter", $filter) }
    }
}

$p = Start-Process -FilePath $unity -ArgumentList $uargs -PassThru -NoNewWindow
$p.WaitForExit()
Write-Output "Unity exit code: $($p.ExitCode)"
$errors = Select-String -Path $log -Pattern "error CS\d+|Exception:|\[MiniMayhem\]|Scripts have compiler errors|Shader not found|NullReferenceException" | Select-Object -First 60
$errors | ForEach-Object { $_.Line }
if ($mode -eq "test") {
    $results = Join-Path $logDir "TestResults_$platform.xml"
    if (Test-Path $results) {
        [xml]$x = Get-Content $results
        $r = $x.'test-run'
        Write-Output "Tests: total=$($r.total) passed=$($r.passed) failed=$($r.failed) skipped=$($r.skipped)"
        $x.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
            Write-Output "FAILED: $($_.fullname)"
            Write-Output ("  " + $_.failure.message.InnerText)
        }
    } else { Write-Output "No test results file produced." }
}



