# Comparison: Old vs New Unity Plugin

## OLD PLUGIN (Windows x86_64 only)
Location: code/ImmUnitySampleProject/Assets/Plugins/x86_64/
File: ImmUnityPlugin.dll
Size: 2,075,648 bytes (2.0 MB)
Platform: Windows x86_64
Last Modified: 2026-01-04

Dependencies (separate DLLs):
- Audio360.dll
- jpeg62.dll
- libpng16.dll
- ogg.dll
- opus.dll
- opusenc.dll
- vorbis.dll
- vorbisenc.dll
- zlib1.dll

## NEW PLUGIN (Android ARM64)
Location: code/ImmUnitySampleProject/Assets/Plugins/Android/arm64-v8a/
File: libImmUnityPlugin.so
Size: 2,984,280 bytes (2.9 MB)
Platform: Android ARM64-v8a (Meta Quest)
Last Modified: 2026-01-06

Dependencies: ALL STATICALLY LINKED (no separate .so files needed)
- zlib 1.3.1
- libpng 1.6.40
- libjpeg-turbo 3.0.1
- libogg 1.3.5
- libvorbis 1.3.7
- opus 1.4

## KEY DIFFERENCES

### 1. Platform Support
OLD: Windows Editor/Standalone only
NEW: Android (Meta Quest) devices

### 2. Dependency Management
OLD: Dynamic linking - requires 9 separate DLL files
NEW: Static linking - single .so file with everything built-in

### 3. Size
OLD: 2.0 MB + dependencies
NEW: 2.9 MB (includes all dependencies)

### 4. Architecture
OLD: x86_64 (Intel/AMD 64-bit)
NEW: ARM64-v8a (ARM 64-bit for mobile/VR)

### 5. Build Configuration
OLD: Built for Windows with MSVC
NEW: Built for Android with Clang/LLVM, NDK 24

### 6. Graphics API
OLD: OpenGL/DirectX (Windows)
NEW: OpenGL ES 3.0/3.2 (Android/Quest)

### 7. Audio Backend
OLD: Full audio support (Windows audio APIs)
NEW: NULL audio backend (Android audio handled separately)

### 8. Renderer
OLD: Full renderer support including pretessellated
NEW: Static renderer only (pretessellated disabled for Android)

## WHAT THIS MEANS

Before: You could only test/run IMM content in Unity Editor on Windows
Now: You can build and deploy IMM content to Meta Quest VR headsets!

The new plugin enables:
- Running IMM content natively on Meta Quest devices
- VR playback of immersive media
- Standalone VR applications (no PC required)
- Testing on actual VR hardware

## COMPATIBILITY

Both plugins can coexist:
- Windows plugin: Used by Unity Editor and Windows builds
- Android plugin: Used by Android/Quest builds
- Unity automatically selects the correct plugin per platform
