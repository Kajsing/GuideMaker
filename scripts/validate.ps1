$ErrorActionPreference = "Stop"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -and (Test-Path "C:\Program Files\dotnet\dotnet.exe")) {
    $env:Path = "C:\Program Files\dotnet;$env:Path"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found on PATH. Install .NET 8 SDK or add dotnet.exe to PATH, then rerun this script."
}

function Invoke-DotNet {
    dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $args failed with exit code $LASTEXITCODE."
    }
}

Invoke-DotNet restore .\GuideMaker.sln
Invoke-DotNet build .\GuideMaker.sln --configuration Release --no-restore
Invoke-DotNet test .\GuideMaker.sln --configuration Release --no-restore
