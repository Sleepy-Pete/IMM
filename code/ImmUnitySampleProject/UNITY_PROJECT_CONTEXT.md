# IMM Unity Sample Project - Configuration Context

**Last Updated:** 2026-01-05  
**Unity Version:** 2022.3.8f1  
**Project Location:** `code/ImmUnitySampleProject/`

---

## Project Overview

This Unity project demonstrates the IMM (Immersive Media) player plugin for VR/AR content playback. It supports both Windows PC VR and Meta Quest 3 (Android) platforms using OpenXR.

---

## VR Configuration for Meta Quest 3

### XR Management Setup

**Status:** ✅ Fully Configured

#### Build Targets Configured:
1. **Standalone (Windows)** - For PC VR testing
   - OpenXR Loader enabled
   - Automatic loading: Disabled
   - Automatic running: Disabled

2. **Android (Meta Quest 3)** - For Quest deployment
   - OpenXR Loader enabled
   - Automatic loading: **Enabled**
   - Automatic running: **Enabled**
   - Oculus Quest Support: **Enabled**

#### Key Configuration Files:
- `Assets/XR/XRGeneralSettingsPerBuildTarget.asset` - XR loader configuration per platform
- `Assets/XR/Settings/OpenXR Package Settings.asset` - OpenXR features and settings
- `Assets/XR/Loaders/OpenXRLoader.asset` - OpenXR loader instance

### OpenXR Features

**Enabled Features:**
- ✅ **OculusQuestFeature Android** - Meta Quest device support
- ✅ OpenXR runtime initialization for Android
- ✅ XR_OCULUS_android_initialize_loader extension

**Configuration Location:**
- File: `Assets/XR/Settings/OpenXR Package Settings.asset`
- Feature ID: `com.unity.openxr.feature.oculusquest`
- Targets: Quest 1, Quest 2, Quest 3

---

## Android Build Configuration

### SDK Requirements

**Minimum SDK Version:** API 24 (Android 7.0)  
**Target SDK Version:** Automatic (0)  
**Architecture:** ARM64

**Why API 24?**
- OpenXR loader requires minimum API 24
- Meta Quest devices run Android 10+ (API 29+)
- API 24 ensures compatibility with OpenXR requirements

### Build Settings

**File:** `ProjectSettings/ProjectSettings.asset`

Key Android settings:
```yaml
AndroidMinSdkVersion: 24          # Required for OpenXR
AndroidTargetSdkVersion: 0        # Automatic
AndroidTargetArchitectures: 1     # ARM64
AndroidTargetDevices: 0           # All devices
```

### Graphics API

**Android Graphics APIs:**
- OpenGL ES 3.0+ (Primary)
- Vulkan (Secondary)
- Automatic API selection enabled

---

## Unity Packages

### Core Packages (manifest.json)

**XR Packages:**
- `com.unity.xr.management` v4.5.4 - XR plugin management system
- `com.unity.xr.openxr` v1.14.3 - OpenXR implementation

**Standard Packages:**
- `com.unity.collab-proxy` v2.11.2
- `com.unity.feature.development` v1.0.1
- `com.unity.textmeshpro` v3.0.7
- `com.unity.timeline` v1.7.7
- `com.unity.ugui` v1.0.0
- `com.unity.visualscripting` v1.9.4

**Note:** The `com.ivanmurzak.unity.mcp` package was removed due to build conflicts with test assemblies.

---

## Project Structure

### Assets Organization

```
Assets/
├── ExampleImmFiles/          # Sample .imm files for testing
├── Plugins/
│   ├── Android/             # Android native libraries
│   │   └── arm64-v8a/       # ARM64 architecture for Quest
│   │       └── libImmUnityPlugin.so  # Android Unity plugin
│   └── x86_64/              # Windows native DLLs
│       ├── ImmUnityPlugin.dll
│       ├── Audio360.dll
│       └── [other dependencies]
├── Resources/               # Runtime resources (empty after cleanup)
├── Scenes/
│   └── SampleScene.unity    # Main demo scene
├── Scripts/                 # C# wrapper scripts
│   ├── ImmNativePlugin.cs
│   ├── ImmPlayerManager.cs
│   ├── ImmDocument.cs
│   ├── ImmPlayerExample.cs
│   ├── ImmDiagnostics.cs
│   ├── ImmFeatureExamples.cs
│   ├── ImmFeatureExamplesEditor.cs
│   └── OpenXRFlyRig.cs
└── XR/                      # XR configuration assets
    ├── Loaders/
    ├── Settings/
    └── XRGeneralSettingsPerBuildTarget.asset
```

---

## C# Scripts Overview

### Core Plugin Scripts

| Script | Purpose |
|--------|---------|
| `ImmNativePlugin.cs` | P/Invoke declarations for native DLL interface |
| `ImmPlayerManager.cs` | Singleton manager for plugin lifecycle and rendering |
| `ImmDocument.cs` | Represents a loaded IMM document with playback controls |
| `ImmPlayerExample.cs` | Example component demonstrating plugin usage |
| `ImmDiagnostics.cs` | Debug and diagnostics utilities |
| `ImmFeatureExamples.cs` | Feature demonstration component |
| `ImmFeatureExamplesEditor.cs` | Custom editor for feature examples |
| `OpenXRFlyRig.cs` | VR locomotion controller for OpenXR |

### Key Features

**ImmPlayerManager:**
- Initializes native plugin on startup
- Manages render events and camera matrices
- Handles color space and antialiasing settings
- Persists across scene loads (DontDestroyOnLoad)

**ImmDocument:**
- Load/unload IMM files
- Playback control (play, pause, stop, restart)
- Chapter navigation
- Volume control
- Bounding box queries
- Spawn area management

---

## Build Process

### Building for Meta Quest 3

1. **Switch Platform:**
   - File → Build Settings
   - Select "Android"
   - Click "Switch Platform"

2. **Configure Build:**
   - Ensure XR settings are enabled (Project Settings → XR Plug-in Management → Android)
   - Verify OpenXR is selected as XR plugin
   - Check that Oculus Quest Support feature is enabled

3. **Build APK:**
   - File → Build Settings → Build
   - Or use Build and Run to deploy directly to connected Quest

### Build Output

**Location:** `Builds/Imm-Test.apk`

---

## Known Issues & Solutions

### Issue: Build fails with "minSdkVersion 22 cannot be smaller than version 24"
**Solution:** ✅ Fixed - AndroidMinSdkVersion updated to 24 in ProjectSettings.asset

### Issue: MCP package test assemblies causing build errors
**Solution:** ✅ Fixed - Removed com.ivanmurzak.unity.mcp package from manifest.json

### Issue: VR not working on Quest
**Solution:** Ensure OculusQuestFeature is enabled in OpenXR Package Settings

### Issue: IMM works in Editor but not in Android builds
**Cause:** Missing Android native library - only Windows x86_64 DLLs present
**Solution:** ✅ Build Android plugin using `code/appImmUnity/build_android.bat`
**Details:** See `ANDROID_PLUGIN_BUILD_GUIDE.md` for complete instructions

---

## Testing & Deployment

### Testing on Meta Quest 3

1. Enable Developer Mode on Quest 3
2. Connect Quest to PC via USB-C
3. Use "Build and Run" in Unity
4. Or manually install APK: `adb install -r Builds/Imm-Test.apk`

### Verification Steps

- [ ] XR Management shows Android platform configured
- [ ] OpenXR loader is enabled for Android
- [ ] Oculus Quest Support feature is enabled
- [ ] AndroidMinSdkVersion is 24 or higher
- [ ] Build completes without errors
- [ ] App launches on Quest in VR mode

---

## Related Documentation

**Repo docs (moved to docs/ in July 2026):**
- `docs/history/PROJECT_CONTEXT.md` - Overall project context and structure
- `docs/android/ANDROID_BUILD_GUIDE.md` - Native Android build guide
- `README.md` - IMM format specification

**Unity Scripts:**
- `Assets/Scripts/README.md` - Detailed C# API documentation
- `Assets/Scripts/DEPLOYMENT.md` - DLL deployment instructions

**Build Guides:**
- `docs/history/BUILD_SUCCESS.md` - Windows build status
- `docs/android/ANDROID_QUICK_START.md` - Android quick start guide
- `ANDROID_PLUGIN_BUILD_GUIDE.md` - **Android Unity plugin build instructions**

---

**End of Unity Project Context**

