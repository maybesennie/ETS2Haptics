# ETS2 DualSense Haptics

ETS2 DualSense Haptics is a Windows dashboard and local haptic-mixer foundation for Euro Truck Simulator 2. It can read supported fields from the RenCloud SCS SDK telemetry shared-memory map and calculate separate left/right actuator and L2/R2 trigger target values.

> **Important:** This release does not send output to a DualSense controller. It does not detect or monitor the controller, control adaptive triggers, provide an AI service, or include verified hardware feedback. The four displayed bars are mixer targets only. Do not treat them as physical controller output.

## System requirements

- 64-bit Windows 10 or Windows 11.
- Release package: x64 self-contained single-file .NET application; no separate .NET runtime installation is required.
- Optional ETS2 telemetry: Euro Truck Simulator 2 and RenCloud's SCS SDK telemetry plugin revision 12.
- A DualSense controller is not required for this release because hardware communication is not implemented.

## Installation

### Installer

Run `ETS2DualSense-Setup.exe` from the release `installer` directory and follow the setup wizard. The installer creates a Start Menu shortcut and offers a desktop shortcut. It installs per-user under Local AppData by default.

### Portable

Extract the complete `portable` directory to a writable folder and double-click `ETS2DualSense.exe`. Keep the executable and all adjacent published files together; the portable distribution is the whole directory, not just the executable.

Neither version requires VS Code or a manual build.

## Initial configuration

No API key or source-code editing is required. On first launch the app creates a default JSON configuration in `%APPDATA%\ETS2DualSense\settings.json`. Settings and the selected generic vehicle profile are loaded from there on startup, outside the application/install folder, so the file is retained when the app is updated or uninstalled. This release has no settings editor or profile-management UI; editing that file manually requires valid JSON and the documented schema.

## ETS2 telemetry setup

1. Install the RenCloud SCS SDK telemetry plugin in the game's `bin\win_x64\plugins` directory, following that plugin's installation instructions.
2. Start ETS2 and enter the game.
3. Start ETS2 DualSense Haptics. The dashboard looks for the plugin's `Local\SCSTelemetry` Windows shared-memory map.
4. The current reader accepts plugin revision 12. It reports disconnected, inactive/paused, stale, inconsistent, and unsupported states rather than fabricating telemetry.

The adapter reads available speed, RPM, throttle, brake, gear, retarder, road-substance index, and truck brand/name fields. Engine load, ABS state, and wheel slip are not present in the supported map and remain unavailable. Road-surface names may not map to a known mixer category.

## DualSense USB connection

USB connection instructions are not applicable yet: this release contains no DualSense HID transport, controller discovery, input monitoring, or output reports. Connecting a controller will not cause haptics or adaptive-trigger effects.

## AI service configuration and internet

There is no AI integration or online profile service in this release. The local telemetry reader and mixer do not make network requests. Internet access is only needed separately to obtain the SCS telemetry plugin or newer application releases.

## Updating

- **Installer:** install a newer release over the existing version. User configuration is stored separately in `%APPDATA%\ETS2DualSense`.
- **Portable:** replace the old portable application directory with the new release. Configuration remains in `%APPDATA%\ETS2DualSense`.

## Uninstalling

Use the application entry in Windows **Settings > Apps > Installed apps** or the Start Menu uninstall shortcut. Portable installations can be removed by deleting the extracted folder. User configuration in `%APPDATA%\ETS2DualSense` is preserved; delete that folder separately only if you also want to remove settings.

## Troubleshooting

- **Dashboard says it is waiting for telemetry:** start ETS2, install/enable the RenCloud SCS telemetry plugin, and confirm the plugin publishes revision 12 shared memory.
- **Unsupported plugin revision:** this release only reads revision 12. The adapter will not guess memory offsets for another revision.
- **Values become unavailable while driving:** check that the game is not paused and that the SCS telemetry update counter is advancing.
- **Controller is not detected or does not vibrate:** controller detection and hardware output are not included in this release.
- **The installer is blocked by Windows:** the installer is not code-signed. Use the Windows warning's publisher information to verify the file came from your trusted release source before choosing whether to proceed.

## Known limitations

- No DualSense USB or Bluetooth connection, input monitoring, output scheduling, actuator output, adaptive-trigger output, or safe hardware reset.
- No online AI, AI credentials/configuration, truck database, editable haptic settings UI, or truck-specific profile database. A generic profile and haptic defaults are persisted in the user configuration file.
- No validated real-time output latency or hardware feedback.
- The ETS2 adapter requires RenCloud SCS SDK plugin revision 12 and has not been validated against a running game in this environment.
- Automated test execution is blocked on the packaging machine by its Windows Application Control policy, although the test project compiles.

See [DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) for implementation and validation status.

## Build from source

Requires the .NET 10 SDK on Windows:

```powershell
dotnet build .\EuroTruckHaptics.sln -c Release
dotnet test .\EuroTruckHaptics.sln -c Release
dotnet publish .\src\EuroTruckHaptics\EuroTruckHaptics.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The published Windows executable is named `ETS2DualSense.exe`. The portable release directory includes the executable and this documentation; the .NET runtime is bundled in the executable.
