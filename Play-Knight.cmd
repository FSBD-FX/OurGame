@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\play-prologue.ps1" -GodotPath "%~dp0..\Tools\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe" -DotnetRoot "%~dp0..\Tools\dotnet-9.0.317"
if errorlevel 1 (
  echo.
  echo Knight could not start. Keep this window open for the error details above.
  pause
)
endlocal
