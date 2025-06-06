@echo OFF
setlocal EnableDelayedExpansion

REM --- Configuration ---
set NODEJS_VERSION=22.16.0
set NODEJS_ARCH=x64
set PLAYWRIGHT_DIR_NAME=Playwright

REM === Validate Parent Directory from Command Line Argument ===
if "%~1"=="" (
    echo ERROR: Parent directory not provided.
    echo Usage: %~nx0 ^<ParentNodeDirectory^>
    echo Example: %~nx0 "C:\MyNodeJSInstallations"
    exit /b 1
)

set "PARENT_NODE_DIR=%~1"

REM Check if the provided parent directory exists
if not exist "!PARENT_NODE_DIR!\" (
    echo ERROR: The specified parent directory does not exist:
    echo        "!PARENT_NODE_DIR!"
    echo This directory should contain your Node.js installation folder.
    exit /b 1
)

REM --- Construct Paths ---
set "NODEJS_FOLDER=node-v%NODEJS_VERSION%-win-%NODEJS_ARCH%"
set "NODE_BIN_PATH=%PARENT_NODE_DIR%\%NODEJS_FOLDER%"
set "PLAYWRIGHT_PATH=%PARENT_NODE_DIR%\%PLAYWRIGHT_DIR_NAME%"

REM Check if the Playwright directory exists
if not exist "!PLAYWRIGHT_PATH!\" (
    echo ERROR: Playwright directory does not exist:
    echo        "!PLAYWRIGHT_PATH!"
    echo Please run the Playwright setup script first.
    exit /b 1
)

REM Check if npx.cmd exists
if not exist "!NODE_BIN_PATH!\npx.cmd" (
    echo ERROR: Could not find npx.cmd at:
    echo        "!NODE_BIN_PATH!\npx.cmd"
    echo Ensure Node.js version '%NODEJS_VERSION%-%NODEJS_ARCH%' is installed in "!PARENT_NODE_DIR!".
    exit /b 1
)

echo ========================================================================
echo Installing Playwright
echo ========================================================================
echo Node.js Path: "!NODE_BIN_PATH!"
echo Playwright Path: "!PLAYWRIGHT_PATH!"
echo ========================================================================

REM --- Change to Project Directory and Run Commands ---
echo Changing directory to: "!PLAYWRIGHT_PATH!"
pushd "!PLAYWRIGHT_PATH!"
if !errorlevel! neq 0 (
    echo ERROR: Could not change directory to "!PLAYWRIGHT_PATH!".
    exit /b 1
)

echo.
echo Installing Playwright browsers and system dependencies...
echo This may take several minutes depending on your internet connection.
echo Administrative privileges may be required for system dependencies.
echo.

REM Set PATH temporarily to include the Node.js directory first
set "PATH=!NODE_BIN_PATH!;%PATH%"

REM Run npx playwright install
call "!NODE_BIN_PATH!\npx.cmd" playwright install --with-deps --no-shell
if !errorlevel! neq 0 (
    echo ERROR: "npx playwright install --with-deps --no-shell" failed.
    echo This may be due to insufficient permissions for installing system dependencies.
    echo Try running this script as Administrator if system dependencies are required.
    popd
    exit /b 1
)

echo.
echo Installation complete!
echo ========================================================================
echo Playwright browsers and system dependencies have been successfully installed.
echo ========================================================================

popd
exit /b 0