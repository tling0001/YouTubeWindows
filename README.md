# YouTube Leanback for Windows

## Requirements

**Operating System**
- Windows 10 1607 LTSC/Enterprise or later
- Windows 11
- Windows Server 2016/2019/2022/2025

**Processor**
- x64

**Runtime**
1. Install WebView2 Runtime: [https://go.microsoft.com/fwlink/p/?LinkId=2124703](https://go.microsoft.com/fwlink/p/?LinkId=2124703)  
   OR place the WebView2 Shared Runtime in the runtime directory (PlayReady DRM will not be available).

2. Install the .NET 10 Runtime.

## Control

- `Mice Right Button` `Esc`  - Back
- `Mice Left Button` `Enter` - Confirm
- `Arrow Keys`               - Move Cursor
- `F11`                      - Fullscreen
- `F5` `Ctrl + R`            - Reload App

## Advanced

**Command Line Parameters**

```
--allow-auto-hdr					Allows stretching SDR content to HDR (e.g., NVIDIA RTX HDR)
--kiosk					Starts in kiosk-style borderless fullscreen mode
--windowed				Starts in a normal resizable window instead of kiosk mode
--proxy-server="http://proxy-ip:proxy-port"		Access via proxy
--enable-features={Feature1,Feature2,...}		Enable features such as Feature1, Feature2, etc. (Refer to Microsoft Edge documentation)
```

## Building Yourself

1. Open `YouTubeWindows.sln` in Visual Studio 2022 or later with the .NET 10 SDK installed.
2. Restore NuGet packages if Visual Studio does not do it automatically.
3. Build the solution.

The four launchers will be built into the shared `bin\Debug` or `bin\Release` folder as:

- `YouTube.exe`
- `YouTubeKids.exe`
- `YouTubeTV.exe`
- `YouTubeMusic.exe`

You can also build from the command line with:

```powershell
dotnet build YouTubeWindows.sln
```

To publish all launchers for x86, x64, and arm64 into separate folders, run:

```powershell
.\Publish-All.ps1
```

That creates folders like `publish\win-x86`, `publish\win-x64`, and `publish\win-arm64`.
Each architecture folder is flattened, so all four EXEs and their shared runtime files sit together in the same `publish\win-...` folder.
It also creates zip files in your Downloads folder named `YouTubeLeanbackWindows-x86.zip`, `YouTubeLeanbackWindows-x86-64.zip`, and `YouTubeLeanbackWindows-arm64.zip`.
