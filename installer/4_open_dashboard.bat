@echo off
chcp 65001 >nul 2>&1
title Windows Pack Control Center - 대시보드 열기

echo ======================================
echo  Windows Pack Control Center
echo  대시보드 열기
echo ======================================
echo.

set "DASHBOARD_URL=http://localhost:8088"

echo [INFO] 대시보드 URL: %DASHBOARD_URL%
echo.

REM 프로세스 실행 여부 확인
tasklist /FI "IMAGENAME eq ControlCenter.Gateway.exe" 2>nul | find /I "ControlCenter.Gateway.exe" >nul
if %ERRORLEVEL% neq 0 (
    echo [WARNING] Control Center가 실행되지 않고 있습니다.
    echo [WARNING] 먼저 프로그램을 시작해 주세요.
    echo.
    echo  바탕화면의 'Windows Pack Control Center' 아이콘을 더블클릭하거나
    echo  시작 메뉴에서 실행할 수 있습니다.
    echo.
    set /p "START_NOW=지금 프로그램을 시작하시겠습니까? (Y/N): "
    if /I "!START_NOW!"=="Y" (
        echo.
        echo [INFO] 프로그램을 시작합니다...
        if exist "C:\Program Files\Thingswell\WindowsPackControlCenter\ControlCenter.Gateway.exe" (
            start "" "C:\Program Files\Thingswell\WindowsPackControlCenter\ControlCenter.Gateway.exe"
            echo [INFO] 3초 대기 중...
            timeout /t 3 /nobreak >nul
        ) else (
            echo [ERROR] 실행 파일을 찾을 수 없습니다. 설치를 먼저 진행해 주세요.
            goto :end
        )
    ) else (
        goto :end
    )
)

echo [INFO] 기본 브라우저에서 대시보드를 엽니다...
start "" "%DASHBOARD_URL%"
echo.
echo [OK] 브라우저가 열렸습니다.
echo     URL: %DASHBOARD_URL%

:end
echo.
pause
