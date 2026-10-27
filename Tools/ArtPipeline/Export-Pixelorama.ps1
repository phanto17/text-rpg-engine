param(
    [string]$Pixelorama = $env:PIXELORAMA_EXE,
    [string]$OutputDirectory,
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $Pixelorama) {
    $Pixelorama = Join-Path $repo 'Tools/Local/Pixelorama-1.2.3/Pixelorama-Windows-64bit/Pixelorama.exe'
}
if (-not (Test-Path -LiteralPath $Pixelorama -PathType Leaf)) { throw 'Set PIXELORAMA_EXE to the tested Pixelorama executable.' }
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repo ('ArtBuild/export-' + [guid]::NewGuid().ToString('N')) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Use a fresh output directory to prevent accidental overwrites.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$manifest = Get-Content (Join-Path $repo 'ArtSource/pilots.json') -Raw | ConvertFrom-Json
foreach ($asset in ($manifest.assets | Where-Object { $_.source -eq 'pixelorama' })) {
    $source = Join-Path $repo "ArtSource/Pixelorama/$($asset.category)/$($asset.id).pxo"
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing master: $source" }
    $folder = Join-Path $OutputDirectory $asset.category
    New-Item -ItemType Directory -Force -Path $folder | Out-Null
    $output = Join-Path $folder ($asset.id + '.png')
    $info = [Diagnostics.ProcessStartInfo]::new($Pixelorama)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    foreach ($arg in @('--headless', '--quit', '--', $source, '--export', '--spritesheet', '--scale', '1', '--output', $output)) {
        $info.ArgumentList.Add($arg)
    }
    $process = [Diagnostics.Process]::Start($info)
    try {
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(60000)) { $process.Kill($true); throw "Export timed out: $source" }
        $stdout.GetAwaiter().GetResult() | Set-Content (Join-Path $folder ($asset.id + '.stdout.log'))
        $stderr.GetAwaiter().GetResult() | Set-Content (Join-Path $folder ($asset.id + '.stderr.log'))
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $output)) { throw "Export failed: $source; inspect staging logs." }
    } finally { $process.Dispose() }
}
& $Python (Join-Path $PSScriptRoot 'validate_assets.py') $OutputDirectory --source pixelorama
if ($LASTEXITCODE -ne 0) { throw 'Exported asset validation failed.' }
Get-ChildItem -LiteralPath $OutputDirectory -Recurse -Filter '*.png' | Get-FileHash -Algorithm SHA256 |
    Select-Object Path,Hash | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'hashes.json')
Write-Output "Validated exports: $OutputDirectory"
