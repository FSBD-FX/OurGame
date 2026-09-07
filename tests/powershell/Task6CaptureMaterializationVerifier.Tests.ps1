[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$MaterializationCaptureRoot,
    [Parameter(Mandatory)][string]$RedRoot,
    [string]$EvidenceLog
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$MaterializationCaptureRoot = (Resolve-Path -LiteralPath $MaterializationCaptureRoot).Path
. (Join-Path $ProjectRoot 'tools\EvidenceChecks.ps1')

function Assert-Task6CaptureMaterializationVerifier {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "Task6 capture-materialization verifier assertion failed: $Message" }
}

function Get-Task6CaptureMaterializationChildHost {
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
    throw 'Task6 capture-materialization verifier cannot resolve a child PowerShell host.'
}

function Set-Task6CaptureMaterializationManifestValue {
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
    if ($seen -ne 1) {
        throw "Task6 capture-materialization manifest must have exactly one $Name entry; actual=$seen"
    }
    [IO.File]::WriteAllLines($Path, $rewritten.ToArray(), [Text.UTF8Encoding]::new($false))
}

function Copy-Task6CaptureMaterializationFixture {
    param([Parameter(Mandatory)][string]$Name)

    $root = Join-Path $RedRoot $Name
    Assert-Task6CaptureMaterializationVerifier (-not (Test-Path -LiteralPath $root)) "fresh materialization fixture root: $Name"
    $sourceManifest = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization.manifest.txt'
    $sourceDirectory = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization'
    Assert-Task6CaptureMaterializationVerifier (Test-Path -LiteralPath $sourceManifest -PathType Leaf) 'source materialization manifest exists'
    Assert-Task6CaptureMaterializationVerifier (Test-Path -LiteralPath $sourceDirectory -PathType Container) 'source materialization directory exists'
    New-Item -ItemType Directory -Path $root -Force | Out-Null
    Copy-Item -LiteralPath $sourceDirectory -Destination $root -Recurse -Force
    $manifest = Join-Path $root 'task6-capture-materialization.manifest.txt'
    Copy-Item -LiteralPath $sourceManifest -Destination $manifest -Force
    $sourceTerminalLog = Join-Path $MaterializationCaptureRoot 'task6-capture-materialization.log'
    Assert-Task6CaptureMaterializationVerifier (Test-Path -LiteralPath $sourceTerminalLog -PathType Leaf) `
        'source materialization terminal log exists'
    Copy-Item -LiteralPath $sourceTerminalLog -Destination (Join-Path $root 'task6-capture-materialization.log') -Force

    # Rebase actual import/builder evidence into this isolated capture root. The mode under test
    # must reject a later attempt to point the manifest outside this root.
    $sourceRoot = ([IO.Path]::GetFullPath($MaterializationCaptureRoot)).TrimEnd('\')
    $destinationRoot = ([IO.Path]::GetFullPath($root)).TrimEnd('\')
    $text = [IO.File]::ReadAllText($manifest)
    $rewritten = $text.Replace($sourceRoot, $destinationRoot)
    Assert-Task6CaptureMaterializationVerifier ($rewritten -cne $text) 'fixture rebases nested real-evidence paths'
    [IO.File]::WriteAllText($manifest, $rewritten, [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ Root = $root; Manifest = $manifest }
}

function Invoke-Task6CaptureMaterializationManifestGate {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][string]$CaptureRoot, [Parameter(Mandatory)][string]$Manifest)
    $verificationRoot = Join-Path $RedRoot ('verify-' + $Name)
    $outputLog = Join-Path $RedRoot ($Name + '.verify.log')
    $verify = Join-Path $ProjectRoot 'tools\verify-prologue.ps1'
    $childHost = Get-Task6CaptureMaterializationChildHost
    $prior = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $childHost -NoProfile -ExecutionPolicy Bypass -File $verify -ProjectRoot $ProjectRoot `
            -CaptureMaterializationManifestOnly -CaptureMaterializationManifest $Manifest `
            -CaptureRoot $CaptureRoot -VerificationRoot $verificationRoot 2>&1
        $exitCode = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prior }
    [IO.File]::WriteAllText($outputLog, (($output | ForEach-Object { $_.ToString() }) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    return [pscustomobject]@{ Name = $Name; ExitCode = $exitCode; Output = @($output | ForEach-Object { $_.ToString() }); Log = $outputLog }
}

Describe 'Task 6 capture materialization verifier consumer' {
    It 'requires an in-root six-builder semantic manifest through real child verifier calls' {
        Assert-Task6CaptureMaterializationVerifier (-not (Test-Path -LiteralPath $RedRoot)) 'materialization consumer RED root starts fresh'
        $projectFull = ([IO.Path]::GetFullPath($ProjectRoot)).TrimEnd('\')
        $redFull = ([IO.Path]::GetFullPath($RedRoot)).TrimEnd('\')
        Assert-Task6CaptureMaterializationVerifier (-not $redFull.StartsWith(($projectFull + '\'), [StringComparison]::OrdinalIgnoreCase)) `
            'materialization consumer RED evidence is external to the candidate'
        New-Item -ItemType Directory -Path $RedRoot -Force | Out-Null
        $results = [Collections.Generic.List[object]]::new()

        $canonical = Copy-Task6CaptureMaterializationFixture -Name 'canonical'
        $results.Add((Invoke-Task6CaptureMaterializationManifestGate -Name 'canonical' -CaptureRoot $canonical.Root -Manifest $canonical.Manifest))

        $builderOrder = Copy-Task6CaptureMaterializationFixture -Name 'builder-order'
        $first = 'builder_01=BuildCanyonEntrance|script=res://tools/build/prologue/BuildCanyonEntrance.cs'
        $second = 'builder_02=BuildCanyonDescent|script=res://tools/build/prologue/BuildCanyonDescent.cs'
        $lines = @(Get-Content -LiteralPath $builderOrder.Manifest)
        Assert-Task6CaptureMaterializationVerifier ((@($lines | Where-Object { $_ -ceq $first }).Count) -eq 1) 'builder-order fixture has first canonical builder'
        Assert-Task6CaptureMaterializationVerifier ((@($lines | Where-Object { $_ -ceq $second }).Count) -eq 1) 'builder-order fixture has second canonical builder'
        $swapped = @($lines | ForEach-Object {
                if ($_ -ceq $first) { $second }
                elseif ($_ -ceq $second) { $first }
                else { $_ }
            })
        [IO.File]::WriteAllLines($builderOrder.Manifest, $swapped, [Text.UTF8Encoding]::new($false))
        $results.Add((Invoke-Task6CaptureMaterializationManifestGate -Name 'builder-order' -CaptureRoot $builderOrder.Root -Manifest $builderOrder.Manifest))

        $semanticSha = Copy-Task6CaptureMaterializationFixture -Name 'semantic-sha'
        Set-Task6CaptureMaterializationManifestValue -Path $semanticSha.Manifest -Name 'generated_runtime_semantics_sha256' -Value ('0' * 64)
        $results.Add((Invoke-Task6CaptureMaterializationManifestGate -Name 'semantic-sha' -CaptureRoot $semanticSha.Root -Manifest $semanticSha.Manifest))

        $outside = Copy-Task6CaptureMaterializationFixture -Name 'manifest-path'
        $outsideManifest = Join-Path $RedRoot 'outside-materialization.manifest.txt'
        Copy-Item -LiteralPath $outside.Manifest -Destination $outsideManifest -Force
        $results.Add((Invoke-Task6CaptureMaterializationManifestGate -Name 'manifest-path' -CaptureRoot $outside.Root -Manifest $outsideManifest))

        $expected = @{
            'builder-order' = 'capture_materialization_builder_order_required'
            'semantic-sha' = 'capture_materialization_semantic_sha_mismatch'
            'manifest-path' = 'capture_materialization_manifest_outside_capture_root'
        }
        $summary = Join-Path $RedRoot 'task6-capture-materialization-consumer-summary.txt'
        $summaryLines = [Collections.Generic.List[string]]::new()
        $summaryLines.Add('schema=task6-capture-materialization-consumer-v1')
        foreach ($result in $results) {
            $expectedLabel = if ($expected.ContainsKey($result.Name)) { $expected[$result.Name] } else { 'canonical_acceptance' }
            $summaryLines.Add('case=' + $result.Name + '|child_exit=' + $result.ExitCode + '|expected=' + $expectedLabel + '|log=' + $result.Log)
        }
        [IO.File]::WriteAllLines($summary, $summaryLines.ToArray(), [Text.UTF8Encoding]::new($false))
        if (-not [string]::IsNullOrWhiteSpace($EvidenceLog)) {
            [IO.File]::WriteAllLines($EvidenceLog, $summaryLines.ToArray(), [Text.UTF8Encoding]::new($false))
        }

        $canonicalResult = @($results | Where-Object { $_.Name -ceq 'canonical' })[0]
        $errors = [Collections.Generic.List[string]]::new()
        if ($canonicalResult.ExitCode -ne 0) {
            $errors.Add('canonical child gate exit=' + $canonicalResult.ExitCode + ' output=' + ($canonicalResult.Output -join ' | '))
        }
        else {
            $canonicalMarkers = @($canonicalResult.Output | Where-Object { $_ -match '^TASK6_CAPTURE_MATERIALIZATION_MANIFEST_OK ' })
            if ($canonicalMarkers.Count -ne 1) { $errors.Add('canonical terminal marker missing_or_duplicate') }
        }
        foreach ($result in @($results | Where-Object { $_.Name -ne 'canonical' })) {
            $text = $result.Output -join "`n"
            if ($result.ExitCode -eq 0) {
                $errors.Add('mutation accepted=' + $result.Name)
            }
            elseif ($text -notmatch [regex]::Escape($expected[$result.Name])) {
                $errors.Add('mutation wrong_label=' + $result.Name + ' expected=' + $expected[$result.Name])
            }
        }
        Assert-Task6CaptureMaterializationVerifier ($errors.Count -eq 0) ($errors -join '; ')
    }
}
