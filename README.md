# TextSlayers

글자로만 진행되는 실시간 게이지(ATB) 전투 텍스트 RPG. Unity 6 (6000.3.22f1), 외부 패키지 없음, UI 는 IMGUI.
기획: [Docs/Design/TextSlayers_Concept_v0.1.md](Docs/Design/TextSlayers_Concept_v0.1.md)

## 실행
1. Unity Hub 에서 이 폴더 열기 (6000.3.22f1).
2. `Assets/Scenes/Main.unity` 열고 Play. (다른 씬에서 Play 해도 `GameRoot` 가 자동 생성됨)
3. 씬이 깨졌으면 메뉴 **TextSlayers → 씬 · 빌드 설정 초기화**.

## 조작
| 화면 | 키보드 | 마우스 · 터치 |
|---|---|---|
| 타이틀 | 아무 키 | 시작 |
| 전투 | `1` 베기 · `2` 트리플 슬래시 · `3` 파이어 블레이드 · `4` 방어, 끝나면 `Enter` | 행동 버튼, 후퇴 |
| 결과 | `Enter` / `Space` | 로비로 |

## 현재 구현 (v0.1 기본 사양)
- 화면: 타이틀 → 로비(스테이지 · 캐릭터 정보 · 저장 · 기록 초기화) → 전투 → 결과
- 대기형 ATB 전투: 에른 게이지가 차면 시간 정지 → 행동 선택. 적은 자동 행동. 보스는 강타를 한 행동 전에 예고.
- 스테이지 1 「고블린 숲」 4웨이브 (고블린 → 고블린 궁수 → 고블린 → 홉 고블린)
- 경험치 · 레벨업, 골드 · 메모리, `persistentDataPath/save.json` 저장
- 수치는 전부 `[임시]` — `Assets/Scripts/Data/GameData.cs` 맨 위 상수와 표

## 폴더
```
Assets/Scripts/Core    GameRoot (상태 · 화면 전환, 자동 생성), TextLog (색 로그)
Assets/Scripts/Data    Stats, GameData (스킬 · 적 · 스테이지 · 성장 표)
Assets/Scripts/Battle  BattleSystem (ATB 진행), Combatant, Damage (피해 계산 유일 경로)
Assets/Scripts/Save    PlayerProgress, SaveSystem
Assets/Scripts/UI      GameView (IMGUI 전 화면)
Assets/Editor          SceneSetup (씬 · 빌드 설정 생성), BattleSim (자동 전투 시뮬)
Docs/Design            기획서
```

## 밸런스 시뮬레이션
메뉴 **TextSlayers → 전투 시뮬레이션** (콘솔에 레벨별 승률 출력). 배치 모드:
```
Unity.exe -batchmode -quit -projectPath <이 폴더> -executeMethod TextSlayers.EditorTools.BattleSim.Run -logFile sim.log
```
