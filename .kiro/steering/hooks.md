# Windows Local Control Center - 훅(Hooks) 설정 문서

개발 워크플로우에서 사용하는 자동화 명령어 및 트리거를 정의한다.

---

## 1. 빌드 훅

### Frontend 빌드
React SPA를 빌드하고 결과물을 Backend의 wwwroot/로 복사한다.

```bash
# Frontend 빌드 (web/ 디렉터리에서 실행)
cd web && npm run build
```

빌드 결과물 복사:
```bash
# Linux/macOS
cp -r web/dist/* gateway/src/wwwroot/

# Windows (PowerShell)
Copy-Item -Path "web\dist\*" -Destination "gateway\src\wwwroot\" -Recurse -Force
```

### Backend 빌드
.NET 프로젝트를 빌드한다.

```bash
dotnet build gateway/src/ControlCenter.Gateway.csproj
```

### 통합 빌드 (Frontend + Backend)
빌드 스크립트를 통해 전체 프로젝트를 빌드한다.

```powershell
# Windows
./build.ps1
```

```bash
# Linux/CI
./build.sh
```

**빌드 스크립트 수행 순서:**
1. `web/` 디렉터리에서 `npm install` (의존성 설치)
2. `npm run build` (React 빌드)
3. 빌드 결과물을 `gateway/src/wwwroot/`로 복사
4. `dotnet build gateway/src/ControlCenter.Gateway.csproj`

### Release 빌드 (배포용)
Windows x64 자체 포함(self-contained) 단일 실행 파일로 빌드한다.

```bash
dotnet publish gateway/src/ControlCenter.Gateway.csproj \
  -c Release \
  -r win-x64 \
  --self-contained \
  -p:PublishSingleFile=true \
  -o ./publish
```

**결과물:**
- `./publish/ControlCenter.Gateway.exe` (~60MB)
- wwwroot/ 포함 (React SPA 정적 파일)
- .NET 런타임 포함 (대상 PC에 별도 설치 불필요)

---

## 2. 테스트 훅

### 단위 테스트 실행
모든 단위 테스트를 실행한다.

```bash
dotnet test gateway/tests/
```

### Property-Based 테스트만 실행
FsCheck 기반 Property 테스트만 필터링하여 실행한다.

```bash
dotnet test gateway/tests/ --filter "Category=PropertyTest"
```

### Frontend 테스트 실행
React 컴포넌트 및 유틸리티 테스트를 실행한다.

```bash
cd web && npm test
```

### 전체 테스트 (Backend + Frontend)
모든 테스트를 순차 실행한다.

```bash
# Backend 테스트 실행 후 Frontend 테스트 실행
dotnet test gateway/tests/ && cd web && npm test
```

### 테스트 커버리지 리포트
```bash
dotnet test gateway/tests/ --collect:"XPlat Code Coverage"
```

---

## 3. 개발 서버 실행 훅

### Backend 개발 서버
.NET 개발 서버를 실행한다. 기본 포트: 8088

```bash
dotnet run --project gateway/src/ControlCenter.Gateway.csproj
```

**환경 변수:**
- `ASPNETCORE_ENVIRONMENT=Development` (자동 설정)
- 핫 리로드: `dotnet watch` 사용 가능

```bash
# 핫 리로드 포함 실행
dotnet watch run --project gateway/src/ControlCenter.Gateway.csproj
```

### Frontend 개발 서버 (Vite)
Vite 개발 서버를 실행한다. 기본 포트: 5173

```bash
cd web && npm run dev
```

**개발 모드 특징:**
- HMR (Hot Module Replacement) 활성화
- Backend API 프록시 설정 (`vite.config.ts`에서 `http://localhost:8088`으로 프록시)
- 소스맵 활성화

### 통합 실행 (프로덕션 모드 로컬 테스트)
1. Frontend 빌드 → wwwroot 복사
2. Backend 실행
3. 브라우저에서 `http://localhost:8088` 접속

```bash
# 1단계: Frontend 빌드
cd web && npm run build

# 2단계: 결과물 복사
cp -r dist/* ../gateway/src/wwwroot/

# 3단계: Backend 실행
cd ../gateway/src && dotnet run
```

또는 빌드 스크립트 사용:
```powershell
# Windows
./build.ps1
dotnet run --project gateway/src/ControlCenter.Gateway.csproj
```

**접속 확인:**
- 대시보드: http://localhost:8088
- API 상태: http://localhost:8088/api/dashboard/summary
- SignalR Hub: ws://localhost:8088/hubs/events

---

## 4. 코드 품질 훅

### Frontend Lint
ESLint를 사용한 코드 품질 검사.

```bash
cd web && npm run lint
```

**규칙:**
- React Hooks 규칙 적용
- React Refresh 규칙 적용
- TypeScript ESLint 규칙 적용

### Frontend Lint 자동 수정
```bash
cd web && npm run lint -- --fix
```

### Backend Format
.NET 코드 포매팅 (.editorconfig 기반).

```bash
dotnet format gateway/src/
```

### Backend Analyzer 검사
```bash
dotnet build gateway/src/ --warnaserror
```

---

## 5. DB 마이그레이션 훅

### 마이그레이션 추가
스키마 변경 시 새 마이그레이션을 생성한다.

```bash
dotnet ef migrations add {MigrationName} --project gateway/src/
```

**명명 규칙 예시:**
- `InitialCreate` - 최초 스키마 생성
- `AddCameraStatusField` - 카메라 상태 필드 추가
- `AddEventSyncFields` - 이벤트 동기화 필드 추가
- `CreateEventIndexes` - 이벤트 인덱스 생성

### 마이그레이션 적용
대기 중인 마이그레이션을 DB에 적용한다.

```bash
dotnet ef database update --project gateway/src/
```

### 마이그레이션 되돌리기
마지막 마이그레이션을 되돌린다.

```bash
dotnet ef database update {PreviousMigrationName} --project gateway/src/
```

### 마이그레이션 삭제 (미적용 상태에서만)
```bash
dotnet ef migrations remove --project gateway/src/
```

### 참고: 자동 마이그레이션
- 앱 시작 시 `db.Database.EnsureCreated()`로 스키마 자동 생성
- 개발 환경에서는 EF Core가 자동으로 DB를 생성/업데이트
- 프로덕션에서는 명시적 마이그레이션 관리 권장

### EF Core 도구 설치 (최초 1회)
```bash
dotnet tool install --global dotnet-ef
```

---

## 6. 배포 전 체크리스트

릴리스 빌드 전 아래 항목을 모두 확인한다:

- [ ] 모든 Backend 테스트 통과: `dotnet test gateway/tests/`
- [ ] 모든 Property 테스트 통과: `dotnet test gateway/tests/ --filter "Category=PropertyTest"`
- [ ] Frontend 테스트 통과: `cd web && npm test`
- [ ] Frontend Lint 통과: `cd web && npm run lint`
- [ ] Frontend 빌드 성공: `cd web && npm run build`
- [ ] wwwroot/에 최신 프론트엔드 복사 완료
- [ ] Backend 빌드 성공: `dotnet build gateway/src/`
- [ ] Self-contained 릴리스 빌드 성공
- [ ] SQLite DB 자동 생성 확인 (새 폴더에서 실행 테스트)
- [ ] Port 8088 리스닝 확인: 브라우저에서 http://localhost:8088 접속
- [ ] SignalR 연결 확인: 대시보드 실시간 업데이트 동작
- [ ] MQTT 연결 확인: AIBox 이벤트 수신 테스트
- [ ] 로그 파일 생성 확인: `logs/` 폴더에 일자별 로그 생성

---

## 7. 빠른 참조 명령어

| 작업 | 명령어 |
|------|--------|
| Backend 빌드 | `dotnet build gateway/src/ControlCenter.Gateway.csproj` |
| Backend 실행 | `dotnet run --project gateway/src/ControlCenter.Gateway.csproj` |
| Backend 감시 실행 | `dotnet watch run --project gateway/src/ControlCenter.Gateway.csproj` |
| Backend 테스트 | `dotnet test gateway/tests/` |
| Backend 포맷 | `dotnet format gateway/src/` |
| Frontend 의존성 설치 | `cd web && npm install` |
| Frontend 개발 실행 | `cd web && npm run dev` |
| Frontend 빌드 | `cd web && npm run build` |
| Frontend 린트 | `cd web && npm run lint` |
| Frontend 테스트 | `cd web && npm test` |
| 통합 빌드 (Win) | `./build.ps1` |
| 통합 빌드 (Linux) | `./build.sh` |
| 릴리스 빌드 | `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true` |
| DB 마이그레이션 추가 | `dotnet ef migrations add {Name} --project gateway/src/` |
| DB 마이그레이션 적용 | `dotnet ef database update --project gateway/src/` |

---

## 8. 환경별 설정

### 개발 환경 (Development)
- Backend: `http://localhost:8088`
- Frontend Dev Server: `http://localhost:5173` (Vite)
- CORS: localhost:5173, localhost:3000, localhost:8088 허용
- 로그 레벨: Debug
- DB: `controlcenter.db` (프로젝트 루트)

### 프로덕션 환경 (Production)
- 단일 실행 파일: `ControlCenter.Gateway.exe`
- Frontend: wwwroot/ 내장 (빌드 결과물)
- CORS: 동일 출처만 허용
- 로그 레벨: Information
- DB: 실행 파일과 같은 디렉터리에 자동 생성
- 포트: 8088 (appsettings.json에서 변경 가능)
