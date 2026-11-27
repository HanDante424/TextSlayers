using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// IMGUI 화면 전부 (기획 v0.1 7장). 화면 짧은 변을 RefShort 로 보고 확대 · 축소한다.
    /// 키보드: 전투 1~4 행동, 타이틀 아무 키, 결과 Enter/Space.
    /// </summary>
    public static partial class GameView
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

        static GUIStyle label, center, title, logStyle, button, barText, small, smallButton;
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
                case GameScreen.FieldStageSelect: DrawFieldStageSelect(g); break;
                case GameScreen.PartySelect: DrawPartySelect(g); break;
                case GameScreen.FieldBattle: DrawFieldBattle(g); break;
                case GameScreen.Loadout: DrawLoadout(g); break;
                case GameScreen.EnemySetup: DrawEnemySetup(g); break;
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

                case GameScreen.FieldBattle:
                    if (g.Field != null && g.Field.IsOver && IsConfirm(e.keyCode)) { g.FinishFieldBattle(); e.Use(); }
                    else if (g.Field != null && g.Field.Stage.IsTest && DebugKey(g, e.keyCode)) e.Use();
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
            if (GUI.Button(MenuRect(y), "필드 전투 <size=16>(v0.2 테스트)</size>", button)) { resetArmed = false; g.Go(GameScreen.FieldStageSelect); }
            y += ButtonH + 12;
            if (GUI.Button(MenuRect(y), "<color=#ffd76a>디버그 맵</color>", button)) { resetArmed = false; g.EnterDebugMap(); }
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

        // ───────────────────────── v0.2 필드 전투 ─────────────────────────
        const float CardH = 72f;
        const float CardMinW = 290f;
        const float SideW = 380f;
        const float FieldLogH = 190f;
        const float TopBarH = 52f;

        static readonly Color FieldBg = new Color(0.10f, 0.13f, 0.11f);
        static readonly Color FieldLine = new Color(0.16f, 0.20f, 0.17f);
        static readonly Color PlayerColor = new Color(0.30f, 0.55f, 0.95f);
        static readonly Color EnemyColor = new Color(0.85f, 0.35f, 0.30f);
        static readonly Color ImmortalColor = new Color(0.95f, 0.80f, 0.30f);
        static readonly Color GuardColor = new Color(0.6f, 0.85f, 1f);
        static readonly Color StunColor = new Color(1f, 0.9f, 0.3f);
        static readonly Color MarkColor = new Color(1f, 0.3f, 0.3f);

        static Vector2 partyScroll;

        static void DrawFieldStageSelect(GameRoot g)
        {
            Header("필드 전투 (v0.2)");
            float y = 90;
            float w = Mathf.Min(vw - Pad * 2, 760f);
            float x = (vw - w) / 2;
            foreach (var st in FieldData.Stages)
            {
                string tag = st.IsTest ? "  <color=#ffd76a>[테스트]</color>" : "";
                if (GUI.Button(new Rect(x, y, w, ButtonH), $"{st.Name}{tag}", button)) g.ChooseFieldStage(st);
                GUI.Label(new Rect(x + 8, y + ButtonH + 4, w - 16, 30), $"<color=#999999>{WaveSummary(st)}</color>", label);
                y += ButtonH + 48;
            }
            BackButton(g);
        }

        /// <summary>웨이브 구성 요약 (예: "테스트 더미 1 ×4, 테스트 더미 2 ×3").</summary>
        static string WaveSummary(FieldStageDef st)
        {
            var waves = new string[st.Waves.Length];
            for (int i = 0; i < st.Waves.Length; i++)
            {
                var seen = new System.Collections.Generic.List<string>();
                var counts = new System.Collections.Generic.List<string>();
                foreach (var id in st.Waves[i])
                {
                    if (seen.Contains(id)) continue;
                    seen.Add(id);
                    int n = 0;
                    foreach (var other in st.Waves[i]) if (other == id) n++;
                    counts.Add($"{GameData.Enemy(id).Name} ×{n}");
                }
                waves[i] = string.Join(", ", counts);
            }
            return string.Join("  →  ", waves);
        }

        static void DrawPartySelect(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null) { g.Go(GameScreen.FieldStageSelect); return; }
            var sel = g.SelectedParty;
            Header($"출전 선택  {sel.Count}/{FieldConst.MaxPartySize}  —  {stage.Name}");
            int activeTotal = 0;
            foreach (var c in FieldData.Roster) if (c.Enabled) activeTotal++;

            // 선택 순서 = 편성 순서
            var names = new string[sel.Count];
            for (int i = 0; i < sel.Count; i++) names[i] = $"{i + 1}. {FieldData.Character(sel[i]).Name}";
            string order = sel.Count > 0 ? string.Join("   ", names) : $"<color=#999999>캐릭터를 눌러 선택하세요 (출전 가능 {activeTotal}명)</color>";
            GUI.Label(new Rect(Pad, 70, vw - Pad * 2, 32), order, center);

            // 캐릭터 목록 (스크롤, GUILayout 미사용)
            float top = 110f;
            float bottom = vh - Pad - ButtonH - Pad;
            var view = new Rect(Pad, top, vw - Pad * 2, bottom - top);
            float innerW = view.width - 20f;
            int cols = Mathf.Max(2, Mathf.FloorToInt(innerW / CardMinW));
            float gap = 8f;
            float cw = (innerW - gap * (cols - 1)) / cols;
            // 출전 가능(스킬 컨셉 있음) 캐릭터를 앞에, 비활성은 뒤에 흐리게 (사용자 2026-11-26)
            var roster = new System.Collections.Generic.List<CharacterDef>();
            foreach (var c in FieldData.Roster) if (c.Enabled) roster.Add(c);
            foreach (var c in FieldData.Roster) if (!c.Enabled) roster.Add(c);
            int rows = Mathf.CeilToInt(roster.Count / (float)cols);
            var content = new Rect(0, 0, innerW, rows * (CardH + gap));

            Fill(view, Panel);
            partyScroll = GUI.BeginScrollView(view, partyScroll, content);
            for (int i = 0; i < roster.Count; i++)
            {
                var c = roster[i];
                int idx = sel.IndexOf(c.Id);
                var r = new Rect((i % cols) * (cw + gap), (i / cols) * (CardH + gap), cw, CardH);
                if (idx >= 0) Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), PlayerColor);
                string mark = idx >= 0 ? $"<color=#8fd3ff>[{idx + 1}]</color> " : "";
                string info = c.Enabled ? $"{c.Kind} · {FieldData.RoleName(c.BaseRole)} · {FieldData.StrategyName(c.DefaultStrategy)}" : "비활성 (스킬 컨셉 없음)";
                string text = $"{mark}<b>{c.Name}</b>\n<size=15><color=#aaaaaa>{info}</color></size>";
                GUI.enabled = c.Enabled;
                if (GUI.Button(r, text, button)) g.ToggleParty(c.Id);
                GUI.enabled = true;
            }
            GUI.EndScrollView();

            float by = vh - Pad - ButtonH;
            float bw = (vw - Pad * 3) / 2;
            if (GUI.Button(new Rect(Pad, by, bw, ButtonH), "뒤로", button)) g.Go(GameScreen.FieldStageSelect);
            GUI.enabled = sel.Count > 0;
            if (GUI.Button(new Rect(Pad * 2 + bw, by, bw, ButtonH), $"다음: 전술 · 스킬 ({sel.Count}명)", button)) g.GoLoadout();
            GUI.enabled = true;
        }

        static void DrawFieldBattle(GameRoot g)
        {
            var b = g.Field;
            if (b == null) { g.Go(GameScreen.Lobby); return; }

            // 상단 바
            Fill(new Rect(0, 0, vw, TopBarH), Panel);
            string wave = b.WaveCount > 1 ? $"  <color=#999999>웨이브 {b.WaveIndex + 1}/{b.WaveCount}</color>" : "";
            string altar = b.HasAltar ? (b.EyeOpen ? "   <color=#ff6b6b>악신의 눈</color>" : $"   <color=#cc8888>재단 {b.Altar:0}/50</color>") : "";
            GUI.Label(new Rect(Pad, 10, vw * 0.6f, 32), $"<b>{b.Stage.Name}</b>{wave}   <color=#aaaaaa>{b.Time:0.0}초</color>{altar}", label);

            bool debug = b.Stage.IsTest;
            if (debug && dbgTool != DebugTool.None)
                GUI.Label(new Rect(vw * 0.35f, 10, vw * 0.3f, 32), $"<color=#ffd76a>{ToolHint(dbgTool)}</color>", center);
            if (debug && g.FieldPaused)
                GUI.Label(new Rect(vw * 0.35f, 30, vw * 0.3f, 24), "<color=#ff8f8f><size=16>일시정지</size></color>", center);

            // 필드
            float areaW = vw - Pad * 3 - SideW;
            float areaH = vh - TopBarH - FieldLogH - Pad * 3 - (debug ? DebugBarH + Pad : 0f);
            float unit = Mathf.Min(areaW / FieldConst.Width, areaH / FieldConst.Height);
            var field = new Rect(Pad, TopBarH + Pad, FieldConst.Width * unit, FieldConst.Height * unit);
            DrawField(field, unit, b);

            // 오른쪽: 아군 피해 집계
            var side = new Rect(vw - Pad - SideW, TopBarH + Pad, SideW, areaH);
            DrawPartyPanel(side, b);

            // 아래: (디버그 막대) + 로그. 버튼 · 클릭은 로그를 그린 뒤 (HandleKeys 주석 참고)
            float logY = TopBarH + Pad * 2 + areaH;
            float barY = logY;
            if (debug) logY += DebugBarH + Pad;
            DrawLog(new Rect(Pad, logY, vw - Pad * 2, vh - logY - Pad), g.Log);

            if (debug)
            {
                DrawDebugBar(new Rect(Pad, barY, vw - Pad * 2, DebugBarH), g, b);
                HandleFieldClick(field, unit, b);
                if (GUI.Button(new Rect(vw - Pad - 120, 6, 120, 40), "나가기", button)) { dbgTool = DebugTool.None; g.ExitDebugMap(); return; }
            }
            else if (GUI.Button(new Rect(vw - Pad - 120, 6, 120, 40), b.IsOver ? "결과 보기" : "나가기", button)) { g.FinishFieldBattle(); return; }
            if (!b.IsOver && GUI.Button(new Rect(vw - Pad * 2 - 240, 6, 120, 40), $"속도 ×{g.FieldSpeed:0}", button)) g.CycleFieldSpeed();
            if (GUI.Button(new Rect(vw - Pad * 3 - 400, 6, 150, 40), $"<size=17>{FxScaleLabel}</size>", button)) CycleFxScale();
        }

        // ───────────────────────── 디버그 맵 도구 (사용자 2026-11-26) ─────────────────────────
        // 배치 · 제거 도구를 고른 뒤 필드를 클릭한다. 단축키: 1 더미 1 · 2 더미 2 · 3 제거 · Esc 도구 해제 · P 일시정지
        //                                              A 더미 AI · C 쿨타임 초기화 · R 집계 초기화
        enum DebugTool { None, PlaceMelee, PlaceRanged, Remove, Strategy }
        const float DebugBarH = 44f;
        const float RemovePickDist = 1.0f;      // 제거 도구: 클릭 지점에서 이 거리 안의 가장 가까운 적
        const string DummyMelee = "test_dummy_melee";
        const string DummyRanged = "test_dummy_ranged";
        static DebugTool dbgTool;

        static string ToolHint(DebugTool t) => t switch
        {
            DebugTool.PlaceMelee => "필드를 클릭해 더미 1 (근거리) 배치",
            DebugTool.PlaceRanged => "필드를 클릭해 더미 2 (원거리) 배치",
            DebugTool.Remove => "제거할 적을 클릭",
            DebugTool.Strategy => "전술을 바꿀 적을 클릭 (돌진 → 후방 침입 → 도주 · 견제 → 후방 우선)",
            _ => "",
        };

        static void SelectTool(DebugTool t) => dbgTool = dbgTool == t ? DebugTool.None : t;

        static void DrawDebugBar(Rect r, GameRoot g, FieldBattle b)
        {
            Fill(r, Panel);
            string[] labels =
            {
                ToolLabel("더미 1 배치", DebugTool.PlaceMelee),
                ToolLabel("더미 2 배치", DebugTool.PlaceRanged),
                ToolLabel("제거", DebugTool.Remove),
                ToolLabel("전술 변경", DebugTool.Strategy),
                "전부 제거",
                "기본 배치",
                b.EnemyAI ? "적 AI <color=#8fff9f>켜짐</color>" : "적 AI <color=#ff8f8f>꺼짐</color>",
                g.FieldPaused ? "<color=#ff8f8f>재개</color>" : "일시정지",
                "쿨타임 초기화",
                "집계 초기화",
            };
            float gap = 6f;
            float bw = (r.width - 12f - gap * (labels.Length - 1)) / labels.Length;
            for (int i = 0; i < labels.Length; i++)
            {
                var br = new Rect(r.x + 6f + i * (bw + gap), r.y + 4f, bw, r.height - 8f);
                if (!GUI.Button(br, labels[i], smallButton)) continue;
                switch (i)
                {
                    case 0: SelectTool(DebugTool.PlaceMelee); break;
                    case 1: SelectTool(DebugTool.PlaceRanged); break;
                    case 2: SelectTool(DebugTool.Remove); break;
                    case 3: SelectTool(DebugTool.Strategy); break;
                    case 4: b.DebugClearEnemies(); break;
                    case 5: b.DebugResetEnemies(); break;
                    case 6: b.EnemyAI = !b.EnemyAI; break;
                    case 7: g.ToggleFieldPause(); break;
                    case 8: b.DebugResetCooldowns(); break;
                    case 9: b.DebugResetMeters(); break;
                }
            }
        }

        static string ToolLabel(string text, DebugTool t) => dbgTool == t ? $"<color=#ffd76a>[{text}]</color>" : text;

        static void HandleFieldClick(Rect field, float unit, FieldBattle b)
        {
            Event e = Event.current;
            if (dbgTool == DebugTool.None || e.type != EventType.MouseDown || !field.Contains(e.mousePosition)) return;
            if (e.button == 1) { dbgTool = DebugTool.None; e.Use(); return; } // 오른쪽 클릭: 도구 해제

            var pos = new Vector2((e.mousePosition.x - field.x) / unit, (e.mousePosition.y - field.y) / unit);
            switch (dbgTool)
            {
                case DebugTool.PlaceMelee:
                case DebugTool.PlaceRanged:
                    if (!b.DebugSpawn(dbgTool == DebugTool.PlaceMelee ? DummyMelee : DummyRanged, pos))
                        GameRoot.Instance?.ShowToast($"적은 최대 {FieldBattle.MaxDebugEnemies}명까지 배치할 수 있습니다.");
                    break;
                case DebugTool.Remove:
                    b.DebugRemoveAt(pos, RemovePickDist);
                    break;
                case DebugTool.Strategy:
                    b.DebugCycleStrategyAt(pos, RemovePickDist);
                    break;
            }
            e.Use();
        }

        static bool DebugKey(GameRoot g, KeyCode k)
        {
            var b = g.Field;
            switch (k)
            {
                case KeyCode.Alpha1: case KeyCode.Keypad1: SelectTool(DebugTool.PlaceMelee); return true;
                case KeyCode.Alpha2: case KeyCode.Keypad2: SelectTool(DebugTool.PlaceRanged); return true;
                case KeyCode.Alpha3: case KeyCode.Keypad3: case KeyCode.Delete: SelectTool(DebugTool.Remove); return true;
                case KeyCode.Alpha4: case KeyCode.Keypad4: SelectTool(DebugTool.Strategy); return true;
                case KeyCode.Escape: dbgTool = DebugTool.None; return true;
                case KeyCode.P: g.ToggleFieldPause(); return true;
                case KeyCode.A: b.EnemyAI = !b.EnemyAI; return true;
                case KeyCode.C: b.DebugResetCooldowns(); return true;
                case KeyCode.R: b.DebugResetMeters(); return true;
                case KeyCode.E: b.DebugOpenEye(); return true;      // 악신의 눈 바로 열기
                case KeyCode.F: b.DebugIgnite(); return true;       // 점화 100 (화염의 팔)
                case KeyCode.H: b.DebugHurtAllies(); return true;   // 아군 체력 25% (옥염 · 저체력 조건)
            }
            return false;
        }

        const float ShakeTime = 0.2f, ShakePixels = 5f;
        const float GhostStep = 0.025f;      // 넉백 잔상 간격(초)

        static void DrawField(Rect r, float unit, FieldBattle b)
        {
            // 큰 넉백: 화면(필드)만 살짝 흔든다. 클릭 판정 좌표는 바꾸지 않는다.
            float since = b.Time - b.LastHeavyKnockTime;
            if (since >= 0f && since < ShakeTime)
            {
                float k = 1f - since / ShakeTime;
                r.x += Mathf.Sin(b.Time * 110f) * ShakePixels * k;
                r.y += Mathf.Cos(b.Time * 87f) * ShakePixels * 0.6f * k;
            }
            Fill(r, FieldBg);
            for (int i = 1; i < (int)FieldConst.Width; i++)
                Fill(new Rect(r.x + i * unit, r.y, 1, r.height), FieldLine);
            for (int i = 1; i < (int)FieldConst.Height; i++)
                Fill(new Rect(r.x, r.y + i * unit, r.width, 1), FieldLine);

            DrawGroundEffects(r, unit, b);          // 바닥 잔흔은 유닛 아래
            foreach (var u in b.Enemies) DrawUnit(r, unit, u, b.Time);
            foreach (var u in b.Players) DrawUnit(r, unit, u, b.Time);
            DrawEffects(r, unit, b);
            UpdateAndDrawParticles(r, unit, b);
            if (b.EyeOpen) DrawEvilEye(new Vector2(r.center.x, r.y + unit * 1.1f), unit * 1.6f, b.Time, 0.55f);
            if (b.ImpactTimer > 0f) InvertRect(r);      // 임팩트 프레임: 필드 색 반전
        }

        static Material invertMaterial;

        /// <summary>
        /// area(가상 좌표) 안의 화면 색을 뒤집는다. Unity 내장 셰이더의 블렌드를 '1 - 배경색' 으로 두고 흰 사각형을 그린다.
        /// 셰이더를 못 찾으면 흰 섬광으로 대신한다.
        /// </summary>
        static void InvertRect(Rect area)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (invertMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) { Fill(area, new Color(1f, 1f, 1f, 0.5f)); return; }
                invertMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                invertMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                invertMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                invertMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                invertMaterial.SetInt("_ZWrite", 0);
                invertMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
            float x0 = area.x / vw, x1 = area.xMax / vw, y0 = 1f - area.y / vh, y1 = 1f - area.yMax / vh;
            GL.PushMatrix();
            invertMaterial.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.Vertex3(x0, y0, 0f);
            GL.Vertex3(x1, y0, 0f);
            GL.Vertex3(x1, y1, 0f);
            GL.Vertex3(x0, y1, 0f);
            GL.End();
            GL.PopMatrix();
        }

        static readonly Color EyeColor = new Color(0.85f, 0.15f, 0.2f);

        /// <summary>악신의 눈 (필드 위 · 월영 캐릭터 위). 가로로 긴 눈 + 붉은 홍채 + 검은 동공.</summary>
        static void DrawEvilEye(Vector2 c, float size, float time, float alpha)
        {
            EnsureFx();
            float blink = 0.85f + 0.15f * Mathf.Sin(time * 3f);
            DrawFxRect(fxDisc, c, size * 2.4f, size * 1.1f * blink, EyeColor, 0.35f * alpha);
            DrawFxRect(fxRing, c, size * 2.0f, size * 0.85f * blink, new Color(1f, 0.6f, 0.6f), alpha);
            DrawFx(fxDisc, c, size * 0.9f, EyeColor, alpha);
            DrawFxRect(Texture2D.whiteTexture, c, size * 0.12f, size * 0.5f * blink, Color.black, alpha);
        }

        static readonly Color GluttonBody = new Color(0.45f, 0.45f, 0.5f);
        static readonly Color ArmorColor = new Color(1f, 0.82f, 0.25f);
        static readonly Color ShadowBody = new Color(0.06f, 0.05f, 0.08f);
        static readonly Color ReloadColor = new Color(1f, 0.8f, 0.3f);
        const float HitFlashTime = 0.12f;

        static void DrawUnit(Rect field, float unit, FieldUnit u, float now)
        {
            if (!u.Alive || u.Hidden) return;       // 맵 이탈 중에는 그리지 않는다
            EnsureFx();
            float d = FieldConst.Radius * 2f * unit;
            float cx = field.x + u.Pos.x * unit, cy = field.y + u.Pos.y * unit;
            float flash = Mathf.Clamp01(1f - (now - u.LastHitTime) / HitFlashTime);
            if (flash > 0f) { cx += Mathf.Sin(now * 90f) * 2f * flash; }            // 피격: 살짝 흔들림
            // 공중에 뜸 (띄워짐 · 높이 도약): 그림자는 바닥에, 몸은 위로
            if (u.AirTimer > 0f && u.AirTotal > 0f)
            {
                float k = Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - u.AirTimer / u.AirTotal));
                DrawFxRect(fxDisc, new Vector2(cx, cy + d * 0.35f), d * 1.4f, d * 0.6f, Color.black, 0.75f);
                cy -= u.AirHeight * unit * k;
            }
            var body = new Rect(cx - d / 2, cy - d / 2, d, d);
            if (u.C.Guarding) Fill(new Rect(body.x - 3, body.y - 3, body.width + 6, body.height + 6), GuardColor);
            if (u.SuperArmor)   // 슈퍼아머: 금색 테두리
                Fill(new Rect(body.x - 3, body.y - 3, body.width + 6, body.height + 6), ArmorColor);

            // 무기 상시 표시 (벤데타: 등 뒤 케이스 + 손의 권총)
            if (u.Def != null && u.Def.ShowWeapons)
            {
                float side = u.Facing.x >= 0f ? 1f : -1f;
                if (now >= u.CaseDownUntil)     // 케이스를 내려박은 동안에는 등에 없다
                {
                    Fill(new Rect(cx - side * d * 0.55f - d * 0.22f, cy - d * 0.45f, d * 0.44f, d * 0.8f), new Color(0.35f, 0.33f, 0.3f));   // 케이스
                    Fill(new Rect(cx - side * d * 0.55f - d * 0.18f, cy - d * 0.4f, d * 0.36f, d * 0.08f), new Color(0.6f, 0.55f, 0.45f));
                }
                Fill(new Rect(cx + side * d * 0.5f - (side < 0 ? d * 0.34f : 0f), cy, d * 0.34f, d * 0.14f), new Color(0.75f, 0.75f, 0.8f)); // 권총
            }

            Color bodyColor = u.IsPlayer ? PlayerColor : EnemyColor;
            // 넉백 잔상: 지나온 자리에 옅은 몸통
            if (u.PushTimer > 0f)
            {
                Vector2 vel = u.PushVel * (u.PushTimer / FieldConst.PushTime) * unit;
                for (int k = 3; k >= 1; k--)
                    Fill(new Rect(body.x - vel.x * GhostStep * k, body.y - vel.y * GhostStep * k, d, d),
                         new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.4f / k));
            }
            if (u.Glutton) bodyColor = GluttonBody;          // 글러트니: 회색
            if (u.MoonShadow) bodyColor = ShadowBody;        // 월영: 검은색으로 뒤덮임
            if (u.FireArm) bodyColor = Color.Lerp(bodyColor, SkillColor.Fire, 0.45f);
            Fill(body, bodyColor);
            if (flash > 0f) Fill(body, new Color(1f, 1f, 1f, 0.7f * flash));
            GUI.Label(body, ShortName(u), small);
            if (u.Immortal)   // 테스트 더미: 전술 한 글자 (돌 · 침 · 견 · 후)
                GUI.Label(new Rect(cx - 20, body.y - 15, 40, 14), $"<size=11><color=#ffd76a>{StrategyMark(u.Strategy)}</color></size>", small);
            if (u.MoonShadow) DrawEvilEye(new Vector2(cx, cy), d * 0.45f, now, 0.9f);   // 캐릭터를 덮는 눈

            // 상태 표시: 기절(노란 고리) · 표식(빨간 마름모)
            if (u.StunTimer > 0f || u.Marked) EnsureFx();
            if (u.StunTimer > 0f)
                DrawFx(fxRing, new Vector2(cx, body.y - unit * 0.25f), unit * 0.6f, StunColor, 0.9f);
            if (u.Marked)
                DrawFxRect(Texture2D.whiteTexture, new Vector2(cx + d * 0.45f, body.y - unit * 0.2f), unit * 0.28f, unit * 0.28f, MarkColor, 1f, 45f);

            var bar = new Rect(body.x, body.yMax + 2, d, 5);
            Fill(bar, BarBack);
            Fill(new Rect(bar.x, bar.y, bar.width * (u.Immortal ? 1f : u.HpRatio), bar.height), u.Immortal ? ImmortalColor : HpColor);

            // 총잡이: 장전 막대 (장전 중) · 남은 기본탄
            if (u.Def != null && u.Def.Gunner)
            {
                var rb = new Rect(body.x, bar.yMax + 2, d, 4);
                if (u.ReloadTimer > 0f)
                {
                    Fill(rb, BarBack);
                    Fill(new Rect(rb.x, rb.y, rb.width * (1f - u.ReloadTimer / FieldBattle.ReloadTime), rb.height), ReloadColor);
                    GUI.Label(new Rect(cx - 30, rb.yMax, 60, 16), "<size=12><color=#ffcc55>장전</color></size>", small);
                }
                else
                {
                    float pip = d / FieldBattle.GunnerAmmo;
                    for (int i = 0; i < u.Ammo; i++) Fill(new Rect(rb.x + i * pip + 1, rb.y, pip - 2, rb.height), new Color(0.9f, 0.9f, 0.9f));
                }
            }
            // 점화 막대 (화염의 팔 전)
            if (u.Def != null && u.Def.FireArmTrait && !u.FireArm)
            {
                var ib = new Rect(body.x, bar.yMax + 2, d, 3);
                Fill(ib, BarBack);
                Fill(new Rect(ib.x, ib.y, ib.width * (u.Ignite / 100f), ib.height), SkillColor.Fire);
            }
        }

        /// <summary>필드 위 표시 이름: 아군은 앞 두 글자, 더미는 번호 (1 근거리 · 2 원거리).</summary>
        static string StrategyMark(Strategy s) => s switch
        {
            Strategy.Charge => "돌진",
            Strategy.Flank => "침입",
            Strategy.Kite => "견제",
            Strategy.Backline => "후방",
            _ => "",
        };

        static string ShortName(FieldUnit u)
        {
            string n = u.Name;
            if (!u.IsPlayer && n.StartsWith("테스트 더미")) return "D" + n.Substring(n.Length - 1);
            return n.Length <= 2 ? n : n.Substring(0, 2);
        }

        static void DrawPartyPanel(Rect r, FieldBattle b)
        {
            Fill(r, Panel);
            float x = r.x + 10, w = r.width - 20, y = r.y + 8;
            float t = b.MeterTime;
            int total = 0;
            foreach (var u in b.Players) total += u.DamageDealt;
            GUI.Label(new Rect(x, y, w, 30), $"<b>아군</b>  <color=#aaaaaa>총 피해 {total} · 초당 {total / t:0.0}</color>", label);
            y += 34;

            foreach (var u in b.Players)
            {
                if (y + 56 > r.yMax) break;
                string dead = u.Alive ? "" : " <color=#ff8f8f>(쓰러짐)</color>";
                GUI.Label(new Rect(x, y, w, 26),
                    $"{u.Name}{dead} <size=15><color=#aaaaaa>{FieldData.RoleName(u.Role)} · {FieldData.StrategyName(u.Strategy)}</color></size>", label);
                y += 26;
                DrawBar(new Rect(x, y, w * 0.45f, 20), u.HpRatio, HpColor, $"{u.C.Hp}/{u.C.Stats.HP}");
                GUI.Label(new Rect(x + w * 0.45f + 8, y - 4, w * 0.55f - 8, 26),
                    $"<size=16>피해 {u.DamageDealt} · 초당 {u.DamageDealt / t:0.0}</size>", label);
                y += 30;
            }

            int taken = 0;
            foreach (var e in b.Enemies) taken += e.DamageTaken;
            if (y + 30 <= r.yMax)
                GUI.Label(new Rect(x, y + 4, w, 30), $"<b>적</b> <color=#aaaaaa>{b.Enemies.Count}명 · 받은 피해 {taken}</color>", label);
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

            small = new GUIStyle(center) { fontSize = 14, wordWrap = false };
            small.normal.textColor = Color.white;

            barText = new GUIStyle(center) { fontSize = 16, wordWrap = false };
            barText.normal.textColor = Color.white;

            button = new GUIStyle(GUI.skin.button) { fontSize = FontSize, richText = true, wordWrap = true };
            smallButton = new GUIStyle(button) { fontSize = 16, wordWrap = false };   // button 을 만든 뒤에 복사해야 한다
        }
    }
}
