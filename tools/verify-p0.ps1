$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
. (Join-Path $PSScriptRoot 'EvidenceChecks.ps1')
$dotnetRoot = 'G:\godot\dotnet-sdk'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_ROOT_X64 = $dotnetRoot
$env:DOTNET_ROLL_FORWARD = 'LatestMajor'
$env:Path = "$dotnetRoot;$env:Path"

function Join-PhaseLogsDeterministically {
    param(
        [Parameter(Mandatory)][string]$OutputPath,
        [Parameter(Mandatory)][string[]]$Paths
    )

    $chunks = foreach ($path in $Paths) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Required P0 phase log is missing: $path"
        }
        Get-Content -LiteralPath $path -Raw
    }
    [System.IO.File]::WriteAllText($OutputPath, ($chunks -join [Environment]::NewLine),
        [System.Text.UTF8Encoding]::new($false))
}

$generatedScenes = @(
    'scenes\player\Player.tscn',
    'scenes\enemies\RustShellSquire.tscn',
    'scenes\enemies\GraveLampMoth.tscn',
    'scenes\enemies\BellBackLaborer.tscn',
    'scenes\mechanisms\BellChainMechanism.tscn',
    'scenes\prologue\rooms\AwakeningCourtyard.tscn',
    'scenes\prologue\rooms\BrokenToothCorridor.tscn',
    'scenes\prologue\rooms\UpperPendulum.tscn',
    'scenes\prologue\rooms\LowerBuriedMachinery.tscn',
    'scenes\prologue\rooms\BellChainMechanism.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)
$actorFormalPackedScenes = @(
    'scenes\presentation\RustShellFormalVisual.tscn',
    'scenes\presentation\GraveLampMothFormalVisual.tscn',
    'scenes\presentation\BellBackLaborerFormalVisual.tscn'
)
$semanticResourcePaths = @($generatedScenes + $actorFormalPackedScenes | Select-Object -Unique)
$builders = @(
    @{ Name = 'BuildPlayer'; Script = 'res://tools/build/BuildPlayer.cs' },
    @{ Name = 'BuildRustShellSquire'; Script = 'res://tools/build/BuildRustShellSquire.cs' },
    @{ Name = 'BuildGraveLampMoth'; Script = 'res://tools/build/prologue/BuildGraveLampMoth.cs' },
    @{ Name = 'BuildBellBackLaborer'; Script = 'res://tools/build/prologue/BuildBellBackLaborer.cs' },
    @{ Name = 'BuildBellChainMechanism'; Script = 'res://tools/build/prologue/BuildBellChainMechanism.cs' },
    @{ Name = 'BuildAwakeningCourtyard'; Script = 'res://tools/build/prologue/BuildAwakeningCourtyard.cs' },
    @{ Name = 'BuildBrokenToothCorridor'; Script = 'res://tools/build/prologue/BuildBrokenToothCorridor.cs' },
    @{ Name = 'BuildUpperPendulum'; Script = 'res://tools/build/prologue/BuildUpperPendulum.cs' },
    @{ Name = 'BuildLowerBuriedMachinery'; Script = 'res://tools/build/prologue/BuildLowerBuriedMachinery.cs' },
    @{ Name = 'BuildBellChainRoom'; Script = 'res://tools/build/prologue/BuildBellChainRoom.cs' },
    @{ Name = 'BuildPrologueLevel'; Script = 'res://tools/build/prologue/BuildPrologueLevel.cs' }
)
$snapshots = @{}
$snapshotResourceTexts = @{}
$logs = [System.Collections.Generic.List[string]]::new()
$intentionalNegativeTestWarnings = @(
    'WARNING: PresentationProfile: VisualScene root must implement IEntityVisual2D for VisualRoot.',
    "WARNING: PresentationProfile: cue 'Hurt' root must be Node2D.",
    "WARNING: RoomPresentationProfile: VisualScene root must be Node2D for 'VisualRoot'.",
    "WARNING: Guidance target 'MechanismBossExit' is missing in room 'BellChainMechanism'.",
    'WARNING: Rejected prologue flow transition Completed->Recovering.'
)
foreach ($relativePath in $generatedScenes) {
    $path = Join-Path $project $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required P0 generated scene is missing before verification: $path"
    }
    $snapshots[$relativePath] = [System.IO.File]::ReadAllText($path)
}
foreach ($relativePath in $semanticResourcePaths) {
    $path = Join-Path $project $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required P0 semantic resource is missing before verification: $path"
    }
    $resourcePath = 'res://' + $relativePath.Replace('\', '/')
    $snapshotResourceTexts[$resourcePath] = [System.IO.File]::ReadAllText($path)
}

try {
    & "$dotnetRoot\dotnet.exe" build (Join-Path $project 'Knight.csproj')
    if ($LASTEXITCODE) { throw "P0 C# build failed with exit code $LASTEXITCODE" }

    # Import before builders so a clean checkout registers newly added C# scene
    # scripts and their committed resource UIDs before a generated scene is reloaded.
    $importLog = Join-Path $project 'verify-p0-import.log'
    $importConsole = Join-Path $project 'verify-p0-import.console.log'
    $logs.Add($importLog); $logs.Add($importConsole)
    $importResult = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
        '--headless', '--path', $project, '--import', '--log-file', $importLog
    ) -ConsolePath $importConsole
    $importResult.Lines | ForEach-Object { $_ }
    if ($importResult.ExitCode) { throw "P0 import failed with exit code $($importResult.ExitCode)" }
    Assert-NoEngineLogErrors -Paths @($importLog, $importConsole)

    foreach ($builder in $builders) {
        $log = Join-Path $project ("verify-p0-builder-{0}.log" -f $builder.Name)
        $console = Join-Path $project ("verify-p0-builder-{0}.console.log" -f $builder.Name)
        $logs.Add($log); $logs.Add($console)
        $builderResult = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
            '--headless', '--path', $project, '--script', $builder.Script, '--log-file', $log
        ) -ConsolePath $console
        $builderResult.Lines | ForEach-Object { $_ }
        if ($builderResult.ExitCode) { throw "P0 scene builder failed: $($builder.Name)" }
        Assert-NoEngineLogErrors -Paths @($log, $console)
    }
    $builtResourceTexts = @{}
    foreach ($relativePath in $semanticResourcePaths) {
        $resourcePath = 'res://' + $relativePath.Replace('\', '/')
        $builtResourceTexts[$resourcePath] = [System.IO.File]::ReadAllText(
            (Join-Path $project $relativePath))
    }
    foreach ($relativePath in $generatedScenes) {
        $resourcePath = 'res://' + $relativePath.Replace('\', '/')
        $actual = $builtResourceTexts[$resourcePath]
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $snapshots[$relativePath] `
            -ActualText $actual -Label $relativePath `
            -ExpectedResourceTexts $snapshotResourceTexts `
            -ActualResourceTexts $builtResourceTexts
    }

    $testsPreLog = Join-Path $project 'verify-p0-tests-pre.log'
    $testsPreConsole = Join-Path $project 'verify-p0-tests-pre.console.log'
    $testsPostLog = Join-Path $project 'verify-p0-tests-post.log'
    $testsPostConsole = Join-Path $project 'verify-p0-tests-post.console.log'
    $testsLog = Join-Path $project 'verify-p0-tests.log'
    $testsConsole = Join-Path $project 'verify-p0-tests.console.log'
    $logs.Add($testsLog); $logs.Add($testsConsole)
    foreach ($phaseRun in @(
        @{ Phase = 'pre'; Log = $testsPreLog; Console = $testsPreConsole },
        @{ Phase = 'post'; Log = $testsPostLog; Console = $testsPostConsole })) {
        $testsResult = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
            '--headless', '--path', $project, '--script', 'res://tests/RunPrologueTests.cs',
            '--log-file', $phaseRun.Log, '--', "--phase=$($phaseRun.Phase)"
        ) -ConsolePath $phaseRun.Console
        $testsResult.Lines | ForEach-Object { $_ }
        if ($testsResult.ExitCode) {
            throw "P0 test phase '$($phaseRun.Phase)' failed with exit code $($testsResult.ExitCode)"
        }
        Assert-ExactLogMarker -Path $phaseRun.Log -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Log -Marker "PROLOGUE_TEST_PHASE_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$($phaseRun.Phase)"
        Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_OK phase=$($phaseRun.Phase)"
        $overallCount = @((Get-Content -LiteralPath $phaseRun.Log) |
            Where-Object { $_ -ceq 'PROLOGUE_TESTS_OK' }).Count
        $consoleOverallCount = @((Get-Content -LiteralPath $phaseRun.Console) |
            Where-Object { $_ -ceq 'PROLOGUE_TESTS_OK' }).Count
        $expectedOverallCount = if ($phaseRun.Phase -eq 'post') { 1 } else { 0 }
        if ($overallCount -ne $expectedOverallCount) {
            throw "P0 test phase '$($phaseRun.Phase)' expected PROLOGUE_TESTS_OK count $expectedOverallCount; found $overallCount."
        }
        if ($consoleOverallCount -ne $expectedOverallCount) {
            throw "P0 test phase '$($phaseRun.Phase)' console expected PROLOGUE_TESTS_OK count $expectedOverallCount; found $consoleOverallCount."
        }
    }
    Join-PhaseLogsDeterministically -OutputPath $testsLog -Paths @($testsPreLog, $testsPostLog)
    Join-PhaseLogsDeterministically -OutputPath $testsConsole -Paths @($testsPreConsole, $testsPostConsole)
    Assert-ExactLogMarker -Path $testsLog -Marker 'PROLOGUE_TESTS_OK'
    Assert-ExactLogMarker -Path $testsConsole -Marker 'PROLOGUE_TESTS_OK'
    Assert-EngineLogContract -Path $testsLog `
        -ExpectedWarnings $intentionalNegativeTestWarnings
    Assert-EngineLogContract -Path $testsConsole `
        -ExpectedWarnings $intentionalNegativeTestWarnings

    $smokeLog = Join-Path $project 'verify-p0-smoke.log'
    $smokeConsole = Join-Path $project 'verify-p0-smoke.console.log'
    $logs.Add($smokeLog); $logs.Add($smokeConsole)
    $smokeResult = Invoke-MergedNativeCommand -FilePath $godot -ArgumentList @(
        '--headless', '--path', $project, '--script', 'res://tests/smoke/P0CoreSliceSmoke.cs',
        '--log-file', $smokeLog
    ) -ConsolePath $smokeConsole
    $smokeResult.Lines | ForEach-Object { $_ }
    if ($smokeResult.ExitCode) { throw "P0 smoke failed with exit code $($smokeResult.ExitCode)" }
    Assert-ExactLogMarker -Path $smokeLog -Marker 'P0_CORE_SLICE_OK'
    Assert-ExactLogMarker -Path $smokeConsole -Marker 'P0_CORE_SLICE_OK'
    foreach ($scanPath in $logs.ToArray()) {
        if ($scanPath -ceq $testsLog -or $scanPath -ceq $testsConsole) {
            Assert-EngineLogContract -Path $scanPath `
                -ExpectedWarnings $intentionalNegativeTestWarnings
        }
        else { Assert-NoEngineLogErrors -Paths @($scanPath) }
    }
    'P0_VERIFY_OK'
}
finally {
    foreach ($relativePath in $generatedScenes) {
        $path = Join-Path $project $relativePath
        $current = if (Test-Path -LiteralPath $path -PathType Leaf) { [System.IO.File]::ReadAllText($path) } else { $null }
        if ($current -cne $snapshots[$relativePath]) {
            [System.IO.File]::WriteAllText($path, $snapshots[$relativePath], [System.Text.UTF8Encoding]::new($false))
        }
    }
}
