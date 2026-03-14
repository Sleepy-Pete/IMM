@echo off
REM Build script for ImmUnityPlugin Android library

echo ========================================
echo Building ImmUnityPlugin for Android
echo ========================================
echo.

REM Check if Android SDK is set
if "%ANDROID_HOME%"=="" (
    if exist "C:\Users\%USERNAME%\AppData\Local\Android\Sdk" (
        set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk
        echo Using Android SDK: %ANDROID_HOME%
    ) else (
        echo ERROR: ANDROID_HOME not set and SDK not found in default location
        echo Please set ANDROID_HOME environment variable
        exit /b 1
    )
)

REM Check if NDK exists
set NDK_PATH=%ANDROID_HOME%\ndk\24.0.8215888
if not exist "%NDK_PATH%" (
    echo ERROR: NDK not found at %NDK_PATH%
    echo Please install NDK 24.0.8215888 via Android Studio SDK Manager
    exit /b 1
)

REM Check if CMake exists
set CMAKE_PATH=%ANDROID_HOME%\cmake\3.22.1\bin\cmake.exe
if not exist "%CMAKE_PATH%" (
    echo ERROR: CMake not found at %CMAKE_PATH%
    echo Please install CMake 3.22.1 via Android Studio SDK Manager
    exit /b 1
)

echo Using NDK: %NDK_PATH%
echo Using CMake: %CMAKE_PATH%
echo.

REM Create build directory
set BUILD_DIR=%~dp0android-build
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"

REM Configure CMake
echo Configuring CMake...
call "%CMAKE_PATH%" -S "%~dp0" -B "%BUILD_DIR%" -DCMAKE_TOOLCHAIN_FILE="%NDK_PATH%\build\cmake\android.toolchain.cmake" -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-26 -DANDROID_STL=c++_shared -DCMAKE_BUILD_TYPE=Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: CMake configuration failed!
    exit /b %ERRORLEVEL%
)

echo.
echo Building...
"%CMAKE_PATH%" --build "%BUILD_DIR%" --config Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================
echo Build successful!
echo ========================================
echo.
echo Output: %BUILD_DIR%\arm64-v8a\libImmUnityPlugin.so
echo.
echo Next steps:
echo 1. Copy libImmUnityPlugin.so to Unity project:
echo    %~dp0..\ImmUnitySampleProject\Assets\Plugins\Android\arm64-v8a\
echo.
echo 2. Run the copy script:
echo    copy_android_plugin_to_unity.bat
echo.

pause

