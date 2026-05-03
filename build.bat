@echo off
cd /d "c:\Users\robin\Desktop\Velox strap"
rmdir /s /q bin 2>nul
rmdir /s /q obj 2>nul
dotnet publish -c Release --self-contained true -r win-x64 /p:PublishTrimmed=false
if %errorlevel% neq 0 (
    echo Build failed
    pause
    exit /b 1
)
copy "bin\Release\net8.0-windows\win-x64\publish\VeloxStrap.exe" "VeloxStrap-Setup-v1.0.0.exe" /y
echo Build completed successfully
pause