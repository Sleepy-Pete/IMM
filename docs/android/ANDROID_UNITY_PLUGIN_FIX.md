# Android Unity Plugin Fix - Quick Reference

## Problem Summary

**Issue:** Unity Editor can play IMM content, but Android builds don't run the IMM scripts.

**Root Cause:** Missing Android native library
- ✅ **Editor has:** `Assets/Plugins/x86_64/ImmUnityPlugin.dll` (Windows)
- ❌ **Android needs:** `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so` (ARM64)

---

## Quick Fix (3 Steps)

### 1. Build Android Plugin
```batch
cd a:\Github\IMM2\IMM\code\appImmUnity
build_android.bat
```

### 2. Copy to Unity
```batch
copy_android_plugin_to_unity.bat
```

### 3. Build Unity Project
- Open Unity
- File → Build Settings → Android
- Build and Run

**Done!** IMM should now work on Quest.

---

## What Was Created

### Build System Files
- ✅ `code/appImmUnity/CMakeLists.txt` - Android build configuration
- ✅ `code/appImmUnity/build_android.bat` - Build script
- ✅ `code/appImmUnity/copy_android_plugin_to_unity.bat` - Copy script

### Documentation
- ✅ `code/ImmUnitySampleProject/ANDROID_PLUGIN_BUILD_GUIDE.md` - Complete guide
- ✅ `ANDROID_UNITY_PLUGIN_FIX.md` - This quick reference
- ✅ Updated `code/ImmUnitySampleProject/UNITY_PROJECT_CONTEXT.md`

### Output
- 📦 `code/appImmUnity/android-build/arm64-v8a/libImmUnityPlugin.so`
- 📦 `code/ImmUnitySampleProject/Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`

---

## Prerequisites

Already installed for Android viewer:
- ✅ Android SDK
- ✅ NDK 25.2.9519653
- ✅ CMake 3.22.1

---

## Verification

### Check Build Output
```batch
dir code\appImmUnity\android-build\arm64-v8a\libImmUnityPlugin.so
```

### Check Unity Plugin
```batch
dir code\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a\libImmUnityPlugin.so
```

### Test on Quest
1. Build and deploy Unity project
2. Launch app on Quest
3. IMM content should play correctly

---

## Troubleshooting

### "NDK not found"
Install NDK 25.2.9519653 via Android Studio SDK Manager

### "CMake not found"
Install CMake 3.22.1 via Android Studio SDK Manager

### "ANDROID_HOME not set"
```batch
setx ANDROID_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk"
```

### Unity "DllNotFoundException"
1. Check file exists in `Assets/Plugins/Android/arm64-v8a/`
2. Select file in Unity Project window
3. Verify Inspector settings: Android enabled, CPU: ARM64
4. Right-click → Reimport

---

## Technical Details

### Platform Comparison

| Aspect | Windows (Editor) | Android (Quest) |
|--------|------------------|-----------------|
| **Plugin File** | ImmUnityPlugin.dll | libImmUnityPlugin.so |
| **Location** | Assets/Plugins/x86_64/ | Assets/Plugins/Android/arm64-v8a/ |
| **Architecture** | x86_64 | ARM64 |
| **Graphics API** | DirectX 11 | OpenGL ES 3 |
| **Audio** | Audio360.dll | Unity audio |

### Build Process

```
CMake + Android NDK
        ↓
libImmCore.a → libImmImporter.a → libImmPlayer.a
        ↓
libImmUnityPlugin.so (ARM64)
        ↓
Unity Assets/Plugins/Android/arm64-v8a/
        ↓
Android APK includes plugin
        ↓
Works on Quest! 🎉
```

---

## Related Documentation

- **Complete Guide:** `code/ImmUnitySampleProject/ANDROID_PLUGIN_BUILD_GUIDE.md`
- **Unity Context:** `code/ImmUnitySampleProject/UNITY_PROJECT_CONTEXT.md`
- **Android Build:** `ANDROID_QUICK_START.md`
- **Project Overview:** `PROJECT_CONTEXT.md`

---

**Created:** 2026-01-05  
**Status:** Ready to build and deploy

