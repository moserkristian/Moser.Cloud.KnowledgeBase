@echo off
setlocal
cd /d "%~dp0"
title Native Ollama
echo.
echo  Native Ollama pre policy assistant. Okno nechaj otvorene.
echo  Ak vyskoci UAC (admin), klikni Yes - uvolni to port 11434.
echo.
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Ensure-NativeOllama.ps1" %*
set EXITCODE=%ERRORLEVEL%
if not "%EXITCODE%"=="0" (
  echo.
  echo  Skript skoncil s chybou %EXITCODE%.
  pause
)
exit /b %EXITCODE%
