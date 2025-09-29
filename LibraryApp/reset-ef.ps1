$ErrorActionPreference = "Stop"

dotnet tool update -g dotnet-ef --version 8.0.10 | Out-Null
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.10 | Out-Null
dotnet add package Microsoft.EntityFrameworkCore.Tools     --version 8.0.10 | Out-Null
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.10 | Out-Null
dotnet add package Microsoft.AspNetCore.Identity.UI                 --version 8.0.10 | Out-Null

if (Test-Path "./Migrations") {
    Write-Host "Deleting ./Migrations ..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force "./Migrations"
}

Write-Host "Dropping database..." -ForegroundColor Yellow
dotnet ef database drop --force

dotnet clean
dotnet restore
dotnet build

Write-Host "Creating InitialCreate migration..." -ForegroundColor Yellow
dotnet ef migrations add InitialCreate

Write-Host "Updating database..." -ForegroundColor Yellow
dotnet ef database update

Write-Host "`Clean migration created and executed" -ForegroundColor Green
