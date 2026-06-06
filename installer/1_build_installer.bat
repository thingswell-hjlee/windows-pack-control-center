@echo off
chcp 65001 >nul 2>&1
title Windows Pack Control Center - 설치파일 빌드

echo ======================================
echo  Windows Pack Control Center
echo  설치파일 빌드
echo ======================================
echo.

REM 프로젝트 루트 계산 (installer/ 상위 디렉토리)
set "PROJECT_ROOT=%~dp0.."
set "INSTALLER_DIR=%~dp0"
set "BUILD_SCRIPT=%INSTALLER_DIR%build-installer.ps1"
set "OUTPUT_FILE=%PROJECT_ROOT%\dist\installer\WindowsPackControlCenter_Setup_1.0.0.exe"

echo [INFO] 프로젝트 루트: %PROJECT_ROOT%
echo [INFO] 빌드 스크립트: %BUILD_SCRIPT%
echo.

REM build-installer.ps1 존재 확인
if not exist "%BUILD_SCRIPT%" (
    echo [ERROR] 빌드 스크립트를 찾을 수 없습니다: %BUILD_SCRIPT%
    echo [ERROR] installer 폴더에서 실행해 주세요.
    goto :error
)

echo [1/2] PowerShell 빌드 스크립트 실행 중...
echo.
powershell -ExecutionPolicy Bypass -File "%BUILD_SCRIPT%"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] 빌드에 실패했습니다. 위의 오류 메시지를 확인해 주세요.
    goto :error
)

echo.
echo [2/2] 설치파일 확인 중...

if exist "%OUTPUT_FILE%" (
    echo.
    echo ======================================
    echo  빌드 성공!
    echo ======================================
    echo.
    echo  설치파일 위치:
    echo  %OUTPUT_FILE%
    echo.
) else (
    echo.
    echo [WARNING] 설치파일이 생성되지 않았습니다.
    echo [WARNING] Inno Setup 6.x가 설치되어 있는지 확인해 주세요.
    echo [WARNING] 다운로드: https://jrsoftware.org/isdl.php
    echo.
    echo  .NET publish는 성공했을 수 있습니다.
    echo  dist\publish\ 폴더를 확인해 주세요.
)

goto :end

:error
echo.
echo ======================================
echo  빌드 실패
echo ======================================
echo.

:end
echo.
pause
