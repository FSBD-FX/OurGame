[CmdletBinding()]
param(
    [string]$VerifyP0Path,
    [switch]$SkipMutation
)

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $project 'tools\EvidenceChecks.ps1')
if ([string]::IsNullOrWhiteSpace($VerifyP0Path)) {
    $VerifyP0Path = Join-Path $project 'tools\verify-p0.ps1'
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Assert-RejectsSemanticComparison(
    [string]$Expected,
    [string]$Actual,
    [System.Collections.IDictionary]$ExpectedResources,
    [System.Collections.IDictionary]$ActualResources,
    [string]$Message
) {
    $rejected = $false
    try {
        Assert-GodotSceneSemanticallyEquivalent -ExpectedText $Expected -ActualText $Actual `
            -Label 'scenes\enemies\RustShellSquire.tscn' `
            -ExpectedResourceTexts $ExpectedResources -ActualResourceTexts $ActualResources
    }
    catch { $rejected = $_.Exception.Message -match 'semantic content' }
    Assert-True $rejected $Message
}

$actorWithoutOverride = @'
[gd_scene format=3]
[ext_resource type="PackedScene" path="res://scenes/presentation/RustShellFormalVisual.tscn" id="1_actor"]
[node name="RustShellSquire" type="CharacterBody2D"]
[node name="VisualRoot" type="Node2D" parent="."]
[node name="Formal" type="Node2D" parent="VisualRoot" instance=ExtResource("1_actor")]
'@
$actorWithInheritedOverride = $actorWithoutOverride + [Environment]::NewLine +
    'metadata/profile_bounds = Rect2(-42, -42, 104, 68)' + [Environment]::NewLine
$formalRoot = @'
[gd_scene format=3]
[node name="RustShellFormalVisual" type="Node2D"]
metadata/profile_bounds = Rect2(-42, -42, 104, 68)
'@
$formalPath = 'res://scenes/presentation/RustShellFormalVisual.tscn'
$formalMap = @{ $formalPath = $formalRoot }

# RED: before verify-p0 provides the packed-scene maps, a regenerated inherited
# override is a real semantic difference and must fail closed.
Assert-RejectsSemanticComparison -Expected $actorWithoutOverride -Actual $actorWithInheritedOverride `
    -ExpectedResources $null -ActualResources $null `
    -Message 'resource-map-free actor comparison rejects an inherited profile_bounds override'
Write-Output 'TASK4_P0_SEMANTIC_NO_RESOURCE_MAP_RED_OBSERVED'

# GREEN: only the exact root value of the referenced PackedScene may be folded.
Assert-GodotSceneSemanticallyEquivalent -ExpectedText $actorWithoutOverride `
    -ActualText $actorWithInheritedOverride -Label 'scenes\enemies\RustShellSquire.tscn' `
    -ExpectedResourceTexts $formalMap -ActualResourceTexts $formalMap

Assert-RejectsSemanticComparison -Expected $actorWithoutOverride `
    -Actual ($actorWithInheritedOverride.Replace('Rect2(-42, -42, 104, 68)', 'Rect2(-41, -42, 104, 68)')) `
    -ExpectedResources $formalMap -ActualResources $formalMap `
    -Message 'non-inherited profile_bounds value remains semantic drift'
Assert-RejectsSemanticComparison -Expected $actorWithoutOverride `
    -Actual ($actorWithInheritedOverride + 'metadata/profile_bounds = Rect2(-42, -42, 104, 68)' + [Environment]::NewLine) `
    -ExpectedResources $formalMap -ActualResources $formalMap `
    -Message 'duplicate profile_bounds override remains semantic drift'
Assert-RejectsSemanticComparison -Expected $actorWithoutOverride -Actual $actorWithInheritedOverride `
    -ExpectedResources @{} -ActualResources @{} `
    -Message 'missing Formal PackedScene text cannot fold an override'
Assert-RejectsSemanticComparison -Expected $actorWithoutOverride -Actual $actorWithInheritedOverride `
    -ExpectedResources $formalMap -ActualResources @{ $formalPath = '[node name="BrokenFormal" type="Node2D" parent="."]' } `
    -Message 'malformed Formal PackedScene root cannot fold an override'

$verifySource = Get-Content -LiteralPath $VerifyP0Path -Raw
Assert-True ($verifySource -match '\$semanticResourcePaths\s*=') `
    'verify-p0 declares a resource scope for actor Formal PackedScenes'
Assert-True ($verifySource -match "'scenes\\presentation\\RustShellFormalVisual\.tscn'") `
    'verify-p0 includes RustShell Formal PackedScene in its resource scope'
Assert-True ($verifySource -match "'scenes\\presentation\\GraveLampMothFormalVisual\.tscn'") `
    'verify-p0 includes GraveLampMoth Formal PackedScene in its resource scope'
Assert-True ($verifySource -match "'scenes\\presentation\\BellBackLaborerFormalVisual\.tscn'") `
    'verify-p0 includes BellBackLaborer Formal PackedScene in its resource scope'
Assert-True ($verifySource -match '-ExpectedResourceTexts\s+\$snapshotResourceTexts') `
    'verify-p0 passes startup resource texts into semantic comparison'
Assert-True ($verifySource -match '-ActualResourceTexts\s+\$builtResourceTexts') `
    'verify-p0 passes built resource texts into semantic comparison'

if (-not $SkipMutation) {
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) (
        'knight-task4-p0-semantic-wiring-' + [guid]::NewGuid().ToString('N') + '.ps1')
    try {
        $mutated = $verifySource -replace '(?ms)\s*-ExpectedResourceTexts\s+\$snapshotResourceTexts\s*`?\r?\n\s*-ActualResourceTexts\s+\$builtResourceTexts', ''
        Assert-True ($mutated -cne $verifySource) 'wiring mutation deletes both resource-map arguments'
        [System.IO.File]::WriteAllText($tempPath, $mutated, [System.Text.UTF8Encoding]::new($false))
        $priorErrorAction = $ErrorActionPreference
        try {
            $ErrorActionPreference = 'Continue'
            & powershell -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath `
                -VerifyP0Path $tempPath -SkipMutation 2>&1 | Out-Null
            $mutationExit = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $priorErrorAction
        }
        Assert-True ($mutationExit -ne 0) 'deleting verify-p0 resource-map wiring fails the focused contract'
        Write-Output "TASK4_P0_SEMANTIC_WIRING_DELETE_RED_OBSERVED exit=$mutationExit"
    }
    finally {
        if (Test-Path -LiteralPath $tempPath -PathType Leaf) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction Stop
        }
    }
}

Write-Output 'TASK4_P0_SEMANTIC_RESOURCE_WIRING_OK'
