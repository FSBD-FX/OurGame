param([switch]$SkipSmoke)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$evidenceTest = Join-Path $project 'tests\powershell\EvidenceChecks.Tests.ps1'
$evidenceResult = @(& $evidenceTest)
if (@($evidenceResult | Where-Object { $_ -ceq 'EVIDENCE_CHECKS_OK' }).Count -ne 1) {
    throw "PowerShell evidence checks did not complete successfully: $evidenceTest"
}
$evidenceResult
$preferredDotnetRoot = 'G:\godot\dotnet-sdk'
$fallbackDotnetRoot = 'F:\Documents\' + [string][char]0x6E38 + [char]0x620F + '\tools\runtime\dotnet-9.0.317'
if (Test-Path -LiteralPath (Join-Path $preferredDotnetRoot 'dotnet.exe')) {
    $dotnetRoot = $preferredDotnetRoot
} elseif (Test-Path -LiteralPath (Join-Path $fallbackDotnetRoot 'dotnet.exe')) {
    $dotnetRoot = $fallbackDotnetRoot
} else {
    throw "No supported .NET runtime found. Expected $preferredDotnetRoot or $fallbackDotnetRoot."
}
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"
& "$dotnetRoot\dotnet.exe" build (Join-Path $project 'Knight.csproj')
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$logs = [System.Collections.Generic.List[string]]::new()
$generatedScenes = @(
    'scenes\player\Player.tscn',
    'scenes\enemies\RustShellSquire.tscn',
    'scenes\prototype\PrototypeRoom.tscn'
)
$sceneSnapshots = @{}
foreach ($relativePath in $generatedScenes) {
    $path = Join-Path $project $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required generated scene is missing before verification: $path"
    }
    $sceneSnapshots[$relativePath] = [System.IO.File]::ReadAllText($path)
}
$builders = @(
    @{ Name = 'BuildPlayer'; Scene = $generatedScenes[0] },
    @{ Name = 'BuildRustShellSquire'; Scene = $generatedScenes[1] },
    @{ Name = 'BuildPrototypeRoom'; Scene = $generatedScenes[2] }
)
Assert-RequiredBuilderPlan -BuilderNames @($builders | ForEach-Object { $_.Name })
try {
    try {
        foreach ($builder in $builders) {
            $builderLog = Join-Path $project ("verify-builder-{0}.log" -f $builder.Name); $logs.Add($builderLog)
            & $godot --headless --path $project --script ("res://tools/build/{0}.cs" -f $builder.Name) --log-file $builderLog
            if ($LASTEXITCODE) { throw "Scene builder failed with exit code $LASTEXITCODE`: $($builder.Name)" }
            Assert-NoEngineLogErrors -Paths @($builderLog)
        }
        foreach ($relativePath in $generatedScenes) {
            $path = Join-Path $project $relativePath
            $generatedText = [System.IO.File]::ReadAllText($path)
            Assert-GodotSceneSemanticallyEquivalent -ExpectedText $sceneSnapshots[$relativePath] `
                -ActualText $generatedText -Label $relativePath
        }
    }
    finally {
        foreach ($relativePath in $generatedScenes) {
            $path = Join-Path $project $relativePath
            $currentText = if (Test-Path -LiteralPath $path -PathType Leaf) {
                [System.IO.File]::ReadAllText($path)
            } else { $null }
            if ($currentText -cne $sceneSnapshots[$relativePath]) {
                [System.IO.File]::WriteAllText($path, $sceneSnapshots[$relativePath], [System.Text.UTF8Encoding]::new($false))
            }
        }
    }
$importLog = Join-Path $project 'verify-import.log'; $logs.Add($importLog)
& $godot --headless --path $project --import --log-file $importLog
if ($LASTEXITCODE) { throw "Godot import failed with exit code $LASTEXITCODE" }
$unitLog = Join-Path $project 'verify-unit.log'; $logs.Add($unitLog)
& $godot --headless --path $project --script 'res://tests/RunPrototypeTests.cs' --log-file $unitLog
if ($LASTEXITCODE) { throw "Prototype tests failed with exit code $LASTEXITCODE" }
Assert-ExactLogMarker -Path $unitLog -Marker 'PROTOTYPE_TESTS_OK'
$smoke = Join-Path $project 'tests\smoke\PrototypeSmoke.cs'
if (-not $SkipSmoke -and (Test-Path -LiteralPath $smoke)) {
    $smokeLog = Join-Path $project 'verify-smoke.log'; $logs.Add($smokeLog)
    & $godot --headless --path $project --script 'res://tests/smoke/PrototypeSmoke.cs' --log-file $smokeLog
    if ($LASTEXITCODE) { throw "Prototype smoke failed with exit code $LASTEXITCODE" }
    Assert-ExactLogMarker -Path $smokeLog -Marker 'PROTOTYPE_SMOKE_OK'
} elseif (-not $SkipSmoke) {
    throw "Required smoke script is missing: $smoke"
}
Assert-NoEngineLogErrors -Paths $logs.ToArray()
}
finally {
    foreach ($relativePath in $generatedScenes) {
        $path = Join-Path $project $relativePath
        $currentText = if (Test-Path -LiteralPath $path -PathType Leaf) {
            [System.IO.File]::ReadAllText($path)
        } else { $null }
        if ($currentText -cne $sceneSnapshots[$relativePath]) {
            [System.IO.File]::WriteAllText($path, $sceneSnapshots[$relativePath], [System.Text.UTF8Encoding]::new($false))
        }
    }
}
