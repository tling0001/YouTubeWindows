# YouTube Leanback for Windows

## Requirements

**Operating System**
- Windows 7/8.1/10/11
- Windows Server 2008 R2/2012 R2/2012/2016/2019/2022/2025

**Processor**
- x86/x64/arm64

**Runtime**
1. Install WebView2 Runtime: [https://go.microsoft.com/fwlink/p/?LinkId=2124703](https://go.microsoft.com/fwlink/p/?LinkId=2124703)  
   OR place the WebView2 Shared Runtime in the runtime directory (PlayReady DRM will not be available).

2. .NET 10 (Windows)

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
--proxy-server="http://proxy-ip:proxy-port"		Access via proxy
--enable-features={Feature1,Feature2,...}		Enable features such as Feature1, Feature2, etc. (Refer to Microsoft Edge documentation)
```

