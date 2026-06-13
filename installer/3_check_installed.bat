@echo off
chcp 65001 >nul 2>&1
title Windows Pack Control Center - 설치 확인

echo ======================================
echo  Windows Pack Control Center
echo  설치 상태 확인
echo ======================================
echo.

set PASS_COUNT=0
set FAIL_COUNT=0

REM 1. 프로그램 폴더 확인
echo [검사 1/6] 프로그램 폴더 확인...
if exist "C:\Program Files\Thingswell\WindowsPackControlCenter" (
    echo   [PASS] C:\Program Files\Thingswell\WindowsPackControlCenter
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] C:\Program Files\Thingswell\WindowsPackControlCenter 없음
    set /a FAIL_COUNT+=1
)

REM 2. 실행 파일 확인
echo [검사 2/6] 실행 파일 확인...
if exist "C:\Program Files\Thingswell\WindowsPackControlCenter\ControlCenter.Gateway.exe" (
    echo   [PASS] ControlCenter.Gateway.exe 존재
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] ControlCenter.Gateway.exe 없음
    set /a FAIL_COUNT+=1
)

REM 3. 데이터 폴더 확인
echo [검사 3/6] 데이터 폴더 확인...
if exist "C:\ProgramData\Thingswell\WindowsPackControlCenter" (
    echo   [PASS] C:\ProgramData\Thingswell\WindowsPackControlCenter
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] C:\ProgramData\Thingswell\WindowsPackControlCenter 없음
    set /a FAIL_COUNT+=1
)

REM 4. 데이터베이스 파일 확인
echo [검사 4/6] 데이터베이스 파일 확인...
if exist "C:\ProgramData\Thingswell\WindowsPackControlCenter\controlcenter.db" (
    echo   [PASS] controlcenter.db 존재
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] controlcenter.db 없음 (프로그램을 한 번 실행해야 생성됩니다)
    set /a FAIL_COUNT+=1
)

REM 5. 로그 폴더 확인
echo [검사 5/6] 로그 폴더 확인...
if exist "C:\ProgramData\Thingswell\WindowsPackControlCenter\logs" (
    echo   [PASS] logs 폴더 존재
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] logs 폴더 없음
    set /a FAIL_COUNT+=1
)

REM 6. 프로세스 실행 확인
echo [검사 6/6] 프로세스 실행 확인...
tasklist /FI "IMAGENAME eq ControlCenter.Gateway.exe" 2>nul | find /I "ControlCenter.Gateway.exe" >nul
if %ERRORLEVEL% equ 0 (
    echo   [PASS] ControlCenter.Gateway.exe 실행 중
    set /a PASS_COUNT+=1
) else (
    echo   [FAIL] ControlCenter.Gateway.exe 실행되지 않음
    set /a FAIL_COUNT+=1
)

echo.
echo ======================================
echo  검사 결과: %PASS_COUNT% PASS / %FAIL_COUNT% FAIL
echo ======================================
echo.

if %FAIL_COUNT% equ 0 (
    echo  [SUCCESS] 모든 항목이 정상입니다!
    echo.
    echo  대시보드 접속: http://localhost:8088
    echo  (4_open_dashboard.bat 을 실행하면 자동으로 열립니다)
) else (
    echo  [WARNING] 일부 항목이 확인되지 않았습니다.
    echo.
    echo  해결 방법:
    echo  - 설치가 안 된 경우: 2_run_installer.bat 실행
    echo  - DB/로그 없는 경우: 프로그램을 한 번 실행한 후 재확인
    echo  - 프로세스 미실행: 바탕화면 아이콘으로 프로그램 시작
)

echo.
pause
