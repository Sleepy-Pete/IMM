@echo off
echo Building IMM Project...
echo.

set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if not exist %MSBUILD% (
    echo ERROR: MSBuild not found at %MSBUILD%
    echo Please install Visual Studio 2022 or update the path in this script.
    exit /b 1
)

echo Using MSBuild: %MSBUILD%
echo.

echo Building solution in Release x64 configuration...
%MSBUILD% code\projects\windows\imm.sln /p:Configuration=Release /p:Platform=x64 /m /v:minimal

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo BUILD FAILED!
    exit /b %ERRORLEVEL%
)

echo.
echo BUILD SUCCESSFUL!
echo.
echo Output files should be in:
echo - code\appImmViewer\exe\
echo - code\appImmUnity\exe\
echo.

