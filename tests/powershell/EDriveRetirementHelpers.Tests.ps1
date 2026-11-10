$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$scope = @((Join-Path $projectRoot '.superpowers/task4-run.ps1')) + @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot '.superpowers/sdd') -Directory | ForEach-Object {
        Get-ChildItem -LiteralPath $_.FullName -File -Filter '*.ps1'
    } | ForEach-Object FullName
) + @(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.ps1' | ForEach-Object FullName)
$failures = [Collections.Generic.List[string]]::new()
foreach ($file in $scope) {
    $tokens = $null; $parseErrors = $null
    $ast = [Management.Automation.Language.Parser]::ParseFile($file, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { $failures.Add("PARSE $file"); continue }
    # Inspect executable strings, not comments or immutable historical receipts.
    foreach ($literal in $ast.FindAll({param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst]}, $true)) {
        # The Task17B CSV retains original source identities. This exact literal is
        # used only as Replace's old-text argument; it is never a filesystem target.
        if ($literal.Value -ceq ('E:' + '/OurGame/Knight/.evidence') -and
            $file.EndsWith('archive-task17b-frozen-media.ps1') -and
            $literal.Parent -is [Management.Automation.Language.InvokeMemberExpressionAst] -and
            $literal.Parent.Member.Value -ceq 'Replace' -and
            $literal.Parent.Arguments[0] -eq $literal) { continue }
        if ($literal.Value -match '^(?:E:[\\/]OurGame[\\/]Knight|G:[\\/]godot)(?:[\\/]|$)') {
            $failures.Add("LEGACY_PATH $file`:$($literal.Extent.StartLineNumber) $($literal.Value)")
        }
    }
}
if ($failures.Count) { throw ($failures -join "`n") }
$title = Get-Content -LiteralPath (Join-Path $projectRoot '.superpowers/sdd/2026-10-18-thruster-player-integration/title-visibility-v001.ps1') -Raw
if ($title -notmatch [regex]::Escape('$sourceBaseline=''G:/knight/历史资料/E-OurGame-Knight/.evidence/october13-stable-claw/prologue-pre-20261019-063826-570-8448e5ee909348acb3a4ab800e354382/source-before.txt''')) { throw 'Historical source baseline identity must remain separate from current project' }
if ($title -notmatch [regex]::Escape("Join-Path 'G:/knight/验证证据/october19-title-visibility'")) { throw 'New output must not enter historical mirror' }
Write-Output "E_RETIREMENT_HELPERS_OK parsed=$($scope.Count) legacyFilesystemRoots=0 historyAndOutputSeparated=true"
