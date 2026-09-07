@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\play-prologue.ps1"
if errorlevel 1 (
  echo.
  echo Knight could not start. Keep this window open for the error details above.
  pause
)
endlocal
