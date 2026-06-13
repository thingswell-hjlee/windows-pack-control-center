@echo off
chcp 65001 >nul 2>&1
title Windows Pack Control Center - 설치 실행

echo ======================================
echo  Windows Pack Control Center
echo  설치 실행
echo ======================================
echo.

set "PROJECT_ROOT=%~dp0.."
set "INSTALLER_FILE=%PROJECT_ROOT%\dist\installer\WindowsPackControlCenter_Setup_1.0.0.exe"

echo [INFO] 설치파일 확인 중...
echo [INFO] 경로: %INSTALLER_FILE%
echo.

if not exist "%INSTALLER_FILE%" (
    echo [ERROR] 설치파일을 찾을 수 없습니다.
    echo.
    echo  먼저 1_build_installer.bat 을 실행하여
    echo  설치파일을 생성해 주세요.
    echo.
    goto :end
)

echo [INFO] 설치파일을 찾았습니다.
echo [INFO] 설치 프로그램을 실행합니다...
echo.
echo  ※ 관리자 권한이 필요할 수 있습니다.
echo  ※ UAC 창이 표시되면 '예'를 클릭해 주세요.
echo.

start "" "%INSTALLER_FILE%"

echo [OK] 설치 프로그램이 실행되었습니다.
echo.

:end
echo.
pause
