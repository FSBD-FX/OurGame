$ErrorActionPreference = 'Stop'
$helper = Join-Path $PSScriptRoot '..\..\tools\EvidenceChecks.ps1'
. $helper

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Assert-BossProofUsesPlayerCombatOnly([string]$Text, [string]$Label) {
    $applyDamageCalls = [regex]::Matches($Text, '(?m)^(?<line>[^\r\n]*\.\s*ApplyDamage\s*\()')
    foreach ($call in $applyDamageCalls) {
        Assert-True ([regex]::IsMatch(
            $call.Groups['line'].Value,
            '_player\s*\.\s*GetNode<HealthComponent>\("Health"\)\s*\.\s*ApplyDamage\s*\(')) `
            "$Label cannot call ApplyDamage on non-player health"
    }
    Assert-True (-not [regex]::IsMatch(
        $Text,
        'GetNode<Hurtbox>\("Hurtbox"\)\s*\.\s*Receive\s*\(')) `
        "$Label cannot call the Boss Hurtbox receiver directly"

    $hurtboxAliases = [regex]::Matches(
        $Text,
        '(?<name>[_A-Za-z]\w*)\s*=\s*[^;\r\n]*GetNode<Hurtbox>\("Hurtbox"\)')
    foreach ($match in $hurtboxAliases) {
        $name = [regex]::Escape($match.Groups['name'].Value)
        Assert-True (-not [regex]::IsMatch(
            $Text,
            "\b$name\s*\.\s*(?:Receive|Call|CallDeferred|Set|SetDeferred|SetIndexed)\s*\(")) `
            "$Label cannot mutate the Boss Hurtbox through alias '$($match.Groups['name'].Value)'"
    }

    $bossAliases = [regex]::Matches(
        $Text,
        '(?<name>[_A-Za-z]\w*)\s*=\s*[^;\r\n]*(?:CurrentBoss|GetNode<FallenBellkeeper>)')
    foreach ($match in $bossAliases) {
        $name = [regex]::Escape($match.Groups['name'].Value)
        Assert-True (-not [regex]::IsMatch(
            $Text,
            "\b$name\s*\.\s*(?:Set|SetDeferred|SetIndexed|Call|CallDeferred)\s*\(")) `
            "$Label cannot mutate Boss '$($match.Groups['name'].Value)' through a dynamic setter"
        Assert-True (-not [regex]::IsMatch(
            $Text,
            "\b$name\s*\.\s*(?:[_A-Za-z]\w*\s*\.\s*)*[_A-Za-z]\w*\s*=(?!=)")) `
            "$Label cannot assign Boss state or properties through '$($match.Groups['name'].Value)'"
    }

    $healthAliases = [regex]::Matches(
        $Text,
        '(?<name>[_A-Za-z]\w*)\s*=\s*(?<owner>[^;\r\n]+?)\.GetNode<HealthComponent>\("Health"\)')
    foreach ($match in $healthAliases) {
        if ([regex]::IsMatch($match.Groups['owner'].Value, '\b_player\b')) { continue }
        $name = [regex]::Escape($match.Groups['name'].Value)
        Assert-True (-not [regex]::IsMatch($Text, "\b$name\s*\.\s*[_A-Za-z]\w*\s*\(")) `
            "$Label cannot call a mutator on non-player health alias '$($match.Groups['name'].Value)'"
        Assert-True (-not [regex]::IsMatch(
            $Text,
            "\b$name\s*\.\s*(?:[_A-Za-z]\w*\s*\.\s*)*[_A-Za-z]\w*\s*=(?!=)")) `
            "$Label cannot assign non-player health through '$($match.Groups['name'].Value)'"
    }

    $combatAliases = [regex]::Matches(
        $Text,
        '(?<name>[_A-Za-z]\w*)\s*=\s*_player\s*\.\s*GetNode<PlayerCombat>\("Combat"\)')
    Assert-True ($combatAliases.Count -gt 0) `
        "$Label must resolve the real player's PlayerCombat"
    $connected = $false
    foreach ($match in $combatAliases) {
        $name = [regex]::Escape($match.Groups['name'].Value)
        if ([regex]::IsMatch($Text, "\b$name\s*\.\s*AttackConnected\s*\+=")) {
            $connected = $true
            break
        }
    }
    Assert-True $connected "$Label must subscribe the resolved PlayerCombat to AttackConnected"
    Assert-True ([regex]::IsMatch($Text, 'Press\s*\(\s*InputActions\.Attack\s*\)')) `
        "$Label must issue real attack input"
}

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("knight-prologue-evidence-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
try {
    $badBossProof = @'
HealthComponent bossHealth = boss.GetNode<HealthComponent>("Health");
bossHealth.ApplyDamage(new DamageRequest(5, Vector2.Right, 0f, 1UL));
'@
    $bypassRejected = $false
    try { Assert-BossProofUsesPlayerCombatOnly -Text $badBossProof -Label 'bad fixture' }
    catch { $bypassRejected = $true }
    Assert-True $bypassRejected 'direct Boss health setup is rejected by the proof-source gate'

    $badBossFieldProof = @'
_trackedBossHealth = boss.GetNode<HealthComponent>("Health");
_trackedBossHealth.ApplyDamage(new DamageRequest(5, Vector2.Right, 0f, 1UL));
PlayerCombat playerCombat = _player.GetNode<PlayerCombat>("Combat");
playerCombat.AttackConnected += OnAttackConnected;
Press(InputActions.Attack);
'@
    $fieldBypassRejected = $false
    try { Assert-BossProofUsesPlayerCombatOnly -Text $badBossFieldProof -Label 'bad field fixture' }
    catch { $fieldBypassRejected = $true }
    Assert-True $fieldBypassRejected 'field-based direct Boss health setup is rejected by the proof-source gate'

    $badBossHurtboxProofs = @(
        @{ Label = 'local alias'; Source = @'
Hurtbox bossTarget = boss.GetNode<Hurtbox>("Hurtbox");
PlayerCombat playerCombat = _player.GetNode<PlayerCombat>("Combat");
playerCombat.AttackConnected += OnAttackConnected;
Press(InputActions.Attack);
bossTarget.Receive(new DamageRequest(5, Vector2.Right, 0f, 1UL));
'@ },
        @{ Label = 'field alias'; Source = @'
_trackedBossHurtbox = boss.GetNode<Hurtbox>("Hurtbox");
_playerCombat = _player.GetNode<PlayerCombat>("Combat");
_playerCombat.AttackConnected += OnAttackConnected;
Press(InputActions.Attack);
_trackedBossHurtbox.Call("Receive", new DamageRequest(5, Vector2.Right, 0f, 1UL));
'@ }
    )
    foreach ($proof in $badBossHurtboxProofs) {
        $hurtboxAliasRejected = $false
        try { Assert-BossProofUsesPlayerCombatOnly -Text $proof.Source -Label $proof.Label }
        catch { $hurtboxAliasRejected = $true }
        Assert-True $hurtboxAliasRejected `
            "$($proof.Label) direct Boss Hurtbox mutation is rejected by the proof-source gate"
    }

    $realCombatProofPrefix = @'
FallenBellkeeper boss = arena.CurrentBoss;
Hurtbox bossHurtbox = boss.GetNode<Hurtbox>("Hurtbox");
PlayerCombat playerCombat = _player.GetNode<PlayerCombat>("Combat");
playerCombat.AttackConnected += hurtbox => { if (hurtbox == bossHurtbox) hits++; };
Press(InputActions.Attack);
'@
    $badBossStateProofs = @(
        @{ Label = 'Boss Set'; Mutation = 'boss.Set("CurrentState", FallenBellkeeperState.Defeated);' },
        @{ Label = 'Boss SetIndexed'; Mutation = 'boss.SetIndexed("Health:CurrentHealth", 1);' },
        @{ Label = 'Boss health property'; Mutation = @'
HealthComponent bossHealth = boss.GetNode<HealthComponent>("Health");
bossHealth.CurrentHealth = 1;
'@ },
        @{ Label = 'Boss health field Set'; Mutation = @'
_trackedBossHealth = boss.GetNode<HealthComponent>("Health");
_trackedBossHealth.Set("CurrentHealth", 1);
'@ }
    )
    foreach ($proof in $badBossStateProofs) {
        $stateMutationRejected = $false
        try {
            Assert-BossProofUsesPlayerCombatOnly `
                -Text ($realCombatProofPrefix + [Environment]::NewLine + $proof.Mutation) `
                -Label $proof.Label
        }
        catch { $stateMutationRejected = $true }
        Assert-True $stateMutationRejected `
            "$($proof.Label) mutation is rejected by the proof-source gate"
    }

    $project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $productionAuditSource = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\audit-prologue-production.ps1'))
    Assert-True (-not [regex]::IsMatch($productionAuditSource, "'--verbose'")) `
        'display-backed performance audit excludes verbose resource I/O from measured frames'
    Assert-True ([regex]::IsMatch(
        $productionAuditSource,
        "'--fixed-fps',\s*'60'[\s\S]*'--display-driver',\s*'windows'")) `
        'display-backed performance audit retains fixed 60 FPS Windows rendering'
    $baseProductionAuditSource = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\audit-prologue-base-hardgates.ps1'))
    foreach ($requiredAuditSource in @(
        "-Marker 'PROLOGUE_VISUAL_EVIDENCE_OK'",
        "-Marker 'PROLOGUE_VISUAL_PERFORMANCE_OK'",
        "-Script 'res://tests/presentation/PrologueVisualAuditTask6.cs'",
        "PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=4 driver=windows",
        'Assert-ExactLogMarker -Path $consolePath -Marker $Marker',
        'Assert-NoEngineLogErrors -Paths @($log, $consolePath)')) {
        Assert-True ($baseProductionAuditSource.Contains($requiredAuditSource)) `
            "BASE production audit preserves generated-sector evidence/performance and merged-console contract '$requiredAuditSource'"
    }
    Assert-True ($productionAuditSource.Contains('& $baseAudit -AuditRoot $BaseAuditRoot -SkipBuild:$SkipBuild')) `
        'production audit delegates the complete BASE display-backed audit to its explicit external root'
    Assert-True (-not $baseProductionAuditSource.Contains("'--performance-only'") -and
        -not $baseProductionAuditSource.Contains("'res://tests/presentation/PrologueVisualAudit.cs'")) `
        'BASE audit rejects the superseded legacy performance-only-only six-room recorder path'
    $visualAuditSource = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tests\presentation\PrologueVisualAudit.cs'))
    Assert-True ($visualAuditSource.Contains(
        'if (_performanceSampleCount != ExpectedPerformanceSamples)')) `
        'performance audit requires its exact deterministic sample count'
    foreach ($certifiedContract in @(
        'CertifiedPerformanceSamples = 691',
        'CertifiedPerformanceRooms = 6',
        'CertifiedPerformanceRealProofs = 23',
        'CertifiedPerformanceTransitions = 7',
        'CertifiedPerformanceFrameCeilingMilliseconds = 50d',
        'ExpectedPerformanceSamples != CertifiedPerformanceSamples',
        'Route.Length != CertifiedPerformanceRooms',
        'RequiredTask4ProofNames.Length != CertifiedPerformanceRealProofs',
        '_capturedRuntimeProofs.Count != CertifiedPerformanceRealProofs',
        '_performanceRoomTransitions != CertifiedPerformanceTransitions',
        '_maxFrameMilliseconds > CertifiedPerformanceFrameCeilingMilliseconds')) {
        Assert-True ($visualAuditSource.Contains($certifiedContract)) `
            "performance audit fail-closes certified contract '$certifiedContract'"
    }
    Assert-True ($visualAuditSource.Contains(
        'if (!_performanceOnly || _frame < PerformanceSamplingStartFrame) return;')) `
        'performance-only process samples before and throughout real proof playback'
    Assert-True ($visualAuditSource.Contains('Time.GetTicksUsec()')) `
        'presentation frame budget uses a monotonic callback-to-callback wall clock'
    Assert-True ($visualAuditSource.Contains(
        'presentation_budget_basis=process_callback_wall_clock')) `
        'performance evidence labels its callback wall-clock measurement basis'
    Assert-True (-not $visualAuditSource.Contains(
        'Performance.GetMonitor(Performance.Monitor.TimeProcess)')) `
        'delayed aggregate TimeProcess monitor cannot drive the per-frame presentation budget'
    Assert-True ($visualAuditSource.Contains('OnPerformanceNodeAdded') -and
        $visualAuditSource.Contains('OnPerformanceNodeRemoved')) `
        'performance audit maintains an event-driven active-particle candidate index'
    Assert-True (-not $visualAuditSource.Contains('CountParticles(_level)')) `
        'per-frame budget measurement cannot include an audit-only full-tree particle traversal'
    Assert-True ($visualAuditSource.Contains('PrepareRuntimeProofActors();')) `
        'real proof actors are prewarmed before the measured presentation window'
    Assert-True ($visualAuditSource.Contains('private void PrintEvidence(string message)') -and
        $visualAuditSource.Contains('if (!_performanceOnly) GD.Print(message);')) `
        'performance-only timing suppresses evidence-detail log I/O while retaining internal proofs'
    Assert-True (-not [regex]::IsMatch($visualAuditSource,
        'GD\.Print\(\$?"TASK4_REAL_')) `
        'real-publisher detail markers flow through the evidence-only logger'
    Assert-True (-not $visualAuditSource.Contains('_frame % 15')) `
        'active particle upper bound is sampled on every certified performance frame'
    foreach ($relativePath in @(
        'tests\presentation\BossRetryPresentation.cs',
        'tests\smoke\PrologueCriticalPathSmoke.cs')) {
        $source = [System.IO.File]::ReadAllText((Join-Path $project $relativePath))
        Assert-BossProofUsesPlayerCombatOnly -Text $source -Label $relativePath
    }

    $requiredBuilders = @(
        'BuildPlayerFormalVisual',
        'BuildPlayer',
        'BuildEnemyFormalVisuals',
        'BuildRustShellSquire',
        'BuildGraveLampMoth',
        'BuildBellBackLaborer',
        'BuildFallenBellkeeperFormalVisual',
        'BuildFallenBellkeeper',
        'BuildPrologueMechanismVisuals',
        'BuildBellChainMechanism',
        'BuildPrologueEnvironmentKit',
        'BuildAwakeningCourtyard',
        'BuildBrokenToothCorridor',
        'BuildUpperPendulum',
        'BuildLowerBuriedMachinery',
        'BuildBellChainRoom',
        'BuildFallenBellkeeperArena',
        'BuildPrologueUi',
        'BuildPrologueVfx',
        'BuildCanyonEntrance',
        'BuildCanyonDescent',
        'BuildCanyonBasin',
        'BuildCanyonAscent',
        'BuildLostTemple',
        'BuildPrologueLevel'
    )
    Assert-PrologueBuilderPlan -BuilderNames $requiredBuilders

    $swapped = @($requiredBuilders)
    $swapped[4] = 'BuildBellBackLaborer'
    $swapped[5] = 'BuildGraveLampMoth'
    $wrongOrderRejected = $false
    try { Assert-PrologueBuilderPlan -BuilderNames $swapped } catch { $wrongOrderRejected = $true }
    Assert-True $wrongOrderRejected 'swapping Moth and Laborer builders is rejected'

    $builderLogs = foreach ($builder in $requiredBuilders) {
        $path = Join-Path $fixtureRoot ("builder-{0}.log" -f $builder)
        Set-Content -LiteralPath $path -Value "$builder OK"
        $path
    }
    Assert-RequiredFiles -Paths $builderLogs -Label 'builder log'
    Remove-Item -LiteralPath $builderLogs[4] -Force
    $missingLogRejected = $false
    try { Assert-RequiredFiles -Paths $builderLogs -Label 'builder log' } catch { $missingLogRejected = $true }
    Assert-True $missingLogRejected 'a missing builder log is rejected'

    $testsLog = Join-Path $fixtureRoot 'tests.log'
    $smokeLog = Join-Path $fixtureRoot 'smoke.log'
    Set-Content -LiteralPath $testsLog -Value 'PROLOGUE_TESTS_OK'
    Set-Content -LiteralPath $smokeLog -Value 'PROLOGUE_CRITICAL_PATH_OK'
    Assert-ExactLogMarker -Path $testsLog -Marker 'PROLOGUE_TESTS_OK'
    Assert-ExactLogMarker -Path $smokeLog -Marker 'PROLOGUE_CRITICAL_PATH_OK'
    Add-Content -LiteralPath $smokeLog -Value 'PROLOGUE_CRITICAL_PATH_OK'
    $duplicateRejected = $false
    try { Assert-ExactLogMarker -Path $smokeLog -Marker 'PROLOGUE_CRITICAL_PATH_OK' } catch { $duplicateRejected = $true }
    Assert-True $duplicateRejected 'duplicate smoke marker is rejected'

    Set-Content -LiteralPath $smokeLog -Value 'ERROR: fixture engine failure'
    $engineErrorRejected = $false
    try { Assert-NoEngineLogErrors -Paths @($smokeLog) } catch { $engineErrorRejected = $true }
    Assert-True $engineErrorRejected 'engine errors are rejected even when the process exits zero'

    $warningGate = Get-Command Assert-EngineLogContract -ErrorAction SilentlyContinue
    Assert-True ($null -ne $warningGate) `
        'engine evidence exposes an exact first-line warning contract'
    $intentionalWarnings = @(
        'WARNING: PresentationProfile: VisualScene root must implement IEntityVisual2D for VisualRoot.',
        "WARNING: PresentationProfile: cue 'Hurt' root must be Node2D.",
        "WARNING: RoomPresentationProfile: VisualScene root must be Node2D for 'VisualRoot'.",
        "WARNING: Guidance target 'MechanismBossExit' is missing in room 'BellChainMechanism'.",
        'WARNING: Rejected prologue flow transition Completed->Recovering.'
    )
    foreach ($warningPath in @(
        (Join-Path $fixtureRoot 'warning-contract.log'),
        (Join-Path $fixtureRoot 'warning-contract.console.log'))) {
        Set-Content -LiteralPath $warningPath -Value $intentionalWarnings
        Assert-EngineLogContract -Path $warningPath -ExpectedWarnings $intentionalWarnings

        Add-Content -LiteralPath $warningPath -Value 'WARNING: unexpected fixture warning.'
        $unexpectedWarningRejected = $false
        try {
            Assert-EngineLogContract -Path $warningPath -ExpectedWarnings $intentionalWarnings
        }
        catch { $unexpectedWarningRejected = $_.Exception.Message -match 'unexpected fixture warning' }
        Assert-True $unexpectedWarningRejected `
            "unexpected warning is rejected independently in $([System.IO.Path]::GetFileName($warningPath))"

        Set-Content -LiteralPath $warningPath -Value ($intentionalWarnings + $intentionalWarnings[0])
        $duplicateExpectedWarningRejected = $false
        try {
            Assert-EngineLogContract -Path $warningPath -ExpectedWarnings $intentionalWarnings
        }
        catch { $duplicateExpectedWarningRejected = $_.Exception.Message -match 'found 2' }
        Assert-True $duplicateExpectedWarningRejected `
            "duplicate expected warning is rejected independently in $([System.IO.Path]::GetFileName($warningPath))"

        $changedWarnings = @($intentionalWarnings)
        $changedWarnings[1] = "WARNING: PresentationProfile: cue 'Hurt' root should be Node2D."
        Set-Content -LiteralPath $warningPath -Value $changedWarnings
        $changedExpectedWarningRejected = $false
        try {
            Assert-EngineLogContract -Path $warningPath -ExpectedWarnings $intentionalWarnings
        }
        catch { $changedExpectedWarningRejected = $_.Exception.Message -match 'should be Node2D' }
        Assert-True $changedExpectedWarningRejected `
            "changed expected warning is rejected independently in $([System.IO.Path]::GetFileName($warningPath))"
    }

    $failClosedFixtures = @(
        @{
            Name = 'ordinary Godot log fatal signature'
            Path = (Join-Path $fixtureRoot 'ordinary-fatal.log')
            Forced = $false
            Lines = @('FATAL: editor crash handler was invoked')
        },
        @{
            Name = 'ordinary merged console native exception'
            Path = (Join-Path $fixtureRoot 'ordinary-native.console.log')
            Forced = $false
            Lines = @('Unhandled native exception in GodotSharp')
        },
        @{
            Name = 'forced Godot log RID leak'
            Path = (Join-Path $fixtureRoot 'forced-rid.log')
            Forced = $true
            Lines = @(
                'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE',
                'WARNING: 3 RID allocations of type CanvasItem were leaked at exit')
        },
        @{
            Name = 'forced merged console Windows access violation'
            Path = (Join-Path $fixtureRoot 'forced-access-violation.console.log')
            Forced = $true
            Lines = @(
                'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE',
                'Unhandled exception at 0xC0000005: Access violation reading location 0x00000000')
        }
    )
    foreach ($fixture in $failClosedFixtures) {
        Set-Content -LiteralPath $fixture.Path -Value $fixture.Lines
        $fatalRejected = $false
        try {
            if ($fixture.Forced) {
                Assert-OnlyExpectedEngineError -Path $fixture.Path `
                    -ExpectedPattern 'FORCED_SMOKE_FAILURE'
            }
            else { Assert-NoEngineLogErrors -Paths @($fixture.Path) }
        }
        catch { $fatalRejected = $true }
        Assert-True $fatalRejected "$($fixture.Name) is rejected by the fail-closed evidence gate"
    }

    $sameLineForcedFixtures = @(
        @{
            Name = 'forced Godot log same-line fatal access violation'
            Path = (Join-Path $fixtureRoot 'forced-same-line-fatal.log')
            Line = 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE Fatal error. 0xC0000005'
        },
        @{
            Name = 'forced Godot log same-line RID leak'
            Path = (Join-Path $fixtureRoot 'forced-same-line-rid.log')
            Line = 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE WARNING: 3 RID allocations were leaked'
        },
        @{
            Name = 'forced merged console same-line fatal access violation'
            Path = (Join-Path $fixtureRoot 'forced-same-line-fatal.console.log')
            Line = 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE Fatal error. 0xC0000005'
        },
        @{
            Name = 'forced merged console same-line RID leak'
            Path = (Join-Path $fixtureRoot 'forced-same-line-rid.console.log')
            Line = 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE WARNING: 3 RID allocations were leaked'
        }
    )
    foreach ($fixture in $sameLineForcedFixtures) {
        Set-Content -LiteralPath $fixture.Path -Value $fixture.Line
        $sameLineFailureRejected = $false
        try {
            Assert-OnlyExpectedEngineError -Path $fixture.Path `
                -ExpectedPattern 'FORCED_SMOKE_FAILURE'
        }
        catch { $sameLineFailureRejected = $true }
        Assert-True $sameLineFailureRejected `
            "$($fixture.Name) cannot hide behind the allowed forced-failure token"
    }

    foreach ($leakLine in @(
        'WARNING: 8 ObjectDB instances were leaked at exit',
        '4 resources still in use at exit')) {
        Set-Content -LiteralPath $smokeLog -Value $leakLine
        $ordinaryLeakRejected = $false
        try { Assert-NoEngineLogErrors -Paths @($smokeLog) } catch { $ordinaryLeakRejected = $true }
        Assert-True $ordinaryLeakRejected `
            "ordinary merged Godot output rejects cleanup leak '$leakLine'"
    }

    $mergedCapture = Get-Command Invoke-MergedNativeCommand -ErrorAction SilentlyContinue
    Assert-True ($null -ne $mergedCapture) `
        'verification exposes a PowerShell-5-safe merged native command capture'
    $nativeConsole = Join-Path $fixtureRoot 'native-merged.console.log'
    $nativeFixture = Join-Path $fixtureRoot 'native-merged-fixture.ps1'
    $currentHostPath = (Get-Process -Id $PID).Path
    Assert-True (-not [string]::IsNullOrWhiteSpace($currentHostPath) -and
        (Test-Path -LiteralPath $currentHostPath -PathType Leaf)) `
        'native capture fixture runs only through the actual current PowerShell host executable'
    $captureHostProbe = Join-Path $project 'tools\capture-prologue.ps1'
    $captureHostProbeConsole = Join-Path $fixtureRoot 'capture-host-probe.console.log'
    $captureHostProbeResult = Invoke-MergedNativeCommand `
        -FilePath $currentHostPath `
        -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $captureHostProbe, '-HostProbeOnly') `
        -ConsolePath $captureHostProbeConsole
    Assert-True ($captureHostProbeResult.ExitCode -eq 0 -and
        (Get-Content -LiteralPath $captureHostProbeConsole -Raw) -match
            'TASK6_CAPTURE_HOST_OK ps_major=\d+ child_host=(pwsh\.exe|powershell\.exe)') `
        'native capture fixture verifies the real Task 6 child-host resolver before exercising merged output'
    Set-Content -LiteralPath $nativeFixture -Value @'
Write-Output 'MERGED_STDOUT'
[Console]::Error.WriteLine('MERGED_STDERR')
exit 7
'@
    $priorPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Stop'
    $nativeResult = Invoke-MergedNativeCommand `
        -FilePath $currentHostPath `
        -ArgumentList @('-NoProfile', '-File', $nativeFixture) `
        -ConsolePath $nativeConsole
    Assert-True ($nativeResult.ExitCode -eq 7) `
        'merged native capture preserves the child process exit code'
    $nativeText = Get-Content -LiteralPath $nativeConsole -Raw
    Assert-True ($nativeText.Contains('MERGED_STDOUT') -and $nativeText.Contains('MERGED_STDERR')) `
        'merged native capture persists both stdout and stderr'
    Assert-True ($ErrorActionPreference -ceq 'Stop') `
        'merged native capture restores the caller ErrorActionPreference'
    $ErrorActionPreference = $priorPreference

    $visibleTailGate = Get-Command Assert-VisiblePngTail -ErrorAction SilentlyContinue
    Assert-True ($null -ne $visibleTailGate) `
        'capture verification exposes a visible final PNG-tail contract'
    Add-Type -AssemblyName System.Drawing
    $referencePath = Join-Path $fixtureRoot 'structured-reference.png'
    $referenceBitmap = [System.Drawing.Bitmap]::new(64, 36)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($referenceBitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(9, 15, 20))
            $graphics.FillRectangle([System.Drawing.Brushes]::DarkSlateGray, 0, 0, 64, 5)
            $graphics.FillRectangle([System.Drawing.Brushes]::Goldenrod, 7, 8, 50, 3)
            $graphics.FillRectangle([System.Drawing.Brushes]::Gainsboro, 13, 15, 38, 3)
            $graphics.FillRectangle([System.Drawing.Brushes]::DimGray, 13, 21, 28, 3)
            $graphics.FillEllipse([System.Drawing.Brushes]::Turquoise, 51, 27, 5, 5)
        }
        finally { $graphics.Dispose() }
        $referenceBitmap.Save($referencePath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $referenceBitmap.Dispose() }

    $uniformTail = @()
    $visibleTail = @()
    $noiseTail = @()
    $wrongSceneTail = @()
    $random = [System.Random]::new(20260821)
    for ($index = 0; $index -lt 3; $index++) {
        $uniformPath = Join-Path $fixtureRoot ("uniform-tail-{0}.png" -f $index)
        $uniformBitmap = [System.Drawing.Bitmap]::new(64, 36)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($uniformBitmap)
            try { $graphics.Clear([System.Drawing.Color]::FromArgb(76, 76, 76)) }
            finally { $graphics.Dispose() }
            $uniformBitmap.Save($uniformPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $uniformBitmap.Dispose() }
        $uniformTail += $uniformPath

        $visiblePath = Join-Path $fixtureRoot ("visible-tail-{0}.png" -f $index)
        $visibleBitmap = [System.Drawing.Bitmap]::new(64, 36)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($visibleBitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::FromArgb(9, 15, 20))
                $graphics.FillRectangle([System.Drawing.Brushes]::DarkSlateGray, 0, 0, 64, 5)
                $graphics.FillRectangle([System.Drawing.Brushes]::Goldenrod, 7, 8, 50, 3)
                $graphics.FillRectangle([System.Drawing.Brushes]::Gainsboro, 13, 15, 38, 3)
                $graphics.FillRectangle([System.Drawing.Brushes]::DimGray, 13, 21, 28, 3)
                $graphics.FillEllipse([System.Drawing.Brushes]::Turquoise, 51 + $index, 27, 5, 5)
            }
            finally { $graphics.Dispose() }
            $visibleBitmap.Save($visiblePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $visibleBitmap.Dispose() }
        $visibleTail += $visiblePath

        $noisePath = Join-Path $fixtureRoot ("noise-tail-{0}.png" -f $index)
        $noiseBitmap = [System.Drawing.Bitmap]::new(64, 36)
        try {
            for ($y = 0; $y -lt $noiseBitmap.Height; $y++) {
                for ($x = 0; $x -lt $noiseBitmap.Width; $x++) {
                    $noiseBitmap.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(
                        $random.Next(256), $random.Next(256), $random.Next(256)))
                }
            }
            $noiseBitmap.Save($noisePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $noiseBitmap.Dispose() }
        $noiseTail += $noisePath

        $wrongScenePath = Join-Path $fixtureRoot ("wrong-scene-tail-{0}.png" -f $index)
        $wrongSceneBitmap = [System.Drawing.Bitmap]::new(64, 36)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($wrongSceneBitmap)
            try {
                $graphics.Clear([System.Drawing.Color]::FromArgb(245, 240, 220))
                $graphics.FillRectangle([System.Drawing.Brushes]::Black, 0, 0, 32, 18)
                $graphics.FillRectangle([System.Drawing.Brushes]::DarkRed, 32, 18, 32, 18)
            }
            finally { $graphics.Dispose() }
            $wrongSceneBitmap.Save($wrongScenePath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $wrongSceneBitmap.Dispose() }
        $wrongSceneTail += $wrongScenePath
    }

    $referenceMutationFailures = [System.Collections.Generic.List[string]]::new()
    foreach ($mutation in @(
        @{ Name = 'ROUND5_HIGH_ENTROPY_REFERENCE_MUTATION'; Paths = $noiseTail },
        @{ Name = 'ROUND5_WRONG_SCENE_REFERENCE_MUTATION'; Paths = $wrongSceneTail })) {
        $namedSimilarityRejection = $false
        try {
            Assert-VisiblePngTail -Paths $mutation.Paths -ReferencePath $referencePath `
                -Label $mutation.Name
        }
        catch {
            $namedSimilarityRejection = $_.Exception.Message -match
                'same-scene reference mismatch.*mean_rgb_mae=.*changed_fraction='
        }
        if (-not $namedSimilarityRejection) { $referenceMutationFailures.Add($mutation.Name) }
    }
    Assert-True ($referenceMutationFailures.Count -eq 0) `
        ("reference similarity mutations were not rejected: " +
            ($referenceMutationFailures -join ', '))

    $uniformTailRejected = $false
    try {
        Assert-VisiblePngTail -Paths $uniformTail -ReferencePath $referencePath `
            -Label 'uniform fixture'
    }
    catch { $uniformTailRejected = $true }
    Assert-True $uniformTailRejected 'uniform gray final capture frames are rejected'
    Assert-VisiblePngTail -Paths $visibleTail -ReferencePath $referencePath `
        -Label 'structured slight-change fixture'

    foreach ($badReference in @(
        @{ Label = 'missing'; Expected = 'missing'; Path = (Join-Path $fixtureRoot 'missing-reference.png') },
        @{ Label = 'corrupt'; Expected = 'invalid'; Path = (Join-Path $fixtureRoot 'corrupt-reference.png') })) {
        if ($badReference.Label -eq 'corrupt') {
            [System.IO.File]::WriteAllBytes($badReference.Path, [byte[]](1, 2, 3))
        }
        $badReferenceRejected = $false
        try {
            Assert-VisiblePngTail -Paths $visibleTail -ReferencePath $badReference.Path `
                -Label "$($badReference.Label) reference fixture"
        }
        catch {
            $badReferenceRejected = $_.Exception.Message -match
                "same-scene reference PNG is $($badReference.Expected)"
        }
        Assert-True $badReferenceRejected "$($badReference.Label) reference fails closed"
    }

    $wrongSizeReferencePath = Join-Path $fixtureRoot 'wrong-size-reference.png'
    $wrongSizeReference = [System.Drawing.Bitmap]::new(32, 18)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($wrongSizeReference)
        try {
            $graphics.Clear([System.Drawing.Color]::FromArgb(9, 15, 20))
            $graphics.FillRectangle([System.Drawing.Brushes]::Goldenrod, 4, 4, 24, 10)
        }
        finally { $graphics.Dispose() }
        $wrongSizeReference.Save($wrongSizeReferencePath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $wrongSizeReference.Dispose() }
    $wrongSizeRejected = $false
    try {
        Assert-VisiblePngTail -Paths $visibleTail -ReferencePath $wrongSizeReferencePath `
            -Label 'wrong-size reference fixture'
    }
    catch { $wrongSizeRejected = $_.Exception.Message -match 'dimensions differ' }
    Assert-True $wrongSizeRejected 'reference dimension mismatch fails closed'

    $forcedLog = Join-Path $fixtureRoot 'forced.log'
    Set-Content -LiteralPath $forcedLog -Value 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE'
    $expectedErrorFilter = Get-Command Assert-OnlyExpectedEngineError -ErrorAction SilentlyContinue
    Assert-True ($null -ne $expectedErrorFilter) `
        'forced-failure verification exposes an exact expected-error filter'
    Assert-OnlyExpectedEngineError -Path $forcedLog -ExpectedPattern 'FORCED_SMOKE_FAILURE'
    Add-Content -LiteralPath $forcedLog -Value 'SCRIPT ERROR: unexpected fixture failure'
    $extraForcedErrorRejected = $false
    try {
        Assert-OnlyExpectedEngineError -Path $forcedLog -ExpectedPattern 'FORCED_SMOKE_FAILURE'
    }
    catch { $extraForcedErrorRejected = $true }
    Assert-True $extraForcedErrorRejected `
        'forced-failure verification rejects any second engine or script error'

    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
    $drainPluginRelative = 'addons\verification_gc_drain\plugin.cfg'
    $drainSourceRelative = 'addons\verification_gc_drain\VerificationGcDrain.cs'
    $drainPluginPath = Join-Path $projectRoot $drainPluginRelative
    $drainSourcePath = Join-Path $projectRoot $drainSourceRelative
    Assert-True (Test-Path -LiteralPath $drainPluginPath -PathType Leaf) `
        'headless editor GC-drain plugin descriptor exists'
    Assert-True (Test-Path -LiteralPath $drainSourcePath -PathType Leaf) `
        'headless editor GC-drain plugin source exists'

    $projectSettings = Get-Content -LiteralPath (Join-Path $projectRoot 'project.godot') -Raw
    $drainResourcePath = 'res://addons/verification_gc_drain/plugin.cfg'
    Assert-True ([regex]::Matches(
        $projectSettings, [regex]::Escape($drainResourcePath)).Count -eq 1) `
        'GC-drain plugin is enabled exactly once and is not a runtime autoload'

    $drainPlugin = Get-Content -LiteralPath $drainPluginPath -Raw
    Assert-True ($drainPlugin.Contains('script="VerificationGcDrain.cs"')) `
        'GC-drain plugin descriptor binds the reviewed C# source'
    $drainSource = Get-Content -LiteralPath $drainSourcePath -Raw
    Assert-True ($drainSource.Contains('[Tool]') -and $drainSource.Contains(': EditorPlugin')) `
        'GC drain is an editor-only plugin'
    Assert-True ($drainSource.Contains('public override void _ExitTree()')) `
        'GC drain runs only from editor teardown'
    Assert-True ($drainSource.Contains(
        'if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.Ordinal))')) `
        'GC drain has an ordinal headless DisplayServer guard'
    Assert-True ($drainSource.Contains('GC.WaitForPendingFinalizers()')) `
        'GC drain finishes managed wrappers before native teardown'
    Assert-True (-not [regex]::IsMatch(
        $drainSource,
        '\b(?:_Process|_PhysicsProcess|SetProcess|SetPhysicsProcess|AddChild)\s*\(')) `
        'GC drain has no resident per-frame or game-tree behavior'
    $runtimeDrainReferences = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'scenes') `
        -Recurse -File -Filter '*.tscn' | Select-String -SimpleMatch $drainResourcePath)
    Assert-True ($runtimeDrainReferences.Count -eq 0) `
        'GC drain is absent from runtime game scenes'

    foreach ($leakLine in @(
        'WARNING: 4 ObjectDB instances were leaked at exit',
        'ERROR: 2 resources still in use at exit')) {
        Set-Content -LiteralPath $forcedLog `
            -Value 'ERROR: System.InvalidOperationException: FORCED_SMOKE_FAILURE'
        Add-Content -LiteralPath $forcedLog -Value $leakLine
        $lateLeakRejected = $false
        try {
            Assert-OnlyExpectedEngineError -Path $forcedLog -ExpectedPattern 'FORCED_SMOKE_FAILURE'
        }
        catch { $lateLeakRejected = $true }
        Assert-True $lateLeakRejected `
            "forced-failure verification rejects late cleanup output '$leakLine'"
    }

    $movie = Join-Path $fixtureRoot 'prologue.avi'
    [System.IO.File]::WriteAllBytes($movie, [byte[]](1, 2, 3))
    $startedUtc = [DateTime]::UtcNow.AddSeconds(-1)
    Assert-FreshNonEmptyOutputs -Paths @($movie) -StartedUtc $startedUtc
    (Get-Item -LiteralPath $movie).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-5)
    $staleRejected = $false
    try { Assert-FreshNonEmptyOutputs -Paths @($movie) -StartedUtc $startedUtc } catch { $staleRejected = $true }
    Assert-True $staleRejected 'stale movie evidence is rejected'
    [System.IO.File]::WriteAllBytes($movie, [byte[]]::new(0))
    (Get-Item -LiteralPath $movie).LastWriteTimeUtc = [DateTime]::UtcNow
    $emptyRejected = $false
    try { Assert-FreshNonEmptyOutputs -Paths @($movie) -StartedUtc $startedUtc } catch { $emptyRejected = $true }
    Assert-True $emptyRejected 'empty movie evidence is rejected'

    $sceneBefore = @'
[gd_scene format=3]
[ext_resource type="Script" path="res://fixture.cs" id="1_old"]
[sub_resource type="Resource" id="Resource_old"]
script = ExtResource("1_old")
[node name="Fixture" type="Node" unique_id=101]
metadata/value = SubResource("Resource_old")
'@
    $sceneRegenerated = @'
[gd_scene format=3]
[ext_resource type="Script" path="res://fixture.cs" id="1_new"]
[sub_resource type="Resource" id="Resource_new"]
script = ExtResource("1_new")
[node name="Fixture" type="Node" unique_id=999]
metadata/value = SubResource("Resource_new")
'@
    $stableSemanticGate = Get-Command Assert-StableGeneratedSceneSemantics `
        -ErrorAction SilentlyContinue
    Assert-True ($null -ne $stableSemanticGate) `
        'dirty generated scenes expose a stable committed-canonical semantic gate'
    Assert-GodotSceneSemanticallyEquivalent -ExpectedText $sceneBefore `
        -ActualText $sceneRegenerated -Label 'generated-id-fixture'
    $dirtyBuilderMutation = $sceneRegenerated.Replace(
        'metadata/value = SubResource("Resource_new")', 'metadata/value = null')
    $dirtyBuilderMutationRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics -StartupText $sceneRegenerated `
            -CommittedText $sceneBefore -ActualText $dirtyBuilderMutation `
            -StartedDirty $true -Label 'dirty-builder-mutation-fixture'
    }
    catch {
        $dirtyBuilderMutationRejected = $_.Exception.Message -match
            'Generated Godot scene changed semantic content: dirty-builder-mutation-fixture'
    }
    Assert-True $dirtyBuilderMutationRejected `
        'dirty generated scene mutation is compared with committed canonical semantics and rejected'

    $actorSceneWithoutInheritedOverride = @'
[gd_scene format=3]
[ext_resource type="PackedScene" path="res://scenes/presentation/RustShellFormalVisual.tscn" id="1_old"]
[node name="RustShellSquire" type="CharacterBody2D"]
[node name="VisualRoot" type="Node2D" parent="."]
[node name="Formal" type="Node2D" parent="VisualRoot" instance=ExtResource("1_old")]
'@
    $actorSceneWithRedundantInheritedOverride = @'
[gd_scene format=3]
[ext_resource type="PackedScene" path="res://scenes/presentation/RustShellFormalVisual.tscn" id="1_new"]
[node name="RustShellSquire" type="CharacterBody2D"]
[node name="VisualRoot" type="Node2D" parent="."]
[node name="Formal" type="Node2D" parent="VisualRoot" instance=ExtResource("1_new")]
metadata/profile_bounds = Rect2(-42, -42, 104, 68)
'@
    $formalVisualScene = @'
[gd_scene format=3]
[node name="RustShellFormalVisual" type="Node2D"]
metadata/profile_bounds = Rect2(-42, -42, 104, 68)
'@
    # Production scenes are read from Windows files as CRLF.  Keep this parser
    # fixture explicit so an LF-only shell does not hide a Windows regression.
    $actorSceneWithoutInheritedOverride = $actorSceneWithoutInheritedOverride -replace "`r?`n", "`r`n"
    $actorSceneWithRedundantInheritedOverride = $actorSceneWithRedundantInheritedOverride -replace "`r?`n", "`r`n"
    $formalVisualScene = $formalVisualScene -replace "`r?`n", "`r`n"
    Assert-True ($actorSceneWithRedundantInheritedOverride.Contains("`r`n")) `
        'redundant actor profile fixture uses CRLF production scene text'
    $formalResources = @{
        'res://scenes/presentation/RustShellFormalVisual.tscn' = $formalVisualScene
    }
    Assert-StableGeneratedSceneSemantics `
        -StartupText $actorSceneWithoutInheritedOverride `
        -CommittedText $actorSceneWithoutInheritedOverride `
        -ActualText $actorSceneWithRedundantInheritedOverride `
        -StartedDirty $true -Label 'scenes\enemies\RustShellSquire.tscn' `
        -ExpectedResourceTexts $formalResources -ActualResourceTexts $formalResources

    $changedProfileBoundsRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics `
            -StartupText $actorSceneWithoutInheritedOverride `
            -CommittedText $actorSceneWithoutInheritedOverride `
            -ActualText ($actorSceneWithRedundantInheritedOverride.Replace(
                'Rect2(-42, -42, 104, 68)', 'Rect2(-41, -42, 104, 68)')) `
            -StartedDirty $true -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts $formalResources -ActualResourceTexts $formalResources
    }
    catch { $changedProfileBoundsRejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $changedProfileBoundsRejected `
        'changed actor profile_bounds override remains semantic drift'

    $otherMetadataRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics `
            -StartupText $actorSceneWithoutInheritedOverride `
            -CommittedText $actorSceneWithoutInheritedOverride `
            -ActualText ($actorSceneWithRedundantInheritedOverride +
                "`nmetadata/semantic_layer = `"unexpected`"") `
            -StartedDirty $true -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts $formalResources -ActualResourceTexts $formalResources
    }
    catch { $otherMetadataRejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $otherMetadataRejected `
        'non-profile metadata on the actor instance remains semantic drift'

    $differentNodeOverride = $actorSceneWithoutInheritedOverride + @'

[node name="NotFormal" type="Node2D" parent="VisualRoot" instance=ExtResource("1_old")]
metadata/profile_bounds = Rect2(-42, -42, 104, 68)
'@
    $differentNodeRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics `
            -StartupText $actorSceneWithoutInheritedOverride `
            -CommittedText $actorSceneWithoutInheritedOverride `
            -ActualText $differentNodeOverride -StartedDirty $true `
            -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts $formalResources -ActualResourceTexts $formalResources
    }
    catch { $differentNodeRejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $differentNodeRejected `
        'profile_bounds on a different actor node is never folded'

    $missingFormalReferenceRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics `
            -StartupText $actorSceneWithoutInheritedOverride `
            -CommittedText $actorSceneWithoutInheritedOverride `
            -ActualText $actorSceneWithRedundantInheritedOverride `
            -StartedDirty $true -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts @{} -ActualResourceTexts @{}
    }
    catch { $missingFormalReferenceRejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $missingFormalReferenceRejected `
        'missing referenced PackedScene text cannot fold an actor profile_bounds override'

    $malformedFormalReferenceRejected = $false
    try {
        Assert-StableGeneratedSceneSemantics `
            -StartupText $actorSceneWithoutInheritedOverride `
            -CommittedText $actorSceneWithoutInheritedOverride `
            -ActualText $actorSceneWithRedundantInheritedOverride `
            -StartedDirty $true -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts $formalResources `
            -ActualResourceTexts @{
                'res://scenes/presentation/RustShellFormalVisual.tscn' =
                    '[node name="BrokenFormal" type="Node2D" parent="."]'
            }
    }
    catch { $malformedFormalReferenceRejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $malformedFormalReferenceRejected `
        'unparseable referenced PackedScene root cannot fold an actor profile_bounds override'

    $restoreGate = Get-Command Restore-ProtectedFileSnapshots -ErrorAction SilentlyContinue
    Assert-True ($null -ne $restoreGate) `
        'protected snapshots expose best-effort aggregate restoration'
    $restorePaths = @('restore-first.bin', 'restore-later.bin', 'restore-last.bin')
    $restoreSnapshots = @{
        'restore-first.bin' = [byte[]](1, 2, 3)
        'restore-later.bin' = [byte[]](4, 5, 6)
        'restore-last.bin' = [byte[]](7, 8, 9)
    }
    foreach ($relativePath in $restorePaths) {
        [System.IO.File]::WriteAllBytes((Join-Path $fixtureRoot $relativePath), [byte[]](99))
    }
    $restoreAttempts = [System.Collections.Generic.List[string]]::new()
    $faultingWriter = {
        param([string]$Path, [byte[]]$Bytes)
        $name = [System.IO.Path]::GetFileName($Path)
        $restoreAttempts.Add($name)
        if ($name -ceq 'restore-first.bin') { throw 'INJECTED_RESTORE_WRITE_FAILURE' }
        [System.IO.File]::WriteAllBytes($Path, $Bytes)
    }
    $aggregateRestoreMessage = [string]::Empty
    try {
        Restore-ProtectedFileSnapshots -Root $fixtureRoot -RelativePaths $restorePaths `
            -Snapshots $restoreSnapshots -WriteBytes $faultingWriter
    }
    catch { $aggregateRestoreMessage = $_.Exception.Message }
    Assert-True ($restoreAttempts.Count -eq 3 -and
        $restoreAttempts[0] -ceq 'restore-first.bin' -and
        $restoreAttempts[1] -ceq 'restore-later.bin' -and
        $restoreAttempts[2] -ceq 'restore-last.bin') `
        'restoration attempts every later protected file after the injected first write failure'
    Assert-True ([System.Linq.Enumerable]::SequenceEqual(
        [System.IO.File]::ReadAllBytes((Join-Path $fixtureRoot 'restore-later.bin')),
        $restoreSnapshots['restore-later.bin']) -and
        [System.Linq.Enumerable]::SequenceEqual(
            [System.IO.File]::ReadAllBytes((Join-Path $fixtureRoot 'restore-last.bin')),
            $restoreSnapshots['restore-last.bin'])) `
        'later protected files restore exact bytes despite an earlier write failure'
    Assert-True ($aggregateRestoreMessage.Contains('restore-first.bin') -and
        $aggregateRestoreMessage.Contains('INJECTED_RESTORE_WRITE_FAILURE')) `
        'aggregate restoration error names the failed protected path and injected cause'
    $sceneReferenceDrift = $sceneRegenerated.Replace(
        'metadata/value = SubResource("Resource_new")', 'metadata/value = null')
    $referenceDriftRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $sceneBefore `
            -ActualText $sceneReferenceDrift -Label 'generated-id-fixture'
    }
    catch { $referenceDriftRejected = $true }
    Assert-True $referenceDriftRejected 'resource-reference semantic drift is still rejected'

    $runtimeSemantics = Get-Command Get-Task6GeneratedRuntimeSemantics -ErrorAction SilentlyContinue
    Assert-True ($null -ne $runtimeSemantics) `
        'generated leaf-to-main runtime outputs expose a semantic composite fingerprint helper'
    $runtimeSceneA = Join-Path $fixtureRoot 'runtime-a.tscn'
    $runtimeSceneB = Join-Path $fixtureRoot 'runtime-b.tscn'
    [System.IO.File]::WriteAllText($runtimeSceneA, @'
[gd_scene format=3]

[node name="RuntimeRoot" type="Node2D" unique_id=101]
metadata/route = "canyon"
'@)
    [System.IO.File]::WriteAllText($runtimeSceneB, @'
[gd_scene format=3]

[node name="RuntimeLeaf" type="Node2D" unique_id=202]
metadata/profile = "grapple"
'@)
    $runtimePaths = @('runtime-a.tscn', 'runtime-b.tscn')
    $runtimeFirst = Get-Task6GeneratedRuntimeSemantics -Root $fixtureRoot -RelativePaths $runtimePaths
    [System.IO.File]::WriteAllText($runtimeSceneA, @'
[gd_scene format=3]

[node name="RuntimeRoot" type="Node2D" unique_id=303]
metadata/route = "canyon"
'@)
    [System.IO.File]::WriteAllText($runtimeSceneB, @'
[gd_scene format=3]

[node name="RuntimeLeaf" type="Node2D" unique_id=404]
metadata/profile = "grapple"
'@)
    $runtimeSecond = Get-Task6GeneratedRuntimeSemantics -Root $fixtureRoot -RelativePaths $runtimePaths
    Assert-True ($runtimeFirst.Count -eq 2 -and $runtimeSecond.Count -eq 2 -and
        $runtimeFirst.Sha256 -ceq $runtimeSecond.Sha256) `
        'generated runtime semantic fingerprint is stable across Godot unique_id serialization churn'
    [System.IO.File]::WriteAllText($runtimeSceneB, @'
[gd_scene format=3]

[node name="RuntimeLeaf" type="Node2D" unique_id=505]
metadata/profile = "forbidden"
'@)
    $runtimeTampered = Get-Task6GeneratedRuntimeSemantics -Root $fixtureRoot -RelativePaths $runtimePaths
    Assert-True ($runtimeTampered.Sha256 -cne $runtimeFirst.Sha256) `
        'generated runtime semantic fingerprint rejects a topology or profile tamper'

    $shortAvi = Join-Path $fixtureRoot 'short.avi'
    $header = [byte[]]::new(60)
    [System.Text.Encoding]::ASCII.GetBytes('RIFF').CopyTo($header, 0)
    [System.Text.Encoding]::ASCII.GetBytes('AVI ').CopyTo($header, 8)
    [System.Text.Encoding]::ASCII.GetBytes('avih').CopyTo($header, 12)
    [BitConverter]::GetBytes([uint32]40).CopyTo($header, 16)
    [BitConverter]::GetBytes([uint32]16667).CopyTo($header, 20)
    [BitConverter]::GetBytes([uint32]599).CopyTo($header, 36)
    [BitConverter]::GetBytes([uint32]1280).CopyTo($header, 52)
    [BitConverter]::GetBytes([uint32]720).CopyTo($header, 56)
    [System.IO.File]::WriteAllBytes($shortAvi, $header)
    $shortProofRejected = $false
    try {
        Assert-AviCaptureContract -Path $shortAvi -ExpectedFrameCount 599 -ExpectedWidth 1280 -ExpectedHeight 720 -ExpectedFps 60
    }
    catch { $shortProofRejected = $true }
    Assert-True $shortProofRejected 'a header-only AVI with plausible metadata is rejected'

    $prologueTestRunner = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tests\RunPrologueTests.cs'))
    $bossTestRunner = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tests\RunFallenBellkeeperRuntimeTests.cs'))
    $task4AudioVfxRunner = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tests\RunTask4AudioVfxTests.cs'))
    $prologueVerifier = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\verify-prologue.ps1'))
    $baseHardGates = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\verify-prologue-base-hardgates.ps1'))
    $productionAudit = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\audit-prologue-production.ps1'))
    $baseProductionAudit = [System.IO.File]::ReadAllText(
        (Join-Path $project 'tools\audit-prologue-base-hardgates.ps1'))
    Assert-True (-not $prologueTestRunner.Contains(
        'await FallenBellkeeperRuntimeTests.Run(tree);')) `
        'high-churn Boss runtime suite is isolated from the long post phase'
    Assert-True ($bossTestRunner.Contains('for (int pass = 0; pass < 2; pass++)') -and
        $bossTestRunner.Contains('await FallenBellkeeperRuntimeTests.Run(this);')) `
        'isolated Boss runtime process repeats the complete suite twice'
    Assert-True ($bossTestRunner.Contains(
        'TestSceneTeardown.DrainManagedWrappersAtProcessExit("boss-runtime");')) `
        'isolated Boss runtime process drains managed wrappers before success'
    foreach ($requiredBossIsolationContract in @(
        "'res://tests/RunFallenBellkeeperRuntimeTests.cs'",
        "-Marker 'FALLEN_BELLKEEPER_RUNTIME_TESTS_OK passes=2'",
        "-Marker 'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=boss-runtime'",
        'Assert-NoEngineLogErrors -Paths @($bossRuntimeLog, $bossRuntimeConsole)')) {
        Assert-True ($baseHardGates.Contains($requiredBossIsolationContract)) `
            "BASE hard-gate producer owns fail-closed isolated Boss evidence: $requiredBossIsolationContract"
    }
    Assert-True ($prologueVerifier.Contains('verify-prologue-base-hardgates.ps1') -and
        $prologueVerifier.Contains('-ExternalEvidenceRoot')) `
        'full verifier delegates isolated Boss evidence to the external-root BASE hard-gate producer'
    Assert-True ($task4AudioVfxRunner.Contains(
        'TestSceneTeardown.DrainManagedWrappersAtProcessExit("task4-audio-vfx");')) `
        'display-backed audio/VFX runner drains managed wrappers before success'
    foreach ($requiredAudioVfxDrainContract in @(
        "'PROLOGUE_TEST_PHASE_EXIT_DRAIN_OK phase=task4-audio-vfx'",
        "'TASK4_AUDIO_VFX_TESTS_OK'",
        'Assert-OrderedExactLogMarkers -Path $task4Log -Markers $task4DrainMarkers')) {
        Assert-True ($baseProductionAudit.Contains($requiredAudioVfxDrainContract)) `
            "BASE audit producer owns ordered audio/VFX teardown evidence: $requiredAudioVfxDrainContract"
    }
    Assert-True ($productionAudit.Contains('audit-prologue-base-hardgates.ps1') -and
        $productionAudit.Contains('-AuditRoot $BaseAuditRoot')) `
        'production audit delegates audio/VFX evidence to the BASE audit producer'

    Write-Output 'PROLOGUE_EVIDENCE_CHECKS_OK'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
