# Grade Tracker – WiX v7 desktop deployment

A small C# (.NET 8, Windows Forms) desktop app packaged as an MSI with **WiX Toolset v7**.
Built for SWE40006 Deployment Activity 1, Task 1 (levels 1.1 – 1.4).

The app records unit marks and calculates grade, WAM and GPA. It is deliberately split into
several DLLs so the installer has real dependencies to deploy:

| File | Type | Purpose |
|---|---|---|
| `GradeTracker.exe` / `GradeTracker.dll` | Executable (WinForms) | User interface |
| `GradeTracker.Core.dll` | Our class library | Grade, WAM and GPA calculations, validation |
| `GradeTracker.Data.dll` | Our class library | Saves/loads data as JSON in `%LOCALAPPDATA%\GradeTracker` |
| `Newtonsoft.Json.dll` | Third-party NuGet library | JSON serialisation (used by Data) |
| `GradeTracker.runtimeconfig.json`, `GradeTracker.deps.json` | .NET host files | Required runtime + dependency map |
| .NET 8 Desktop Runtime | External prerequisite | Checked by the installer (Netfx extension) |

Dependency chain: `GradeTracker.exe → GradeTracker.Data.dll → Newtonsoft.Json.dll`, and both
`exe` and `Data` → `GradeTracker.Core.dll`.

## Repository layout

```
GradeTracker/
├─ build.ps1                          # publish app + build MSI
├─ src/
│  ├─ GradeTracker.Core/              # DLL 1 – business logic
│  ├─ GradeTracker.Data/              # DLL 2 – persistence (+ Newtonsoft.Json)
│  └─ GradeTracker.App/               # WinForms .exe
├─ installer/GradeTracker.Installer/  # WiX v7 project
│  ├─ GradeTracker.Installer.wixproj  # SDK project, AcceptEula, extensions
│  ├─ Package.wxs                     # package, upgrade, .NET check, UI
│  ├─ Folders.wxs                     # install + Start menu folders
│  ├─ AppComponents.wxs               # one component per file, shortcut
│  ├─ Package.en-us.wxl               # strings (manufacturer, messages)
│  └─ License.rtf                     # shown in the installer wizard
└─ tools/
   ├─ GradeTrackerSandbox.wsb         # Windows Sandbox test config
   └─ StoreLogo_300x300.png           # Store listing logo
```

## 1. Environment setup (Windows 10/11 x64)

1. **Visual Studio 2022 (17.8+) or 2026** with the **.NET desktop development** workload.
2. **.NET 8 SDK** (installed with VS; check with `dotnet --list-sdks`).
3. **HeatWave** extension (Extensions → Manage Extensions → search "HeatWave") so VS can open `.wixproj` files.
4. **WiX v7 command-line tool** (optional but useful for inspecting MSIs):
   ```powershell
   dotnet tool install --global wix --version 7.0.0
   wix --version
   wix eula accept wix7        # one-time, per user
   ```
5. **Windows Sandbox** for clean install tests (Windows Pro/Enterprise/Education):
   `Turn Windows features on or off → Windows Sandbox`, then reboot.
6. **Git** + a public GitHub repository (the report needs a public URL).

Tip: keep the repository in a short path without spaces, e.g. `C:\dev\GradeTracker`.

## 2. Build

```powershell
cd C:\dev\GradeTracker
.\build.ps1
```
If PowerShell blocks the script: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`.

Output: `installer\GradeTracker.Installer\bin\...\GradeTrackerSetup.msi`

To open everything in Visual Studio, create a solution once:
```powershell
dotnet new sln -n GradeTracker
dotnet sln add src\GradeTracker.Core src\GradeTracker.Data src\GradeTracker.App
```
Then in VS: right-click the solution → Add → Existing Project → `GradeTracker.Installer.wixproj`.
(Run `build.ps1` or publish the app first – the installer packages the `publish` folder.)

## 3. Test

```powershell
# Install with a verbose log (great evidence for the report)
msiexec /i GradeTrackerSetup.msi /l*v install.log

# Silent install / uninstall (needed later for the Microsoft Store)
msiexec /i GradeTrackerSetup.msi /qn
msiexec /x GradeTrackerSetup.msi /qn

# Inspect what WiX put inside the MSI
wix msi decompile GradeTrackerSetup.msi -o decompiled.wxs
```

Clean-machine test: edit `HostFolder` in `tools\GradeTrackerSandbox.wsb`, double-click it,
run `C:\Installer\...\GradeTrackerSetup.msi` inside the sandbox. The sandbox has no
.NET 8 Desktop Runtime, so the installer should stop with the "needs .NET 8" message –
install the runtime inside the sandbox and try again.

Verify: Start menu shortcut, `C:\Program Files\YourName\Grade Tracker\` contents,
Settings → Apps entry, *About / DLL info* button (shows each DLL's version and load path),
upgrade (bump `AppVersion` to 1.0.1, rebuild, install over the top), uninstall.

## 4. Troubleshooting

| Error | Cause | Fix |
|---|---|---|
| `WIX7015: You must accept the OSMF EULA` | WiX v7 enforces the Open Source Maintenance Fee EULA | `<AcceptEula>wix7</AcceptEula>` in the `.wixproj` (already there) or `wix eula accept wix7` |
| `WIX0103: Cannot find the File file '...publish\...'` | App not published yet | Run `.\build.ps1` (or `dotnet publish` to `publish\`) first |
| `The Wix element has an incorrect namespace` | v3 syntax/namespace | Use `http://wixtoolset.org/schemas/v4/wxs` (v4–v7 share it) |
| Installer: "needs the .NET 8 Desktop Runtime" | Target PC lacks runtime | Install it, or publish self-contained |
| App: "You must install or update .NET to run this application" | Runtime check removed/bypassed | Same as above |
| `.wixproj` won't load in VS | HeatWave missing/old | Install/update HeatWave (supports WiX v7) |
| Save error / `UnauthorizedAccessException` | Writing into Program Files | App writes to `%LOCALAPPDATA%` instead (by design) |
| "A newer version is already installed" | Downgrade blocked by `MajorUpgrade` | Uninstall first or raise the version |

## 5. Microsoft Store

See the report section "Deploying to the Microsoft Store". Summary: publish either
(a) as an **MSIX** (Microsoft hosts and signs it for free; package the app self-contained), or
(b) as a **Win32 MSI** (upload a link to the signed `GradeTrackerSetup.msi` hosted on HTTPS,
e.g. GitHub Releases; the MSI must be signed with a certificate from a trusted CA and support
silent install). Developer registration: https://storedeveloper.microsoft.com

## License
MIT
