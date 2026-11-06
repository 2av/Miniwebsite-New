# Three upload folders, one for each domain:
#   publish\API       API site (https://api.sharebizz.com)
#   publish\Admin     admin domain, at the site root
#   publish\frontend  user domain, at the site root
# Admin and the user site are built to call https://api.sharebizz.com.
$ErrorActionPreference = 'Stop'
$repo = $PSScriptRoot
$publish = Join-Path $repo 'publish'
$apiOut = Join-Path $publish 'API'
$adminOut = Join-Path $publish 'Admin'
$frontOut = Join-Path $publish 'frontend'

foreach ($dir in @($apiOut, $adminOut, $frontOut)) {
  if (Test-Path $dir) {
    Write-Host "Cleaning $dir"
    Remove-Item $dir -Recurse -Force
  }
}

Write-Host "Building user site"
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
Write-Host "  API       -> the API domain (https://api.sharebizz.com)"
Write-Host "  Admin     -> the admin domain, site root"
Write-Host "  frontend  -> the user domain, site root"
Write-Host "Do not replace the server appsettings.json. Keep the keys already on the server."
