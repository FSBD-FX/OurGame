param(
    [Parameter(Mandatory)][string]$InputJson,
    [string]$OutputDirectory,
    [string]$FailureLog,
    [switch]$SelfTest,
    [string]$SelfTestReceipt
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-CurrentMap($Map) {
    if($Map.schema-cne'knight.current-layout.v1'-or$Map.staticConfigurationOnly-isnot[bool]-or
        !$Map.staticConfigurationOnly-or$Map.wholeGameAccepted-isnot[bool]-or$Map.wholeGameAccepted){throw 'Map schema/status rejected'}
    if(($Map.bounds-join',')-cne'0,0,7680,4320'){throw 'Unexpected design bounds'}
    function Point($p,[int]$length=2){
        if(@($p).Count-ne$length){throw 'Malformed map coordinate'}
        foreach($n in $p){
            if(!($n-is[byte]-or$n-is[short]-or$n-is[int]-or$n-is[long]-or$n-is[single]-or$n-is[double]-or$n-is[decimal])-or
                ![double]::IsFinite([double]$n)){throw 'Non-numeric/non-finite map coordinate'}
        }
        if($p[0]-lt0-or$p[1]-lt0-or$p[0]-gt7680-or$p[1]-gt4320){throw 'Map point outside design bounds'}
        if($length-eq4-and($p[2]-le0-or$p[3]-le0-or$p[0]+$p[2]-gt7680-or$p[1]+$p[3]-gt4320)){throw 'Invalid map rectangle'}
    }
    function Unique($rows){
        $ids=@($rows | ForEach-Object {$_.id})
        if(@($ids | Select-Object -Unique).Count-ne$ids.Count-or@($ids | Where-Object {$_-isnot[string]-or[string]::IsNullOrWhiteSpace($_)}).Count){throw 'Missing/duplicate map identity'}
    }
    function Scalar($value,[double]$expected){
        if(!($value-is[byte]-or$value-is[short]-or$value-is[int]-or$value-is[long]-or$value-is[single]-or$value-is[double]-or$value-is[decimal])-or
            ![double]::IsFinite([double]$value)-or[double]$value-ne$expected){throw 'Configured moving gear numeric parameter rejected'}
    }
    if(@($Map.sectors).Count-ne5-or@($Map.solids).Count-ne158-or@($Map.enemies).Count-ne76-or
        @($Map.fragments).Count-ne9-or$Map.requiredFragments-ne6-or@($Map.bosses).Count-ne1-or
        @($Map.regions).Count-ne15-or@($Map.checkpoints).Count-ne3-or
        @($Map.regions | Where-Object id -CEQ 'IncineratorAnnex').Count-ne1){throw 'Incomplete current map counts'}
    foreach($collection in @('sectors','solids','enemies','fragments','objects','bosses','checkpoints','regions','surfaces')){Unique @($Map.$collection)}
    foreach($s in $Map.solids){
        if(@($s.corners).Count-ne4){throw 'Structural solid needs four affine corners'}
        foreach($p in $s.corners){Point $p}
    }
    foreach($s in $Map.sectors){Point $s.bounds 4;Point $s.offset}
    foreach($r in $Map.regions){Point $r.bounds 4}
    if(@($Map.enemies | Where-Object cohort -EQ 'encounter').Count-ne26-or
       @($Map.enemies | Where-Object cohort -EQ 'resident').Count-ne50){throw 'Encounter/resident split rejected'}
    $familyCounts=@{Native=8;Scrap=12;Precision=10;Bone=10;Ghost=10}
    foreach($family in $familyCounts.Keys){
        if(@($Map.enemies | Where-Object {$_.cohort-eq'resident'-and$_.family-ceq$family}).Count-ne$familyCounts[$family]){throw 'Resident family roster rejected'}
    }
    foreach($e in $Map.enemies){
        Point $e.point
        if($null-ne$e.alternatePoint){Point $e.alternatePoint}
        if($e.cohort-eq'resident'){
            Point $e.territory 4
            if(@($e.patrol).Count-lt1){throw 'Resident patrol absent'}
            foreach($p in $e.patrol){Point $p}
        }elseif($e.cohort-eq'encounter'){Point $e.arena 4;Point $e.activation 4}
        else{throw 'Unknown target cohort'}
    }
    foreach($collection in @('fragments','objects','bosses','checkpoints','surfaces')){foreach($row in $Map.$collection){Point $row.point}}
    if($null-eq$Map.PSObject.Properties['configuredHazardVolumes']-or@($Map.configuredHazardVolumes).Count-ne1){throw 'Exactly one configured hazard volume is required'}
    Unique @($Map.configuredHazardVolumes)
    $volume=$Map.configuredHazardVolumes[0]
    $poolId='CanyonBasin/Gameplay/Mechanisms/IncineratorPool'
    if($volume.id-cne$poolId-or$volume.objectId-cne$poolId-or$volume.type-cne'CanyonIncinerator'-or
        $volume.sector-cne'CanyonBasin'-or$volume.path-cne'Gameplay/Mechanisms/IncineratorPool'){
        throw 'Configured hazard identity/linkage rejected'
    }
    $poolObjects=@($Map.objects | Where-Object id -CEQ $volume.objectId)
    if($poolObjects.Count-ne1-or$poolObjects[0].type-cne$volume.type-or$poolObjects[0].sector-cne$volume.sector-or
        $poolObjects[0].path-cne$volume.path-or$poolObjects[0].point[0]-ne5800-or$poolObjects[0].point[1]-ne4158){
        throw 'Configured hazard must annotate its one existing object anchor'
    }
    $expectedCorners=@(@(5440,4020),@(6160,4020),@(6160,4296),@(5440,4296))
    if(@($volume.corners).Count-ne4){throw 'Configured hazard needs four corners'}
    for($i=0;$i-lt4;$i++){
        Point $volume.corners[$i]
        if($volume.corners[$i][0]-ne$expectedCorners[$i][0]-or$volume.corners[$i][1]-ne$expectedCorners[$i][1]){
            throw 'Configured hazard corners differ from the current saved volume'
        }
    }
    if(!($volume.pulseSeconds-is[byte]-or$volume.pulseSeconds-is[short]-or$volume.pulseSeconds-is[int]-or
        $volume.pulseSeconds-is[long]-or$volume.pulseSeconds-is[single]-or$volume.pulseSeconds-is[double]-or$volume.pulseSeconds-is[decimal])-or
        ![double]::IsFinite([double]$volume.pulseSeconds)-or$volume.pulseSeconds-ne0.1-or
        $volume.damagePolicy-cne'max-health-contact-attempt'-or$volume.unblockable-isnot[bool]-or!$volume.unblockable-or
        $volume.respectsHealthInvulnerability-isnot[bool]-or!$volume.respectsHealthInvulnerability-or
        $volume.status-cne'configured-volume-not-live-activation'){
        throw 'Configured hazard timing/health policy or static status rejected'
    }
    if(@($Map.objects).Count-eq0-or@($Map.sources).Count-lt8-or$Map.terrainRevision-cnotmatch'^[0-9A-F]{64}$'){throw 'Map provenance absent'}
    foreach($source in $Map.sources){if($source.path-cnotmatch'^res://'-or$source.sha256-cnotmatch'^[0-9A-F]{64}$'){throw 'Malformed source binding'}}
    if(@($Map.sources | Where-Object path -CEQ 'res://scripts/mechanisms/CanyonIncinerator.cs').Count-ne1){throw 'Configured hazard source binding absent or duplicate'}
    if(@($Map.objects).Count-ne57-or@($Map.objects | Where-Object type -CEQ 'TraversalSegment2D').Count-ne21-or
        @($Map.objects | Where-Object {$_.type-cmatch'Visual$'}).Count-ne4-or
        @($Map.objects | Where-Object {$_.type-cnotmatch'Visual$'-and$_.type-cne'TraversalSegment2D'}).Count-ne32){throw 'Current object composition rejected'}
    if($null-eq$Map.PSObject.Properties['configuredMovingGears']-or@($Map.configuredMovingGears).Count-ne1){throw 'Exactly one configured moving gear is required'}
    Unique @($Map.configuredMovingGears)
    $gear=$Map.configuredMovingGears[0]
    $gearId='CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear'
    if($gear.id-cne$gearId-or$gear.objectId-cne$gearId-or$gear.type-cne'CanyonMovingGear'-or
        $gear.sector-cne'CanyonBasin'-or$gear.path-cne'Gameplay/Mechanisms/UpperGalleryMovingGear'){
        throw 'Configured moving gear identity/linkage rejected'
    }
    $gearObjects=@($Map.objects | Where-Object id -CEQ $gear.objectId)
    if($gearObjects.Count-ne1-or@($Map.objects | Where-Object type -CEQ 'CanyonMovingGear').Count-ne1-or
        $gearObjects[0].type-cne$gear.type-or$gearObjects[0].sector-cne$gear.sector-or$gearObjects[0].path-cne$gear.path-or
        $gearObjects[0].status-cne'saved-anchor-not-dynamic-danger-envelope'-or
        $gearObjects[0].point[0]-ne3400-or$gearObjects[0].point[1]-ne3550){throw 'Configured moving gear must link its one saved initial object anchor'}
    Point $gear.start;Point $gear.end;Point $gear.envelope 4
    if($gear.start[0]-ne3400-or$gear.start[1]-ne3550-or$gear.end[0]-ne3400-or$gear.end[1]-ne3630-or
        $gear.envelope[0]-ne3376-or$gear.envelope[1]-ne3526-or$gear.envelope[2]-ne48-or$gear.envelope[3]-ne128){
        throw 'Configured moving gear rail/envelope differs from current saved configuration'
    }
    Scalar $gear.radius 24;Scalar $gear.periodSeconds 3.2;Scalar $gear.damage 2;Scalar $gear.contactIntervalSeconds 0.5
    if($gear.envelopePolicy-cne'conservative-aabb-not-damage-shape'-or$gear.status-cne'configured-rail-not-runtime-route-proof'){
        throw 'Configured moving gear static envelope/status policy rejected'
    }
    if($null-eq$Map.PSObject.Properties['configuredArcGears']-or@($Map.configuredArcGears).Count-ne1){throw 'Exactly one configured arc gear is required'}
    Unique @($Map.configuredArcGears)
    $arc=$Map.configuredArcGears[0]
    $arcId='CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear'
    if($arc.id-cne$arcId-or$arc.objectId-cne$arcId-or$arc.type-cne'CanyonArcGear'-or
        $arc.sector-cne'CanyonBasin'-or$arc.path-cne'Gameplay/Mechanisms/UpperGalleryUnderArcGear'){
        throw 'Configured arc identity/linkage rejected'
    }
    $arcObjects=@($Map.objects|Where-Object id -CEQ $arc.objectId)
    if($arcObjects.Count-ne1-or@($Map.objects|Where-Object type -CEQ 'CanyonArcGear').Count-ne1-or
        $arcObjects[0].type-cne$arc.type-or$arcObjects[0].sector-cne$arc.sector-or$arcObjects[0].path-cne$arc.path-or
        $arcObjects[0].status-cne'saved-anchor-not-dynamic-danger-envelope'){
        throw 'Configured arc must annotate exactly its own saved initial anchor'
    }
    Point $arcObjects[0].point;Point $arc.start;Point $arc.pivot;Point $arc.end;Point $arc.envelope 4
    if(($arcObjects[0].point-join',')-cne'3320,3956'-or($arc.start-join',')-cne'3320,3956'-or
        ($arc.pivot-join',')-cne'3400,3716'-or($arc.end-join',')-cne'3480,3956'){
        throw 'Configured arc start/pivot/signed end differ from actual current authoring'
    }
    Scalar $arc.radius 24;Scalar $arc.periodSeconds 3.2;Scalar $arc.damage 2
    Scalar $arc.contactIntervalSeconds 0.5;Scalar $arc.swingRadians -0.6435011
    # Explicit float32-outward Bounds values from the current saved export.
    # They include the interior low point, unlike endpoint-only height48.
    foreach($i in 0..3){Scalar $arc.envelope[$i] @(3295.9998,3931.9998,208.00049,60.982666)[$i]}
    if($arc.envelopePolicy-cne'conservative-aabb-not-damage-shape'-or$arc.status-cne'configured-arc-not-runtime-route-proof'){
        throw 'Configured arc static envelope/status policy rejected'
    }
    foreach($relative in @('scripts/mechanisms/CanyonMovingGear.cs','scripts/mechanisms/MovingGearMotionModel.cs',
        'scripts/mechanisms/CanyonArcGear.cs','scripts/mechanisms/ArcGearMotionModel.cs','scripts/mechanisms/CircularArcSweep.cs')){
        $binding=@($Map.sources | Where-Object path -CEQ ('res://'+$relative))
        $fixedSource=Join-Path (Split-Path $PSScriptRoot) $relative
        Assert-MapAncestors $fixedSource
        if($binding.Count-ne1-or$binding[0].sha256-cne(Get-FileHash -LiteralPath $fixedSource -Algorithm SHA256).Hash){
            throw 'Configured moving gear source binding absent,duplicate or not current'
        }
    }
}

function Assert-MapAncestors([string]$Path) {
    for($part=[IO.Path]::GetFullPath($Path);$part;$part=[IO.Path]::GetDirectoryName($part)){
        if((Test-Path -LiteralPath $part)-and((Get-Item -LiteralPath $part -Force).Attributes-band[IO.FileAttributes]::ReparsePoint)){
            throw 'Reparse source/receipt ancestor rejected'
        }
    }
}
function Read-CurrentMap([string]$Path) {
    if(![IO.Path]::IsPathFullyQualified($Path)){throw 'An absolute source JSON path is required'}
    $full=[IO.Path]::GetFullPath($Path)
    if(!($full.StartsWith('G:\knight\验证证据\',[StringComparison]::OrdinalIgnoreCase) -or
         $full.StartsWith('G:\knight\过程图稿与视频\',[StringComparison]::OrdinalIgnoreCase))){throw 'Source is outside KNIGHT artifact storage'}
    Assert-MapAncestors $full
    $item=Get-Item -LiteralPath $full
    if($item.Length-gt2MB-or$item.Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Invalid map source file'}
    return Get-Content -LiteralPath $full -Raw | ConvertFrom-Json
}

$map=Read-CurrentMap $InputJson
if($SelfTest){
    if(!$SelfTestReceipt-or![IO.Path]::IsPathFullyQualified($SelfTestReceipt)-or
        [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($SelfTestReceipt))-cne[IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($InputJson))-or
        (Test-Path -LiteralPath $SelfTestReceipt)){throw 'Fresh self-test receipt must be beside its source JSON'}
    Assert-CurrentMap $map
    $cases=@(
        @{name='schema'; change={param($x) $x.schema='other.layout'}},
        @{name='missing-target'; change={param($x) $x.enemies=@($x.enemies | Select-Object -Skip 1)}},
        @{name='duplicate-target'; change={param($x) $x.enemies[1].id=$x.enemies[0].id}},
        @{name='nonfinite-coordinate'; change={param($x) $x.enemies[0].point[0]=[double]::NaN}},
        @{name='outside-solid'; change={param($x) $x.solids[0].corners[0][0]=9000}},
        @{name='missing-fragment'; change={param($x) $x.fragments=@($x.fragments | Select-Object -Skip 1)}},
        @{name='false-acceptance'; change={param($x) $x.wholeGameAccepted=$true}},
        @{name='missing-configured-volume'; change={param($x) $x.PSObject.Properties.Remove('configuredHazardVolumes')}},
        @{name='duplicate-configured-volume'; change={param($x) $x.configuredHazardVolumes+=$x.configuredHazardVolumes[0]}},
        @{name='wrong-volume-id'; change={param($x) $x.configuredHazardVolumes[0].id='wrong-id'}},
        @{name='wrong-volume-corners'; change={param($x) $x.configuredHazardVolumes[0].corners[0][0]=5439}},
        @{name='nonfinite-volume-coordinate'; change={param($x) $x.configuredHazardVolumes[0].corners[0][0]=[double]::NaN}},
        @{name='false-live-volume-acceptance'; change={param($x) $x.configuredHazardVolumes[0].status='live-activation-accepted'}},
        @{name='wrong-volume-linkage'; change={param($x) $x.configuredHazardVolumes[0].objectId=$x.objects[0].id}},
        @{name='missing-volume-object'; change={param($x) $x.objects=@($x.objects | Where-Object type -CNE 'CanyonIncinerator')}},
        @{name='wrong-volume-object-type'; change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonIncinerator').type='CanyonSteamJet'}},
        @{name='wrong-volume-sector'; change={param($x) $x.configuredHazardVolumes[0].sector='CanyonAscent'}},
        @{name='wrong-volume-path'; change={param($x) $x.configuredHazardVolumes[0].path='Gameplay/Mechanisms/Other'}},
        @{name='wrong-volume-pulse'; change={param($x) $x.configuredHazardVolumes[0].pulseSeconds=0.2}},
        @{name='wrong-volume-damage-policy'; change={param($x) $x.configuredHazardVolumes[0].damagePolicy='instant-death'}},
        @{name='blockable-volume'; change={param($x) $x.configuredHazardVolumes[0].unblockable=$false}},
        @{name='volume-ignores-health-invulnerability'; change={param($x) $x.configuredHazardVolumes[0].respectsHealthInvulnerability=$false}},
        @{name='volume-missing-source-binding'; change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/CanyonIncinerator.cs')}},
        @{name='missing-annex-region'; change={param($x) $x.regions=@($x.regions | Where-Object id -CNE 'IncineratorAnnex')}},
        @{name='non-numeric-volume-pulse'; change={param($x) $x.configuredHazardVolumes[0].pulseSeconds='0.1'}},
        @{name='missing-moving-gear';change={param($x) $x.PSObject.Properties.Remove('configuredMovingGears')}},
        @{name='duplicate-moving-gear';change={param($x) $x.configuredMovingGears+=$x.configuredMovingGears[0]}},
        @{name='wrong-moving-gear-id';change={param($x) $x.configuredMovingGears[0].id='wrong-id'}},
        @{name='wrong-moving-gear-linkage';change={param($x) $x.configuredMovingGears[0].objectId=$x.objects[0].id}},
        @{name='wrong-moving-gear-type';change={param($x) $x.configuredMovingGears[0].type='CanyonFixedGear'}},
        @{name='wrong-moving-gear-sector';change={param($x) $x.configuredMovingGears[0].sector='CanyonAscent'}},
        @{name='wrong-moving-gear-path';change={param($x) $x.configuredMovingGears[0].path='Gameplay/Mechanisms/Other'}},
        @{name='wrong-moving-gear-start';change={param($x) $x.configuredMovingGears[0].start[0]=3399}},
        @{name='wrong-moving-gear-end';change={param($x) $x.configuredMovingGears[0].end[1]=3629}},
        @{name='nonfinite-moving-gear-start';change={param($x) $x.configuredMovingGears[0].start[0]=[double]::NaN}},
        @{name='outside-moving-gear-end';change={param($x) $x.configuredMovingGears[0].end[1]=4400}},
        @{name='wrong-moving-gear-envelope';change={param($x) $x.configuredMovingGears[0].envelope[3]=127}},
        @{name='outside-moving-gear-envelope';change={param($x) $x.configuredMovingGears[0].envelope=@(3376,4300,48,128)}},
        @{name='nonfinite-moving-gear-envelope';change={param($x) $x.configuredMovingGears[0].envelope[0]=[double]::NaN}},
        @{name='wrong-moving-gear-radius';change={param($x) $x.configuredMovingGears[0].radius=25}},
        @{name='non-numeric-moving-gear-radius';change={param($x) $x.configuredMovingGears[0].radius='24'}},
        @{name='wrong-moving-gear-period';change={param($x) $x.configuredMovingGears[0].periodSeconds=0}},
        @{name='nonfinite-moving-gear-period';change={param($x) $x.configuredMovingGears[0].periodSeconds=[double]::NaN}},
        @{name='wrong-moving-gear-damage';change={param($x) $x.configuredMovingGears[0].damage=3}},
        @{name='non-numeric-moving-gear-damage';change={param($x) $x.configuredMovingGears[0].damage='2'}},
        @{name='wrong-moving-gear-interval';change={param($x) $x.configuredMovingGears[0].contactIntervalSeconds=-0.5}},
        @{name='non-numeric-moving-gear-interval';change={param($x) $x.configuredMovingGears[0].contactIntervalSeconds='0.5'}},
        @{name='moving-gear-false-damage-envelope';change={param($x) $x.configuredMovingGears[0].envelopePolicy='exact-live-damage-shape'}},
        @{name='moving-gear-false-live-acceptance';change={param($x) $x.configuredMovingGears[0].status='runtime-route-accepted'}},
        @{name='missing-moving-gear-object';change={param($x) $x.objects=@($x.objects | Where-Object type -CNE 'CanyonMovingGear')}},
        @{name='wrong-moving-gear-object-point';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').point[1]=3551}},
        @{name='wrong-moving-gear-object-type';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').type='CanyonFixedGear'}},
        @{name='wrong-moving-gear-object-status';change={param($x) ($x.objects | Where-Object type -CEQ 'CanyonMovingGear').status='runtime-route-accepted'}},
        @{name='moving-gear-missing-source';change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/CanyonMovingGear.cs')}},
        @{name='moving-gear-duplicate-source';change={param($x) $x.sources+=($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/CanyonMovingGear.cs')}},
        @{name='moving-gear-wrong-source-sha';change={param($x) ($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/CanyonMovingGear.cs').sha256=('A'*64)}},
        @{name='moving-model-missing-source';change={param($x) $x.sources=@($x.sources | Where-Object path -CNE 'res://scripts/mechanisms/MovingGearMotionModel.cs')}},
        @{name='moving-model-duplicate-source';change={param($x) $x.sources+=($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/MovingGearMotionModel.cs')}},
        @{name='moving-model-wrong-source-sha';change={param($x) ($x.sources | Where-Object path -CEQ 'res://scripts/mechanisms/MovingGearMotionModel.cs').sha256=('A'*64)}}
    )
    $rows=@(foreach($case in $cases){
        $bad=$map | ConvertTo-Json -Depth 100 | ConvertFrom-Json
        & $case.change $bad
        $rejected=$false
        try{Assert-CurrentMap $bad}catch{$rejected=$true}
        [pscustomobject]@{name=$case.name;rejected=$rejected}
    })
    $receipt=[ordered]@{version='v331';kind='renderer-validation-not-native-qualification';sourceSha256=(Get-FileHash -LiteralPath $InputJson -Algorithm SHA256).Hash;
        rendererSha256=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash;
        positive=1;negative=$rows;passed=(@($rows | Where-Object {!$_.rejected}).Count-eq0);nativeBehavior=$false}
    Assert-MapAncestors $SelfTestReceipt
    $receiptBytes=[Text.UTF8Encoding]::new($false).GetBytes(($receipt | ConvertTo-Json -Depth 8))
    $receiptFile=[IO.File]::Open($SelfTestReceipt,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$receiptFile.Write($receiptBytes,0,$receiptBytes.Length)}finally{$receiptFile.Dispose()}
    if(!$receipt.passed){throw 'Renderer validation admitted malformed current-map data'}
    Write-Output ('CURRENT_MAP_RENDERER_VALIDATION_OK version=v331 positive=1 negative='+$rows.Count+' native_behavior=false')
    exit 0
}
Assert-CurrentMap $map
if(!$OutputDirectory-or![IO.Path]::IsPathFullyQualified($OutputDirectory)){throw 'A fresh absolute archive directory is required'}
$destination=[IO.Path]::GetFullPath($OutputDirectory)
if(!$destination.StartsWith('G:\knight\过程图稿与视频\',[StringComparison]::OrdinalIgnoreCase)-or
    (Test-Path -LiteralPath $destination)){throw 'Do not overwrite or leave KNIGHT media storage'}
for($ancestor=[IO.Path]::GetDirectoryName($destination);$ancestor;$ancestor=[IO.Path]::GetDirectoryName($ancestor)){
    if(Test-Path -LiteralPath $ancestor){
        if((Get-Item -LiteralPath $ancestor -Force).Attributes-band[IO.FileAttributes]::ReparsePoint){throw 'Reparse ancestor rejected'}
    }
}
function Encode-MapText($text){return [Net.WebUtility]::HtmlEncode([string]$text)}
function Number($value){return ([double]$value).ToString('0.###',[Globalization.CultureInfo]::InvariantCulture)}
$inputSha=(Get-FileHash -LiteralPath $InputJson -Algorithm SHA256).Hash
$rendererSha=(Get-FileHash -LiteralPath $PSCommandPath -Algorithm SHA256).Hash
$nativeCase=[IO.Path]::GetDirectoryName([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($InputJson)))
foreach($bindingFile in @('provenance.json','engine.stdout.log','source-before.txt','fresh-binaries.txt')){
    Assert-MapAncestors (Join-Path $nativeCase $bindingFile)
}
$provenance=Get-Content -LiteralPath (Join-Path $nativeCase 'provenance.json') -Raw | ConvertFrom-Json
if($provenance.phase-cne'resident-map-preview'-or$provenance.engine.ExitCode-ne0-or$provenance.engine.TimedOut-or
    $provenance.failure-or@($provenance.guardFailures).Count){throw 'Native map source was not strictly qualified'}
$nativeRaw=Get-Content -LiteralPath (Join-Path $nativeCase 'engine.stdout.log') -Raw
$digestRows=[regex]::Matches($nativeRaw,'(?m)^CURRENT_PROLOGUE_MAP_DATA_SHA256 sha256=([0-9A-F]{64})\r?$')
if($digestRows.Count-ne1-or$digestRows[0].Groups[1].Value-cne$inputSha){throw 'Native map payload digest absent or changed'}
$familyColors=@{Encounter='#A7B6C5';Native='#67BA9F';Scrap='#E6AE68';Precision='#6FAFE9';Bone='#D5C9AD';Ghost='#AD99DA'}
$sectorNames=@{CanyonEntrance='入口 / 荒坡';CanyonDescent='下降峡谷';CanyonBasin='盆地 / 车间';CanyonAscent='上行机井';LostTemple='神殿 / Boss'}
$familyNames=@{Encounter='原遭遇';Native='原生';Scrap='废弃';Precision='精工';Bone='骨骸';Ghost='幽灵'}
$regionNames=@{LowCeilingBridge='入口低顶桥';StaggeredRift='交错裂峡';ControlledAbyss='深渊 / 气流';ArchiveSideCave='档案侧洞';FrozenDetour='冰封支路';BasinCamp='盆地营地';GearPistons='齿轮活塞';SteamValves='蒸汽阀室';CounterweightLift='配重升降';PendulumGallery='摆锤横廊';SplitLevelEncounter='错层遭遇';SealedCourtyard='封印庭院';BossHall='Boss大厅';SafeExit='有支撑出口';IncineratorAnnex='熔炉附室'}
$objectTypeGroups=@($map.objects | Group-Object type)
$traversalCount=@($map.objects | Where-Object type -CEQ 'TraversalSegment2D').Count
$visualCount=@($map.objects | Where-Object {$_.type-cmatch'Visual$'}).Count
$logicObjects=@($map.objects | Where-Object {$_.type-cnotmatch'Visual$'-and$_.type-cne'TraversalSegment2D'})
$deviceLabels=@{}
for($i=0;$i-lt$logicObjects.Count;$i++){$deviceLabels[$logicObjects[$i].id]='M'+($i+1).ToString('00')}
$observation=$null
if($FailureLog){
    $logPath=[IO.Path]::GetFullPath($FailureLog)
    if(!$logPath.StartsWith('G:\knight\验证证据\',[StringComparison]::OrdinalIgnoreCase)-or[IO.Path]::GetFileName($logPath)-cne'engine.stdout.log'){throw 'Only retained KNIGHT native stdout may supply an observation'}
    Assert-MapAncestors $logPath
    $damage=@(Get-Content -LiteralPath $logPath | Where-Object { $_.StartsWith('TASK10_ACCEPTED_DAMAGE ') } | Select-Object -Last 1)
    if($damage.Count-ne1){throw 'No actual accepted-damage row in supplied log'}
    $row=$damage[0].Substring('TASK10_ACCEPTED_DAMAGE '.Length) | ConvertFrom-Json
    if($row.attributed-isnot[bool]-or!$row.attributed-or$row.source.StableId-notin@($map.enemies.id)-or
        !($row.remaining-is[int]-or$row.remaining-is[long])-or$row.remaining-lt0-or
        ![double]::IsFinite($row.playerX)-or![double]::IsFinite($row.playerY)-or
        $row.playerX-lt0-or$row.playerX-gt7680-or$row.playerY-lt0-or$row.playerY-gt4320){throw 'Unsupported damage observation'}
    $observation=[ordered]@{kind='one-test-observation-not-fixed-hazard';sourceId=$row.source.StableId;
        point=@($row.playerX,$row.playerY);physics=$row.physics;remaining=$row.remaining;
        logSha256=(Get-FileHash -LiteralPath $logPath -Algorithm SHA256).Hash;
        case=[IO.Path]::GetFileName([IO.Path]::GetDirectoryName($logPath))}
}
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new(3200,1800)
$graphics=[Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode=[Drawing.Drawing2D.SmoothingMode]::AntiAlias
$fonts=@{}
$svg=[Text.StringBuilder]::new()
[void]$svg.AppendLine('<svg xmlns="http://www.w3.org/2000/svg" width="3200" height="1800" viewBox="0 0 3200 1800" role="img"><title>KNIGHT 当前配置侧视图</title>')
function Text($text,[double]$x,[double]$y,[int]$size=18,[string]$color='#DCE5EE'){
    if(!$fonts.ContainsKey($size)){$fonts[$size]=[Drawing.Font]::new('Microsoft YaHei',$size,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel)}
    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
    try{$graphics.DrawString([string]$text,$fonts[$size],$brush,[single]$x,[single]$y)}finally{$brush.Dispose()}
    [void]$svg.AppendLine('<text x="'+(Number $x)+'" y="'+(Number ($y+$size))+'" fill="'+$color+'" font-family="Microsoft YaHei,sans-serif" font-size="'+$size+'">'+(Encode-MapText $text)+'</text>')
}
function Poly($points,[string]$fill,[string]$stroke,[string]$attrs=''){
    $pixels=[Drawing.PointF[]]@($points | ForEach-Object {[Drawing.PointF]::new([single](64+$_[0]*2432/7680),[single](220+$_[1]*1368/4320))})
    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($fill))
    $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml($stroke),[single]1)
    try{$graphics.FillPolygon($brush,$pixels);$graphics.DrawPolygon($pen,$pixels)}finally{$brush.Dispose();$pen.Dispose()}
    $xy=@($pixels | ForEach-Object {(Number $_.X)+','+(Number $_.Y)})-join' '
    [void]$svg.AppendLine('<polygon '+$attrs+' points="'+$xy+'" fill="'+$fill+'" stroke="'+$stroke+'" stroke-width="1"/>')
}
function Box($rect,[string]$fill,[string]$stroke,[string]$attrs=''){
    Poly @(@($rect[0],$rect[1]),@(($rect[0]+$rect[2]),$rect[1]),@(($rect[0]+$rect[2]),($rect[1]+$rect[3])),@($rect[0],($rect[1]+$rect[3]))) $fill $stroke $attrs
}
function Mark($point,[string]$color,[double]$radius,[string]$kind,[string]$id,[string]$label=''){
    $x=64+$point[0]*2432/7680;$y=220+$point[1]*1368/4320
    $brush=[Drawing.SolidBrush]::new([Drawing.ColorTranslator]::FromHtml($color))
    try{$graphics.FillEllipse($brush,[single]($x-$radius),[single]($y-$radius),[single]($radius*2),[single]($radius*2))}finally{$brush.Dispose()}
    [void]$svg.AppendLine('<circle data-kind="'+$kind+'" data-id="'+(Encode-MapText $id)+'" cx="'+(Number $x)+'" cy="'+(Number $y)+'" r="'+(Number $radius)+'" fill="'+$color+'"><title>'+(Encode-MapText ($id+' '+$label))+'</title></circle>')
    if($label){Text $label ($x+8) ($y-14) 11 $color}
}
function Rail($gear,[string]$label,[string]$anchorLabel){
    $x1=64+$gear.start[0]*2432/7680;$y1=220+$gear.start[1]*1368/4320
    $x2=64+$gear.end[0]*2432/7680;$y2=220+$gear.end[1]*1368/4320
    $radius=$gear.radius*2432/7680
    $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#EAC875'),[single]2)
    try{
        $graphics.DrawLine($pen,[single]$x1,[single]$y1,[single]$x2,[single]$y2)
        foreach($center in @(@($x1,$y1),@($x2,$y2))){
            $graphics.DrawEllipse($pen,[single]($center[0]-$radius),[single]($center[1]-$radius),[single]($radius*2),[single]($radius*2))
        }
    }finally{$pen.Dispose()}
    [void]$svg.AppendLine('<g data-kind="configured-moving-gear" data-id="'+(Encode-MapText $gear.id)+'" data-object-id="'+(Encode-MapText $gear.objectId)+'" data-object-label="'+$anchorLabel+'" data-map-label="'+$label+'" data-status="configured-rail-not-runtime-route-proof">')
    [void]$svg.AppendLine('<line data-kind="configured-rail-centerline" x1="'+(Number $x1)+'" y1="'+(Number $y1)+'" x2="'+(Number $x2)+'" y2="'+(Number $y2)+'" stroke="#EAC875" stroke-width="2"/>')
    [void]$svg.AppendLine('<circle data-kind="configured-rail-start" cx="'+(Number $x1)+'" cy="'+(Number $y1)+'" r="'+(Number $radius)+'" fill="none" stroke="#EAC875" stroke-width="2"/>')
    [void]$svg.AppendLine('<circle data-kind="configured-rail-end" cx="'+(Number $x2)+'" cy="'+(Number $y2)+'" r="'+(Number $radius)+'" fill="none" stroke="#EAC875" stroke-width="2"/>')
    [void]$svg.AppendLine('</g>')
    Text ($label+' / '+$anchorLabel+' 配置轨道') ($x2+12) ($y2+6) 13 '#EAC875'
}
function Arc($gear,[string]$label,[string]$anchorLabel){
    $scale=2432/7680
    $x1=64+$gear.start[0]*$scale;$y1=220+$gear.start[1]*$scale
    $x2=64+$gear.end[0]*$scale;$y2=220+$gear.end[1]*$scale
    $px=64+$gear.pivot[0]*$scale;$py=220+$gear.pivot[1]*$scale
    $dx=$gear.start[0]-$gear.pivot[0];$dy=$gear.start[1]-$gear.pivot[1]
    $length=[Math]::Sqrt($dx*$dx+$dy*$dy)*$scale;$radius=$gear.radius*$scale
    $startDegrees=[Math]::Atan2($dy,$dx)*180/[Math]::PI;$sweepDegrees=$gear.swingRadians*180/[Math]::PI
    $pen=[Drawing.Pen]::new([Drawing.ColorTranslator]::FromHtml('#79D4D0'),[single]2)
    try{
        $graphics.DrawArc($pen,[single]($px-$length),[single]($py-$length),[single]($length*2),[single]($length*2),[single]$startDegrees,[single]$sweepDegrees)
        $graphics.DrawLine($pen,[single]$px,[single]$py,[single]$x1,[single]$y1)
        foreach($center in @(@($x1,$y1),@($x2,$y2))){$graphics.DrawEllipse($pen,[single]($center[0]-$radius),[single]($center[1]-$radius),[single]($radius*2),[single]($radius*2))}
        $graphics.DrawEllipse($pen,[single]($px-3),[single]($py-3),[single]6,[single]6)
    }finally{$pen.Dispose()}
    $sweep=if($gear.swingRadians-gt0){1}else{0}
    $path='M'+(Number $x1)+' '+(Number $y1)+' A'+(Number $length)+' '+(Number $length)+' 0 0 '+$sweep+' '+(Number $x2)+' '+(Number $y2)
    [void]$svg.AppendLine('<g data-kind="configured-arc-gear" data-id="'+(Encode-MapText $gear.id)+'" data-object-id="'+(Encode-MapText $gear.objectId)+'" data-object-label="'+$anchorLabel+'" data-map-label="'+$label+'" data-status="configured-arc-not-runtime-route-proof">')
    [void]$svg.AppendLine('<path data-kind="configured-arc-centerline" d="'+$path+'" fill="none" stroke="#79D4D0" stroke-width="2"/>')
    [void]$svg.AppendLine('<line x1="'+(Number $px)+'" y1="'+(Number $py)+'" x2="'+(Number $x1)+'" y2="'+(Number $y1)+'" stroke="#79D4D0" stroke-width="1" stroke-dasharray="4 3"/>')
    [void]$svg.AppendLine('<circle data-kind="configured-arc-pivot" cx="'+(Number $px)+'" cy="'+(Number $py)+'" r="3" fill="none" stroke="#79D4D0"/>')
    foreach($endpoint in @(@('start',$x1,$y1),@('end',$x2,$y2))){
        [void]$svg.AppendLine('<circle data-kind="configured-arc-'+$endpoint[0]+'" cx="'+(Number $endpoint[1])+'" cy="'+(Number $endpoint[2])+'" r="'+(Number $radius)+'" fill="none" stroke="#79D4D0" stroke-width="2"/>')
    }
    [void]$svg.AppendLine('</g>')
    Text ($label+' / '+$anchorLabel+' 悬吊弧线') ($x2+12) ($y2+6) 13 '#79D4D0'
}
try{
    $graphics.Clear([Drawing.ColorTranslator]::FromHtml('#0B141E'))
    [void]$svg.AppendLine('<rect width="3200" height="1800" fill="#0B141E"/>')
    Text 'KNIGHT · 当前关卡配置侧视图' 64 42 38
    Text ([string]$map.solids.Count+' 结构固体  /  76 普通目标 = 26 原遭遇 + 50 常驻  /  9 残片（门槛6件，另需机关条件）') 64 105 23
    Text '配置与通路审阅，不是关卡画面。Y向下；实际世界 7680 × 4320。未覆盖设计区不表示可通路线。' 64 155 19 '#91A9BB'
    foreach($sector in $map.sectors){Box $sector.bounds '#102231' '#476172' ('data-kind="sector" data-id="'+(Encode-MapText $sector.id)+'"')}
    foreach($region in $map.regions){
        $fill=if($region.optional){'#1C2435'}else{'#112736'}
        Box $region.bounds $fill '#2D4657' ('data-kind="region" data-id="'+(Encode-MapText $region.id)+'"')
    }
    foreach($solid in $map.solids){Poly $solid.corners '#29424F' '#5A7381' ('data-kind="solid" data-id="'+(Encode-MapText $solid.id)+'"')}
    $ordinal=0
    foreach($volume in $map.configuredHazardVolumes){
        $ordinal++;$volumeLabel='H'+$ordinal.ToString('00');$anchorLabel=$deviceLabels[$volume.objectId]
        Poly $volume.corners '#663D2C' '#F4A168' ('data-kind="configured-hazard-volume" data-id="'+(Encode-MapText $volume.id)+'" data-object-id="'+(Encode-MapText $volume.objectId)+'" data-object-label="'+$anchorLabel+'" data-map-label="'+$volumeLabel+'"')
        Text ($volumeLabel+' / '+$anchorLabel+' 熔炉配置范围') (72+$volume.corners[0][0]*2432/7680) (228+$volume.corners[0][1]*1368/4320) 15 '#F4A168'
    }
    $ordinal=0
    foreach($gear in $map.configuredMovingGears){$ordinal++;Rail $gear ('R'+$ordinal.ToString('00')) $deviceLabels[$gear.objectId]}
    $ordinal=0
    foreach($gear in $map.configuredArcGears){$ordinal++;Arc $gear ('A'+$ordinal.ToString('00')) $deviceLabels[$gear.objectId]}
    foreach($region in $map.regions){
        $suffix=if($region.optional){' [支路]'}else{''}
        Text ($regionNames[$region.id]+$suffix) (70+$region.bounds[0]*2432/7680) (226+$region.bounds[1]*1368/4320) 16 '#A8BECE'
    }
    $ordinal=0
    foreach($device in $logicObjects){$ordinal++;Mark $device.point '#D98D69' 4 'object' $device.id ('M'+$ordinal.ToString('00'))}
    $ordinal=0
    foreach($enemy in $map.enemies){$ordinal++;Mark $enemy.point $familyColors[$enemy.family] 6 'enemy' $enemy.id ('E'+$ordinal.ToString('00'))}
    $ghostPixel=$bitmap.GetPixel(1568,729)
    if($ghostPixel.R+$ghostPixel.G+$ghostPixel.B-lt400){throw 'Literal lost-echo projected mark is not visibly rendered'}
    $ordinal=0
    foreach($fragment in $map.fragments){$ordinal++;Mark $fragment.point '#E8D183' 7 'fragment' $fragment.id ('F'+$ordinal.ToString('00'))}
    foreach($checkpoint in $map.checkpoints){Mark $checkpoint.point '#9CD8B1' 9 'checkpoint' $checkpoint.id '存档'}
    foreach($boss in $map.bosses){Mark $boss.point '#E894A5' 12 'boss' $boss.id 'Boss配置点'}
    if($observation){
        $observationLabel=if($observation.remaining-eq0){'此次测试击杀点（非固定陷阱）'}else{'此次测试受伤点（非固定陷阱）'}
        Mark $observation.point '#F28078' 5 'observation' $observation.sourceId $observationLabel
    }
    Text '分布与阅读方式' 2550 235 26
    $legendY=292
    foreach($family in @('Encounter','Native','Scrap','Precision','Bone','Ghost')){
        $count=@($map.enemies | Where-Object family -EQ $family).Count
        Text ('● '+$familyNames[$family]+'  '+$count) 2550 $legendY 22 $familyColors[$family]
        $legendY+=42
    }
    Text '五引擎区配置目标' 2550 585 24
    $legendY=632
    foreach($sector in $map.sectors){
        Text ($sectorNames[$sector.id]+'  '+@($map.enemies | Where-Object sector -EQ $sector.id).Count) 2550 $legendY 21
        $legendY+=40
    }
    $notes=@(
        'E编号：普通敌人配置目标；不是同屏活体数。',
        'F编号：9残片；绿色大点：3处恢复位置。',
        ('M编号：'+$logicObjects.Count+'个机关/门/补给锚点。'),
        ('另有'+$traversalCount+'通行段、'+$visualCount+'外观组件，只列在清单。'),
        'H01：已有M锚点的静态配置范围，非新陷阱。',
        '每0.1秒尝试最大生命值伤害，不可格挡；',
        '仍经生命/无敌帧处理，不代表触碰必死。',
        'R01：移动齿轮配置起终圆与直线，非路线证明。',
        '保守AABB仅列参数；不是实际伤害形状。',
        'Boss：配置召唤位置，不是已出生统计。',
        'ECHO：1个附随指导身份，无固定刷新点。',
        '独立友方身体/商人：0（未来内容不混算）。',
        ([string]$map.regions.Count+'功能域；紫暗区为可选支路。'),
        '出生替代点、巡游域、门禁与抓取材质见清单。',
        '阴影区是未覆盖设计空间，不表示可通行。',
        '未绘制虚构的已验证跳跃轨迹。',
        '本图不作路线/资源/美术验收。'
    )
    $legendY=875
    foreach($note in $notes){Text $note 2550 $legendY 19 '#A9BDCC';$legendY+=41}
    Text '地图是静态配置证据，不是新美术或试玩验收。' 64 1633 23 '#D9B795'
    if($observation){
        $note=if($observation.remaining-eq0){'红点是一次自动探索被击杀的位置，不是新增固定陷阱或必死区域。'}else{'红点是一次测试的受伤位置，不是死亡点、新增固定陷阱或必死区域。'}
        Text $note 64 1678 19 '#F28078'
    }
    Text ('数据 SHA256  '+$inputSha.Substring(0,24)+'…   /   地形 Revision  '+$map.terrainRevision.Substring(0,24)+'…') 64 1720 17 '#91A9BB'
    [void]$svg.AppendLine('</svg>')
    $svgText=$svg.ToString()
    [xml]$svgDocument=$svgText
    foreach($expected in @(@('solid',158),@('enemy',76),@('fragment',9),@('checkpoint',3),@('boss',1),@('region',15),@('configured-hazard-volume',1),@('configured-moving-gear',1),@('configured-arc-gear',1),@('object',32))){
        if($svgDocument.SelectNodes("//*[@data-kind='$($expected[0])']").Count-ne$expected[1]){throw 'Actual SVG composition count mismatch'}
    }
    $ghost=$svgDocument.SelectSingleNode("//*[@data-kind='enemy' and @data-id='canyon.v2.resident.split-level.02']")
    if([Math]::Abs([double]::Parse($ghost.cx,[Globalization.CultureInfo]::InvariantCulture)-1568.167)-gt.01-or
       [Math]::Abs([double]::Parse($ghost.cy,[Globalization.CultureInfo]::InvariantCulture)-729.2)-gt.01){throw 'Independent literal marker projection mismatch'}
    $hazard=$svgDocument.SelectSingleNode("//*[@data-kind='configured-hazard-volume' and @data-map-label='H01']")
    if($hazard.GetAttribute('data-id')-cne'CanyonBasin/Gameplay/Mechanisms/IncineratorPool'-or
       $hazard.GetAttribute('data-object-id')-cne$hazard.GetAttribute('data-id')-or
       $hazard.GetAttribute('data-object-label')-cne$deviceLabels['CanyonBasin/Gameplay/Mechanisms/IncineratorPool']-or
       $hazard.points-cne'1786.667,1493 2014.667,1493 2014.667,1580.4 1786.667,1580.4'){
        throw 'Configured hazard rendered identity/projection mismatch'
    }
    $rail=$svgDocument.SelectSingleNode("//*[@data-kind='configured-moving-gear' and @data-map-label='R01']")
    $centerline=$rail.SelectSingleNode("*[@data-kind='configured-rail-centerline']")
    $startCircle=$rail.SelectSingleNode("*[@data-kind='configured-rail-start']")
    $endCircle=$rail.SelectSingleNode("*[@data-kind='configured-rail-end']")
    $gearAnchor=$svgDocument.SelectSingleNode("//*[@data-kind='object' and @data-id='CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear']")
    if($rail.GetAttribute('data-id')-cne'CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear'-or
        $rail.GetAttribute('data-object-id')-cne$gearAnchor.GetAttribute('data-id')-or
        $rail.GetAttribute('data-object-label')-cne$deviceLabels['CanyonBasin/Gameplay/Mechanisms/UpperGalleryMovingGear']-or
        $gearAnchor.GetAttribute('cx')-cne'1140.667'-or$gearAnchor.GetAttribute('cy')-cne'1344.167'-or
        $centerline.GetAttribute('x1')-cne'1140.667'-or$centerline.GetAttribute('y1')-cne'1344.167'-or
        $centerline.GetAttribute('x2')-cne'1140.667'-or$centerline.GetAttribute('y2')-cne'1369.5'-or
        $startCircle.GetAttribute('cx')-cne'1140.667'-or$startCircle.GetAttribute('cy')-cne'1344.167'-or$startCircle.GetAttribute('r')-cne'7.6'-or
        $endCircle.GetAttribute('cx')-cne'1140.667'-or$endCircle.GetAttribute('cy')-cne'1369.5'-or$endCircle.GetAttribute('r')-cne'7.6'-or
        $rail.SelectNodes('.//*[local-name()="polygon" or local-name()="rect"]').Count-ne0-or
        $rail.SelectNodes('.//*[local-name()="line"]').Count-ne1-or$rail.SelectNodes('.//*[local-name()="circle"]').Count-ne2){
        throw 'Independent configured moving gear point/rail projection mismatch'
    }
    $arcGroup=$svgDocument.SelectSingleNode("//*[@data-kind='configured-arc-gear' and @data-map-label='A01']")
    $arcPath=$arcGroup.SelectSingleNode("*[@data-kind='configured-arc-centerline']")
    $arcPivot=$arcGroup.SelectSingleNode("*[@data-kind='configured-arc-pivot']")
    $arcStart=$arcGroup.SelectSingleNode("*[@data-kind='configured-arc-start']")
    $arcEnd=$arcGroup.SelectSingleNode("*[@data-kind='configured-arc-end']")
    if($arcGroup.GetAttribute('data-id')-cne'CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear'-or
        $arcGroup.GetAttribute('data-object-id')-cne$arcGroup.GetAttribute('data-id')-or
        $arcGroup.GetAttribute('data-object-label')-cne$deviceLabels['CanyonBasin/Gameplay/Mechanisms/UpperGalleryUnderArcGear']-or
        $arcPath.GetAttribute('d')-cne'M1115.333 1472.733 A80.111 80.111 0 0 0 1166 1472.733'-or
        $arcPivot.GetAttribute('cx')-cne'1140.667'-or$arcPivot.GetAttribute('cy')-cne'1396.733'-or
        $arcStart.GetAttribute('cx')-cne'1115.333'-or$arcStart.GetAttribute('cy')-cne'1472.733'-or$arcStart.GetAttribute('r')-cne'7.6'-or
        $arcEnd.GetAttribute('cx')-cne'1166'-or$arcEnd.GetAttribute('cy')-cne'1472.733'-or$arcEnd.GetAttribute('r')-cne'7.6'-or
        $arcGroup.SelectNodes('.//*[local-name()="polygon" or local-name()="rect"]').Count-ne0-or
        $arcGroup.SelectNodes('.//*[local-name()="path"]').Count-ne1-or$arcGroup.SelectNodes('.//*[local-name()="circle"]').Count-ne3){
        throw 'Independent configured arc curve/pivot/endpoint projection mismatch'
    }
    function Table([string]$title,$rows,[string[]]$columns){
        $out=[Text.StringBuilder]::new();[void]$out.Append('<details><summary>'+(Encode-MapText $title)+' ('+@($rows).Count+')</summary><table><thead><tr>')
        foreach($column in $columns){[void]$out.Append('<th>'+(Encode-MapText $column)+'</th>')}
        [void]$out.Append('</tr></thead><tbody>')
        foreach($row in $rows){
            [void]$out.Append('<tr>')
            foreach($column in $columns){
                $property=$row.PSObject.Properties[$column];$value=if($null-ne$property){$property.Value}else{$null}
                $shown=if($null-eq$value){'—'}elseif($value-is[string]-or$value-is[ValueType]){[string]$value}else{$value | ConvertTo-Json -Depth 12 -Compress}
                [void]$out.Append('<td>'+(Encode-MapText $shown)+'</td>')
            }
            [void]$out.Append('</tr>')
        }
        [void]$out.Append('</tbody></table></details>');return $out.ToString()
    }
    function LabelRow($row,[string]$label){
        $copy=[ordered]@{mapLabel=$label}
        foreach($property in $row.PSObject.Properties){$copy[$property.Name]=$property.Value}
        return [pscustomobject]$copy
    }
    $enemyRows=@(for($i=0;$i-lt$map.enemies.Count;$i++){LabelRow $map.enemies[$i] ('E'+($i+1).ToString('00'))})
    $fragmentRows=@(for($i=0;$i-lt$map.fragments.Count;$i++){LabelRow $map.fragments[$i] ('F'+($i+1).ToString('00'))})
    $volumeRows=@(for($i=0;$i-lt$map.configuredHazardVolumes.Count;$i++){
        $row=LabelRow $map.configuredHazardVolumes[$i] ('H'+($i+1).ToString('00'))
        $row | Add-Member -NotePropertyName objectMapLabel -NotePropertyValue $deviceLabels[$row.objectId]
        $row
    })
    $railRows=@(for($i=0;$i-lt$map.configuredMovingGears.Count;$i++){
        $row=LabelRow $map.configuredMovingGears[$i] ('R'+($i+1).ToString('00'))
        # Keep the linkage immediately after R01 so the table's association is explicit.
        $linked=[ordered]@{mapLabel=$row.mapLabel;objectMapLabel=$deviceLabels[$row.objectId]}
        foreach($property in $row.PSObject.Properties){if($property.Name-cne'mapLabel'){$linked[$property.Name]=$property.Value}}
        [pscustomobject]$linked
    })
    $arcRows=@(for($i=0;$i-lt$map.configuredArcGears.Count;$i++){
        $row=LabelRow $map.configuredArcGears[$i] ('A'+($i+1).ToString('00'))
        $linked=[ordered]@{mapLabel=$row.mapLabel;objectMapLabel=$deviceLabels[$row.objectId]}
        foreach($property in $row.PSObject.Properties){if($property.Name-cne'mapLabel'){$linked[$property.Name]=$property.Value}}
        [pscustomobject]$linked
    })
    $objectRows=@(foreach($row in $map.objects){
        $copy=LabelRow $row $(if($deviceLabels.ContainsKey($row.id)){$deviceLabels[$row.id]}else{'表列，不在总图编号'})
        $linked=@($volumeRows | Where-Object objectId -CEQ $row.id)
        $copy | Add-Member -NotePropertyName configuredVolumeLabel -NotePropertyValue $(if($linked.Count){$linked[0].mapLabel}else{'—'})
        $linkedRail=@($railRows | Where-Object objectId -CEQ $row.id)
        $copy | Add-Member -NotePropertyName configuredRailLabel -NotePropertyValue $(if($linkedRail.Count){$linkedRail[0].mapLabel}else{'—'})
        $linkedArc=@($arcRows | Where-Object objectId -CEQ $row.id)
        $copy | Add-Member -NotePropertyName configuredArcLabel -NotePropertyValue $(if($linkedArc.Count){$linkedArc[0].mapLabel}else{'—'})
        $copy
    })
    $typeRows=@(foreach($group in $objectTypeGroups){[pscustomobject]@{type=$group.Name;records=$group.Count}})
    $html='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>KNIGHT 当前配置地图 v331</title>'+
        '<style>body{margin:24px;background:#0b141e;color:#dce5ee;font:16px/1.6 system-ui,Microsoft YaHei,sans-serif}a{color:#9dc8ed}img{width:100%;height:auto}summary{cursor:pointer;padding:12px 0}table{border-collapse:collapse;width:100%;margin-bottom:24px}td,th{padding:8px;text-align:left;border-bottom:1px solid #2d4657;overflow-wrap:anywhere;vertical-align:top}details{overflow:auto}code{overflow-wrap:anywhere}</style>'+
        '<h1>KNIGHT 当前关卡配置图 · v331</h1><p>'+ $map.solids.Count+'结构固体；76普通配置目标（26原遭遇＋50常驻）；9残片；3检查点；'+$map.regions.Count+'功能域。静态锚点不是动态危险扫掠，配置目标不是同屏或每局必杀数。本图不作路线/资源/美术验收。</p>'+
        '<p><a href="current-map.svg">打开可缩放总图</a> · <a href="current-map.png">打开PNG</a> · <a href="current-map.json">完整来源数据</a>。下表含替代出生位置、真正的常驻巡游域/路径与源场景；原遭遇arena不冒称巡逻域。原26保留场景类名，新增50有族系/角色名。</p>'+
        '<img src="current-map.svg" alt="当前关卡的实际结构地形与配置点位，逐点数据在下方表格">'+
        '<p>Boss是配置召唤点。ECHO是附随指导身份，没有固定刷新点；独立友方身体与商人为0。M编号是机关/门/补给等对象锚点，不是独立陷阱计数；外观组件不计为独立机关，挑战名称不是质量验收。另有'+$traversalCount+'通行段、'+$visualCount+'外观组件。图不替代游戏内迷雾小地图，不提前泄露给玩家。</p>'+
        '<p>H01为已有熔炉M锚点的静态配置范围，不新增对象或结构固体，不证明实时激活/动态扫掠。配置为每0.1秒尝试最大生命值接触伤害，不可格挡，但仍经生命值与无敌帧管线处理；不是触碰即死保证。HTML列出原始corners、objectId及对应M编号。</p>'+
        '<p>R01关联一个移动齿轮M锚点，只画真实配置的起终点半径圆与中心连线，不画AABB伤害区。半径24、周期3.2秒、伤害2、接触间隔0.5秒；envelope [3376,3526,48,128]仅为配置轨道的保守轴对齐包围盒（conservative-aabb-not-damage-shape），不是实际伤害形状、像素覆盖或运行时路线证明（configured-rail-not-runtime-route-proof）。</p>'+
        (Table '普通敌人目标（mapLabel对应总图E编号）' $enemyRows @('mapLabel','id','name','cohort','family','archetype','sector','region','point','scene','batch','alternatePoint','territory','patrol','arena','activation','status'))+
        (Table '机关/门/补给/通行段及外观组件（mapLabel对应M编号）' $objectRows @('mapLabel','configuredVolumeLabel','configuredRailLabel','configuredArcLabel','id','type','sector','point','path','ids','status'))+
        (Table '配置危险范围（H编号关联已有M锚点；不是实时激活）' $volumeRows @('mapLabel','objectMapLabel','id','objectId','type','sector','path','corners','pulseSeconds','damagePolicy','unblockable','respectsHealthInvulnerability','status'))+
        (Table '移动齿轮配置轨道（R编号关联M锚点；保守包围盒不是伤害形状）' $railRows @('mapLabel','objectMapLabel','id','objectId','type','sector','path','start','end','radius','periodSeconds','damage','contactIntervalSeconds','envelope','envelopePolicy','status'))+
        '<p>A01是新增工坊悬吊齿轮：画真实支点、signed圆弧与两端半径圆，不用直线弦替代弧，不把保守AABB画成伤害区。电机周期驱动；原阀门关闭后可低走通过，高跳有齿轮风险。仅配置图，不是普通声画、自然路线或美术验收。</p>'+
        (Table '悬吊齿轮配置弧线（A编号关联M锚点）' $arcRows @('mapLabel','objectMapLabel','id','objectId','type','sector','path','start','pivot','end','swingRadians','radius','periodSeconds','damage','contactIntervalSeconds','envelope','envelopePolicy','status'))+
        (Table '对象类型分组（记录数不是独立陷阱或质量计数）' $typeRows @('type','records'))+
        (Table '残片（mapLabel对应F编号）' $fragmentRows @('mapLabel','id','sector','point','optional','path'))+
        (Table '检查点配置' $map.checkpoints @('id','point'))+
        (Table '抓取面材质（不是实时可用性）' $map.surfaces @('id','point','material','status'))+
        (Table '功能域' $map.regions @('id','sector','bounds','optional','rest'))+
        (Table '拓扑门禁（不是已验证移动轨迹）' $map.edges @('from','to','bidirectional','gate','mechanism','shortcut','status'))+
        (Table 'Boss配置点' $map.bosses @('id','point','scene','status'))+
        (Table '读取来源SHA256' $map.sources @('path','sha256'))+
        '<p>数据SHA256：<code>'+$inputSha+'</code><br>地形Revision：<code>'+$map.terrainRevision+'</code></p></html>'
    $stream=[IO.MemoryStream]::new()
    try{$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$png=$stream.ToArray()}finally{$stream.Dispose()}
    if($bitmap.Width-ne3200-or$bitmap.Height-ne1800-or$png.Length-lt1000){throw 'PNG composition invalid'}
    # Publish only after all in-memory composition checks. Never overwrite an older version.
    [void](New-Item -ItemType Directory -Path $destination)
    $original=Join-Path $destination 'original';$presentation=Join-Path $destination 'presentation'
    [void](New-Item -ItemType Directory -Path $original);[void](New-Item -ItemType Directory -Path $presentation)
    function SaveNew([string]$path,[byte[]]$bytes){
        $file=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try{$file.Write($bytes,0,$bytes.Length)}finally{$file.Dispose()}
    }
    $utf8=[Text.UTF8Encoding]::new($false)
    $files=@{'current-map.json'=[IO.File]::ReadAllBytes([IO.Path]::GetFullPath($InputJson));
        'current-map.svg'=$utf8.GetBytes($svgText);'current-map.png'=$png;'current-map.html'=$utf8.GetBytes($html)}
    if($observation){$files['current-observation.json']=$utf8.GetBytes(($observation | ConvertTo-Json -Depth 8))}
    foreach($name in $files.Keys){SaveNew (Join-Path $original $name) $files[$name];SaveNew (Join-Path $presentation $name) $files[$name]}
    SaveNew (Join-Path $original 'render-source.ps1') ([IO.File]::ReadAllBytes($PSCommandPath))
    $bindings=[ordered]@{version='v331';status='static-configuration-not-gameplay-or-art-acceptance';nativeCase=[IO.Path]::GetFileName($nativeCase);
        sourceJsonSha256=$inputSha;nativeSourceManifestSha256=(Get-FileHash -LiteralPath (Join-Path $nativeCase 'source-before.txt') -Algorithm SHA256).Hash;
        nativeBinaryManifestSha256=(Get-FileHash -LiteralPath (Join-Path $nativeCase 'fresh-binaries.txt') -Algorithm SHA256).Hash;
        rendererSha256=$rendererSha;terrainRevision=$map.terrainRevision;pngWidth=3200;pngHeight=1800;
        solids=$map.solids.Count;regions=$map.regions.Count;configuredHazardVolumes=$map.configuredHazardVolumes.Count;configuredMovingGears=$map.configuredMovingGears.Count;configuredArcGears=$map.configuredArcGears.Count;
        movingGearStatus='configured-rail-not-runtime-route-proof';movingGearEnvelopePolicy='conservative-aabb-not-damage-shape';
        arcGearStatus='configured-arc-not-runtime-route-proof';arcGearEnvelopePolicy='conservative-aabb-not-damage-shape';
        ordinaryTargets=76;residentTargets=50;originalEncounterSlots=26;fragments=9;logicalAnchors=$logicObjects.Count;
        traversalSegments=$traversalCount;visualComponents=$visualCount;objectTypeGroups=$typeRows;
        sourceRecords=$map.objects.Count;humanAccepted=$false;observation=$observation;files=@()}
    $bindings.files=@(foreach($folder in @('original','presentation')){foreach($file in Get-ChildItem -LiteralPath (Join-Path $destination $folder) -File){
        [ordered]@{path=$folder+'/'+$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash}
    }})
    foreach($name in $files.Keys){
        if((Get-FileHash -LiteralPath (Join-Path $original $name)).Hash-cne(Get-FileHash -LiteralPath (Join-Path $presentation $name)).Hash){throw 'Presentation copy differs'}
    }
    SaveNew (Join-Path $destination 'source-version-status.json') ($utf8.GetBytes(($bindings | ConvertTo-Json -Depth 12)))
    SaveNew (Join-Path $destination 'README.md') ($utf8.GetBytes("# 当前配置地图 v331`n`n原件在original，展示副本在presentation，逐文件SHA与来源在source-version-status.json。`n`n本图不作路线/资源/美术验收；机关锚点不是独立陷阱数或动态扫掠，挑战名称不是质量验收。普通目标76不等于同屏数量。保留原26替代出生点，不重复计数。`n`nH01是已有M锚点的熔炉配置范围，不新增结构或陷阱；不证明实时激活。每0.1秒尝试最大生命值接触伤害，不可格挡，仍尊重生命/无敌帧管线，不是触碰即死保证。`n`nR01仅画移动齿轮配置起终点半径圆和连线，关联真实M锚点。保守envelope只列在HTML，不是伤害形状、像素覆盖或运行时路线证明；不新增结构固体。`n`nHTML有完整敌人/机关/配置范围/配置轨道/类型分组/残片/检查点/抓取材质/拓扑门禁清单。如有红点，它是一次测试的受伤/死亡观察；以current-observation.json剩余生命为准，不是新固定陷阱。`n"))
    Write-Output ('CURRENT_MAP_RENDER_OK version=v331 solids='+$map.solids.Count+' regions='+$map.regions.Count+' configured_volumes='+$map.configuredHazardVolumes.Count+' configured_rails='+$map.configuredMovingGears.Count+' targets=76 fragments=9 png=3200x1800 copies_identical=true native_behavior=false path='+$destination)
}finally{
    $graphics.Dispose();$bitmap.Dispose();foreach($font in $fonts.Values){$font.Dispose()}
}
