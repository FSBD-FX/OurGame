param([Parameter(Mandatory=$true)][string]$ProofCase)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$evidenceRoot='E:/OurGame/Knight/.evidence/october10-keyboard-collection-fog'
$archiveRoot='E:/OurGame/Knight/过程图稿与视频/2026-10-10-键盘操作-v001-局部实跑'
if($ProofCase -notmatch '^keyboard-[0-9]{8}-[0-9]{6}-[0-9]{3}-[0-9a-f]{32}$'){throw 'Invalid proof case name'}
if(Test-Path -LiteralPath $archiveRoot){throw 'Prior archive exists; never overwrite'}
$proofRoot=Join-Path $evidenceRoot $ProofCase
if(!(Test-Path -LiteralPath (Join-Path $proofRoot 'keyboard-original.avi'))){throw 'Missing original movie'}
foreach($folder in @('original','presentation','frames','source')) {
    $null=New-Item -ItemType Directory -Path (Join-Path $archiveRoot $folder)
}
Copy-Item -LiteralPath $evidenceRoot -Destination (Join-Path $archiveRoot 'original/evidence') -Recurse
$regressions=@('prototype-20261010-092330-493-0e7d4b6235194f37b966e621e5dd5834',
    'fall-20261010-093146-424-a72577473c004e19be487ad34aa3a11c',
    'release-20261010-093146-425-bb7b03875c1c4db2a3d64fcac213eeb2',
    'auto-pull-20261010-093146-424-995aa63a1a1544588e194f51674eea56')
$null=New-Item -ItemType Directory -Path (Join-Path $archiveRoot 'original/regressions')
foreach($case in $regressions) {Copy-Item -LiteralPath (Join-Path 'E:/OurGame/Knight/.evidence/october07-fall-damage' $case) `
    -Destination (Join-Path $archiveRoot "original/regressions/$case") -Recurse}
$sourceFiles=@('project.godot','tools/configure/ConfigureProject.cs','tools/build/BuildPlayer.cs',
    'scenes/player/Player.tscn','scripts/shared/InputActions.cs','scripts/player/PlayerInputFrame.cs',
    'scripts/player/PlayerActionCommandModel.cs','scripts/player/PlayerKeyboardAimModel.cs',
    'scripts/player/PlayerInputReader.cs','scripts/player/PlayerActionModeController.cs',
    'scripts/player/PlayerShieldController.cs','scripts/player/PlayerCombat.cs',
    'scripts/abilities/MechanicalClawController.cs','scripts/abilities/MechanicalClawTargetResolver.cs',
    'scripts/ui/PrologueFormalHud.cs','scripts/interactions/IEnvironmentInteractable.cs',
    'scripts/interactions/EnvironmentInteractionModel.cs','scripts/interactions/EnvironmentInteractionController.cs',
    'tests/RunKeyboardControlsTests.cs','tests/unit/PlayerActionCommandModelTests.cs',
    'tests/unit/PlayerKeyboardAimModelTests.cs','tests/unit/EnvironmentInteractionModelTests.cs',
    'tests/unit/ProjectFoundationTests.cs','tests/integration/KeyboardActionRuntimeTests.cs',
    'tests/integration/KeyboardClawTargetTests.cs','tests/integration/KeyboardClawRuntimeTests.cs',
    'tests/integration/EnvironmentInteractionRuntimeTests.cs','tests/presentation/KeyboardControlsProof.cs',
    '.superpowers/sdd/2026-10-07-keyboard-collection-fog/verify-feature.ps1',
    '.superpowers/sdd/2026-10-07-keyboard-controls/progress.md','README.md','tools/archive-keyboard-proof.ps1')
$recorded=@{}
foreach($line in Get-Content -LiteralPath (Join-Path $proofRoot 'source-before.txt')) {
    $pair=$line.Split('|'); $recorded[$pair[0].Replace('\','/')]= $pair[1]
}
foreach($relative in $sourceFiles) {
    $source=Join-Path $projectRoot $relative
    if($recorded.ContainsKey($relative) -and (Get-FileHash -LiteralPath $source).Hash -cne $recorded[$relative]) {
        throw "Source changed since recording: $relative"
    }
    $destination=Join-Path $archiveRoot "source/$relative"
    $null=New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force
    Copy-Item -LiteralPath $source -Destination $destination
}
$movie=Join-Path $archiveRoot "original/evidence/$ProofCase/keyboard-original.avi"
$preview=Join-Path $archiveRoot 'presentation/keyboard-v001.mp4'
$ffmpeg='G:/小丸工具箱/tools/ffmpeg.exe'
& $ffmpeg -nostdin -i $movie -c:v libx264 -crf 18 -pix_fmt yuv420p -an -movflags +faststart $preview 2> (Join-Path $archiveRoot 'presentation/transcode.log')
if($LASTEXITCODE -ne 0){throw 'Transcode failed'}
& $ffmpeg -nostdin -i $preview -vf 'fps=4' (Join-Path $archiveRoot 'frames/frame-%03d.png') 2> (Join-Path $archiveRoot 'presentation/frames.log')
if($LASTEXITCODE -ne 0){throw 'Frame extraction failed'}
& $ffmpeg -nostdin -i $preview -f null - 2> (Join-Path $archiveRoot 'presentation/decode.log')
if($LASTEXITCODE -ne 0){throw 'Full decode failed'}
$items=@(Get-ChildItem -LiteralPath $archiveRoot -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative=$_.FullName.Substring($archiveRoot.Length+1).Replace('\','/')
    $source=$null
    if($relative.StartsWith('original/evidence/')){$source=Join-Path $evidenceRoot $relative.Substring(18)}
    elseif($relative.StartsWith('original/regressions/')){$source=Join-Path 'E:/OurGame/Knight/.evidence/october07-fall-damage' $relative.Substring(21)}
    elseif($relative.StartsWith('source/')){$source=Join-Path $projectRoot $relative.Substring(7)}
    $hash=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    if($source -and (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -cne $hash){throw "Copy mismatch: $relative"}
    [pscustomobject]@{Path=$relative;Bytes=$_.Length;SHA256=$hash;Source=$source;
        Version='keyboard-v001';Status='isolated fixed-60 proof, not full-level/final-art/human/realtime acceptance';Verified=$true}
})
$items | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $archiveRoot 'index.json') -Encoding utf8
Write-Output "ARCHIVE=$archiveRoot"
Write-Output "INDEX_ITEMS=$($items.Count)"
Write-Output "INDEX_BYTES=$(($items | Measure-Object -Property Bytes -Sum).Sum)"
Write-Output "PREVIEW=$preview"
