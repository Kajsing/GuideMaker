$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -and (Test-Path "C:\Program Files\dotnet\dotnet.exe")) {
    $env:Path = "C:\Program Files\dotnet;$env:Path"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found on PATH. Install .NET 8 SDK or add dotnet.exe to PATH, then rerun this script."
}

dotnet test .\GuideMaker.sln --configuration Debug
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE."
}
