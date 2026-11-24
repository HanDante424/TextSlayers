using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// IMGUI 화면 전부 (기획 v0.1 7장). 화면 짧은 변을 RefShort 로 보고 확대 · 축소한다.
    /// 키보드: 전투 1~4 행동, 타이틀 아무 키, 결과 Enter/Space.
    /// </summary>
    public static class GameView
    {
        // ── 레이아웃 [임시] ──
        const float RefShort = 720f;      // 짧은 변 기준 해상도
        const float Pad = 16f;
        const float ButtonH = 64f;
        const float BarH = 26f;
        const float MenuW = 420f;
        const float NarrowWidth = 900f;   // 이보다 좁으면 행동 버튼 2×2
        const int FontSize = 22;
        const int LogFontSize = 21;
        const int TitleFontSize = 56;

        static readonly Color Bg = new Color(0.07f, 0.07f, 0.09f);
        static readonly Color Panel = new Color(0.12f, 0.12f, 0.15f);
        static readonly Color BarBack = new Color(0.2f, 0.2f, 0.24f);
        static readonly Color HpColor = new Color(0.85f, 0.25f, 0.25f);
        static readonly Color MpColor = new Color(0.25f, 0.5f, 0.9f);
        static readonly Color GaugeColor = new Color(0.95f, 0.75f, 0.2f);
        static readonly Color ExpColor = new Color(0.4f, 0.8f, 0.4f);

        static GUIStyle label, center, title, logStyle, button, barText;
        static Texture2D white;
        static Vector2 logScroll;
        static int lastLogVersion = -1;
        static bool resetArmed;
        static KeyCode heldKey = KeyCode.None;
        static float vw, vh;

        public static void Draw(GameRoot g)
        {
            EnsureStyles();
            float scale = Mathf.Min(Screen.width, Screen.height) / RefShort;
            if (scale <= 0f) return;
            vw = Screen.width / scale;
            vh = Screen.height / scale;

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Fill(new Rect(0, 0, vw, vh), Bg);

            switch (g.Current)
            {
                case GameScreen.Title: DrawTitle(g); break;
                case GameScreen.Lobby: DrawLobby(g); break;
                case GameScreen.StageSelect: DrawStageSelect(g); break;
                case GameScreen.Character: DrawCharacter(g); break;
                case GameScreen.Battle: DrawBattle(g); break;
                case GameScreen.Result: DrawResult(g); break;
            }

            DrawToast(g);
            // 키 입력은 화면을 다 그린 뒤 처리한다. 먼저 처리하면 같은 이벤트 안에서 로그 줄 수가
            // 늘어나 GUILayout 의 Layout / 그리기 컨트롤 수가 어긋난다.
            HandleKeys(g);
            GUI.matrix = oldMatrix;
        }

        // ───────────────────────── 입력 ─────────────────────────

        static void HandleKeys(GameRoot g)
        {
            Event e = Event.current;
            if (e.type == EventType.KeyUp && e.keyCode == heldKey)
            {
                heldKey = KeyCode.None;
                return;
            }
            if (e.type != EventType.KeyDown || e.keyCode == KeyCode.None) return;
            if (e.keyCode == heldKey) { e.Use(); return; } // 누르고 있을 때 반복 입력 무시
            heldKey = e.keyCode;

            switch (g.Current)
            {
                case GameScreen.Title:
                    g.Go(GameScreen.Lobby);
                    e.Use();
                    break;

                case GameScreen.Battle:
                    int idx = KeyToIndex(e.keyCode);
                    var b = g.Battle;
                    if (b != null && idx >= 0 && idx < GameData.ErnSkills.Length && b.Phase == BattlePhase.PlayerTurn)
                    {
                        b.UseSkill(GameData.ErnSkills[idx]);
                        e.Use();
                    }
                    else if (b != null && b.IsOver && IsConfirm(e.keyCode))
                    {
                        g.FinishBattle();
                        e.Use();
                    }
                    break;

                case GameScreen.Result:
                    if (IsConfirm(e.keyCode)) { g.Go(GameScreen.Lobby); e.Use(); }
                    break;
            }
        }

        static int KeyToIndex(KeyCode k)
        {
            if (k >= KeyCode.Alpha1 && k <= KeyCode.Alpha9) return k - KeyCode.Alpha1;
            if (k >= KeyCode.Keypad1 && k <= KeyCode.Keypad9) return k - KeyCode.Keypad1;
            return -1;
        }

        static bool IsConfirm(KeyCode k) => k == KeyCode.Return || k == KeyCode.KeypadEnter || k == KeyCode.Space;

        // ───────────────────────── 화면들 ─────────────────────────

        static void DrawTitle(GameRoot g)
        {
            float y = vh * 0.3f;
            GUI.Label(new Rect(0, y, vw, 80), "TEXT SLAYERS", title);
            GUI.Label(new Rect(0, y + 90, vw, 40), "글자로 베어 넘기는 슬레이어", center);
            if (GUI.Button(MenuRect(vh * 0.62f), "시작", button)) g.Go(GameScreen.Lobby);
            GUI.Label(new Rect(0, vh * 0.62f + ButtonH + 12, vw, 30), "<color=#888888>아무 키나 누르세요</color>", center);
        }

        static void DrawLobby(GameRoot g)
        {
            var p = g.Progress;
            Header("로비");
            GUI.Label(new Rect(Pad, 80, vw - Pad * 2, 32),
                $"에른  Lv.{p.Level}    골드 {p.Gold}    메모리 {p.Memory}", center);
            DrawExpBar(new Rect((vw - MenuW) / 2, 118, MenuW, BarH), p);

            float y = 180;
            if (GUI.Button(MenuRect(y), "스테이지", button)) { resetArmed = false; g.Go(GameScreen.StageSelect); }
            y += ButtonH + 12;
            if (GUI.Button(MenuRect(y), "캐릭터 정보", button)) { resetArmed = false; g.Go(GameScreen.Character); }
            y += ButtonH + 12;
            if (GUI.Button(MenuRect(y), "저장", button)) { resetArmed = false; g.SaveNow(); }
            y += ButtonH + 12;
            string resetText = resetArmed ? "<color=#ff8f8f>정말 초기화? (한 번 더 누르기)</color>" : "기록 초기화";
            if (GUI.Button(MenuRect(y), resetText, button))
            {
                if (resetArmed) { g.ResetProgress(); resetArmed = false; }
                else resetArmed = true;
            }
        }

        static void DrawStageSelect(GameRoot g)
        {
            Header("스테이지");
            float y = 90;
            float w = Mathf.Min(vw - Pad * 2, 760f);
            float x = (vw - w) / 2;
            for (int i = 0; i < GameData.Stages.Length; i++)
            {
                var st = GameData.Stages[i];
                bool unlocked = g.IsStageUnlocked(i);
                string mark = g.Progress.IsCleared(st.Id) ? "  <color=#ffd76a>★ 클리어</color>" : "";
                string text = unlocked ? $"{i + 1}. {st.Name}{mark}" : $"{i + 1}. ???  (이전 스테이지 클리어 필요)";

                GUI.enabled = unlocked;
                if (GUI.Button(new Rect(x, y, w, ButtonH), text, button)) g.StartStage(st);
                GUI.enabled = true;

                if (unlocked)
                {
                    var names = new string[st.Waves.Length];
                    for (int k = 0; k < names.Length; k++) names[k] = GameData.Enemy(st.Waves[k]).Name;
                    GUI.Label(new Rect(x + 8, y + ButtonH + 4, w - 16, 30),
                        $"<color=#999999>{string.Join(" → ", names)}</color>", label);
                }
                y += ButtonH + 48;
            }
            BackButton(g);
        }

        static void DrawCharacter(GameRoot g)
        {
            var p = g.Progress;
            var s = GameData.ErnStatsAt(p.Level);
            Header("캐릭터 정보");

            float w = Mathf.Min(vw - Pad * 2, 760f);
            float x = (vw - w) / 2;
            float y = 84;
            GUI.Label(new Rect(x, y, w, 32), $"<b>에른</b>  Lv.{p.Level}  —  화염 검사", label);
            y += 40;
            DrawExpBar(new Rect(x, y, w, BarH), p);
            y += BarH + 16;

            string stats =
                $"HP {s.HP}    MP {s.MP}\n" +
                $"공격 {s.ATK}    방어 {s.DEF}    속도 {s.SPD:0.#}\n" +
                $"치명타 {s.CRIT * 100f:0}%    치명타 피해 {s.CRITDMG * 100f:0}%";
            GUI.Label(new Rect(x, y, w, 100), stats, label);
            y += 110;

            GUI.Label(new Rect(x, y, w, 32), "<b>행동</b>", label);
            y += 36;
            foreach (var sk in GameData.ErnSkills)
            {
                string mp = sk.MpCost > 0 ? $"MP {sk.MpCost}" : "MP 0";
                GUI.Label(new Rect(x, y, w, 30), $"[{sk.Key}] {sk.Name}  <color=#8fa8ff>{mp}</color>  <color=#999999>{sk.Desc}</color>", label);
                y += 32;
            }
            BackButton(g);
        }

        static void DrawBattle(GameRoot g)
        {
            var b = g.Battle;
            if (b == null) { g.Go(GameScreen.Lobby); return; }

            // 상단: 에른 / 적 상태
            float statusH = 2 * (BarH * 2 + 44) + Pad;
            Fill(new Rect(0, 0, vw, statusH + Pad), Panel);
            float y = Pad;
            DrawCombatant(new Rect(Pad, y, vw - Pad * 2, 0), b.Player, $"에른 Lv.{g.Progress.Level}", true);
            y += BarH * 2 + 44 + Pad;
            string boss = b.Enemy.Def.IsBoss ? " <color=#ffb347>[보스]</color>" : "";
            string charge = b.Enemy.Charging ? "  <color=#ffb347>강타 준비!</color>" : "";
            DrawCombatant(new Rect(Pad, y, vw - Pad * 2, 0), b.Enemy,
                $"{b.Enemy.Name}{boss}  <color=#999999>({b.WaveIndex + 1}/{b.WaveCount})</color>{charge}", false);

            // 하단: 행동
            bool narrow = vw < NarrowWidth;
            float actionsH = b.IsOver ? ButtonH : (narrow ? ButtonH * 2 + 8 : ButtonH);
            float actionsY = vh - Pad - actionsH;
            float hintY = actionsY - 36;

            // 로그
            float logY = statusH + Pad * 2;
            DrawLog(new Rect(Pad, logY, vw - Pad * 2, hintY - logY - 4), g.Log);

            GUI.Label(new Rect(Pad, hintY, vw - Pad * 2, 32), TurnHint(b), center);

            // 후퇴 — 로그를 그린 뒤에 둔다 (클릭 시 로그 줄이 늘어나므로, HandleKeys 주석 참고)
            if (!b.IsOver && GUI.Button(new Rect(vw - Pad - 110, Pad, 110, 40), "후퇴", button)) b.Retreat();

            if (b.IsOver)
            {
                if (GUI.Button(new Rect(Pad, actionsY, vw - Pad * 2, ButtonH), "결과 보기 (Enter)", button)) g.FinishBattle();
                return;
            }
            DrawActions(new Rect(Pad, actionsY, vw - Pad * 2, actionsH), b, narrow);
        }

        static void DrawResult(GameRoot g)
        {
            var r = g.LastResult;
            string head = r.Won ? "<color=#ffd76a>승리!</color>" : (r.Retreated ? "후퇴" : "<color=#ff8f8f>패배...</color>");
            GUI.Label(new Rect(0, vh * 0.15f, vw, 80), head, title);
            GUI.Label(new Rect(0, vh * 0.15f + 80, vw, 36), r.StageName, center);

            string body = $"경험치 +{r.Exp}\n골드 +{r.Gold}";
            if (r.Memory > 0) body += $"\n메모리 +{r.Memory}";
            if (r.LevelAfter > r.LevelBefore) body += $"\n<color=#8fd3ff>레벨 업! Lv.{r.LevelBefore} → Lv.{r.LevelAfter}</color>";
            if (!r.Won) body += $"\n<color=#999999>(보상 {GameData.DefeatRewardRate * 100f:0}%만 획득)</color>";
            if (!r.Saved) body += "\n<color=#ff8f8f>저장 실패</color>";
            GUI.Label(new Rect(0, vh * 0.15f + 140, vw, 220), body, center);

            if (GUI.Button(MenuRect(vh - Pad - ButtonH - 40), "로비로 (Enter)", button)) g.Go(GameScreen.Lobby);
        }

        // ───────────────────────── 부품 ─────────────────────────

        static void DrawCombatant(Rect area, Combatant c, string nameText, bool showMp)
        {
            float x = area.x, w = area.width, y = area.y;
            GUI.Label(new Rect(x, y, w, 32), nameText, label);
            y += 36;
            float half = (w - Pad) / 2;
            DrawBar(new Rect(x, y, half, BarH), c.HpRatio, HpColor, $"HP {c.Hp}/{c.Stats.HP}");
            if (showMp) DrawBar(new Rect(x + half + Pad, y, half, BarH), c.MpRatio, MpColor, $"MP {c.Mp}/{c.Stats.MP}");
            y += BarH + 8;
            string guard = c.Guarding ? "  [방어]" : "";
            DrawBar(new Rect(x, y, w, BarH), c.GaugeRatio, GaugeColor, $"행동 게이지{guard}");
        }

        static void DrawActions(Rect area, BattleSystem b, bool narrow)
        {
            var skills = GameData.ErnSkills;
            int cols = narrow ? 2 : skills.Length;
            int rows = Mathf.CeilToInt(skills.Length / (float)cols);
            float gap = 8f;
            float bw = (area.width - gap * (cols - 1)) / cols;
            float bh = (area.height - gap * (rows - 1)) / rows;

            for (int i = 0; i < skills.Length; i++)
            {
                var sk = skills[i];
                int cx = i % cols, cy = i / cols;
                var r = new Rect(area.x + cx * (bw + gap), area.y + cy * (bh + gap), bw, bh);
                string mp = sk.MpCost > 0 ? $"  <size=16>MP {sk.MpCost}</size>" : "";
                GUI.enabled = b.CanUse(sk);
                if (GUI.Button(r, $"[{sk.Key}] {sk.Name}{mp}", button)) b.UseSkill(sk);
                GUI.enabled = true;
            }
        }

        static string TurnHint(BattleSystem b) => b.Phase switch
        {
            BattlePhase.PlayerTurn => "<color=#8fd3ff><b>에른의 차례</b> — 행동을 고르세요 (1~4)</color>",
            BattlePhase.WaveClear => "<color=#999999>다음 적을 기다리는 중...</color>",
            BattlePhase.Won => "<color=#ffd76a>스테이지 클리어</color>",
            BattlePhase.Lost => "<color=#ff8f8f>전투 종료</color>",
            _ => "<color=#999999>게이지가 차는 중...</color>",
        };

        static void DrawLog(Rect area, TextLog log)
        {
            Fill(area, Panel);
            if (log.Version != lastLogVersion)
            {
                lastLogVersion = log.Version;
                logScroll.y = float.MaxValue; // 새 줄이 오면 맨 아래로
            }
            GUILayout.BeginArea(new Rect(area.x + 8, area.y + 6, area.width - 16, area.height - 12));
            logScroll = GUILayout.BeginScrollView(logScroll);
            var lines = log.Lines;
            for (int i = 0; i < lines.Count; i++) GUILayout.Label(lines[i], logStyle);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static void DrawExpBar(Rect r, PlayerProgress p)
        {
            bool max = p.Level >= GameData.MaxLevel;
            int need = GameData.ExpToNext(p.Level);
            DrawBar(r, max ? 1f : (float)p.Exp / need, ExpColor, max ? "EXP MAX" : $"EXP {p.Exp}/{need}");
        }

        static void DrawBar(Rect r, float ratio, Color color, string text)
        {
            Fill(r, BarBack);
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(ratio), r.height), color);
            GUI.Label(r, text, barText);
        }

        static void Header(string text)
        {
            Fill(new Rect(0, 0, vw, 64), Panel);
            GUI.Label(new Rect(0, 12, vw, 40), $"<b>{text}</b>", center);
        }

        static void BackButton(GameRoot g)
        {
            if (GUI.Button(MenuRect(vh - Pad - ButtonH), "뒤로", button)) g.Go(GameScreen.Lobby);
        }

        static void DrawToast(GameRoot g)
        {
            if (string.IsNullOrEmpty(g.Toast)) return;
            var r = new Rect((vw - MenuW) / 2, vh * 0.8f, MenuW, 48);
            Fill(r, new Color(0, 0, 0, 0.8f));
            GUI.Label(r, g.Toast, center);
        }

        static Rect MenuRect(float y) => new Rect((vw - MenuW) / 2, y, MenuW, ButtonH);

        static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        static void EnsureStyles()
        {
            if (label != null) return;

            white = Texture2D.whiteTexture;

            label = new GUIStyle(GUI.skin.label) { fontSize = FontSize, richText = true, wordWrap = true };
            label.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            title = new GUIStyle(center) { fontSize = TitleFontSize, fontStyle = FontStyle.Bold };

            logStyle = new GUIStyle(label) { fontSize = LogFontSize, margin = new RectOffset(0, 0, 2, 2) };

            barText = new GUIStyle(center) { fontSize = 16, wordWrap = false };
            barText.normal.textColor = Color.white;

            button = new GUIStyle(GUI.skin.button) { fontSize = FontSize, richText = true, wordWrap = true };
        }
    }
}
