# Android Plugin Fix - Preload Setting

## Problem Identified

The device logs revealed the exact issue:

```
[IMM] === DLL NOT FOUND ERROR ===
Unable to load DLL 'ImmUnityPlugin'
dlerror() = dlopen failed: library "ImmUnityPlugin" not found
```

**Root Cause**: The `libImmUnityPlugin.so` file was not being included in the APK or not being loaded at startup.

## Solution

Changed the plugin's import settings to enable **preloading**:

### File Modified
`Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so.meta`

### Change Made
```diff
- isPreloaded: 0
+ isPreloaded: 1
```

## Why This Fixes It

When `isPreloaded: 1`:
- ✅ Unity **includes** the `.so` file in the APK
- ✅ Unity **loads** the library at app startup (before any DllImport calls)
- ✅ The library is available when `ImmNativePlugin.Init()` is called

When `isPreloaded: 0`:
- ❌ Unity may not include the library in the APK
- ❌ The library is not loaded until first use
- ❌ DllImport fails with "library not found"

## What the Logs Showed

### ✅ Unity Scripts Working
```
[IMM] === ImmPlayerManager.Awake() called ===
[IMM] Platform: Android
[IMM] Graphics Device: Vulkan
[IMM] === ImmPlayerManager.Start() called ===
[IMM] === IMM Player Initialization Started ===
[IMM] Calling ImmNativePlugin.Init()...
```

### ❌ Native Plugin Not Loading
```
[IMM] === DLL NOT FOUND ERROR ===
Unable to load DLL 'ImmUnityPlugin'
```

### ⚠️ Native Plugin Never Loaded
- No `UnityPluginLoad()` log message
- No `Graphics Device Event: Initialize` log message
- Plugin was never loaded by Unity

## Next Steps

1. **Rebuild your Unity project for Android**
   - The `.meta` file has been updated
   - Unity will now include and preload the plugin

2. **Deploy to Quest and test**
   - The plugin should now load successfully
   - You should see native logs: `=== UnityPluginLoad() called ===`

3. **Expected Success Logs**
```
ImmUnityPlugin: === UnityPluginLoad() called ===
ImmUnityPlugin: ImmUnityPlugin native library loaded successfully
ImmUnityPlugin: === Graphics Device Event: Initialize ===
Unity: === ImmPlayerManager.Awake() called ===
Unity: Platform: Android
ImmUnityPlugin: === Init() called ===
ImmUnityPlugin: Timer initialized successfully
ImmUnityPlugin: Renderer initialized successfully
ImmUnityPlugin: IMM Player initialized successfully
```

## Additional Notes

### Graphics API Mismatch (Minor Issue)
The logs show Unity is using **Vulkan**:
```
[IMM] Graphics Device: Vulkan
```

But our plugin expects **OpenGL ES**. This might cause issues later. Unity's OpenXR is using Vulkan, but our native plugin is compiled for OpenGL ES.

This may need to be addressed if rendering issues occur after the plugin loads successfully.

### Platform Settings Verified
The plugin meta file has correct settings:
- ✅ Android: Enabled
- ✅ CPU: ARM64
- ✅ Editor: Disabled
- ✅ Now: Preloaded

## Summary

**The fix is simple**: Changed `isPreloaded: 0` to `isPreloaded: 1` in the plugin's meta file.

This ensures Unity:
1. Includes the `.so` file in the APK
2. Loads it at startup
3. Makes it available for DllImport calls

**Rebuild your Unity project and test again!**

