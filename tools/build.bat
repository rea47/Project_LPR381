@echo off
echo Building Operations Research Solver...
dotnet clean
dotnet build --configuration Release
if %ERRORLEVEL% EQU 0 (
    echo Build successful!
) else (
    echo Build failed!
)
pause