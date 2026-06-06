# Windows Installer / Deployment

## Overview

The Control Center is deployed as a self-contained .NET 8 application for Windows x64. No .NET Runtime installation is required on the target machine.

## Build for Deployment

### Using the Build Script (Recommended)

From the project root:

```powershell
# PowerShell (Windows)
./build.ps1

# Bash (Linux/CI)
./build.sh
```

This produces a complete deployment package in the `dist/` folder.

### Manual Build

#### Step 1: Build the React Frontend

```bash
cd web
npm install
npm run build
```

This outputs the built frontend to `gateway/src/wwwroot/`.

#### Step 2: Publish the .NET Gateway

```bash
cd gateway/src
dotnet publish -c Release -r win-x64 --self-contained -o ../../dist
```

Options explained:
- `-c Release` - Release configuration (optimized)
- `-r win-x64` - Target Windows 64-bit
- `--self-contained` - Include .NET runtime (no runtime install needed on target)
- `-o ../../dist` - Output to dist/ folder at project root

## Deployment Package

The `dist/` folder contains everything needed to run on the target machine:

```
dist/
├── ControlCenter.Gateway.exe   # Main executable
├── wwwroot/                    # React dashboard (embedded)
│   ├── index.html
│   └── assets/
├── appsettings.json            # Configuration file
├── web.config                  # IIS configuration (optional)
└── (runtime DLLs)              # .NET runtime and dependencies
```

## Installation on Target Machine

1. Copy the entire `dist/` folder to the target machine:
   ```
   Recommended path: C:\ControlCenter\
   ```

2. (Optional) Edit `appsettings.json` to customize configuration.

3. Run `ControlCenter.Gateway.exe` to start the application.

4. Access the dashboard at http://localhost:8088

## Running as a Windows Service

To run the application as a Windows Service (starts automatically on boot):

### Register the Service

```cmd
sc create ControlCenterGateway ^
    binPath= "C:\ControlCenter\ControlCenter.Gateway.exe" ^
    start= auto ^
    DisplayName= "Control Center Gateway"
```

### Start the Service

```cmd
sc start ControlCenterGateway
```

### Stop the Service

```cmd
sc stop ControlCenterGateway
```

### Remove the Service

```cmd
sc stop ControlCenterGateway
sc delete ControlCenterGateway
```

## Firewall Configuration

If the dashboard needs to be accessed from other machines on the network:

```powershell
# Allow inbound connections to port 8088
New-NetFirewallRule -DisplayName "Control Center Web" `
    -Direction Inbound -Port 8088 -Protocol TCP -Action Allow
```

## Updating

1. Stop the application or service.
2. Replace all files in the installation folder EXCEPT:
   - `controlcenter.db` (database - preserves data)
   - `appsettings.json` (configuration - preserves custom settings)
3. Start the application or service.

## Troubleshooting Deployment

### Application fails to start
- Check that the target machine is Windows 10 x64 or later
- Ensure port 8088 is not in use by another application
- Check the `logs/` folder for error details

### Missing wwwroot files
- Ensure the frontend was built before publishing (`npm run build` in web/)
- Verify `wwwroot/index.html` exists in the dist/ folder

### Permission issues
- If running as a service, ensure the service account has read/write access to the installation folder
- The application needs write access for `controlcenter.db` and the `logs/` folder

## Alternative: Framework-Dependent Deployment

If the .NET 8 Runtime is already installed on the target machine, you can create a smaller package:

```bash
dotnet publish -c Release -r win-x64 --no-self-contained -o ../../dist
```

This requires [.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) to be installed on the target.
