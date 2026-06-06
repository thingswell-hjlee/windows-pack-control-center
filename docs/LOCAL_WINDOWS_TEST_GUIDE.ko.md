# Windows 로컬 설치 테스트 가이드

## 개요

이 문서는 Windows PC에서 Windows Pack Control Center를 빌드, 설치, 실행, 테스트하는 방법을 설명합니다.
복잡한 명령어 없이 **더블클릭**으로 모든 과정을 진행할 수 있습니다.

## 사전 요구사항

- Windows 10 x64 이상
- .NET 8 SDK 설치 (https://dotnet.microsoft.com/download)
- Node.js 18+ 설치 (https://nodejs.org/)
- Inno Setup 6.x 설치 (https://jrsoftware.org/isdl.php) — 설치파일 생성 시 필요

## 배치 파일 목록

| 순서 | 파일명 | 역할 |
|------|--------|------|
| 1 | `1_build_installer.bat` | 프론트엔드 빌드 + .NET publish + 설치파일 생성 |
| 2 | `2_run_installer.bat` | 생성된 설치파일 실행 |
| 3 | `3_check_installed.bat` | 설치 상태 확인 (PASS/FAIL) |
| 4 | `4_open_dashboard.bat` | 브라우저에서 대시보드 열기 |
| 5 | `5_stop_program.bat` | 실행 중인 프로그램 종료 |

## 사용 순서

### Step 1: 설치파일 빌드

1. `installer` 폴더로 이동합니다.
2. `1_build_installer.bat`을 더블클릭합니다.
3. 빌드가 완료되면 `dist\installer\` 폴더에 설치파일이 생성됩니다.

> ⚠️ Inno Setup이 설치되지 않은 경우, .NET publish만 완료되고 설치파일은 생성되지 않습니다.
> 이 경우 `dist\publish\ControlCenter.Gateway.exe`를 직접 실행할 수 있습니다.

### Step 2: 설치 실행

1. `2_run_installer.bat`을 더블클릭합니다.
2. UAC 권한 요청이 표시되면 '예'를 클릭합니다.
3. 설치 마법사의 안내를 따릅니다.
4. 바탕화면 아이콘 생성 옵션을 선택합니다.

### Step 3: 설치 확인

1. `3_check_installed.bat`을 더블클릭합니다.
2. 6개 항목의 PASS/FAIL 결과를 확인합니다.
3. 모든 항목이 PASS이면 설치가 정상 완료된 것입니다.

### Step 4: 대시보드 열기

1. `4_open_dashboard.bat`을 더블클릭합니다.
2. 프로그램이 실행되지 않은 경우 자동 시작을 안내합니다.
3. 기본 브라우저에서 http://localhost:8088 이 열립니다.

### Step 5: 프로그램 종료

1. `5_stop_program.bat`을 더블클릭합니다.
2. 실행 중인 프로세스가 종료됩니다.

## 문제 해결

### "빌드에 실패했습니다" 오류
- .NET 8 SDK가 설치되어 있는지 확인: `dotnet --version`
- Node.js가 설치되어 있는지 확인: `node --version`
- npm이 설치되어 있는지 확인: `npm --version`

### "설치파일을 찾을 수 없습니다" 오류
- `1_build_installer.bat`을 먼저 실행해 주세요.
- Inno Setup 6.x가 설치되어 있는지 확인해 주세요.

### "프로세스 종료에 실패했습니다" 오류
- 배치 파일을 마우스 오른쪽 버튼으로 클릭 후 '관리자 권한으로 실행'을 선택하세요.

### 대시보드에 접속할 수 없는 경우
- 프로그램이 실행 중인지 `3_check_installed.bat`으로 확인합니다.
- 포트 8088이 다른 프로그램에 의해 사용되고 있을 수 있습니다.
- Windows 방화벽에서 포트 8088이 허용되어 있는지 확인합니다.

## 디렉토리 구조 (설치 후)

```
C:\Program Files\Thingswell\WindowsPackControlCenter\
  └── ControlCenter.Gateway.exe    (실행 파일)

C:\ProgramData\Thingswell\WindowsPackControlCenter\
  ├── appsettings.json             (설정 파일)
  ├── controlcenter.db             (데이터베이스)
  └── logs\
      └── gateway-20260606.log     (로그 파일)
```

## 삭제 방법

1. Windows 설정 > 앱 > 'Windows Pack Control Center' 찾기 > 제거
2. 또는 시작 메뉴 > 'Windows Pack Control Center 제거' 클릭
3. 사용자 데이터 삭제 여부를 묻는 대화상자에서 선택
