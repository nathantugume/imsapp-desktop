# Updates app.manifest and Package.appxmanifest version to match the csproj Version.
# Usage: .\update-manifest-version.ps1 -Version "1.0.0"
param([string]$Version = "1.0.0")

$manifestVersion = if ($Version.Split('.').Length -eq 3) { "$Version.0" } else { $Version }

$appManifest = "app.manifest"
if (Test-Path $appManifest) {
    $content = Get-Content $appManifest -Raw
    $content = $content -replace '(<assemblyIdentity\s+version=")[^"]+(")', "`${1}$manifestVersion`${2}"
    Set-Content $appManifest $content -NoNewline
}

$pkgManifest = "Package.appxmanifest"
if (Test-Path $pkgManifest) {
    $content = Get-Content $pkgManifest -Raw
    $content = $content -replace '(<Identity[^>]*\sVersion=")[^"]+(")', "`${1}$manifestVersion`${2}"
    Set-Content $pkgManifest $content -NoNewline
}
