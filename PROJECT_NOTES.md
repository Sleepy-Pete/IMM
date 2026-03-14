# IMM2 Project Development Notes

## Project Overview
This project aims to achieve **feature parity** between Windows and Android platforms for IMM (Immersive Media) playback, with the ultimate goal of cross-platform distribution.

---

## Primary Goals

### 1. **Android Platform Stability** (Current Priority)
- **Objective:** Achieve a stable Android build with full Windows functionality
- **Target Devices:**
  - Meta Quest (primary target)
  - Other Android-based VR headsets (future)
  - Apple Vision Pro (future consideration)
- **Status:** In development
- **Next Milestone:** Build for Android distribution on headset

### 2. **Windows Platform Maintenance**
- **Current Status:**
  - ✅ Visual Studio build works
  - ✅ Unity Plugin runs sample IMM files
  - ⚠️ **Known Issue:** Large projects crash
    - **Possible Cause:** IMM files from newer Quill versions
    - **Action Required:** Investigation needed

### 3. **Cross-Platform Distribution**
- **Target Platforms:**
  - Meta Quest (Android)
  - Windows PC VR
  - Apple Vision Pro (future)
  - Other Android-based VR devices (future)

---

## Known Issues & Investigation Areas

### Windows Platform Issues
1. **Large Project Crashes**
   - **Symptom:** Unity crashes when loading large IMM files
   - **Hypothesis:** Version mismatch between Quill exporter and IMM player
   - **Investigation Needed:**
     - Determine Quill version used to create problematic IMM files
     - Compare IMM format versions
     - Check memory usage and resource limits
     - Validate IMM file structure

### Android Platform Gaps
Based on codebase analysis, the following features are **disabled or excluded** on Android:

1. **Audio360 Spatial Audio SDK** ❌
   - Windows: Full support
   - Android: Disabled (Unity handles audio)
   - **Impact:** No ambisonic audio, no Audio360 spatial features

2. **Pretessellated Paint Renderer** ❌
   - Windows: Available
   - Android: Excluded (requires offline shader compilation)
   - **Impact:** May affect performance on complex paint layers

3. **OGG/OPUS Audio Codecs** ❌
   - Windows: Supported
   - Android: Excluded (external libraries not built)
   - **Impact:** Limited to WAV audio format
   - **Action Required:** Build OGG/OPUS libraries for Android if needed

4. **Render Reporter** ❌
   - Windows: Enabled
   - Android: Disabled
   - **Impact:** Less detailed rendering statistics

---

## Quill Integration & IMM Export

### Critical Investigation Area
**IMPORTANT:** There is a Quill directory and terminal that needs investigation for:
- Understanding how IMM file export works
- Determining if there's a direct export to Quill files
- Investigating version compatibility issues

### Action Items
- [ ] Locate Quill directory in codebase
- [ ] Examine Quill exporter implementation
- [ ] Document IMM export process
- [ ] Identify version compatibility requirements
- [ ] Test export with various Quill versions
- [ ] Determine if bidirectional conversion (IMM ↔ Quill) is possible

---

## Development Priorities

### Phase 1: Android Stability (Current)
- [ ] Complete Android build configuration
- [ ] Test core IMM playback on Quest
- [ ] Validate rendering pipeline (OpenGL ES 3)
- [ ] Test audio playback (Unity audio system)
- [ ] Performance testing on Quest hardware

### Phase 2: Feature Parity
- [ ] Investigate Audio360 alternatives for Android
- [ ] Build OGG/OPUS codec support for Android
- [ ] Optimize paint rendering without pretessellated renderer
- [ ] Implement render statistics for Android

### Phase 3: Windows Stability
- [ ] Investigate large project crashes
- [ ] Test with various Quill versions
- [ ] Validate IMM format compatibility
- [ ] Memory profiling and optimization

### Phase 4: Quill Integration
- [ ] Document Quill exporter functionality
- [ ] Establish version compatibility matrix
- [ ] Create testing pipeline for IMM files
- [ ] Implement validation tools

### Phase 5: Cross-Platform Distribution
- [ ] Package for Meta Quest store
- [ ] Create Windows installer/distribution
- [ ] Explore Vision Pro compatibility
- [ ] Support additional Android VR devices

---

## Technical Considerations

### Platform-Specific Rendering
- **Windows:** DirectX 11/12, OpenGL 4.x
- **Android:** OpenGL ES 3 only
- **Implication:** Shader code must support both paths

### Audio Architecture
- **Windows:** Audio360 SDK + DirectSound
- **Android:** Unity audio system only
- **Implication:** Spatial audio features differ between platforms

### Build System
- **Windows:** Visual Studio + CMake
- **Android:** CMake + Android NDK
- **Unity:** Plugin for both platforms

---

## Resources & References

### Key Directories
- `/player/` - Core IMM player implementation
- `/unity/` - Unity plugin integration
- `/quill/` - **[TO INVESTIGATE]** Quill exporter and format tools

### Key Files
- `CMakeLists.txt` - Build configuration
- `player/src/immUnityPlugin.cpp` - Unity plugin entry point
- `player/src/immPlayer.cpp` - Core player logic

### Documentation
- `PLATFORM_COMPATIBILITY.md` - Detailed platform differences (see previous analysis)

---

## Notes for Future Development

1. **Always test on actual hardware** - Quest behavior differs from Unity Editor
2. **Monitor memory usage** - Large IMM files may have memory constraints on mobile
3. **Version compatibility is critical** - Track Quill versions used for IMM creation
4. **Audio format matters** - Use WAV for Android compatibility
5. **Performance profiling** - Paint-heavy scenes need optimization on Quest

---

## Questions to Answer

1. What is the exact IMM format version supported?
2. Which Quill versions are compatible?
3. Can we implement Audio360 features using Unity's spatial audio?
4. What is the maximum IMM file size/complexity for Quest?
5. Is there a Quill → IMM → Quill round-trip conversion?
6. What causes Windows crashes on large projects?

---

**Last Updated:** 2026-01-05
**Status:** Active Development - Android Build Phase

