using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 전투 화면에서 캐릭터 카드를 눌렀을 때 (사용자 2026-11-28): 카메라가 그 캐릭터를 확대해 따라가고,
    /// 전투는 느려지며(GameRoot), 작은 설명 창이 뜬다. 다시 누르기 · 닫기 · 필드 클릭 · Esc 로 돌아간다.
    /// </summary>
    public static partial class GameView
    {
        // ── 확대 카메라 · 설명 창 [임시] ──
        const float FocusZoom = 2f;                  // 확대 배율
        const float CamZoomRate = 5f;                // 초당 배율 변화
        const float CamFollowRate = 10f;             // 따라가는 빠르기 (클수록 바로 붙는다)
        const float InfoW = 270f, InfoPad = 10f, InfoLineH = 20f, InfoHeadH = 26f;
        const float InfoHpH = 18f, InfoTabH = 28f;
        const int InfoBodyLines = 7;                 // 탭 내용 줄 수: 창 높이는 탭 · 캐릭터와 무관하게 같다
        const float InfoH = InfoPad * 2 + InfoHeadH + InfoLineH + InfoHpH + InfoTabH + 6f + InfoLineH * InfoBodyLines;
        const float InfoMargin = 8f;
        const float AvoidHalfW = 0.8f, AvoidUp = 1.3f;   // 설명 창에 가리지 않게 볼 캐릭터의 크기 (필드 단위)
        const float InfoSkillSize = 52f, InfoSkillGap = 6f;   // 스킬 탭의 정사각형 카드
        const float InfoSkillCaptionH = 16f, InfoSkillRowGap = 4f;
        const float TipGap = 6f, TipPad = 10f, TipCloseW = 24f;

        enum InfoTab { Stats, Skills, Graph }
        static readonly string[] InfoTabNames = { "스탯", "스킬", "그래프" };
        static InfoTab infoTab;
        static FieldUnit tipUnit;                    // 툴팁이 열린 캐릭터
        static string tipSkillId;                    // 툴팁으로 보는 스킬 · 패시브 (null = 닫힘)
        static bool tipIsPassive;
        static Vector2 tipScroll;

        static FieldBattle camBattle;
        static float camZoom = 1f;
        static Vector2 camCenter;                    // 화면 가운데에 오는 필드 좌표
        static GUIStyle infoText;

        /// <summary>
        /// 지금 카메라로 필드를 그릴 사각형과 단위 길이. view 는 화면에서 필드가 차지하는 칸이고,
        /// 확대 중에는 돌려주는 사각형이 view 보다 크다 (넘치는 부분은 MaskOutside 가 덮는다).
        /// </summary>
        static Rect CameraField(GameRoot g, FieldBattle b, Rect view, float baseUnit, out float unit)
        {
            var whole = new Vector2(FieldConst.Width, FieldConst.Height) * 0.5f;
            if (camBattle != b) { camBattle = b; camZoom = 1f; camCenter = whole; }

            var focus = g.FieldFocus;
            if (focus != tipUnit) { tipSkillId = null; tipUnit = focus; }     // 보는 캐릭터가 바뀌면 툴팁은 닫는다
            if (Event.current.type == EventType.Repaint)
            {
                float dt = Time.unscaledDeltaTime;   // 카메라는 전투 배속과 무관하게 움직인다
                camZoom = Mathf.MoveTowards(camZoom, focus != null ? FocusZoom : 1f, CamZoomRate * dt);
                var goal = focus != null ? FocusGoal(focus.Pos, !focus.IsPlayer, view, baseUnit) : whole;
                camCenter = Vector2.Lerp(camCenter, goal, 1f - Mathf.Exp(-CamFollowRate * dt));
            }

            // 필드 바깥이 보이지 않게 가운데를 제한한다 (배율 1 이면 정확히 필드 가운데)
            var half = whole / camZoom;
            // 좌우는 확대한 만큼 더 나갈 수 있다: 끝에 있는 캐릭터를 설명 창 밖으로 빼기 위해
            float extraSide = (InfoW / (baseUnit * FocusZoom) + AvoidHalfW) * Mathf.Clamp01((camZoom - 1f) / (FocusZoom - 1f));
            var c = new Vector2(Mathf.Clamp(camCenter.x, half.x - extraSide, FieldConst.Width - half.x + extraSide),
                                Mathf.Clamp(camCenter.y, half.y, FieldConst.Height - half.y));
            unit = baseUnit * camZoom;
            return new Rect(view.center.x - c.x * unit, view.center.y - c.y * unit,
                            FieldConst.Width * unit, FieldConst.Height * unit);
        }

        /// <summary>
        /// 확대 카메라가 향할 가운데 (필드 좌표). 보통은 캐릭터이고 필드 밖이 보이지 않게 제한한다.
        /// 캐릭터가 설명 창(아군은 왼쪽 위, 적은 오른쪽 위: infoRight)에 가리면 창 옆에 오도록 카메라를 좌우로 옮긴다.
        /// </summary>
        static Vector2 FocusGoal(Vector2 pos, bool infoRight, Rect view, float baseUnit)
        {
            float zu = baseUnit * FocusZoom;
            var half = new Vector2(FieldConst.Width, FieldConst.Height) * (0.5f / FocusZoom);
            var goal = new Vector2(Mathf.Clamp(pos.x, half.x, FieldConst.Width - half.x),
                                   Mathf.Clamp(pos.y, half.y, FieldConst.Height - half.y));
            float sx = view.width / 2 + (pos.x - goal.x) * zu, sy = view.height / 2 + (pos.y - goal.y) * zu;   // view 안의 위치
            float clearX = InfoMargin + InfoW + AvoidHalfW * zu;
            bool covered = tipSkillId != null || sy - AvoidUp * zu < InfoMargin + InfoH;   // 툴팁은 창 아래로 이어진다
            if (infoRight) clearX = view.width - clearX;      // view 안에서 캐릭터가 넘어가면 안 되는 선
            bool behind = infoRight ? sx > clearX : sx < clearX;
            if (behind && covered) goal.x = pos.x - (clearX - view.width / 2) / zu;
            return goal;
        }

        /// <summary>확대한 필드가 view 밖으로 넘친 부분을 배경색으로 덮는다. 회전 이펙트가 있어 클립 그룹 대신 덮어 그린다.</summary>
        static void MaskOutside(Rect view)
        {
            if (camZoom <= 1.001f) return;
            Fill(new Rect(0, 0, vw, view.y), Bg);
            Fill(new Rect(0, view.yMax, vw, vh - view.yMax), Bg);
            Fill(new Rect(0, view.y, view.x, view.height), Bg);
            Fill(new Rect(view.xMax, view.y, vw - view.xMax, view.height), Bg);
        }

        /// <summary>카드를 누름: 같은 캐릭터면 닫고, 다른 캐릭터면 그쪽으로 옮긴다.</summary>
        static void ToggleFocus(FieldUnit u)
        {
            var g = GameRoot.Instance;
            if (g != null) g.FieldFocus = g.FieldFocus == u ? null : u;
        }

        /// <summary>
        /// 설명 창 (작게, 필드 위쪽 구석에 고정: 아군은 왼쪽, 적은 오른쪽): 이름 · 체력바 아래의 탭 3개(스탯 · 체력 · 그래프). 그린 사각형을 돌려준다.
        /// </summary>
        static Rect DrawFocusInfo(GameRoot g, FieldUnit u, Rect view)
        {
            if (infoText == null)
                infoText = new GUIStyle(label) { fontSize = 14, wordWrap = false, padding = new RectOffset(0, 0, 0, 0), clipping = TextClipping.Clip };

            // 누른 카드가 있는 쪽에 띄운다 (사용자 2026-11-28)
            float infoX = u.IsPlayer ? view.x + InfoMargin : view.xMax - InfoMargin - InfoW;
            var r = new Rect(infoX, view.y + InfoMargin, InfoW, InfoH);
            Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), u.IsPlayer ? PlayerColor : EnemyColor);
            Fill(r, new Color(0.08f, 0.08f, 0.1f, 0.94f));

            float x = r.x + InfoPad, w = r.width - InfoPad * 2, y = r.y + InfoPad;
            GUI.Label(new Rect(x, y, w - 30f, InfoHeadH), $"<size=18><b>{u.Name}</b></size>", infoText);
            if (GUI.Button(new Rect(r.xMax - InfoPad - 24f, y - 2f, 24f, 24f), "✕", smallButton)) g.FieldFocus = null;
            y += InfoHeadH;

            string kind = u.Def != null ? $"{u.Def.Kind} · " : "";
            string role = u.IsPlayer ? $"{FieldData.RoleName(u.Role)} · " : "";
            GUI.Label(new Rect(x, y, w, InfoLineH), $"<color=#aaaaaa>{kind}{role}{FieldData.StrategyName(u.Strategy)}</color>", infoText);
            y += InfoLineH;

            DrawUnitHp(new Rect(x, y + 1f, w, 15f), u);
            y += InfoHpH;

            float tw = (w - 8f) / InfoTabNames.Length;
            for (int i = 0; i < InfoTabNames.Length; i++)
            {
                bool on = (int)infoTab == i;
                var tab = new Rect(x + i * (tw + 4f), y + 1f, tw, InfoTabH - 4f);
                if (!GUI.Button(tab, on ? $"<color=#ffd76a><b>{InfoTabNames[i]}</b></color>" : InfoTabNames[i], smallButton)) continue;
                infoTab = (InfoTab)i;
                tipSkillId = null;
            }
            y += InfoTabH + 6f;

            var body = new Rect(x, y, w, InfoLineH * InfoBodyLines);
            if (infoTab == InfoTab.Stats) DrawInfoStats(body, u);
            else if (infoTab == InfoTab.Skills) DrawInfoSkills(body, u);
            else DrawInfoGraph(body, u);
            return r;
        }

        static void DrawUnitHp(Rect r, FieldUnit u)
        {
            Fill(r, BarBack);
            Fill(new Rect(r.x, r.y, r.width * (u.Immortal ? 1f : u.HpRatio), r.height), u.IsPlayer ? AllyHpColor : EnemyHpColor);
            GUI.Label(r, u.Immortal ? "∞" : $"{u.C.Hp}/{u.C.Stats.HP}", small);
        }

        static void InfoLine(Rect body, int line, string text) =>
            GUI.Label(new Rect(body.x, body.y + line * InfoLineH, body.width, InfoLineH), text, infoText);

        static void InfoPair(Rect body, int line, string a, string av, string b, string bv)
        {
            float y = body.y + line * InfoLineH, half = body.width / 2;
            GUI.Label(new Rect(body.x, y, half, InfoLineH), $"<color=#999999>{a}</color>  {av}", infoText);
            GUI.Label(new Rect(body.x + half, y, half, InfoLineH), $"<color=#999999>{b}</color>  {bv}", infoText);
        }

        /// <summary>스탯 탭: 능력치만.</summary>
        static void DrawInfoStats(Rect body, FieldUnit u)
        {
            var s = u.C.Stats;
            InfoPair(body, 0, "공격력", $"{s.ATK}", "마력", $"{s.MAG}");
            InfoPair(body, 1, "방어력", $"{s.DEF}", "저항력", $"{s.RES}");
            InfoPair(body, 2, "공격/초", $"{s.SPD * FieldConst.AttackPerSpd:0.00}", "이동 속도", $"{u.Mov:0.0#}");
            InfoPair(body, 3, "치명타", $"{s.CRIT * 100f:0}%", "치명 피해", $"{s.CRITDMG * 100f:0}%");
            InfoPair(body, 4, "사거리", $"{u.Range:0.0#}", "강인함", $"{u.Toughness:0}");
        }

        /// <summary>
        /// 스킬 탭: 위 줄은 패시브, 아래 줄은 액티브의 정사각형 카드. 누르면 창 아래에 세부 설명 툴팁이 열린다.
        /// </summary>
        static void DrawInfoSkills(Rect body, FieldUnit u)
        {
            EnsureLoadoutStyles();
            float y = body.y;
            void Caption(string text)
            {
                GUI.Label(new Rect(body.x, y, body.width, InfoSkillCaptionH), $"<size=12><color=#999999>{text}</color></size>", infoText);
                y += InfoSkillCaptionH;
            }
            void None() => GUI.Label(new Rect(body.x, y, body.width, InfoSkillSize), "<color=#777777>없음</color>", infoText);

            Caption("패시브");
            int passives = 0;
            void PassiveCard(FieldPassiveDef p) => DrawInfoSkillCard(
                new Rect(body.x + passives++ * (InfoSkillSize + InfoSkillGap), y, InfoSkillSize, InfoSkillSize), p.Id, p.Name, true, 0f, 0f);
            var fixedPassive = FieldData.Passive(u.Def?.FixedPassive);   // 고정 패시브(벤데타 기본탄)가 먼저
            if (fixedPassive != null) PassiveCard(fixedPassive);
            if (u.Passive != null) PassiveCard(u.Passive);
            if (passives == 0) None();
            y += InfoSkillSize + InfoSkillRowGap;

            Caption("액티브");
            int count = 0;
            foreach (var sk in u.Skills) if (sk != null) count++;
            // 한 줄에 다 들어가게: 장착 수가 많으면 카드를 줄인다
            float size = count > 0 ? Mathf.Min(InfoSkillSize, Mathf.Floor((body.width - InfoSkillGap * (count - 1)) / count)) : InfoSkillSize;
            int slot = 0;
            for (int i = 0; i < u.Skills.Length; i++)
            {
                if (u.Skills[i] == null) continue;
                float left = i < u.Cooldowns.Length ? u.Cooldowns[i] : 0f;
                DrawInfoSkillCard(new Rect(body.x + slot++ * (size + InfoSkillGap), y, size, size),
                    u.Skills[i].Id, u.Skills[i].Name, false, left, u.Skills[i].EffectiveCooldown);
            }
            if (count == 0) None();
        }

        /// <summary>작은 스킬 카드. 쿨타임 중에는 남은 만큼 위에서부터 어둡게 덮고 남은 초를 적는다.</summary>
        static void DrawInfoSkillCard(Rect r, string id, string name, bool passive, float left, float total)
        {
            if (id == tipSkillId && passive == tipIsPassive) Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), ViewColor);
            bool clicked = GUI.Button(r, GUIContent.none, button);
            var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
            Fill(inner, passive ? CardArtBack : CardNamePicked);
            GUI.Label(inner, $"<size=12>{name}</size>", skillName);
            if (left > 0f)
            {
                Fill(new Rect(inner.x, inner.y, inner.width, inner.height * Mathf.Clamp01(left / Mathf.Max(0.01f, total))), new Color(0f, 0f, 0f, 0.6f));
                GUI.Label(new Rect(inner.x, inner.yMax - 16f, inner.width, 15f), $"<color=#ffd76a>{left:0.0}</color>", small);
            }
            if (!clicked) return;
            bool same = id == tipSkillId && passive == tipIsPassive;
            tipSkillId = same ? null : id;           // 같은 카드를 다시 누르면 닫는다
            tipIsPassive = passive;
            tipScroll = Vector2.zero;
        }

        /// <summary>
        /// 스킬 툴팁: 설명 창 바로 아래, 같은 너비. 내용은 전술창의 설명과 같다. 열려 있지 않으면 빈 사각형.
        /// </summary>
        static Rect DrawSkillTooltip(Rect info, Rect view)
        {
            if (tipSkillId == null || infoTab != InfoTab.Skills) return Rect.zero;
            EnsureLoadoutStyles();
            var content = new GUIContent(SkillDescription(tipSkillId, tipIsPassive));
            float textW = info.width - TipPad * 2 - 14f;
            float textH = descText.CalcHeight(content, textW - TipCloseW);
            float maxH = view.yMax - InfoMargin - info.yMax - TipGap;
            var r = new Rect(info.x, info.yMax + TipGap, info.width, Mathf.Min(textH + TipPad * 2, maxH));
            Fill(new Rect(r.x - 2, r.y - 2, r.width + 4, r.height + 4), ViewColor);
            Fill(r, new Color(0.08f, 0.08f, 0.1f, 0.96f));

            var area = new Rect(r.x + TipPad, r.y + TipPad, r.width - TipPad * 2, r.height - TipPad * 2);
            tipScroll = GUI.BeginScrollView(area, tipScroll, new Rect(0, 0, textW, textH), GUIStyle.none, GUI.skin.verticalScrollbar);
            GUI.Label(new Rect(0, 0, textW - TipCloseW, textH), content, descText);
            GUI.EndScrollView();
            if (GUI.Button(new Rect(r.xMax - TipPad - TipCloseW, r.y + 6f, TipCloseW, TipCloseW), "✕", smallButton)) tipSkillId = null;
            return r;
        }

        /// <summary>그래프 탭: 준 피해 · 받은 피해 · 회복. 막대 길이는 같은 팀 최고값 기준, % 는 팀 합계에서 차지하는 몫.</summary>
        static void DrawInfoGraph(Rect body, FieldUnit u)
        {
            var b = GameRoot.Instance != null ? GameRoot.Instance.Field : null;
            if (b == null) return;
            var team = u.IsPlayer ? b.Players : b.Enemies;
            float h = body.height / Metrics.Length;
            for (int i = 0; i < Metrics.Length; i++)
            {
                var m = Metrics[i];
                TeamTotals(team, m, out int max, out int total);
                int value = m.Value(u);
                float y = body.y + i * h;
                GUI.Label(new Rect(body.x, y, body.width, InfoLineH),
                    $"{m.Name}  <b>{value}</b>  <color=#999999>팀의 {Share(value, total)}</color>", infoText);
                DrawMeter(new Rect(body.x, y + InfoLineH + 2f, body.width, 14f), value, max, m.Color);
            }
        }

        /// <summary>
        /// 필드 클릭: 설명 창 · 툴팁 밖의 필드를 누르면 툴팁이 열려 있으면 툴팁만, 아니면 확대를 푼다.
        /// 디버그 도구를 고른 동안에는 도구가 클릭을 쓴다. 창의 버튼들을 그린 뒤에 부른다.
        /// </summary>
        static void HandleFocusClick(GameRoot g, Rect view, Rect info, Rect tip)
        {
            Event e = Event.current;
            if (g.FieldFocus == null || e.type != EventType.MouseDown || !view.Contains(e.mousePosition)) return;
            if (info.Contains(e.mousePosition) || tip.Contains(e.mousePosition)) { e.Use(); return; }   // 창 안의 클릭은 필드로 넘기지 않는다
            if (dbgTool != DebugTool.None) return;
            if (tipSkillId != null) tipSkillId = null;
            else g.FieldFocus = null;
            e.Use();
        }
    }
}
