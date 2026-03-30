@echo off
setlocal enabledelayedexpansion

:: Build Windows LiteHtmlLib DLLs (x64, x86, ARM64) and copy to runtimes/
::
:: Requires:
::   - Visual Studio Build Tools with "Desktop development with C++" workload
::   - Windows SDK
::   - For ARM64: the ARM64 build tools component
::     (VS Installer > Individual Components > "MSVC ARM64 build tools")

set SCRIPT_DIR=%~dp0
set VCXPROJ=%SCRIPT_DIR%LiteHtmlLib\LiteHtmlLib.vcxproj
set RUNTIMES=%SCRIPT_DIR%runtimes
set BUILT=0
set FAILED=0
set SKIPPED=0
set "PFX86=%ProgramFiles(x86)%"

:: Find VS installation via vswhere
set "VSWHERE=!PFX86!\Microsoft Visual Studio\Installer\vswhere.exe"
set VS_PATH=
if exist "!VSWHERE!" (
    for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2^>nul`) do (
        set "VS_PATH=%%i"
    )
)

:: Fallback: search common VS install locations
if "!VS_PATH!"=="" (
    call :find_vs
)
if "!VS_PATH!"=="" (
    echo ERROR: Cannot find Visual Studio. Install Visual Studio Build Tools with C++ workload.
    exit /b 1
)

set "VCVARSALL=!VS_PATH!\VC\Auxiliary\Build\vcvarsall.bat"
if not exist "!VCVARSALL!" (
    echo ERROR: vcvarsall.bat not found at !VCVARSALL!
    exit /b 1
)
echo Using VS: !VS_PATH!

:: Detect PlatformToolset from installed MSVC tools version
set TOOLSET=
set "MSVC_TOOLS_DIR=!VS_PATH!\VC\Tools\MSVC"
if exist "!MSVC_TOOLS_DIR!" (
    for /f "tokens=*" %%d in ('dir /b /ad /o-n "!MSVC_TOOLS_DIR!" 2^>nul') do (
        if "!TOOLSET!"=="" (
            for /f "tokens=1,2 delims=." %%a in ("%%d") do (
                set "MINOR=%%b"
                set "TOOLSET=v%%a!MINOR:~0,1!"
            )
        )
    )
)
if "!TOOLSET!"=="" (
    echo WARNING: Could not detect PlatformToolset, using vcxproj defaults.
    set "TOOLSET_ARG="
) else (
    echo Detected PlatformToolset: !TOOLSET!
    set "TOOLSET_ARG=/p:PlatformToolset=!TOOLSET!"
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

:: Check which platforms are available in MSBuild targets
:: Find the latest VC targets version
set "VC_TARGETS_DIR=!VS_PATH!\MSBuild\Microsoft\VC"
set VC_LATEST=
if exist "!VC_TARGETS_DIR!" (
    for /f "tokens=*" %%d in ('dir /b /ad /o-n "!VC_TARGETS_DIR!" 2^>nul') do (
        if "!VC_LATEST!"=="" set "VC_LATEST=%%d"
    )
)

:: Build x64
echo ============================================================
echo Building win-x64...
echo ============================================================
call :build amd64 x64 win-x64
if !ERRORLEVEL!==2 ( set /a SKIPPED+=1 ) else if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )

:: Build x86
echo ============================================================
echo Building win-x86...
echo ============================================================
call :build x86 Win32 win-x86
if !ERRORLEVEL!==2 ( set /a SKIPPED+=1 ) else if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )

:: Build ARM64 (check if platform is available)
echo ============================================================
echo Building win-arm64...
echo ============================================================
set ARM64_AVAILABLE=0
if defined VC_LATEST (
    if exist "!VC_TARGETS_DIR!\!VC_LATEST!\Platforms\ARM64" set ARM64_AVAILABLE=1
)
if !ARM64_AVAILABLE!==0 (
    echo SKIPPED: ARM64 platform not available in MSBuild targets.
    echo   Install "MSVC ARM64 build tools" via VS Installer to enable.
    set /a SKIPPED+=1
) else (
    call :build arm64 ARM64 win-arm64
    if !ERRORLEVEL!==2 ( set /a SKIPPED+=1 ) else if !ERRORLEVEL!==1 ( set /a FAILED+=1 ) else ( set /a BUILT+=1 )
)

echo.
echo ============================================================
echo Results: !BUILT! built, !FAILED! failed, !SKIPPED! skipped
echo ============================================================
if !BUILT! GTR 0 (
    echo.
    echo Updated:
    for %%a in (win-x64 win-x86 win-arm64) do (
        if exist "!RUNTIMES!\%%a\native\LiteHtmlLib.dll" echo   runtimes\%%a\native\LiteHtmlLib.dll
    )
)
if !FAILED! GTR 0 ( exit /b 1 ) else ( exit /b 0 )

:find_vs
for %%v in (18 17) do (
    for %%e in (BuildTools Community Professional Enterprise) do (
        if exist "!PFX86!\Microsoft Visual Studio\%%v\%%e\VC\Auxiliary\Build\vcvarsall.bat" (
            set "VS_PATH=!PFX86!\Microsoft Visual Studio\%%v\%%e"
            exit /b 0
        )
    )
)
for %%y in (2022 2019) do (
    for %%e in (BuildTools Community Professional Enterprise) do (
        if exist "%ProgramFiles%\Microsoft Visual Studio\%%y\%%e\VC\Auxiliary\Build\vcvarsall.bat" (
            set "VS_PATH=%ProgramFiles%\Microsoft Visual Studio\%%y\%%e"
            exit /b 0
        )
        if exist "!PFX86!\Microsoft Visual Studio\%%y\%%e\VC\Auxiliary\Build\vcvarsall.bat" (
            set "VS_PATH=!PFX86!\Microsoft Visual Studio\%%y\%%e"
            exit /b 0
        )
    )
)
exit /b 1

:build
:: %1 = vcvarsall arch (amd64, x86, arm64)
:: %2 = msbuild platform (x64, Win32, ARM64)
:: %3 = runtime folder name (win-x64, win-x86, win-arm64)
setlocal
call "!VCVARSALL!" %1 >nul 2>&1
if errorlevel 1 (
    echo ERROR: vcvarsall.bat failed for %1
    exit /b 1
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
