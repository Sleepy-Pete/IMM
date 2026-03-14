# SDK Download and Setup Guide

This guide provides detailed instructions for downloading and setting up all required SDKs and libraries for building the IMM Viewer Android APK.

---

## Table of Contents

1. [Oculus Mobile SDK](#oculus-mobile-sdk)
2. [Audio360 SDK](#audio360-sdk)
3. [Third-Party Libraries](#third-party-libraries)
4. [Verification](#verification)

---

## Oculus Mobile SDK

### What is it?
The Oculus Mobile SDK (also called VrApi) is Meta's native SDK for developing VR applications on Quest devices. It provides low-level access to VR features like head tracking, rendering, and input.

### Why do we need it?
The IMM Viewer uses VrApi to:
- Initialize VR mode on Quest
- Get head tracking data
- Render stereo images to the headset
- Handle VR-specific input and events

### Download Instructions

#### Step 1: Create Meta Developer Account
1. Go to https://developer.oculus.com/
2. Click "Sign Up" or "Log In"
3. Complete the registration process
4. Accept the developer terms and conditions

#### Step 2: Download the SDK
1. Visit: https://developer.oculus.com/downloads/native-android/
2. Sign in with your Meta developer account
3. Find "Oculus Mobile SDK" in the downloads list
4. Click "Download" for the latest version (v57.0 or later recommended)
5. Save the ZIP file (e.g., `ovr_sdk_mobile_1.57.0.zip`)

#### Step 3: Extract the SDK
1. Extract the downloaded ZIP file to a temporary location
2. You should see a directory structure like:
   ```
   ovr_sdk_mobile_1.57.0/
   ├── VrApi/
   │   ├── Include/
   │   │   ├── VrApi.h
   │   │   ├── VrApi_Types.h
   │   │   ├── VrApi_Helpers.h
   │   │   └── ... (other headers)
   │   └── Libs/
   │       └── Android/
   │           ├── arm64-v8a/
   │           │   └── libvrapi.so
   │           └── armeabi-v7a/
   │               └── libvrapi.so
   ├── VrAppFramework/
   ├── 1stParty/
   └── ... (other directories)
   ```

#### Step 4: Install to Project

**Option A: Use the setup script (Recommended)**
```batch
# From the IMM project root
setup_oculus_sdk.bat
```
Follow the prompts to specify where you extracted the SDK.

**Option B: Manual installation**
```batch
# Copy VrApi directory to the project
xcopy /E /I "C:\path\to\ovr_sdk_mobile_1.57.0\VrApi" "thirdparty\ovr-mobile-sdk\VrApi"
```

#### Step 5: Verify Installation
Check that these files exist:
- `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`
- `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi_Types.h`
- `thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`

---

## Audio360 SDK

### What is it?
Audio360 (also called Facebook 360 Spatial Workstation) is Meta's spatial audio SDK for creating immersive 3D audio experiences.

### Why do we need it?
The IMM format supports spatial audio, and the Audio360 SDK provides:
- Ambisonic audio decoding
- 3D audio spatialization
- Head-tracked audio rendering

### Download Instructions

#### Option 1: From Meta (Recommended)
1. Visit: https://facebook360.fb.com/spatial-workstation/
2. Download the "Audio360 SDK"
3. Extract the SDK
4. Look for Android libraries in: `Audio360/Android/arm64-v8a/`

#### Option 2: Check Existing Installation
The Audio360 SDK may already be in your `thirdparty/audio360-sdk/` directory from the Windows build.

**Check for Android libraries:**
```
thirdparty/audio360-sdk/Audio360/Android/arm64-v8a/libAudio360.so
```

If the Android libraries are missing, you'll need to either:
- Download the Android-specific Audio360 SDK
- Or build the Audio360 SDK for Android from source

### Installation
Copy the Audio360 Android libraries to:
```
thirdparty/audio360-sdk/Audio360/Android/arm64-v8a/libAudio360.so
```

---

## Third-Party Libraries

The following libraries need to be built or obtained for Android ARM64 architecture.

### Required Libraries

| Library | Purpose | Version |
|---------|---------|---------|
| libjpeg-turbo | JPEG image decoding | 2.1+ |
| libpng | PNG image support | 1.6+ |
| libogg | Ogg container format | 1.3+ |
| libvorbis | Vorbis audio codec | 1.3+ |
| opus | Opus audio codec | 1.3+ |
| libopusenc | Opus encoding | 0.2+ |
| zlib | Compression | 1.2+ |

### Option 1: Build with Android NDK (Advanced)

Each library needs to be cross-compiled for Android ARM64. Here's a template:

```bash
# Set up environment
export ANDROID_NDK_HOME=/path/to/ndk/25.2.9519653
export TOOLCHAIN=$ANDROID_NDK_HOME/toolchains/llvm/prebuilt/windows-x86_64

# Example: Building zlib
cd thirdparty/zlib
mkdir build-android
cd build-android

cmake .. \
  -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=arm64-v8a \
  -DANDROID_PLATFORM=android-26 \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_INSTALL_PREFIX=../android/arm64-v8a

cmake --build . --config Release
cmake --install .
```

Repeat this process for each library, adjusting the CMake options as needed.

### Option 2: Use Prebuilt Libraries (Recommended)

#### Using vcpkg
```bash
# Install vcpkg
git clone https://github.com/Microsoft/vcpkg.git
cd vcpkg
./bootstrap-vcpkg.bat

# Set Android triplet
set VCPKG_DEFAULT_TRIPLET=arm64-android

# Install libraries
vcpkg install zlib:arm64-android
vcpkg install libpng:arm64-android
vcpkg install libjpeg-turbo:arm64-android
vcpkg install libogg:arm64-android
vcpkg install libvorbis:arm64-android
vcpkg install opus:arm64-android
```

#### Copy to Project
After building or downloading, copy the `.so` files to:
```
thirdparty/
├── zlib/android/arm64-v8a/libz.so
├── libpng/android/arm64-v8a/libpng16.so
├── libjpeg-turbo/android/arm64-v8a/libjpeg.so
├── libogg/android/arm64-v8a/libogg.so
├── libvorbis/android/arm64-v8a/libvorbis.so
├── libvorbis/android/arm64-v8a/libvorbisfile.so
├── opus/android/arm64-v8a/libopus.so
└── libopusenc/android/arm64-v8a/libopusenc.so
```

---

## Verification

### Check Oculus Mobile SDK
```batch
dir thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h
dir thirdparty\ovr-mobile-sdk\VrApi\Libs\Android\arm64-v8a\libvrapi.so
```

### Check Audio360 SDK
```batch
dir thirdparty\audio360-sdk\Audio360\Android\arm64-v8a\libAudio360.so
```

### Check Third-Party Libraries
```batch
dir thirdparty\zlib\android\arm64-v8a\libz.so
dir thirdparty\libpng\android\arm64-v8a\libpng16.so
dir thirdparty\libjpeg-turbo\android\arm64-v8a\libjpeg.so
dir thirdparty\libogg\android\arm64-v8a\libogg.so
dir thirdparty\libvorbis\android\arm64-v8a\libvorbis.so
dir thirdparty\opus\android\arm64-v8a\libopus.so
```

### Run Verification Script
```batch
# TODO: Create verify_dependencies.bat script
verify_dependencies.bat
```

---

## Troubleshooting

### "Cannot download Oculus Mobile SDK"
- Ensure you have a Meta developer account
- Accept all terms and conditions
- Try a different browser if download fails

### "VrApi library not found"
- Make sure you copied the entire VrApi directory
- Check that you're using the ARM64 version (not ARM32)
- Verify the path: `thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/`

### "Audio360 SDK doesn't have Android libraries"
- The Windows version of Audio360 SDK may not include Android libraries
- Download the Android-specific version from Meta
- Or build from source using the Android NDK

### "Third-party libraries not building"
- Ensure Android NDK is installed and in PATH
- Check that CMake version is 3.22.1 or later
- Verify ANDROID_NDK_HOME environment variable is set

---

## Next Steps

After downloading and setting up all SDKs:

1. ✅ Verify all dependencies are in place
2. 📖 Read `ANDROID_BUILD_GUIDE.md` for build instructions
3. 🔨 Run `build_android.bat debug` to build the APK
4. 📱 Run `deploy_to_quest.bat debug` to install on Quest

---

## Additional Resources

- **Meta Quest Developer Center:** https://developer.oculus.com/
- **VrApi Documentation:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/
- **Audio360 Documentation:** https://facebook360.fb.com/spatial-workstation/
- **Android NDK Guide:** https://developer.android.com/ndk/guides
- **vcpkg Documentation:** https://vcpkg.io/


