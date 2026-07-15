# IMM Viewer Android Build Guide
## Building APK for Meta Quest Devices

**Last Updated:** January 4, 2026  
**Target Platform:** Meta Quest 2, Quest 3, Quest Pro  
**Build System:** Android Studio with Gradle + CMake

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Required SDK Downloads](#required-sdk-downloads)
3. [Project Structure](#project-structure)
4. [Build Configuration](#build-configuration)
5. [Building the APK](#building-the-apk)
6. [Testing and Deployment](#testing-and-deployment)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Development Environment

- **Operating System:** Windows 10/11, macOS, or Linux
- **RAM:** 16+ GB recommended
- **Storage:** 20+ GB free space
- **Android Studio:** Hedgehog (2023.1.1) or later
- **JDK:** Java 17 (bundled with Android Studio)

### Required Software

1. **Android Studio** - Download from [developer.android.com](https://developer.android.com/studio)
2. **Android SDK Platform 33** (Android 13)
3. **Android SDK Build Tools 33.0.2** or later
4. **Android NDK 25.2.9519653** (Side by side)
5. **CMake 3.22.1** or later
6. **Git** (for version control)

---

## Required SDK Downloads

### 1. Oculus Mobile SDK (VrApi)

**CRITICAL:** The project requires the Oculus Mobile SDK which contains VrApi headers and libraries.

**Download Location:**
- Visit: https://developer.oculus.com/downloads/native-android/
- Package: "Oculus Mobile SDK" (latest version compatible with Quest)
- Recommended Version: v57.0 or later

**What You Need:**
```
OculusMobileSDK/
├── VrApi/
│   ├── Include/           # VrApi headers (VrApi.h, VrApi_*.h)
│   └── Libs/
│       └── Android/
│           └── arm64-v8a/ # libvrapi.so
├── VrAppFramework/        # Optional framework code
└── 1stParty/              # Sample code and utilities
```

**Installation Steps:**
1. Download the Oculus Mobile SDK ZIP file
2. Extract to a temporary location
3. Copy the VrApi directory to: `thirdparty/ovr-mobile-sdk/VrApi/`
4. Verify the following files exist:
   - `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`
   - `thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`

### 2. Third-Party Android Libraries

The following libraries need ARM64 (.so) versions. You have two options:

#### Option A: Build from Source (Recommended)
Build each library for Android ARM64 using the Android NDK.

#### Option B: Use Prebuilt Libraries
Download prebuilt ARM64 libraries from vcpkg or other sources.

**Required Libraries:**
- **libjpeg-turbo** (ARM64) - Image decoding
- **libpng** (ARM64) - PNG image support
- **libogg** (ARM64) - Ogg container format
- **libvorbis** (ARM64) - Vorbis audio codec
- **opus** (ARM64) - Opus audio codec
- **libopusenc** (ARM64) - Opus encoding
- **zlib** (ARM64) - Compression
- **Audio360 SDK** (ARM64) - Spatial audio (Facebook/Meta)

**Audio360 SDK for Android:**
- The Audio360 SDK includes Android ARM64 libraries
- Location in SDK: `Audio360/Android/arm64-v8a/`
- Copy to: `thirdparty/audio360-sdk/Audio360/Android/arm64-v8a/`

---

## Project Structure

The Android project is organized as follows:

```
IMM/
├── code/
│   ├── projects/
│   │   └── android/              # Android Studio project root
│   │       ├── build.gradle      # Root build configuration
│   │       ├── settings.gradle   # Project settings
│   │       ├── gradle.properties # Gradle properties
│   │       └── gradle/
│   │           └── wrapper/      # Gradle wrapper
│   ├── appImmViewer/             # Main application module
│   │   ├── build.gradle          # App-level build config
│   │   ├── CMakeLists.txt        # Native build configuration
│   │   ├── proguard-rules.pro    # ProGuard rules
│   │   └── src/
│   │       └── android/
│   │           ├── AndroidManifest.xml
│   │           ├── java/         # Kotlin/Java source
│   │           ├── cpp/          # Native C++ code
│   │           ├── res/          # Android resources
│   │           └── assets/       # App assets (IMM files)
│   ├── libImmCore/
│   │   └── CMakeLists.txt        # Core library build
│   ├── libImmImporter/
│   │   └── CMakeLists.txt        # Importer library build
│   └── libImmPlayer/
│       └── CMakeLists.txt        # Player library build
└── thirdparty/
    ├── ovr-mobile-sdk/           # ⚠️ NEEDS TO BE ADDED
    │   └── VrApi/
    ├── audio360-sdk/
    ├── libjpeg-turbo/
    ├── libpng/
    └── ... (other dependencies)
```

---

## Build Configuration

### Step 1: Install Android Studio and SDK Components

1. **Install Android Studio:**
   - Download from https://developer.android.com/studio
   - Run the installer and follow the setup wizard
   - Install the Android SDK and Android Virtual Device

2. **Install Required SDK Components:**
   - Open Android Studio
   - Go to **Tools > SDK Manager**
   - Under **SDK Platforms** tab, install:
     - ✅ Android 13.0 (API Level 33)
     - ✅ Android 8.0 (API Level 26) - Minimum for Quest

   - Under **SDK Tools** tab, install:
     - ✅ Android SDK Build-Tools 33.0.2
     - ✅ NDK (Side by side) version 25.2.9519653
     - ✅ CMake version 3.22.1
     - ✅ Android SDK Platform-Tools
     - ✅ Android SDK Command-line Tools

3. **Set Environment Variables (Windows):**
   ```batch
   setx ANDROID_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk"
   setx ANDROID_NDK_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk\ndk\25.2.9519653"
   setx JAVA_HOME "C:\Program Files\Android\Android Studio\jbr"
   ```

4. **Set Environment Variables (macOS/Linux):**
   ```bash
   export ANDROID_HOME=$HOME/Library/Android/sdk
   export ANDROID_NDK_HOME=$ANDROID_HOME/ndk/25.2.9519653
   export JAVA_HOME=/Applications/Android\ Studio.app/Contents/jbr/Contents/Home
   ```

### Step 2: Download and Install Oculus Mobile SDK

1. **Download the SDK:**
   - Go to https://developer.oculus.com/downloads/native-android/
   - Sign in with your Meta/Oculus developer account
   - Download "Oculus Mobile SDK" (latest version)

2. **Extract and Install:**
   ```batch
   # Extract the downloaded ZIP file
   # Copy VrApi directory to the project
   xcopy /E /I "OculusMobileSDK\VrApi" "thirdparty\ovr-mobile-sdk\VrApi"
   ```

3. **Verify Installation:**
   Check that these files exist:
   - `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`
   - `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi_Types.h`
   - `thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`

### Step 3: Prepare Third-Party Libraries for Android

You need ARM64 versions of all third-party libraries. Here are your options:

#### Option A: Build Libraries with Android NDK (Advanced)

Each library needs to be cross-compiled for Android ARM64. Example for zlib:

```bash
cd thirdparty/zlib
mkdir build-android
cd build-android

cmake .. \
  -DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake \
  -DANDROID_ABI=arm64-v8a \
  -DANDROID_PLATFORM=android-26 \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_INSTALL_PREFIX=../android/arm64-v8a

make -j8
make install
```

Repeat for: libjpeg-turbo, libpng, libogg, libvorbis, opus, libopusenc

#### Option B: Use Prebuilt Libraries (Recommended for Quick Start)

Download prebuilt Android ARM64 libraries from vcpkg or other sources.

#### Audio360 SDK for Android

The Audio360 SDK should include Android libraries:
- Check: `thirdparty/audio360-sdk/Audio360/Android/arm64-v8a/libAudio360.so`
- If missing, download the Android-specific Audio360 SDK

---

## Building the APK

### Method 1: Using Android Studio (Recommended)

1. **Open the Project:**
   - Launch Android Studio
   - Select **File > Open**
   - Navigate to `code/projects/android/`
   - Click **OK**

2. **Sync Gradle:**
   - Android Studio will automatically sync Gradle
   - Wait for "Gradle sync finished" message
   - If errors occur, check the Build Output window

3. **Build the APK:**
   - Select **Build > Build Bundle(s) / APK(s) > Build APK(s)**
   - Wait for build to complete
   - APK location: `code/appImmViewer/build/outputs/apk/debug/appImmViewer-debug.apk`

4. **Build Release APK:**
   - Select **Build > Generate Signed Bundle / APK**
   - Choose **APK**
   - Create or select a keystore
   - Select **release** build variant
   - Click **Finish**

### Method 2: Using Command Line (Gradle)

1. **Navigate to project directory:**
   ```batch
   cd code\projects\android
   ```

2. **Build Debug APK:**
   ```batch
   gradlew assembleDebug
   ```

3. **Build Release APK:**
   ```batch
   gradlew assembleRelease
   ```

4. **Clean build:**
   ```batch
   gradlew clean
   ```

### Method 3: Using Build Script (Automated)

Create `build_android.bat` in the project root:

```batch
@echo off
echo Building IMM Viewer Android APK...
echo.

cd code\projects\android

echo Cleaning previous build...
call gradlew clean

echo Building Debug APK...
call gradlew assembleDebug

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    exit /b %ERRORLEVEL%
)

echo.
echo BUILD SUCCESSFUL!
echo.
echo APK Location:
echo %CD%\..\..\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk
echo.

pause
```

---

## Testing and Deployment

### Prepare Your Meta Quest Device

1. **Enable Developer Mode:**
   - Install the Meta Quest mobile app on your phone
   - Sign in with your Meta account
   - Go to **Menu > Devices**
   - Select your Quest headset
   - Go to **Developer Mode** and toggle it ON
   - Restart your Quest headset

2. **Install ADB Drivers (Windows only):**
   - Download Oculus ADB Drivers from:
     https://developer.oculus.com/downloads/package/oculus-adb-drivers/
   - Extract and run the installer
   - Connect Quest via USB-C cable

3. **Verify ADB Connection:**
   ```batch
   adb devices
   ```
   You should see your Quest device listed.

### Install APK on Quest

#### Method 1: Using Android Studio

1. Connect Quest to PC via USB-C
2. Put on the Quest headset
3. Allow USB debugging when prompted
4. In Android Studio, select your device from the device dropdown
5. Click the **Run** button (green play icon)
6. The app will install and launch automatically

#### Method 2: Using ADB Command Line

1. **Install Debug APK:**
   ```batch
   adb install -r code\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk
   ```

2. **Install Release APK:**
   ```batch
   adb install -r code\appImmViewer\build\outputs\apk\release\appImmViewer-release.apk
   ```

3. **Uninstall (if needed):**
   ```batch
   adb uninstall org.linuxfoundation.imm.player
   ```

### Push IMM Files to Device

1. **Create app directory:**
   ```batch
   adb shell mkdir -p /sdcard/Android/data/org.linuxfoundation.imm.player/files
   ```

2. **Push sample IMM file:**
   ```batch
   adb push exampleImmFiles\sample1.imm /sdcard/Android/data/org.linuxfoundation.imm.player/files/
   ```

3. **Verify file:**
   ```batch
   adb shell ls -lh /sdcard/Android/data/org.linuxfoundation.imm.player/files/
   ```

### Launch and Test the App

1. **Launch from Quest:**
   - Put on the Quest headset
   - Go to **Library > Unknown Sources**
   - Find "Imm Viewer"
   - Click to launch

2. **Launch via ADB:**
   ```batch
   adb shell am start -n org.linuxfoundation.imm.player/.MainActivity
   ```

3. **View Logs:**
   ```batch
   adb logcat -s ImmViewer:V MainActivity:V VrApi:V
   ```

### Debugging

1. **Enable Native Debugging in Android Studio:**
   - Go to **Run > Edit Configurations**
   - Select your app configuration
   - Under **Debugger** tab, select **Native**
   - Set breakpoints in C++ code
   - Click **Debug** button

2. **View Logcat:**
   - In Android Studio, open **Logcat** window
   - Filter by package: `org.linuxfoundation.imm.player`
   - Look for errors or warnings

3. **Common Issues:**
   - **App crashes on launch:** Check logcat for native library loading errors
   - **Black screen:** Verify VrApi initialization and OpenGL ES context
   - **No IMM file loaded:** Check file permissions and paths
   - **Audio not working:** Verify Audio360 library is loaded

---

## Troubleshooting

### Build Errors

#### Error: "VrApi.h: No such file or directory"

**Solution:** Oculus Mobile SDK not installed correctly.
```batch
# Verify VrApi headers exist
dir thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h
```

#### Error: "libvrapi.so: cannot find"

**Solution:** VrApi library not in correct location.
```batch
# Check library exists
dir thirdparty\ovr-mobile-sdk\VrApi\Libs\Android\arm64-v8a\libvrapi.so
```

#### Error: "Undefined reference to Audio360 functions"

**Solution:** Audio360 library not linked.
- Verify `libAudio360.so` exists in `thirdparty/audio360-sdk/Audio360/Android/arm64-v8a/`
- Check CMakeLists.txt has correct library path

#### Error: "CMake Error: Could not find CMAKE_ANDROID_NDK"

**Solution:** NDK not installed or environment variable not set.
```batch
# Set NDK path
setx ANDROID_NDK_HOME "C:\Users\%USERNAME%\AppData\Local\Android\Sdk\ndk\25.2.9519653"
```

### Runtime Errors

#### App crashes immediately on launch

**Check:**
1. Logcat for native library loading errors
2. All .so files are ARM64 (not x86 or ARM32)
3. AndroidManifest.xml permissions are granted

#### Black screen in VR

**Check:**
1. VrApi initialization succeeded (check logs)
2. OpenGL ES 3.2 context created successfully
3. Eye buffers allocated correctly

#### IMM file not loading

**Check:**
1. File exists on device: `adb shell ls /sdcard/Android/data/org.linuxfoundation.imm.player/files/`
2. File permissions: `adb shell ls -l /sdcard/Android/data/org.linuxfoundation.imm.player/files/`
3. App has READ_EXTERNAL_STORAGE permission

---

## Advanced Configuration

### Optimizing APK Size

1. **Enable ProGuard/R8:**
   In `build.gradle`:
   ```gradle
   buildTypes {
       release {
           minifyEnabled true
           shrinkResources true
       }
   }
   ```

2. **Use APK Splits:**
   ```gradle
   splits {
       abi {
           enable true
           reset()
           include 'arm64-v8a'
           universalApk false
       }
   }
   ```

### Performance Optimization

1. **Compiler Optimizations:**
   In CMakeLists.txt:
   ```cmake
   if(CMAKE_BUILD_TYPE STREQUAL "Release")
       add_compile_options(-O3 -ffast-math -DNDEBUG)
   endif()
   ```

2. **Link-Time Optimization:**
   ```cmake
   set(CMAKE_INTERPROCEDURAL_OPTIMIZATION TRUE)
   ```

### Signing for Release

1. **Create Keystore:**
   ```batch
   keytool -genkey -v -keystore imm-release-key.keystore -alias imm-key -keyalg RSA -keysize 2048 -validity 10000
   ```

2. **Configure Signing in build.gradle:**
   ```gradle
   android {
       signingConfigs {
           release {
               storeFile file("imm-release-key.keystore")
               storePassword "your_password"
               keyAlias "imm-key"
               keyPassword "your_password"
           }
       }
       buildTypes {
           release {
               signingConfig signingConfigs.release
           }
       }
   }
   ```

---

## Quick Start Checklist

Use this checklist to ensure you have everything set up:

- [ ] Android Studio installed with SDK Platform 33
- [ ] Android NDK 25.2.9519653 installed
- [ ] CMake 3.22.1 installed
- [ ] Oculus Mobile SDK downloaded and extracted to `thirdparty/ovr-mobile-sdk/`
- [ ] VrApi headers exist: `thirdparty/ovr-mobile-sdk/VrApi/Include/VrApi.h`
- [ ] VrApi library exists: `thirdparty/ovr-mobile-sdk/VrApi/Libs/Android/arm64-v8a/libvrapi.so`
- [ ] Third-party ARM64 libraries built or downloaded
- [ ] Audio360 Android library available
- [ ] Meta Quest device in Developer Mode
- [ ] ADB drivers installed (Windows)
- [ ] Quest connected and authorized via USB
- [ ] Project opened in Android Studio
- [ ] Gradle sync completed successfully
- [ ] CMakeLists.txt updated with library paths
- [ ] Build successful (no errors)
- [ ] APK installed on Quest device
- [ ] Sample IMM file pushed to device
- [ ] App launches and displays VR content

---

## Next Steps

After successfully building and deploying the APK:

1. **Test with Multiple IMM Files:**
   - Test with different IMM formats (mono, stereo, 360°, 180°)
   - Verify audio playback and spatial audio
   - Test head tracking and controller input

2. **Performance Testing:**
   - Monitor frame rate (should maintain 72 FPS on Quest 2, 90 FPS on Quest 3)
   - Check for thermal throttling during extended use
   - Profile CPU and GPU usage

3. **User Interface:**
   - Implement file browser for selecting IMM files
   - Add playback controls (play, pause, seek)
   - Add settings menu for quality options

4. **Distribution:**
   - Submit to Meta Quest Store (requires Meta developer account)
   - Or distribute via SideQuest for testing
   - Or use App Lab for early access

---

## Additional Resources

### Documentation
- **Meta Quest Developer Center:** https://developer.oculus.com/
- **VrApi Documentation:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/
- **Android NDK Guide:** https://developer.android.com/ndk/guides
- **CMake Android Guide:** https://developer.android.com/ndk/guides/cmake

### Tools
- **SideQuest:** https://sidequestvr.com/ (Alternative app store and ADB tool)
- **Meta Quest Developer Hub:** https://developer.oculus.com/downloads/package/oculus-developer-hub-win/
- **Android Studio:** https://developer.android.com/studio

### Community
- **Meta Quest Developer Forums:** https://forums.oculusvr.com/
- **Reddit r/OculusQuestDev:** https://www.reddit.com/r/OculusQuestDev/
- **Discord - Quest Development:** Various community servers

---

## Appendix: File Structure Reference

### Complete Android Project Structure

```
IMM/
├── ANDROID_BUILD_GUIDE.md          # This guide
├── build_android.bat                # Build script
├── code/
│   ├── projects/
│   │   └── android/
│   │       ├── build.gradle         # Root build config
│   │       ├── settings.gradle      # Project settings
│   │       ├── gradle.properties    # Gradle properties
│   │       ├── gradlew              # Gradle wrapper (Unix)
│   │       ├── gradlew.bat          # Gradle wrapper (Windows)
│   │       └── gradle/
│   │           └── wrapper/
│   │               ├── gradle-wrapper.jar
│   │               └── gradle-wrapper.properties
│   ├── appImmViewer/
│   │   ├── build.gradle             # App build config
│   │   ├── CMakeLists.txt           # Native build config
│   │   ├── proguard-rules.pro       # ProGuard rules
│   │   ├── src/
│   │   │   └── android/
│   │   │       ├── AndroidManifest.xml
│   │   │       ├── java/
│   │   │       │   └── org/linuxfoundation/imm/player/
│   │   │       │       ├── MainActivity.kt
│   │   │       │       ├── ImmNativeInterface.kt
│   │   │       │       └── Utils.kt
│   │   │       ├── cpp/
│   │   │       │   ├── OvrApp.cpp
│   │   │       │   └── OvrApp.h
│   │   │       ├── res/
│   │   │       │   ├── values/
│   │   │       │   │   └── strings.xml
│   │   │       │   └── mipmap-*/
│   │   │       │       └── ic_launcher.png
│   │   │       └── assets/
│   │   │           └── (IMM files go here)
│   │   └── build/
│   │       └── outputs/
│   │           └── apk/
│   │               ├── debug/
│   │               │   └── appImmViewer-debug.apk
│   │               └── release/
│   │                   └── appImmViewer-release.apk
│   ├── libImmCore/
│   │   ├── CMakeLists.txt
│   │   └── src/
│   ├── libImmImporter/
│   │   ├── CMakeLists.txt
│   │   └── src/
│   └── libImmPlayer/
│       ├── CMakeLists.txt
│       └── src/
└── thirdparty/
    ├── ovr-mobile-sdk/              # ⚠️ Download required
    │   └── VrApi/
    │       ├── Include/
    │       │   ├── VrApi.h
    │       │   └── VrApi_*.h
    │       └── Libs/
    │           └── Android/
    │               └── arm64-v8a/
    │                   └── libvrapi.so
    ├── audio360-sdk/
    │   └── Audio360/
    │       ├── include/
    │       └── Android/
    │           └── arm64-v8a/
    │               └── libAudio360.so
    └── (other third-party libraries)
```

---

## Version History

- **v1.0.0** (2026-01-04) - Initial Android build system setup
  - Created Gradle build configuration
  - Created CMake build files for native libraries
  - Documented Oculus Mobile SDK integration
  - Added build and deployment instructions

---

**End of Guide**

For questions or issues, please refer to the troubleshooting section or consult the Meta Quest developer documentation.

