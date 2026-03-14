# Building IMM Viewer for Meta Quest (Android)

This README provides a quick overview of building the IMM Viewer Android APK for Meta Quest devices.

---

## 🚀 Quick Start

**New to Android development?** Start here: [`ANDROID_QUICK_START.md`](ANDROID_QUICK_START.md)

**Experienced developer?** Jump to: [`ANDROID_BUILD_GUIDE.md`](ANDROID_BUILD_GUIDE.md)

---

## 📋 Prerequisites

- **Android Studio** with SDK Platform 33 and NDK 25.2.9519653
- **Oculus Mobile SDK** (VrApi) - [Download required](https://developer.oculus.com/downloads/native-android/)
- **Meta Quest device** with Developer Mode enabled
- **Windows PC** (or macOS/Linux with adjustments)

---

## 🛠️ Setup Steps

### 1. Verify Your Setup
```batch
verify_android_setup.bat
```
This will check if all required components are installed.

### 2. Install Oculus Mobile SDK
```batch
setup_oculus_sdk.bat
```
Follow the prompts to download and install the Oculus Mobile SDK.

### 3. Build the APK
```batch
build_android.bat debug
```
This will compile the native code and create an APK.

### 4. Deploy to Quest
```batch
deploy_to_quest.bat debug
```
This will install the APK on your connected Quest device.

---

## 📁 Project Structure

```
IMM/
├── code/
│   ├── projects/android/        # Android Studio project
│   ├── appImmViewer/            # Main application
│   ├── libImmCore/              # Core library
│   ├── libImmImporter/          # IMM file importer
│   └── libImmPlayer/            # Playback engine
│
├── thirdparty/
│   ├── ovr-mobile-sdk/          # Oculus Mobile SDK (download required)
│   ├── audio360-sdk/            # Spatial audio SDK
│   └── (other libraries)        # Image/audio codecs
│
├── ANDROID_QUICK_START.md       # 🌟 Start here if new to Android
├── ANDROID_BUILD_GUIDE.md       # Complete build instructions
├── SDK_DOWNLOAD_GUIDE.md        # SDK download instructions
├── ANDROID_PROJECT_SUMMARY.md   # Technical summary
│
├── build_android.bat            # Build automation script
├── deploy_to_quest.bat          # Deployment script
├── setup_oculus_sdk.bat         # SDK setup helper
└── verify_android_setup.bat     # Setup verification
```

---

## 📚 Documentation

| Document | Purpose | Audience |
|----------|---------|----------|
| [`ANDROID_QUICK_START.md`](ANDROID_QUICK_START.md) | Fast-track guide | Beginners |
| [`ANDROID_BUILD_GUIDE.md`](ANDROID_BUILD_GUIDE.md) | Complete reference | All users |
| [`SDK_DOWNLOAD_GUIDE.md`](SDK_DOWNLOAD_GUIDE.md) | SDK setup details | All users |
| [`ANDROID_PROJECT_SUMMARY.md`](ANDROID_PROJECT_SUMMARY.md) | Technical overview | Developers |
| [`code/projects/android/README.md`](code/projects/android/README.md) | Android project info | Developers |

---

## 🔧 Build Scripts

| Script | Purpose |
|--------|---------|
| `verify_android_setup.bat` | Check if setup is complete |
| `setup_oculus_sdk.bat` | Install Oculus Mobile SDK |
| `build_android.bat` | Build the APK |
| `deploy_to_quest.bat` | Deploy to Quest device |

---

## ⚡ Quick Commands

```batch
# Verify setup
verify_android_setup.bat

# Build debug APK
build_android.bat debug

# Build release APK
build_android.bat release

# Clean build
build_android.bat clean

# Deploy debug APK to Quest
deploy_to_quest.bat debug

# Deploy release APK to Quest
deploy_to_quest.bat release
```

---

## 🎯 Build Targets

- **Debug APK:** For development and testing
  - Includes debug symbols
  - Allows native debugging
  - Larger file size
  
- **Release APK:** For distribution
  - Optimized and minified
  - Smaller file size
  - Requires signing key

---

## 🐛 Troubleshooting

### Build fails with "VrApi.h not found"
**Solution:** Install Oculus Mobile SDK
```batch
setup_oculus_sdk.bat
```

### "No device found" when deploying
**Solution:** 
1. Enable Developer Mode on Quest
2. Connect via USB-C
3. Allow USB debugging in headset

### App crashes on launch
**Solution:** Check logs
```batch
adb logcat -s ImmViewer:V MainActivity:V VrApi:V
```

See [`ANDROID_BUILD_GUIDE.md`](ANDROID_BUILD_GUIDE.md) for more troubleshooting.

---

## 📱 Supported Devices

- Meta Quest 2
- Meta Quest 3
- Meta Quest Pro

**Minimum Android Version:** 8.0 (API 26)  
**Target Android Version:** 13 (API 33)  
**Architecture:** ARM64-v8a only

---

## 🔗 Useful Links

- **Meta Quest Developer Center:** https://developer.oculus.com/
- **VrApi Documentation:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/
- **Android NDK Guide:** https://developer.android.com/ndk/guides
- **Meta Developer Forums:** https://forums.oculusvr.com/

---

## 📝 License

See [`code/libImmCore/LICENSE.TXT`](code/libImmCore/LICENSE.TXT) for license information.

---

## 🎉 Getting Started

1. **Read:** [`ANDROID_QUICK_START.md`](ANDROID_QUICK_START.md)
2. **Verify:** Run `verify_android_setup.bat`
3. **Setup:** Run `setup_oculus_sdk.bat`
4. **Build:** Run `build_android.bat debug`
5. **Deploy:** Run `deploy_to_quest.bat debug`
6. **Enjoy:** Put on your Quest and launch the app!

---

**Questions?** Check the documentation or visit the Meta Quest developer forums.

**Ready to build?** Start with [`ANDROID_QUICK_START.md`](ANDROID_QUICK_START.md)!


