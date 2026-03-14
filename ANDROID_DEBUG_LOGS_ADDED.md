# Android Debug Logs Added - IMM Unity Plugin

## Summary

Comprehensive logging has been added to both the Unity C# scripts and the native Android plugin to help diagnose why the IMMPlayer doesn't run on the Meta Quest headset.

## Changes Made

### 1. Unity C# Side (`ImmPlayerManager.cs`)

Added detailed logging to track:
- **Awake()**: Platform detection, Unity version, device model, graphics device type
- **Start()**: Initialization trigger
- **Initialize()**: Step-by-step initialization process including:
  - Configuration parameters (color space, antialiasing, paths)
  - Native plugin Init() call and return value
  - Render event function pointer
  - Success/failure states

### 2. Native Plugin Side (`main.cpp`)

Added Android logcat logging using `__android_log_print` with tag `ImmUnityPlugin`:

**Key logging points:**
- **UnityPluginLoad()**: Plugin library loading confirmation
- **iOnGraphicsDeviceEvent()**: Graphics device initialization and renderer type detection
- **Init()**: Complete initialization sequence including:
  - Input parameters (color space, antialiasing, temp folder)
  - Platform detection (Android vs Desktop)
  - Timer initialization
  - Sound backend creation and initialization
  - Renderer creation and initialization (OpenGL ES on Android)
  - IMM Player configuration and initialization
  - Success/failure at each step

## Log Tags to Filter

When viewing device logs, filter by:
- **Tag**: `ImmUnityPlugin` (native plugin logs)
- **Tag**: `Unity` (Unity engine logs)
- **Package**: Your app's package name (e.g., `com.DefaultCompany.ImmUnitySampleProject`)

## What to Look For in Logs

### Expected Success Flow:
```
ImmUnityPlugin: === UnityPluginLoad() called ===
ImmUnityPlugin: ImmUnityPlugin native library loaded successfully
ImmUnityPlugin: === Graphics Device Event: Initialize ===
ImmUnityPlugin: Unity Graphics Renderer Type: [number]
ImmUnityPlugin: Using OpenGL ES 3.0 (Android)
Unity: === ImmPlayerManager.Awake() called ===
Unity: Platform: Android
Unity: === ImmPlayerManager.Start() called ===
Unity: === IMM Player Initialization Started ===
ImmUnityPlugin: === Init() called ===
ImmUnityPlugin: Platform: Android - Skipping file-based logging
ImmUnityPlugin: Timer initialized successfully
ImmUnityPlugin: SoundBackend created successfully
ImmUnityPlugin: Platform: Android - Using OpenGL ES
ImmUnityPlugin: Renderer created successfully
ImmUnityPlugin: Renderer initialized successfully
ImmUnityPlugin: IMM Player initialized successfully
ImmUnityPlugin: === Init() completed successfully ===
Unity: === IMM Player Initialized Successfully ===
```

### Common Failure Points to Check:

1. **Plugin Not Loading**:
   - Missing: `UnityPluginLoad() called`
   - Cause: Plugin file not included in build or wrong architecture

2. **Graphics Initialization Failure**:
   - Missing: `Graphics Device Event: Initialize`
   - Wrong renderer type (should be OpenGL ES 3.0 on Quest)

3. **Init() Failure**:
   - Error at specific step (timer, sound, renderer, player)
   - Return code will indicate which component failed

4. **C# Script Not Running**:
   - Missing: `ImmPlayerManager.Awake() called`
   - Cause: GameObject not in scene or script disabled

## Next Steps

1. **Build your Unity project for Android** with the updated plugin
2. **Deploy to Meta Quest headset**
3. **Collect logs** using one of these methods:
   - ADB: `adb logcat -s ImmUnityPlugin Unity`
   - Meta Quest Developer Hub (MQDH)
   - Android Studio Logcat viewer
4. **Share the logs** to diagnose the issue

## Files Modified

- `code/ImmUnitySampleProject/Assets/Scripts/ImmPlayerManager.cs`
- `code/appImmUnity/src/main.cpp`
- `code/appImmUnity/android-build/arm64-v8a/libImmUnityPlugin.so` (rebuilt)
- `code/ImmUnitySampleProject/Assets/Plugins/Android/arm64-v8a/libImmUnityPlugin.so` (updated)

## Build Status

✅ Android plugin rebuilt successfully with logging
✅ Plugin copied to Unity project
✅ Ready for Unity build and deployment

---

**Note**: The logs use Android's logcat system, so they will appear in the device logs, not in Unity's console or log files.

