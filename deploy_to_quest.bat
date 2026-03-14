@echo off
REM ============================================================================
REM IMM Viewer Quest Deployment Script
REM ============================================================================
REM This script deploys the IMM Viewer APK to a connected Meta Quest device
REM and optionally pushes sample IMM files.
REM
REM Prerequisites:
REM   - Meta Quest device connected via USB
REM   - Developer Mode enabled on Quest
REM   - ADB drivers installed (Windows)
REM   - APK already built
REM
REM Usage:
REM   deploy_to_quest.bat [debug|release]
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
echo %BLUE%                IMM Viewer Quest Deployment Script%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Parse command line arguments
set BUILD_TYPE=debug
if not "%1"=="" set BUILD_TYPE=%1

REM Validate build type
if /i "%BUILD_TYPE%"=="debug" goto :valid_type
if /i "%BUILD_TYPE%"=="release" goto :valid_type
echo %RED%Error: Invalid build type '%BUILD_TYPE%'%RESET%
echo Usage: deploy_to_quest.bat [debug^|release]
exit /b 1

:valid_type

REM Check if ADB is available
where adb >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo %RED%Error: ADB not found in PATH%RESET%
    echo Please install Android SDK Platform-Tools or add it to PATH.
    exit /b 1
)

echo %GREEN%✓ ADB found%RESET%

REM Check if device is connected
echo %YELLOW%Checking for connected Quest device...%RESET%
adb devices | findstr /r "device$" >nul
if %ERRORLEVEL% neq 0 (
    echo %RED%Error: No Quest device detected!%RESET%
    echo.
    echo Please ensure:
    echo 1. Quest is connected via USB-C cable
    echo 2. Developer Mode is enabled on Quest
    echo 3. USB debugging is authorized (check headset for prompt)
    echo.
    echo Run 'adb devices' to see connected devices.
    exit /b 1
)

echo %GREEN%✓ Quest device connected%RESET%
echo.

REM Set APK path based on build type
if /i "%BUILD_TYPE%"=="debug" (
    set "APK_PATH=code\appImmViewer\build\outputs\apk\debug\appImmViewer-debug.apk"
) else (
    set "APK_PATH=code\appImmViewer\build\outputs\apk\release\appImmViewer-release.apk"
)

REM Check if APK exists
if not exist "%APK_PATH%" (
    echo %RED%Error: APK not found at %APK_PATH%%RESET%
    echo.
    echo Please build the APK first using:
    echo   build_android.bat %BUILD_TYPE%
    exit /b 1
)

echo %GREEN%✓ APK found: %APK_PATH%%RESET%
echo.

REM Uninstall previous version (if exists)
echo %YELLOW%Uninstalling previous version (if exists)...%RESET%
adb uninstall org.linuxfoundation.imm.player >nul 2>&1
echo %GREEN%✓ Previous version removed%RESET%

REM Install APK
echo.
echo %YELLOW%Installing APK to Quest...%RESET%
adb install -r "%APK_PATH%"
if %ERRORLEVEL% neq 0 (
    echo %RED%Error: Failed to install APK%RESET%
    exit /b %ERRORLEVEL%
)

echo %GREEN%✓ APK installed successfully%RESET%
echo.

REM Ask if user wants to push sample files
set /p PUSH_FILES="Do you want to push sample IMM files to the device? (Y/N): "
if /i "%PUSH_FILES%"=="Y" goto :push_files
goto :launch_app

:push_files
echo.
echo %YELLOW%Creating app directory on device...%RESET%
adb shell mkdir -p /sdcard/Android/data/org.linuxfoundation.imm.player/files
echo %GREEN%✓ Directory created%RESET%

REM Check if sample files exist
if exist "exampleImmFiles\sample1.imm" (
    echo %YELLOW%Pushing sample1.imm...%RESET%
    adb push exampleImmFiles\sample1.imm /sdcard/Android/data/org.linuxfoundation.imm.player/files/
    echo %GREEN%✓ sample1.imm pushed%RESET%
)

REM Push all IMM files in exampleImmFiles directory
for %%f in (exampleImmFiles\*.imm) do (
    echo %YELLOW%Pushing %%~nxf...%RESET%
    adb push "%%f" /sdcard/Android/data/org.linuxfoundation.imm.player/files/
    echo %GREEN%✓ %%~nxf pushed%RESET%
)

echo.
echo %GREEN%✓ All IMM files pushed to device%RESET%
echo.

REM List files on device
echo %BLUE%Files on device:%RESET%
adb shell ls -lh /sdcard/Android/data/org.linuxfoundation.imm.player/files/
echo.

:launch_app
REM Ask if user wants to launch the app
set /p LAUNCH="Do you want to launch the app now? (Y/N): "
if /i not "%LAUNCH%"=="Y" goto :end

echo.
echo %YELLOW%Launching IMM Viewer...%RESET%
adb shell am start -n org.linuxfoundation.imm.player/.MainActivity
if %ERRORLEVEL% neq 0 (
    echo %RED%Error: Failed to launch app%RESET%
    exit /b %ERRORLEVEL%
)

echo %GREEN%✓ App launched%RESET%
echo.
echo %BLUE%Put on your Quest headset to see the app!%RESET%
echo.

REM Ask if user wants to view logs
set /p VIEW_LOGS="Do you want to view app logs? (Y/N): "
if /i not "%VIEW_LOGS%"=="Y" goto :end

echo.
echo %YELLOW%Viewing logs (Press Ctrl+C to stop)...%RESET%
echo.
adb logcat -s ImmViewer:V MainActivity:V VrApi:V Unity:V

:end
echo.
echo %GREEN%============================================================================%RESET%
echo %GREEN%                      DEPLOYMENT COMPLETE!%RESET%
echo %GREEN%============================================================================%RESET%
echo.
pause

