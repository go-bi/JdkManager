@echo off
echo ========================================
echo   JDK Manager Publish Script
echo ========================================
echo.

echo [1/2] Cleaning old builds...
call dotnet clean src\JdkManager\JdkManager.csproj

echo [2/2] Publishing application...
call dotnet publish src\JdkManager\JdkManager.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseAppHost=true -o publish\win-x64

if %ERRORLEVEL% neq 0 (
    echo.
    echo [Error] Publish failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   Publish completed!
echo   Output: publish\win-x64\
echo   Executable: JdkManager.exe
echo ========================================
echo.
pause
