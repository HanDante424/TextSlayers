using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 캐릭터 선택 다음 화면: 캐릭터별 전술 · 패시브 1 · 액티브 3 선택 (사용자 2026-11-26).
    /// 개편 (사용자 2026-11-28): 왼쪽에 캐릭터 카드를 세로로, 오른쪽에 스탯 · 전술(드롭박스) · 정사각형 스킬 카드 · 스킬 설명 창.
    /// </summary>
    public static partial class GameView
    {
        // ── 전술 · 스킬 선택 레이아웃 [임시] ──
        const float RosterCardMaxH = 132f;           // 왼쪽 세로형 카드 (가로 4 : 세로 6) 최대 높이
        const float RosterTagH = 46f;                // 아래 이름 (두 줄까지) · 역할
        const float RosterGap = 6f;
        const float DetailPad = 12f;
        const float DetailHeadH = 40f;
        const float StatCellH = 30f;
        const int StatCols = 5;
        const float DropW = 220f, DropH = 36f;
        const float SkillCardMax = 84f;
        const float SkillGap = 8f;
        const float SkillCaptionH = 28f;
        const float EquipStripH = 20f;
        const float EquipButtonW = 120f, EquipButtonH = 44f;

        static readonly Color DetailBoxColor = new Color(0.16f, 0.16f, 0.2f);
        static readonly Color EquipColor = new Color(1f, 0.84f, 0.42f);
        static readonly Color ViewColor = new Color(0.9f, 0.95f, 1f);

        static string loadoutFocusId;      // 오른쪽에 보여 주는 캐릭터
        static string loadoutSkillId;      // 설명 창에 보여 주는 스킬 · 패시브
        static bool strategyOpen;          // 전술 드롭박스가 열려 있는가
        static Vector2 skillDescScroll, rosterScroll;
        static GUIStyle skillName, equipText, descText, dropText, rosterName;

        static void DrawLoadout(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null || g.SelectedParty.Count == 0) { strategyOpen = false; g.Go(GameScreen.PartySelect); return; }
            EnsureCardStyles();
            EnsureLoadoutStyles();
            if (!g.SelectedParty.Contains(loadoutFocusId)) FocusCharacter(g.SelectedParty[0]);

            float top = 76f;
            float leftW = RosterColumnW();
            float rightX = Pad * 2 + leftW;
            float by = vh - Pad - ButtonH;
            var detail = new Rect(rightX, top, vw - rightX - Pad, by - 12f - top);
            var drop = new Rect(detail.xMax - DetailPad - DropW, detail.y + DetailPad, DropW, DropH);

            // 열린 드롭박스의 클릭은 다른 컨트롤보다 먼저 처리해야 아래 버튼이 같이 눌리지 않는다
            if (strategyOpen) HandleStrategyList(g.Loadout(loadoutFocusId), drop);

            Header($"전술 · 스킬 선택  —  {stage.Name}");
            DrawRosterColumn(g, new Rect(Pad, top, leftW, vh - Pad - top));
            DrawLoadoutDetail(g, detail, drop);

            float bw = (detail.width - Pad) / 2;
            if (GUI.Button(new Rect(rightX, by, bw, ButtonH), "뒤로 (캐릭터 선택)", button)) g.Go(GameScreen.PartySelect);
            bool ready = g.LoadoutReady(out string problem);
            string next = stage.IsTest ? "다음: 적 전술" : "다음: 배치";
            string go = ready ? next : $"<size=16><color=#ffb347>{problem}</color></size>";
            if (GUI.Button(new Rect(rightX + bw + Pad, by, bw, ButtonH), go, button)) g.NextFromLoadout();

            if (strategyOpen) DrawStrategyList(g.Loadout(loadoutFocusId), drop);   // 맨 위에 보이도록 마지막에 그린다
        }

        static void FocusCharacter(string id)
        {
            loadoutFocusId = id;
            loadoutSkillId = null;
            skillDescScroll = Vector2.zero;
        }

        static float RosterColumnW() => RosterCardW();

        /// <summary>세로형(4:6) 카드 너비: 최대 출전 인원이 한 화면에 들어가는 높이에서 정한다.</summary>
        static float RosterCardW()
        {
            float height = vh - Pad - 76f;      // 왼쪽 열 높이 (위 76 ~ 아래 여백)
            float ch = Mathf.Min(RosterCardMaxH, Mathf.Floor((height - RosterGap * (FieldConst.MaxPartySize - 1)) / FieldConst.MaxPartySize));
            return ch / CardAspect;
        }

        /// <summary>
        /// 세로로 긴 캐릭터 카드 한 장: 위 도트, 아래 이름 · 역할, 왼쪽 위에 편성 순서.
        /// 전술창의 왼쪽 열과 배치 화면의 대기 열이 같이 쓴다. 누르는 처리는 부르는 쪽에서 한다.
        /// </summary>
        static void DrawRosterCard(Rect r, PartySlot slot, int order, bool focused, bool dim, string warning)
        {
            var c = slot.Def;
            var art = new Rect(r.x + CardPad, r.y + CardPad, r.width - CardPad * 2, r.height - RosterTagH - CardPad * 2);
            var tag = new Rect(art.x, art.yMax, art.width, RosterTagH);
            Fill(art, CardArtBack);
            Fill(tag, focused ? CardNamePicked : CardNameBack);

            var sprite = GetFieldSprite(c.Id);
            if (sprite != null) DrawPortrait(art, sprite, dim ? CardDimTint : Color.white);
            else GUI.Label(art, "?", cardEmpty);

            var skills = slot.Passive == ConceptCharacters.GluttonPassive ? ConceptCharacters.GluttonSkills : slot.Skills;
            var role = FieldData.CurrentRole(c, slot.Strategy, skills, slot.Passive);
            GUI.Label(new Rect(r.x + 1, tag.y, r.width - 2, tag.height), $"{c.Name}\n<size=11><color=#aaaaaa>{FieldData.RoleName(role)}</color></size>", rosterName);   // 좁은 칸이라 글자는 카드 폭을 거의 다 쓴다
            GUI.Label(new Rect(art.x + 3, art.y, 30, 20), $"<size=15><b>{order}</b></size>", cardMark);
            if (warning != null)
            {
                var strip = new Rect(art.x, art.yMax - 18f, art.width, 18f);
                Fill(strip, new Color(0f, 0f, 0f, 0.7f));
                GUI.Label(strip, $"<size=11><color=#ffb347>{warning}</color></size>", barText);
            }
        }

        /// <summary>왼쪽: 출전 캐릭터 카드 (편성 순서, 스크롤). 누르면 오른쪽이 그 캐릭터로 바뀐다.</summary>
        static void DrawRosterColumn(GameRoot g, Rect area)
        {
            int n = g.SelectedParty.Count;
            float cw = RosterCardW(), ch = cw * CardAspect;
            var content = new Rect(0, 0, cw, n * (ch + RosterGap) - RosterGap);
            rosterScroll = GUI.BeginScrollView(area, rosterScroll, content);
            for (int i = 0; i < n; i++)
            {
                string id = g.SelectedParty[i];
                var slot = g.Loadout(id);
                var r = new Rect(3f, 3f + i * (ch + RosterGap), cw - 6f, ch - 6f);
                bool focused = id == loadoutFocusId;
                if (focused) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), PlayerColor);
                bool clicked = GUI.Button(r, GUIContent.none, button);
                string warning = ActivesReady(slot) ? null
                    : $"액티브 {slot.Skills.Length}/{Mathf.Min(FieldData.MaxEquipActives, slot.Def.Skills.Length)}";
                DrawRosterCard(r, slot, i + 1, focused, false, warning);
                if (clicked) FocusCharacter(id);
            }
            GUI.EndScrollView();
        }

        static bool ActivesReady(PartySlot slot) =>
            slot.Passive == ConceptCharacters.GluttonPassive ||
            slot.Skills.Length == Mathf.Min(FieldData.MaxEquipActives, slot.Def.Skills.Length);

        /// <summary>오른쪽: 이름 · 전술 → 스탯 → 패시브 · 액티브 카드 → 설명 창.</summary>
        static void DrawLoadoutDetail(GameRoot g, Rect r, Rect drop)
        {
            string id = loadoutFocusId;
            var slot = g.Loadout(id);
            var c = slot.Def;
            bool glutton = slot.Passive == ConceptCharacters.GluttonPassive;
            var actives = glutton ? ConceptCharacters.GluttonSkills : c.Skills;   // 글러트니면 식 3종으로 고정

            Fill(r, Panel);
            float x = r.x + DetailPad, w = r.width - DetailPad * 2, y = r.y + DetailPad;

            var role = FieldData.CurrentRole(c, slot.Strategy, glutton ? ConceptCharacters.GluttonSkills : slot.Skills, slot.Passive);
            float headW = drop.x - 64f - x;
            GUI.Label(new Rect(x, y, headW, DetailHeadH),
                $"<size=26><b>{c.Name}</b></size>  <size=15><color=#aaaaaa>{c.Kind} · 현재 역할 {FieldData.RoleName(role)}</color></size>", label);
            GUI.Label(new Rect(drop.x - 56f, drop.y, 52f, drop.height), "<size=17>전술</size>", dropText);
            if (GUI.Button(drop, $"{FieldData.StrategyName(slot.Strategy)}  ▼", smallButton)) strategyOpen = true;
            y += DetailHeadH + 8f;

            y = DrawLoadoutStats(new Rect(x, y, w, 0f), slot, g.Progress.Level) + 6f;

            if (c.Passives.Length == 0 && actives.Length == 0)
            {
                GUI.Label(new Rect(x, y + 8f, w, 32f), "<color=#999999>스킬 · 패시브 미정 — 기본 공격만 사용</color>", label);
                return;
            }

            // 보고 있는 스킬이 이 캐릭터 목록에 없으면 첫 장착 액티브(없으면 첫 칸)로
            if (!IsPassiveOf(c, loadoutSkillId) && System.Array.IndexOf(actives, loadoutSkillId) < 0)
                loadoutSkillId = slot.Skills.Length > 0 && !glutton ? slot.Skills[0] : actives.Length > 0 ? actives[0] : c.Passives[0];

            float size = Mathf.Min(SkillCardMax, (w - SkillGap * 5) / 6f);

            string fixedNote = c.FixedPassive != null ? " · 고정 1" : "";
            GUI.Label(new Rect(x, y, w, SkillCaptionH), $"<size=17>패시브  <color=#999999>1개 장착{fixedNote}</color></size>", label);
            y += SkillCaptionH;
            float px = x;
            if (c.FixedPassive != null)   // 고정 패시브는 맨 앞에, 고르는 패시브와 조금 띄운다
            {
                DrawSkillCard(new Rect(px, y, size, size), c.FixedPassive, FieldData.Passive(c.FixedPassive)?.Name, true, "고정");
                px += size + SkillGap * 3;
            }
            for (int i = 0; i < c.Passives.Length; i++)
                DrawSkillCard(new Rect(px + i * (size + SkillGap), y, size, size), c.Passives[i],
                    FieldData.Passive(c.Passives[i])?.Name, slot.Passive == c.Passives[i]);
            y += size + 6f;

            string head = glutton ? "액티브  <color=#999999>글러트니 고정</color>"
                : $"액티브  <color=#999999>{slot.Skills.Length}/{Mathf.Min(FieldData.MaxEquipActives, c.Skills.Length)} 장착</color>";
            GUI.Label(new Rect(x, y, w, SkillCaptionH), $"<size=17>{head}</size>", label);
            y += SkillCaptionH;
            for (int i = 0; i < actives.Length; i++)
                DrawSkillCard(new Rect(x + i * (size + SkillGap), y, size, size), actives[i],
                    FieldData.Skill(actives[i])?.Name, glutton || System.Array.IndexOf(slot.Skills, actives[i]) >= 0);
            y += size + 10f;

            DrawSkillDescription(g, new Rect(x, y, w, r.yMax - DetailPad - y), slot, glutton);
        }

        /// <summary>스탯 표 (지금 레벨, 장착 패시브의 수치 보정 포함 — 전투 계산과 같은 함수). 그린 뒤의 아래쪽 y 를 돌려준다.</summary>
        static float DrawLoadoutStats(Rect r, PartySlot slot, int level)
        {
            var c = slot.Def;
            var passive = FieldData.Passive(slot.Passive);
            var s = FieldData.ApplyPassive(FieldData.StatsAt(c, level), passive);
            float mov = c.Mov * (passive != null ? passive.MovMul : 1f);
            bool glutton = slot.Passive == ConceptCharacters.GluttonPassive;
            var cells = new (string Name, string Value)[]
            {
                ("체력", $"{s.HP}"), ("공격력", $"{s.ATK}"), ("마력", $"{s.MAG}"), ("방어력", $"{s.DEF}"), ("저항력", $"{s.RES}"),
                ("공격/초", $"{s.SPD * FieldConst.AttackPerSpd:0.00}"), ("치명타", $"{s.CRIT * 100f:0}%"), ("치명 피해", $"{s.CRITDMG * 100f:0}%"),
                ("이동 속도", $"{mov:0.0#}"), ("사거리", glutton ? "근거리" : $"{c.Range:0.0#}"),
            };
            int rows = (cells.Length + StatCols - 1) / StatCols;
            float noteH = c.NumbersSet ? 0f : 22f;
            Fill(new Rect(r.x, r.y, r.width, rows * StatCellH + 8f + noteH), DetailBoxColor);
            float cw = (r.width - 16f) / StatCols;
            for (int i = 0; i < cells.Length; i++)
                GUI.Label(new Rect(r.x + 10f + (i % StatCols) * cw, r.y + 4f + (i / StatCols) * StatCellH, cw - 4f, StatCellH),
                    $"<size=16><color=#999999>{cells[i].Name}</color>  <b>{cells[i].Value}</b></size>", dropText);
            if (!c.NumbersSet)
                GUI.Label(new Rect(r.x + 10f, r.y + 4f + rows * StatCellH, r.width - 20f, noteH),
                    "<size=13><color=#ffb347>수치 미정 캐릭터 — 스탯은 임시값, 피해는 0</color></size>", dropText);
            return r.y + rows * StatCellH + 8f + noteH;
        }

        /// <summary>정사각형 스킬 카드 (아이콘이 생기기 전에는 이름만). 누르면 설명 창만 바뀐다.</summary>
        static void DrawSkillCard(Rect r, string id, string name, bool equipped, string mark = "장착")
        {
            if (id == loadoutSkillId) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), ViewColor);
            bool clicked = GUI.Button(r, GUIContent.none, button);
            var inner = new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6);
            Fill(inner, equipped ? CardNamePicked : CardArtBack);
            if (equipped)
            {
                var strip = new Rect(inner.x, inner.y, inner.width, EquipStripH);
                Fill(strip, EquipColor);
                GUI.Label(strip, mark, equipText);
            }
            GUI.Label(new Rect(inner.x + 1, inner.y + EquipStripH, inner.width - 2, inner.height - EquipStripH),
                equipped ? $"<b>{name ?? id}</b>" : $"<color=#bbbbbb>{name ?? id}</color>", skillName);
            if (clicked && loadoutSkillId != id)
            {
                loadoutSkillId = id;
                skillDescScroll = Vector2.zero;
            }
        }

        static bool IsPassiveOf(CharacterDef c, string id) =>
            id != null && (id == c.FixedPassive || System.Array.IndexOf(c.Passives, id) >= 0);

        /// <summary>설명 창: 보고 있는 스킬의 설명과 장착 · 해제 버튼.</summary>
        static void DrawSkillDescription(GameRoot g, Rect r, PartySlot slot, bool glutton)
        {
            Fill(r, DetailBoxColor);
            string id = loadoutSkillId;
            bool isPassive = IsPassiveOf(slot.Def, id);
            bool isFixed = isPassive && id == slot.Def.FixedPassive;
            bool equipped = isFixed || (isPassive ? slot.Passive == id : glutton || System.Array.IndexOf(slot.Skills, id) >= 0);

            var buttonRect = new Rect(r.xMax - EquipButtonW - 10f, r.y + 10f, EquipButtonW, EquipButtonH);
            string action = isFixed ? "고정" : isPassive ? (equipped ? "장착 중" : "장착") : glutton ? "고정" : equipped ? "해제" : "장착";
            GUI.enabled = isPassive ? !equipped : !glutton;
            bool pressed = GUI.Button(buttonRect, action, button);
            GUI.enabled = true;

            var view = new Rect(r.x + 10f, r.y + 6f, buttonRect.x - r.x - 20f, r.height - 12f);
            var content = new GUIContent(SkillDescription(id, isPassive));
            float innerW = view.width - 20f;
            float h = descText.CalcHeight(content, innerW);
            skillDescScroll = GUI.BeginScrollView(view, skillDescScroll, new Rect(0, 0, innerW, h));
            GUI.Label(new Rect(0, 0, innerW, h), content, descText);
            GUI.EndScrollView();

            if (!pressed) return;
            if (isPassive) slot.Passive = id;
            else g.ToggleActive(loadoutFocusId, id);
        }

        /// <summary>
        /// 스킬 설명 (전술창 설명 창과 전투 툴팁 공용). 사용자 2026-11-28 형식:
        /// 이름 → 한 줄 설명 (쿨타임) → 패시브 (패시브를 가진 액티브는 따로) → 액티브 → 강화 액티브. 내용이 없는 칸은 생략한다.
        /// </summary>
        static string SkillDescription(string id, bool isPassive)
        {
            var passive = isPassive ? FieldData.Passive(id) : null;
            var skill = isPassive ? null : FieldData.Skill(id);
            var text = SkillTexts.Get(id);
            string name = passive?.Name ?? skill?.Name ?? id;

            // 한 줄 설명: 태그 · 대상 (문서에 문장 설명이 따로 없다)
            var summary = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(text?.Tag)) summary.Add(text.Tag);
            if (!string.IsNullOrEmpty(text?.Target)) summary.Add($"대상: {text.Target}");
            string line = summary.Count > 0 ? string.Join(" · ", summary) : isPassive ? "패시브" : "액티브";
            if (skill != null)
                line += skill.Decided ? $"  (쿨타임 {skill.Cooldown:0.#}초)" : $"  (쿨타임 {skill.EffectiveCooldown:0.#}초 · 수치 미정)";

            var lines = new System.Text.StringBuilder($"<size=21><b>{name}</b></size>\n<color=#aaaaaa>{line}</color>");
            void Section(string head, string color, string body)
            {
                if (!string.IsNullOrEmpty(body)) lines.Append($"\n<size=8> </size>\n<color={color}><b>{head}</b></color>\n{body}");
            }

            string passiveBody = text?.Passive ?? "";
            if (passive != null)
                passiveBody += (passiveBody.Length > 0 ? "\n" : "") + $"<color=#999999>{passive.Desc}</color>";
            Section("패시브", "#8fff9f", passiveBody);

            if (skill != null)
            {
                string active = text?.Active ?? "";
                if (!string.IsNullOrEmpty(skill.Desc) && !(text?.ActiveHasNumbers ?? false))
                    active += (active.Length > 0 ? "\n" : "") + skill.Desc;
                Section("액티브", "#8fd3ff", active.Length > 0 ? active : "<color=#999999>세부 설명 미정</color>");
                Section("강화 액티브", "#ffd76a", text?.Enhanced);
            }
            lines.Append("\n<size=8> </size>\n<size=13><color=#777777>컨셉 설명 — 적힌 효과 중 일부는 아직 구현 전</color></size>");
            return lines.ToString();
        }

        // ── 전술 드롭박스 ──

        static Rect StrategyItemRect(Rect drop, int i) => new Rect(drop.x, drop.yMax + 2f + i * drop.height, drop.width, drop.height);

        /// <summary>열린 목록의 입력. 항목을 누르면 바꾸고, 어디를 누르든 닫는다.</summary>
        static void HandleStrategyList(PartySlot slot, Rect drop)
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel) { e.Use(); return; }
            if (e.type != EventType.MouseDown) return;
            for (int i = 0; i < FieldData.PlayerStrategies.Length; i++)
                if (e.button == 0 && StrategyItemRect(drop, i).Contains(e.mousePosition)) slot.Strategy = FieldData.PlayerStrategies[i];
            strategyOpen = false;
            e.Use();
        }

        static void DrawStrategyList(PartySlot slot, Rect drop)
        {
            var list = FieldData.PlayerStrategies;
            Fill(new Rect(drop.x - 2f, drop.yMax, drop.width + 4f, list.Length * drop.height + 4f), ViewColor);
            for (int i = 0; i < list.Length; i++)
            {
                var item = StrategyItemRect(drop, i);
                bool hover = item.Contains(Event.current.mousePosition);
                Fill(item, hover ? CardNamePicked : CardNameBack);
                GUI.Label(item, Pick(FieldData.StrategyName(list[i]), slot.Strategy == list[i]), barText);
            }
        }

        static void EnsureLoadoutStyles()
        {
            if (skillName != null) return;
            rosterName = new GUIStyle(center) { fontSize = 12, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
            skillName = new GUIStyle(center) { fontSize = 15, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
            equipText = new GUIStyle(center) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false, padding = new RectOffset(0, 0, 0, 0) };
            equipText.normal.textColor = new Color(0.12f, 0.1f, 0.05f);
            descText = new GUIStyle(label) { fontSize = 16, padding = new RectOffset(0, 0, 0, 0) };
            dropText = new GUIStyle(label) { alignment = TextAnchor.MiddleLeft, wordWrap = false };
        }

        // ── 디버그 맵: 더미별 종류 · 전술 (사용자 2026-11-26) ──
        const string DummyMeleeId = "test_dummy_melee", DummyRangedId = "test_dummy_ranged";
        const float EnemyRowH = 40f;
        static Vector2 enemyScroll;

        static void DrawEnemySetup(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null) { g.Go(GameScreen.Lobby); return; }
            var setup = g.DebugEnemies;
            var list = setup.Entries;
            Header($"적 전술 · 배치  —  {stage.Name}  ({setup.Total}/{FieldBattle.MaxDebugEnemies}명)");

            float w = vw - Pad * 2, x = Pad, y = 74f;
            var strategies = FieldData.PlayerStrategies;

            // 위: 추가 · 일괄
            float tb = 150f;
            if (GUI.Button(new Rect(x, y, tb, EnemyRowH - 4f), "+ 더미 1 (근거리)", smallButton)) g.AddDebugEnemy(DummyMeleeId);
            if (GUI.Button(new Rect(x + tb + 6f, y, tb, EnemyRowH - 4f), "+ 더미 2 (원거리)", smallButton)) g.AddDebugEnemy(DummyRangedId);
            if (GUI.Button(new Rect(x + (tb + 6f) * 2, y, 110f, EnemyRowH - 4f), "전부 삭제", smallButton)) list.Clear();
            if (GUI.Button(new Rect(x + (tb + 6f) * 2 + 116f, y, 110f, EnemyRowH - 4f), "기본값", smallButton)) g.ResetDebugEnemies();
            float ax = x + (tb + 6f) * 2 + 236f;
            GUI.Label(new Rect(ax, y, 110f, EnemyRowH), "<size=16>전체 전술</size>", label);
            float abw = (w - (ax - x) - 110f) / strategies.Length;
            for (int i = 0; i < strategies.Length; i++)
                if (GUI.Button(new Rect(ax + 110f + i * abw, y, abw - 4f, EnemyRowH - 4f), $"<size=15>{FieldData.StrategyName(strategies[i])}</size>", smallButton))
                    foreach (var en in list) en.Strategy = strategies[i];
            y += EnemyRowH + 6f;

            // 목록: 더미마다 종류 · 전술 · 삭제
            float bottom = vh - Pad - ButtonH - Pad;
            var view = new Rect(x, y, w, bottom - y);
            float innerW = view.width - 20f;
            var content = new Rect(0, 0, innerW, Mathf.Max(1, list.Count) * EnemyRowH);
            Fill(view, Panel);
            enemyScroll = GUI.BeginScrollView(view, enemyScroll, content);
            int remove = -1;
            const float NumW = 50f, TypeW = 120f, DelW = 70f;
            float sbw = (innerW - NumW - TypeW * 2 - DelW - 30f) / strategies.Length;
            for (int i = 0; i < list.Count; i++)
            {
                var en = list[i];
                float ry = i * EnemyRowH + 2f, rx = 6f;
                if (i % 2 == 0) Fill(new Rect(0, ry - 2f, innerW, EnemyRowH), new Color(1f, 1f, 1f, 0.03f));
                GUI.Label(new Rect(rx, ry, NumW, EnemyRowH - 4f), $"<size=16>#{i + 1}</size>", label);
                rx += NumW;
                if (GUI.Button(new Rect(rx, ry, TypeW - 4f, EnemyRowH - 6f), Pick("더미 1", en.Id == DummyMeleeId), smallButton)) en.Id = DummyMeleeId;
                if (GUI.Button(new Rect(rx + TypeW, ry, TypeW - 4f, EnemyRowH - 6f), Pick("더미 2", en.Id == DummyRangedId), smallButton)) en.Id = DummyRangedId;
                rx += TypeW * 2 + 12f;
                for (int k = 0; k < strategies.Length; k++)
                    if (GUI.Button(new Rect(rx + k * sbw, ry, sbw - 4f, EnemyRowH - 6f), Pick(FieldData.StrategyName(strategies[k]), en.Strategy == strategies[k]), smallButton))
                        en.Strategy = strategies[k];
                rx += sbw * strategies.Length + 8f;
                if (GUI.Button(new Rect(rx, ry, DelW, EnemyRowH - 6f), "<color=#ff8f8f>삭제</color>", smallButton)) remove = i;
            }
            if (list.Count == 0) GUI.Label(new Rect(6f, 6f, innerW, 30f), "<color=#999999>더미가 없습니다. 위에서 추가하세요 (0명으로 출전해도 됩니다).</color>", label);
            GUI.EndScrollView();
            if (remove >= 0) list.RemoveAt(remove);   // 그린 뒤에 지운다

            float by = vh - Pad - ButtonH;
            float bw2 = (vw - Pad * 3) / 2;
            if (GUI.Button(new Rect(Pad, by, bw2, ButtonH), "뒤로 (전술 · 스킬)", button)) g.Go(GameScreen.Loadout);
            if (GUI.Button(new Rect(Pad * 2 + bw2, by, bw2, ButtonH), $"다음: 배치 (아군 {g.SelectedParty.Count}명 · 적 {setup.Total}명)", button)) g.GoPlacement();
        }

        static string Pick(string text, bool on) => on ? $"<color=#ffd76a><b>[{text}]</b></color>" : $"<color=#bbbbbb>{text}</color>";
    }
}
