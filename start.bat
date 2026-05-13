@echo off
setlocal

set API_HEALTH=https://localhost:7128/health

:: Start the API in a new terminal window
echo Starting API...
start "API" cmd /k "cd /d "%~dp0API" && dotnet run"

:: Wait until the API health endpoint responds with "healthy"
echo Waiting for API to be ready...
:wait_loop
    timeout /t 2 /nobreak >nul
    curl -sk "%API_HEALTH%" | findstr /i "healthy" >nul 2>&1
    if errorlevel 1 goto wait_loop

echo API is ready - starting UI apps.

:: Start the User UI frontend (pnpm dev) in a new terminal window
start "UI-User (pnpm)" cmd /k "cd /d "%~dp0UI-User" && pnpm dev"

:: Start the User UI backend (dotnet) in a new terminal window
start "UI-User (dotnet)" cmd /k "cd /d "%~dp0UI-User" && dotnet run"

:: Start the Admin UI frontend (pnpm dev) in a new terminal window
start "UI-Admin (pnpm)" cmd /k "cd /d "%~dp0UI-Admin" && pnpm dev"

:: Start the Admin UI backend (dotnet) in a new terminal window
start "UI-Admin (dotnet)" cmd /k "cd /d "%~dp0UI-Admin" && dotnet run"

:: Open the Admin UI in the default browser
start "" "http://localhost:5154"
start "" "http://localhost:5291"

endlocal