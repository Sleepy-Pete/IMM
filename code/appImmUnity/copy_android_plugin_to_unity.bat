@echo off
REM Copy Android Unity plugin to Unity project

echo ========================================
echo Copying Android Unity Plugin
echo ========================================
echo.

set SOURCE_SO=%~dp0android-build\arm64-v8a\libImmUnityPlugin.so
set DEST_DIR=%~dp0..\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a

REM Check if source file exists
if not exist "%SOURCE_SO%" (
    echo ERROR: libImmUnityPlugin.so not found!
    echo Please build the Android plugin first using build_android.bat
    exit /b 1
)

REM Create destination directory if it doesn't exist
if not exist "%DEST_DIR%" (
    echo Creating directory: %DEST_DIR%
    mkdir "%DEST_DIR%"
)

REM Copy the file
echo Copying: %SOURCE_SO%
echo To: %DEST_DIR%
copy /Y "%SOURCE_SO%" "%DEST_DIR%\"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Failed to copy file!
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Copy successful!
echo ========================================
echo.
echo The Android Unity plugin has been copied to:
echo %DEST_DIR%\libImmUnityPlugin.so
echo.
echo Next steps:
echo 1. Open Unity project
echo 2. Select the libImmUnityPlugin.so file in Project window
echo 3. In Inspector, verify platform settings:
echo    - Android: Enabled
echo    - CPU: ARM64
echo 4. Build your Unity project for Android
echo.

pause

