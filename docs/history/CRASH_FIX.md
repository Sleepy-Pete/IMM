# Android Crash Fix - SoundBackend NULL Pointer

## Problem Summary

After fixing the missing `libc++_shared.so` dependency, the app now loads the plugin successfully but **crashes immediately on startup** with:

```
signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x0
Cause: null pointer dereference
```

## Root Cause Analysis

### The Crash Location
The crash occurs in the `Init()` function at line 470 of `main.cpp`:

```cpp
const int num = gImmUnityPlugin.IMM.mSoundBackend->GetNumDevices();
```

### Why It Crashes

1. **DirectSoundOVR is Windows-only:**
   - The code tries to create a `DirectSoundOVR` sound backend
   - In `piSound.cpp` (lines 16-19), `DirectSoundOVR` is wrapped in `#ifndef ANDROID`
   - On Android, `piCreateSoundEngineBackend(piSoundEngineBackend::API::DirectSoundOVR, ...)` returns `nullptr`

2. **Null pointer not handled:**
   - The code checks if the backend is null and logs an error (lines 455-459)
   - **BUT** it continues execution instead of returning early
   - Later code tries to call methods on the null pointer (line 470)
   - **Result:** NULL POINTER DEREFERENCE → CRASH

### Evidence from Logs

```
01-07 17:36:36.123 I/ImmUnityPlugin: Creating sound backend...
01-07 17:36:36.124 E/ImmUnityPlugin: Failed to create SoundBackend
01-07 17:36:36.125 F/libc: Fatal signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 0x0
```

The plugin loads successfully, but crashes when trying to use the null sound backend.

## Solution

### Changes Made to `code/appImmUnity/src/main.cpp`

**Before (lines 451-489):**
```cpp
// SOUND ENGINE
ALOGI("Creating sound backend...");
gImmUnityPlugin.IMM.mSoundBackend = piCreateSoundEngineBackend(
    piSoundEngineBackend::API::DirectSoundOVR, &gImmUnityPlugin.IMM.mLog);

if(!gImmUnityPlugin.IMM.mSoundBackend)
{
    gImmUnityPlugin.IMM.mLog.Printf(LT_ERROR, L"Failed to create SoundBackend.");
    ALOGE("Failed to create SoundBackend");
}
// ... continues to use the null pointer ...
```

**After:**
```cpp
// SOUND ENGINE
ALOGI("Creating sound backend...");
#if defined(__ANDROID__) || defined(ANDROID)
    // On Android, use NULL sound backend since DirectSoundOVR is not available
    ALOGI("Android platform detected - using NULL sound backend");
    gImmUnityPlugin.IMM.mSoundBackend = piCreateSoundEngineBackend(
        piSoundEngineBackend::API::Null, &gImmUnityPlugin.IMM.mLog);
#else
    // On Windows/Desktop, use DirectSoundOVR
    gImmUnityPlugin.IMM.mSoundBackend = piCreateSoundEngineBackend(
        piSoundEngineBackend::API::DirectSoundOVR, &gImmUnityPlugin.IMM.mLog);
#endif

if(!gImmUnityPlugin.IMM.mSoundBackend)
{
    gImmUnityPlugin.IMM.mLog.Printf(LT_ERROR, L"Failed to create SoundBackend.");
    ALOGE("Failed to create SoundBackend");
    return -1;  // EXIT EARLY - don't continue with null pointer
}
```

### Key Changes

1. **Platform-specific backend selection:**
   - Android: Use `piSoundEngineBackend::API::Null`
   - Windows/Desktop: Use `piSoundEngineBackend::API::DirectSoundOVR`

2. **Early exit on failure:**
   - Added `return -1;` if sound backend creation fails
   - Prevents null pointer dereference

3. **Skip Rift device search on Android:**
   - Wrapped the Rift device enumeration in `#if !defined(__ANDROID__)`
   - This code is only relevant for desktop VR

## Implementation Steps

1. ✅ **Modified source code** - `code/appImmUnity/src/main.cpp`
2. ✅ **Rebuilt native plugin** - Used CMake to rebuild for Android
3. ✅ **Copied to Unity project** - Updated `Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so`

## Next Steps

You need to rebuild the Unity project to include the fixed plugin:

1. **Open Unity Editor**
2. **Build APK** (`File > Build Settings > Build`)
3. **Install on Quest:**
   ```powershell
   adb -s 2G0YC1ZF98028F install -r path\to\your.apk
   ```
4. **Test and monitor logs:**
   ```powershell
   adb -s 2G0YC1ZF98028F logcat -c
   adb -s 2G0YC1ZF98028F logcat | Select-String "ImmUnityPlugin|IMM"
   ```

## Expected Results

### Before Fix:
```
I/ImmUnityPlugin: Creating sound backend...
E/ImmUnityPlugin: Failed to create SoundBackend
F/libc: Fatal signal 11 (SIGSEGV) - CRASH
```

### After Fix:
```
I/ImmUnityPlugin: Creating sound backend...
I/ImmUnityPlugin: Android platform detected - using NULL sound backend
I/ImmUnityPlugin: SoundBackend created successfully
I/ImmUnityPlugin: SoundBackend initialized successfully
I/ImmUnityPlugin: Init() completed successfully
```

## Files Modified

- ✅ `code/appImmUnity/src/main.cpp` - Fixed sound backend initialization
- ✅ `code/ImmUnitySampleProject/Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so` - Updated with fixed version

## Summary

The crash was caused by attempting to use DirectSoundOVR on Android, which is not available on that platform. The fix uses the NULL sound backend on Android (which is designed for this purpose) and adds proper error handling to prevent null pointer dereferences.

