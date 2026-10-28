# Builds the Leyner Learner installer: publishes the app (self-contained, 64-bit) into publish\, then compiles
# LeynerLearner.iss with Inno Setup into output\LeynerLearner-Setup-<version>.exe.
#
# usage (from any folder):  powershell -ExecutionPolicy Bypass -File Installer\build.ps1
# Needs the .NET 10 SDK and Inno Setup 6 (https://jrsoftware.org/isdl.php).
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$project = Join-Path $here '..\LeynerLearner\LeynerLearner.csproj'
$publish = Join-Path $here 'publish'

# The version is the project's <Version>.
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) { throw "No <Version> in $project" }
Write-Host "Leyner Learner $version"

# 1. Publish.
if (Test-Path $publish) { Remove-Item -Recurse -Force $publish }
dotnet publish $project -c Release -r win-x64 --self-contained true -o $publish -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }
if (-not (Test-Path (Join-Path $publish 'Data\manifest.json'))) { throw 'The published app has no Data folder' }

# 2. Compile the installer.
$iscc = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
          "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 is not installed (https://jrsoftware.org/isdl.php). The app is published in Installer\publish.' }
& $iscc /Q "/DAppVersion=$version" (Join-Path $here 'LeynerLearner.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed' }
Get-ChildItem (Join-Path $here 'output') -Filter "LeynerLearner-Setup-$version.exe" | ForEach-Object {
    Write-Host ("Wrote {0} ({1:N1} MB)" -f $_.FullName, ($_.Length / 1MB)) }
