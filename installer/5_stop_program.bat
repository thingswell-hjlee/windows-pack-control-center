@echo off
chcp 65001 >nul 2>&1
title Windows Pack Control Center - 프로그램 종료

echo ======================================
echo  Windows Pack Control Center
echo  프로그램 종료
echo ======================================
echo.

echo [INFO] 실행 중인 프로세스 확인 중...

tasklist /FI "IMAGENAME eq ControlCenter.Gateway.exe" 2>nul | find /I "ControlCenter.Gateway.exe" >nul
if %ERRORLEVEL% neq 0 (
    echo.
    echo [INFO] ControlCenter.Gateway.exe 가 실행되고 있지 않습니다.
    echo [INFO] 종료할 프로세스가 없습니다.
    goto :end
)

echo [INFO] ControlCenter.Gateway.exe 가 실행 중입니다.
echo [INFO] 프로세스를 종료합니다...
echo.

taskkill /F /IM ControlCenter.Gateway.exe >nul 2>&1

if %ERRORLEVEL% equ 0 (
    echo [OK] 프로그램이 성공적으로 종료되었습니다.
) else (
    echo [WARNING] 프로세스 종료에 실패했습니다.
    echo [WARNING] 관리자 권한으로 다시 실행해 보세요.
    echo.
    echo  관리자 권한 실행 방법:
    echo  이 파일을 마우스 오른쪽 버튼으로 클릭 후
    echo  '관리자 권한으로 실행'을 선택하세요.
)

:end
echo.
pause
