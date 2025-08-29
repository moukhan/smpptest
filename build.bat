@echo off
echo Building SMPP Tester for .NET 4...
echo.

REM Try to find MSBuild for .NET 4
set MSBUILD_PATH=""

REM Check common MSBuild locations for .NET 4
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
) else if exist "%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe" (
    set MSBUILD_PATH="%ProgramFiles(x86)%\MSBuild\12.0\Bin\MSBuild.exe"
) else if exist "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
    set MSBUILD_PATH="%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

if %MSBUILD_PATH%=="" (
    echo ERROR: Could not find MSBuild for .NET Framework 4.0
    echo.
    echo Please ensure one of the following is installed:
    echo - Visual Studio 2010 or later
    echo - .NET Framework 4.0 SDK
    echo - Build Tools for Visual Studio
    echo.
    echo Alternative: Try using the .NET Framework 4.0 compiler directly:
    echo %windir%\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:exe /out:SMPPTester.exe SMPPTester\*.cs
    pause
    exit /b 1
)

echo Using MSBuild: %MSBUILD_PATH%
echo.

REM Build the solution
%MSBUILD_PATH% SMPPTester.sln /p:Configuration=Release /p:Platform=x86 /p:TargetFrameworkVersion=v4.0

if %ERRORLEVEL%==0 (
    echo.
    echo Build successful!
    echo.
    echo Executable location: SMPPTester\bin\Release\SMPPTester.exe
    echo.
    echo Usage example:
    echo SMPPTester\bin\Release\SMPPTester.exe -h 192.168.1.100 -p 2775 -u testuser -w testpass
    echo.
) else (
    echo.
    echo Build failed! Error code: %ERRORLEVEL%
    echo.
)

pause