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
        const float CardMinW = 128f;                 // 카드 최소 너비 (이 너비로 열 수를 정하고 남는 폭은 나눠 늘린다)
        const float CardAspect = 6f / 4f;            // 세로 / 가로
        const float CardGap = 8f;
        const float CardNameH = 48f;                 // 아래 이름표 높이 (긴 이름은 두 줄)
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

            // 캐릭터 목록 (스크롤, GUILayout 미사용): 소속마다 제목 한 줄 + 카드 줄
            float top = 110f;
            float bottom = vh - Pad - ButtonH - Pad;
            var view = new Rect(Pad, top, vw - Pad * 2, bottom - top);
            float innerW = view.width - 20f;
            int cols = Mathf.Max(2, Mathf.FloorToInt((innerW + CardGap) / (CardMinW + CardGap)));
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
            partyScroll = GUI.BeginScrollView(view, partyScroll, new Rect(0, 0, innerW, contentH));
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
                    DrawCharacterCard(g, r, members[i], sel.IndexOf(members[i].Id));
                }
                y += Mathf.CeilToInt(members.Count / (float)cols) * (ch + CardGap) + FactionGap;
            }
            GUI.EndScrollView();

            float by = vh - Pad - ButtonH;
            float bw = (vw - Pad * 4) / 3;
            if (GUI.Button(new Rect(Pad, by, bw, ButtonH), "뒤로", button)) g.Go(GameScreen.Lobby);
            GUI.enabled = sel.Count > 0;
            if (GUI.Button(new Rect(Pad * 2 + bw, by, bw, ButtonH), "선택 초기화", button)) g.ClearParty();
            if (GUI.Button(new Rect(Pad * 3 + bw * 2, by, bw, ButtonH), $"다음: 전술 · 스킬 ({sel.Count}명)", button)) g.GoLoadout();
            GUI.enabled = true;
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

            var art = new Rect(r.x + CardPad, r.y + CardPad, r.width - CardPad * 2, r.height - CardNameH - CardPad * 2);
            var tag = new Rect(r.x + CardPad, art.yMax, r.width - CardPad * 2, CardNameH);
            Fill(art, CardArtBack);
            Fill(tag, pick >= 0 ? CardNamePicked : CardNameBack);

            var sprite = GetFieldSprite(c.Id);
            if (sprite != null) PaintFieldSprite(CardSpriteRect(art), sprite.Art, false, c.Enabled ? Color.white : CardDimTint);
            else GUI.Label(art, c.Enabled ? "?" : "?\n<size=14>비활성</size>", cardEmpty);

            string name = c.Enabled ? c.Name : $"<color=#777777>{c.Name}</color>";
            GUI.Label(tag, name, cardName);
            if (pick >= 0) GUI.Label(new Rect(art.x + 4, art.y + 2, 40, 26), $"<b>{pick + 1}</b>", cardMark);

            if (clicked) g.ToggleParty(c.Id);
        }

        /// <summary>도트 자리 안에 들어가는 가장 큰 정수배(화면 픽셀 기준, 최소 1배) 정사각형. 화면 픽셀에 맞춘다.</summary>
        static Rect CardSpriteRect(Rect area)
        {
            float screenPerUi = Screen.width / vw;
            int zoom = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(area.width, area.height) * screenPerUi / FieldSpriteCanvasSize));
            float size = zoom * FieldSpriteCanvasSize / screenPerUi;
            float Snap(float v) => Mathf.Round(v * screenPerUi) / screenPerUi;
            return new Rect(Snap(area.center.x - size * 0.5f), Snap(area.center.y - size * 0.5f), size, size);
        }

        static void EnsureCardStyles()
        {
            if (cardName != null) return;
            cardName = new GUIStyle(center) { fontSize = 16, wordWrap = true };
            cardMark = new GUIStyle(label) { fontSize = 20, wordWrap = false };
            cardMark.normal.textColor = new Color(0.56f, 0.83f, 1f);
            cardEmpty = new GUIStyle(center) { fontSize = 34 };
            cardEmpty.normal.textColor = new Color(0.4f, 0.4f, 0.45f);
        }
    }
}
