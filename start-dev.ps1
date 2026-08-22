<#
Starts both the ArcanumBudget API and the Angular client for local development.
Each runs in its own PowerShell window so their logs stay separate and both
processes keep running independently of this script.
#>

$root = $PSScriptRoot
$apiPath = Join-Path $root "src\ArcanumBudget.Api"
$clientPath = Join-Path $root "client"

Write-Host "Starting API (https://localhost:61670 / http://localhost:61671)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$apiPath'; dotnet run" -WindowStyle Normal

Write-Host "Starting Angular client (http://localhost:4200)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$clientPath'; npm start" -WindowStyle Normal

Write-Host "Waiting for the client to come up, then opening the landing page..." -ForegroundColor Cyan
Start-Sleep -Seconds 20
Start-Process "http://localhost:4200"
