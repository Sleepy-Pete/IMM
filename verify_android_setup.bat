@echo off
REM ============================================================================
REM Android Build Setup Verification Script
REM ============================================================================
REM This script checks if all required components are installed and configured
REM correctly for building the IMM Viewer Android APK.
REM ============================================================================

setlocal enabledelayedexpansion

REM Set colors for output
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "CYAN=[96m"
set "RESET=[0m"

set ERRORS=0
set WARNINGS=0

echo %BLUE%============================================================================%RESET%
echo %BLUE%          Android Build Setup Verification%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Check Android SDK
echo %CYAN%Checking Android SDK...%RESET%
if defined ANDROID_HOME (
    if exist "%ANDROID_HOME%" (
        echo %GREEN%✓ ANDROID_HOME is set: %ANDROID_HOME%%RESET%
    ) else (
        echo %RED%✗ ANDROID_HOME is set but directory doesn't exist: %ANDROID_HOME%%RESET%
        set /a ERRORS+=1
    )
) else (
    echo %RED%✗ ANDROID_HOME environment variable not set%RESET%
    set /a ERRORS+=1
)

REM Check Android NDK
echo %CYAN%Checking Android NDK...%RESET%
if defined ANDROID_NDK_HOME (
    if exist "%ANDROID_NDK_HOME%" (
        echo %GREEN%✓ ANDROID_NDK_HOME is set: %ANDROID_NDK_HOME%%RESET%
    ) else (
        echo %RED%✗ ANDROID_NDK_HOME is set but directory doesn't exist: %ANDROID_NDK_HOME%%RESET%
        set /a ERRORS+=1
    )
) else (
    echo %YELLOW%⚠ ANDROID_NDK_HOME environment variable not set%RESET%
    echo   Will try default location...
    set /a WARNINGS+=1
)

REM Check ADB
echo %CYAN%Checking ADB...%RESET%
where adb >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo %GREEN%✓ ADB found in PATH%RESET%
) else (
    echo %YELLOW%⚠ ADB not found in PATH%RESET%
    echo   You may need to add Android SDK platform-tools to PATH
    set /a WARNINGS+=1
)

REM Check Gradle
echo %CYAN%Checking Gradle...%RESET%
if exist "code\projects\android\gradlew.bat" (
    echo %GREEN%✓ Gradle wrapper found%RESET%
) else (
    echo %YELLOW%⚠ Gradle wrapper not found%RESET%
    echo   Will be downloaded on first build
    set /a WARNINGS+=1
)

echo.
echo %BLUE%============================================================================%RESET%
echo %BLUE%          Checking Required SDKs and Libraries%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Check Oculus Mobile SDK
echo %CYAN%Checking Oculus Mobile SDK...%RESET%
set VRAPI_HEADER=thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h
set VRAPI_LIB=thirdparty\ovr-mobile-sdk\VrApi\Libs\Android\arm64-v8a\Release\libvrapi.so

if exist "%VRAPI_HEADER%" (
    echo %GREEN%✓ VrApi headers found%RESET%
) else (
    echo %RED%✗ VrApi headers not found: %VRAPI_HEADER%%RESET%
    echo   Run: setup_oculus_sdk.bat
    set /a ERRORS+=1
)

if exist "%VRAPI_LIB%" (
    echo %GREEN%✓ VrApi library found%RESET%
) else (
    echo %RED%✗ VrApi library not found: %VRAPI_LIB%%RESET%
    echo   Run: setup_oculus_sdk.bat
    set /a ERRORS+=1
)

REM Check Audio360 SDK
echo %CYAN%Checking Audio360 SDK...%RESET%
set AUDIO360_LIB=thirdparty\audio360-sdk\Audio360\Android\arm64-v8a\libAudio360.so

if exist "%AUDIO360_LIB%" (
    echo %GREEN%✓ Audio360 library found%RESET%
) else (
    echo %YELLOW%⚠ Audio360 library not found: %AUDIO360_LIB%%RESET%
    echo   Spatial audio will not work. See SDK_DOWNLOAD_GUIDE.md
    set /a WARNINGS+=1
)

REM Check third-party libraries
echo %CYAN%Checking third-party libraries...%RESET%

set LIBS_FOUND=0
set LIBS_TOTAL=7

if exist "thirdparty\zlib\android\arm64-v8a\libz.so" (
    echo %GREEN%✓ zlib found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ zlib not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\libpng\android\arm64-v8a\libpng16.so" (
    echo %GREEN%✓ libpng found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ libpng not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\libjpeg-turbo\android\arm64-v8a\libjpeg.so" (
    echo %GREEN%✓ libjpeg-turbo found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ libjpeg-turbo not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\libogg\android\arm64-v8a\libogg.so" (
    echo %GREEN%✓ libogg found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ libogg not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\libvorbis\android\arm64-v8a\libvorbis.so" (
    echo %GREEN%✓ libvorbis found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ libvorbis not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\opus\android\arm64-v8a\libopus.so" (
    echo %GREEN%✓ opus found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ opus not found%RESET%
    set /a WARNINGS+=1
)

if exist "thirdparty\libopusenc\android\arm64-v8a\libopusenc.so" (
    echo %GREEN%✓ libopusenc found%RESET%
    set /a LIBS_FOUND+=1
) else (
    echo %YELLOW%⚠ libopusenc not found%RESET%
    set /a WARNINGS+=1
)

echo.
echo %CYAN%Third-party libraries: %LIBS_FOUND%/%LIBS_TOTAL% found%RESET%
if %LIBS_FOUND% lss %LIBS_TOTAL% (
    echo %YELLOW%Some features may not work without these libraries.%RESET%
    echo See SDK_DOWNLOAD_GUIDE.md for instructions.
)

echo.
echo %BLUE%============================================================================%RESET%
echo %BLUE%          Checking Project Files%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Check project structure
echo %CYAN%Checking project structure...%RESET%

if exist "code\projects\android\build.gradle" (
    echo %GREEN%✓ Root build.gradle found%RESET%
) else (
    echo %RED%✗ Root build.gradle not found%RESET%
    set /a ERRORS+=1
)

if exist "code\projects\android\settings.gradle" (
    echo %GREEN%✓ settings.gradle found%RESET%
) else (
    echo %RED%✗ settings.gradle not found%RESET%
    set /a ERRORS+=1
)

if exist "code\appImmViewer\build.gradle" (
    echo %GREEN%✓ App build.gradle found%RESET%
) else (
    echo %RED%✗ App build.gradle not found%RESET%
    set /a ERRORS+=1
)

if exist "code\appImmViewer\CMakeLists.txt" (
    echo %GREEN%✓ CMakeLists.txt found%RESET%
) else (
    echo %RED%✗ CMakeLists.txt not found%RESET%
    set /a ERRORS+=1
)

if exist "code\appImmViewer\src\android\AndroidManifest.xml" (
    echo %GREEN%✓ AndroidManifest.xml found%RESET%
) else (
    echo %RED%✗ AndroidManifest.xml not found%RESET%
    set /a ERRORS+=1
)

echo.
echo %BLUE%============================================================================%RESET%
echo %BLUE%          Summary%RESET%
echo %BLUE%============================================================================%RESET%
echo.

if %ERRORS% equ 0 (
    if %WARNINGS% equ 0 (
        echo %GREEN%✓ All checks passed! You're ready to build.%RESET%
        echo.
        echo %CYAN%Next steps:%RESET%
        echo   1. Run: build_android.bat debug
        echo   2. Connect your Quest device
        echo   3. Run: deploy_to_quest.bat debug
    ) else (
        echo %YELLOW%⚠ Setup complete with %WARNINGS% warning(s)%RESET%
        echo.
        echo %CYAN%You can build the APK, but some features may not work.%RESET%
        echo See warnings above for details.
        echo.
        echo %CYAN%To build anyway:%RESET%
        echo   Run: build_android.bat debug
    )
) else (
    echo %RED%✗ Setup incomplete: %ERRORS% error(s), %WARNINGS% warning(s)%RESET%
    echo.
    echo %CYAN%Please fix the errors above before building.%RESET%
    echo.
    echo %CYAN%Common fixes:%RESET%
    echo   - Install Android Studio and SDK components
    echo   - Run: setup_oculus_sdk.bat
    echo   - See: ANDROID_BUILD_GUIDE.md for detailed instructions
)

echo.
pause
exit /b %ERRORS%

