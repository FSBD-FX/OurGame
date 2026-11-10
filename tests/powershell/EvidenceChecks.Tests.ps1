$ErrorActionPreference = 'Stop'
$helper = Join-Path $PSScriptRoot '..\..\tools\EvidenceChecks.ps1'
. $helper

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Get-InnermostExceptionMessage([Exception]$Exception) {
    while ($null -ne $Exception.InnerException) { $Exception = $Exception.InnerException }
    return $Exception.Message
}

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("knight-evidence-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
try {
    $avi = Join-Path $fixtureRoot 'prototype.avi'
    $frame = Join-Path $fixtureRoot 'frame00000000.png'
    $audio = Join-Path $fixtureRoot 'frame.wav'
    $unrelated = Join-Path $fixtureRoot 'keep.txt'
    Set-Content -LiteralPath $avi -Value 'old avi'
    Set-Content -LiteralPath $frame -Value 'old frame'
    Set-Content -LiteralPath $audio -Value 'old audio'
    Set-Content -LiteralPath $unrelated -Value 'keep'

    Remove-PrototypeCaptureOutputs -ProofPath $fixtureRoot
    Assert-True (-not (Test-Path -LiteralPath $avi)) 'AVI mode removes only the prior AVI'
    Assert-True (Test-Path -LiteralPath $frame) 'AVI mode preserves numbered frames'
    Assert-True (Test-Path -LiteralPath $audio) 'AVI mode preserves frame audio'
    Assert-True (Test-Path -LiteralPath $unrelated) 'AVI mode preserves unrelated files'

    Set-Content -LiteralPath $avi -Value 'keep avi'
    Remove-PrototypeCaptureOutputs -ProofPath $fixtureRoot -FramesOnly
    Assert-True (Test-Path -LiteralPath $avi) 'frame mode preserves AVI'
    Assert-True (-not (Test-Path -LiteralPath $frame)) 'frame mode removes numbered frames'
    Assert-True (-not (Test-Path -LiteralPath $audio)) 'frame mode removes frame audio'
    Assert-True (Test-Path -LiteralPath $unrelated) 'frame mode preserves unrelated files'

    $log = Join-Path $fixtureRoot 'evidence.log'
    Set-Content -LiteralPath $log -Value 'not the marker'
    $missingRejected = $false
    try { Assert-ExactLogMarker -Path $log -Marker 'EXPECTED_OK' } catch { $missingRejected = $true }
    Assert-True $missingRejected 'missing marker is rejected'

    Set-Content -LiteralPath $log -Value @('prefix EXPECTED_OK', 'EXPECTED_OK', 'EXPECTED_OK')
    $duplicateRejected = $false
    try { Assert-ExactLogMarker -Path $log -Marker 'EXPECTED_OK' } catch { $duplicateRejected = $true }
    Assert-True $duplicateRejected 'marker must be one exact line exactly once'

    Set-Content -LiteralPath $log -Value 'EXPECTED_OK'
    Assert-ExactLogMarker -Path $log -Marker 'EXPECTED_OK'

    Set-Content -LiteralPath $log -Value 'ERROR: fixture failure'
    $engineErrorRejected = $false
    try { Assert-NoEngineLogErrors -Paths @($log) } catch { $engineErrorRejected = $true }
    Assert-True $engineErrorRejected 'engine error lines are rejected'

    $fresh = Join-Path $fixtureRoot 'fresh.bin'
    Set-Content -LiteralPath $fresh -Value 'payload'
    $startedUtc = [DateTime]::UtcNow.AddSeconds(-1)
    Assert-FreshNonEmptyOutputs -Paths @($fresh) -StartedUtc $startedUtc
    (Get-Item -LiteralPath $fresh).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-5)
    $staleRejected = $false
    try { Assert-FreshNonEmptyOutputs -Paths @($fresh) -StartedUtc $startedUtc } catch { $staleRejected = $true }
    Assert-True $staleRejected 'stale output is rejected'

    $beforeScene = @'
[gd_scene format=3]
[node name="Fixture" type="Node2D" unique_id=101]
position = Vector2(1, 2)
'@
    $newIdsOnly = @'
[gd_scene format=3]
[node name="Fixture" type="Node2D" unique_id=999]
position = Vector2(1, 2)
'@
    Assert-GodotSceneSemanticallyEquivalent -ExpectedText $beforeScene -ActualText $newIdsOnly -Label 'fixture'

    $semanticDrift = $newIdsOnly.Replace('Vector2(1, 2)', 'Vector2(3, 4)')
    $driftRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $beforeScene -ActualText $semanticDrift -Label 'fixture'
    } catch { $driftRejected = $true }
    Assert-True $driftRejected 'scene semantic drift is rejected even when generated IDs also change'

    $resourceOrderBefore = @'
[gd_scene format=3]
[ext_resource type="Script" path="res://fixture_a.cs" id="1_old"]
[ext_resource type="PackedScene" path="res://fixture_b.tscn" id="2_old"]
[sub_resource type="Resource" id="Resource_old"]
value = 7
[node name="Fixture" type="Node2D"]
script = ExtResource("1_old")
fixture = ExtResource("2_old")
data = SubResource("Resource_old")
'@
    $resourceOrderAfter = @'
[gd_scene format=3]
[ext_resource type="PackedScene" path="res://fixture_b.tscn" id="77_new"]
[ext_resource type="Script" path="res://fixture_a.cs" id="88_new"]
[sub_resource type="Resource" id="Resource_new"]
value = 7
[node name="Fixture" type="Node2D"]
script = ExtResource("88_new")
fixture = ExtResource("77_new")
data = SubResource("Resource_new")
'@
    Assert-GodotSceneSemanticallyEquivalent -ExpectedText $resourceOrderBefore `
        -ActualText $resourceOrderAfter -Label 'reordered-ext-resources'

    $ordinaryIdStringBefore = $resourceOrderBefore.Replace(
        'data = SubResource("Resource_old")',
        "data = SubResource(`"Resource_old`")`nmetadata/resource_note = `"1_old`"")
    $ordinaryIdStringAfter = $resourceOrderAfter.Replace(
        'data = SubResource("Resource_new")',
        "data = SubResource(`"Resource_new`")`nmetadata/resource_note = `"88_new`"")
    $ordinaryIdStringRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $ordinaryIdStringBefore `
            -ActualText $ordinaryIdStringAfter -Label 'ordinary-string-equals-ext-id'
    } catch { $ordinaryIdStringRejected = $true }
    Assert-True $ordinaryIdStringRejected `
        'ordinary metadata strings equal to regenerated ext IDs remain semantic content'

    $tokenCollisionBefore = $resourceOrderBefore.Replace(
        'data = SubResource("Resource_old")',
        "data = SubResource(`"Resource_old`")`nmetadata/resource_note = `"__GODOT_CANONICAL_EXT_RESOURCE_0__`"")
    $tokenCollisionAfter = $resourceOrderAfter.Replace(
        'data = SubResource("Resource_new")',
        "data = SubResource(`"Resource_new`")`nmetadata/resource_note = `"88_new`"")
    $tokenCollisionRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $tokenCollisionBefore `
            -ActualText $tokenCollisionAfter -Label 'generated-token-collision'
    } catch { $tokenCollisionRejected = $true }
    Assert-True $tokenCollisionRejected `
        'ordinary strings cannot collide with generated normalization tokens'

    $duplicateExtId = $resourceOrderBefore.Replace(
        'id="2_old"', 'id="1_old"').Replace(
        'ExtResource("2_old")', 'ExtResource("1_old")')
    $duplicateExtIdRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $duplicateExtId `
            -ActualText $duplicateExtId -Label 'duplicate-ext-id'
    } catch { $duplicateExtIdRejected = $true }
    Assert-True $duplicateExtIdRejected 'duplicate ext_resource IDs are rejected as ambiguous'

    $duplicateExtIdentity = $resourceOrderBefore.Replace(
        'type="PackedScene" path="res://fixture_b.tscn"',
        'type="Script" path="res://fixture_a.cs"')
    $duplicateExtIdentityRejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $duplicateExtIdentity `
            -ActualText $duplicateExtIdentity -Label 'duplicate-ext-identity'
    } catch { $duplicateExtIdentityRejected = $true }
    Assert-True $duplicateExtIdentityRejected `
        'duplicate ext_resource type/path identities are rejected as ambiguous'

    $duplicateSubDeclaration = @'
[sub_resource type="Resource" id="Resource_old"]
[node name="Fixture" type="Node2D"]
'@
    $duplicateSubId = $resourceOrderBefore.Replace(
        '[node name="Fixture" type="Node2D"]', $duplicateSubDeclaration)
    $duplicateSubIdReason = ''
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $duplicateSubId `
            -ActualText $duplicateSubId -Label 'duplicate-sub-resource-id'
    } catch { $duplicateSubIdReason = Get-InnermostExceptionMessage $_.Exception }
    Assert-True ($duplicateSubIdReason -ceq
        "Duplicate sub_resource id 'Resource_old' is ambiguous.") `
        'duplicate sub_resource IDs fail for the explicit ambiguity reason'

    $missingExtReference = $resourceOrderAfter.Replace(
        'fixture = ExtResource("77_new")', 'fixture = ExtResource("missing")')
    $missingExtReason = ''
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $resourceOrderBefore `
            -ActualText $missingExtReference -Label 'missing-ext-resource-reference'
    } catch { $missingExtReason = Get-InnermostExceptionMessage $_.Exception }
    Assert-True ($missingExtReason -ceq
        "ExtResource reference uses undeclared id 'missing'.") `
        'undeclared ExtResource references fail for the explicit missing-ID reason'

    $missingSubReference = $resourceOrderAfter.Replace(
        'data = SubResource("Resource_new")', 'data = SubResource("missing")')
    $missingSubReason = ''
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $resourceOrderBefore `
            -ActualText $missingSubReference -Label 'missing-sub-resource-reference'
    } catch { $missingSubReason = Get-InnermostExceptionMessage $_.Exception }
    Assert-True ($missingSubReason -ceq
        "SubResource reference uses undeclared id 'missing'.") `
        'undeclared SubResource references fail for the explicit missing-ID reason'

    foreach ($negative in @(
        @{ Name = 'ext-resource path'; Text = $resourceOrderAfter.Replace('fixture_b.tscn', 'fixture_c.tscn') },
        @{ Name = 'ext-resource type'; Text = $resourceOrderAfter.Replace('type="PackedScene"', 'type="Resource"') },
        @{ Name = 'ext-resource wiring'; Text = $resourceOrderAfter.Replace('fixture = ExtResource("77_new")', 'fixture = ExtResource("88_new")') },
        @{ Name = 'sub-resource content'; Text = $resourceOrderAfter.Replace('value = 7', 'value = 8') },
        @{ Name = 'ordinary scene semantics'; Text = $resourceOrderAfter.Replace('type="Node2D"', 'type="Node"') }
    )) {
        $negativeRejected = $false
        try {
            Assert-GodotSceneSemanticallyEquivalent -ExpectedText $resourceOrderBefore `
                -ActualText $negative.Text -Label $negative.Name
        } catch { $negativeRejected = $true }
        Assert-True $negativeRejected "$($negative.Name) drift remains rejected"
    }

    Assert-RequiredBuilderPlan -BuilderNames @('BuildPlayer', 'BuildRustShellSquire', 'BuildPrototypeRoom')
    $missingBuilderRejected = $false
    try { Assert-RequiredBuilderPlan -BuilderNames @('BuildPlayer', 'BuildPrototypeRoom') } catch {
        $missingBuilderRejected = $true
    }
    Assert-True $missingBuilderRejected 'a missing leaf scene builder is rejected'
    $wrongOrderRejected = $false
    try { Assert-RequiredBuilderPlan -BuilderNames @('BuildPrototypeRoom', 'BuildPlayer', 'BuildRustShellSquire') } catch {
        $wrongOrderRejected = $true
    }
    Assert-True $wrongOrderRejected 'parent-before-leaf scene generation is rejected'

    $verifyScript = Get-Content -LiteralPath (Join-Path $PSScriptRoot '..\..\tools\verify.ps1') -Raw
    $builderPositions = @(
        $verifyScript.IndexOf("Name = 'BuildPlayer'", [StringComparison]::Ordinal),
        $verifyScript.IndexOf("Name = 'BuildRustShellSquire'", [StringComparison]::Ordinal),
        $verifyScript.IndexOf("Name = 'BuildPrototypeRoom'", [StringComparison]::Ordinal)
    )
    Assert-True ($builderPositions[0] -ge 0 -and $builderPositions[0] -lt $builderPositions[1] -and
        $builderPositions[1] -lt $builderPositions[2]) 'verify.ps1 contains the required leaf-to-parent builder plan'
    Assert-True ($verifyScript.Contains('Assert-GodotSceneSemanticallyEquivalent')) `
        'verify.ps1 gates generated scene semantics'
    Assert-True ($verifyScript.Contains("[System.IO.File]::WriteAllText(`$path, `$sceneSnapshots[`$relativePath]")) `
        'verify.ps1 restores generated scene snapshots'

    Write-Output 'EVIDENCE_CHECKS_OK'
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
