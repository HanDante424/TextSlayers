using UnityEngine;

namespace TextSlayers
{
    public enum GameScreen { Title, Lobby, StageSelect, Character, Battle, Result, FieldStageSelect, PartySelect, FieldBattle, Loadout, EnemySetup }

    public struct ResultInfo
    {
        public string StageName;
        public bool Won;
        public bool Retreated;
        public int Exp, Gold, Memory;
        public int LevelBefore, LevelAfter;
        public bool Saved;
    }

    /// <summary>
    /// 게임 전체 상태 · 화면 전환. 씬에 없으면 첫 씬 로드 뒤 자동 생성된다 (씬 편집 없이 실행 가능).
    /// 화면 그리기는 GameView (IMGUI).
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        const float ToastSeconds = 2f;
        static readonly Color BackgroundColor = new Color(0.07f, 0.07f, 0.09f);

        public static GameRoot Instance { get; private set; }

        public GameScreen Current { get; private set; } = GameScreen.Title;
        public PlayerProgress Progress { get; private set; }
        public TextLog Log { get; } = new TextLog();
        public BattleSystem Battle { get; private set; }

        // ── v0.2 필드 전투 ──
        static readonly float[] FieldSpeeds = { 1f, 2f, 4f };
        public FieldBattle Field { get; private set; }
        public FieldStageDef PendingFieldStage { get; private set; }
        /// <summary>출전 선택 (선택한 순서 = 편성 순서). 세션 동안 유지.</summary>
        public readonly System.Collections.Generic.List<string> SelectedParty =
            new System.Collections.Generic.List<string> { "stium", "ern", "mel", "vendetta", "paracelsus" };
        int fieldSpeedIndex;
        public float FieldSpeed => FieldSpeeds[fieldSpeedIndex];
        /// <summary>필드 전투 일시정지 (디버그). 멈춘 동안 이펙트도 멈춘다.</summary>
        public bool FieldPaused { get; private set; }
        public ResultInfo LastResult { get; private set; }

        public string Toast { get; private set; }
        float toastTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<GameRoot>() != null) return;

            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = BackgroundColor;
                cam.orthographic = true;
            }
            new GameObject("GameRoot").AddComponent<GameRoot>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Progress = SaveSystem.Load();
        }

        void Update()
        {
            if (Current == GameScreen.Battle && Battle != null) Battle.Tick(Time.deltaTime);
            if (Current == GameScreen.FieldBattle && Field != null)
            {
                if (!FieldPaused) Field.Update(Time.deltaTime, FieldSpeed);
                DrainFieldEvents();
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) Toast = null;
            }
        }

        void OnGUI() => GameView.Draw(this);

        public void Go(GameScreen screen) => Current = screen;

        public bool IsStageUnlocked(int index)
            => index <= 0 || Progress.IsCleared(GameData.Stages[index - 1].Id);

        public void StartStage(StageDef stage)
        {
            Log.Clear();
            Battle = new BattleSystem(stage, Progress.Level, Log, System.Environment.TickCount);
            Current = GameScreen.Battle;
        }

        /// <summary>전투가 끝난 뒤 보상 적용 · 저장 · 결과 화면.</summary>
        public void FinishBattle()
        {
            if (Battle == null || !Battle.IsOver) return;

            Battle.GetFinalRewards(out int exp, out int gold, out int memory);
            int before = Progress.Level;
            Progress.AddExp(exp);
            Progress.Gold += gold;
            Progress.Memory += memory;

            bool won = Battle.Phase == BattlePhase.Won;
            if (won && !Progress.IsCleared(Battle.Stage.Id)) Progress.ClearedStages.Add(Battle.Stage.Id);

            LastResult = new ResultInfo
            {
                StageName = Battle.Stage.Name,
                Won = won,
                Retreated = Battle.Retreated,
                Exp = exp, Gold = gold, Memory = memory,
                LevelBefore = before, LevelAfter = Progress.Level,
                Saved = SaveSystem.Save(Progress),
            };
            Battle = null;
            Current = GameScreen.Result;
        }

        // ───────────── v0.2 필드 전투 ─────────────

        /// <summary>로비에서 디버그 맵(테스트 맵)으로 바로 간다 → 출전 선택.</summary>
        public void EnterDebugMap()
        {
            foreach (var st in FieldData.Stages)
                if (st.IsTest) { ChooseFieldStage(st); return; }
            ShowToast("디버그 맵이 없습니다.");
        }

        /// <summary>디버그 맵 나가기: 보상 · 결과 화면 없이 로비로.</summary>
        public void ExitDebugMap()
        {
            Field = null;
            FieldPaused = false;
            Log.Clear();
            Current = GameScreen.Lobby;
        }

        public void ToggleFieldPause() => FieldPaused = !FieldPaused;

        public void ChooseFieldStage(FieldStageDef stage)
        {
            PendingFieldStage = stage;
            Current = GameScreen.PartySelect;
        }

        /// <summary>출전 토글. 선택 순서가 편성 순서. 최대 FieldConst.MaxPartySize.</summary>
        public void ToggleParty(string characterId)
        {
            if (SelectedParty.Remove(characterId)) return;
            if (!FieldData.Character(characterId).Enabled) return;   // 비활성 캐릭터는 출전 불가
            if (SelectedParty.Count >= FieldConst.MaxPartySize)
            {
                ShowToast($"최대 {FieldConst.MaxPartySize}명까지 선택할 수 있습니다.");
                return;
            }
            SelectedParty.Add(characterId);
        }

        // ── 출전 구성 (전술 · 패시브 1 · 액티브 3), 세션 동안 캐릭터별로 유지 ──
        readonly System.Collections.Generic.Dictionary<string, PartySlot> loadouts =
            new System.Collections.Generic.Dictionary<string, PartySlot>();

        public PartySlot Loadout(string characterId)
        {
            if (!loadouts.TryGetValue(characterId, out var slot))
            {
                slot = FieldData.DefaultSlot(characterId);
                if (System.Array.IndexOf(FieldData.PlayerStrategies, slot.Strategy) < 0) slot.Strategy = Strategy.Charge;
                loadouts[characterId] = slot;
            }
            return slot;
        }

        /// <summary>캐릭터 선택 → 전술 · 스킬 선택 화면.</summary>
        public void GoLoadout()
        {
            if (SelectedParty.Count == 0) return;
            Current = GameScreen.Loadout;
        }

        /// <summary>액티브 장착 토글 (최대 3).</summary>
        public void ToggleActive(string characterId, string skillId)
        {
            var slot = Loadout(characterId);
            var list = new System.Collections.Generic.List<string>(slot.Skills);
            if (list.Remove(skillId)) { slot.Skills = list.ToArray(); return; }
            if (list.Count >= FieldData.MaxEquipActives)
            {
                ShowToast($"액티브는 {FieldData.MaxEquipActives}개까지 장착합니다.");
                return;
            }
            // 캐릭터 스킬 목록 순서를 유지한다
            list.Add(skillId);
            var order = FieldData.Character(characterId).Skills;
            list.Sort((a, b) => System.Array.IndexOf(order, a).CompareTo(System.Array.IndexOf(order, b)));
            slot.Skills = list.ToArray();
        }

        /// <summary>모든 출전 캐릭터가 액티브 3개를 골랐는가 (글러트니는 고정이라 예외).</summary>
        /// <summary>디버그 맵 적 구성 (세션 동안 유지). 기본: 더미 1 돌진 × 4, 더미 2 도주 · 견제 × 3.</summary>
        public EnemySetup DebugEnemies { get; } = MakeDefaultEnemies();

        static EnemySetup MakeDefaultEnemies()
        {
            var e = new EnemySetup();
            e.Add("test_dummy_melee", Strategy.Charge, 4);
            e.Add("test_dummy_ranged", Strategy.Kite, 3);
            return e;
        }

        /// <summary>전술 · 스킬 선택 다음: 디버그 맵이면 적 전술 창, 아니면 바로 출전.</summary>
        public void NextFromLoadout()
        {
            if (!LoadoutReady(out string problem)) { ShowToast(problem); return; }
            if (PendingFieldStage != null && PendingFieldStage.IsTest) Current = GameScreen.EnemySetup;
            else StartFieldStage();
        }

        /// <summary>더미 1명 추가 (종류 기본 전술: 근거리 돌진 · 원거리 도주 · 견제).</summary>
        public void AddDebugEnemy(string id)
        {
            if (DebugEnemies.Total >= FieldBattle.MaxDebugEnemies)
            {
                ShowToast($"적은 최대 {FieldBattle.MaxDebugEnemies}명까지 배치할 수 있습니다.");
                return;
            }
            DebugEnemies.Add(id, FieldData.Enemy(id).Strategy);
        }

        public void ResetDebugEnemies()
        {
            DebugEnemies.Entries.Clear();
            DebugEnemies.Add("test_dummy_melee", Strategy.Charge, 4);
            DebugEnemies.Add("test_dummy_ranged", Strategy.Kite, 3);
        }

        public bool LoadoutReady(out string problem)
        {
            foreach (var id in SelectedParty)
            {
                var slot = Loadout(id);
                if (slot.Passive == ConceptCharacters.GluttonPassive) continue;
                int need = Mathf.Min(FieldData.MaxEquipActives, slot.Def.Skills.Length);   // 스킬 미정 캐릭터는 0개
                if (slot.Skills.Length != need)
                {
                    problem = $"{slot.Def.Name}: 액티브 {need}개를 골라 주세요 ({slot.Skills.Length}/{need})";
                    return false;
                }
            }
            problem = null;
            return true;
        }

        public void StartFieldStage()
        {
            if (PendingFieldStage == null || SelectedParty.Count == 0) return;
            if (!LoadoutReady(out string problem)) { ShowToast(problem); return; }
            var party = new PartySlot[SelectedParty.Count];
            for (int i = 0; i < party.Length; i++)
            {
                var l = Loadout(SelectedParty[i]);
                party[i] = new PartySlot { Def = l.Def, Strategy = l.Strategy, Skills = (string[])l.Skills.Clone(), Passive = l.Passive };
            }
            Log.Clear();
            var enemies = PendingFieldStage.IsTest ? DebugEnemies : null;
            Field = new FieldBattle(PendingFieldStage, party, Progress.Level, System.Environment.TickCount, recordEvents: true, enemies: enemies);
            FieldPaused = false;
            DrainFieldEvents();
            Current = GameScreen.FieldBattle;
        }

        public void CycleFieldSpeed() => fieldSpeedIndex = (fieldSpeedIndex + 1) % FieldSpeeds.Length;

        /// <summary>전투 이벤트를 로그로 옮긴다. 기본 공격 피해는 너무 많아 로그에 남기지 않는다 (화면의 피해 집계로 본다).</summary>
        void DrainFieldEvents()
        {
            var events = Field.Events;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                string who = e.Source != null ? e.Source.Name : "";
                LogKind kind = e.Source == null ? LogKind.System : (e.Source.IsPlayer ? LogKind.Player : LogKind.Enemy);
                switch (e.Kind)
                {
                    case FieldEventKind.Hit:
                        if (e.Quiet || e.Text == "공격") break;
                        Log.Add($"{who} 「{e.Text}」 → {e.Target.Name} {e.Amount}{(e.Crit ? " 치명타!" : "")}", kind);
                        break;
                    case FieldEventKind.Heal:
                        Log.Add($"{who} 「{e.Text}」 → {e.Target.Name} +{e.Amount}", kind);
                        break;
                    case FieldEventKind.Skill:
                        Log.Add($"{who} 「{e.Text}」", kind);
                        break;
                    case FieldEventKind.Death:
                        Log.Add(e.Text, LogKind.Warning);
                        break;
                    default:
                        Log.Add(e.Text, LogKind.System);
                        break;
                }
            }
            events.Clear();
        }

        /// <summary>필드 전투 종료 (나가기 포함) → 보상 · 저장 · 결과 화면.</summary>
        public void FinishFieldBattle()
        {
            if (Field == null) return;
            if (!Field.IsOver) Field.Retreat();

            Field.GetFinalRewards(out int exp, out int gold, out int memory);
            int before = Progress.Level;
            Progress.AddExp(exp);
            Progress.Gold += gold;
            Progress.Memory += memory;

            bool won = Field.Phase == FieldPhase.Won;
            if (won && !Progress.IsCleared(Field.Stage.Id)) Progress.ClearedStages.Add(Field.Stage.Id);

            LastResult = new ResultInfo
            {
                StageName = Field.Stage.Name,
                Won = won,
                Retreated = Field.Retreated,
                Exp = exp, Gold = gold, Memory = memory,
                LevelBefore = before, LevelAfter = Progress.Level,
                Saved = SaveSystem.Save(Progress),
            };
            Field = null;
            Current = GameScreen.Result;
        }

        public void SaveNow() => ShowToast(SaveSystem.Save(Progress) ? "저장했습니다." : "저장에 실패했습니다.");

        public void ResetProgress()
        {
            SaveSystem.Delete();
            Progress = new PlayerProgress();
            ShowToast("기록을 초기화했습니다.");
        }

        public void ShowToast(string text)
        {
            Toast = text;
            toastTimer = ToastSeconds;
        }
    }
}
