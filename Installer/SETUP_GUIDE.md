# IMS App Desktop – Setup & Installer Guide

This guide explains how to build the app, bundle MySQL, and create an installer for distribution.

---

## Quick Start (Development)

1. **Run without installer**
   - Open the solution in Visual Studio and run (F5), or:
   - `dotnet run --project imsapp-desktop`
   - If MySQL is not running, the app will try to use bundled MySQL (see below).

2. **Using existing MySQL**
   - Ensure MySQL is running on `localhost:3306` with user `root` and no password.
   - The app uses database `imsapp` (created automatically if missing).
   - Or edit `%LocalAppData%\imsapp-desktop\appsettings.json` to set your connection string.

---

## Bundled MySQL (Zero-Config for End Users)

The app can start a bundled MySQL instance so users don’t need to install MySQL.

### How it works

1. **First launch**
   - App tries to connect to the configured MySQL (default: localhost:3306).
   - If that fails, it looks for a `mysql` folder next to the executable.
   - If found, it initializes a data directory in `%LocalAppData%\imsapp-desktop\mysql-data`, starts MySQL on port **3307**, creates the `imsapp` database, runs the schema, and saves the connection string to config.

2. **Subsequent launches**
   - App loads the saved connection string and connects to the bundled MySQL on port 3307.

3. **Config file**
   - `%LocalAppData%\imsapp-desktop\appsettings.json`
   - Fields: `ConnectionString`, `BundledMySQLPath`, `BundledPort` (default 3307).

---

## Creating the Installer

### Prerequisites

- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`)
- .NET 10 SDK

### Build the installer

**App only** (users provide their own MySQL):

```powershell
.\Installer\build-installer.ps1
```

**With bundled database** (zero-config for end users):

```powershell
.\Installer\build-installer.ps1 -IncludeDatabase
```

The script downloads MySQL 8.0.44 (~200 MB) on first run and caches it. Output:
- App only: `Installer\Output\IMSApp-Setup-{version}.exe`
- With DB: `Installer\Output\IMSApp-Setup-WithDB-{version}.exe`

Or manually:
1. Publish: `dotnet publish -c Release -r win-x64 --self-contained true -o publish`
2. Build: Open `Installer\setup.iss` (or `setup-with-db.iss`) in Inno Setup.

### Install and test

- Run the installer (no admin required).
- Launch IMS App from the Start menu.
- On first run, ensure MySQL is running (or configure connection in Settings).
- The app installs to %LocalAppData%\IMS App to avoid WinUI 3 issues with Program Files.
- Default login: `admin@gmail.com` / `password` (change after first login).

---

## Folder structure for installer

```
Installer/
├── setup.iss          # App-only installer (no MySQL)
├── setup-with-db.iss  # Installer with bundled MySQL
├── build-installer.ps1 # Publish + build script (-IncludeDatabase for DB variant)
├── mysql-staging/     # Created by -IncludeDatabase (MySQL extracted here)
├── Output/            # Generated installers
└── SETUP_GUIDE.md
```

The script uses `..\publish\` (created by dotnet publish) as the app source.

---

## MySQL requirement

- **App-only installer**: Users must have MySQL or configure the connection in Settings.
- **With-DB installer** (`-IncludeDatabase`): Bundles MySQL 8.0.44. On first launch, the app initializes the database automatically.

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Cannot connect to MySQL" | Ensure MySQL is running, or add bundled MySQL. |
| "Bundled MySQL not found" | Place `mysql` folder next to the exe or set `BundledMySQLPath` in config. |
| Port 3307 in use | Change `BundledPort` in config or stop the conflicting service. |
| Schema not applied | Delete `%LocalAppData%\imsapp-desktop\mysql-data` and restart. |

---

## Default admin credentials

- **Email:** admin@gmail.com  
- **Password:** password  

Change these after first login.
