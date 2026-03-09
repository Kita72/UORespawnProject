@echo off
:: UORespawn - Double-click publish launcher
:: Runs publish-windows.ps1 and keeps the window open when done

pushd "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "publish-windows.ps1"
echo.
pause
popd
