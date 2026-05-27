[CmdletBinding(SupportsShouldProcess = $true)]
param(
  [string]$Destination
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Source = Join-Path $Root "skills"

if (-not (Test-Path -LiteralPath $Source)) {
  throw "Missing skills directory: $Source"
}

if (-not $Destination) {
  if ($env:CODEX_HOME) {
    $Destination = Join-Path $env:CODEX_HOME "skills"
  } else {
    $Destination = Join-Path $HOME ".codex\skills"
  }
}

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$SkillNames = @(
  "godot-asset-interview",
  "godot-asset-review",
  "godot-asset-execute"
)

foreach ($Name in $SkillNames) {
  $From = Join-Path $Source $Name
  $To = Join-Path $Destination $Name

  if (-not (Test-Path -LiteralPath $From)) {
    throw "Missing skill source: $From"
  }

  if ($PSCmdlet.ShouldProcess($To, "Install $Name")) {
    if (Test-Path -LiteralPath $To) {
      Remove-Item -LiteralPath $To -Recurse -Force
    }

    Copy-Item -LiteralPath $From -Destination $To -Recurse
    Write-Host "Installed $Name -> $To"
  }
}

if ($WhatIfPreference) {
  Write-Host "Preview complete. No files were changed."
} else {
  Write-Host "Done. Restart Codex or open a new session to reload skill metadata."
}
