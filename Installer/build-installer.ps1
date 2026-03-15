# Build IMS App installer
# Run from project root: .\Installer\build-installer.ps1
# Or: cd Installer; .\build-installer.ps1

$ErrorActionPreference = "Stop"
$InstallerDir = $PSScriptRoot
$ProjectRoot = Split-Path $InstallerDir -Parent
$PublishDir = Join-Path $ProjectRoot "publish"

# Step 1: Publish the app
Write-Host "Publishing application..."
Push-Location $ProjectRoot
try {
    dotnet publish imsapp-desktop.csproj -c Release -o publish -r win-x64 --self-contained true
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    Pop-Location
}

# Step 2: Find Inno Setup
$isccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $null
foreach ($p in $isccPaths) {
    if (Test-Path $p) { $iscc = $p; break }
}
if (-not $iscc) {
    Write-Host "Inno Setup not found. Install from: winget install JRSoftware.InnoSetup"
    Write-Host "Or download: https://jrsoftware.org/isinfo.php"
    exit 1
}

# Step 3: Get version and build installer
$version = (Select-String -Path (Join-Path $ProjectRoot "imsapp-desktop.csproj") -Pattern '<Version>([^<]+)</Version>').Matches.Groups[1].Value
Write-Host "Building installer (version $version)..."
Push-Location $InstallerDir
try {
    & $iscc "/DAppVersion=$version" "setup.iss"
    if ($LASTEXITCODE -eq 0) {
        $out = Join-Path $InstallerDir "Output\IMSApp-Setup-$version.exe"
        if (Test-Path $out) {
            Write-Host "`nInstaller created: $out"
        } else {
            $fallback = Get-ChildItem (Join-Path $InstallerDir "Output") -Filter "IMSApp-Setup-*.exe" | Select-Object -First 1
            if ($fallback) { Write-Host "`nInstaller created: $($fallback.FullName)" }
        }
    } else {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
