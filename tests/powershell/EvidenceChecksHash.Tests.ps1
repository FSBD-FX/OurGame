[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
. (Join-Path $project 'tools\EvidenceChecks.ps1')

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "Assertion failed: $Message" }
}

function Get-NativeSha256([string]$LiteralPath) {
    return (Microsoft.PowerShell.Utility\Get-FileHash -LiteralPath $LiteralPath -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-Sha256OfUtf8([string]$Text) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($algorithm.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant()
    }
    finally { $algorithm.Dispose() }
}

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("evidence-checks-hash-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
$priorLocation = Get-Location
try {
    $hashHelper = Get-Command Get-EvidenceSha256 -ErrorAction SilentlyContinue
    Assert-True ($null -ne $hashHelper) 'EvidenceChecks exposes Get-EvidenceSha256'

    $emptyPath = Join-Path $fixtureRoot 'empty.bin'
    $abcPath = Join-Path $fixtureRoot 'abc.bin'
    [System.IO.File]::WriteAllBytes($emptyPath, [byte[]]::new(0))
    [System.IO.File]::WriteAllBytes($abcPath, [System.Text.Encoding]::ASCII.GetBytes('abc'))

    Set-Location -LiteralPath $fixtureRoot
    $relativeAbcHash = Get-EvidenceSha256 -LiteralPath '.\abc.bin'
    $absoluteEmptyHash = Get-EvidenceSha256 -LiteralPath $emptyPath
    Assert-True ($relativeAbcHash -ceq 'ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad') `
        'relative literal abc bytes have the hand-derived SHA-256'
    Assert-True ($absoluteEmptyHash -ceq 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855') `
        'absolute literal empty bytes have the hand-derived SHA-256'
    Assert-True ($relativeAbcHash -ceq (Get-NativeSha256 -LiteralPath '.\abc.bin')) `
        'relative literal helper hash matches native Get-FileHash semantics'
    Assert-True ($absoluteEmptyHash -ceq (Get-NativeSha256 -LiteralPath $emptyPath)) `
        'absolute literal helper hash matches native Get-FileHash semantics'

    Add-Type -AssemblyName System.Drawing
    $firstPng = Join-Path $fixtureRoot 'manifest-a.png'
    $secondPng = Join-Path $fixtureRoot 'manifest-b.png'
    foreach ($specification in @(
        @{ Path = $firstPng; Color = [System.Drawing.Color]::DarkSlateBlue },
        @{ Path = $secondPng; Color = [System.Drawing.Color]::DarkOliveGreen }
    )) {
        $bitmap = [System.Drawing.Bitmap]::new(4, 3)
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try { $graphics.Clear($specification.Color) }
            finally { $graphics.Dispose() }
            $bitmap.Save($specification.Path, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally { $bitmap.Dispose() }
    }
    $pngPaths = @($firstPng, $secondPng)
    $nativeManifestLines = @($pngPaths | Sort-Object | ForEach-Object {
        "$(Split-Path -Leaf $_)=$(Get-NativeSha256 -LiteralPath $_)"
    })
    $nativeManifestSha256 = Get-Sha256OfUtf8 -Text ($nativeManifestLines -join "`n")
    $contract = Assert-PngCaptureContract -Paths $pngPaths -ExpectedCount 2 `
        -ExpectedWidth 4 -ExpectedHeight 3 -Label 'deterministic hash manifest'
    Assert-True ($contract.ManifestSha256 -ceq $nativeManifestSha256) `
        'capture manifest SHA-256 exactly matches pre-change native Get-FileHash semantics'
    Assert-True ($contract.ComparisonManifestSha256 -ceq $nativeManifestSha256) `
        'un-normalized comparison manifest preserves pre-change native semantics'

    Write-Output 'EVIDENCE_CHECKS_HASH_OK'
}
finally {
    Set-Location -LiteralPath $priorLocation
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
