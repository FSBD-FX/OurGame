param([Parameter(Mandatory)][string]$NativeCase,[string]$PublishedMap,[Parameter(Mandatory)][string]$ReceiptName,
    [switch]$ValidationOnlyFixtureNewVolume)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$renderer=Join-Path $PSScriptRoot 'render-current-prologue-map.ps1'
$runtime='C:/Users/22499/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
$source=Join-Path $NativeCase 'storage/current-prologue-map.json'
$nativeRoot=[IO.Path]::GetFullPath($NativeCase)
if(!$nativeRoot.StartsWith('G:\knight\验证证据\',[StringComparison]::OrdinalIgnoreCase)-or$ReceiptName-cnotmatch'^[a-z0-9-]+$'){throw 'Scoped native artifact and fresh receipt name required'}
$fixture=Join-Path $nativeRoot ('storage/'+$ReceiptName)
if(Test-Path -LiteralPath $fixture){throw 'Do not overwrite a prior review fixture'}
if(!$ValidationOnlyFixtureNewVolume-and!$PublishedMap){throw 'PublishedMap is required for publication review controls'}
[void](New-Item -ItemType Directory -Path $fixture)
if($ValidationOnlyFixtureNewVolume){
    # Validation-only fixtures exercise the real -SelfTest boundary. They never create
    # native success provenance, render media, or qualify their source for publication.
    $rows=[Collections.Generic.List[object]]::new()
    function RunValidation([string]$name,[string]$path,[bool]$wantAccepted){
        $receiptPath=Join-Path ([IO.Path]::GetDirectoryName($path)) ($name+'-selftest.json')
        $start=[Diagnostics.ProcessStartInfo]::new($runtime)
        $start.UseShellExecute=$false;$start.CreateNoWindow=$true
        $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
        foreach($argument in @('-NoProfile','-File',$renderer,'-InputJson',$path,'-SelfTest','-SelfTestReceipt',$receiptPath)){
            [void]$start.ArgumentList.Add($argument)
        }
        $process=[Diagnostics.Process]::Start($start)
        $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
        if(!$process.WaitForExit(60000)){$process.Kill();$process.WaitForExit();throw 'Owned validation-only renderer timed out'}
        $exitCode=$process.ExitCode;$outText=$stdout.GetAwaiter().GetResult();$errText=$stderr.GetAwaiter().GetResult();$process.Dispose()
        $accepted=$exitCode-eq0-and(Test-Path -LiteralPath $receiptPath)
        $rows.Add([pscustomobject]@{name=$name;wantAccepted=$wantAccepted;accepted=$accepted;
            passed=($accepted-eq$wantAccepted)-and($wantAccepted-or!(Test-Path -LiteralPath $receiptPath));
            exit=$exitCode;stdout=$outText;stderr=$errText;path=$path})
    }
    $baseMap=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
    $hasVolume=$null-ne$baseMap.PSObject.Properties['configuredHazardVolumes']
    if(!$hasVolume){
        $incompletePath=Join-Path $fixture 'failed-native-158-missing-volume-validation-only.json'
        [IO.File]::WriteAllBytes($incompletePath,[IO.File]::ReadAllBytes($source))
        RunValidation 'actual-failed-158-input-rejected' $incompletePath $false
        $volume=[pscustomobject]@{id='CanyonBasin/Gameplay/Mechanisms/IncineratorPool';objectId='CanyonBasin/Gameplay/Mechanisms/IncineratorPool';
            type='CanyonIncinerator';sector='CanyonBasin';path='Gameplay/Mechanisms/IncineratorPool';
            corners=@(@(5440,4020),@(6160,4020),@(6160,4296),@(5440,4296));pulseSeconds=0.1;
            damagePolicy='max-health-contact-attempt';unblockable=$true;respectsHealthInvulnerability=$true;
            status='configured-volume-not-live-activation'}
        $baseMap | Add-Member -NotePropertyName configuredHazardVolumes -NotePropertyValue @($volume)
        $hazardSource='res://scripts/mechanisms/CanyonIncinerator.cs'
        if(@($baseMap.sources | Where-Object path -CEQ $hazardSource).Count-eq0){
            $baseMap.sources+= [pscustomobject]@{path=$hazardSource;sha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $PSScriptRoot) 'scripts/mechanisms/CanyonIncinerator.cs') -Algorithm SHA256).Hash}
        }
    }
    if($null-eq$baseMap.PSObject.Properties['configuredMovingGears']){
        # Keep this older volume-only regression runnable after the current rail
        # contract expands. This is still a named validation fixture, never native provenance.
        $gearId='CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear'
        $baseMap.objects+=[pscustomobject]@{id=$gearId;type='CanyonMovingGear';sector='CanyonBasin';path='Gameplay/Mechanisms/UpperGalleryMovingGear';
            point=@(3400,3550);status='saved-anchor-not-dynamic-danger-envelope'}
        $baseMap | Add-Member -NotePropertyName configuredMovingGears -NotePropertyValue @([pscustomobject]@{
            id=$gearId;objectId=$gearId;type='CanyonMovingGear';sector='CanyonBasin';path='Gameplay/Mechanisms/UpperGalleryMovingGear';
            start=@(3400,3550);end=@(3400,3630);radius=24;periodSeconds=3.2;damage=2;contactIntervalSeconds=0.5;
            envelope=@(3376,3526,48,128);envelopePolicy='conservative-aabb-not-damage-shape';status='configured-rail-not-runtime-route-proof'
        })
        foreach($relative in @('scripts/mechanisms/CanyonMovingGear.cs','scripts/mechanisms/MovingGearMotionModel.cs')){
            $baseMap.sources+=[pscustomobject]@{path='res://'+$relative;sha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $PSScriptRoot) $relative) -Algorithm SHA256).Hash}
        }
    }
    $cases=@(
        @{name='newvolume-positive-validation-only';change={param($x)};accepted=$true},
        @{name='newvolume-missing';change={param($x) $x.PSObject.Properties.Remove('configuredHazardVolumes')};accepted=$false},
        @{name='newvolume-duplicate';change={param($x) $x.configuredHazardVolumes+= $x.configuredHazardVolumes[0]};accepted=$false},
        @{name='newvolume-wrong-id';change={param($x) $x.configuredHazardVolumes[0].id='wrong-id'};accepted=$false},
        @{name='newvolume-wrong-corners';change={param($x) $x.configuredHazardVolumes[0].corners[0][0]=5439};accepted=$false},
        @{name='newvolume-nonfinite-corner';change={param($x) $x.configuredHazardVolumes[0].corners[0][0]='NaN'};accepted=$false},
        @{name='newvolume-false-live-acceptance';change={param($x) $x.configuredHazardVolumes[0].status='live-activation-accepted'};accepted=$false},
        @{name='newvolume-wrong-linkage';change={param($x) $x.configuredHazardVolumes[0].objectId=$x.objects[0].id};accepted=$false},
        @{name='newvolume-missing-object';change={param($x) $x.objects=@($x.objects | Where-Object type -CNE 'CanyonIncinerator')};accepted=$false},
        @{name='newvolume-wrong-object-type';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonIncinerator').type='CanyonSteamJet'};accepted=$false},
        @{name='newvolume-wrong-sector';change={param($x) $x.configuredHazardVolumes[0].sector='CanyonAscent'};accepted=$false},
        @{name='newvolume-wrong-path';change={param($x) $x.configuredHazardVolumes[0].path='Gameplay/Mechanisms/Other'};accepted=$false},
        @{name='newvolume-wrong-pulse';change={param($x) $x.configuredHazardVolumes[0].pulseSeconds=0.2};accepted=$false},
        @{name='newvolume-non-numeric-pulse';change={param($x) $x.configuredHazardVolumes[0].pulseSeconds='0.1'};accepted=$false},
        @{name='newvolume-wrong-damage-policy';change={param($x) $x.configuredHazardVolumes[0].damagePolicy='instant-death'};accepted=$false},
        @{name='newvolume-blockable';change={param($x) $x.configuredHazardVolumes[0].unblockable=$false};accepted=$false},
        @{name='newvolume-ignores-health-invulnerability';change={param($x) $x.configuredHazardVolumes[0].respectsHealthInvulnerability=$false};accepted=$false},
        @{name='newvolume-missing-source-binding';change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/CanyonIncinerator.cs')};accepted=$false},
        @{name='newvolume-missing-region';change={param($x) $x.regions=@($x.regions | Where-Object id -CNE 'IncineratorAnnex')};accepted=$false}
    )
    foreach($case in $cases){
        $map=$baseMap | ConvertTo-Json -Depth 100 | ConvertFrom-Json
        & $case.change $map
        $path=Join-Path $fixture ($case.name+'.json')
        [IO.File]::WriteAllText($path,($map | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
        RunValidation $case.name $path $case.accepted
    }
    $result=[ordered]@{kind='newvolume-validation-only-fixtures-not-native';version='v314';sourceNativeQualificationClaimed=$false;
        sourceJson=$source;sourceJsonSha256=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash;
        rendererSha256=(Get-FileHash -LiteralPath $renderer -Algorithm SHA256).Hash;
        controls=$rows.ToArray();passed=(@($rows | Where-Object {!$_.passed}).Count-eq0);nativeBehavior=$false;mediaPublished=$false}
    $receipt=Join-Path $fixture 'newvolume-validation-only-results.json'
    [IO.File]::WriteAllText($receipt,($result | ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
    foreach($row in $rows){Write-Output ('NEWVOLUME_VALIDATION_ONLY name='+$row.name+' passed='+$row.passed)}
    Write-Output ('NEWVOLUME_VALIDATION_RECEIPT='+$receipt)
    if(!$result.passed){throw 'Validation-only current volume controls failed'}
    exit 0
}
$target=Join-Path 'G:/knight/Knight/.superpowers/sdd/2026-10-30-current-map-preview' ('receipt-target-'+$ReceiptName)
if(Test-Path -LiteralPath $target){throw 'Do not overwrite an owned junction target'}
[void](New-Item -ItemType Directory -Path $target)
[IO.File]::WriteAllBytes((Join-Path $target 'map.json'),[IO.File]::ReadAllBytes($source))
$junction=Join-Path $fixture 'redirected-source'
[void](New-Item -ItemType Junction -Path $junction -Value $target)
$rows=[Collections.Generic.List[object]]::new()
function InvokeRenderer([string[]]$arguments){
    $start=[Diagnostics.ProcessStartInfo]::new($runtime)
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach($arg in @('-NoProfile','-File',$renderer)+$arguments){[void]$start.ArgumentList.Add($arg)}
    $process=[Diagnostics.Process]::Start($start)
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    if(!$process.WaitForExit(60000)){$owned=$process.Id;$process.Kill();$process.WaitForExit();throw "Owned renderer helper timed out pid=$owned"}
    $result=[pscustomobject]@{exit=$process.ExitCode;stdout=$stdout.GetAwaiter().GetResult();stderr=$stderr.GetAwaiter().GetResult()}
    $process.Dispose();return $result
}
function Record([string]$name,[bool]$passed,$details){$rows.Add([pscustomobject]@{name=$name;passed=$passed;details=$details})}
$escaped=Join-Path $junction 'escaped-receipt.json'
$r=InvokeRenderer @('-InputJson',(Join-Path $junction 'map.json'),'-SelfTest','-SelfTestReceipt',$escaped)
Record 'ancestor-junction-rejected' ($r.exit-ne0-and!(Test-Path -LiteralPath (Join-Path $target 'escaped-receipt.json'))) $r
$existing=Join-Path $NativeCase ('storage/existing-'+$ReceiptName+'.json')
if(Test-Path -LiteralPath $existing){throw 'Receipt fixture already exists'}
[IO.File]::WriteAllText($existing,'preserve-this-exact-file')
$before=(Get-FileHash -LiteralPath $existing).Hash
$r=InvokeRenderer @('-InputJson',$source,'-SelfTest','-SelfTestReceipt',$existing)
Record 'existing-receipt-preserved' ($r.exit-ne0-and(Get-FileHash -LiteralPath $existing).Hash-ceq$before) $r
$before=(Get-FileHash -LiteralPath (Join-Path $PublishedMap 'source-version-status.json')).Hash
$r=InvokeRenderer @('-InputJson',$source,'-OutputDirectory',$PublishedMap)
Record 'existing-output-preserved' ($r.exit-ne0-and(Get-FileHash -LiteralPath (Join-Path $PublishedMap 'source-version-status.json')).Hash-ceq$before) $r
$html=Get-Content -LiteralPath (Join-Path $PublishedMap 'presentation/current-map.html') -Raw
Record 'png-labels-resolve-in-table' ([regex]::IsMatch($html,'(?s)<tr>(?:(?!</tr>).)*<td>E74</td>(?:(?!</tr>).)*canyon\.v2\.resident\.split-level\.02(?:(?!</tr>).)*</tr>')) 'Literal E74 must resolve to the actual lost-echo stable ID in one row'
[xml]$publishedSvg=Get-Content -LiteralPath (Join-Path $PublishedMap 'presentation/current-map.svg') -Raw
$hazard=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-hazard-volume' and @data-map-label='H01']")
$linkedAnchor=$publishedSvg.SelectSingleNode("//*[@data-kind='object' and @data-id='CanyonBasin/Gameplay/Mechanisms/IncineratorPool']")
Record 'h01-links-existing-m18-anchor' ($null-ne$hazard-and$null-ne$linkedAnchor-and
    $hazard.GetAttribute('data-id')-ceq'CanyonBasin/Gameplay/Mechanisms/IncineratorPool'-and
    $hazard.GetAttribute('data-object-id')-ceq$linkedAnchor.GetAttribute('data-id')-and
    $hazard.GetAttribute('data-object-label')-ceq'M18') 'H01 annotates the existing M18 pool object, not an added trap'
Record 'h01-current-volume-projection' ($null-ne$hazard-and$hazard.GetAttribute('points')-ceq'1786.667,1493 2014.667,1493 2014.667,1580.4 1786.667,1580.4') 'Hand-derived projection of [5440,4020]..[6160,4296] in the 3200x1800 map'
Record 'current-composition-exact-counts' ($publishedSvg.SelectNodes("//*[@data-kind='solid']").Count-eq158-and
    $publishedSvg.SelectNodes("//*[@data-kind='region']").Count-eq15-and
    $publishedSvg.SelectNodes("//*[@data-kind='configured-hazard-volume']").Count-eq1-and
    $publishedSvg.SelectNodes("//*[@data-kind='configured-moving-gear']").Count-eq1-and
    $publishedSvg.SelectNodes("//*[@data-kind='configured-arc-gear']").Count-eq1-and
    $publishedSvg.SelectNodes("//*[@data-kind='object']").Count-eq32) '158 structural solids,15 regions,1 hazard annotation,1 rail,1 arc and32 logic anchors are distinct record groups'
Record 'h01-health-policy-table' ([regex]::IsMatch($html,'(?s)<tr>(?:(?!</tr>).)*<td>H01</td><td>M18</td>(?:(?!</tr>).)*<td>0\.1</td><td>max-health-contact-attempt</td><td>True</td><td>True</td><td>configured-volume-not-live-activation</td></tr>')) 'Same rendered H01 table row must expose pulse,damage attempt,unblockable and health-invulnerability policy'
$publishedBinding=Get-Content -LiteralPath (Join-Path $PublishedMap 'source-version-status.json') -Raw | ConvertFrom-Json
Record 'current-index-counts-derived-from-payload' ($publishedBinding.version-ceq'v331'-and$publishedBinding.solids-eq158-and
    $publishedBinding.regions-eq15-and$publishedBinding.configuredHazardVolumes-eq1-and$publishedBinding.configuredMovingGears-eq1-and$publishedBinding.configuredArcGears-eq1-and$publishedBinding.logicalAnchors-eq32-and
    $publishedBinding.traversalSegments-eq21-and$publishedBinding.visualComponents-eq4-and$publishedBinding.sourceRecords-eq57) 'Current counts belong in the source/version/status index; no anchor,volume,rail or arc is promoted to runtime route proof'
$rail=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-moving-gear' and @data-map-label='R01']")
$gearAnchor=$publishedSvg.SelectSingleNode("//*[@data-kind='object' and @data-id='CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear']")
Record 'r01-links-one-moving-gear-anchor' ($null-ne$rail-and$null-ne$gearAnchor-and
    $rail.GetAttribute('data-id')-ceq'CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear'-and
    $rail.GetAttribute('data-object-id')-ceq$gearAnchor.GetAttribute('data-id')-and
    $rail.GetAttribute('data-object-label')-cmatch'^M[0-9]{2}$'-and
    $gearAnchor.InnerText.Contains($rail.GetAttribute('data-object-label'))) 'R01 resolves by stable ID to its own M label, not a hardcoded ordinal'
$centerline=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-rail-centerline']")
$startCircle=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-rail-start']")
$endCircle=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-rail-end']")
Record 'r01-independent-literal-projection' ($null-ne$centerline-and$null-ne$startCircle-and$null-ne$endCircle-and
    $centerline.GetAttribute('x1')-ceq'1140.667'-and$centerline.GetAttribute('y1')-ceq'1344.167'-and
    $centerline.GetAttribute('x2')-ceq'1140.667'-and$centerline.GetAttribute('y2')-ceq'1369.5'-and
    $startCircle.GetAttribute('cx')-ceq'1140.667'-and$startCircle.GetAttribute('cy')-ceq'1344.167'-and$startCircle.GetAttribute('r')-ceq'7.6'-and
    $endCircle.GetAttribute('cx')-ceq'1140.667'-and$endCircle.GetAttribute('cy')-ceq'1369.5'-and$endCircle.GetAttribute('r')-ceq'7.6') 'Hand-derived3200x1800 projection of start[3400,3550],end[3400,3630],radius24'
Record 'moving-gear-independent-point-projection' ($null-ne$gearAnchor-and$gearAnchor.GetAttribute('cx')-ceq'1140.667'-and
    $gearAnchor.GetAttribute('cy')-ceq'1344.167') 'The object anchor independently projects the configured initial center,not its envelope origin'
Record 'r01-no-aabb-damage-shape' ($null-ne$rail-and$rail.SelectNodes('.//*[local-name()="polygon" or local-name()="rect"]').Count-eq0-and
    $rail.SelectNodes('.//*[local-name()="line"]').Count-eq1-and$rail.SelectNodes('.//*[local-name()="circle"]').Count-eq2) 'Only real endpoint circles and their centerline are drawn; the conservative AABB is table-only'
Record 'r01-configured-parameters-table' ([regex]::IsMatch($html,'(?s)<tr>(?:(?!</tr>).)*<td>R01</td><td>M[0-9]{2}</td>(?:(?!</tr>).)*<td>\[3400,3550\]</td><td>\[3400,3630\]</td><td>24</td><td>3\.2</td><td>2</td><td>0\.5</td><td>\[3376,3526,48,128\]</td><td>conservative-aabb-not-damage-shape</td><td>configured-rail-not-runtime-route-proof</td></tr>')) 'One R01 row exposes linkage,all saved parameters and the conservative-not-damage-shape/runtime-proof boundaries'
Record 'r01-source-index-not-live-proof' ($publishedBinding.movingGearStatus-ceq'configured-rail-not-runtime-route-proof'-and
    $publishedBinding.movingGearEnvelopePolicy-ceq'conservative-aabb-not-damage-shape'-and
    $publishedBinding.humanAccepted-is[bool]-and!$publishedBinding.humanAccepted) 'Index cannot promote static rail geometry into live or human acceptance'
$arc=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-arc-gear' and @data-map-label='A01']")
$arcAnchor=$publishedSvg.SelectSingleNode("//*[@data-kind='object' and @data-id='CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear']")
$curve=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-arc-centerline']")
$pivot=$publishedSvg.SelectSingleNode("//*[@data-kind='configured-arc-pivot']")
Record 'a01-links-one-real-arc-anchor' ($null-ne$arc-and$null-ne$arcAnchor-and
    $arc.GetAttribute('data-id')-ceq'CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear'-and
    $arc.GetAttribute('data-object-id')-ceq$arcAnchor.GetAttribute('data-id')-and
    $arc.GetAttribute('data-object-label')-ceq'M20') 'A01 annotates the saved arc sibling M20, not the old R01/M19'
Record 'a01-literal-signed-circular-projection' ($null-ne$curve-and$null-ne$pivot-and
    $curve.GetAttribute('d')-ceq'M1115.333 1472.733 A80.111 80.111 0 0 0 1166 1472.733'-and
    $pivot.GetAttribute('cx')-ceq'1140.667'-and$pivot.GetAttribute('cy')-ceq'1396.733') 'Hand-derived true circular projection; negative-angle SVG sweep is0, not a chord'
Record 'a01-no-aabb-harm-area' ($null-ne$arc-and$arc.SelectNodes('.//*[local-name()="rect" or local-name()="polygon"]').Count-eq0-and
    $arc.SelectNodes('.//*[local-name()="path"]').Count-eq1-and$arc.SelectNodes('.//*[local-name()="circle"]').Count-eq3) 'Three circles are the physical pivot and two actual endpoint radii, not a filled reservation'
Record 'a01-configured-table-parameters' ([regex]::IsMatch($html,'(?s)<tr>(?:(?!</tr>).)*<td>A01</td><td>M20</td>(?:(?!</tr>).)*<td>\[3320,3956\]</td><td>\[3400,3716\]</td><td>\[3480,3956\]</td><td>-0\.6435011</td><td>24</td><td>3\.2</td><td>2</td><td>0\.5</td>(?:(?!</tr>).)*<td>conservative-aabb-not-damage-shape</td><td>configured-arc-not-runtime-route-proof</td></tr>')) 'One row links real geometry, motor timing, damage and non-runtime status'
Record 'a01-source-index-not-live-proof' ($publishedBinding.configuredArcGears-eq1-and
    $publishedBinding.arcGearStatus-ceq'configured-arc-not-runtime-route-proof'-and
    $publishedBinding.arcGearEnvelopePolicy-ceq'conservative-aabb-not-damage-shape'-and!$publishedBinding.humanAccepted) 'Static arc projection cannot certify live passage or human acceptance'
function FixtureCase([string]$name,[bool]$matchingDigest){
    $root=Join-Path $fixture $name;[void](New-Item -ItemType Directory -Path (Join-Path $root 'storage'))
    foreach($file in @('provenance.json','source-before.txt','fresh-binaries.txt','engine.stdout.log')){
        [IO.File]::WriteAllBytes((Join-Path $root $file),[IO.File]::ReadAllBytes((Join-Path $NativeCase $file)))
    }
    $map=Get-Content -LiteralPath $source -Raw | ConvertFrom-Json
    $map.enemies[0].name='<script>untrusted & "label"</script>'
    $path=Join-Path $root 'storage/current-prologue-map.json'
    [IO.File]::WriteAllText($path,($map | ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
    if($matchingDigest){
        $log=Get-Content -LiteralPath (Join-Path $root 'engine.stdout.log') -Raw
        $hash=(Get-FileHash -LiteralPath $path).Hash
        $log=[regex]::Replace($log,'(?m)^(CURRENT_PROLOGUE_MAP_DATA_SHA256 sha256=)[0-9A-F]{64}',('${1}'+$hash))
        [IO.File]::WriteAllText((Join-Path $root 'engine.stdout.log'),$log)
    }
    return $path
}
$badSource=FixtureCase 'changed-payload-text-fixture' $false
$r=InvokeRenderer @('-InputJson',$badSource,'-OutputDirectory',('G:/knight/过程图稿与视频/review-digest-rejection-'+$ReceiptName))
Record 'changed-payload-rejected' ($r.exit-ne0-and$r.stderr.Contains('payload digest absent or changed')) $r
$safeSource=FixtureCase 'escaped-label-text-fixture' $true
$nonfatalRoot=Join-Path $fixture 'controlled-nonfatal-row';[void](New-Item -ItemType Directory -Path $nonfatalRoot)
$row=[ordered]@{attributed=$true;source=@{StableId='canyon.v2.resident.split-level.02'};playerX=4696.565;playerY=1567.4756;physics=9416;remaining=1}
$logPath=Join-Path $nonfatalRoot 'engine.stdout.log'
[IO.File]::WriteAllText($logPath,('TASK10_ACCEPTED_DAMAGE '+($row | ConvertTo-Json -Depth 5 -Compress)))
$output='G:/knight/过程图稿与视频/2026-11-08_地图导出控制夹具_v314_'+$ReceiptName
$r=InvokeRenderer @('-InputJson',$safeSource,'-OutputDirectory',$output,'-FailureLog',$logPath)
if($r.exit-eq0){
    $h=Get-Content -LiteralPath (Join-Path $output 'original/current-map.html') -Raw
    $s=Get-Content -LiteralPath (Join-Path $output 'original/current-map.svg') -Raw
    Record 'actual-table-escapes-label' ($h.Contains('&lt;script&gt;untrusted')-and!$h.Contains('<script>untrusted')) 'Actual table consumes controlled label; complete provenance is a clearly named text fixture, not native evidence'
    Record 'nonfatal-observation-not-called-kill' (!$s.Contains('击杀')-and$s.Contains('受伤')) 'Controlled remaining=1 must be damage, not death'
}else{
    Record 'actual-table-escapes-label' $false $r
    Record 'nonfatal-observation-not-called-kill' $false $r
}
$result=[ordered]@{kind='renderer-review-controls-not-native';version='v314';rendererSha256=(Get-FileHash -LiteralPath $renderer).Hash;
    nativeSourceCase=[IO.Path]::GetFileName($nativeRoot);controls=$rows.ToArray();passed=(@($rows | Where-Object {!$_.passed}).Count-eq0);
    junctionTarget=$target;fixture=$fixture;nativeBehavior=$false}
$receipt=Join-Path $fixture 'review-controls.json'
[IO.File]::WriteAllText($receipt,($result | ConvertTo-Json -Depth 10),[Text.UTF8Encoding]::new($false))
foreach($row in $rows){Write-Output ('MAP_REVIEW_CONTROL name='+$row.name+' passed='+$row.passed)}
Write-Output ('MAP_REVIEW_RECEIPT='+$receipt)
if(!$result.passed){throw 'Renderer review controls failed'}
