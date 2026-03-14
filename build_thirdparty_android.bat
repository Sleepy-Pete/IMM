@echo off
REM Build third-party libraries for Android ARM64
REM This script builds all required third-party libraries for the Unity Android plugin

echo ========================================
echo Building Third-Party Libraries for Android ARM64
echo ========================================
echo.

REM Check if Android SDK is set
if "%ANDROID_HOME%"=="" (
    if exist "C:\Users\%USERNAME%\AppData\Local\Android\Sdk" (
        set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk
    ) else (
        echo ERROR: ANDROID_HOME not set
        exit /b 1
    )
)

set NDK_PATH=%ANDROID_HOME%\ndk\24.0.8215888
set CMAKE_PATH=%ANDROID_HOME%\cmake\3.22.1\bin\cmake.exe
set NINJA_PATH=%ANDROID_HOME%\cmake\3.22.1\bin\ninja.exe

if not exist "%NDK_PATH%" (
    echo ERROR: NDK not found at %NDK_PATH%
    exit /b 1
)

if not exist "%CMAKE_PATH%" (
    echo ERROR: CMake not found at %CMAKE_PATH%
    exit /b 1
)

echo Using NDK: %NDK_PATH%
echo Using CMake: %CMAKE_PATH%
echo.

REM Function to build a library
REM Usage: call :BuildLibrary library_name source_dir

echo ========================================
echo Building zlib...
echo ========================================
call :BuildLibrary zlib thirdparty\zlib

echo.
echo ========================================
echo Building libpng...
echo ========================================
call :BuildLibrary libpng thirdparty\libpng

echo.
echo ========================================
echo Building libjpeg-turbo...
echo ========================================
call :BuildLibrary libjpeg-turbo thirdparty\libjpeg-turbo

echo.
echo ========================================
echo Building libogg...
echo ========================================
call :BuildLibrary libogg thirdparty\libogg

echo.
echo ========================================
echo Building libvorbis...
echo ========================================
call :BuildLibrary libvorbis thirdparty\libvorbis

echo.
echo ========================================
echo Building opus...
echo ========================================
call :BuildLibrary opus thirdparty\opus

echo.
echo ========================================
echo Building libopusenc...
echo ========================================
call :BuildLibrary libopusenc thirdparty\libopusenc

echo.
echo ========================================
echo All libraries built successfully!
echo ========================================
echo.
echo Libraries are installed in:
echo   thirdparty\[library]\android\arm64-v8a\
echo.
echo Next step: Run build_android_unity_plugin.bat
echo.
pause
exit /b 0

:BuildLibrary
setlocal
set LIB_NAME=%~1
set SRC_DIR=%~2
set BUILD_DIR=%SRC_DIR%\build-android
set INSTALL_DIR=%SRC_DIR%\android\arm64-v8a

echo Building %LIB_NAME%...
echo Source: %SRC_DIR%
echo Build: %BUILD_DIR%
echo Install: %INSTALL_DIR%
echo.

if not exist "%SRC_DIR%\CMakeLists.txt" (
    echo WARNING: %SRC_DIR%\CMakeLists.txt not found, skipping...
    endlocal
    exit /b 0
)

REM Create build directory
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%"

REM Configure
"%CMAKE_PATH%" -G Ninja ^
    -S "%SRC_DIR%" ^
    -B "%BUILD_DIR%" ^
    -DCMAKE_TOOLCHAIN_FILE="%NDK_PATH%\build\cmake\android.toolchain.cmake" ^
    -DANDROID_ABI=arm64-v8a ^
    -DANDROID_PLATFORM=android-26 ^
    -DCMAKE_BUILD_TYPE=Release ^
    -DCMAKE_INSTALL_PREFIX="%INSTALL_DIR%" ^
    -DCMAKE_MAKE_PROGRAM="%NINJA_PATH%"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: CMake configuration failed for %LIB_NAME%
    endlocal
    exit /b 1
)

REM Build
"%CMAKE_PATH%" --build "%BUILD_DIR%" --config Release

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Build failed for %LIB_NAME%
    endlocal
    exit /b 1
)

REM Install
"%CMAKE_PATH%" --install "%BUILD_DIR%"

if %ERRORLEVEL% NEQ 0 (
    echo ERROR: Install failed for %LIB_NAME%
    endlocal
    exit /b 1
)

echo %LIB_NAME% built successfully!
endlocal
exit /b 0

