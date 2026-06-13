# Windows Pack Control Center 운영 가이드

## 서비스 시작/종료

### 시작

- **Start Menu:** Windows Pack Control Center 바로가기 클릭
- **바탕화면:** 아이콘 더블클릭 (설치 시 선택한 경우)
- **자동 시작:** Windows 로그인 시 자동 실행 (설치 시 선택한 경우)
- **수동 실행:**
  ```powershell
  & "C:\Program Files\Thingswell\WindowsPackControlCenter\ControlCenter.Gateway.exe"
  ```

### 종료

- 작업 관리자에서 `ControlCenter.Gateway.exe` 프로세스 종료
- 또는 PowerShell에서:
  ```powershell
  taskkill /IM ControlCenter.Gateway.exe /F
  ```

## 설정 변경

설정 파일 위치: `C:\ProgramData\Thingswell\WindowsPackControlCenter\appsettings.json`

### 포트 변경

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:9090"
      }
    }
  }
}
```

변경 후 서비스를 재시작해야 합니다.

### MQTT 설정

```json
{
  "Mqtt": {
    "DefaultPort": 1883,
    "ReconnectDelaySeconds": 5,
    "HeartbeatTimeoutSeconds": 60,
    "HeartbeatCheckIntervalSeconds": 15,
    "ChannelCapacity": 10000,
    "KeepAliveSeconds": 30
  }
}
```

| 설정 | 설명 | 기본값 |
|------|------|--------|
| DefaultPort | MQTT 브로커 기본 포트 | 1883 |
| ReconnectDelaySeconds | 연결 끊김 시 재접속 대기 시간 | 5초 |
| HeartbeatTimeoutSeconds | 장치 오프라인 판단 시간 | 60초 |
| HeartbeatCheckIntervalSeconds | 하트비트 점검 주기 | 15초 |
| ChannelCapacity | 이벤트 큐 최대 크기 | 10000 |
| KeepAliveSeconds | MQTT Keep-Alive 주기 | 30초 |

### 카메라 상태 점검 설정

```json
{
  "CameraStatus": {
    "CheckIntervalSeconds": 30,
    "TimeoutSeconds": 3,
    "MaxConcurrentChecks": 5
  }
}
```

## MQTT 장치 설정

### AIBox 장치 등록

1. 대시보드 > 장치 관리 > 장치 추가
2. 필수 입력 항목:
   - **장치 ID** (`device_id`): AIBox 고유 식별자
   - **장치 이름** (`device_name`): 관리용 표시 이름
   - **MQTT 호스트** (`mqtt_host`): AIBox IP 주소
   - **MQTT 포트** (`mqtt_port`): 기본 1883
3. 선택 입력 항목:
   - **사이트 ID/이름**: 설치 현장 정보
   - **위치**: 설치 위치 설명
   - **MQTT 인증**: 사용자명/비밀번호 (필요 시)
   - **이벤트 토픽**: 커스텀 MQTT 토픽 패턴
   - **상태 토픽**: 장치 상태 MQTT 토픽
4. 등록 후 "활성화" 상태로 설정하면 MQTT 연결이 시작됩니다.

### MQTT 연결 정보 변경

장치의 MQTT 연결 정보(호스트, 포트, 인증)를 변경하면 기존 연결이 자동으로 끊기고 새 설정으로 재접속됩니다.

### 장치 비활성화/활성화

- **비활성화**: MQTT 연결을 끊고 상태를 "disabled"로 변경
- **활성화**: MQTT 연결을 시작하고 하트비트 모니터링 시작

## 카메라 등록

### 카메라 추가

1. 대시보드 > 카메라 관리 > 카메라 추가
2. 필수 입력:
   - **카메라 ID** (`camera_id`): 고유 식별자
   - **카메라 이름** (`camera_name`): 표시 이름
   - **연결 장치** (`device_id`): 드롭다운에서 선택
3. 선택 입력:
   - **RTSP URL**: 영상 스트림 주소
   - **IP 주소**: 카메라 네트워크 주소
   - **위치**: 설치 위치
   - **모델**: 카메라 모델명

### 카메라 상태 점검 우선순위

카메라 상태는 다음 순서로 확인됩니다 (Waterfall):
1. **AIBox 이벤트** - 최근 이벤트에 카메라 ID가 포함되면 온라인
2. **RTSP TCP 연결** - RTSP URL로 TCP 소켓 연결 시도
3. **ICMP Ping** - IP 주소로 Ping 테스트
4. **ONVIF 쿼리** - ONVIF 프로토콜로 상태 확인

## 이벤트 모니터링

### 실시간 이벤트 대시보드

- 대시보드 메인 페이지에서 최근 이벤트를 실시간으로 확인
- 심각도별 이벤트 수 표시 (HIGH/MEDIUM/LOW)
- 온라인/오프라인 장치 수 표시

### 이벤트 필터링

이벤트 목록에서 다음 기준으로 필터링 가능:
- 장치별
- 카메라별
- 이벤트 유형별
- 심각도별 (HIGH, MEDIUM, LOW)
- 확인 상태별 (미확인/확인)
- 날짜 범위

### 이벤트 확인(Acknowledge)

1. 이벤트 상세 페이지에서 "확인" 버튼 클릭
2. 확인자 이름 입력
3. 확인 후 상태가 "confirmed"로 변경

### 조치 메모

이벤트에 대한 조치 내용을 메모로 기록:
1. 이벤트 상세 페이지에서 "조치 메모" 섹션
2. 내용 입력 후 "저장" 클릭

## 내보내기 및 보고

### CSV 내보내기

1. 이벤트 목록 페이지에서 원하는 필터 적용
2. "CSV 내보내기" 버튼 클릭
3. 파일이 자동으로 다운로드됨

### Excel 내보내기

1. 이벤트 목록 페이지에서 원하는 필터 적용
2. "Excel 내보내기" 버튼 클릭
3. `.xlsx` 파일이 다운로드됨

내보내기에 포함되는 컬럼:
- 이벤트 ID, 유형, 심각도, 타임스탬프
- 장치 ID/이름, 카메라 ID/이름
- 신뢰도, 확인 상태, 확인자, 확인 시간
- 조치 메모

## 문제 해결

### 일반적인 문제

| 증상 | 원인 | 해결 방법 |
|------|------|-----------|
| 대시보드 접속 불가 | 서비스 미실행 | 작업관리자에서 프로세스 확인, 재시작 |
| "포트 사용 중" 오류 | 포트 충돌 | 다른 프로세스 종료 또는 포트 변경 |
| 장치 "오프라인" 표시 | MQTT 연결 끊김 | 네트워크, AIBox 전원, MQTT 설정 확인 |
| 카메라 "오프라인" 표시 | 카메라 연결 끊김 | 카메라 전원, 네트워크, RTSP URL 확인 |
| 이벤트 수신 안됨 | MQTT 토픽 불일치 | 장치의 이벤트 토픽 설정 확인 |

### MQTT 연결 문제

1. AIBox가 네트워크에 연결되어 있는지 확인
2. AIBox의 MQTT 브로커가 실행 중인지 확인
3. 방화벽에서 MQTT 포트(기본 1883)가 허용되는지 확인
4. MQTT 인증 정보가 올바른지 확인

### 카메라 연결 문제

1. 카메라 전원 및 네트워크 연결 확인
2. RTSP URL이 올바른지 확인 (VLC 등으로 테스트)
3. 카메라 IP 주소에 Ping 가능한지 확인

## 로그 파일 위치

```
C:\ProgramData\Thingswell\WindowsPackControlCenter\logs\
├── gateway-20240101.log
├── gateway-20240102.log
└── ...
```

- 일별 자동 롤링 (31일간 보관)
- 로그 레벨: Information, Warning, Error, Fatal

### 로그에서 문제 확인

```powershell
# 최근 에러 확인
Select-String -Path "C:\ProgramData\Thingswell\WindowsPackControlCenter\logs\gateway-*.log" -Pattern "Error|Fatal" | Select-Object -Last 20
```

## 데이터베이스 백업

SQLite 데이터베이스 파일 위치:
```
C:\ProgramData\Thingswell\WindowsPackControlCenter\controlcenter.db
```

### 수동 백업

```powershell
# 서비스 종료 후 백업
taskkill /IM ControlCenter.Gateway.exe /F
Copy-Item "C:\ProgramData\Thingswell\WindowsPackControlCenter\controlcenter.db" "C:\Backup\controlcenter_backup_$(Get-Date -Format 'yyyyMMdd').db"
# 서비스 재시작
& "C:\Program Files\Thingswell\WindowsPackControlCenter\ControlCenter.Gateway.exe"
```

### 자동 백업 (작업 스케줄러)

```powershell
# backup-db.ps1
$src = "C:\ProgramData\Thingswell\WindowsPackControlCenter\controlcenter.db"
$dst = "C:\Backup\controlcenter_backup_$(Get-Date -Format 'yyyyMMdd').db"
$backupDir = "C:\Backup"

if (-not (Test-Path $backupDir)) { New-Item -ItemType Directory -Path $backupDir }
Copy-Item $src $dst -Force

# 30일 이상 된 백업 삭제
Get-ChildItem $backupDir -Filter "controlcenter_backup_*.db" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
    Remove-Item
```

Windows 작업 스케줄러에 매일 실행되도록 등록하면 자동 백업이 가능합니다.
