@echo off
setlocal enabledelayedexpansion

:: Build Windows LiteHtmlLib DLLs (x64, x86, ARM64) and copy to runtimes/
::
:: Run from a Developer Command Prompt or ensure msbuild is on PATH.

set SCRIPT_DIR=%~dp0
set VCXPROJ=%SCRIPT_DIR%LiteHtmlLib\LiteHtmlLib.vcxproj
set RUNTIMES=%SCRIPT_DIR%runtimes
set BUILT=0
set FAILED=0
set "VCVARSALL="
set "TOOLSET_ARG="

:: If msbuild isn't on PATH, try to find VS and set up environment
where msbuild >nul 2>&1
if errorlevel 1 (
    set "VS_PATH="
    set "PFX86=%ProgramFiles(x86)%"
    :: Try vswhere first
    set "VSWHERE=!PFX86!\Microsoft Visual Studio\Installer\vswhere.exe"
    if exist "!VSWHERE!" (
        for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2^>nul`) do (
            set "VS_PATH=%%i"
        )
    )
    :: Fallback: search common locations
    if not defined VS_PATH (
        for %%v in (18 17) do (
            for %%e in (BuildTools Community Professional Enterprise) do (
                if not defined VS_PATH if exist "!PFX86!\Microsoft Visual Studio\%%v\%%e\VC\Auxiliary\Build\vcvarsall.bat" (
                    set "VS_PATH=!PFX86!\Microsoft Visual Studio\%%v\%%e"
                )
            )
        )
    )
    if not defined VS_PATH (
        for %%y in (2022 2019) do (
            for %%e in (BuildTools Community Professional Enterprise) do (
                if not defined VS_PATH if exist "%ProgramFiles%\Microsoft Visual Studio\%%y\%%e\VC\Auxiliary\Build\vcvarsall.bat" (
                    set "VS_PATH=%ProgramFiles%\Microsoft Visual Studio\%%y\%%e"
                )
            )
        )
    )
    if defined VS_PATH (
        set "VCVARSALL=!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat"
        echo Found VS: !VS_PATH!
    )
)

:: Detect PlatformToolset from installed MSVC tools version
if defined VS_PATH (
    set "MSVC_TOOLS_DIR=!VS_PATH!\VC\Tools\MSVC"
    if exist "!MSVC_TOOLS_DIR!" (
        for /f "tokens=*" %%d in ('dir /b /ad /o-n "!MSVC_TOOLS_DIR!" 2^>nul') do (
            if "!TOOLSET_ARG!"=="" (
                for /f "tokens=1,2 delims=." %%a in ("%%d") do (
                    set "MINOR=%%b"
                    set "TOOLSET=v%%a!MINOR:~0,1!"
                    set "TOOLSET_ARG=/p:PlatformToolset=!TOOLSET!"
                    echo Detected PlatformToolset: !TOOLSET!
                )
            )
        )
    )
)
echo.

:: Initialize submodule if needed
if not exist "%SCRIPT_DIR%litehtml\src\html.h" (
    echo Initializing litehtml submodule...
    git -C "%SCRIPT_DIR%" submodule update --init
    if errorlevel 1 (
        echo ERROR: Failed to initialize submodule
        exit /b 1
    )
)

:: Build x64
echo ============================================================
echo Building win-x64...
echo ============================================================
call :build amd64 x64 win-x64
if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )

:: Build x86
echo ============================================================
echo Building win-x86...
echo ============================================================
call :build x86 Win32 win-x86
if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )

:: Build ARM64
echo ============================================================
echo Building win-arm64...
echo ============================================================
call :build arm64 ARM64 win-arm64
if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )

echo.
echo ============================================================
echo Results: !BUILT! built, !FAILED! failed
echo ============================================================
if !BUILT! GTR 0 (
    echo.
    echo Updated:
    for %%a in (win-x64 win-x86 win-arm64) do (
        if exist "!RUNTIMES!\%%a\native\LiteHtmlLib.dll" echo   runtimes\%%a\native\LiteHtmlLib.dll
    )
)
if !FAILED! GTR 0 ( exit /b 1 ) else ( exit /b 0 )

:build
:: %1 = vcvarsall arch (amd64, x86, arm64)
:: %2 = msbuild platform (x64, Win32, ARM64)
:: %3 = runtime folder name (win-x64, win-x86, win-arm64)
setlocal
if defined VCVARSALL (
    call "!VCVARSALL!" %1 >nul 2>&1
)
msbuild "%VCXPROJ%" /p:Configuration=Release /p:Platform=%2 !TOOLSET_ARG! /p:WindowsTargetPlatformVersion=10.0 /v:minimal
if errorlevel 1 (
    echo ERROR: Build failed for %3
    exit /b 1
)
:: Copy DLL to runtimes
if not exist "%RUNTIMES%\%3\native" mkdir "%RUNTIMES%\%3\native"
copy /y "%SCRIPT_DIR%LiteHtmlLib\bin\Release\%3\LiteHtmlLib.dll" "%RUNTIMES%\%3\native\LiteHtmlLib.dll"
if errorlevel 1 (
    echo ERROR: Failed to copy DLL for %3
    exit /b 1
)
echo %3 OK
endlocal
exit /b 0
