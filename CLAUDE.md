# CLAUDE.md — TextSlayers (Claude Code용)

규칙 · 구조 · 진행 상태는 **[AGENTS.md](AGENTS.md)** 가 기준. 먼저 읽고 따를 것. 소통 · 주석 · 커밋 메시지는 한국어.

## 검증 (Unity 에디터가 이 프로젝트를 열고 있지 않을 때)
배치 모드로 컴파일 + 실행 확인:
```
"C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe" -batchmode -quit -projectPath <이 폴더> -executeMethod TextSlayers.EditorTools.BattleSim.Run -logFile <임시폴더>\sim.log
```
- 로그에서 `error CS` 가 없고 `[TextSlayers] 전투 시뮬레이션` 결과가 나오면 컴파일 · 전투 로직 정상.
- 에디터가 열려 있으면 배치 모드가 실패함 → "에디터에서 메뉴 TextSlayers → 전투 시뮬레이션 실행" 을 사용자에게 안내.
- Play 화면은 배치 모드로 확인할 수 없음 → 보고에 "Play 미확인".

## Windows 메모
- 한글 커밋 메시지는 파일에 UTF-8 로 쓴 뒤 `git commit -F 파일`.
