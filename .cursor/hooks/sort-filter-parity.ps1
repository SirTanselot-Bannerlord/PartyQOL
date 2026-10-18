# Sort/Filter settings parity: track paired window edits, force follow-up on stop if one-sided.
# Allowlist: put parity-ok: <reason> in the edited file (XML comment or // comment).

$ErrorActionPreference = 'Stop'

function Write-HookJson([hashtable]$obj) {
    ($obj | ConvertTo-Json -Compress -Depth 6)
}

function Get-WorkspaceRoot([object]$payload) {
    if ($payload.workspace_roots -and @($payload.workspace_roots).Count -gt 0) {
        return [string]@($payload.workspace_roots)[0]
    }
    if ($env:CURSOR_PROJECT_DIR) { return $env:CURSOR_PROJECT_DIR }
    return (Get-Location).Path
}

function Normalize-RelPath([string]$root, [string]$absolutePath) {
    if ([string]::IsNullOrWhiteSpace($absolutePath)) { return $null }
    $rootFull = [System.IO.Path]::GetFullPath($root).TrimEnd('\', '/')
    $fileFull = [System.IO.Path]::GetFullPath($absolutePath)
    if (-not $fileFull.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $null
    }
    return $fileFull.Substring($rootFull.Length).TrimStart('\', '/').Replace('\', '/')
}

function Get-ParityPairs {
    @(
        @{
            Id = 'settings-prefab'
            Sort = 'GUI/Prefabs/Popups/PartyQOLSortSettings.xml'
            Filter = 'GUI/Prefabs/Popups/PartyQOLFilterSettings.xml'
        },
        @{
            Id = 'settings-vm'
            Sort = 'UI/Popups/SortSettingsVM.cs'
            Filter = 'UI/Popups/FilterSettingsVM.cs'
        }
    )
}

function Test-HasParityOkComment([string]$absolutePath) {
    if (-not (Test-Path -LiteralPath $absolutePath)) { return $false }
    $text = Get-Content -LiteralPath $absolutePath -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrEmpty($text)) { return $false }
    return [bool]($text -match '(?i)parity-ok\s*:')
}

function Test-PathTouched([string[]]$touched, [string]$rel) {
    foreach ($t in $touched) {
        if ([string]::Equals($t, $rel, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Read-State([string]$statePath) {
    if (-not (Test-Path -LiteralPath $statePath)) {
        return @{ touched = @() }
    }
    $raw = Get-Content -LiteralPath $statePath -Raw -ErrorAction SilentlyContinue
    if ([string]::IsNullOrWhiteSpace($raw)) {
        return @{ touched = @() }
    }
    $obj = $raw | ConvertFrom-Json
    $list = @()
    if ($null -ne $obj.touched) {
        foreach ($t in @($obj.touched)) {
            if ($t) { $list += [string]$t }
        }
    }
    return @{ touched = $list }
}

function Write-State([string]$statePath, [string]$conversationId, [string[]]$touched) {
    $dir = Split-Path -Parent $statePath
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    $payload = @{
        conversation_id = $conversationId
        touched         = @($touched)
    }
    ($payload | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath $statePath -Encoding utf8
}

$stdin = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($stdin)) {
    Write-HookJson @{}
    exit 0
}

try {
    $payload = $stdin | ConvertFrom-Json
}
catch {
    Write-HookJson @{}
    exit 0
}

$event = [string]$payload.hook_event_name
$conversationId = [string]$payload.conversation_id
if ([string]::IsNullOrWhiteSpace($conversationId)) {
    $conversationId = 'unknown'
}

$root = Get-WorkspaceRoot $payload
$statePath = Join-Path $root ".cursor\hooks\state\sort-filter-parity-$conversationId.json"
$pairs = Get-ParityPairs

if ($event -eq 'afterFileEdit') {
    $rel = Normalize-RelPath $root ([string]$payload.file_path)
    if ($rel) {
        $isPaired = $false
        foreach ($pair in $pairs) {
            if ([string]::Equals($rel, $pair.Sort, [System.StringComparison]::OrdinalIgnoreCase) -or
                [string]::Equals($rel, $pair.Filter, [System.StringComparison]::OrdinalIgnoreCase)) {
                $isPaired = $true
                break
            }
        }
        if ($isPaired) {
            $state = Read-State $statePath
            $touched = @($state.touched)
            if (-not (Test-PathTouched $touched $rel)) {
                $touched += $rel
                Write-State $statePath $conversationId $touched
            }
        }
    }
    Write-HookJson @{}
    exit 0
}

if ($event -eq 'stop') {
    $status = [string]$payload.status
    if ($status -ne 'completed') {
        Write-HookJson @{}
        exit 0
    }

    $state = Read-State $statePath
    $touched = @($state.touched)
    if ($touched.Count -eq 0) {
        Write-HookJson @{}
        exit 0
    }

    $violations = New-Object System.Collections.Generic.List[string]
    foreach ($pair in $pairs) {
        $sortTouched = Test-PathTouched $touched $pair.Sort
        $filterTouched = Test-PathTouched $touched $pair.Filter
        if ($sortTouched -eq $filterTouched) { continue }

        $editedRel = if ($sortTouched) { $pair.Sort } else { $pair.Filter }
        $missingRel = if ($sortTouched) { $pair.Filter } else { $pair.Sort }
        $editedAbs = Join-Path $root ($editedRel -replace '/', '\')

        if (Test-HasParityOkComment $editedAbs) { continue }

        $violations.Add(('edited {0} without {1}' -f $editedRel, $missingRel)) | Out-Null
    }

    if ($violations.Count -eq 0) {
        if (Test-Path -LiteralPath $statePath) {
            Remove-Item -LiteralPath $statePath -Force -ErrorAction SilentlyContinue
        }
        Write-HookJson @{}
        exit 0
    }

    $nl = [Environment]::NewLine
    $body = 'Sort/Filter parity incomplete for this turn.' + $nl
    foreach ($v in $violations) {
        $body += ('- {0}' -f $v) + $nl
    }
    $body += 'Mirror the same change in the peer file before finishing.' + $nl
    $body += 'If this is intentionally domain-only, add an allowlist comment to the edited file: parity-ok: <reason>' + $nl
    $xmlEx = '<!-- parity-ok: domain-only -->'
    $csEx = '// parity-ok: domain-only'
    $body += ('Examples: {0} (XML) or {1} (C#).' -f $xmlEx, $csEx)

    Write-HookJson @{ followup_message = $body }
    exit 0
}

Write-HookJson @{}
exit 0
