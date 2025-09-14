param(
    [switch]$Commit,
    [switch]$Push,
    [string]$Title,
    [string]$Summary,
    [string]$NotesPath,
    [int]$Commits = 10,
    [switch]$FromClipboard,
    [string]$DiscussionPath
)

$ErrorActionPreference = 'Stop'

# Compute UTC+8 timestamp in a filename-safe format
$timestamp = (Get-Date).ToUniversalTime().AddHours(8).ToString('yyyy-MM-dd_HH-mm-ss-\U\T\C+8')
$repoRoot = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $repoRoot 'docs/chat-history'
New-Item -ItemType Directory -Path $dir -Force | Out-Null

$file = Join-Path $dir ("$timestamp.md")

# Gather repo snapshot
$branch = git rev-parse --abbrev-ref HEAD 2>$null
$head = git log -1 --oneline 2>$null
$status = git status --porcelain=v1 -u 2>$null
$recentCommits = git log --oneline -n $Commits 2>$null

# Optional notes content
$notesContent = $null
if ($NotesPath) {
    $np = Resolve-Path -ErrorAction SilentlyContinue -- $NotesPath
    if ($np) {
        $notesContent = Get-Content -Raw -- $np
    }
} else {
    $defaultNotes = Join-Path $dir 'notes.md'
    if (Test-Path $defaultNotes) {
        $notesContent = Get-Content -Raw -- $defaultNotes
    }
}

# Optional full discussion transcript
$discussionText = $null
if ($FromClipboard) {
    try { $discussionText = Get-Clipboard -Raw } catch { }
}
if (-not $discussionText -and $DiscussionPath) {
    $dp = Resolve-Path -ErrorAction SilentlyContinue -- $DiscussionPath
    if ($dp) { $discussionText = Get-Content -Raw -- $dp }
}

# Compose content
$content = @()
$content += "# Chat History — $((Get-Date).ToUniversalTime().AddHours(8).ToString('yyyy-MM-dd HH:mm:ss')) (UTC+8)"
$content += ''
$content += 'This document records the recent discussion and actions.'
$content += ''

# Discussion section
if ($Title) { $content += "## $Title" } else { $content += '## Discussion' }
if ($Summary) {
    $content += $Summary
} else {
    $content += '_No summary provided. Use -Summary "..." or -NotesPath to include discussion details._'
}
$content += ''
if ($notesContent) {
    $content += '### Notes'
    $content += $notesContent
    $content += ''
}
if ($discussionText) {
    $content += '### Transcript'
    $content += '```text'
    $content += ($discussionText -split "`n")
    $content += '```'
    $content += ''
}

# Repo snapshot
$content += '## Repo snapshot'
if ($branch) { $content += "- Branch: $branch" }
if ($head) { $content += "- HEAD: $head" }
$content += ''

# Working tree status
$content += '## Working tree status'
if ($status) {
    $content += '```'
    $content += ($status -split "`n")
    $content += '```'
} else {
    $content += '- clean'
}
$content += ''

# Recent commits
$content += "## Recent commits (last $Commits)"
if ($recentCommits) {
    $content += ($recentCommits | ForEach-Object { "- $_" })
} else {
    $content += '- (no recent commits found or git not available)'
}
$content += ''
$content += '---'
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
