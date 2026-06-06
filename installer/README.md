# Windows Pack Control Center - Installer

Windows Pack Control Center를 위한 Inno Setup 기반 Windows 설치 패키지입니다.

## Prerequisites (빌드 환경 요구사항)

| Tool | Version | Download |
|------|---------|----------|
| .NET 8 SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org/ |
| Inno Setup | 6.x | https://jrsoftware.org/isdl.php |

## How to Build (빌드 방법)

### PowerShell (권장)

```powershell
# 프로젝트 루트에서 실행
.\installer\build-installer.ps1
```

### Custom Options

```powershell
.\installer\build-installer.ps1 -Configuration Release -Runtime win-x64 -InnoSetupPath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
```

### Build Output

빌드 완료 후 생성되는 파일:

- `dist/publish/` - 자체 포함(self-contained) 단일 실행 파일
- `dist/installer/WindowsPackControlCenter_Setup_1.0.0.exe` - 설치 프로그램

## Directory Structure After Installation (설치 후 디렉토리 구조)

### Program Files (프로그램 파일)

```
C:\Program Files\Thingswell\WindowsPackControlCenter\
├── ControlCenter.Gateway.exe    # Main executable
├── wwwroot/                     # Embedded React dashboard
└── (runtime dependencies)
```

### Application Data (애플리케이션 데이터)

```
C:\ProgramData\Thingswell\WindowsPackControlCenter\
├── appsettings.json             # Configuration overrides
├── controlcenter.db             # SQLite database
├── logs/                        # Application logs
│   └── gateway-YYYYMMDD.log
└── data/                        # Additional data files
```

## Configuration (설정)

설정 파일 위치: `C:\ProgramData\Thingswell\WindowsPackControlCenter\appsettings.json`

주요 설정:

| Setting | Default | Description |
|---------|---------|-------------|
| Kestrel:Endpoints:Http:Url | http://0.0.0.0:8088 | HTTP 서버 포트 |
| Mqtt:DefaultPort | 1883 | MQTT 기본 포트 |
| Mqtt:HeartbeatTimeoutSeconds | 60 | 장치 오프라인 판단 시간 |
| CameraStatus:CheckIntervalSeconds | 30 | 카메라 상태 점검 주기 |

## Log Files (로그 파일)

로그 파일 위치: `C:\ProgramData\Thingswell\WindowsPackControlCenter\logs\`

- 일별 롤링 로그 (최대 31일 보관)
- 파일명 패턴: `gateway-YYYYMMDD.log`

## Starting and Stopping (시작/종료)

### 시작

- **Start Menu:** Windows Pack Control Center 바로가기 클릭
- **Desktop:** 바탕화면 아이콘 (설치 시 선택한 경우)
- **Auto-start:** Windows 로그인 시 자동 실행 (설치 시 선택한 경우)

### 종료

- 시스템 트레이에서 종료하거나
- 작업 관리자에서 `ControlCenter.Gateway.exe` 프로세스 종료

### 대시보드 접속

브라우저에서 http://localhost:8088 접속

## Uninstallation (제거)

1. Windows 설정 > 앱 > Windows Pack Control Center > 제거
2. 또는: Start Menu > Windows Pack Control Center > 제거

제거 시 사용자 데이터 삭제 여부를 선택할 수 있습니다:
- **예:** 데이터베이스, 로그, 설정 파일 모두 삭제
- **아니오:** ProgramData 폴더의 데이터 보존 (재설치 시 기존 데이터 유지)

## Firewall (방화벽)

설치 시 Windows 방화벽에 TCP 포트 8088 인바운드 규칙이 자동 추가됩니다.
제거 시 해당 규칙이 자동 삭제됩니다.

## Troubleshooting (문제 해결)

### 포트 충돌

포트 8088이 이미 사용 중인 경우:
1. `appsettings.json`에서 포트 변경
2. 또는 충돌하는 프로세스 종료

### 설치 프로그램 빌드 실패

1. Inno Setup 6.x가 설치되어 있는지 확인
2. .NET 8 SDK가 설치되어 있는지 확인
3. Node.js 18+가 설치되어 있는지 확인
4. `npm install` 실행 시 네트워크 연결 확인
