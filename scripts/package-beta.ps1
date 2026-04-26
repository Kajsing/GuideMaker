$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$timestamp = Get-Date -Format "yyyyMMdd-HHmm"
$artifactsDirectory = Join-Path $repoRoot "artifacts"
$packageName = "GuideMaker-beta-$timestamp-win-x64"
$publishDirectory = Join-Path $artifactsDirectory $packageName
$zipPath = Join-Path $artifactsDirectory "$packageName.zip"

New-Item -ItemType Directory -Force -Path $artifactsDirectory | Out-Null

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}

dotnet publish `
    (Join-Path $repoRoot "src\GuideMaker.App\GuideMaker.App.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    --output $publishDirectory

Copy-Item `
    -LiteralPath (Join-Path $repoRoot "docs\beta-testing.md") `
    -Destination (Join-Path $publishDirectory "BETA_TESTING.md") `
    -Force

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $zipPath -Force

Write-Host "Beta package created:"
Write-Host $zipPath
