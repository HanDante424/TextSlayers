using UnityEngine;

namespace TextSlayers
{
    public enum GameScreen { Title, Lobby, StageSelect, Character, Battle, Result }

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
