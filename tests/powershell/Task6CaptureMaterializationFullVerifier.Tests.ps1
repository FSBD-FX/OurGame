[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$FormalCaptureRoot,
    [Parameter(Mandatory)][string]$MaterializationCaptureRoot,
    [Parameter(Mandatory)][string]$RedRoot,
    [string]$EvidenceLog
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$FormalCaptureRoot = (Resolve-Path -LiteralPath $FormalCaptureRoot).Path
$MaterializationCaptureRoot = (Resolve-Path -LiteralPath $MaterializationCaptureRoot).Path
. (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1')

function Assert-Task6CaptureMaterializationFullVerifier {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "Task6 full capture-materialization verifier assertion failed: $Message" }
}

function Get-Task6CaptureMaterializationFullVerifierChildHost {
    $current = Get-Process -Id $PID -ErrorAction SilentlyContinue
    if ($null -ne $current -and -not [string]::IsNullOrWhiteSpace($current.Path) -and
        (Test-Path -LiteralPath $current.Path -PathType Leaf)) {
        return $current.Path
    }
    foreach ($name in @('pwsh', 'powershell.exe', 'powershell')) {
        $command = Get-Command $name -ErrorAction SilentlyContinue
        if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
            return $command.Source
        }
    }
    throw 'Task6 full capture-materialization verifier cannot resolve a child PowerShell host.'
}

function Get-Task6CaptureMaterializationFullVerifierCandidateFingerprint {
    $capture = Join-Path $ProjectRoot 'tools\capture-prologue.ps1'
    $childHost = Get-Task6CaptureMaterializationFullVerifierChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $capture -RuntimeFingerprintProbeOnly 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    Assert-Task6CaptureMaterializationFullVerifier ($exitCode -eq 0) 'capture fingerprint probe succeeds'
    $markers = @($output | Where-Object { $_.ToString() -match '^TASK6_CAPTURE_RUNTIME_FINGERPRINT_OK ' })
    Assert-Task6CaptureMaterializationFullVerifier ($markers.Count -eq 1) 'capture fingerprint probe emits exactly one terminal'
    $match = [regex]::Match($markers[0].ToString(), 'candidate_runtime_sha256=(?<hash>[A-Fa-f0-9]{64})')
    Assert-Task6CaptureMaterializationFullVerifier $match.Success 'capture fingerprint probe contains a candidate SHA-256'
    return $match.Groups['hash'].Value
}

function Set-Task6CaptureMaterializationFullVerifierManifestValue {
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Value
    )
    $prefix = $Name + '='
    $seen = 0
    $rewritten = [Collections.Generic.List[string]]::new()
    foreach ($line in @(Get-Content -LiteralPath $Path)) {
        if ($line.StartsWith($prefix, [StringComparison]::Ordinal)) {
            $seen++
            $rewritten.Add($prefix + $Value)
        }
        else {
            $rewritten.Add($line)
        }
    }
    if ($seen -eq 0) {
        $rewritten.Add($prefix + $Value)
    }
    elseif ($seen -ne 1) {
        throw "Task6 full capture manifest must have at most one $Name entry; actual=$seen"
    }
    [IO.File]::WriteAllLines($Path, $rewritten.ToArray(), [Text.UTF8Encoding]::new($false))
}

function Get-Task6CaptureMaterializationFullVerifierSingleValue {
    param([Parameter(Mandatory)][string]$Path, [Parameter(Mandatory)][string]$Name)
    $prefix = $Name + '='
    $matches = @((Get-Content -LiteralPath $Path) | Where-Object {
            $_.StartsWith($prefix, [StringComparison]::Ordinal)
        })
    Assert-Task6CaptureMaterializationFullVerifier ($matches.Count -eq 1) "fixture has one $Name"
    return $matches[0].Substring($prefix.Length)
}

function Rebind-Task6CaptureMaterializationFullVerifierOuterManifest {
    param(
        [Parameter(Mandatory)]$Bundle,
        [Parameter(Mandatory)][string]$ManifestPath
    )
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $Bundle.Manifest `
        -Name 'task6_capture_materialization_manifest' -Value $ManifestPath
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $Bundle.Manifest `
        -Name 'task6_capture_materialization_manifest_sha256' -Value (Get-EvidenceSha256 -LiteralPath $ManifestPath)
}

function New-Task6CaptureMaterializationFullVerifierBundle {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$CandidateHash)

    $root = Join-Path $RedRoot $Name
    Assert-Task6CaptureMaterializationFullVerifier (-not (Test-Path -LiteralPath $root)) "fresh full verifier fixture root: $Name"
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    $sourceCaptureManifest = Join-Path $FormalCaptureRoot 'capture-manifest.txt'
    Assert-Task6CaptureMaterializationFullVerifier (Test-Path -LiteralPath $sourceCaptureManifest -PathType Leaf) `
        'old formal capture fixture has a capture manifest'
    $captureManifest = Join-Path $root 'capture-manifest.txt'
    Copy-Item -LiteralPath $sourceCaptureManifest -Destination $captureManifest -Force

    $sourceMaterializationManifest = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization.manifest.txt'
    $sourceMaterializationDirectory = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization'
    $sourceMaterializationLog = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization.log'
    Assert-Task6CaptureMaterializationFullVerifier (Test-Path -LiteralPath $sourceMaterializationManifest -PathType Leaf) `
        'source materialization manifest exists'
    Assert-Task6CaptureMaterializationFullVerifier (Test-Path -LiteralPath $sourceMaterializationDirectory -PathType Container) `
        'source materialization directory exists'
    Assert-Task6CaptureMaterializationFullVerifier (Test-Path -LiteralPath $sourceMaterializationLog -PathType Leaf) `
        'source materialization terminal exists'

    Copy-Item -LiteralPath $sourceMaterializationDirectory -Destination $root -Recurse -Force
    $materializationManifest = Join-Path $root 'task6-capture-materialization.manifest.txt'
    Copy-Item -LiteralPath $sourceMaterializationManifest -Destination $materializationManifest -Force
    $materializationLog = Join-Path $root 'task6-capture-materialization.log'
    Copy-Item -LiteralPath $sourceMaterializationLog -Destination $materializationLog -Force

    $sourceRoot = ([IO.Path]::GetFullPath($MaterializationCaptureRoot)).TrimEnd('\')
    $destinationRoot = ([IO.Path]::GetFullPath($root)).TrimEnd('\')
    $materializationText = [IO.File]::ReadAllText($materializationManifest)
    $rebasedMaterializationText = $materializationText.Replace($sourceRoot, $destinationRoot)
    Assert-Task6CaptureMaterializationFullVerifier ($rebasedMaterializationText -cne $materializationText) `
        'full verifier fixture rebases nested materialization evidence paths'
    [IO.File]::WriteAllText($materializationManifest, $rebasedMaterializationText, [Text.UTF8Encoding]::new($false))

    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest -Name 'capture_root' -Value $root
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest `
        -Name 'candidate_runtime_sha256' -Value $CandidateHash
    $bundle = [pscustomobject]@{
        Root = $root
        Manifest = $captureManifest
        MaterializationManifest = $materializationManifest
        MaterializationLog = $materializationLog
    }
    Rebind-Task6CaptureMaterializationFullVerifierOuterManifest -Bundle $bundle -ManifestPath $materializationManifest
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest `
        -Name 'task6_capture_materialization_log' -Value $materializationLog
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest `
        -Name 'task6_capture_materialization_log_sha256' -Value (Get-EvidenceSha256 -LiteralPath $materializationLog)
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest `
        -Name 'task6_generated_runtime_semantics_sha256' `
        -Value (Get-Task6CaptureMaterializationFullVerifierSingleValue -Path $materializationManifest -Name 'generated_runtime_semantics_sha256')
    Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $captureManifest `
        -Name 'task6_generated_runtime_semantics_count' `
        -Value (Get-Task6CaptureMaterializationFullVerifierSingleValue -Path $materializationManifest -Name 'generated_runtime_semantics_count')
    return $bundle
}

function Invoke-Task6CaptureMaterializationFullVerifierEvidenceGate {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)]$Bundle)
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $childHost = Get-Task6CaptureMaterializationFullVerifierChildHost
    $verificationRoot = Join-Path $RedRoot ('verify-' + $Name)
    $log = Join-Path $RedRoot ($Name + '.verify.log')
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify -ProjectRoot $ProjectRoot `
            -CaptureEvidenceOnly -CaptureRoot $Bundle.Root -VerificationRoot $verificationRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($log, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n",
        [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ Name = $Name; ExitCode = $exitCode; Output = @($output | ForEach-Object { $_.ToString() }); Log = $log }
}

Describe 'Task 6 full verifier capture-materialization consumption' {
    It 'requires the full verifier to reject rebinding materialization builder, semantic, and root evidence' {
        Assert-Task6CaptureMaterializationFullVerifier (-not (Test-Path -LiteralPath $RedRoot)) 'full verifier RED root starts fresh'
        $projectFull = ([IO.Path]::GetFullPath($ProjectRoot)).TrimEnd('\')
        $redFull = ([IO.Path]::GetFullPath($RedRoot)).TrimEnd('\')
        Assert-Task6CaptureMaterializationFullVerifier (-not $redFull.StartsWith(($projectFull + '\'), [StringComparison]::OrdinalIgnoreCase)) `
            'full verifier RED evidence is external to the candidate'
        New-Item -ItemType Directory -Path $RedRoot -Force | Out-Null
        $candidateHash = Get-Task6CaptureMaterializationFullVerifierCandidateFingerprint
        $results = [Collections.Generic.List[object]]::new()

        $canonical = New-Task6CaptureMaterializationFullVerifierBundle -Name 'canonical' -CandidateHash $candidateHash
        $results.Add((Invoke-Task6CaptureMaterializationFullVerifierEvidenceGate -Name 'canonical' -Bundle $canonical))

        $builderOrder = New-Task6CaptureMaterializationFullVerifierBundle -Name 'builder-order' -CandidateHash $candidateHash
        $first = 'builder_01=BuildCanyonEntrance|script=res://tools/build/prologue/BuildCanyonEntrance.cs'
        $second = 'builder_02=BuildCanyonDescent|script=res://tools/build/prologue/BuildCanyonDescent.cs'
        $builderLines = @(Get-Content -LiteralPath $builderOrder.MaterializationManifest)
        Assert-Task6CaptureMaterializationFullVerifier ((@($builderLines | Where-Object { $_ -ceq $first }).Count) -eq 1) `
            'builder-order fixture has first canonical descriptor'
        Assert-Task6CaptureMaterializationFullVerifier ((@($builderLines | Where-Object { $_ -ceq $second }).Count) -eq 1) `
            'builder-order fixture has second canonical descriptor'
        $swapped = @($builderLines | ForEach-Object {
                if ($_ -ceq $first) { $second }
                elseif ($_ -ceq $second) { $first }
                else { $_ }
            })
        [IO.File]::WriteAllLines($builderOrder.MaterializationManifest, $swapped, [Text.UTF8Encoding]::new($false))
        Rebind-Task6CaptureMaterializationFullVerifierOuterManifest -Bundle $builderOrder `
            -ManifestPath $builderOrder.MaterializationManifest
        $results.Add((Invoke-Task6CaptureMaterializationFullVerifierEvidenceGate -Name 'builder-order' -Bundle $builderOrder))

        $semanticSha = New-Task6CaptureMaterializationFullVerifierBundle -Name 'semantic-sha' -CandidateHash $candidateHash
        Set-Task6CaptureMaterializationFullVerifierManifestValue -Path $semanticSha.MaterializationManifest `
            -Name 'generated_runtime_semantics_sha256' -Value ('0' * 64)
        Rebind-Task6CaptureMaterializationFullVerifierOuterManifest -Bundle $semanticSha `
            -ManifestPath $semanticSha.MaterializationManifest
        $results.Add((Invoke-Task6CaptureMaterializationFullVerifierEvidenceGate -Name 'semantic-sha' -Bundle $semanticSha))

        $outside = New-Task6CaptureMaterializationFullVerifierBundle -Name 'manifest-outside-capture-root' -CandidateHash $candidateHash
        $outsideManifest = Join-Path $RedRoot 'outside-materialization.manifest.txt'
        Copy-Item -LiteralPath $outside.MaterializationManifest -Destination $outsideManifest -Force
        Rebind-Task6CaptureMaterializationFullVerifierOuterManifest -Bundle $outside -ManifestPath $outsideManifest
        $results.Add((Invoke-Task6CaptureMaterializationFullVerifierEvidenceGate -Name 'manifest-outside-capture-root' -Bundle $outside))

        $summaryLines = [Collections.Generic.List[string]]::new()
        $summaryLines.Add('schema=task6-capture-materialization-full-verifier-v1')
        foreach ($result in $results) {
            $summaryLines.Add('case=' + $result.Name + '|child_exit=' + $result.ExitCode + '|log=' + $result.Log)
        }
        $summary = Join-Path $RedRoot 'task6-capture-materialization-full-verifier-summary.txt'
        [IO.File]::WriteAllLines($summary, $summaryLines.ToArray(), [Text.UTF8Encoding]::new($false))
        if (-not [string]::IsNullOrWhiteSpace($EvidenceLog)) {
            [IO.File]::WriteAllLines($EvidenceLog, $summaryLines.ToArray(), [Text.UTF8Encoding]::new($false))
        }

        $canonicalResult = @($results | Where-Object { $_.Name -ceq 'canonical' })[0]
        Assert-Task6CaptureMaterializationFullVerifier ($canonicalResult.ExitCode -eq 0) (
            'canonical full capture fixture passes current full verifier: ' + ($canonicalResult.Output -join ' | '))
        $canonicalMarkers = @($canonicalResult.Output | Where-Object { $_ -match '^PROLOGUE_CAPTURE_EVIDENCE_OK ' })
        Assert-Task6CaptureMaterializationFullVerifier ($canonicalMarkers.Count -eq 1) `
            'canonical full verifier fixture emits one capture-evidence terminal'

        $accepted = @($results | Where-Object { $_.Name -ne 'canonical' -and $_.ExitCode -eq 0 } |
            ForEach-Object { $_.Name })
        Assert-Task6CaptureMaterializationFullVerifier ($accepted.Count -eq 0) (
            'full verifier accepted rebinding mutation(s): ' + ($accepted -join ','))
    }
}
