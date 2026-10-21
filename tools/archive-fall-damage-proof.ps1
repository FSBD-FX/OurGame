param(
    [Parameter(Mandatory=$true)][ValidateSet('v001','v002')][string]$Version,
    [Parameter(Mandatory=$true)][string]$ProofCase
)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sourceRoot='E:/OurGame/Knight/.evidence/october07-fall-damage'
$targetRoot="E:/OurGame/Knight/过程图稿与视频/2026-10-07-摔伤机制-$Version-局部实跑"
if($ProofCase -notmatch '^proof-[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-f]{32}$') {
    throw 'ProofCase must be an existing isolated proof case directory name'
}
if(Test-Path -LiteralPath $targetRoot){throw 'Prior proof version exists; never overwrite it'}
foreach($folder in @('original','presentation','frames','source')) {
    $null=New-Item -ItemType Directory -Path (Join-Path $targetRoot $folder)
}
Copy-Item -LiteralPath $sourceRoot -Destination (Join-Path $targetRoot 'original/evidence') -Recurse
$sourceFiles=@('scripts/player/PlayerMotor.cs','scripts/player/PlayerFallDamageModel.cs',
    'scripts/player/PlayerCombat.cs','scripts/config/PlayerTuning.cs',
    'scripts/abilities/MechanicalClawController.cs','scripts/prologue/PrologueLevelController.cs',
    'scripts/prologue/seamless/SeamlessPrologueWorld.cs','tools/build/prologue/CanyonContentBuilder.cs',
    'scenes/prologue/sectors/CanyonDescent.tscn','tests/RunFallDamageTests.cs',
    'tests/unit/PlayerFallDamageModelTests.cs','tests/integration/PlayerFallDamageRuntimeTests.cs',
    'tests/presentation/PlayerFallDamageProof.cs','tools/verify-fall-damage.ps1')
foreach($relative in $sourceFiles) {
    $destination=Join-Path (Join-Path $targetRoot 'source') $relative
    $null=New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
    Copy-Item -LiteralPath (Join-Path $projectRoot $relative) -Destination $destination
    if((Get-FileHash -LiteralPath $destination).Hash -cne
       (Get-FileHash -LiteralPath (Join-Path $projectRoot $relative)).Hash){throw "Source copy mismatch: $relative"}
}
$movie=Join-Path $targetRoot "original/evidence/$ProofCase/fall-damage-original.avi"
$preview=Join-Path $targetRoot "presentation/fall-damage-$Version.mp4"
$ffmpeg='G:/小丸工具箱/tools/ffmpeg.exe'
& $ffmpeg -nostdin -i $movie -c:v libx264 -crf 18 -pix_fmt yuv420p -an -movflags +faststart $preview 2> (Join-Path $targetRoot 'presentation/transcode.log')
if($LASTEXITCODE -ne 0){throw 'Proof preview transcode failed'}
& $ffmpeg -nostdin -i $preview -vf 'fps=4' (Join-Path $targetRoot 'frames/frame-%03d.png') 2> (Join-Path $targetRoot 'presentation/frames.log')
if($LASTEXITCODE -ne 0){throw 'Proof frame extraction failed'}
& $ffmpeg -nostdin -i $preview -f null - 2> (Join-Path $targetRoot 'presentation/decode.log')
if($LASTEXITCODE -ne 0){throw 'Proof full decode failed'}
$items=@(Get-ChildItem -LiteralPath $targetRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    [pscustomobject]@{
        Path=$_.FullName.Substring($targetRoot.Length+1)
        Bytes=$_.Length
        SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        Status='isolated fixed-60 runtime proof; not full-level or human acceptance'
    }
})
$items | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $targetRoot 'index.json') -Encoding utf8
Write-Output "ARCHIVE=$targetRoot"
Write-Output "INDEX_ITEMS=$($items.Count)"
Write-Output "PREVIEW=$preview"
