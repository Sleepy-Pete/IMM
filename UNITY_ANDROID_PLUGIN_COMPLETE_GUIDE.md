# Complete Guide: Building Unity Android Plugin for Meta Quest

## Overview

This guide walks you through building a complete Unity plugin for Meta Quest with all necessary Android capabilities for VR rendering.

## Prerequisites

✅ **Already Installed:**
- Android SDK
- NDK 24.0.8215888
- CMake 3.22.1
- Ninja build system

## Project Structure

```
IMM/
├── code/
│   ├── appImmUnity/                    # Unity plugin source
│   │   ├── src/main.cpp                # Plugin implementation
│   │   ├── CMakeLists.txt              # Android build config
│   │   ├── build_android.bat           # Build script
│   │   └── copy_android_plugin_to_unity.bat
│   ├── libImmCore/                     # Core IMM library
│   ├── libImmImporter/                 # IMM file importer
│   └── libImmPlayer/                   # IMM player engine
├── code/ImmUnitySampleProject/
│   └── Assets/Plugins/
│       ├── Android/
│       │   ├── AndroidManifest.xml     # ✅ Meta Quest permissions
│       │   └── arm64-v8a/
│       │       └── libImmUnityPlugin.so  # Target output
│       └── x86_64/
│           └── ImmUnityPlugin.dll      # Windows Editor plugin
└── thirdparty/                         # Third-party dependencies
```

## Step 1: Build Third-Party Libraries

The Unity plugin requires Android ARM64 versions of several libraries.

### Option A: Automated Build (Recommended)

```batch
build_thirdparty_android.bat
```

This will build all required libraries:
- zlib
- libpng
- libjpeg-turbo
- libogg
- libvorbis
- opus
- libopusenc

### Option B: Manual Build

For each library, run:

```powershell
cd thirdparty\[library-name]
mkdir build-android
cd build-android

cmake -G Ninja `
  -S .. `
  -B . `
  -DCMAKE_TOOLCHAIN_FILE="C:\Users\%USERNAME%\AppData\Local\Android\Sdk\ndk\24.0.8215888\build\cmake\android.toolchain.cmake" `
  -DANDROID_ABI=arm64-v8a `
  -DANDROID_PLATFORM=android-26 `
  -DCMAKE_BUILD_TYPE=Release `
  -DCMAKE_INSTALL_PREFIX="..\android\arm64-v8a" `
  -DCMAKE_MAKE_PROGRAM="C:\Users\%USERNAME%\AppData\Local\Android\Sdk\cmake\3.22.1\bin\ninja.exe"

cmake --build . --config Release
cmake --install .
```

### Option C: Use Prebuilt Libraries

Download prebuilt Android ARM64 libraries and place them in:
```
thirdparty/[library]/android/arm64-v8a/
```

## Step 2: Build the Unity Plugin

Once third-party libraries are ready:

```batch
cd code\appImmUnity
build_android.bat
```

This will:
1. Configure CMake with Android NDK toolchain
2. Build libImmCore, libImmImporter, libImmPlayer
3. Link everything into libImmUnityPlugin.so
4. Output to: `code\appImmUnity\android-build\arm64-v8a\libImmUnityPlugin.so`

## Step 3: Copy Plugin to Unity Project

```batch
cd code\appImmUnity
copy_android_plugin_to_unity.bat
```

This copies the plugin to:
```
code\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a\libImmUnityPlugin.so
```

## Step 4: Configure Unity Project

### 4.1 Verify Plugin Settings

In Unity Editor:
1. Select `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`
2. In Inspector, verify:
   - ✅ Android: Enabled
   - ✅ CPU: ARM64
   - ❌ Editor: Disabled
   - ❌ Standalone: Disabled

### 4.2 Build Settings

1. **File → Build Settings**
2. **Platform:** Android
3. **Texture Compression:** ASTC
4. **Minimum API Level:** Android 8.0 (API 26)
5. **Target API Level:** Automatic (highest installed)
6. **Scripting Backend:** IL2CPP
7. **Target Architectures:** ARM64 ✅

### 4.3 Player Settings

**XR Settings:**
- ✅ Virtual Reality Supported
- **Virtual Reality SDKs:** Oculus

**Other Settings:**
- **Package Name:** com.yourcompany.immunity
- **Minimum API Level:** Android 8.0 (API 26)
- **Target API Level:** Automatic
- **Scripting Backend:** IL2CPP
- **API Compatibility Level:** .NET Standard 2.1
- **Target Architectures:** ARM64

## Step 5: Build and Deploy

### Build APK

1. **File → Build Settings → Build**
2. Choose output location
3. Wait for build to complete

### Deploy to Quest

```batch
adb install -r path\to\your.apk
```

Or use **Build and Run** in Unity to automatically deploy.

## AndroidManifest.xml Features

The plugin includes a custom AndroidManifest.xml with Meta Quest-specific features:

```xml
<!-- VR Permissions -->
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
<uses-permission android:name="android.permission.MODIFY_AUDIO_SETTINGS" />

<!-- VR Features -->
<uses-feature android:name="android.hardware.vr.headtracking" android:version="1" android:required="true" />
<uses-feature android:glEsVersion="0x00030002" android:required="true" />

<!-- Meta Quest Support -->
<meta-data android:name="com.oculus.supportedDevices" android:value="all" />
<meta-data android:name="com.samsung.android.vr.application.mode" android:value="vr_only" />
<meta-data android:name="com.oculus.application.colorspace" android:value="!Rift" />
```

## Troubleshooting

### Build Fails: "undefined reference to jpeg_*"

**Cause:** Third-party libraries not built for Android ARM64

**Solution:** Run `build_thirdparty_android.bat` or manually build the libraries

### Unity: "DllNotFoundException: libImmUnityPlugin"

**Cause:** Plugin not copied to correct location or wrong platform settings

**Solution:**
1. Verify file exists: `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`
2. Check Inspector settings (Android enabled, ARM64)
3. Right-click → Reimport

### Quest: App crashes on launch

**Cause:** Missing permissions or incompatible API level

**Solution:**
1. Check AndroidManifest.xml is included
2. Verify Minimum API Level is 26 or higher
3. Check logcat: `adb logcat | findstr IMM`

## Next Steps

After successful build:
1. Test IMM content playback in Unity Editor (Windows)
2. Build for Android and test on Quest
3. Verify VR rendering and head tracking
4. Test IMM file loading and playback

## Related Documentation

- `UNITY_ANDROID_PLUGIN_BUILD_STATUS.md` - Current build status
- `ANDROID_UNITY_PLUGIN_FIX.md` - Quick reference
- `code/ImmUnitySampleProject/ANDROID_PLUGIN_BUILD_GUIDE.md` - Detailed guide
- `ANDROID_BUILD_GUIDE.md` - General Android build info

---

**Created:** 2026-01-07  
**Status:** Ready for third-party library builds

