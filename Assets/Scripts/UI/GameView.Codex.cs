using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 도감 (사용자 2026-11-28): 로비에서 들어와 모든 캐릭터의 카드를 소속별로 모아 보고, 카드를 누르면 상세창이 열린다.
    /// </summary>
    public static partial class GameView
    {
        // ── 도감 상세창 [임시] ──
        const float CodexSideW = 260f;               // 왼쪽 (큰 도트 + 소개) 열 최대 너비
        const float CodexSideRatio = 0.28f;          // 좁은 화면에서는 화면 너비의 이 비율
        const float CodexInfoLineH = 26f;
        const float CodexGroupGap = 32f;             // 패시브와 변신 액티브 사이

        static Vector2 codexScroll;
        static string codexId;                       // 상세창에 띄운 캐릭터 (null = 목록)
        static string codexSkillId;                  // 설명을 보는 스킬 · 패시브
        static Vector2 codexDescScroll;

        static void OpenCodex()
        {
            codexId = null;
            codexScroll = Vector2.zero;
        }

        static void DrawCodex(GameRoot g)
        {
            EnsureCardStyles();
            if (codexId != null) { DrawCodexDetail(g, FieldData.Character(codexId)); return; }

            int ready = 0;
            foreach (var c in FieldData.Roster) if (GetFieldSprite(c.Id) != null) ready++;
            Header($"도감  {FieldData.Roster.Length}명");
            GUI.Label(new Rect(Pad, 70, vw - Pad * 2, 32),
                $"<size=17><color=#999999>카드를 누르면 상세 정보를 봅니다 · 도트 있음 {ready}명</color></size>", center);

            float top = 110f;
            float by = vh - Pad - ButtonH;
            DrawFactionGrid(new Rect(Pad, top, vw - Pad * 2, by - Pad - top), ref codexScroll, DrawCodexCard);

            BackButton(g);
        }

        /// <summary>도감 카드. 도트가 아직 없는 캐릭터는 '?' 와 '준비 중'.</summary>
        static void DrawCodexCard(Rect r, CharacterDef c)
        {
            bool clicked = GUI.Button(r, GUIContent.none, button);
            bool has = GetFieldSprite(c.Id) != null;
            DrawCardFace(r, c, !has, CardNameBack, "?\n<size=14>준비 중</size>");
            if (!clicked) return;
            codexId = c.Id;
            codexSkillId = null;
            codexDescScroll = Vector2.zero;
        }

        /// <summary>상세창: 왼쪽에 큰 도트 · 소개, 오른쪽에 스탯 · 스킬 카드 · 설명. 목록의 스크롤 위치는 그대로 둔다.</summary>
        static void DrawCodexDetail(GameRoot g, CharacterDef c)
        {
            EnsureLoadoutStyles();
            Header($"도감  —  {c.Name}");

            float top = 76f;
            float by = vh - Pad - ButtonH;
            float sideW = Mathf.Min(CodexSideW, vw * CodexSideRatio);
            DrawCodexProfile(new Rect(Pad, top, sideW, by - 12f - top), c);

            var right = new Rect(Pad * 2 + sideW, top, vw - Pad * 3 - sideW, by - 12f - top);
            DrawCodexSkills(g, right, c);

            if (GUI.Button(MenuRect(by), "목록으로", button)) codexId = null;
        }

        static void DrawCodexProfile(Rect r, CharacterDef c)
        {
            Fill(r, Panel);
            float side = r.width - DetailPad * 2;
            var art = new Rect(r.x + DetailPad, r.y + DetailPad, side, side);
            Fill(art, CardArtBack);

            var sprite = GetFieldSprite(c.Id);
            if (sprite != null)
            {
                // 칸에 들어가는 가장 큰 정수배
                float screenPerUi = Screen.width / vw;
                int zoom = Mathf.Max(1, Mathf.FloorToInt(side * screenPerUi / FieldSpriteCanvasSize));
                float size = zoom * FieldSpriteCanvasSize / screenPerUi;
                float Snap(float v) => Mathf.Round(v * screenPerUi) / screenPerUi;
                PaintFieldSprite(new Rect(Snap(art.center.x - size / 2), Snap(art.center.y - size / 2), size, size), sprite.Art, false, Color.white);
            }
            else GUI.Label(art, "?\n<size=14>준비 중</size>", cardEmpty);

            var factions = new List<string>();      // 복수 소속은 모두 적는다
            foreach (var f in FactionData.All)
                if (System.Array.IndexOf(f.Members, c.Id) >= 0) factions.Add(f.Name);

            float y = art.yMax + 10f;
            GUI.Label(new Rect(art.x, y, side, 34f), $"<size=22><b>{c.Name}</b></size>", center);
            y += 40f;
            void Line(string head, string value)
            {
                var content = new GUIContent($"<color=#999999>{head}</color>  {value}");
                float h = Mathf.Max(CodexInfoLineH, descText.CalcHeight(content, side));
                GUI.Label(new Rect(art.x, y, side, h), content, descText);
                y += h;
            }
            Line("유형", c.Kind);
            Line("소속", string.Join(" · ", factions));
            Line("기본 역할", FieldData.RoleName(c.BaseRole));
            Line("기본 전술", FieldData.StrategyName(c.DefaultStrategy));
        }

        static void DrawCodexSkills(GameRoot g, Rect r, CharacterDef c)
        {
            Fill(r, Panel);
            float x = r.x + DetailPad, w = r.width - DetailPad * 2, y = r.y + DetailPad;

            // 패시브를 끼우지 않은 기본 스탯 (지금 레벨)
            var plain = new PartySlot { Def = c, Strategy = c.DefaultStrategy, Skills = new string[0], Passive = null };
            y = DrawLoadoutStats(new Rect(x, y, w, 0f), plain, g.Progress.Level) + 6f;

            // 스킬 컨셉이 없는 캐릭터의 스킬 칸은 자리 표시라 보여 주지 않는다
            var passives = new List<string>();
            if (c.Enabled)
            {
                if (c.FixedPassive != null) passives.Add(c.FixedPassive);
                passives.AddRange(c.Passives);
            }
            var actives = c.Enabled ? c.Skills : new string[0];
            string[] transform = !c.Enabled ? null
                : System.Array.IndexOf(c.Passives, ConceptCharacters.GluttonPassive) >= 0 ? ConceptCharacters.GluttonSkills
                : System.Array.IndexOf(c.Passives, ConceptCharacters.MoonShadowPassive) >= 0 ? ConceptCharacters.MoonShadowSkills : null;

            if (passives.Count == 0 && actives.Length == 0)
            {
                GUI.Label(new Rect(x, y + 8f, w, 32f), "<color=#999999>스킬 · 패시브 미정</color>", label);
                return;
            }
            if (!passives.Contains(codexSkillId) && System.Array.IndexOf(actives, codexSkillId) < 0 &&
                (transform == null || System.Array.IndexOf(transform, codexSkillId) < 0))
                codexSkillId = actives.Length > 0 ? actives[0] : passives[0];

            // 한 줄에 들어갈 카드 수에 맞춘다: 액티브 6장, 패시브 줄은 변신 액티브까지
            float size = Mathf.Min(SkillCardMax, (w - SkillGap * 5) / 6f);
            if (transform != null)
                size = Mathf.Min(size, (w - CodexGroupGap - SkillGap * (passives.Count + transform.Length - 2)) / (passives.Count + transform.Length));

            GUI.Label(new Rect(x, y, w, SkillCaptionH), "<size=17>패시브</size>", label);
            float transformX = x + passives.Count * (size + SkillGap) - SkillGap + CodexGroupGap;
            if (transform != null) GUI.Label(new Rect(transformX, y, w, SkillCaptionH), "<size=17>변신 액티브</size>", label);
            y += SkillCaptionH;
            for (int i = 0; i < passives.Count; i++)
                CodexSkillCard(new Rect(x + i * (size + SkillGap), y, size, size), passives[i],
                    FieldData.Passive(passives[i])?.Name, passives[i] == c.FixedPassive ? "고정" : null);
            if (transform != null)
                for (int i = 0; i < transform.Length; i++)
                    CodexSkillCard(new Rect(transformX + i * (size + SkillGap), y, size, size), transform[i], FieldData.Skill(transform[i])?.Name, null);
            y += size + 6f;

            GUI.Label(new Rect(x, y, w, SkillCaptionH), "<size=17>액티브</size>", label);
            y += SkillCaptionH;
            for (int i = 0; i < actives.Length; i++)
                CodexSkillCard(new Rect(x + i * (size + SkillGap), y, size, size), actives[i], FieldData.Skill(actives[i])?.Name, null);
            y += size + 10f;

            var box = new Rect(x, y, w, r.yMax - DetailPad - y);
            Fill(box, DetailBoxColor);
            var area = new Rect(box.x + 10f, box.y + 6f, box.width - 20f, box.height - 12f);
            var text = new GUIContent(SkillDescription(codexSkillId, passives.Contains(codexSkillId)));
            float innerW = area.width - 20f;
            float textH = descText.CalcHeight(text, innerW);
            codexDescScroll = GUI.BeginScrollView(area, codexDescScroll, new Rect(0, 0, innerW, textH));
            GUI.Label(new Rect(0, 0, innerW, textH), text, descText);
            GUI.EndScrollView();
        }

        /// <summary>도감의 스킬 카드: 장착 표시 없이 이름만, mark 가 있으면 위쪽 띠.</summary>
        static void CodexSkillCard(Rect r, string id, string name, string mark)
        {
            if (id == codexSkillId) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), ViewColor);
            bool clicked = GUI.Button(r, GUIContent.none, button);
            var inner = new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6);
            Fill(inner, CardArtBack);
            if (mark != null)
            {
                var strip = new Rect(inner.x, inner.y, inner.width, EquipStripH);
                Fill(strip, EquipColor);
                GUI.Label(strip, mark, equipText);
            }
            GUI.Label(inner, name ?? id, skillName);
            if (!clicked) return;
            codexSkillId = id;
            codexDescScroll = Vector2.zero;
        }
    }
}
