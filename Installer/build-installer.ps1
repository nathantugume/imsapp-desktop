# Build IMS App installer
# Run from: .\Installer\build-installer.ps1
# Or: cd Installer; .\build-installer.ps1

$ErrorActionPreference = "Stop"
$InstallerDir = $PSScriptRoot

# Find Inno Setup (winget installs to LocalAppData)
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

# Build
Push-Location $InstallerDir
try {
    & $iscc "setup.iss"
    if ($LASTEXITCODE -eq 0) {
        $out = Join-Path $InstallerDir "Output\IMSApp-Setup-1.0.exe"
        if (Test-Path $out) {
            Write-Host "`nInstaller created: $out"
        }
    } else {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
