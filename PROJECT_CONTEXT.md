# IMM Project Context - Session Notes

**Last Updated:** January 5, 2026
**Project:** IMM (Immersive Media Format) - VR Viewer Development
**Repository:** https://github.com/Sleepy-Pete/IMM

---

## 🎯 Current Focus: Unity Plugin for Android/Quest

> **DECISION (Jan 5, 2026):** We are focusing **exclusively on the Unity path** for Android/Quest deployment.
> The native Android APK approach has been abandoned due to missing SDK dependencies and deprecated VrApi.

### Why Unity?
- ✅ Unity handles VR (OpenXR) - no VrApi/Platform SDK needed
- ✅ Unity handles audio - no Audio360 SDK needed
- ✅ Unity handles input - XR Interaction Toolkit
- ✅ Modern stack - OpenXR is the future, VrApi is deprecated
- ✅ One codebase - don't maintain two VR backends
- ✅ Plugin already has Android code paths (`#ifdef __ANDROID__`)

### Reference Projects
| Project | Location | Purpose |
|---------|----------|---------|
| **ImmUnitySampleProject** | `code/ImmUnitySampleProject/` | Sample Unity project in this repo |
| **Imm-Unity** | `a:\Github\Imm-Unity\` | **Working reference** with functional plugin |

---

## Project Overview

**IMM (Immersive Media)** is an API-neutral runtime immersive media delivery format for VR/AR content. It handles 3D models, paint strokes, 360 panoramas, audio, and animations in a compressed, streamable format.

This fork focuses on **Windows VR development** with Oculus headset support and **Unity plugin development** for cross-platform deployment including Meta Quest.

---

## Repository Structure

### Git Branches
- **`main`** - Original upstream sync (alegna901/IMM)
- **`vr-main`** - Active development branch (based on icosa-mirror)
- **`icosa-mirror-version`** - Local tracking of icosa-mirror/main

### Key Directories
```
IMM/
├── code/
│   ├── projects/
│   │   ├── windows/         # Visual Studio projects
│   │   └── android/         # Android Studio project (NEW)
│   ├── libImmCore/          # Core IMM library
│   ├── libImmImporter/      # IMM file import
│   ├── libImmPlayer/        # Playback engine
│   ├── appImmViewer/        # Multi-platform VR viewer
│   │   ├── exe/             # Built executables and DLLs (Windows)
│   │   ├── src/
│   │   │   ├── viewer/      # Core viewer code
│   │   │   ├── windows/     # Windows-specific code
│   │   │   └── android/     # Android-specific code (NEW)
│   │   ├── build.gradle     # Android app build config (NEW)
│   │   └── CMakeLists.txt   # Android native build (NEW)
│   ├── appImmUnity/         # Unity plugin
│   └── ImmUnitySampleProject/ # Unity sample project
├── thirdparty/              # All dependencies (vendored)
│   ├── audio360-sdk/        # Facebook spatial audio
│   ├── ovr-sdk/             # Oculus PC VR SDK (Windows)
│   ├── ovr-mobile-sdk/      # Oculus Mobile SDK (Android - download required)
│   ├── ovr-platform-sdk/    # Oculus Platform SDK
│   ├── libjpeg-turbo/
│   ├── libpng/
│   ├── libogg/
│   ├── libvorbis/
│   ├── opus/
│   └── zlib/
└── exampleImmFiles/         # Sample IMM files
```

---

## Build Status

### ✅ Windows Build (Successfully Built - January 4, 2026)
- **Configuration:** Release x64
- **IDE:** Visual Studio 2022
- **Windows SDK:** 10.0.26100
- **Build Order:** libImmCore → libImmImporter → libImmPlayer → appImmViewer

**Build Scripts:**
- `build.bat` - Full automated build
- `test_viewer.bat` - Run viewer (non-VR)
- `test_viewer_vr.bat` - Run viewer (VR mode)
- `test_viewer_simple.bat` - Simple test runner

**Build Output:**
- **Executable:** `code/appImmViewer/exe/appImmViewer_Release.exe`
- **Size:** ~2.5 MB
- **Dependencies:** All DLLs in exe/ directory

### ⚠️ Native Android APK (ABANDONED - January 5, 2026)
> **Decision:** Native Android APK path abandoned in favor of Unity plugin approach.
>
> **Reason:** Missing Android libraries for Platform SDK and Audio360 SDK, plus VrApi is deprecated.
> All native Android build files remain in repo but are not actively maintained.

**What was completed:**
- ✅ Gradle 8.11.1 + AGP 8.5.2 infrastructure
- ✅ CMake native build configuration
- ✅ All 67 C++ files compile successfully
- ❌ Linking fails - missing Platform SDK and Audio360 for Android

### ✅ Unity Plugin (ACTIVE - January 5, 2026)
- **Target:** Cross-platform (Windows Editor + Android/Quest)
- **Sample Project:** `code/ImmUnitySampleProject/`
- **Reference Project:** `a:\Github\Imm-Unity\` (working implementation)
- **XR System:** OpenXR 1.14.3
- **Plugin:** `ImmUnityPlugin.dll` (Windows) / `libImmUnityPlugin.so` (Android)

**Current State (Windows):**
- ✅ Windows DLL builds and works
- ✅ C# wrapper scripts complete
- ✅ OpenXR integration configured

**TODO (Android):**
- [ ] Build libImmUnityPlugin.so for ARM64
- [ ] Copy to Unity Plugins/Android folder
- [ ] Test on Quest device

---

## VR Testing Results

### ✅ Non-VR Mode Test (Successful)
- **File:** sample1.imm (5.8 MB)
- **Load Time:** 49 ms CPU, 2 ms SPU, 0 ms GPU
- **Memory:** 27 MB peak, 99 textures/buffers
- **Status:** No memory leaks, clean exit

### ✅ VR Mode Test (Successful)
- **Headset:** Oculus (detected via Oculus Virtual Audio Device)
- **Rendering:** Fast stereo enabled
- **Load Time:** 105 ms CPU, 3 ms SPU, 0 ms GPU
- **Memory:** 69 MB peak, 109 textures/buffers
- **Performance:** 2-3x resource usage vs non-VR (expected)
- **Status:** VR initialization successful, no memory leaks

### VR Configuration
File: `code/appImmViewer/exe/settings.json`
```json
{
  "Rendering": {
    "EnableVR": true,
    "RenderingAPI": "OpenGL",
    "RenderingTechnique": "Static",
    "PixelDensity": 1.0,
    "Supersampling": 1
  },
  "File": {
    "Load": [ "../../../exampleImmFiles/sample1.imm" ]
  }
}
```

---

## Known Issues

### ⚠️ Audio Seek Warnings (Non-Critical)
```
piSoundEngineAudioSDK::Play(): SEEK FAILED!
```
- Appears in both VR and non-VR modes
- Does not prevent audio playback
- Audio360 SDK initializes correctly
- May be related to OGG OPUS format handling

### 📋 Pending Tests
- Test with larger IMM files (e.g., TheArtofChange_v76.imm)
- Test VR controller interaction
- Test spawn area teleportation
- Test spatial audio positioning
- Unity plugin integration

---

## Key Features (from icosa-mirror fork)

### Enhanced Player API
Runtime layer manipulation:
- `SetLayerVisible()` - Toggle layer visibility
- `SetLayerOpacity()` - Adjust layer opacity
- `SetLayerTransform()` - Transform layers at runtime
- `GetLayerInfo()` - Introspect layer properties
- `GetLayerDiagnostics()` - Debug layer state

### VR Support
- Oculus PC VR SDK integration
- Fast stereo rendering
- Spatial audio (Audio360)
- VR-specific spawn areas

### Unity Integration
- Complete Unity plugin (ImmUnityPlugin.dll)
- OpenXR support
- C# API for IMM playback
- Sample project included

---

## Attribution & Upstream

### Repository Lineage
```
Immersive-Foundation/IMM (Inigo Quilez, et al.)
    ↓
alegna901/IMM (Angela Luo) - Current upstream
    ↓
icosa-mirror/IMM (Joan Charmant, Andy Baker) - VR/Unity work
    ↓
Sleepy-Pete/IMM (You) - Active development
```

### Git Remotes
- **origin:** https://github.com/Sleepy-Pete/IMM.git (your fork)
- **upstream:** https://github.com/alegna901/IMM.git (Angela Luo)
- **icosa-mirror:** https://github.com/icosa-mirror/IMM.git (Joan Charmant)

---

## Next Session TODO

### 🔴 HIGH PRIORITY - Unity Plugin for Android/Quest
- [ ] **Test ImmUnitySampleProject** in Unity Editor (Windows) - verify current state
- [ ] **Reference Imm-Unity project** (`a:\Github\Imm-Unity\`) - study working implementation
- [ ] **Build libImmUnityPlugin.so** for Android ARM64:
  - Create CMakeLists.txt for Unity plugin Android build
  - Build libImmCore, libImmImporter, libImmPlayer for ARM64
  - Link into libImmUnityPlugin.so
- [ ] **Deploy to Unity:**
  - Copy .so to `Assets/Plugins/Android/arm64-v8a/`
  - Configure plugin import settings (ARM64, Android)
- [ ] **Build APK from Unity:**
  - Switch platform to Android
  - Configure XR settings for Quest (OpenXR)
  - Build and deploy to Quest
- [ ] **Test on Quest:**
  - Verify IMM loading
  - Test rendering quality
  - Check performance

### 🟡 MEDIUM PRIORITY - Windows
- [ ] Test with TheArtofChange_v76.imm (production content)
- [ ] Investigate audio seek warnings
- [ ] Test VR controller input
- [ ] Profile VR performance with complex scenes

### 🟢 LOW PRIORITY
- [ ] Compare with upstream for new changes
- [ ] Set up CI/CD for automated builds
- [ ] Create developer documentation

---

## Useful Commands

### Windows Build
```batch
build.bat                    # Full rebuild (Windows)
```

### Windows Test
```batch
test_viewer.bat              # Non-VR test
test_viewer_vr.bat           # VR test
```

### Android Build
```batch
verify_android_setup.bat     # Check Android setup
setup_oculus_sdk.bat         # Install Oculus Mobile SDK
build_android.bat debug      # Build debug APK
build_android.bat release    # Build release APK
build_android.bat clean      # Clean build
```

### Android Deploy
```batch
deploy_to_quest.bat debug    # Deploy debug APK to Quest
deploy_to_quest.bat release  # Deploy release APK to Quest
adb devices                  # Check connected devices
adb logcat -s ImmViewer:V    # View app logs
```

### Git
```bash
git checkout vr-main         # Switch to development branch
git fetch --all              # Update all remotes
git log --oneline -10        # View recent commits
```

---

## System Requirements

### Windows Development
- Windows 10/11
- Visual Studio 2022
- Windows SDK 10.0.26100
- 16+ GB RAM recommended

### Android Development
- Windows 10/11, macOS, or Linux
- Android Studio Hedgehog (2023.1.1) or later
- Android SDK Platform 33
- Android NDK 25.2.9519653
- CMake 3.22.1
- 16+ GB RAM recommended
- 20+ GB free disk space

### Windows VR Runtime
- Oculus headset (Quest 2/3, Rift S, etc.)
- Oculus PC software running
- NVIDIA RTX 3090 or equivalent
- 8+ GB VRAM recommended

### Android VR Runtime (Quest Standalone)
- Meta Quest 2, Quest 3, or Quest Pro
- Developer Mode enabled
- USB-C cable for deployment
- Android 8.0 (API 26) or higher

---

## Unity Plugin Development Notes

### Current State (ImmUnitySampleProject)
- **Location:** `code/ImmUnitySampleProject/`
- **Unity Version:** Check `ProjectSettings/ProjectVersion.txt`
- **XR System:** OpenXR 1.14.3 (migrated from Oculus SDK)
- **Scripts:** 8 C# scripts in `Assets/Scripts/`
- **Plugins (Windows):** `Assets/Plugins/x86_64/` contains Windows DLLs

### Key C# Scripts
| Script | Purpose |
|--------|---------|
| `ImmNativePlugin.cs` | P/Invoke declarations for native DLL |
| `ImmPlayerManager.cs` | Singleton manager for plugin lifecycle |
| `ImmDocument.cs` | Represents a loaded IMM document |
| `ImmPlayerExample.cs` | Example usage component |
| `ImmDiagnostics.cs` | Debug/diagnostics utilities |
| `OpenXRFlyRig.cs` | VR locomotion for OpenXR |

### Native Plugin Interface
The plugin DLL name is `ImmUnityPlugin` (no extension - Unity handles per-platform).

**Key exports:**
```csharp
[DllImport("ImmUnityPlugin")] Init(colorSpace, antialiasing, logFile, tempFolder)
[DllImport("ImmUnityPlugin")] Load(fileName)
[DllImport("ImmUnityPlugin")] LoadFromMemory(fileName, size, data)
[DllImport("ImmUnityPlugin")] Unload(id)
[DllImport("ImmUnityPlugin")] GetRenderEventFunc()
[DllImport("ImmUnityPlugin")] GlobalWork(stepsPerFrame)
// ... playback controls (Pause, Resume, Next, Prev, etc.)
```

### Android Plugin Requirements
For Quest/Android, need to provide:
```
Assets/Plugins/Android/arm64-v8a/
├── libImmUnityPlugin.so      # Main plugin
└── (dependencies bundled in or system-provided)
```

### Important Notes for Android Build
1. **Graphics API:** Plugin uses GLES on Android (`piRenderer::API::GLES`)
2. **No Audio360 needed:** Unity handles audio, plugin disables `mRenderReporter` on Android
3. **Platform defines:** Code uses `#if defined(__ANDROID__) || defined(ANDROID)`
4. **Unity callbacks:** Must implement `UnityPluginLoad()` and `UnityPluginUnload()`

### Reference: Imm-Unity Project
**Location:** `a:\Github\Imm-Unity\`
**Purpose:** Working Unity project with functional IMM plugin
**Use for:** Compare plugin setup, build configuration, scene setup

### Build Pipeline for Android
```
1. Build native libraries (CMake + NDK):
   libImmCore.a → libImmImporter.a → libImmPlayer.a
                           ↓
   Link all into: libImmUnityPlugin.so

2. Copy to Unity:
   libImmUnityPlugin.so → Assets/Plugins/Android/arm64-v8a/

3. Unity build:
   File → Build Settings → Android → Build
```

---

## Contact & Resources

### Windows Build
- **IMM Specification:** See README.md
- **Build Issues:** Check BUILD_SUCCESS.md
- **Test Results:** See TEST_RESULTS.md, VR_TEST_RESULTS.md
- **Comparison:** See COMPARISON_UPSTREAM_VS_ICOSA.md

### Android Build
- **Quick Start:** ANDROID_QUICK_START.md
- **Complete Guide:** ANDROID_BUILD_GUIDE.md (724 lines)
- **SDK Downloads:** SDK_DOWNLOAD_GUIDE.md
- **Technical Summary:** ANDROID_PROJECT_SUMMARY.md
- **Quick Reference:** README_ANDROID.md
- **Meta Quest Docs:** https://developer.oculus.com/
- **VrApi Docs:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/

---

## Android Build System Summary

### Created Files (19 total)
**Project Configuration (10 files):**
- `code/projects/android/build.gradle`
- `code/projects/android/settings.gradle`
- `code/projects/android/gradle.properties`
- `code/projects/android/gradle/wrapper/gradle-wrapper.properties`
- `code/projects/android/README.md`
- `code/appImmViewer/build.gradle`
- `code/appImmViewer/CMakeLists.txt`
- `code/appImmViewer/proguard-rules.pro`
- `code/libImmCore/CMakeLists.txt`
- `code/libImmImporter/CMakeLists.txt`
- `code/libImmPlayer/CMakeLists.txt`

**Documentation (6 files):**
- `ANDROID_BUILD_GUIDE.md` (724 lines)
- `ANDROID_QUICK_START.md`
- `SDK_DOWNLOAD_GUIDE.md`
- `ANDROID_PROJECT_SUMMARY.md`
- `README_ANDROID.md`

**Automation Scripts (4 files):**
- `build_android.bat`
- `deploy_to_quest.bat`
- `setup_oculus_sdk.bat`
- `verify_android_setup.bat`

### Key Features
✅ Complete Android Studio project structure
✅ Native C++ build for ARM64 (CMake)
✅ VrApi integration configured
✅ Automated build and deployment scripts
✅ Comprehensive documentation (6 guides)
✅ Setup verification tools
✅ Third-party library linking with fallback

### Status
⏳ **Ready to build** - Requires Oculus Mobile SDK download
📖 **Start here:** `ANDROID_QUICK_START.md`

---

**End of Context File**

