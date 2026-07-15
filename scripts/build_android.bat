@echo off
REM ============================================================================
REM IMM Viewer Android Build Script
REM ============================================================================
REM This script automates the build process for creating an Android APK
REM that can run on Meta Quest devices.
REM
REM Prerequisites:
REM   - Android Studio installed with SDK and NDK
REM   - Oculus Mobile SDK installed in thirdparty/ovr-mobile-sdk/
REM   - Third-party libraries built for Android ARM64
REM
REM Usage:
REM   build_android.bat [debug|release|clean]
REM
REM ============================================================================

setlocal enabledelayedexpansion

REM Set colors for output
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "RESET=[0m"

echo %BLUE%============================================================================%RESET%
echo %BLUE%                    IMM Viewer Android Build Script%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Parse command line arguments
set BUILD_TYPE=debug
if not "%1"=="" set BUILD_TYPE=%1

REM Validate build type
if /i "%BUILD_TYPE%"=="debug" goto :valid_type
if /i "%BUILD_TYPE%"=="release" goto :valid_type
if /i "%BUILD_TYPE%"=="clean" goto :valid_type
echo %RED%Error: Invalid build type '%BUILD_TYPE%'%RESET%
echo Usage: build_android.bat [debug^|release^|clean]
exit /b 1

:valid_type

REM Check if Android SDK is installed
if not defined ANDROID_HOME (
    echo %YELLOW%Warning: ANDROID_HOME not set. Trying default location...%RESET%
    set "ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk"
)

if not exist "%ANDROID_HOME%" (
    echo %RED%Error: Android SDK not found at %ANDROID_HOME%%RESET%
    echo Please install Android Studio and set ANDROID_HOME environment variable.
    exit /b 1
)

echo %GREEN%✓ Android SDK found: %ANDROID_HOME%%RESET%

REM Check if NDK is installed
if not defined ANDROID_NDK_HOME (
    echo %YELLOW%Warning: ANDROID_NDK_HOME not set. Trying default location...%RESET%
    set "ANDROID_NDK_HOME=%ANDROID_HOME%\ndk\25.2.9519653"
)

if not exist "%ANDROID_NDK_HOME%" (
    echo %RED%Error: Android NDK not found at %ANDROID_NDK_HOME%%RESET%
    echo Please install NDK 25.2.9519653 via Android Studio SDK Manager.
    exit /b 1
)

echo %GREEN%✓ Android NDK found: %ANDROID_NDK_HOME%%RESET%

REM Check if Oculus Mobile SDK is installed
set "VRAPI_HEADER=thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h"
if not exist "%VRAPI_HEADER%" (
    echo %RED%Error: Oculus Mobile SDK not found!%RESET%
    echo Expected location: %VRAPI_HEADER%
    echo.
    echo Please download the Oculus Mobile SDK from:
    echo https://developer.oculus.com/downloads/native-android/
    echo.
    echo Extract and copy the VrApi directory to: thirdparty\ovr-mobile-sdk\
    exit /b 1
)

echo %GREEN%✓ Oculus Mobile SDK found%RESET%

REM Navigate to Android project directory
cd code\projects\android
if %ERRORLEVEL% neq 0 (
    echo %RED%Error: Could not navigate to Android project directory%RESET%
    exit /b 1
)

echo.
echo %BLUE%Current directory: %CD%%RESET%
echo.

REM Execute build based on type
if /i "%BUILD_TYPE%"=="clean" goto :clean_build
if /i "%BUILD_TYPE%"=="debug" goto :debug_build
if /i "%BUILD_TYPE%"=="release" goto :release_build

:clean_build
echo %YELLOW%Cleaning previous build...%RESET%
call gradlew clean
if %ERRORLEVEL% neq 0 (
    echo %RED%Clean failed!%RESET%
    exit /b %ERRORLEVEL%
)
echo %GREEN%✓ Clean completed successfully%RESET%
goto :end

:debug_build
echo %YELLOW%Building Debug APK...%RESET%
echo.
call gradlew assembleDebug
if %ERRORLEVEL% neq 0 (
    echo.
    echo %RED%============================================================================%RESET%
    echo %RED%                           BUILD FAILED!%RESET%
    echo %RED%============================================================================%RESET%
    echo.
    echo Check the error messages above for details.
    exit /b %ERRORLEVEL%
)
echo.
echo %GREEN%============================================================================%RESET%
echo %GREEN%                        BUILD SUCCESSFUL!%RESET%
echo %GREEN%============================================================================%RESET%
echo.
echo %GREEN%Debug APK created at:%RESET%
echo %CD%\..\..\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk
echo.
echo %YELLOW%Next steps:%RESET%
echo 1. Connect your Meta Quest device via USB
echo 2. Enable Developer Mode on the Quest
echo 3. Run: adb install -r ..\..\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk
echo.
goto :end

:release_build
echo %YELLOW%Building Release APK...%RESET%
echo.
call gradlew assembleRelease
if %ERRORLEVEL% neq 0 (
    echo.
    echo %RED%============================================================================%RESET%
    echo %RED%                           BUILD FAILED!%RESET%
    echo %RED%============================================================================%RESET%
    echo.
    exit /b %ERRORLEVEL%
)
echo.
echo %GREEN%============================================================================%RESET%
echo %GREEN%                        BUILD SUCCESSFUL!%RESET%
echo %GREEN%============================================================================%RESET%
echo.
echo %GREEN%Release APK created at:%RESET%
echo %CD%\..\..\appImmViewer\build\outputs\apk\release\appImmViewer-release.apk
echo.
goto :end

:end
cd ..\..\..
echo.
pause

