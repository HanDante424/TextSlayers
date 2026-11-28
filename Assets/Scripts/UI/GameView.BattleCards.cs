using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 작은 카드에 넣는 상반신 초상과, 전투 화면 양옆의 아군 · 적 카드 (사용자 2026-11-28).
    /// </summary>
    public static partial class GameView
    {
        // ── 초상 · 전투 카드 [임시] ──
        const int PortraitMinSource = 30;            // 초상에 최소한 보여 줄 원본 픽셀 (가로 · 세로). 이 안에서 가장 큰 정수배
        const float BattleColW = 104f;               // 전투 화면 양옆 카드 열 너비 (스크롤바 자리 포함)
        const float BattleScrollW = 8f;
        const float BattleCardMaxH = 124f;
        const float BattleCardGap = 6f;
        const float BattleColHeadH = 22f;
        const float BattleGraphButtonH = 30f;
        const float BattleNameH = 17f, BattleHpH = 16f;

        static Vector2 allyScroll, foeScroll;
        static GUIStyle cardLine;

        /// <summary>도트의 윗부분(머리 · 상반신)을 area 에 맞게 잘라 화면 픽셀 정수배로 그린다. 도트 크기를 줄이지 않고 칸만 작게 쓴다.</summary>
        static void DrawPortrait(Rect area, FieldSprite sprite, Color tint)
        {
            if (Event.current.type != EventType.Repaint) return;
            float screenPerUi = Screen.width / vw;
            int zoom = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(area.width, area.height) * screenPerUi / PortraitMinSource));
            int srcW = Mathf.Clamp(Mathf.FloorToInt(area.width * screenPerUi / zoom), 1, FieldSpriteCanvasSize);
            int srcH = Mathf.Clamp(Mathf.FloorToInt(area.height * screenPerUi / zoom), 1, FieldSpriteCanvasSize);
            int left = (FieldSpriteCanvasSize - srcW) / 2;
            int top = Mathf.Clamp(sprite.ContentTop - 1, 0, FieldSpriteCanvasSize - srcH);   // 원본 위에서부터의 줄

            float w = srcW * zoom / screenPerUi, h = srcH * zoom / screenPerUi;
            float Snap(float v) => Mathf.Round(v * screenPerUi) / screenPerUi;
            var dest = new Rect(Snap(area.center.x - w * 0.5f), Snap(area.yMax - h), w, h);
            const float N = FieldSpriteCanvasSize;
            var uv = new Rect(left / N, 1f - (top + srcH) / N, srcW / N, srcH / N);
            var previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(dest, sprite.Art, uv);
            GUI.color = previous;
        }

        static float BattleColumnW(bool graph) => BattleColW + (graph ? TeamStripW : 0f);

        /// <summary>
        /// 전투 화면의 한쪽 열: 제목 + 유닛 카드 (많으면 스크롤) + 아래의 그래프 버튼.
        /// 그래프를 켜면 카드마다 필드 쪽에 기여도 3줄이 붙는다 (graphOnRight: 아군 열은 카드 오른쪽, 적 열은 왼쪽).
        /// </summary>
        static void DrawBattleColumn(Rect area, string head, System.Collections.Generic.List<FieldUnit> units, ref Vector2 scroll,
                                     ref bool graph, bool graphOnRight)
        {
            if (cardLine == null) cardLine = new GUIStyle(small) { fontSize = 13, padding = new RectOffset(0, 0, 0, 0) };
            bool open = graph;                          // 이번에 그리는 동안은 값을 바꾸지 않는다 (열 너비와 맞아야 한다)
            float cw = BattleColW - BattleScrollW;
            float strip = open ? TeamStripW : 0f;
            float cardX = open && !graphOnRight ? strip : 0f;
            float stripX = graphOnRight ? cw + TeamStripGap : 0f;
            GUI.Label(new Rect(area.x + cardX, area.y, cw, BattleColHeadH), head, barText);
            if (open) DrawTeamLegend(new Rect(area.x + stripX, area.y, strip - TeamStripGap, BattleColHeadH));

            var graphButton = new Rect(area.x + cardX, area.yMax - BattleGraphButtonH, cw, BattleGraphButtonH);
            if (GUI.Button(graphButton, open ? "<color=#ffd76a>[그래프]</color>" : "그래프", smallButton)) graph = !graph;

            var view = new Rect(area.x, area.y + BattleColHeadH, area.width, graphButton.y - BattleCardGap - area.y - BattleColHeadH);
            // 최대 출전 인원이 스크롤 없이 들어가는 높이
            float ch = Mathf.Min(BattleCardMaxH, Mathf.Floor((view.height - BattleCardGap * (FieldConst.MaxPartySize - 1)) / FieldConst.MaxPartySize));
            float contentH = Mathf.Max(0f, units.Count * (ch + BattleCardGap) - BattleCardGap);
            // 스크롤바는 얇게: 열이 좁아 세로 막대만 둔다
            var bar = GUI.skin.verticalScrollbar;
            var thumb = GUI.skin.verticalScrollbarThumb;
            float barW = bar.fixedWidth, thumbW = thumb.fixedWidth;
            bar.fixedWidth = thumb.fixedWidth = BattleScrollW - 2f;
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, cw + strip, contentH), GUIStyle.none, bar);   // 가로 막대 없음
            var maxes = new int[Metrics.Length];
            var totals = new int[Metrics.Length];
            for (int k = 0; k < Metrics.Length; k++) TeamTotals(units, Metrics[k], out maxes[k], out totals[k]);
            for (int i = 0; i < units.Count; i++)
            {
                float y = i * (ch + BattleCardGap);
                DrawBattleCard(new Rect(cardX, y, cw, ch), units[i], i + 1);
                if (open) DrawTeamStrip(new Rect(stripX, y, strip - TeamStripGap, ch), units[i], maxes, totals);
            }
            GUI.EndScrollView();
            bar.fixedWidth = barW;
            thumb.fixedWidth = thumbW;
        }

        /// <summary>유닛 카드: 초상 · 이름 · 체력바. 누르면 그 캐릭터를 확대해 본다.</summary>
        static void DrawBattleCard(Rect r, FieldUnit u, int order)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            bool focused = GameRoot.Instance != null && GameRoot.Instance.FieldFocus == u;
            Fill(r, focused ? ViewColor : u.IsPlayer ? PlayerColor : EnemyColor);   // 테두리 (보고 있는 카드는 흰색)
            var inner = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);
            Fill(inner, CardNameBack);

            var art = new Rect(inner.x + 1, inner.y + 1, inner.width - 2, inner.height - 2 - BattleNameH - BattleHpH);
            Fill(art, CardArtBack);
            var sprite = u.Def != null ? UnitFieldSprite(u) : null;
            if (sprite != null) DrawPortrait(art, sprite, u.Alive ? Color.white : CardDimTint);
            else
            {
                float side = Mathf.Min(art.width, art.height) - 10f;
                var body = new Rect(art.center.x - side / 2, art.center.y - side / 2, side, side);
                Color color = u.IsPlayer ? PlayerColor : EnemyColor;
                Fill(body, u.Alive ? color : color * 0.45f);
                GUI.Label(body, ShortName(u), small);
            }
            if (u.IsPlayer) GUI.Label(new Rect(art.x + 3, art.y, 30, 20), $"<size=15><b>{order}</b></size>", cardMark);
            if (!u.Alive)
            {
                var strip = new Rect(art.x, art.center.y - 9f, art.width, 18f);
                Fill(strip, new Color(0f, 0f, 0f, 0.7f));
                GUI.Label(strip, "<color=#ff8f8f>쓰러짐</color>", cardLine);
            }

            float y = art.yMax;
            GUI.Label(new Rect(art.x, y, art.width, BattleNameH), CardUnitName(u), cardLine);
            y += BattleNameH;
            var hp = new Rect(art.x + 2, y, art.width - 4, BattleHpH - 1);
            Fill(hp, BarBack);
            Fill(new Rect(hp.x, hp.y, hp.width * (u.Immortal ? 1f : u.HpRatio), hp.height), u.IsPlayer ? AllyHpColor : EnemyHpColor);
            GUI.Label(hp, u.Immortal ? "∞" : $"{u.C.Hp}/{u.C.Stats.HP}", cardLine);

            if (clicked) ToggleFocus(u);
        }

        /// <summary>좁은 카드에 쓰는 이름: 아군은 첫 낱말, 테스트 더미는 '더미 N'.</summary>
        static string CardUnitName(FieldUnit u)
        {
            string n = u.Name;
            if (u.IsPlayer)
            {
                int space = n.IndexOf(' ');
                return space > 0 ? n.Substring(0, space) : n;
            }
            return n.StartsWith("테스트 ") ? n.Substring("테스트 ".Length) : n;
        }
    }
}
