# IMM Viewer Android - Quick Start Guide

Get your IMM Viewer running on Meta Quest in 5 steps!

---

## Prerequisites Checklist

Before you begin, make sure you have:

- [ ] **Windows PC** (or macOS/Linux with adjustments)
- [ ] **Meta Quest device** (Quest 2, Quest 3, or Quest Pro)
- [ ] **USB-C cable** to connect Quest to PC
- [ ] **Meta developer account** (free, create at https://developer.oculus.com/)
- [ ] **16+ GB RAM** on your PC
- [ ] **20+ GB free disk space**

---

## Step 1: Install Development Tools (30 minutes)

### 1.1 Install Android Studio
1. Download from: https://developer.android.com/studio
2. Run installer and follow setup wizard
3. Install Android SDK and Android Virtual Device when prompted

### 1.2 Install SDK Components
1. Open Android Studio
2. Go to **Tools > SDK Manager**
3. Under **SDK Platforms**, install:
   - ✅ Android 13.0 (API Level 33)
   - ✅ Android 8.0 (API Level 26)
4. Under **SDK Tools**, install:
   - ✅ Android SDK Build-Tools 33.0.2
   - ✅ NDK (Side by side) 25.2.9519653
   - ✅ CMake 3.22.1
   - ✅ Android SDK Platform-Tools

### 1.3 Set Environment Variables
Open PowerShell as Administrator and run:
```powershell
[System.Environment]::SetEnvironmentVariable('ANDROID_HOME', "$env:LOCALAPPDATA\Android\Sdk", 'User')
[System.Environment]::SetEnvironmentVariable('ANDROID_NDK_HOME', "$env:LOCALAPPDATA\Android\Sdk\ndk\25.2.9519653", 'User')
```

Restart your terminal/IDE after setting environment variables.

---

## Step 2: Download Oculus Mobile SDK (10 minutes)

### 2.1 Download
1. Go to: https://developer.oculus.com/downloads/native-android/
2. Sign in with your Meta developer account
3. Download "Oculus Mobile SDK" (latest version)
4. Save the ZIP file

### 2.2 Install
Run the setup helper script:
```batch
setup_oculus_sdk.bat
```

Or manually:
1. Extract the downloaded ZIP
2. Copy `VrApi` folder to: `thirdparty\ovr-mobile-sdk\VrApi\`

### 2.3 Verify
Check that this file exists:
```
thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h
```

---

## Step 3: Prepare Third-Party Libraries (Variable Time)

You have two options:

### Option A: Skip for Now (Quick Test)
The build will work without these libraries, but some features won't work:
- ❌ Spatial audio (Audio360)
- ❌ JPEG/PNG image loading
- ❌ Audio codec support

You can add these later and rebuild.

### Option B: Build/Download Libraries (1-2 hours)
See `SDK_DOWNLOAD_GUIDE.md` for detailed instructions on:
- Building libraries with Android NDK
- Or downloading prebuilt libraries with vcpkg

**Required libraries:**
- Audio360 SDK (for spatial audio)
- libjpeg-turbo, libpng (for images)
- libogg, libvorbis, opus (for audio)
- zlib (for compression)

---

## Step 4: Build the APK (5 minutes)

### 4.1 Build Debug APK
Open PowerShell in the project root and run:
```batch
build_android.bat debug
```

Wait for the build to complete. You should see:
```
BUILD SUCCESSFUL!
```

The APK will be at:
```
code\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk
```

### 4.2 Troubleshooting Build Errors

**Error: "VrApi.h not found"**
- Solution: Oculus Mobile SDK not installed. Go back to Step 2.

**Error: "Gradle sync failed"**
- Solution: Check that Android Studio and SDK are installed correctly.

**Error: "NDK not found"**
- Solution: Install NDK 25.2.9519653 via Android Studio SDK Manager.

---

## Step 5: Deploy to Quest (5 minutes)

### 5.1 Prepare Your Quest
1. Put on your Quest headset
2. Go to **Settings > System > Developer**
3. Enable **Developer Mode** (requires Meta developer account)
4. Connect Quest to PC via USB-C cable
5. Allow USB debugging when prompted in headset

### 5.2 Deploy
Run the deployment script:
```batch
deploy_to_quest.bat debug
```

Follow the prompts to:
- Install the APK
- Push sample IMM files
- Launch the app

### 5.3 Manual Deployment (Alternative)
```batch
# Check device is connected
adb devices

# Install APK
adb install -r code\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk

# Push sample IMM file
adb push exampleImmFiles\sample1.imm /sdcard/Android/data/org.linuxfoundation.imm.player/files/

# Launch app
adb shell am start -n org.linuxfoundation.imm.player/.MainActivity
```

---

## Step 6: Test the App

### 6.1 Launch
1. Put on your Quest headset
2. Go to **Library > Unknown Sources**
3. Find "Imm Viewer"
4. Click to launch

### 6.2 What to Expect
- The app should launch in VR mode
- You should see the IMM file loaded
- Head tracking should work
- Controllers should be responsive

### 6.3 View Logs
If something goes wrong, check the logs:
```batch
adb logcat -s ImmViewer:V MainActivity:V VrApi:V
```

---

## Common Issues and Solutions

### "No device found"
- Ensure Quest is connected via USB
- Enable Developer Mode in Quest settings
- Allow USB debugging in headset
- Try a different USB cable or port

### "App crashes immediately"
- Check logcat for errors: `adb logcat -s ImmViewer:V`
- Verify VrApi library is included in APK
- Ensure all native libraries are ARM64

### "Black screen in VR"
- VrApi initialization may have failed
- Check logcat for VrApi errors
- Verify Oculus Mobile SDK is correctly installed

### "IMM file not loading"
- Check file exists: `adb shell ls /sdcard/Android/data/org.linuxfoundation.imm.player/files/`
- Verify file permissions
- Check logcat for file loading errors

---

## Next Steps

### Add More Features
- Implement file browser UI
- Add playback controls
- Support more IMM formats

### Optimize Performance
- Profile CPU/GPU usage
- Optimize rendering pipeline
- Reduce APK size

### Distribute Your App
- Submit to Meta Quest Store
- Or use SideQuest for testing
- Or use App Lab for early access

---

## Documentation Reference

- **Complete Build Guide:** `ANDROID_BUILD_GUIDE.md`
- **SDK Download Guide:** `SDK_DOWNLOAD_GUIDE.md`
- **Build Script:** `build_android.bat`
- **Deploy Script:** `deploy_to_quest.bat`
- **Setup Script:** `setup_oculus_sdk.bat`

---

## Support Resources

- **Meta Quest Developer Center:** https://developer.oculus.com/
- **VrApi Documentation:** https://developer.oculus.com/documentation/native/android/mobile-vrapi/
- **Android NDK Guide:** https://developer.android.com/ndk/guides
- **Meta Developer Forums:** https://forums.oculusvr.com/

---

**Estimated Total Time:** 1-3 hours (depending on library setup)

**Congratulations!** 🎉 You now have IMM Viewer running on your Meta Quest!


