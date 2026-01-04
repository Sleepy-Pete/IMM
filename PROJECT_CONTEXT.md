# IMM Project Context - Session Notes

**Last Updated:** January 4, 2026  
**Project:** IMM (Immersive Media Format) - VR Viewer Development  
**Repository:** https://github.com/Sleepy-Pete/IMM

---

## Project Overview

**IMM (Immersive Media)** is an API-neutral runtime immersive media delivery format for VR/AR content. It handles 3D models, paint strokes, 360 panoramas, audio, and animations in a compressed, streamable format.

This fork focuses on **Windows VR development** with Oculus headset support.

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
│   ├── libImmCore/          # Core IMM library
│   ├── libImmImporter/      # IMM file import
│   ├── libImmPlayer/        # Playback engine
│   ├── appImmViewer/        # Windows VR viewer (our focus)
│   │   ├── exe/             # Built executables and DLLs
│   │   └── src/             # Viewer source code
│   ├── appImmUnity/         # Unity plugin
│   └── ImmUnitySampleProject/ # Unity sample project
├── thirdparty/              # All dependencies (vendored)
│   ├── audio360-sdk/        # Facebook spatial audio
│   ├── ovr-sdk/             # Oculus PC VR SDK
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

### ✅ Successfully Built (January 4, 2026)
- **Configuration:** Release x64
- **IDE:** Visual Studio 2022
- **Windows SDK:** 10.0.26100
- **Build Order:** libImmCore → libImmImporter → libImmPlayer → appImmViewer

### Build Scripts Created
- `build.bat` - Full automated build
- `test_viewer.bat` - Run viewer (non-VR)
- `test_viewer_vr.bat` - Run viewer (VR mode)
- `test_viewer_simple.bat` - Simple test runner

### Build Output
- **Executable:** `code/appImmViewer/exe/appImmViewer_Release.exe`
- **Size:** ~2.5 MB
- **Dependencies:** All DLLs in exe/ directory

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

### High Priority
- [ ] Test with TheArtofChange_v76.imm (production content)
- [ ] Investigate audio seek warnings
- [ ] Test VR controller input
- [ ] Document VR controls and interaction

### Medium Priority
- [ ] Test Unity plugin integration
- [ ] Profile VR performance with complex scenes
- [ ] Test multiple IMM file loading
- [ ] Explore runtime layer manipulation API

### Low Priority
- [ ] Compare with upstream for new changes
- [ ] Set up CI/CD for automated builds
- [ ] Create developer documentation
- [ ] Package release build with installer

---

## Useful Commands

### Build
```batch
build.bat                    # Full rebuild
```

### Test
```batch
test_viewer.bat              # Non-VR test
test_viewer_vr.bat           # VR test
```

### Git
```bash
git checkout vr-main         # Switch to development branch
git fetch --all              # Update all remotes
git log --oneline -10        # View recent commits
```

---

## System Requirements

### Development
- Windows 10/11
- Visual Studio 2022
- Windows SDK 10.0.26100
- 16+ GB RAM recommended

### VR Runtime
- Oculus headset (Quest 2/3, Rift S, etc.)
- Oculus PC software running
- NVIDIA RTX 3090 or equivalent
- 8+ GB VRAM recommended

---

## Contact & Resources

- **IMM Specification:** See README.md
- **Build Issues:** Check BUILD_SUCCESS.md
- **Test Results:** See TEST_RESULTS.md, VR_TEST_RESULTS.md
- **Comparison:** See COMPARISON_UPSTREAM_VS_ICOSA.md

---

**End of Context File**

