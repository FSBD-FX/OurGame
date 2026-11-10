[CmdletBinding()]
param(
    [string]$AuditRoot,
    [string]$BaseAuditRoot,
    [switch]$SkipBuild
)

# The no-root route preserves BASE's complete display-backed production audit.  These
# literals document (and are executed by) audit-prologue-base-hardgates.ps1:
# '--fixed-fps', '60' / '--display-driver', 'windows'
# -Marker 'PROLOGUE_VISUAL_EVIDENCE_OK'
# -Marker 'PROLOGUE_VISUAL_PERFORMANCE_OK'
# -Script 'res://tests/presentation/PrologueVisualAuditTask6.cs'
# Assert-ExactLogMarker -Path $consolePath -Marker $Marker
# Assert-NoEngineLogErrors -Paths @($log, $consolePath)
# PROLOGUE_DISPLAY_BACKED_AUDIT_OK runs=4 driver=windows

$ErrorActionPreference = 'Stop'
$baseAudit = Join-Path $PSScriptRoot 'audit-prologue-base-hardgates.ps1'
$focusedAudit = Join-Path $PSScriptRoot 'audit-prologue-task6-focused.ps1'
if ([string]::IsNullOrWhiteSpace($AuditRoot)) {
    if ([string]::IsNullOrWhiteSpace($BaseAuditRoot)) {
        throw 'Production BASE audit requires -BaseAuditRoot outside the candidate worktree.'
    }
    & $baseAudit -AuditRoot $BaseAuditRoot -SkipBuild:$SkipBuild
    exit $LASTEXITCODE
}

if (-not [string]::IsNullOrWhiteSpace($BaseAuditRoot)) {
    throw 'Production audit accepts either -AuditRoot (focused) or -BaseAuditRoot (BASE), not both.'
}

& $focusedAudit -AuditRoot $AuditRoot -SkipBuild:$SkipBuild
exit $LASTEXITCODE
