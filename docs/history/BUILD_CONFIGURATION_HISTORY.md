# IMM2 Android Build - Configuration History

## Project Overview
- **Project Path:** `a:\Github\IMM2\IMM`
- **Target Platform:** Android (Meta Quest VR)
- **Build System:** Gradle + CMake + NDK
- **Main APK Module:** `code\appImmViewer`
- **Android Project Root:** `code\projects\android`

## Current Environment (Updated: 2026-01-05)

### Android Studio
- **Version:** Android Studio Otter 2 Feature Drop | 2025.2.2 Patch 1
- **Build:** #AI-252.27397.103.2522.14617522 (December 18, 2025)
- **Runtime:** Java 21.0.8 (JetBrains Runtime)
- **Status:** ✅ Latest version installed

### Build Configuration
```
Android Gradle Plugin: 8.5.2
Gradle: 8.5
Kotlin: 1.9.22
Java Compatibility: 17 (source/target)
Gradle JVM: Java 21 (from Android Studio)
NDK: 24.0.8215888
CMake: 3.22.1
```

### Environment Variables
```powershell
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"  # Java 21
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_NDK_HOME = "$env:LOCALAPPDATA\Android\Sdk\ndk\24.0.8215888"
```

## Version Compatibility Matrix

| Component | Version | Reason |
|-----------|---------|--------|
| Android Studio | Otter 2025.2.2 | Latest stable release |
| Gradle | 8.5 | Minimum required for Android Studio Otter |
| AGP | 8.5.2 | Compatible with Gradle 8.5 and Java 21 |
| Kotlin | 1.9.22 | Compatible with AGP 8.5 |
| Java (source/target) | 17 | Required by AGP 8.5 |
| Gradle JVM | 21 | Provided by Android Studio |

## Configuration Files Modified

### 1. `code/projects/android/gradle/wrapper/gradle-wrapper.properties`
```properties
distributionUrl=https\://services.gradle.org/distributions/gradle-8.5-bin.zip
```

### 2. `code/projects/android/build.gradle`
```gradle
kotlin_version = '1.9.22'
classpath 'com.android.tools.build:gradle:8.5.2'
```

### 3. `code/appImmViewer/build.gradle`
```gradle
compileOptions {
    sourceCompatibility JavaVersion.VERSION_17
    targetCompatibility JavaVersion.VERSION_17
}
kotlinOptions {
    jvmTarget = '17'
}
```

### 4. `code/projects/android/gradle.properties`
```properties
org.gradle.configuration-cache=true
org.gradle.java.home=C\:\\Program Files\\Android\\Android Studio\\jbr
```

## Previous Issues Resolved

### Issue 1: Java Version Mismatch (Initial)
- **Error:** "requires class file version 61.0, only recognizes up to 55.0"
- **Cause:** AGP 8.1.4 required Java 17, but Java 11 was configured
- **Resolution:** Downgraded to AGP 7.4.2 temporarily

### Issue 2: Android Studio Compatibility
- **Error:** "AGP 7.4.2 incompatible, latest supported is 7.2.1"
- **Cause:** Old Android Studio version
- **Resolution:** User updated to Android Studio Otter 2025.2.2

### Issue 3: Gradle/Java Incompatibility (Final)
- **Error:** "Java 21.0.8 and Gradle 7.3.3 incompatible"
- **Cause:** Gradle 7.3.3 doesn't support Java 21
- **Resolution:** Upgraded to Gradle 8.5 + AGP 8.5.2 + Java 17 compatibility

## CMake Exclusions Applied

### libImmCore (`code/libImmCore/CMakeLists.txt`)
- Excluded OpenGL 4.x files: `.*gl4.*`, `.*opengl4.*`
- Excluded DirectX 11 files: `.*d3d11.*`, `.*dx11.*`
- Excluded OGG/OPUS codecs: `.*codec.*ogg.*`, `.*codec.*opus.*`

### libImmPlayer (`code/libImmPlayer/CMakeLists.txt`)
- Excluded pretessellated renderer: `.*pretessellated.*`
- Reason: Missing shader compilation step (`.glsl` → `.h`)

### appImmViewer (`code/appImmViewer/CMakeLists.txt`)
- Added `android_native_app_glue` library linkage

## Next Steps

1. **Sync Gradle in Android Studio**
   - File → Invalidate Caches / Restart
   - File → Sync Project with Gradle Files
   
2. **Build the Project**
   - Build → Rebuild Project
   - Or: `.\gradlew.bat clean assembleDebug --stacktrace`

3. **Expected Outcome**
   - Gradle sync should succeed
   - May encounter C++ compilation errors (CMake/NDK issues)
   - Check Build tab for detailed error messages

## Known Potential Issues

1. **Shader Compilation:** Pretessellated renderer excluded due to missing headers
2. **Third-Party Dependencies:** VrApi SDK and other native libraries need verification
3. **CMake Configuration:** May need additional platform-specific exclusions
4. **NDK Version:** Using 24.0.8215888, may need to match project requirements

## Build Commands

### Clean Build
```powershell
cd a:\Github\IMM2\IMM\code\projects\android
.\gradlew.bat clean assembleDebug --stacktrace
```

### Full Clean (including CMake cache)
```powershell
Remove-Item "code\appImmViewer\.cxx" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "code\appImmViewer\build" -Recurse -Force -ErrorAction SilentlyContinue
cd code\projects\android
.\gradlew.bat clean assembleDebug --stacktrace
```

## Files to Check for Errors

If build fails, check these locations:
- **Gradle Output:** Android Studio Build tab
- **CMake Logs:** `code\appImmViewer\build\intermediates\cxx\Debug\*\build_output.txt`
- **NDK Logs:** `code\appImmViewer\build\intermediates\cxx\Debug\*\cmake_build_output.txt`

---
**Last Updated:** 2026-01-05
**Status:** ✅ Configuration complete, ready for build attempt
**Next Session:** Run build and troubleshoot C++ compilation errors if any

