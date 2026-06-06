# 설치 가이드

## 시스템 요구사항

| 항목 | 최소 요구사항 |
|------|---------------|
| 운영체제 | Windows 10 (64bit) 이상 |
| RAM | 4GB 이상 |
| 디스크 | 500MB 이상 여유 공간 |
| 네트워크 | AIBox 장비와 같은 네트워크 |
| 브라우저 | Chrome, Edge, 또는 Firefox (최신 버전) |

> **참고:** Self-contained 배포를 사용하므로 .NET Runtime을 별도로 설치할 필요가 없습니다.

## 설치 절차

### 1단계: 배포 파일 복사

빌드된 `dist/` 폴더의 내용을 설치할 경로에 복사합니다.

```
추천 경로: C:\ControlCenter\
```

복사 후 폴더 구조:
```
C:\ControlCenter\
├── ControlCenter.Gateway.exe    # 실행 파일
├── wwwroot\                     # 웹 대시보드 파일
├── appsettings.json             # 설정 파일
└── (기타 DLL 파일들)
```

### 2단계: 설정 파일 확인

`appsettings.json` 파일을 열어 필요한 설정을 확인합니다:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=controlcenter.db"
  },
  "Mqtt": {
    "DefaultPort": 1883,
    "ReconnectDelaySeconds": 5,
    "HeartbeatTimeoutSeconds": 60
  }
}
```

- `DefaultConnection`: SQLite 데이터베이스 파일 경로 (기본값: 실행 파일과 같은 경로)
- `DefaultPort`: MQTT 기본 포트 (AIBox 장비의 MQTT 포트와 일치해야 함)
- `HeartbeatTimeoutSeconds`: 장비 오프라인 판정 시간 (초)

### 3단계: 방화벽 설정

Windows 방화벽에서 다음 포트를 허용해야 합니다:

| 포트 | 방향 | 용도 |
|------|------|------|
| 8088 | 인바운드 | 웹 대시보드 접근 (같은 네트워크 PC에서 접속 시) |
| 1883 | 아웃바운드 | MQTT 브로커 연결 (AIBox 장비 연결) |

#### 방화벽 규칙 추가 (PowerShell 관리자 권한)

```powershell
# 웹 대시보드 포트 허용
New-NetFirewallRule -DisplayName "Control Center Web" -Direction Inbound -Port 8088 -Protocol TCP -Action Allow

# MQTT 아웃바운드 (보통 기본 허용이지만, 차단된 경우)
New-NetFirewallRule -DisplayName "Control Center MQTT" -Direction Outbound -Port 1883 -Protocol TCP -Action Allow
```

### 4단계: 최초 실행

1. `ControlCenter.Gateway.exe`를 더블 클릭하거나 명령 프롬프트에서 실행합니다:
   ```cmd
   cd C:\ControlCenter
   ControlCenter.Gateway.exe
   ```

2. 콘솔에 다음과 같은 메시지가 표시되면 정상 시작입니다:
   ```
   [INF] Starting Control Center Gateway
   [INF] Now listening on: http://0.0.0.0:8088
   ```

3. 브라우저에서 http://localhost:8088 을 열어 대시보드에 접속합니다.

4. 최초 실행 시 `controlcenter.db` 파일이 자동으로 생성됩니다.

### 5단계: AIBox 장비 등록

1. 대시보드의 "장비 관리" 메뉴로 이동합니다.
2. "장비 추가" 버튼을 클릭합니다.
3. AIBox 장비 정보를 입력합니다:
   - 장비 ID: AIBox 장비의 고유 ID
   - 장비명: 식별하기 쉬운 이름
   - MQTT 호스트: AIBox 장비의 IP 주소
   - MQTT 포트: 기본 1883
4. "저장" 버튼을 클릭하면 자동으로 MQTT 연결이 시작됩니다.

## Windows 서비스로 등록 (선택사항)

시스템 시작 시 자동으로 실행되도록 Windows 서비스로 등록할 수 있습니다:

```cmd
sc create ControlCenterGateway binPath= "C:\ControlCenter\ControlCenter.Gateway.exe" start= auto
sc start ControlCenterGateway
```

서비스 해제:
```cmd
sc stop ControlCenterGateway
sc delete ControlCenterGateway
```

## 업데이트

1. 실행 중인 서비스를 중지합니다.
2. 기존 파일을 새 버전으로 교체합니다. (`controlcenter.db`와 `appsettings.json`은 유지)
3. 서비스를 다시 시작합니다.

> **주의:** `controlcenter.db` 파일을 삭제하면 모든 데이터가 초기화됩니다. 업데이트 전에 백업을 권장합니다.

## 제거

1. 서비스로 등록한 경우 서비스를 먼저 삭제합니다.
2. 설치 폴더(`C:\ControlCenter\`)를 삭제합니다.
3. 방화벽 규칙을 제거합니다:
   ```powershell
   Remove-NetFirewallRule -DisplayName "Control Center Web"
   Remove-NetFirewallRule -DisplayName "Control Center MQTT"
   ```
