# Development Status

Updated: 2026-11-17

## Completed

- Established the WPF presentation / .NET core-engine / read-only telemetry-adapter architecture.
- Created a Windows WPF application shell with explicit disconnected/awaiting-validation UI.
- Added a deterministic local four-channel haptic mixer with independent channel enablement, truck-profile parameters, surface response, wheel-slip differentiation when supplied, brake/retarder response, and ABS pulse modulation.
- Added a Windows read-only adapter for the RenCloud SCS SDK plugin revision 12 shared-memory map, freshness checks, field capability metadata, truck identity/surface parsing, and measured update-rate reporting.
- Wired live telemetry into dashboard values and local mixer target meters (clearly marked as not hardware output).
- Added focused mixer tests for channel separation, surface response, missing values, clamping, and ABS pulses.
- Added default settings/profile persistence in `%APPDATA%\ETS2DualSense\settings.json`.
- Added project build/run instructions.
- Added release README covering installation, telemetry setup, and explicit hardware/AI limitations.
- Added an Inno Setup script for per-user installation, shortcuts, and uninstall.

## In progress

- DualSense USB discovery, input monitoring, safe output lifecycle, and report transport.
- Truck database/profile management, focused telemetry tests, and broader interface binding.

## Known limitations

- The live reader requires the external RenCloud SCS SDK telemetry plugin revision 12. It has not yet been validated against a running ETS2 installation in this environment.
- The mixer currently produces normalized target values; they are not hardware reports.
- Surface and vehicle profile presets are initial tuning parameters, not measured truck-specific profiles.
- Latency, USB stability, and adaptive-trigger support have not been measured or physically validated.
- Online AI/profile services, reconnection, and offline AI profile cache are not implemented.
- Engine/road/traction targets need real-truck tuning; temporal smoothing and a dedicated output scheduler remain outstanding.

## Hardware validation

**Not performed.** No controller output is sent. Independent actuator/trigger behavior and simultaneous operation remain unverified on physical DualSense hardware.

## Release validation

- Release build: successful with zero warnings and errors.
- Automated tests: 5 passed.
- Self-contained win-x64 single-file publish: successful.
- Packaged app startup, default configuration creation/loading, and graceful GUI shutdown: successful.
- Installer compile, per-user install, Start Menu shortcut creation, uninstall, and configuration retention: successful.
- Telemetry and DualSense physical hardware: not available for validation.

## Next tasks

1. Select a maintained DualSense HID transport, verify USB report layouts against upstream references, and add safe reset/output scheduling.
2. Verify telemetry fields and update-rate reporting against a running ETS2 installation.
3. Add tests for telemetry parsing and reconnection alongside the existing haptic mixer cases.
4. Bind live controller state and measured USB timing diagnostics into the WPF dashboard.
5. Add truck-specific profile management and then assess asynchronous, optional AI profile enrichment.

## Telemetry reference

The shared-memory name, layout, and offsets target [RenCloud/scs-sdk-plugin](https://github.com/RenCloud/scs-sdk-plugin), revision 12 (`scs-telemetry-common.hpp`). Fields absent from that map (engine load, wheel slip, and ABS) remain unavailable in the sample and capability registry.
