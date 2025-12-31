@echo off
echo Steam Achievement Manager Launcher

echo Attempting to build...
dotnet build --configuration Release

if exist "upload\SAM.Picker.exe" (
    echo Launching Release build...
    cd upload
    start "" "SAM.Picker.exe"
    exit /b 0
)

if exist "bin\SAM.Picker.exe" (
    echo Launching Debug build...
    cd bin
    start "" "SAM.Picker.exe"
    exit /b 0
)

echo.
echo Could not find SAM.Picker.exe in 'upload' or 'bin'.
echo Please build the solution in Visual Studio.
pause
exit /b 1
