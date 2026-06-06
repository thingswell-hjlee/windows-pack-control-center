# 문제 해결 가이드

## 일반 문제

### 애플리케이션이 시작되지 않음

**증상:** ControlCenter.Gateway.exe 실행 시 바로 종료됨

**해결 방법:**
1. 포트 8088이 다른 프로그램에 의해 사용 중인지 확인합니다:
   ```cmd
   netstat -an | findstr 8088
   ```
2. 사용 중이라면 해당 프로그램을 종료하거나 `appsettings.json`에서 포트를 변경합니다.
3. `logs/` 폴더의 로그 파일을 확인합니다.

---

### 브라우저에서 접속 불가

**증상:** http://localhost:8088 접속 시 "연결할 수 없음" 표시

**해결 방법:**
1. 애플리케이션이 실행 중인지 확인합니다.
2. 명령 프롬프트에서 직접 실행하여 오류 메시지를 확인합니다:
   ```cmd
   cd C:\ControlCenter
   ControlCenter.Gateway.exe
   ```
3. Windows 방화벽에서 포트 8088이 허용되어 있는지 확인합니다.
4. 다른 PC에서 접속하는 경우, localhost 대신 설치 PC의 IP 주소를 사용합니다.

---

## MQTT 관련 문제

### 장비가 "오프라인"으로 표시됨

**증상:** 등록한 장비의 상태가 계속 "오프라인" 또는 "알 수 없음"으로 표시됨

**해결 방법:**
1. AIBox 장비의 전원 및 네트워크 연결을 확인합니다.
2. AIBox 장비의 MQTT 브로커가 실행 중인지 확인합니다.
3. 등록된 MQTT 호스트/포트가 정확한지 확인합니다:
   ```cmd
   # MQTT 포트 연결 테스트
   telnet [AIBox IP] 1883
   ```
4. MQTT 인증 정보(사용자명/비밀번호)가 올바른지 확인합니다.
5. 네트워크 방화벽에서 1883 포트가 허용되어 있는지 확인합니다.

### MQTT 연결 반복 실패

**증상:** 로그에 "MQTT connection failed" 메시지가 반복적으로 표시됨

**해결 방법:**
1. 로그 파일에서 상세 오류 메시지를 확인합니다:
   ```
   logs/gateway-[날짜].log
   ```
2. 일반적인 원인:
   - MQTT 브로커 주소 오류 -> 장비 설정에서 MQTT 호스트 확인
   - 포트 번호 불일치 -> 장비의 실제 MQTT 포트 확인
   - 인증 실패 -> 사용자명/비밀번호 재확인
   - 네트워크 단절 -> ping 테스트로 네트워크 확인

3. `appsettings.json`의 `ReconnectDelaySeconds` 값을 늘려 재시도 간격을 조정할 수 있습니다.

---

## 데이터베이스 관련 문제

### "Database is locked" 오류

**증상:** 로그에 "SQLite Error: database is locked" 메시지가 표시됨

**해결 방법:**
1. 동일한 `controlcenter.db` 파일을 사용하는 다른 프로세스가 없는지 확인합니다.
2. DB 브라우저 등의 외부 도구로 데이터베이스를 열어둔 경우 닫습니다.
3. 애플리케이션을 재시작합니다.

### 데이터베이스 손상

**증상:** 시작 시 "database disk image is malformed" 오류

**해결 방법:**
1. 애플리케이션을 중지합니다.
2. `controlcenter.db` 파일을 백업합니다.
3. SQLite3 도구로 복구를 시도합니다:
   ```cmd
   sqlite3 controlcenter.db ".recover" | sqlite3 controlcenter_recovered.db
   ```
4. 복구가 불가능하면 `controlcenter.db`를 삭제하고 재시작합니다 (데이터 초기화).

### 디스크 공간 부족

**증상:** 이벤트 저장 실패, 로그에 "disk full" 관련 오류

**해결 방법:**
1. 디스크 여유 공간을 확인합니다.
2. `logs/` 폴더의 오래된 로그 파일을 삭제합니다.
3. 불필요한 이벤트 데이터를 정리합니다.
4. 데이터베이스 파일 크기를 확인합니다:
   ```cmd
   dir controlcenter.db
   ```

---

## 네트워크 관련 문제

### 다른 PC에서 대시보드 접속 불가

**증상:** 같은 네트워크의 다른 PC에서 http://[IP]:8088 접속 불가

**해결 방법:**
1. 설치 PC에서 방화벽 인바운드 규칙 확인:
   ```powershell
   Get-NetFirewallRule | Where-Object { $_.DisplayName -like "*Control*" }
   ```
2. 방화벽 규칙이 없으면 추가:
   ```powershell
   New-NetFirewallRule -DisplayName "Control Center Web" -Direction Inbound -Port 8088 -Protocol TCP -Action Allow
   ```
3. 설치 PC의 IP 주소 확인:
   ```cmd
   ipconfig
   ```
4. 두 PC가 같은 서브넷에 있는지 확인합니다.

---

## 성능 관련 문제

### 대시보드 로딩 느림

**증상:** 페이지 로딩이 느리거나 응답이 지연됨

**해결 방법:**
1. 이벤트 수가 매우 많은 경우 필터를 사용하여 조회 범위를 줄입니다.
2. 데이터베이스 파일 크기를 확인합니다 (1GB 이상이면 성능 저하 가능).
3. 오래된 이벤트를 CSV로 내보낸 후 삭제를 고려합니다.
4. PC 리소스(CPU, 메모리) 사용률을 확인합니다.

### 이벤트 수신 지연

**증상:** AIBox에서 이벤트 발생 후 대시보드에 표시되기까지 시간이 걸림

**해결 방법:**
1. 네트워크 지연 확인:
   ```cmd
   ping [AIBox IP]
   ```
2. MQTT 브로커 부하가 높지 않은지 확인합니다.
3. 로그에서 이벤트 처리 시간 관련 메시지를 확인합니다.
4. 브라우저 개발자 도구(F12)에서 WebSocket 연결이 정상인지 확인합니다.

---

## 로그 확인 방법

로그 파일 위치: `logs/gateway-YYYYMMDD.log`

로그 레벨:
- `[INF]` - 정보 (정상 동작)
- `[WRN]` - 경고 (주의 필요)
- `[ERR]` - 오류 (문제 발생)
- `[FTL]` - 치명적 오류 (애플리케이션 중단)

최근 오류만 확인:
```cmd
findstr /C:"[ERR]" logs\gateway-*.log
findstr /C:"[FTL]" logs\gateway-*.log
```

---

## 초기화 방법

모든 데이터를 초기화하고 새로 시작하려면:

1. 애플리케이션을 중지합니다.
2. `controlcenter.db` 파일을 삭제합니다.
3. `logs/` 폴더를 삭제합니다 (선택).
4. 애플리케이션을 다시 시작합니다.

> **경고:** 이 작업은 모든 장비 등록 정보와 이벤트 기록을 삭제합니다. 필요한 경우 먼저 CSV 내보내기로 백업하세요.

---

## 지원 요청

위 해결 방법으로 문제가 해결되지 않는 경우, 다음 정보를 포함하여 지원을 요청하세요:

1. 오류 증상 상세 설명
2. `logs/` 폴더의 최근 로그 파일
3. `appsettings.json` 내용 (비밀번호 제거)
4. Windows 버전 (`winver` 명령으로 확인)
5. 네트워크 구성 정보
