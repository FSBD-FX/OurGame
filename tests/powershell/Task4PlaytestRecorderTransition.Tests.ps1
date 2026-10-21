param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\integration\ProloguePlaytestRecorderTests.cs'),
    [string]$RecorderPath = (Join-Path $PSScriptRoot '..\..\scripts\prologue\ProloguePlaytestRecorder.cs'),
    [string]$InputReaderPath = (Join-Path $PSScriptRoot '..\..\scripts\player\PlayerInputReader.cs'),
    [string]$ReportPath = (Join-Path $PSScriptRoot '..\..\scripts\prologue\ProloguePlaytestReport.cs'),
    [switch]$SkipMutation
)

$ErrorActionPreference = 'Stop'

function Assert-ContainsLiteral {
    param([string]$Text, [string]$Needle, [string]$Label)
    if ($Text.IndexOf($Needle, [StringComparison]::Ordinal) -lt 0) {
        throw "Task4 recorder transition contract requires $Label."
    }
}

function Invoke-MutationRed {
    param([string]$Text, [string]$Needle, [string]$Replacement, [string]$Label)

    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("knight-task4-recorder-transition-" + [Guid]::NewGuid().ToString('N') + '.cs')
    try {
        $mutant = $Text.Replace($Needle, $Replacement)
        if ($mutant -ceq $Text) { throw "Task4 recorder $Label mutation did not change the source." }
        [System.IO.File]::WriteAllText($tempPath, $mutant, [System.Text.UTF8Encoding]::new($false))
        $hostExe = (Get-Process -Id $PID).Path
        & $hostExe -NoProfile -File $PSCommandPath -SourcePath $tempPath -RecorderPath $recorder -SkipMutation
        if ($LASTEXITCODE -eq 0) {
            throw "Task4 recorder $Label mutation unexpectedly passed."
        }
        "TASK4_RECORDER_TRANSITION_$Label`_RED_OBSERVED exit=$LASTEXITCODE"
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath -Force }
    }
}

function Assert-OrderedLiterals {
    param([string]$Text, [string[]]$Needles, [string]$Label)

    $previous = -1
    foreach ($needle in $Needles) {
        $current = $Text.IndexOf($needle, [StringComparison]::Ordinal)
        if ($current -lt 0 -or $current -le $previous) {
            throw "Task5 fall context contract requires ordered $Label."
        }
        $previous = $current
    }
}

function Invoke-RecorderMutationRed {
    param([string]$Text, [string]$Needle, [string]$Replacement, [string]$Label)

    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("knight-task5-fall-context-" + [Guid]::NewGuid().ToString('N') + '.cs')
    try {
        $mutant = $Text.Replace($Needle, $Replacement)
        if ($mutant -ceq $Text) { throw "Task5 recorder $Label mutation did not change the source." }
        [System.IO.File]::WriteAllText($tempPath, $mutant, [System.Text.UTF8Encoding]::new($false))
        $hostExe = (Get-Process -Id $PID).Path
        & $hostExe -NoProfile -File $PSCommandPath -SourcePath $source -RecorderPath $tempPath -SkipMutation
        if ($LASTEXITCODE -eq 0) {
            throw "Task5 recorder $Label mutation unexpectedly passed."
        }
        "TASK5_FALL_CONTEXT_$Label`_RED_OBSERVED exit=$LASTEXITCODE"
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath -Force }
    }
}

$source = [System.IO.Path]::GetFullPath($SourcePath)
if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Task4 recorder transition source is missing: $source"
}
$text = [System.IO.File]::ReadAllText($source)
$recorder = [System.IO.Path]::GetFullPath($RecorderPath)
if (-not (Test-Path -LiteralPath $recorder -PathType Leaf)) {
    throw "Task5 recorder source is missing: $recorder"
}
$recorderText = [System.IO.File]::ReadAllText($recorder)
$inputReader = [System.IO.Path]::GetFullPath($InputReaderPath)
if (-not (Test-Path -LiteralPath $inputReader -PathType Leaf)) {
    throw "Task5 input reader source is missing: $inputReader"
}
$inputReaderText = [System.IO.File]::ReadAllText($inputReader)
$report = [System.IO.Path]::GetFullPath($ReportPath)
if (-not (Test-Path -LiteralPath $report -PathType Leaf)) {
    throw "Task5 report source is missing: $report"
}
$reportText = [System.IO.File]::ReadAllText($report)

$firstBossWait = @'
await WaitForRoomAndGameplayPhysics(tree, level, PrologueRoomId.FallenBellkeeperArena,
            "first Boss arrival");
'@.Trim()
$recoveryWait = @'
await WaitForRoomAndGameplayPhysics(tree, level, PrologueRoomId.AwakeningCourtyard,
            "pre-fight fall recovery");
'@.Trim()
$secondBossWait = @'
await WaitForRoomAndGameplayPhysics(tree, level, PrologueRoomId.FallenBellkeeperArena,
            "second Boss arrival");
'@.Trim()

$hasLegacyTransitionHelper = $text.IndexOf('private static async Task WaitForRoomAndGameplayPhysics(', [StringComparison]::Ordinal) -ge 0
if ($hasLegacyTransitionHelper) {
    # Preserve the historical transition assertions when validating a pre-seam-migration source.
    Assert-ContainsLiteral -Text $text -Needle 'const int maxPhysicsFrames = 12;' `
        -Label 'the twelve-frame physics bound'
    Assert-ContainsLiteral -Text $text -Needle 'SceneTree.SignalName.PhysicsFrame' `
        -Label 'physics-frame polling'
    Assert-ContainsLiteral -Text $text -Needle 'expected={expected}; current={level.CurrentRoomId}; ' `
        -Label 'expected/current timeout diagnostics'
    Assert-ContainsLiteral -Text $text -Needle 'frames={physicsFrames}/{maxPhysicsFrames}; ' `
        -Label 'frame timeout diagnostics'
    Assert-ContainsLiteral -Text $text -Needle 'processing={GameplayPhysicsState(level)}' `
        -Label 'gameplay-physics timeout diagnostics'
    Assert-ContainsLiteral -Text $text -Needle $firstBossWait -Label 'the first Boss arrival wait'
    Assert-ContainsLiteral -Text $text -Needle $recoveryWait -Label 'the recovery completion wait'
    Assert-ContainsLiteral -Text $text -Needle $secondBossWait -Label 'the second Boss arrival wait'
}
else {
    # 802b3ea replaced room polling with the seamless-world suite. The current transition
    # seam is the explicit task-local fall-contract runner, not a dead room helper.
    Assert-ContainsLiteral -Text $text `
        -Needle 'internal static async Task RunTask5FallContextContracts(SceneTree tree)' `
        -Label 'the Task5 recorder contract runner'
    Assert-ContainsLiteral -Text $text -Needle 'InputSamplerKeepsOnlyStableEdgesAndTracksReleases();' `
        -Label 'the bounded input-edge runtime contract'
    Assert-ContainsLiteral -Text $text -Needle 'InputFrameSequenceDoesNotDuplicateJustPressedEdges();' `
        -Label 'the duplicate physical-input sequence contract'
    Assert-ContainsLiteral -Text $text -Needle 'await PlayerInputSequenceAdvancesOnlyOnPhysicsFrames(tree);' `
        -Label 'the monotonic physical-input sequence contract'
    Assert-ContainsLiteral -Text $text -Needle 'await RecoveryRequestedSamplesCurrentInputBeforeReset(tree);' `
        -Label 'the recovery-before-reset input timing contract'
    Assert-ContainsLiteral -Text $text -Needle 'await GroundedSupportUsesTheColliderScenePath(tree);' `
        -Label 'the real grounded-collider runtime contract'
}

$recoveryStart = $recorderText.IndexOf('private void OnRecoveryRequested(string reason)', [StringComparison]::Ordinal)
$recoveryEnd = $recorderText.IndexOf('private void OnRecoveryCompleted(string reason)', [StringComparison]::Ordinal)
if ($recoveryStart -lt 0 -or $recoveryEnd -le $recoveryStart) {
    throw 'Task5 fall context contract requires a bounded recovery-request callback.'
}
$recoveryBody = $recorderText.Substring($recoveryStart, $recoveryEnd - $recoveryStart)
$observeInput = 'ObserveInputEdges();'
$fallCapture = 'PrologueFallSample fall = reason == "FALL RESET" ? CaptureFallContext(reason) : null;'
$recordDeath = '_session.RecordDeath(duringBossAttempt);'
$recordFall = 'if (fall != null) _session.RecordFall(fall);'
$beginRecovery = '_session.BeginRecovery(reason, duringBossAttempt);'
$persist = 'Persist();'
Assert-ContainsLiteral -Text $recorderText -Needle 'internal static IReadOnlyList<string> SampleInputEdges(' `
    -Label 'the real CurrentFrame edge sampler'
Assert-ContainsLiteral -Text $inputReaderText -Needle 'public ulong CurrentFrameSequence { get; private set; }' `
    -Label 'the monotonic physical-input sequence surface'
Assert-ContainsLiteral -Text $inputReaderText -Needle 'CurrentFrameSequence++;' `
    -Label 'the per-physics input sequence increment'
Assert-ContainsLiteral -Text $recorderText -Needle 'internal static bool TryClaimInputFrameSequence(' `
    -Label 'the recorder input-sequence claim gate'
Assert-ContainsLiteral -Text $recorderText `
    -Needle 'if (!TryClaimInputFrameSequence(_input.CurrentFrameSequence, ref _lastInputFrameSequence)) return;' `
    -Label 'the no-duplicate CurrentFrame recorder gate'
Assert-ContainsLiteral -Text $recorderText -Needle 'if (frame.PrimaryReleased) edges.Add("LMB_UP");' `
    -Label 'the left-button release edge'
Assert-ContainsLiteral -Text $recorderText -Needle 'collision.GetNormal().Y >= -.5f' `
    -Label 'the floor-only slide-normal filter'
Assert-ContainsLiteral -Text $recorderText -Needle 'checkpoint.HasActivatedCheckpoint ? checkpoint.SectorId.ToString() : "NotActivated"' `
    -Label 'the non-invented checkpoint identity'
Assert-ContainsLiteral -Text $recoveryBody -Needle $fallCapture -Label 'the pre-reset fall snapshot'
Assert-OrderedLiterals -Text $recoveryBody `
    -Needles @($observeInput, $fallCapture, $recordDeath, $recordFall, $beginRecovery, $persist) `
    -Label 'current physics input, snapshot, death, fall, recovery, immediate persistence sequence'
Assert-ContainsLiteral -Text $reportText -Needle 'private static string MarkdownCell(string value)' `
    -Label 'the Markdown dynamic-cell escaping helper'
Assert-ContainsLiteral -Text $reportText -Needle '.Replace("\\", "\\\\")' `
    -Label 'the Markdown backslash escaping'
Assert-ContainsLiteral -Text $reportText -Needle '.Replace("|", "\\|")' `
    -Label 'the Markdown pipe escaping'
Assert-ContainsLiteral -Text $reportText -Needle '.Replace("\r\n", "\n")' `
    -Label 'the Markdown CRLF normalization'
Assert-ContainsLiteral -Text $reportText -Needle '.Replace("\n", "<br>")' `
    -Label 'the Markdown newline cell preservation'
Assert-ContainsLiteral -Text $reportText -Needle 'MarkdownCell(fall.Reason)' `
    -Label 'the escaped fall reason cell'
Assert-ContainsLiteral -Text $reportText -Needle 'MarkdownCell(FormatInputs(fall.RecentInputs))' `
    -Label 'the escaped recent-inputs cell'

if (-not $SkipMutation) {
    if ($hasLegacyTransitionHelper) {
        Invoke-MutationRed -Text $text -Needle $firstBossWait -Replacement '' -Label 'DELETE'
        Invoke-MutationRed -Text $text -Needle $firstBossWait `
            -Replacement $firstBossWait.Replace('PrologueRoomId.FallenBellkeeperArena',
                'PrologueRoomId.AwakeningCourtyard') -Label 'EXPECTATION'
    }
    else {
        Invoke-MutationRed -Text $text `
            -Needle 'await GroundedSupportUsesTheColliderScenePath(tree);' -Replacement '' `
            -Label 'TASK5_GROUNDED_CONTRACT'
    }
    Invoke-RecorderMutationRed -Text $recorderText -Needle $recordFall -Replacement 'if (fall != null) { }' `
        -Label 'PRE_RESET_ORDER'
    # Replace only the bounded recovery callback, not the normal runtime observer.  The child
    # parser then proves the new recovery-time claim itself is required.
    Invoke-RecorderMutationRed -Text $recorderText -Needle $recoveryBody `
        -Replacement $recoveryBody.Replace($observeInput, '') -Label 'RECOVERY_INPUT_TIMING'
    Invoke-RecorderMutationRed -Text $recorderText -Needle 'if (frame.PrimaryReleased) edges.Add("LMB_UP");' `
        -Replacement 'if (frame.PrimaryReleased) edges.Add("MOUSE_UP");' -Label 'INPUT_FILTER'
}

'TASK4_RECORDER_TRANSITION_CONTRACT_OK'
'TASK5_FALL_CONTEXT_PESTER_CONTRACT_OK'

# Expected child-process RED runs intentionally exit nonzero.  Their exit state must not
# turn the canonical direct/Pester invocation red after the parent has asserted it.
$global:LASTEXITCODE = 0
