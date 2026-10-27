param([string]$Directory, [string]$Compare, [string]$Python = 'python')
$ErrorActionPreference = 'Stop'
if (-not $Directory) { $Directory = Join-Path $PSScriptRoot '../../Data/Assets' }
$arguments = @((Join-Path $PSScriptRoot 'validate_assets.py'), $Directory)
if ($Compare) { $arguments += @('--compare', $Compare) }
& $Python @arguments
if ($LASTEXITCODE -ne 0) { throw 'Asset validation failed.' }
