# Robot GUI

This directory contains the Windows Forms control GUI used with the Jetson
inference and GPIO/UART programs in this bundle. Build outputs, Visual Studio
state, NuGet package caches, and installed third-party binaries are excluded.

## Requirements

- Windows 10 or Windows 11
- Visual Studio 2022 or Build Tools with .NET desktop development
- .NET Framework 4.7.2 Developer Pack
- NuGet
- Newtonsoft.Json 13.0.3, restored from `packages.config`
- Serial ports connected to the Delta robot controller and Jetson UART bridge
- Network access to the Jetson camera server at `192.168.55.1:12345`

`Protocol_Test.csproj` also declares the `MLApp` COM type library. The current
source does not call `MLApp`, but an unmodified project build may require a
registered MATLAB Automation server on the build machine. The reference was
preserved to avoid changing the supplied project behavior.

## Restore and build

From a Developer Command Prompt for Visual Studio:

```powershell
nuget restore .\Protocol_Test.csproj -PackagesDirectory .\packages
msbuild .\Protocol_Test.csproj /p:Configuration=Release
msbuild .\ProtocolTests\ProtocolTests.csproj /p:Configuration=Release
```

The restored `packages/` directory and generated `.vs/`, `bin/`, and `obj/`
directories are local build artifacts and must not be committed.

Run the hardware-independent protocol tests with:

```powershell
.\ProtocolTests\bin\Release\CameraProtocolTests.exe
```

## Runtime connections

The GUI defaults are:

| Interface | Default | Configuration |
| --- | --- | --- |
| Delta robot serial port | `COM3` | 115200 baud, 8N1 |
| Jetson GPIO/UART bridge | `COM5` | 115200 baud, 8N1 |
| Jetson camera server | `192.168.55.1:12345` | TCP |

The GUI sends the UTF-8 text `True` to trigger one camera inference response.
The TCP response framing and Jetson-side hardware requirements are documented
in the root `README.md`.

Verify the actual serial-port assignments, robot controller state, emergency
stop, GPIO wiring, and coordinate calibration before connecting or commanding
physical hardware. The stopper BOARD-pin discrepancy described in the root
README also applies to this integrated setup.

