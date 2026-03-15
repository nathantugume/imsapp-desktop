# Build IMS App installer
# Run from project root: .\Installer\build-installer.ps1
# With database: .\Installer\build-installer.ps1 -IncludeDatabase

param(
    [switch]$IncludeDatabase
)

$ErrorActionPreference = "Stop"
$InstallerDir = $PSScriptRoot
$ProjectRoot = Split-Path $InstallerDir -Parent
$PublishDir = Join-Path $ProjectRoot "publish"

# Step 1: Publish the app
Write-Host "Publishing application..."
Push-Location $ProjectRoot
try {
    dotnet publish imsapp-desktop.csproj -c Release -o publish -r win-x64 -p:Platform=x64 --self-contained true
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    # WinUI 3 needs resource.pri next to the exe when running from certain paths
    $priSource = "bin\x64\Release\net10.0-windows10.0.19041.0\win-x64\imsapp-desktop.pri"
    if (Test-Path $priSource) {
        Copy-Item $priSource -Destination "publish\imsapp-desktop.pri" -Force
    }
} finally {
    Pop-Location
}

# Step 2: If -IncludeDatabase, ensure MySQL/MariaDB is staged
$mysqlStaging = Join-Path $InstallerDir "mysql-staging"
$mysqldExe = Join-Path $mysqlStaging "bin\mysqld.exe"
$mariadbdExe = Join-Path $mysqlStaging "bin\mariadbd.exe"
$stagingExists = (Test-Path $mysqldExe) -or (Test-Path $mariadbdExe)

if ($IncludeDatabase) {
    if (-not $stagingExists) {
        Write-Host "Staging MariaDB for bundled database installer..."
        $dbZip = "mariadb-11.4.10-winx64.zip"
        $dbZipPath = Join-Path $InstallerDir $dbZip
        $dbUrl = "https://downloads.mariadb.org/rest-api/mariadb/11.4.10/mariadb-11.4.10-winx64.zip"

        if (-not (Test-Path $dbZipPath)) {
            Write-Host "Downloading MariaDB 11.4.10 (~150 MB)..."
            $tempZip = Join-Path $env:TEMP "mariadb-11.4.10-winx64.zip"
            try {
                $ProgressPreference = "SilentlyContinue"
                [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                Invoke-WebRequest -Uri $dbUrl -OutFile $tempZip -UseBasicParsing -TimeoutSec 600
                Move-Item $tempZip $dbZipPath -Force
            } catch {
                Write-Host ""
                Write-Host "Download failed."
                Write-Host "Manual option: Download from https://mariadb.org/download/"
                Write-Host "  - Select: Windows x86_64, ZIP"
                Write-Host "  - Save as: $dbZipPath"
                Write-Host "Or extract to: $mysqlStaging (must contain bin\mariadbd.exe)"
                exit 1
            }
        }

        if (Test-Path $mysqlStaging) { Remove-Item $mysqlStaging -Recurse -Force }
        Write-Host "Extracting MariaDB..."
        Expand-Archive -Path $dbZipPath -DestinationPath $InstallerDir -Force
        $extractedDir = Join-Path $InstallerDir "mariadb-11.4.10-winx64"
        if (Test-Path $extractedDir) {
            Rename-Item $extractedDir $mysqlStaging
        } else {
            Write-Host "Expected mariadb-11.4.10-winx64 folder not found after extract."
            exit 1
        }
    }
    if (-not (Test-Path $mysqldExe) -and -not (Test-Path $mariadbdExe)) {
        Write-Host "Database staging failed: need bin\mysqld.exe or bin\mariadbd.exe"
        exit 1
    }
    Write-Host "Database staged at $mysqlStaging"
}

# Step 3: Find Inno Setup
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

# Step 4: Get version and build installer
$version = (Select-String -Path (Join-Path $ProjectRoot "imsapp-desktop.csproj") -Pattern '<Version>([^<]+)</Version>').Matches.Groups[1].Value
$setupScript = if ($IncludeDatabase) { "setup-with-db.iss" } else { "setup.iss" }
$outputBase = if ($IncludeDatabase) { "IMSApp-Setup-WithDB-$version" } else { "IMSApp-Setup-$version" }

Write-Host "Building installer (version $version)$(if ($IncludeDatabase) { ' with database' })..."
Push-Location $InstallerDir
try {
    & $iscc "/DAppVersion=$version" $setupScript
    if ($LASTEXITCODE -eq 0) {
        $out = Join-Path $InstallerDir "Output\$outputBase.exe"
        if (Test-Path $out) {
            Write-Host "`nInstaller created: $out"
        } else {
            $fallback = Get-ChildItem (Join-Path $InstallerDir "Output") -Filter "*.exe" | Select-Object -First 1
            if ($fallback) { Write-Host "`nInstaller created: $($fallback.FullName)" }
        }
    } else {
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
