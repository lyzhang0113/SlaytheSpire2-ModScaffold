@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Rebuilding and Installing Mod
echo ========================================
echo.

set "DEFAULT_GAME_DIR=C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
set "MOD_SRC_DIR=%~dp0src"

if not "%~1"=="" (
    set "GAME_DIR=%~1"
    echo Game Directory: !GAME_DIR!
) else (
    set "GAME_DIR="
    for /f "delims=" %%L in ('findstr /C:"GameDir" "!MOD_SRC_DIR!\*.csproj" 2^>nul') do (
        set "LINE=%%L"
    )
    if defined LINE (
        set "GAME_DIR=!LINE:*<GameDir>=!"
        set "GAME_DIR=!GAME_DIR:</GameDir>=!"
    )
    if not defined GAME_DIR set "GAME_DIR=!DEFAULT_GAME_DIR!"
    echo Using GameDir from csproj: !GAME_DIR!
)
echo.

if not exist "!GAME_DIR!\data_sts2_windows_x86_64\sts2.dll" (
    echo ERROR: Game not found at !GAME_DIR!
    echo.
    set /p "GAME_DIR=Enter correct game path: "
    if not exist "!GAME_DIR!\data_sts2_windows_x86_64\sts2.dll" (
        echo ERROR: Still not found. Aborting.
        pause
        exit /b 1
    )
)

set "MOD_NAME="
set "CSProjFile="
for %%F in ("!MOD_SRC_DIR!\*.csproj") do (
    set "CSProjFile=%%~nxF"
    set "MOD_NAME=%%~nF"
)

if not defined MOD_NAME (
    echo ERROR: No .csproj found in !MOD_SRC_DIR!
    pause
    exit /b 1
)

set "MANIFEST_SRC="
for %%F in ("!MOD_SRC_DIR!\*.json") do set "MANIFEST_SRC=%%F"

set "TOOLS_DIR=%~dp0"
set "DOTNET_EXE=!TOOLS_DIR!tools\dotnet\dotnet.exe"
set "DOTNET_ROOT=!TOOLS_DIR!tools\dotnet"
set "MOD_SUBFOLDER=!GAME_DIR!\mods\!MOD_NAME!"
set "GODOT_EXE=!TOOLS_DIR!tools\godot\Godot_v4.5.1-stable_mono_win64.exe"
set "PCK_BUILDER_DIR=!TOOLS_DIR!tools\pck_builder"
set "PCK_SCRIPT=!PCK_BUILDER_DIR!\build_pck.gd"

if not exist "!DOTNET_EXE!" (
    echo ERROR: Local dotnet not found at !DOTNET_EXE!
    echo   Run install.bat first to set up the environment.
    pause
    exit /b 1
)

echo [1/4] Building mod [!MOD_NAME!]...
set "PATH=!DOTNET_ROOT!;!PATH!"
"!DOTNET_EXE!" build "!MOD_SRC_DIR!\!CSProjFile!" -c Release --verbosity quiet 2>nul
if errorlevel 1 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)
echo   - Build succeeded

echo [2/4] Creating mod subfolder...
if not exist "!MOD_SUBFOLDER!" mkdir "!MOD_SUBFOLDER!"
echo   - !MOD_SUBFOLDER!

echo [3/4] Copying files and generating manifests...
copy /Y "!MOD_SRC_DIR!\bin\Release\!MOD_NAME!.dll" "!MOD_SUBFOLDER!\" >nul
echo   - DLL copied

if defined MANIFEST_SRC (
    copy /Y "!MANIFEST_SRC!" "!MOD_SUBFOLDER!\!MOD_NAME!.json" >nul
    echo   - Source manifest copied
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
        "$j = Get-Content '!MANIFEST_SRC!' | ConvertFrom-Json; " ^
        "$out = @{ pck_name = if($j.pck_name){$j.pck_name}else{$j.name}; name = $j.name; author = $j.author; description = $j.description; version = $j.version }; " ^
        "$out | ConvertTo-Json | Set-Content '!MOD_SUBFOLDER!\mod_manifest.json' -Encoding UTF8"
    echo   - mod_manifest.json generated
) else (
    echo   - WARNING: No manifest found in !MOD_SRC_DIR!
    echo {> "!MOD_SUBFOLDER!\mod_manifest.json"
    echo   "pck_name": "!MOD_NAME!",>> "!MOD_SUBFOLDER!\mod_manifest.json"
    echo   "name": "!MOD_NAME!",>> "!MOD_SUBFOLDER!\mod_manifest.json"
    echo   "version": "0.1.0">> "!MOD_SUBFOLDER!\mod_manifest.json"
    echo }>> "!MOD_SUBFOLDER!\mod_manifest.json"
)

echo [4/4] Creating .pck file...
if exist "!GODOT_EXE!" (
    if exist "!PCK_SCRIPT!" (
        "!GODOT_EXE!" --headless --path "!PCK_BUILDER_DIR!" --script "!PCK_SCRIPT!" -- "!MOD_SUBFOLDER!\mod_manifest.json" "!MOD_SUBFOLDER!\!MOD_NAME!.pck"
        if exist "!MOD_SUBFOLDER!\!MOD_NAME!.pck" (
            echo   - PCK created successfully
        ) else (
            echo   - WARNING: PCK creation may have failed
        )
    ) else (
        echo   - WARNING: PCK builder script not found at !PCK_SCRIPT!
    )
) else (
    echo   - WARNING: Godot not found at !GODOT_EXE!, skipping PCK
    echo   - Run install.bat first to set up Godot
)

echo.
echo ========================================
echo Done! Mod installed to:
echo   !MOD_SUBFOLDER!
echo ========================================
echo.
if "%~1"=="" pause
