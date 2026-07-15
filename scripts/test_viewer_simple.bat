@echo off
echo Testing IMM Viewer (using settings.json)...
echo.

cd code\appImmViewer\exe

if not exist "appImmViewer_Release.exe" (
    echo ERROR: Viewer not found
    echo Please run build.bat first.
    exit /b 1
)

echo Launching IMM Viewer...
echo The viewer will load the file specified in settings.json
echo.
echo Press any key in the viewer window to exit when done testing.
echo.

start "" "appImmViewer_Release.exe"

echo.
echo Viewer launched! Check the application window.
echo If you see errors, check debug.txt in this directory.
echo.

cd ..\..\..

