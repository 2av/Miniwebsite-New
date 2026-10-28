# Three upload folders:
#   publish\API       .NET API
#   publish\Admin     admin-web
#   publish\frontend  user-web
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$publish = Join-Path $repo 'publish'
$apiOut = Join-Path $publish 'API'
$adminOut = Join-Path $publish 'Admin'
$frontOut = Join-Path $publish 'frontend'

foreach ($dir in @($apiOut, $adminOut, $frontOut, (Join-Path $publish 'site'))) {
  if (Test-Path $dir) {
    Write-Host "Cleaning $dir"
    Remove-Item $dir -Recurse -Force
  }
}

Write-Host "Building user site (frontend)"
Push-Location (Join-Path $repo 'user-web')
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }
Pop-Location
New-Item -ItemType Directory -Force -Path $frontOut | Out-Null
Copy-Item (Join-Path $repo 'user-web\dist\*') $frontOut -Recurse -Force

Write-Host "Building admin site"
Push-Location (Join-Path $repo 'admin-web')
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }
Pop-Location
New-Item -ItemType Directory -Force -Path $adminOut | Out-Null
Copy-Item (Join-Path $repo 'admin-web\dist\*') $adminOut -Recurse -Force

Write-Host "Publishing API"
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet publish (Join-Path $repo 'dotnet\src\MiniWebsite.Api\MiniWebsite.Api.csproj') -c Release -o $apiOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Ready under $publish"
Write-Host "  API       -> upload as the API site (https://api.sharebizz.com)"
Write-Host "  Admin     -> upload as the admin site"
Write-Host "  frontend  -> upload as the user site"
