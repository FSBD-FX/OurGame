[CmdletBinding()]
param([string]$ProjectRoot)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
}

function Assert-BaseGate([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "BASE hard-gate source contract failed: $Message" }
}

$verifier = Join-Path $ProjectRoot 'tools\verify-prologue-base-hardgates.ps1'
$audit = Join-Path $ProjectRoot 'tools\audit-prologue-base-hardgates.ps1'
foreach ($path in @($verifier, $audit)) {
    Assert-BaseGate (Test-Path -LiteralPath $path -PathType Leaf) "required BASE gate file exists: $path"
}
$verifierText = [IO.File]::ReadAllText($verifier)
$auditText = [IO.File]::ReadAllText($audit)
$fullVerifierText = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'tools\verify-prologue.ps1'))
$uiCaptureRunner = Join-Path $ProjectRoot 'tests\presentation\PrologueUiResolutionCapture.cs'
Assert-BaseGate (Test-Path -LiteralPath $uiCaptureRunner -PathType Leaf) 'required UI-capture runner exists'
$uiCaptureRunnerText = [IO.File]::ReadAllText($uiCaptureRunner)
$retryHelper = Join-Path $ProjectRoot 'tools\Task6ImportRetry.ps1'
$retryTests = Join-Path $ProjectRoot 'tests\powershell\Task6ImportNativeRetry.Tests.ps1'
$uiCaptureTests = Join-Path $ProjectRoot 'tests\powershell\Task6UiCaptureExternal.Tests.ps1'
foreach ($path in @($retryHelper, $retryTests, $uiCaptureTests)) {
    Assert-BaseGate (Test-Path -LiteralPath $path -PathType Leaf) "required import-retry dependency exists: $path"
}
$retryHelperText = [IO.File]::ReadAllText($retryHelper)
foreach ($required in @(
    'Task6ImportRetry.ps1', 'Task6ImportNativeRetry.Tests.ps1',
    'Invoke-Task6ImportWithBoundedNativeRetry',
    'Passed:\s+6\s+Failed:\s+0')) {
    Assert-BaseGate ($verifierText.Contains($required)) "BASE producer binds import-native retry contract: $required"
}
Assert-BaseGate ($retryHelperText.Contains('PROLOGUE_IMPORT_NATIVE_RETRY_OK')) `
    'the dot-sourced import retry helper owns the terminal marker emitted by BASE'
foreach ($required in @(
    'Task6ImportRetry.ps1', 'Task6ImportNativeRetry.Tests.ps1',
    'PROLOGUE_IMPORT_NATIVE_RETRY_OK')) {
    Assert-BaseGate ($fullVerifierText.Contains($required)) "full verifier binds committed import-native retry dependency: $required"
}
foreach ($required in @(
    'VERIFICATION_EDITOR_GC_DRAIN_OK', 'FALLEN_BELLKEEPER_RUNTIME_TESTS_OK passes=2',
    'TASK6_CLAW_DEATH_CLEANUP_OK physical=True idle=True generated_main=True',
    'TASK6_BASE_CRITICAL_MIGRATION_OK schema=2 five_sector=True legacy_thread_smoke=superseded inputmap=True',
    'PROLOGUE_GENERATED_SCENES_RESTORED',
    'PROLOGUE_PROTECTED_ARTIFACT_HASHES_OK count=$($baseSnapshotPaths.Count)',
    'PROLOGUE_BASE_HARD_GATES_OK', 'Assert-OnlyExpectedEngineError', 'ExpectedContext',
    'events=viewport+inputmap os_warp=False', 'TASK6_INPUT_RETICLE_BINDING_OK',
    'ExternalEvidenceRoot', 'ExternalEvidenceManifestOnly',
    'BASE_EXTERNAL_EVIDENCE_ROOT_REQUIRED', 'BASE_EXTERNAL_EVIDENCE_ROOT_INSIDE_PROJECT',
    'PROLOGUE_BASE_EXTERNAL_EVIDENCE_WRITTEN')) {
    Assert-BaseGate ($verifierText.Contains($required)) "BASE verifier retains $required"
}
Assert-BaseGate (-not $verifierText.Contains('PROLOGUE_THREAD_SMOKE_DEATH_CLEANUP_OK')) `
    'BASE verifier rejects the superseded legacy ThreadAbility cleanup terminal as runtime evidence'
Assert-BaseGate ($verifierText.Contains('events=os-warp\+viewport\+inputmap')) `
    'BASE verifier explicitly rejects legacy OS-warp evidence markers'
$builderBlock = [regex]::Match($verifierText,
    '(?s)\$builders = @\((.*?)\r?\n\)\r?\n\$builderNames').Groups[1].Value
Assert-BaseGate (-not [string]::IsNullOrWhiteSpace($builderBlock)) `
    'BASE exposes one bounded builder plan block for order verification'
$previousBuilderOffset = -1
foreach ($activeBuilder in @(
    'BuildCanyonEntrance', 'BuildCanyonDescent', 'BuildCanyonBasin',
    'BuildCanyonAscent', 'BuildLostTemple', 'BuildPrologueLevel'
)) {
    $entry = "@{ Name = '$activeBuilder'; Script = 'res://tools/build/prologue/$activeBuilder.cs' }"
    $matches = [regex]::Matches($builderBlock, [regex]::Escape($entry))
    Assert-BaseGate ($matches.Count -eq 1) `
        "BASE declares active canyon builder exactly once: $activeBuilder"
    Assert-BaseGate ($matches[0].Index -gt $previousBuilderOffset) `
        "BASE orders active canyon builders leaf-to-main: $activeBuilder"
    $previousBuilderOffset = $matches[0].Index
}
foreach ($required in @(
    '$activeCanyonScenes = @(', 'TASK6_ACTIVE_CANYON_BUILD_OK',
    '$descentStaticBodies -ne 15', '$descentProfiles -ne 15',
    'TASK6_ACTIVE_CANYON_ARTIFACT_HASHES_OK count=$($activeCanyonScenes.Count)'
)) {
    Assert-BaseGate ($verifierText.Contains($required)) "BASE retains active canyon clean-tree gate: $required"
}
$runtimeScenes = @(
    'scenes\prologue\sectors\CanyonEntrance.tscn',
    'scenes\prologue\sectors\CanyonDescent.tscn',
    'scenes\prologue\sectors\CanyonBasin.tscn',
    'scenes\prologue\sectors\CanyonAscent.tscn',
    'scenes\prologue\sectors\LostTemple.tscn',
    'scenes\prologue\PrologueLevel.tscn'
)
$runtimeSceneBlock = [regex]::Match($verifierText,
    '(?s)\$generatedRuntimeScenes = @\((.*?)\r?\n\)').Groups[1].Value
Assert-BaseGate (-not [string]::IsNullOrWhiteSpace($runtimeSceneBlock)) `
    'BASE declares one ordered generated-runtime semantic scene list'
$previousRuntimeOffset = -1
foreach ($runtimeScene in $runtimeScenes) {
    $matches = [regex]::Matches($runtimeSceneBlock, [regex]::Escape("'$runtimeScene'"))
    Assert-BaseGate ($matches.Count -eq 1) `
        "BASE declares generated-runtime scene exactly once: $runtimeScene"
    Assert-BaseGate ($matches[0].Index -gt $previousRuntimeOffset) `
        "BASE orders generated-runtime scenes leaf-to-main: $runtimeScene"
    $previousRuntimeOffset = $matches[0].Index
}
foreach ($required in @(
    'Get-Task6GeneratedRuntimeSemantics -Root $project -RelativePaths $generatedRuntimeScenes',
    'PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=$($generatedRuntimeSemantics.Sha256) count=$($generatedRuntimeSemantics.Count)',
    "Join-Path `$externalEvidenceRoot 'verify-prologue-generated-runtime-semantics.txt'",
    'PROLOGUE_GENERATED_SCENES_RESTORED byte_drift=$builderByteDriftCount',
    'semantic_drift=0')) {
    Assert-BaseGate ($verifierText.Contains($required)) `
        "BASE binds generated-runtime semantics and restores clean bytes: $required"
}
$runtimeFingerprintOffset = $verifierText.IndexOf(
    'Get-Task6GeneratedRuntimeSemantics -Root $project -RelativePaths $generatedRuntimeScenes',
    [StringComparison]::Ordinal)
$testsPreOffset = $verifierText.IndexOf('$testsPreLog = Join-Path $externalEvidenceRoot', [StringComparison]::Ordinal)
Assert-BaseGate ($runtimeFingerprintOffset -ge 0 -and $testsPreOffset -gt $runtimeFingerprintOffset) `
    'BASE fingerprints generated leaf-to-main bytes before runtime pre/post consumers execute'
foreach ($required in @(
    "'semantic-digest-tamper.child.log'", "'topology-path-tamper.child.log'",
    "'Passed:\s+8\s+Failed:\s+0'",
    'TASK6_BASE_EXTERNAL_EVIDENCE_PESTER_OK red_cases=7 green_cases=1 child_gates=True project_root_fallback=False generated_runtime_semantics=True',
    'PROLOGUE_GENERATED_RUNTIME_SEMANTICS_OK sha=([a-f0-9]{64}) count=6',
    "Join-Path `$baseEvidenceRoot 'verify-prologue-generated-runtime-semantics.txt'",
    'TASK6_BASE_GENERATED_RUNTIME_SEMANTICS_BOUND_OK sha=$baseRuntimeSemanticSha count=6'
)) {
    Assert-BaseGate ($fullVerifierText.Contains($required)) `
        "full verifier retains generated-runtime external-evidence child coverage: $required"
}
foreach ($required in @(
    'PROLOGUE_VISUAL_EVIDENCE_OK', 'PROLOGUE_VISUAL_PERFORMANCE_OK',
    'PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=4 driver=windows', 'PROLOGUE_PRODUCTION_AUDIT_OK')) {
    Assert-BaseGate ($auditText.Contains($required)) "BASE audit retains $required"
}
Assert-BaseGate ($auditText.Contains('AuditRoot')) 'BASE audit requires an explicit external audit root'
Assert-BaseGate ($auditText.Contains('outside the candidate worktree')) `
    'BASE audit rejects project-root output fallback'
foreach ($required in @(
    'KNIGHT_UI_CAPTURE_ROOT', 'KNIGHT_UI_CAPTURE_EVIDENCE_ROOT', 'UiResolutionOnly',
    'UiResolutionManifestOnly', 'TASK6_UI_CAPTURE_EXTERNAL_OK',
    'TASK6_UI_CAPTURE_MANIFEST_OK', 'PROLOGUE_UI_CAPTURE_PNG_COUNT_MISMATCH',
    'resolution_count=4')) {
    Assert-BaseGate ($auditText.Contains($required)) "BASE audit retains external UI-capture contract: $required"
}
foreach ($required in @(
    'PROLOGUE_UI_CAPTURE_ROOT_REQUIRED', 'PROLOGUE_UI_CAPTURE_ROOT_INSIDE_PROJECT',
    'PROLOGUE_UI_CAPTURE_ROOT_OUTSIDE_EVIDENCE', 'ResolveExternalCaptureRoot')) {
    Assert-BaseGate ($uiCaptureRunnerText.Contains($required)) "UI runner retains external capture contract: $required"
}
Assert-BaseGate (-not $uiCaptureRunnerText.Contains('res://screenshots/task10-final-hud')) `
    'UI runner has no project-root screenshot fallback'
$generated = @(
    'scenes\presentation\PlayerFormalVisual.tscn', 'scenes\player\Player.tscn',
    'scenes\presentation\RustShellFormalVisual.tscn', 'scenes\presentation\GraveLampMothFormalVisual.tscn',
    'scenes\presentation\BellBackLaborerFormalVisual.tscn', 'scenes\presentation\FallenBellkeeperFormalVisual.tscn',
    'scenes\presentation\PrologueEnvironmentKit.tscn', 'scenes\presentation\GraveLampVisual.tscn',
    'scenes\presentation\ThreadAnchorVisual.tscn', 'scenes\presentation\BellChainVisual.tscn',
    'scenes\presentation\ShortcutDoorVisual.tscn', 'scenes\vfx\PrologueVfx.tscn',
    'scenes\enemies\RustShellSquire.tscn', 'scenes\enemies\GraveLampMoth.tscn',
    'scenes\enemies\BellBackLaborer.tscn', 'scenes\boss\FallenBellkeeper.tscn',
    'scenes\mechanisms\BellChainMechanism.tscn', 'scenes\prologue\rooms\AwakeningCourtyard.tscn',
    'scenes\prologue\rooms\BrokenToothCorridor.tscn', 'scenes\prologue\rooms\UpperPendulum.tscn',
    'scenes\prologue\rooms\LowerBuriedMachinery.tscn', 'scenes\prologue\rooms\BellChainMechanism.tscn',
    'scenes\prologue\rooms\FallenBellkeeperArena.tscn', 'scenes\ui\PrologueUi.tscn',
    'scenes\prologue\PrologueLevel.tscn', 'project.godot'
)
Assert-BaseGate ($generated.Count -eq 26) 'BASE protects 25 generated scenes plus project.godot'
foreach ($relative in $generated) {
    Assert-BaseGate (Test-Path -LiteralPath (Join-Path $ProjectRoot $relative) -PathType Leaf) `
        "protected BASE artifact exists: $relative"
}
'BASE_HARD_GATES_SOURCE_OK generated=25 project=1 terminal=preserved'
