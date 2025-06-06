@echo off
setlocal EnableDelayedExpansion

REM --- Configuration ---
set NODEJS_VERSION=22.16.0
set NODEJS_ARCH=x64

REM === Validate and Get Parent Extraction Directory from Command Line Argument ===
if "%~1"=="" (
    echo ERROR: Parent extraction directory not provided.
    echo Usage: %~nx0 ^<ParentExtractionDirectory^>
    echo Example: %~nx0 "C:\MyNodeJSInstallations"
    exit /b 1
)
set "NODEJS_EXTRACT_PARENT_DIR=%~1"

REM Check if the provided parent directory exists
if not exist "!NODEJS_EXTRACT_PARENT_DIR!\" (
    echo ERROR: The specified parent extraction directory does not exist:
    echo        "!NODEJS_EXTRACT_PARENT_DIR!"
    echo Please create this directory first or provide an existing one.
    exit /b 1
)
echo Parent extraction directory: "!NODEJS_EXTRACT_PARENT_DIR!"
echo.

set NODEJS_DIR_NAME=node-v%NODEJS_VERSION%-win-%NODEJS_ARCH%
set NODEJS_FILENAME=%NODEJS_DIR_NAME%.zip
set NODEJS_URL=https://nodejs.org/dist/v%NODEJS_VERSION%/%NODEJS_FILENAME%

REM Using %TEMP% for downloads is generally safer and cleaner
set DOWNLOAD_DIR=%TEMP%\nodejs_download_%RANDOM%_%RANDOM%
set DOWNLOAD_PATH=!DOWNLOAD_DIR!\%NODEJS_FILENAME%

REM This will be the full path to the extracted folder
set FINAL_NODE_EXTRACTED_PATH=!NODEJS_EXTRACT_PARENT_DIR!\%NODEJS_DIR_NAME%

echo ========================================================================
echo Node.js Standalone Download ^& Extract
echo ========================================================================
echo Version:                     %NODEJS_VERSION%
echo Architecture:                %NODEJS_ARCH%
echo Download URL:                %NODEJS_URL%
echo Download to:                 "!DOWNLOAD_PATH!"
echo Node.js will be extracted to: "!FINAL_NODE_EXTRACTED_PATH!"
echo ========================================================================
echo.

REM Create download directory (temporary)
if not exist "!DOWNLOAD_DIR!" mkdir "!DOWNLOAD_DIR!"
if !errorlevel! neq 0 (
    echo ERROR: Could not create temporary download directory: "!DOWNLOAD_DIR!". Check permissions.
    exit /b 1
)

REM PowerShell command to download and extract with improved error handling and retry logic
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop'; ^
    $downloadUrl = '%NODEJS_URL%'; ^
    $downloadPath = '%DOWNLOAD_PATH%'; ^
    $extractDestinationParent = '%NODEJS_EXTRACT_PARENT_DIR%'; ^
    $finalExtractedPath = '%FINAL_NODE_EXTRACTED_PATH%'; ^
    $maxRetries = 3; ^
    $retryCount = 0; ^
    $downloaded = $false; ^
    ^
    Write-Host \"Download starting...\" -ForegroundColor Cyan; ^
    ^
    while (-not $downloaded -and $retryCount -lt $maxRetries) { ^
        try { ^
            if ($retryCount -gt 0) { ^
                Write-Host \"Retry attempt $retryCount of $maxRetries...\" -ForegroundColor Yellow; ^
                Start-Sleep -Seconds ($retryCount * 2); ^
            } ^
            ^
            $wc = New-Object Net.WebClient; ^
            $wc.Headers.Add('User-Agent', 'Node.js Installer Script'); ^
            Write-Host \"Downloading Node.js from $downloadUrl to $downloadPath...\"; ^
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew(); ^
            $wc.DownloadFile($downloadUrl, $downloadPath); ^
            $stopwatch.Stop(); ^
            Write-Host \"Download complete in $($stopwatch.Elapsed.TotalSeconds) seconds.\" -ForegroundColor Green; ^
            $downloaded = $true; ^
        } ^
        catch { ^
            $retryCount++; ^
            if ($retryCount -ge $maxRetries) { ^
                Write-Host \"ERROR: Download failed after $maxRetries attempts: $($_.Exception.Message)\" -ForegroundColor Red; ^
                exit 1; ^
            } ^
        } ^
    } ^
    ^
    if (Test-Path $downloadPath) { ^
        $fileSize = (Get-Item $downloadPath).Length / 1MB; ^
        Write-Host \"Downloaded file size: $([Math]::Round($fileSize, 2)) MB\" -ForegroundColor Cyan; ^
        ^
        try { ^
            Write-Host \"Extracting $downloadPath to parent directory $extractDestinationParent...\" -ForegroundColor Cyan; ^
            if (Test-Path $finalExtractedPath) { ^
                Write-Host \"Warning: Destination directory already exists. Files will be overwritten.\" -ForegroundColor Yellow; ^
            } ^
            Expand-Archive -Path $downloadPath -DestinationPath $extractDestinationParent -Force; ^
            Write-Host \"Extraction complete.\" -ForegroundColor Green; ^
            ^
            if (Test-Path \"$finalExtractedPath\\node.exe\") { ^
                $nodeVersion = & \"$finalExtractedPath\\node.exe\" -v; ^
                Write-Host \"Verified Node.js version: $nodeVersion\" -ForegroundColor Green; ^
            } else { ^
                Write-Host \"Warning: node.exe not found in extracted directory.\" -ForegroundColor Yellow; ^
            } ^
        } ^
        catch { ^
            Write-Host \"ERROR: Extraction failed: $($_.Exception.Message)\" -ForegroundColor Red; ^
            exit 1; ^
        } ^
    } else { ^
        Write-Host \"ERROR: Download appears to have failed. File not found.\" -ForegroundColor Red; ^
        exit 1; ^
    }"

if !errorlevel! neq 0 (
    echo.
    echo ERROR: Node.js download/extraction failed. See PowerShell messages above.
    goto :cleanup_with_error
)

echo.
echo ========================================================================
echo Node.js download and extraction successful!
echo ========================================================================
echo The Node.js ZIP file '%NODEJS_FILENAME%' has been extracted.
echo A folder named '%NODEJS_DIR_NAME%' has been created inside '!NODEJS_EXTRACT_PARENT_DIR!'.
echo.
echo Node.js binaries are located in:
echo   "!FINAL_NODE_EXTRACTED_PATH!"
echo.
echo To run Node.js from this location, you can use:
echo   "!FINAL_NODE_EXTRACTED_PATH!\node.exe" -v
echo   "!FINAL_NODE_EXTRACTED_PATH!\npm.cmd" -v
echo ========================================================================

REM Clean up the downloaded ZIP file and temporary directory
:cleanup
if exist "!DOWNLOAD_PATH!" (
    del "!DOWNLOAD_PATH!" >nul 2>&1
)
if exist "!DOWNLOAD_DIR!" (
    rd "!DOWNLOAD_DIR!" /s /q >nul 2>&1
)
exit /b 0

:cleanup_with_error
if exist "!DOWNLOAD_PATH!" (
    del "!DOWNLOAD_PATH!" >nul 2>&1
)
if exist "!DOWNLOAD_DIR!" (
    rd "!DOWNLOAD_DIR!" /s /q >nul 2>&1
)
exit /b 1