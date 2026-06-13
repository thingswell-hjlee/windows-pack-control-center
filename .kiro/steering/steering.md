# Windows Local Control Center - 프로젝트 스티어링 문서

## 1. 프로젝트 개요

Windows Local Control Center는 AIBox 엣지 디바이스와 네트워크 카메라를 통합 관제하는 로컬 웹 기반 모니터링 시스템이다.
Windows PC에서 단일 실행 파일로 동작하며, 브라우저를 통해 `http://localhost:8088`에서 대시보드에 접근한다.

**핵심 구성요소:**
- **Local Gateway** (Backend): .NET 8 ASP.NET Core 서버 - MQTT 수신, 이벤트 정규화, DB 저장, REST API, SignalR 제공
- **Web Dashboard** (Frontend): React SPA - 실시간 모니터링, 이벤트 관리, 장치 설정 UI
- **SQLite Database**: 단일 파일 DB - 이벤트, 장치, 카메라 데이터 영구 저장
- **MQTT Receiver**: AIBox에서 발행하는 이벤트/상태 메시지 수신

**운영 환경:** 로컬 네트워크 전용 (인터넷 불필요), 향후 AWS 클라우드 확장 대비 설계

---

## 2. 기술 스택

### Backend
| 기술 | 용도 |
|------|------|
| .NET 8 | 런타임 |
| ASP.NET Core Minimal API | HTTP REST API |
| MQTTnet 4.x | MQTT 클라이언트 (AIBox 연결) |
| SQLite (via EF Core) | 데이터베이스 |
| SignalR | 실시간 WebSocket 통신 |
| Serilog | 로깅 (Console + Rolling File) |
| System.Threading.Channels | 서비스 간 비동기 메시지 전달 |

### Frontend
| 기술 | 용도 |
|------|------|
| React 19 | UI 프레임워크 |
| TypeScript 6.x | 타입 안전성 |
| Vite 8.x | 빌드 도구 |
| TailwindCSS 4.x | 스타일링 |
| TanStack Query | 서버 상태 관리 |
| @microsoft/signalr | 실시간 이벤트 수신 |
| React Router DOM | SPA 라우팅 |
| Axios | HTTP 클라이언트 |
| Day.js | 날짜/시간 처리 |
| Lucide React | 아이콘 |

### 테스트
| 기술 | 용도 |
|------|------|
| xUnit | 단위 테스트 |
| FsCheck | Property-Based Testing |
| FluentAssertions | 어설션 |
| Moq | 모킹 |
| WebApplicationFactory | 통합 테스트 |

---

## 3. 아키텍처 원칙

### 3.1 Local Gateway 중심 아키텍처
- Local Gateway가 **모든 MQTT 수신**, **이벤트 정규화**, **DB 저장**을 전담한다.
- 브라우저(Web Dashboard)는 **절대 MQTT Broker에 직접 연결하지 않는다**.
- 프론트엔드는 REST API와 SignalR만 사용하여 데이터에 접근한다.

### 3.2 표준 Event Schema
- 모든 이벤트는 AIBox 원본 포맷과 관계없이 **Standard Event Schema**로 변환 후 저장한다.
- 원본 페이로드(`raw_payload`)도 함께 보관하여 추후 재처리 가능하도록 한다.
- Schema 변환 실패 시에도 원본은 반드시 보존한다.

### 3.3 실시간 이벤트 전달
- SignalR을 통해 서버→클라이언트 실시간 이벤트를 푸시한다.
- Hub 경로: `/hubs/events`
- 메시지 종류: `NewEvent`, `DeviceStatusChanged`, `CameraStatusChanged`, `EventAcknowledged`, `EventMemoUpdated`

### 3.4 데이터 저장
- **SQLite 단일 파일 DB** 사용 (`controlcenter.db`)
- 별도 DB 서버 설치 불필요
- EF Core를 통한 ORM 접근
- 앱 시작 시 DB 자동 생성 및 마이그레이션 적용

### 3.5 Background Service 파이프라인
```
MQTT 수신 → Event Channel → Normalize → Deduplicate → SQLite 저장 → SignalR Broadcast
```
- `Channel<T>`를 통한 서비스 간 비동기 메시지 전달
- 각 서비스는 독립적으로 동작하며, 하나가 실패해도 전체 시스템은 계속 동작

### 3.6 오프라인 우선 설계
- 인터넷 없이 완전 동작
- `sync_status` 필드로 향후 AWS 연동 대비
- 모든 스키마는 향후 클라우드 시스템과 호환되도록 설계

---

## 4. 코딩 규칙

### 4.1 Backend 규칙

#### Background Service 패턴
모든 백그라운드 작업은 `BackgroundService`를 상속한다:
- `MqttReceiverService` - MQTT 브로커 연결 및 메시지 수신
- `EventNormalizerService` - 이벤트 정규화 및 저장
- `DeviceStatusService` - 장치 상태 모니터링
- `CameraStatusService` - 카메라 상태 체크

#### Channel 기반 통신
```csharp
// 서비스 간 메시지 전달에 In-memory Channel<T> 사용
BoundedChannel<MqttMessage>(capacity: 10000, BoundedChannelFullMode.DropOldest)
```

#### Entity Framework Core + SQLite
- `AppDbContext`를 통한 데이터 접근
- 모든 엔티티에 `created_at`, `updated_at` 타임스탬프 포함
- 인덱스: `device_id`, `timestamp`, `event_type`, `severity`, `ack_status`

#### Minimal API (Controllers 미사용)
```csharp
// 올바른 예시
app.MapGet("/api/devices", async (AppDbContext db) => { ... });
app.MapPost("/api/devices", async (Device device, AppDbContext db) => { ... });

// 사용하지 않음 - Controller 패턴
[ApiController] // 사용 금지
public class DevicesController { ... } // 사용 금지
```

#### 설정 관리
- **모든 설정은 `appsettings.json`에서 관리** (하드코딩 금지)
- MQTT 포트, 재연결 간격, 하트비트 타임아웃 등 모든 매직넘버는 설정 파일로 이동
- 환경별 설정: `appsettings.Development.json`

#### event_id 생성 규칙
```
track_id 있는 경우: {device_id}-{ts_ms}-{event_type}-{camera_id}-{track_id}
track_id 없는 경우: {device_id}-{ts_ms}-{event_type}-{camera_id}
```

#### bbox 저장 형식
- 항상 객체 형태로 저장: `{"x":x,"y":y,"w":w,"h":h}`
- 배열 형태 `[x, y, w, h]`로 수신 시 반드시 객체로 변환 후 저장
- JSON 문자열로 DB에 저장

#### Severity 분류
```csharp
// 정적 Dictionary로 O(1) 조회
private static readonly Dictionary<string, string> SeverityMap = new()
{
    ["FALL_DETECTED"] = "HIGH",
    ["ROI_INTRUSION"] = "HIGH",
    ["HAZARD_PROXIMITY"] = "HIGH",
    ["DEVICE_OFFLINE"] = "HIGH",
    ["WORK_ZONE_ENTRY"] = "MEDIUM",
    ["WORK_NO_HELMET"] = "MEDIUM",
    ["WORK_NO_VEST"] = "MEDIUM",
    ["WORK_NO_MASK"] = "MEDIUM",
    ["CAMERA_OFFLINE"] = "MEDIUM",
    ["SYSTEM_WARNING"] = "MEDIUM",
    ["UNKNOWN_EVENT"] = "LOW"
};
// 미등록 이벤트: UNKNOWN_EVENT / LOW
```

#### 에러 처리
- Background Service 내 예외는 로그 후 계속 동작 (프로세스 종료 금지)
- MQTT 연결 실패: 로그 → 5초 대기 → 재시도 루프
- 중복 이벤트: 조용히 무시 (로그 레벨 Debug)
- 잘못된 페이로드: 경고 로그 + 메시지 폐기

### 4.2 Frontend 규칙

#### 프로젝트 구조
```
web/src/
├── components/      # 재사용 가능한 UI 컴포넌트
├── pages/           # 페이지 컴포넌트
├── hooks/           # 커스텀 훅
├── services/        # API 호출, SignalR 연결
├── types/           # TypeScript 인터페이스/타입
├── utils/           # 유틸리티 함수
├── App.tsx          # 라우터 설정
└── main.tsx         # 엔트리포인트
```

#### TypeScript 엄격 모드
- `strict: true` 필수
- `any` 타입 사용 금지 (불가피한 경우 주석 필수)
- 모든 API 응답에 대한 타입 정의 필수

#### 상태 관리
- 서버 상태: TanStack Query 사용
- 실시간 이벤트: SignalR + React State
- 로컬 UI 상태: React useState/useReducer

#### UI 언어
- **모든 사용자 향 텍스트는 한국어**로 작성
- 코드 내 변수명, 함수명은 영어 사용
- 주석은 한국어 또는 영어 가능

---

## 5. 이벤트 처리 파이프라인

```
[AIBox MQTT Publish]
       │
       ▼
[MqttReceiverService] ─── Topic 라우팅 ───┐
       │                                   │
       ▼ (/event/)                        ▼ (/status/)
[Event Channel]                     [Status Channel]
       │                                   │
       ▼                                   ▼
[EventNormalizerService]            [DeviceStatusService]
       │                                   │
       ├─ JSON 파싱                        ├─ 하트비트 갱신
       ├─ event_id 생성                    ├─ 타임아웃 체크 (60초)
       ├─ Severity 분류                    └─ 상태 변경 → SignalR
       ├─ bbox 변환
       ├─ 장치/카메라 이름 보강
       ├─ 중복 체크
       ├─ SQLite 저장
       └─ SignalR Broadcast (NewEvent)
```

---

## 6. MVP 범위 제한

### 포함 (MVP)
- AIBox 장치 등록/관리/상태 모니터링
- 네트워크 카메라 등록/관리/상태 체크
- MQTT 이벤트 수신 및 정규화
- 실시간 이벤트 대시보드
- 이벤트 필터링/검색/페이지네이션
- 이벤트 확인(Ack) 및 조치 메모
- 이벤트 CSV/Excel 내보내기
- 대시보드 요약 통계

### 제외 (MVP 이후)
- AWS 클라우드 업로드/동기화 실행
- AIBox ROI(관심영역) 편집
- AI 모델 선택 및 변경
- DeepStream 파이프라인 튜닝
- OTA(무선 업데이트) 패치
- 복잡한 사용자 권한 관리 (RBAC)
- 다중 사용자 인증
- 영상 스트리밍 (RTSP 뷰어)
- 알림 (이메일, SMS, 슬랙)

---

## 7. 폴더 구조

```
windows-pack-control-center/
├── .kiro/
│   ├── specs/                  # Kiro 스펙 문서
│   └── steering/               # 스티어링 문서 (본 파일)
├── gateway/
│   ├── src/                    # .NET Backend 소스코드
│   │   ├── Data/               # EF Core DbContext
│   │   ├── Endpoints/          # Minimal API 엔드포인트
│   │   ├── Hubs/               # SignalR Hub
│   │   ├── Models/             # 엔티티 모델
│   │   ├── Services/           # Background Services
│   │   ├── Properties/         # launchSettings.json
│   │   ├── wwwroot/            # 프론트엔드 빌드 결과물
│   │   ├── Program.cs          # 엔트리포인트
│   │   ├── appsettings.json    # 설정 파일
│   │   └── ControlCenter.Gateway.csproj
│   └── tests/                  # xUnit 테스트 프로젝트
├── web/
│   ├── src/                    # React Frontend 소스코드
│   │   ├── components/         # UI 컴포넌트
│   │   ├── pages/              # 페이지 컴포넌트
│   │   ├── hooks/              # 커스텀 훅
│   │   ├── services/           # API/SignalR 서비스
│   │   ├── types/              # TypeScript 타입
│   │   └── utils/              # 유틸리티
│   ├── public/                 # 정적 에셋
│   ├── package.json
│   └── vite.config.ts
├── docs/                       # 한국어 문서
│   ├── INSTALL.ko.md           # 설치 가이드
│   ├── USER_GUIDE.ko.md        # 사용자 가이드
│   └── TROUBLESHOOTING.ko.md   # 문제 해결
├── installer/                  # 설치 프로그램 관련
├── build.ps1                   # Windows 빌드 스크립트
├── build.sh                    # Linux/CI 빌드 스크립트
└── README.md
```

---

## 8. 테스트 규칙

### 8.1 프레임워크
- **단위 테스트**: xUnit + FluentAssertions + Moq
- **Property-Based Testing**: FsCheck (xUnit 통합)
- **통합 테스트**: WebApplicationFactory + SQLite In-Memory

### 8.2 테스트 원칙
- 모든 이벤트 정규화 로직은 **반드시 Property Test로 검증**
- Property Test 최소 반복 횟수: **100회**
- 테스트 카테고리 태그: `[Trait("Category", "PropertyTest")]`
- 실제 데이터로 테스트 (Mock 최소화)

### 8.3 Property Test 대상
1. **Event ID 결정론** - 같은 입력 → 항상 같은 event_id
2. **정규화 완전성** - 모든 필수 필드 비어있지 않음
3. **Severity 분류 전체성** - 어떤 event_type이든 반드시 HIGH/MEDIUM/LOW 중 하나
4. **Bbox 변환 왕복** - 배열→객체 변환 후 값 보존
5. **중복 제거** - 같은 event_id는 한 번만 저장

### 8.4 테스트 파일 위치
```
gateway/tests/
├── Unit/
│   ├── EventNormalizerTests.cs
│   ├── EventIdGeneratorTests.cs
│   ├── SeverityClassifierTests.cs
│   └── BboxConverterTests.cs
├── Property/
│   ├── EventIdDeterminismProperty.cs
│   ├── NormalizationCompletenessProperty.cs
│   ├── SeverityClassificationProperty.cs
│   └── BboxConversionProperty.cs
└── Integration/
    ├── DevicesApiTests.cs
    ├── EventsApiTests.cs
    └── EventPipelineTests.cs
```

---

## 9. UI 언어

- **사용자 인터페이스**: 한국어
- **에러 메시지**: 한국어
- **대시보드 라벨**: 한국어
- **코드**: 영어 (변수명, 함수명, 클래스명)
- **기술 문서**: 한국어 (docs/ 폴더)
- **코드 주석**: 한국어 또는 영어

---

## 10. 포트 및 네트워크 설정

| 항목 | 기본값 | 설정 위치 |
|------|--------|-----------|
| HTTP 포트 | **8088** | `appsettings.json` → Kestrel 설정 |
| MQTT 기본 포트 | 1883 | 장치 등록 시 기본값 |
| SignalR Hub | `/hubs/events` | 코드 고정 |
| REST API 접두사 | `/api/` | 코드 고정 |
| SPA Fallback | `/index.html` | wwwroot 기준 |

---

## 11. 배포 모델

- **단일 실행 파일** 배포: `dotnet publish -r win-x64 --self-contained -p:PublishSingleFile=true`
- .NET 런타임 설치 불필요 (self-contained)
- SQLite DB 자동 생성
- React SPA는 wwwroot/에 포함되어 함께 배포
- 대상 OS: Windows 10+ (x64)
