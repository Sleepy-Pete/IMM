@echo off
echo Testing IMM Viewer...
echo.

set VIEWER="code\appImmViewer\exe\appImmViewer_Release.exe"
set SAMPLE="exampleImmFiles\sample1.imm"

if not exist %VIEWER% (
    echo ERROR: Viewer not found at %VIEWER%
    echo Please run build.bat first.
    exit /b 1
)

if not exist %SAMPLE% (
    echo ERROR: Sample file not found at %SAMPLE%
    exit /b 1
)

echo Launching IMM Viewer with sample file...
echo Viewer: %VIEWER%
echo Sample: %SAMPLE%
echo.
echo Press Ctrl+C to stop the viewer when done testing.
echo.

start "" %VIEWER% %SAMPLE%

echo.
echo Viewer launched! Check the application window.
echo.

