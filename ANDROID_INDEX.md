# Android Build System - Complete Index

This index provides a comprehensive overview of all Android-related files, documentation, and resources for building the IMM Viewer APK for Meta Quest devices.

---

## 📖 Documentation (Start Here)

### For Beginners
1. **[ANDROID_QUICK_START.md](ANDROID_QUICK_START.md)** ⭐ START HERE
   - 5-step quick start guide
   - Prerequisites checklist
   - Time estimates for each step
   - Common issues and solutions

### For All Users
2. **[ANDROID_BUILD_GUIDE.md](ANDROID_BUILD_GUIDE.md)** - Complete Reference (724 lines)
   - Detailed build instructions
   - SDK installation procedures
   - Third-party library setup
   - Testing and deployment
   - Troubleshooting guide
   - Performance optimization

3. **[SDK_DOWNLOAD_GUIDE.md](SDK_DOWNLOAD_GUIDE.md)** - SDK Setup
   - Oculus Mobile SDK download
   - Audio360 SDK setup
   - Third-party library building
   - vcpkg usage guide
   - Verification procedures

### Quick Reference
4. **[README_ANDROID.md](README_ANDROID.md)** - Quick Reference
   - Command cheat sheet
   - Project structure overview
   - Troubleshooting quick fixes
   - Useful links

### Technical Details
5. **[ANDROID_PROJECT_SUMMARY.md](ANDROID_PROJECT_SUMMARY.md)** - Technical Overview
   - What was created
   - File structure reference
   - Build process flowchart
   - Success criteria
   - Timeline estimates

6. **[code/projects/android/README.md](code/projects/android/README.md)** - Android Project
   - Android Studio project info
   - Build instructions
   - Project structure
   - Troubleshooting

---

## 🛠️ Build Scripts

### Setup and Verification
- **`verify_android_setup.bat`** - Check if all dependencies are installed
- **`setup_oculus_sdk.bat`** - Interactive Oculus Mobile SDK installer

### Building
- **`build_android.bat`** - Build Android APK
  - Usage: `build_android.bat [debug|release|clean]`
  - Checks dependencies
  - Builds APK with Gradle
  - Reports build status

### Deployment
- **`deploy_to_quest.bat`** - Deploy APK to Quest device
  - Usage: `deploy_to_quest.bat [debug|release]`
  - Installs APK via ADB
  - Pushes sample IMM files
  - Launches app
  - Views logs

---

## 📁 Project Files

### Root Configuration (`code/projects/android/`)
- `build.gradle` - Root build configuration
- `settings.gradle` - Project settings
- `gradle.properties` - Gradle properties
- `gradle/wrapper/gradle-wrapper.properties` - Gradle wrapper
- `README.md` - Android project documentation

### App Configuration (`code/appImmViewer/`)
- `build.gradle` - App-level build config
- `CMakeLists.txt` - Native C++ build config
- `proguard-rules.pro` - ProGuard optimization rules

### Library Configuration
- `code/libImmCore/CMakeLists.txt` - Core library build
- `code/libImmImporter/CMakeLists.txt` - Importer library build
- `code/libImmPlayer/CMakeLists.txt` - Player library build

### Source Code
- `code/appImmViewer/src/android/AndroidManifest.xml` - App manifest
- `code/appImmViewer/src/android/java/` - Kotlin/Java source
- `code/appImmViewer/src/android/cpp/` - Native C++ code
- `code/appImmViewer/src/android/res/` - Android resources
- `code/appImmViewer/src/android/assets/` - App assets

---

## 🎯 Quick Commands

### Setup
```batch
verify_android_setup.bat     # Check setup status
setup_oculus_sdk.bat         # Install Oculus Mobile SDK
```

### Build
```batch
build_android.bat debug      # Build debug APK
build_android.bat release    # Build release APK
build_android.bat clean      # Clean build
```

### Deploy
```batch
deploy_to_quest.bat debug    # Deploy debug APK
deploy_to_quest.bat release  # Deploy release APK
```

### Manual Commands
```batch
# Check connected devices
adb devices

# Install APK manually
adb install -r code\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk

# Push IMM file
adb push exampleImmFiles\sample1.imm /sdcard/Android/data/org.linuxfoundation.imm.player/files/

# Launch app
adb shell am start -n org.linuxfoundation.imm.player/.MainActivity

# View logs
adb logcat -s ImmViewer:V MainActivity:V VrApi:V
```

---

## 📋 Prerequisites Checklist

- [ ] Android Studio installed
- [ ] Android SDK Platform 33 installed
- [ ] Android NDK 25.2.9519653 installed
- [ ] CMake 3.22.1 installed
- [ ] Oculus Mobile SDK downloaded and installed
- [ ] Meta Quest device in Developer Mode
- [ ] ADB drivers installed (Windows)
- [ ] USB-C cable for device connection

---

## 🔗 External Resources

### Meta/Oculus
- **Meta Quest Developer Center:** https://developer.oculus.com/
- **Oculus Mobile SDK Download:** https://developer.oculus.com/downloads/native-android/
- **VrApi Documentation:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/
- **Meta Developer Forums:** https://forums.oculusvr.com/

### Android Development
- **Android Studio:** https://developer.android.com/studio
- **Android NDK Guide:** https://developer.android.com/ndk/guides
- **CMake Android Guide:** https://developer.android.com/ndk/guides/cmake

### Tools
- **SideQuest:** https://sidequestvr.com/ (Alternative app store)
- **Meta Quest Developer Hub:** https://developer.oculus.com/downloads/package/oculus-developer-hub-win/

---

## 🎓 Learning Path

### Step 1: Understand the Project
Read: `README.md` and `PROJECT_CONTEXT.md`

### Step 2: Quick Start
Read: `ANDROID_QUICK_START.md`

### Step 3: Verify Setup
Run: `verify_android_setup.bat`

### Step 4: Install SDKs
Follow: `SDK_DOWNLOAD_GUIDE.md`  
Run: `setup_oculus_sdk.bat`

### Step 5: Build APK
Run: `build_android.bat debug`

### Step 6: Deploy to Quest
Run: `deploy_to_quest.bat debug`

### Step 7: Advanced Topics
Read: `ANDROID_BUILD_GUIDE.md` for optimization, troubleshooting, and advanced features

---

## 📊 File Summary

| Category | Count | Files |
|----------|-------|-------|
| Documentation | 6 | Quick Start, Build Guide, SDK Guide, Summary, README, Project README |
| Build Scripts | 4 | build_android, deploy_to_quest, setup_oculus_sdk, verify_android_setup |
| Project Config | 5 | build.gradle (2), settings.gradle, gradle.properties, wrapper |
| Native Config | 4 | CMakeLists.txt (4 files) |
| App Config | 2 | build.gradle, proguard-rules.pro |
| **Total** | **21** | **All Android-related files** |

---

## 🚀 Getting Started Now

1. Open `ANDROID_QUICK_START.md`
2. Follow the 5 steps
3. Build your first APK
4. Deploy to Quest
5. Enjoy VR! 🎉

---

**Questions?** Check the documentation or visit the Meta Quest developer forums.

**Need help?** All documentation includes troubleshooting sections.

**Ready to build?** Start with `ANDROID_QUICK_START.md`!


