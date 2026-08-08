$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $projectRoot "backend\src\OnlineSupermarket.Api\OnlineSupermarket.Api.csproj"
$outputDirectory = Join-Path $projectRoot "docs\api"
$outputPath = Join-Path $outputDirectory "openapi.json"
$endpoint = "http://127.0.0.1:5077/openapi/v1.json"
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
$previousConnectionString = $env:ConnectionStrings__DefaultConnection
$apiProcess = $null

try {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__DefaultConnection = "Server=localhost;Port=3306;Database=openapi_export;User=export;Password=export"
    New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    $apiProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run", "--no-launch-profile", "--project", $apiProject, "--urls", "http://127.0.0.1:5077" `
        -WorkingDirectory $projectRoot `
        -WindowStyle Hidden `
        -PassThru

    $ready = $false
    foreach ($attempt in 1..60) {
        try {
            Invoke-WebRequest -UseBasicParsing -Uri $endpoint -OutFile $outputPath
            $ready = $true
            break
        }
        catch {
            Start-Sleep -Milliseconds 250
        }
    }

    if (-not $ready) {
        throw "OpenAPI endpoint did not become ready at $endpoint."
    }

    Write-Output "Exported OpenAPI contract to $outputPath"
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
    }

    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
    $env:ConnectionStrings__DefaultConnection = $previousConnectionString
}
