# task 진행 현황

## 완료
- [x] .NET 8 + CQRS 레이어 구조 구성
- [x] 필수 API 3종 구현 (`GET /api/employee`, `GET /api/employee/{name}`, `POST /api/employee`)
- [x] CSV/JSON 파서 강화 (BOM, 헤더, 단일 JSON 객체 허용)
- [x] Unit 테스트 작성 (파서/커맨드/쿼리 검증)
- [x] Integration 테스트 작성 (핵심 API 성공/실패 시나리오)
- [x] CI 워크플로우에서 `dotnet test` 자동 실행

## 진행중
- [x] 통합 테스트 시나리오 추가 보강 (form field 기반 입력, 잘못된 페이징)

## 남은 작업
- [ ] 로컬/실환경에서 `dotnet test task_20260212.sln` 실제 통과 확인
- [ ] API 수동 E2E 확인 (`dotnet run` 후 swagger + 주요 요청)

## 특이사항
- 현재 작업 환경에는 `dotnet` SDK가 없어 로컬 실행 검증은 CI 또는 SDK 설치 환경에서 확인 필요
