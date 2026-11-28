using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 출전 선택 화면 (사용자 2026-11-28): 가로 4 : 세로 6 카드에 도트와 아래 이름표, 소속별 분류, 선택 초기화.
    /// </summary>
    public static partial class GameView
    {
        // ── 출전 선택 레이아웃 [임시] ──
        const float CardMinW = 104f;                 // 카드 최소 너비 (이 너비로 열 수를 정하고 남는 폭은 나눠 늘린다)
        const float CardAspect = 6f / 4f;            // 세로 / 가로
        const float CardGap = 8f;
        const float CardNameH = 40f;                 // 아래 이름표 높이 (긴 이름은 두 줄)
        const float CardSpriteRefUi = 122f;          // 도트 배율을 정하는 기준 크기. 카드를 줄여도 도트 크기는 그대로 (사용자 2026-11-28)
        const int CardSpriteMaxW = 61;               // 도트에서 실제로 그려진 가장 넓은 · 높은 크기 (원본 px, 출전 가능 도트 기준)
        const int CardSpriteMaxH = 63;
        const float CardPad = 4f;
        const float FactionHeadH = 34f;
        const float FactionGap = 10f;                // 소속 묶음 사이

        static readonly Color CardArtBack = new Color(0.17f, 0.17f, 0.21f);
        static readonly Color CardNameBack = new Color(0.08f, 0.08f, 0.10f);
        static readonly Color CardNamePicked = new Color(0.14f, 0.24f, 0.45f);
        static readonly Color CardDimTint = new Color(0.45f, 0.45f, 0.45f, 0.8f);

        static Vector2 partyScroll;
        static GUIStyle cardName, cardMark, cardEmpty;

        static void DrawPartySelect(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null) { g.Go(GameScreen.Lobby); return; }
            EnsureCardStyles();
            var sel = g.SelectedParty;
            Header($"출전 선택  {sel.Count}/{FieldConst.MaxPartySize}  —  {stage.Name}");
            int activeTotal = 0;
            foreach (var c in FieldData.Roster) if (c.Enabled) activeTotal++;

            // 선택 순서 = 편성 순서
            var names = new string[sel.Count];
            for (int i = 0; i < sel.Count; i++) names[i] = $"{i + 1}. {FieldData.Character(sel[i]).Name}";
            string order = sel.Count > 0 ? string.Join("   ", names) : $"<color=#999999>캐릭터를 눌러 선택하세요 (출전 가능 {activeTotal}명)</color>";
            GUI.Label(new Rect(Pad, 70, vw - Pad * 2, 32), order, center);

            float top = 110f;
            float bottom = vh - Pad - ButtonH - Pad;
            var view = new Rect(Pad, top, vw - Pad * 2, bottom - top);
            DrawFactionGrid(view, ref partyScroll, (r, c) => DrawCharacterCard(g, r, c, sel.IndexOf(c.Id)));

            float by = vh - Pad - ButtonH;
            float bw = (vw - Pad * 4) / 3;
            if (GUI.Button(new Rect(Pad, by, bw, ButtonH), "뒤로", button)) g.Go(GameScreen.Lobby);
            GUI.enabled = sel.Count > 0;
            if (GUI.Button(new Rect(Pad * 2 + bw, by, bw, ButtonH), "선택 초기화", button)) g.ClearParty();
            if (GUI.Button(new Rect(Pad * 3 + bw * 2, by, bw, ButtonH), $"다음: 전술 · 스킬 ({sel.Count}명)", button)) g.GoLoadout();
            GUI.enabled = true;
        }

        /// <summary>캐릭터 카드를 소속별로 늘어놓는 스크롤 격자 (출전 선택 · 도감). 카드 한 장은 drawCard 가 그린다.</summary>
        static void DrawFactionGrid(Rect view, ref Vector2 scroll, System.Action<Rect, CharacterDef> drawCard)
        {
            float innerW = view.width - 20f;
            // 도트가 큰 배율이 되는 창 크기에서는 그려진 부분이 들어가도록 카드 최소 너비를 늘린다
            float spriteSize = CardSpriteSize();
            float spriteW = spriteSize * CardSpriteMaxW / FieldSpriteCanvasSize;
            float spriteH = spriteSize * CardSpriteMaxH / FieldSpriteCanvasSize;
            float minW = Mathf.Max(CardMinW, spriteW, (spriteH + CardNameH + CardPad) / CardAspect);
            int cols = Mathf.Max(2, Mathf.FloorToInt((innerW + CardGap) / (minW + CardGap)));
            float cw = (innerW - CardGap * (cols - 1)) / cols;
            float ch = cw * CardAspect;

            // 보여 줄 캐릭터가 없는 소속(양의문)은 제목도 그리지 않는다
            var groups = new List<(Faction Faction, List<CharacterDef> Members)>();
            float contentH = 0f;
            foreach (var f in FactionData.All)
            {
                var list = SortedMembers(f);
                if (list.Count == 0) continue;
                groups.Add((f, list));
                contentH += FactionHeadH + Mathf.CeilToInt(list.Count / (float)cols) * (ch + CardGap) + FactionGap;
            }

            Fill(view, Panel);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, innerW, contentH));
            float y = 0f;
            foreach (var (f, members) in groups)
            {
                GUI.Label(new Rect(4, y + 2, innerW - 8, FactionHeadH - 4),
                    $"<b>{f.Name}</b>  <size=16><color=#999999>{members.Count}명</color></size>", label);
                Fill(new Rect(0, y + FactionHeadH - 4, innerW, 1), BarBack);
                y += FactionHeadH;

                for (int i = 0; i < members.Count; i++)
                {
                    var r = new Rect((i % cols) * (cw + CardGap), y + (i / cols) * (ch + CardGap), cw, ch);
                    drawCard(r, members[i]);
                }
                y += Mathf.CeilToInt(members.Count / (float)cols) * (ch + CardGap) + FactionGap;
            }
            GUI.EndScrollView();
        }

        /// <summary>이 소속에서 보여 줄 캐릭터. 소속 안에서는 출전 가능 캐릭터를 앞에, 비활성은 뒤에 둔다 (사용자 2026-11-26).</summary>
        static List<CharacterDef> SortedMembers(Faction f)
        {
            var list = new List<CharacterDef>(f.Members.Length);
            foreach (bool enabled in new[] { true, false })
                foreach (var id in f.Members)
                {
                    var c = FieldData.Character(id);
                    if (c.Enabled == enabled && FactionData.DisplayFaction(id) == f) list.Add(c);
                }
            return list;
        }

        /// <summary>카드 한 장: 위는 도트, 아래는 이름표. pick = 편성 순서 (-1 = 미선택).</summary>
        static void DrawCharacterCard(GameRoot g, Rect r, CharacterDef c, int pick)
        {
            if (pick >= 0) Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), PlayerColor);

            GUI.enabled = c.Enabled;
            bool clicked = GUI.Button(r, GUIContent.none, button);
            GUI.enabled = true;

            var art = DrawCardFace(r, c, !c.Enabled, pick >= 0 ? CardNamePicked : CardNameBack, c.Enabled ? "?" : "?\n<size=14>비활성</size>");
            if (pick >= 0) GUI.Label(new Rect(art.x + 4, art.y + 2, 40, 26), $"<b>{pick + 1}</b>", cardMark);

            if (clicked) g.ToggleParty(c.Id);
        }

        /// <summary>카드 앞면: 위 도트(없으면 emptyText), 아래 이름표. dim 이면 흐리게. 도트 칸을 돌려준다.</summary>
        static Rect DrawCardFace(Rect r, CharacterDef c, bool dim, Color tagColor, string emptyText)
        {
            var art = new Rect(r.x + CardPad, r.y + CardPad, r.width - CardPad * 2, r.height - CardNameH - CardPad * 2);
            var tag = new Rect(r.x + CardPad, art.yMax, r.width - CardPad * 2, CardNameH);
            Fill(art, CardArtBack);
            Fill(tag, tagColor);

            var sprite = GetFieldSprite(c.Id);
            if (sprite != null) PaintFieldSprite(CardSpriteRect(art), sprite.Art, false, dim ? CardDimTint : Color.white);
            else GUI.Label(art, emptyText, cardEmpty);

            GUI.Label(tag, dim ? $"<color=#777777>{c.Name}</color>" : c.Name, cardName);
            return art;
        }

        /// <summary>카드 도트 한 변의 UI 크기: 기준 크기 안의 가장 큰 정수배 (화면 픽셀 기준, 최소 1배).</summary>
        static float CardSpriteSize()
        {
            float screenPerUi = Screen.width / vw;
            int zoom = Mathf.Max(1, Mathf.FloorToInt(CardSpriteRefUi * screenPerUi / FieldSpriteCanvasSize));
            return zoom * FieldSpriteCanvasSize / screenPerUi;
        }

        /// <summary>도트 자리 가운데에 놓는다. 투명 여백은 카드 밖으로 나가도 된다. 화면 픽셀에 맞춘다.</summary>
        static Rect CardSpriteRect(Rect area)
        {
            float screenPerUi = Screen.width / vw;
            float size = CardSpriteSize();
            float Snap(float v) => Mathf.Round(v * screenPerUi) / screenPerUi;
            return new Rect(Snap(area.center.x - size * 0.5f), Snap(area.center.y - size * 0.5f), size, size);
        }

        static void EnsureCardStyles()
        {
            if (cardName != null) return;
            cardName = new GUIStyle(center) { fontSize = 15, wordWrap = true, padding = new RectOffset(0, 0, 0, 0) };
            cardMark = new GUIStyle(label) { fontSize = 20, wordWrap = false };
            cardMark.normal.textColor = new Color(0.56f, 0.83f, 1f);
            cardEmpty = new GUIStyle(center) { fontSize = 34 };
            cardEmpty.normal.textColor = new Color(0.4f, 0.4f, 0.45f);
        }
    }
}
