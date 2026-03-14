# IMM Viewer Android Project

This directory contains the Android Studio project for building the IMM Viewer APK for Meta Quest devices.

## Quick Start

### Prerequisites
1. Android Studio Hedgehog (2023.1.1) or later
2. Android SDK Platform 33
3. Android NDK 25.2.9519653
4. CMake 3.22.1
5. Oculus Mobile SDK (VrApi)

### Building

**Option 1: Android Studio**
1. Open this directory in Android Studio
2. Wait for Gradle sync to complete
3. Build > Build Bundle(s) / APK(s) > Build APK(s)

**Option 2: Command Line**
```bash
# From this directory
./gradlew assembleDebug    # Build debug APK
./gradlew assembleRelease  # Build release APK
./gradlew clean            # Clean build
```

**Option 3: Build Script (from project root)**
```bash
# From IMM root directory
build_android.bat debug    # Build debug APK
build_android.bat release  # Build release APK
build_android.bat clean    # Clean build
```

## Project Structure

```
android/
├── build.gradle           # Root build configuration
├── settings.gradle        # Project settings
├── gradle.properties      # Gradle properties
└── gradle/
    └── wrapper/           # Gradle wrapper files
```

The actual application code is in `../../appImmViewer/`:
```
appImmViewer/
├── build.gradle           # App-level build config
├── CMakeLists.txt         # Native C++ build config
├── proguard-rules.pro     # ProGuard rules
└── src/
    └── android/
        ├── AndroidManifest.xml
        ├── java/          # Kotlin source code
        ├── cpp/           # Native C++ code
        ├── res/           # Android resources
        └── assets/        # App assets
```

## Important Notes

### Oculus Mobile SDK Required
The project requires the Oculus Mobile SDK (VrApi) which is NOT included in this repository.

**Download from:** https://developer.oculus.com/downloads/native-android/

**Installation:**
1. Download and extract the Oculus Mobile SDK
2. Copy the `VrApi` directory to: `../../../thirdparty/ovr-mobile-sdk/VrApi/`
3. Verify these files exist:
   - `../../../thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`
   - `../../../thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`

### Third-Party Libraries
The project also requires ARM64 versions of:
- Audio360 SDK
- libjpeg-turbo
- libpng
- libogg
- libvorbis
- opus
- libopusenc
- zlib

See `../../../ANDROID_BUILD_GUIDE.md` for detailed instructions on building or obtaining these libraries.

## Build Output

After a successful build, the APK will be located at:
- **Debug:** `../../appImmViewer/build/outputs/apk/debug/appImmViewer-debug.apk`
- **Release:** `../../appImmViewer/build/outputs/apk/release/appImmViewer-release.apk`

## Deployment

### Install on Quest
```bash
# Connect Quest via USB and enable Developer Mode
adb install -r ../../appImmViewer/build/outputs/apk/debug/appImmViewer-debug.apk
```

### Or use the deployment script (from project root)
```bash
deploy_to_quest.bat debug    # Deploy debug APK
deploy_to_quest.bat release  # Deploy release APK
```

## Troubleshooting

### Gradle Sync Failed
- Ensure Android SDK and NDK are installed
- Check that `ANDROID_HOME` environment variable is set
- Verify NDK version 25.2.9519653 is installed

### CMake Error: VrApi.h not found
- Oculus Mobile SDK not installed correctly
- Check path: `../../../thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`

### Build Error: Undefined reference to VrApi functions
- VrApi library not found
- Check path: `../../../thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`

### App crashes on Quest
- Check logcat: `adb logcat -s ImmViewer:V MainActivity:V VrApi:V`
- Verify all native libraries are ARM64 (not x86 or ARM32)
- Ensure Developer Mode is enabled on Quest

## Documentation

For complete build instructions, see:
- **Main Guide:** `../../../ANDROID_BUILD_GUIDE.md`
- **Deployment:** `../../../deploy_to_quest.bat`
- **Build Script:** `../../../build_android.bat`

## Support

For issues or questions:
1. Check the troubleshooting section in `ANDROID_BUILD_GUIDE.md`
2. Review Meta Quest developer documentation: https://developer.oculus.com/
3. Check VrApi documentation: https://developer.oculus.com/documentation/native/android/mobile-vrapi/

---

**Target Platform:** Meta Quest 2, Quest 3, Quest Pro  
**Minimum Android Version:** 8.0 (API 26)  
**Target Android Version:** 13 (API 33)  
**Architecture:** ARM64-v8a only

