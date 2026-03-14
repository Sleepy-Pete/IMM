# Android Unity Plugin Build Guide

## Problem

Your Unity project works in the Editor but not in Android builds because:

- **Editor uses:** Windows x86_64 DLLs in `Assets/Plugins/x86_64/`
- **Android needs:** ARM64 .so library in `Assets/Plugins/Android/arm64-v8a/`
- **Missing:** `libImmUnityPlugin.so` for Android

## Solution Overview

Build the Android version of the IMM Unity plugin and add it to your Unity project.

---

## Prerequisites

### 1. Android SDK & NDK

You should already have these from building the Android viewer app:

- **Android SDK** (via Android Studio)
- **NDK 25.2.9519653** (installed via SDK Manager)
- **CMake 3.22.1** (installed via SDK Manager)

### 2. Environment Variable

Set `ANDROID_HOME` environment variable (if not already set):
```
C:\Users\YourUsername\AppData\Local\Android\Sdk
```

---

## Build Steps

### Step 1: Build the Android Plugin

Navigate to the Unity plugin directory and run the build script:

```batch
cd a:\Github\IMM2\IMM\code\appImmUnity
build_android.bat
```

This will:
1. Configure CMake with Android NDK toolchain
2. Build libImmCore.a, libImmImporter.a, libImmPlayer.a
3. Link everything into `libImmUnityPlugin.so`
4. Output to: `code/appImmUnity/android-build/arm64-v8a/libImmUnityPlugin.so`

**Expected output:**
```
========================================
Build successful!
========================================

Output: android-build\arm64-v8a\libImmUnityPlugin.so
```

### Step 2: Copy to Unity Project

Run the copy script:

```batch
copy_android_plugin_to_unity.bat
```

This copies `libImmUnityPlugin.so` to:
```
Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so
```

### Step 3: Configure Unity Import Settings

1. **Open Unity** and let it import the new file
2. **Select** `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so` in Project window
3. **In Inspector**, verify settings:
   - ✅ **Android** - Enabled
   - ✅ **CPU** - ARM64
   - ❌ **Editor** - Disabled
   - ❌ **Standalone** - Disabled

Unity should auto-detect these settings, but verify them.

---

## Verify the Fix

### Test in Unity Editor
1. Play the scene in Editor - should still work (uses Windows DLL)

### Test Android Build
1. **File → Build Settings**
2. **Platform:** Android
3. **Build and Run** (or just Build)
4. Deploy to Quest 3
5. **Expected:** IMM content now plays correctly!

---

## Troubleshooting

### Build Error: "NDK not found"
**Solution:** Install NDK 25.2.9519653 via Android Studio SDK Manager

### Build Error: "CMake not found"
**Solution:** Install CMake 3.22.1 via Android Studio SDK Manager

### Build Error: "ANDROID_HOME not set"
**Solution:** Set environment variable:
```batch
setx ANDROID_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk"
```
Then restart command prompt.

### Unity Error: "DllNotFoundException: ImmUnityPlugin"
**Cause:** Plugin not found or wrong platform settings

**Solution:**
1. Check file exists: `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`
2. Check Inspector settings (Android enabled, ARM64)
3. Reimport the asset (right-click → Reimport)

### App Crashes on Quest
**Check logcat:**
```batch
adb logcat | findstr "ImmUnity"
```

Common issues:
- Missing dependencies (should be bundled in .so)
- Graphics API mismatch (plugin uses GLES3)
- Initialization failure (check native logs)

---

## Technical Details

### What Gets Built

The Android build creates:
- `libImmCore.a` - Core IMM functionality
- `libImmImporter.a` - IMM file parsing
- `libImmPlayer.a` - Playback engine
- `libImmUnityPlugin.so` - Unity interface (links all above)

### Platform Differences

| Feature | Windows (Editor) | Android (Quest) |
|---------|------------------|-----------------|
| Graphics API | DirectX 11 | OpenGL ES 3 |
| Audio | Audio360.dll | Unity audio |
| Architecture | x86_64 | ARM64 |
| File Extension | .dll | .so |

### CMake Configuration

The build uses Android NDK toolchain with:
- **ABI:** arm64-v8a (Quest 3 architecture)
- **Platform:** android-26 (Android 8.0)
- **STL:** c++_shared
- **Build Type:** Release

---

## Next Steps

After successful build and deployment:

1. ✅ Test basic IMM playback on Quest
2. ✅ Test all IMM features (chapters, volume, etc.)
3. ✅ Test with different IMM files
4. ✅ Performance testing on device

---

## Files Created

- `code/appImmUnity/CMakeLists.txt` - Android build configuration
- `code/appImmUnity/build_android.bat` - Build script
- `code/appImmUnity/copy_android_plugin_to_unity.bat` - Copy script
- This guide

---

**Last Updated:** 2026-01-05

