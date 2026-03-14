@echo off
REM ============================================================================
REM Oculus Mobile SDK Setup Helper
REM ============================================================================
REM This script helps you set up the Oculus Mobile SDK for Android development.
REM
REM The Oculus Mobile SDK must be downloaded manually from Meta's developer site
REM because it requires authentication and acceptance of license terms.
REM
REM This script will:
REM   1. Check if the SDK is already installed
REM   2. Provide download instructions
REM   3. Help you extract and install the SDK to the correct location
REM
REM ============================================================================

setlocal enabledelayedexpansion

REM Set colors for output
set "GREEN=[92m"
set "RED=[91m"
set "YELLOW=[93m"
set "BLUE=[94m"
set "CYAN=[96m"
set "RESET=[0m"

echo %BLUE%============================================================================%RESET%
echo %BLUE%              Oculus Mobile SDK Setup Helper%RESET%
echo %BLUE%============================================================================%RESET%
echo.

REM Check if SDK is already installed
set "VRAPI_HEADER=thirdparty\ovr-mobile-sdk\VrApi\Include\VrApi.h"
set "VRAPI_LIB=thirdparty\ovr-mobile-sdk\VrApi\Libs\Android\arm64-v8a\libvrapi.so"

if exist "%VRAPI_HEADER%" (
    if exist "%VRAPI_LIB%" (
        echo %GREEN%✓ Oculus Mobile SDK is already installed!%RESET%
        echo.
        echo %CYAN%SDK Location:%RESET%
        echo   Headers: %VRAPI_HEADER%
        echo   Library: %VRAPI_LIB%
        echo.
        echo %GREEN%You're ready to build the Android APK!%RESET%
        echo.
        pause
        exit /b 0
    )
)

echo %YELLOW%Oculus Mobile SDK not found.%RESET%
echo.
echo %CYAN%The Oculus Mobile SDK is required to build IMM Viewer for Meta Quest.%RESET%
echo.

REM Provide download instructions
echo %BLUE%============================================================================%RESET%
echo %BLUE%                    DOWNLOAD INSTRUCTIONS%RESET%
echo %BLUE%============================================================================%RESET%
echo.
echo %CYAN%Step 1: Download the SDK%RESET%
echo.
echo   1. Visit: %YELLOW%https://developer.oculus.com/downloads/native-android/%RESET%
echo.
echo   2. Sign in with your Meta/Oculus developer account
echo      (Create one if you don't have it: https://developer.oculus.com/)
echo.
echo   3. Download the latest "Oculus Mobile SDK" package
echo      (Look for "ovr_sdk_mobile_*.zip")
echo.
echo   4. Save the ZIP file to a location you can remember
echo.
echo %CYAN%Step 2: Extract the SDK%RESET%
echo.
echo   After downloading, you have two options:
echo.
echo   %YELLOW%Option A: Let this script help you (Recommended)%RESET%
echo     - Extract the ZIP file anywhere on your computer
echo     - Run this script again and follow the prompts
echo.
echo   %YELLOW%Option B: Manual installation%RESET%
echo     - Extract the ZIP file
echo     - Copy the "VrApi" folder to: %CD%\thirdparty\ovr-mobile-sdk\
echo.
echo %BLUE%============================================================================%RESET%
echo.

REM Ask if user has already downloaded the SDK
set /p DOWNLOADED="Have you already downloaded the Oculus Mobile SDK? (Y/N): "
if /i not "%DOWNLOADED%"=="Y" (
    echo.
    echo %YELLOW%Please download the SDK first, then run this script again.%RESET%
    echo.
    echo Opening download page in your browser...
    start https://developer.oculus.com/downloads/native-android/
    echo.
    pause
    exit /b 0
)

REM Ask for the extracted SDK location
echo.
echo %CYAN%Please provide the path where you extracted the Oculus Mobile SDK.%RESET%
echo %YELLOW%Example: C:\Downloads\ovr_sdk_mobile_1.57.0%RESET%
echo.
set /p SDK_PATH="Enter the path to the extracted SDK: "

REM Remove quotes if present
set SDK_PATH=%SDK_PATH:"=%

REM Check if path exists
if not exist "%SDK_PATH%" (
    echo.
    echo %RED%Error: Path does not exist: %SDK_PATH%%RESET%
    echo.
    pause
    exit /b 1
)

REM Check if VrApi directory exists in the provided path
if not exist "%SDK_PATH%\VrApi" (
    echo.
    echo %RED%Error: VrApi directory not found in: %SDK_PATH%%RESET%
    echo.
    echo %YELLOW%Please make sure you provided the correct path to the extracted SDK.%RESET%
    echo The directory should contain a "VrApi" folder.
    echo.
    pause
    exit /b 1
)

REM Create target directory
echo.
echo %YELLOW%Creating target directory...%RESET%
if not exist "thirdparty\ovr-mobile-sdk" mkdir "thirdparty\ovr-mobile-sdk"

REM Copy VrApi directory
echo %YELLOW%Copying VrApi directory...%RESET%
xcopy /E /I /Y "%SDK_PATH%\VrApi" "thirdparty\ovr-mobile-sdk\VrApi"

if %ERRORLEVEL% neq 0 (
    echo.
    echo %RED%Error: Failed to copy VrApi directory%RESET%
    pause
    exit /b 1
)

REM Verify installation
if exist "%VRAPI_HEADER%" (
    if exist "%VRAPI_LIB%" (
        echo.
        echo %GREEN%============================================================================%RESET%
        echo %GREEN%                    INSTALLATION SUCCESSFUL!%RESET%
        echo %GREEN%============================================================================%RESET%
        echo.
        echo %GREEN%✓ Oculus Mobile SDK installed successfully%RESET%
        echo.
        echo %CYAN%Installed files:%RESET%
        echo   Headers: %VRAPI_HEADER%
        echo   Library: %VRAPI_LIB%
        echo.
        echo %YELLOW%Next steps:%RESET%
        echo   1. Build third-party libraries for Android (see ANDROID_BUILD_GUIDE.md)
        echo   2. Run: build_android.bat debug
        echo   3. Deploy to Quest: deploy_to_quest.bat debug
        echo.
    ) else (
        echo.
        echo %RED%Warning: VrApi library not found after installation%RESET%
        echo Expected: %VRAPI_LIB%
    )
) else (
    echo.
    echo %RED%Warning: VrApi headers not found after installation%RESET%
    echo Expected: %VRAPI_HEADER%
)

echo.
pause

