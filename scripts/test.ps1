$ErrorActionPreference = "Stop"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK was not found on PATH. Install .NET 8 SDK or add dotnet.exe to PATH, then rerun this script."
}

dotnet test .\GuideMaker.sln
