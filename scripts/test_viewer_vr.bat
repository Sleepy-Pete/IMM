@echo off
echo ========================================
echo IMM Viewer - VR Mode Test
echo ========================================
echo.
echo Make sure your VR headset is:
echo  - Connected and powered on
echo  - Oculus software is running
echo  - Headset is tracking properly
echo.
echo Press any key to launch the VR viewer...
pause > nul

cd code\appImmViewer\exe

if not exist "appImmViewer_Release.exe" (
    echo ERROR: Viewer not found
    echo Please run build.bat first.
    cd ..\..\..
    exit /b 1
)

echo.
echo Launching IMM Viewer in VR mode...
echo Loading: ../../../exampleImmFiles/sample1.imm
echo.
echo Controls (typical VR controls):
echo  - Look around with your headset
echo  - Use controllers to interact
echo  - Check debug.txt for any issues
echo.

start "" "appImmViewer_Release.exe"

echo.
echo Viewer launched in VR mode!
echo.
echo If the viewer exits immediately, check debug.txt for errors.
echo Common issues:
echo  - Oculus software not running
echo  - Headset not detected
echo  - USB/DisplayPort connection issues
echo.

cd ..\..\..

