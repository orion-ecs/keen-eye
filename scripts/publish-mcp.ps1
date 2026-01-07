# Publish MCP TestBridge server
# Usage: .\scripts\publish-mcp.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$McpProject = Join-Path $ProjectRoot "tools\KeenEyes.Mcp.TestBridge\KeenEyes.Mcp.TestBridge.csproj"
$OutputDir = Join-Path $ProjectRoot ".mcp"

Write-Host "Publishing MCP TestBridge server..." -ForegroundColor Cyan
Write-Host "  Project: $McpProject"
Write-Host "  Output:  $OutputDir"
Write-Host ""

dotnet publish $McpProject -c Release -o $OutputDir --nologo

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "MCP server published successfully!" -ForegroundColor Green
    Write-Host "Executable: $OutputDir\KeenEyes.Mcp.TestBridge.exe"
} else {
    Write-Host ""
    Write-Host "Failed to publish MCP server" -ForegroundColor Red
    exit 1
}
