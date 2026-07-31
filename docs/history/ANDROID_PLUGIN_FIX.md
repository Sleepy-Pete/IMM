# Android Plugin Loading Fix - Missing libc++_shared.so

## Problem Summary

The `libImmUnityPlugin.so` native plugin fails to load on Android Quest devices with the error:
```
dlopen failed: library "ImmUnityPlugin" not found
```

Despite the plugin file being present in the APK, it cannot load due to a **missing dependency**.

## Root Cause Analysis

### The Issue
The plugin is built with `ANDROID_STL=c++_shared`, which means it dynamically links against the C++ standard library (`libc++_shared.so`). This library is **NOT** provided by the Android system and must be bundled with the app.

### Evidence
Using `llvm-readelf -d` on `libImmUnityPlugin.so` reveals these dependencies:

**System-provided libraries (already available on Android):**
- `libandroid.so` - Android native APIs
- `liblog.so` - Logging
- `libEGL.so` - EGL graphics
- `libGLESv3.so` - OpenGL ES 3.x
- `libjnigraphics.so` - Bitmap APIs
- `libm.so` - Math library
- `libdl.so` - Dynamic linker
- `libc.so` - C standard library

**NOT system-provided (must be bundled):**
- `libc++_shared.so` ⚠️ **MISSING FROM APK**

### Why This Happens
When Android's dynamic linker tries to load `libImmUnityPlugin.so`:
1. It discovers the dependency on `libc++_shared.so`
2. It searches for the library in the APK
3. It fails to find it
4. The entire load operation fails with a misleading error message

The error says "library 'ImmUnityPlugin' not found" but really means "dependency of ImmUnityPlugin not found".

## Solution

Copy `libc++_shared.so` from the Android NDK to the Unity project's plugin directory.

### Source Location
```
C:\Users\pjpar\AppData\Local\Android\Sdk\ndk\24.0.8215888\toolchains\llvm\prebuilt\windows-x86_64\sysroot\usr\lib\aarch64-linux-android\libc++_shared.so
```

### Destination
```
code\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a\libc++_shared.so
```

### Unity Metadata
Create a `.meta` file with these settings:
- `isPreloaded: 1` - Load early in the startup sequence
- `CPU: ARM64` - Target architecture
- `OS: Android` - Target platform

## Implementation Steps

1. **Locate the library** in the Android NDK
2. **Copy to Unity project** in the correct architecture folder
3. **Create .meta file** with proper Unity import settings
4. **Rebuild the Unity project** to include the library in the APK
5. **Deploy and test** on Quest device

## Expected Outcome

After this fix:
- ✅ `libc++_shared.so` will be included in the APK
- ✅ Android's dynamic linker will find all dependencies
- ✅ `libImmUnityPlugin.so` will load successfully
- ✅ The IMM Unity plugin will be functional

## Additional Notes

### Why Not Use c++_static?
You could alternatively rebuild the plugin with `ANDROID_STL=c++_static` to statically link the C++ library. This would:
- ✅ Eliminate the external dependency
- ❌ Increase the plugin size
- ❌ Require rebuilding the native code

The current approach (bundling `libc++_shared.so`) is simpler and doesn't require code changes.

### Graphics API Consideration
The logs show Unity is using Vulkan, but the plugin is built for OpenGL ES 3. This may cause rendering issues after the plugin loads. If rendering problems occur, consider:
- Forcing Unity to use OpenGL ES in Player Settings
- Or rebuilding the plugin with Vulkan support

## References

- Android NDK Stable APIs: https://developer.android.com/ndk/guides/stable_apis
- Unity Native Plugins: https://docs.unity3d.com/Manual/NativePlugins.html
- CMakeLists.txt configuration: `code/appImmUnity/CMakeLists.txt` (line 78-95)

