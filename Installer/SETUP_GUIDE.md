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

- [Inno Setup 6](https://jrsoftware.org/isinfo.php)
- .NET 8 SDK
- MySQL 8.0 Windows ZIP (for bundling)

### Step 1: Publish the app

```powershell
cd c:\Users\Nathan\source\repos\imsapp-desktop
dotnet publish -c Release -r win-x64 --self-contained true
```

Output: `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\`

### Step 2: Add bundled MySQL

1. Download MySQL 8.0 for Windows (ZIP):
   - https://dev.mysql.com/downloads/mysql/
   - Choose "Windows (x86, 64-bit), ZIP Archive"

2. Extract the ZIP and copy the contents into:
   ```
   imsapp-desktop\Installer\mysql-bundle\
   ```
   The folder should contain `bin\mysqld.exe`, `bin\mysql.exe`, etc.

3. Optional: Remove unneeded files (docs, test, etc.) to reduce size.

### Step 3: Build the installer

1. Open `Installer\setup.iss` in Inno Setup Compiler.
2. Or run from command line:
   ```powershell
   & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
   ```
3. The installer will be created in `Installer\Output\IMSApp-Setup-1.0.exe`.

### Step 4: Install and test

- Run the installer.
- Launch IMS App from the Start menu.
- On first run, the app will initialize and start bundled MySQL (may take a few seconds).
- Default login: `admin@gmail.com` / `password` (change after first login).

---

## Folder structure for installer

```
Installer/
├── setup.iss          # Inno Setup script
├── mysql-bundle/      # Add MySQL contents here before building
│   ├── bin/
│   │   ├── mysqld.exe
│   │   └── ...
│   └── ...
├── Output/            # Generated installer
│   └── IMSApp-Setup-1.0.exe
└── SETUP_GUIDE.md
```

---

## Build without bundled MySQL

If you omit the `mysql-bundle` folder, the installer will still work. Users must either:

- Have MySQL installed and running, and configure the connection in Settings, or
- Manually place a MySQL folder next to the installer/app.

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
