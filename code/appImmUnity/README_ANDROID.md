# IMM Unity Plugin - Android Build

This directory contains the IMM Unity plugin source code and build scripts for both Windows and Android platforms.

## Overview

The Unity plugin provides a native interface between Unity C# scripts and the IMM player engine.

### Platform Support

| Platform | Architecture | File | Build System |
|----------|-------------|------|--------------|
| **Windows** | x86_64 | ImmUnityPlugin.dll | Visual Studio (appImmUnity.vcxproj) |
| **Android** | ARM64 | libImmUnityPlugin.so | CMake + NDK (CMakeLists.txt) |

## Building for Android

### Prerequisites

- Android SDK
- Android NDK 25.2.9519653
- CMake 3.22.1
- ANDROID_HOME environment variable set

### Build Steps

1. **Run build script:**
   ```batch
   build_android.bat
   ```

2. **Copy to Unity:**
   ```batch
   copy_android_plugin_to_unity.bat
   ```

3. **Output location:**
   ```
   android-build/arm64-v8a/libImmUnityPlugin.so
   ```

### What Gets Built

The Android build creates a shared library that includes:
- **libImmCore.a** - Core IMM functionality (rendering, file I/O)
- **libImmImporter.a** - IMM file format parsing
- **libImmPlayer.a** - Playback engine and timeline
- **libImmUnityPlugin.so** - Unity interface (links all above)

### Build Configuration

- **Target:** Android ARM64 (arm64-v8a)
- **Min SDK:** API 26 (Android 8.0)
- **Graphics:** OpenGL ES 3
- **STL:** c++_shared
- **Build Type:** Release

## Building for Windows

### Prerequisites

- Visual Studio 2022
- Windows SDK

### Build Steps

1. **Open solution:**
   ```
   code/projects/windows/imm.sln
   ```

2. **Build configuration:**
   - Configuration: Release
   - Platform: x64

3. **Or use build script:**
   ```batch
   ..\..\build.bat
   ```

4. **Output location:**
   ```
   exe/ImmUnityPlugin.dll
   ```

## Source Files

- **main.cpp** - Unity plugin interface implementation
  - Unity callbacks (UnityPluginLoad, UnityPluginUnload)
  - Render event handling
  - IMM player integration
  - Platform-specific code (Windows/Android)

- **Unity Interface Headers:**
  - IUnityInterface.h
  - IUnityGraphics.h
  - IUnityGraphicsD3D11.h (Windows only)
  - IUnityGraphicsD3D12.h (Windows only)

## Platform Differences

### Windows (Editor)
- Graphics API: DirectX 11
- Audio: Audio360.dll (spatial audio)
- Dependencies: Multiple DLLs (jpeg, png, ogg, vorbis, etc.)

### Android (Quest)
- Graphics API: OpenGL ES 3
- Audio: Unity's audio system (no Audio360)
- Dependencies: Bundled in .so file
- Render reporter: Disabled

## Integration with Unity

The plugin is used by Unity C# scripts via P/Invoke:

```csharp
[DllImport("ImmUnityPlugin")]
public static extern int Init(int colorSpace, int antialiasing, 
                              string logFile, string tempFolder);

[DllImport("ImmUnityPlugin")]
public static extern int Load(string fileName);
```

Unity automatically selects the correct plugin based on platform:
- **Windows Editor:** `Assets/Plugins/x86_64/ImmUnityPlugin.dll`
- **Android Build:** `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`

## Deployment

After building, copy the plugin to Unity project:

### Windows
```batch
copy exe\ImmUnityPlugin.dll ..\ImmUnitySampleProject\Assets\Plugins\x86_64\
```

### Android
```batch
copy_android_plugin_to_unity.bat
```

Or manually:
```batch
copy android-build\arm64-v8a\libImmUnityPlugin.so ^
     ..\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a\
```

## Troubleshooting

### Android Build Fails

**"NDK not found"**
- Install NDK 25.2.9519653 via Android Studio SDK Manager

**"CMake not found"**
- Install CMake 3.22.1 via Android Studio SDK Manager

**"ANDROID_HOME not set"**
```batch
setx ANDROID_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk"
```

### Unity Integration Issues

**"DllNotFoundException: ImmUnityPlugin"**
- Check plugin file exists in correct location
- Verify plugin import settings in Unity Inspector
- For Android: Ensure platform is set to Android, CPU is ARM64

## Related Documentation

- **Unity Integration:** `../ImmUnitySampleProject/Assets/Scripts/README.md`
- **Android Plugin Guide:** `../ImmUnitySampleProject/ANDROID_PLUGIN_BUILD_GUIDE.md`
- **Deployment:** `../ImmUnitySampleProject/Assets/Scripts/DEPLOYMENT.md`

---

**Last Updated:** 2026-01-05

