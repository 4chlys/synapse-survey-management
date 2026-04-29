@echo off
start cmd /k "cd UI-User && pnpm dev"
start cmd /k "cd UI-User && dotnet run"
start cmd /k "cd API && dotnet run"