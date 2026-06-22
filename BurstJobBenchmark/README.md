# Unity Burst & Job System 성능 비교 벤치마크

## 프로젝트 소개

Unity의 **Burst Compiler**와 **Job System**이 실제 연산 성능에 어떤 영향을 주는지 확인하기 위해 제작한 벤치마크 프로젝트입니다.

동일한 연산을 4가지 방식으로 실행하여 처리 시간을 비교합니다.

### 비교 대상

1. 일반 C# (Single Thread)
2. Burst Compiler만 적용
3. Job System만 적용
4. Burst Compiler + Job System 적용

---

## 개발 목적

Unity 프로젝트를 개발하면서 많은 양의 데이터를 처리해야 하는 경우가 발생합니다.

예를 들어:

- 수천 개의 적 AI 계산
- 대량의 투사체 이동 처리
- 군중(Crowd) 시뮬레이션
- RTS 유닛 경로 탐색
- ECS/DOTS 기반 데이터 처리

이러한 상황에서 Burst Compiler와 Job System을 활용하면 CPU 사용 효율을 크게 향상시킬 수 있습니다.

## 결론
1.Burst Compiler
Burst는 멀티스레드 기술이 아닙니다.

Burst의 역할은 C# 코드를 CPU가 효율적으로 실행할 수 있는 네이티브 코드로 변환하는 것입니다.

주요 효과

SIMD(Vectorization)
CPU 명령어 최적화
수치 연산 성능 향상

2.Job System
Job System은 작업을 여러 Worker Thread에 분산하여 실행하는 시스템입니다.

주요 효과

멀티코어 활용
병렬 처리
메인 스레드 부하 감소
Burst + Job

두 기술은 서로 경쟁 관계가 아니라 상호 보완 관계입니다.

Burst = 연산 자체를 빠르게 수행

Job System = 여러 스레드에서 동시에 수행

Burst + Job = 빠른 연산을 여러 스레드에서 수행

대량의 데이터 처리에서는 두 기술을 함께 사용하는 것이 가장 효율적입니다.