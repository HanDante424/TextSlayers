using System.Text;
using UnityEditor;
using UnityEngine;

namespace TextSlayers.EditorTools
{
    /// <summary>
    /// 필드 전투 자동 시뮬레이션 (기획 v0.2). 실제 FieldBattle 을 화면 없이 고정 스텝으로 돌린다.
    /// 메뉴: TextSlayers / 필드 전투 시뮬레이션
    /// 배치 모드: -executeMethod TextSlayers.EditorTools.FieldSim.Run
    /// 편성 4종(전략방식 비교)을 레벨별로 돌려 승률 · 시간 · 남은 HP · 탱커 피해 비중을 출력한다.
    /// 파티는 스티움 카터렉 · 에른 스트레이 · 멜 발렌티나 (캐릭터 기본 구성) [임시].
    /// </summary>
    public static class FieldSim
    {
        const int RunsPerCase = 100;
        static readonly int[] Levels = { 1, 3, 5 };

        struct Case
        {
            public string Label;
            public Strategy Stium, Ern, Mel;
        }

        [MenuItem("TextSlayers/필드 전투 시뮬레이션")]
        public static void Run()
        {
            var cases = new[]
            {
                new Case { Label = "기본 (돌진 · 후방 침투 · 도주 견제)", Stium = Strategy.Charge, Ern = Strategy.Flank, Mel = Strategy.Kite },
                new Case { Label = "에른 돌진 (침투 없음)", Stium = Strategy.Charge, Ern = Strategy.Charge, Mel = Strategy.Kite },
                new Case { Label = "전원 돌진", Stium = Strategy.Charge, Ern = Strategy.Charge, Mel = Strategy.Charge },
                new Case { Label = "전원 진형 유지", Stium = Strategy.Hold, Ern = Strategy.Hold, Mel = Strategy.Hold },
            };

            var sb = new StringBuilder("[TextSlayers] 필드 전투 시뮬레이션\n");
            foreach (var stage in FieldData.Stages)
            {
                if (stage.IsTest) continue; // 테스트 맵은 끝나지 않으므로 제외
                foreach (int lv in Levels)
                {
                    foreach (var cs in cases)
                    {
                        int wins = 0, stuck = 0;
                        float seconds = 0f, hpLeft = 0f, tankShare = 0f, wavesReached = 0f;
                        for (int i = 0; i < RunsPerCase; i++)
                        {
                            var party = new[]
                            {
                                Slot("stium", cs.Stium), Slot("ern", cs.Ern), Slot("mel", cs.Mel),
                            };
                            var b = new FieldBattle(stage, party, lv, 1000 + i);
                            int steps = 0;
                            int maxSteps = Mathf.CeilToInt(FieldConst.TimeLimit / FieldConst.Dt) + 100;
                            while (!b.IsOver && steps < maxSteps) { b.Step(); steps++; }
                            if (!b.IsOver) stuck++;
                            if (b.TimedOut) stuck++;

                            float taken = 0f, hp = 0f, maxHp = 0f;
                            foreach (var u in b.Players)
                            {
                                taken += u.DamageTaken;
                                hp += u.C.Hp;
                                maxHp += u.C.Stats.HP;
                            }
                            if (taken > 0f) tankShare += b.Players[0].DamageTaken / taken;
                            if (b.Phase == FieldPhase.Won) { wins++; hpLeft += hp / maxHp; }
                            seconds += b.Time;
                            wavesReached += b.WaveIndex + 1;
                        }
                        sb.AppendLine($"{stage.Name} Lv.{lv} [{cs.Label}]: 승률 {100f * wins / RunsPerCase:0}%  " +
                                      $"승리 시 남은 HP {(wins > 0 ? 100f * hpLeft / wins : 0f):0}%  " +
                                      $"평균 도달 웨이브 {wavesReached / RunsPerCase:0.00}/{stage.Waves.Length}  " +
                                      $"평균 {seconds / RunsPerCase:0}초  스티움 피해 비중 {100f * tankShare / RunsPerCase:0}%  멈춤 {stuck}");
                    }
                }
            }
            Debug.Log(sb.ToString());
            TestMapCheck();
        }

        const float TestMapSeconds = 60f;

        /// <summary>
        /// 테스트 맵 점검: 기본 5명으로 60초 진행. 더미가 쓰러지지 않는지, 아군이 피해를 받지 않는지,
        /// 전투가 끝나지 않는지(나가기로만 종료) 확인하고 캐릭터별 피해를 출력한다.
        /// </summary>
        [MenuItem("TextSlayers/테스트 맵 점검")]
        public static void TestMapCheck()
        {
            FieldStageDef stage = null;
            foreach (var s in FieldData.Stages) if (s.IsTest) stage = s;
            if (stage == null) { Debug.LogError("[TextSlayers] 테스트 맵 없음"); return; }

            var ids = new[] { "stium", "ern", "mel", "vendetta", "paracelsus" };
            var party = new PartySlot[ids.Length];
            for (int i = 0; i < ids.Length; i++) party[i] = FieldData.DefaultSlot(ids[i]);
            var b = new FieldBattle(stage, party, 1, 7, recordEvents: true);
            int steps = Mathf.CeilToInt(TestMapSeconds / FieldConst.Dt);
            for (int i = 0; i < steps; i++) b.Step();

            int deadDummies = 0, playerTaken = 0;
            foreach (var e in b.Enemies) if (!e.Alive) deadDummies++;
            foreach (var u in b.Players) playerTaken += u.DamageTaken;

            var sb = new StringBuilder($"[TextSlayers] 테스트 맵 점검 ({TestMapSeconds:0}초, Lv.1, 이벤트 {b.Events.Count}개)\n");
            sb.AppendLine($"상태 {b.Phase}  끝남 {b.IsOver}  더미 {b.Enemies.Count}명 중 쓰러짐 {deadDummies}  아군이 받은 피해 {playerTaken}");
            foreach (var u in b.Players)
                sb.AppendLine($"  {u.Name}: 피해 {u.DamageDealt} (초당 {u.DamageDealt / b.Time:0.0})  위치 ({u.Pos.x:0.0}, {u.Pos.y:0.0})");
            bool ok = !b.IsOver && deadDummies == 0 && playerTaken == 0;
            sb.AppendLine(ok ? "결과: 정상" : "결과: 이상 — 위 수치 확인");
            Debug.Log(sb.ToString());
        }

        static PartySlot Slot(string characterId, Strategy strategy)
        {
            var slot = FieldData.DefaultSlot(characterId);
            slot.Strategy = strategy;
            return slot;
        }
    }
}
