# Comparison: Upstream (alegna901/IMM) vs Icosa-Mirror

**Date:** January 4, 2026  
**Your Fork:** Sleepy-Pete/IMM (currently at upstream/main)  
**Comparison:** upstream/main vs icosa-mirror-version

---

## Executive Summary

**Icosa-mirror is SIGNIFICANTLY ahead** with 11 commits adding massive VR/Unity functionality.  
**Upstream only has** 1 minor commit (updated .gitignore for Android/Gradle).

### File Statistics

| Metric | Count |
|--------|-------|
| **Files Changed** | 536 files |
| **Lines Added** | +135,750 |
| **Lines Removed** | -959 |
| **Net Change** | +134,791 lines |

---

## What Icosa-Mirror Has (That Upstream Doesn't)

### 1. **Complete Unity Integration** ✅
- Full Unity sample project with OpenXR support
- Unity plugin DLL (ImmUnityPlugin.dll)
- C# scripts for IMM playback in Unity:
  - `ImmDocument.cs` - Document management
  - `ImmPlayerManager.cs` - Player lifecycle
  - `ImmFeatureExamples.cs` - Example implementations
  - `ImmDiagnostics.cs` - Debug tools
  - `OpenXRFlyRig.cs` - VR camera rig
- Unity scene with IMM viewer setup
- Complete deployment documentation

### 2. **VR SDK Integration** ✅
- **Oculus PC VR SDK** (LibOVR)
  - Full SDK with headers and libraries
  - OpenGL, DirectX, Vulkan support
  - 3,712 lines in OVR_CAPI.h alone
- **Oculus Platform SDK**
  - Social features, achievements, IAP
  - Multiplayer, voice chat (VoIP)
  - 446 header files
- **Audio360 SDK** (Facebook Spatial Audio)
  - 3D spatial audio engine
  - Ambisonic audio support
  - TBE (Two Big Ears) audio engine

### 3. **Third-Party Dependencies** ✅
All dependencies included with DLLs and headers:
- **libjpeg-turbo** - Fast JPEG encoding/decoding
- **libpng** - PNG image support
- **libogg** - Ogg container format
- **libvorbis** - Vorbis audio codec
- **opus** - Opus audio codec
- **libopusenc** - Opus encoding
- **zlib** - Compression

### 4. **Enhanced Player API** ✅
New runtime controls in `player.h`:
```cpp
// Runtime layer manipulation
bool SetLayerVisible(int docId, int layerId, bool visible);
bool SetLayerOpacity(int docId, int layerId, float opacity);
bool SetLayerTransform(int docId, int layerId, const trans3d & transform);
bool ClearLayerVisibilityOverride(int docId, int layerId);
bool ClearLayerTransformOverride(int docId, int layerId);

// Layer introspection
struct LayerInfo { /* 20+ fields */ };
struct LayerDiagnostics { /* visibility, opacity, transforms */ };
int GetLayerCount(int docId) const;
bool GetLayerInfoByIndex(int docId, int index, LayerInfo & info);
bool GetLayerDiagnostics(int docId, int layerId, LayerDiagnostics & outDiag);
bool IsSequenceReady(int docId) const;
```

### 5. **Unity Plugin Features** ✅
From `main.cpp` in appImmUnity:
- Document loading/unloading
- Playback control (play, pause, seek)
- Layer visibility/opacity control at runtime
- Transform overrides
- Performance metrics
- Diagnostics and introspection
- Thread-safe API

### 6. **Build System Improvements** ✅
- Fixed Visual Studio project paths
- Property sheets for all dependencies
- Proper include/lib paths
- x64 and Win32 configurations

### 7. **Example Content** ✅
- Sample IMM files in Unity project
- Untitled.imm (14 KB)
- sample1.imm (5.8 MB)

---

## What Upstream Has (That Icosa Doesn't)

### 1. **Updated .gitignore** ⚠️
Added Android/Gradle build patterns:
```gitignore
**/build/
**/.cxx/
**/.idea/
**/.gradle/
**/local.properties
**/*.a
**/*.so
**/exe/
```

**However**, icosa-mirror has MORE comprehensive .gitignore:
- Visual Studio specific files (*.suo, *.user, *.sdf, etc.)
- VS cache directories (.vs/, ipch/)
- More granular exclusions

---

## Detailed Commit History

### Icosa-Mirror Commits (11 total):

1. **079dfe0** - Added Audio360 SDK
2. **2db50bb** - Added Oculus PC VR SDK  
3. **c31f4c9** - Added Oculus Platform SDK
4. **793fa88** - Added other dependencies (jpeg, png, ogg, vorbis, opus, zlib)
5. **ee77d4b** - Setup dependencies in projects and fix includes
6. **1650e74** - Updated readme
7. **a53dc4a** - Fix paths to thirdparty
8. **b8edd60** - Fix vxproj
9. **49fb461** - Add missing files for a full Unity project
10. **7677d32** - Expose more runtime controls for Unity plugin
11. **4527e4a** - Update Unity sample project

### Upstream Commits (1 total):

1. **51e9c22** - Update .gitignore (Android/Gradle patterns)

---

## Recommendation

**✅ USE ICOSA-MIRROR AS YOUR BASE**

### Reasons:
1. **Massive functionality gain** - Unity integration, VR SDKs, runtime controls
2. **Production-ready** - Complete build system, dependencies included
3. **Well-documented** - Deployment guides, examples, sample projects
4. **Active development** - 11 commits vs 1
5. **Your use case** - You're testing VR, which requires these SDKs

### What to do about upstream's .gitignore:
Merge the Android/Gradle patterns into icosa-mirror's more comprehensive .gitignore.

---

## Next Steps

1. **Create a new branch on your fork** based on icosa-mirror-version
2. **Cherry-pick or merge** upstream's .gitignore improvements
3. **Push to your fork** (Sleepy-Pete/IMM)
4. **Continue development** on your fork with all the VR/Unity features

Would you like me to help you merge these changes into your fork?

