param(
    [switch]$Commit,
    [switch]$Push
)

$ErrorActionPreference = 'Stop'

# Compute UTC+8 timestamp in a filename-safe format
$timestamp = (Get-Date).ToUniversalTime().AddHours(8).ToString('yyyy-MM-dd_HH-mm-ss-\U\T\C+8')
$repoRoot = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $repoRoot 'docs/chat-history'
New-Item -ItemType Directory -Path $dir -Force | Out-Null

$file = Join-Path $dir ("$timestamp.md")

# Gather recent git commits (last 10)
$recentCommits = git log --oneline -n 10 2>$null

# Compose content
$content = @()
$content += "# Chat History — $((Get-Date).ToUniversalTime().AddHours(8).ToString('yyyy-MM-dd HH:mm:ss')) (UTC+8)"
$content += ''
$content += 'This document records the recent discussion and actions.'
$content += ''
$content += '## Recent commits'
if ($recentCommits) {
    $content += ($recentCommits | ForEach-Object { "- $_" })
} else {
    $content += '- (no recent commits found or git not available)'
}
$content += ''
$content += '## Notes'
$content += '- Created via scripts/new-chat-history.ps1'

Set-Content -Path $file -Value ($content -join [Environment]::NewLine) -Encoding UTF8

Write-Host "Created: $file"

if ($Commit) {
    git add -- "$file"
    git commit -m "docs(history): add chat history $timestamp"
    Write-Host "Committed chat history: $timestamp"
}

if ($Push) {
    git push
    Write-Host 'Pushed to remote.'
}
