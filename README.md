# task

과제 진행용 .NET 8 + CQRS 예시 구현입니다.

## 프로젝트 구조

- `src/Api`: Minimal API 엔드포인트
- `src/Application`: Command/Query, 유효성 검증, 포트(interface)
- `src/Domain`: 도메인 모델
- `src/Infrastructure`: In-memory Repository, CSV/JSON 파서
- `tests/Unit`: 단위 테스트
- `tests/Integration`: API 통합 테스트
- `TASK_STATUS.md`: 진행 상태(완료/진행중/남은 작업) 체크리스트

## 실행 (로컬에 .NET 8 SDK 필요)

```bash
dotnet restore
dotnet run --project src/Api/Api.csproj
```

Swagger: `http://localhost:5000/swagger` (환경에 따라 포트 상이)

## Persistence Layer

- **SQLite** 사용 (`Data Source=employees.db`)
- 앱 시작 시 `EnsureCreated()`로 DB 파일과 테이블을 자동 생성합니다
- **별도 DB 설치나 마이그레이션이 필요 없습니다**

## 샘플 데이터

`samples/` 폴더에 테스트용 데이터를 포함했습니다. Swagger 또는 `curl`로 파일 업로드 테스트 시 사용할 수 있습니다.

- `samples/employees.csv` — CSV 형식 샘플
- `samples/employees.json` — JSON 형식 샘플

## 테스트

```bash
dotnet test task_20260212.sln
```

- Unit: 파서/커맨드/쿼리 유효성 검증
- Integration: 실제 HTTP 요청 기준으로 엔드포인트 동작(성공/실패/조회) 검증
  - 포함 시나리오: JSON 본문 등록, multipart CSV 등록, form field(data) 등록, 잘못된 JSON 400, 잘못된 page 400, 이름 미존재 404
- CI: `.github/workflows/dotnet.yml`에서 push/PR마다 테스트 실행


## API

### 1) 직원 목록 조회 (페이징)

- `GET /api/employee?page={page}&pageSize={pageSize}`

### 2) 이름으로 직원 조회

- `GET /api/employee/{name}`

### 3) 직원 추가

- `POST /api/employee`
- 지원 입력
  - `multipart/form-data` 파일 업로드 (`.csv`, `.json`)
  - request body에 csv 텍스트 직접 전달
  - request body에 json 텍스트 직접 전달(배열/단일 객체 모두 지원)
  - form field(`csv`, `json`, `data`)로 텍스트 전달

## 입력 포맷 예시

CSV (줄 단위):

```text
김철수, user1@example.com, 01075312468, 2018.03.07
박영희, user2@example.com, 01087654321, 2021.04.28
```

JSON:

```json
[
  {"name":"홍길동","email":"user3@example.com","tel":"010-1111-2424","joined":"2012-01-05"},
  {"name":"이몽룡","email":"user4@example.com","tel":"010-3535-7979","joined":"2013-07-01"}
]
```

JSON 단일 객체도 허용:

```json
{"name":"성춘향","email":"user5@example.com","tel":"010-5555-6666","joined":"2020-05-01"}
```

## 구현 포인트

- CQRS 분리
  - `CreateEmployeesCommandHandler`
  - `GetEmployeesQueryHandler`
  - `GetEmployeeByNameQueryHandler`
- 실패 시 `400 Bad Request` 반환 (검증 예외)
- 페이징 파라미터 및 필수 데이터 유효성 검증

## 로그

- **Serilog** 기반 구조적 로깅
  - 콘솔: `Information` 이상 출력 (프레임워크 로그는 `Warning` 이상만)
  - 파일: `Error` 이상만 날짜별 롤링 파일로 기록 (`task_20260212_logs/error-yyyyMMdd.log`)
- Handler 레벨 로그: 요청 수신, 파싱 완료, 등록 완료, 유효성 경고
- API 에러 로그: 400(유효성 실패) / 500(서버 오류) 모두 파일에 기록


## 특이사항 / 선택 이유

- 요청 바디 분류는 **형태 기반 + Content-Type 기반**으로 처리했습니다.
  - 이유: 잘못된 JSON도 JSON으로 분류해 `invalid json payload` 같은 일관된 에러를 반환하기 위해서입니다.
- 저장소(In-memory) 저장 시 **이메일/전화번호 중복**을 막았습니다.
  - 이유: 연락처 데이터의 기본 무결성을 보장하고, 중복 입력을 빠르게 차단하기 위해서입니다.
- CSV는 BOM 및 헤더 라인(`name,email,...`)을 허용했습니다.
  - 이유: 실제 업로드 파일 다양성을 흡수해 사용자 입력 실패를 줄이기 위해서입니다.
