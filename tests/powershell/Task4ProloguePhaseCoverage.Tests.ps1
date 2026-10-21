[CmdletBinding()]
param(
    [string]$SourcePath,
    [string]$VerifierPath,
    [switch]$SkipMutation,
    [switch]$SkipNative
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $SourcePath = Join-Path $project 'tests\RunPrologueTests.cs'
}
if ([string]::IsNullOrWhiteSpace($VerifierPath)) {
    $VerifierPath = Join-Path $project 'tools\verify-prologue.ps1'
}
$source = Get-Content -LiteralPath $SourcePath -Raw
$verifier = Get-Content -LiteralPath $VerifierPath -Raw
$evidenceRoot = Join-Path $project 'screenshots\task4-capture-exit'
$godot = 'G:\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe'

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Get-MethodBody([string]$Name) {
    $start = $source.IndexOf("private static async Task $Name(SceneTree tree)", [StringComparison]::Ordinal)
    Assert-True ($start -ge 0) "RunPrologueTests exposes phase method $Name"
    $next = if ($Name -eq 'RunPrePhase') {
        $source.IndexOf('private static async Task RunPostPhase(SceneTree tree)', $start, [StringComparison]::Ordinal)
    } else {
        $source.IndexOf("`n    }`n}", $start, [StringComparison]::Ordinal)
    }
    Assert-True ($next -gt $start) "RunPrologueTests bounds phase method $Name"
    return $source.Substring($start, $next - $start)
}

function Assert-NativeInvalidPhaseProbe([string]$Name, [string]$PhaseArgument) {
    $stamp = Get-Date -Format 'yyyyMMddHHmmss'
    $log = Join-Path $evidenceRoot "task4-prologue-invalid-phase-$Name-$stamp.log"
    $stdout = "$log.stdout"
    $stderr = "$log.stderr"
    $console = "$log.console"
    $arguments = @('--headless', '--path', $project, '--script', 'res://tests/RunPrologueTests.cs',
        '--log-file', $log)
    if (-not [string]::IsNullOrWhiteSpace($PhaseArgument)) {
        $arguments += @('--', $PhaseArgument)
    }
    $process = Start-Process -FilePath $godot -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    $engineText = if (Test-Path -LiteralPath $log -PathType Leaf) {
        Get-Content -LiteralPath $log -Raw
    } else { [string]::Empty }
    $consoleText = @(
        if (Test-Path -LiteralPath $stdout -PathType Leaf) { Get-Content -LiteralPath $stdout -Raw }
        if (Test-Path -LiteralPath $stderr -PathType Leaf) { Get-Content -LiteralPath $stderr -Raw }
    ) -join [Environment]::NewLine
    [System.IO.File]::WriteAllText($console, $consoleText, [System.Text.UTF8Encoding]::new($false))
    $output = "$engineText$([Environment]::NewLine)$consoleText"
    Assert-True ($process.ExitCode -eq 1) "$Name native invalid-phase probe exits one: actual=$($process.ExitCode)"
    Assert-True ($output.Contains('RunPrologueTests requires --phase=pre or --phase=post')) `
        "$Name reports the fail-closed phase-selection error"
    foreach ($markerPrefix in @(
        'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK', 'PROLOGUE_TEST_PHASE_OK', 'PROLOGUE_TESTS_OK')) {
        Assert-True (-not $output.Contains($markerPrefix)) "$Name emits no terminal success marker: $markerPrefix"
    }
    Write-Output "TASK4_PROLOGUE_INVALID_PHASE_${Name}_RED_OBSERVED exit=$($process.ExitCode) log=$log console=$console"
}

function Assert-StaticMutationRed([string]$Name, [string]$Extension, [string]$InputText, [scriptblock]$Mutate) {
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) (
        'knight-task4-phase-coverage-' + [guid]::NewGuid().ToString('N') + $Extension)
    try {
        $mutated = & $Mutate $InputText
        Assert-True ($mutated -cne $InputText) "$Name changes a real focused-contract source"
        [System.IO.File]::WriteAllText($tempPath, $mutated, [System.Text.UTF8Encoding]::new($false))
        $priorErrorAction = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            if ($Extension -eq '.cs') {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
                    -SourcePath $tempPath -VerifierPath $VerifierPath -SkipMutation -SkipNative 2>&1 | Out-Null
            }
            else {
                & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
                    -SourcePath $SourcePath -VerifierPath $tempPath -SkipMutation -SkipNative 2>&1 | Out-Null
            }
            $mutationExit = $LASTEXITCODE
        }
        finally { $ErrorActionPreference = $priorErrorAction }
        Assert-True ($mutationExit -ne 0) "$Name deletion mutation fails the focused contract"
        Write-Output "TASK4_PROLOGUE_${Name}_STATIC_RED_OBSERVED exit=$mutationExit"
    }
    finally {
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction Stop
        }
    }
}

$pre = Get-MethodBody 'RunPrePhase'
$post = Get-MethodBody 'RunPostPhase'
$preOwned = @(
    'ProductionAssetManifestTests.Run();', 'PrologueAudioManifestTests.Run();', 'GameSettingsTests.Run();',
    'PrologueSaveTests.Run();', 'InputGlyphResolverTests.Run();', 'PrologueStringsTests.Run();',
    'TuningContractTests.Run();', 'PlayerPresentationTests.Run();', 'PlayerPoseMixerTests.Run();',
    'SecondaryMotionModelTests.Run();', 'AttackPresentationFrameTests.Run();',
    'CombatReadabilityExperienceTests.Run();', 'GraveLampMothModelTests.Run();',
    'EncounterDefinitionTests.Run();', 'ThreadAbilityModelTests.Run();', 'ThreadCastModelTests.Run();',
    'ThreadExperienceRuntimeTests.Run();', 'ThreadInteractionTests.Run();',
    'ArmoredHitAccumulatorTests.Run();', 'BellBackLaborerModelTests.Run();',
    'FallenBellkeeperModelTests.Run();', 'DamageSourceIdentityRuntimeTests.Run();',
    'PrologueObjectiveTests.Run();', 'PrologueGuidanceModelTests.Run();',
    'await PrologueGuidanceTargetResolverTests.Run(tree);', 'await PlayerTraversalInputTests.Run(tree);',
    'await PrologueTraversalRouteTests.Run(tree);'
)
$postOwned = @(
    'ProloguePlaytestSessionTests.Run();', 'PrologueRouteGraphTests.Run();', 'PrologueProgressTests.Run();',
    'PrologueRoomContractTests.Run();', 'PrologueUiStructureTests.Run();', 'PrologueHudViewModelTests.Run();',
    'PlayerAnimationMapTests.Run();', 'P0RoomStructureTests.Run();', 'P1LoopStructureTests.Run();',
    'BossSceneStructureTests.Run();', 'await GraveLampMothRuntimeTests.Run(tree);',
    'await EncounterControllerTests.Run(tree);', 'await P1LoopRuntimeTests.Run(tree);',
    'await BellChainRuntimeTests.Run(tree);',
    'await PrologueGuidanceRuntimeTests.Run(tree);', 'await PrologueCompletionTests.Run(tree);',
    'await PrologueLevelRuntimeTests.Run(tree);', 'await ProloguePlaytestRecorderTests.Run(tree);',
    'await PrologueUiRuntimeTests.Run(tree);', 'await PrologueFormalHudTests.Run(tree);',
    'await PlayerFormalVisualTests.Run(tree);', 'EnemyFormalVisualTests.Run();',
    'FallenBellkeeperFormalVisualTests.Run();', 'PrologueEnvironmentStructureTests.Run();',
    'await PrologueEnvironmentRuntimeTests.Run(tree);', 'PrologueMechanismVisualTests.Run();',
    'PresentationCueCoverageTests.Run();', 'await PrologueVfxRuntimeTests.Run(tree);',
    'await PrologueAudioDirectorTests.Run(tree);', 'await PrologueGameFlowTests.Run(tree);',
    'await PrologueProductionAssetRuntimeTests.Run(tree);', 'await PrologueAccessibilityTests.Run(tree);'
)

foreach ($phase in @(@{ Name = 'pre'; Body = $pre; Owned = $preOwned }, @{ Name = 'post'; Body = $post; Owned = $postOwned })) {
    $previous = -1
    foreach ($call in $phase.Owned) {
        $sourceCount = ([regex]::Matches($source, [regex]::Escape($call))).Count
        Assert-True ($sourceCount -eq 1) "test call is owned exactly once across phases: $call count=$sourceCount"
        $index = $phase.Body.IndexOf($call, [StringComparison]::Ordinal)
        Assert-True ($index -gt $previous) "test call stays ordered in $($phase.Name): $call"
        $previous = $index
    }
}
foreach ($call in $preOwned) {
    Assert-True ($post.IndexOf($call, [StringComparison]::Ordinal) -lt 0) "pre call is not duplicated in post: $call"
}
foreach ($call in $postOwned) {
    Assert-True ($pre.IndexOf($call, [StringComparison]::Ordinal) -lt 0) "post call is not duplicated in pre: $call"
}
Assert-True ($source.IndexOf('await FallenBellkeeperRuntimeTests.Run(tree);', [StringComparison]::Ordinal) -lt 0) `
    'high-churn Boss runtime suite is not retained in either long-lived phase'
Assert-True ($source.IndexOf('OS.GetCmdlineUserArgs()', [StringComparison]::Ordinal) -ge 0) 'phase selection reads leak-free user arguments'
Assert-True ($source.IndexOf('TestSceneTeardown.DrainManagedWrappersAtProcessExit("pre")', [StringComparison]::Ordinal) -ge 0) 'pre drains only at process exit'
Assert-True ($source.IndexOf('TestSceneTeardown.DrainManagedWrappersAtProcessExit("post")', [StringComparison]::Ordinal) -ge 0) 'post drains only at process exit'
Assert-True ($source.IndexOf('RunPrologueTests requires --phase=pre or --phase=post', [StringComparison]::Ordinal) -ge 0) `
    'phase selection fails closed with the exact pre/post-only contract'
Assert-True ($source.IndexOf('return "all";', [StringComparison]::Ordinal) -lt 0) `
    'phase selection never defaults to unsafe monolithic all mode'
Assert-True ($source.IndexOf('phase != "all"', [StringComparison]::Ordinal) -lt 0) `
    'runner has no compatibility branch for monolithic all mode'
Assert-True ($source.IndexOf('DrainManagedWrappersAtProcessExit("all")', [StringComparison]::Ordinal) -lt 0) `
    'runner has no monolithic all teardown path'

foreach ($requirement in @(
    'Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=$($phaseRun.Phase)"',
    'Assert-ExactLogMarker -Path $phaseRun.Console -Marker "PROLOGUE_TEST_PHASE_OK phase=$($phaseRun.Phase)"',
    '$consoleOverallCount = @((Get-Content -LiteralPath $phaseRun.Console)',
    "Where-Object { `$_ -ceq 'PROLOGUE_TESTS_OK' }).Count",
    'Assert-ExactLogMarker -Path $testsConsole -Marker ''PROLOGUE_TESTS_OK''',
    'Assert-EngineLogContract -Path $testsLog `',
    'Assert-EngineLogContract -Path $testsConsole `')) {
    Assert-True ($verifier.IndexOf($requirement, [StringComparison]::Ordinal) -ge 0) `
        "verify-prologue validates phase and aggregate console output fail-closed: $requirement"
}

if (-not $SkipMutation) {
    Assert-StaticMutationRed -Name 'PHASE_OWNERSHIP_DELETE' -Extension '.cs' -InputText $source -Mutate {
        param([string]$SourceText)
        [regex]::Replace($SourceText, '(?m)^\s*PrologueRouteGraphTests\.Run\(\);\r?\n', [string]::Empty)
    }
    Assert-StaticMutationRed -Name 'CONSOLE_DRAIN_ASSERTION_DELETE' -Extension '.ps1' -InputText $verifier -Mutate {
        param([string]$VerifierText)
        [regex]::Replace($VerifierText,
            '(?m)^\s*Assert-ExactLogMarker -Path \$phaseRun\.Console -Marker "PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=\$\(\$phaseRun\.Phase\)"\r?\n',
            [string]::Empty, 1)
    }
    Assert-StaticMutationRed -Name 'CONSOLE_OVERALL_ASSERTION_DELETE' -Extension '.ps1' -InputText $verifier -Mutate {
        param([string]$VerifierText)
        [regex]::Replace($VerifierText,
            '(?m)^\s*Assert-ExactLogMarker -Path \$testsConsole -Marker ''PROLOGUE_TESTS_OK''\r?\n',
            [string]::Empty, 1)
    }
}

if (-not $SkipNative) {
    Assert-NativeInvalidPhaseProbe -Name 'MISSING' -PhaseArgument ''
    Assert-NativeInvalidPhaseProbe -Name 'ALL' -PhaseArgument '--phase=all'
}

Write-Output 'TASK4_PROLOGUE_PHASE_COVERAGE_OK'
