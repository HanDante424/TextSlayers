using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 전투 시작 전 배치 화면 (사용자 2026-11-28): 왼쪽 대기 열의 캐릭터 카드를 필드의 배치 구역으로 끌어다 놓는다.
    /// 배치 규칙은 Battle/Deployment.cs.
    /// </summary>
    public static partial class GameView
    {
        // ── 배치 화면 [임시] ──
        const float PlaceHintH = 28f;
        const float GrabHalfW = 0.7f, GrabUp = 1.2f, GrabDown = 0.6f;   // 놓인 캐릭터를 잡는 범위 (필드 단위, 중심 기준)

        static readonly Color ZoneFill = new Color(0.3f, 0.55f, 0.95f, 0.13f);
        static readonly Color ZoneLine = new Color(0.45f, 0.7f, 1f, 0.9f);
        static readonly Color DropGood = new Color(0.4f, 0.9f, 0.5f, 0.6f);
        static readonly Color DropBad = new Color(1f, 0.35f, 0.3f, 0.6f);

        static string dragId;               // 끌고 있는 캐릭터 (null = 없음)
        static Vector2 dragOffset;          // 잡은 지점에서 캐릭터 중심까지 (필드 단위)
        static Vector2 placeScroll;

        static void DrawPlacement(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null || g.SelectedParty.Count == 0) { dragId = null; g.Go(GameScreen.PartySelect); return; }
            EnsureCardStyles();
            EnsureLoadoutStyles();
            EnsureFx();

            float top = 76f;
            float leftW = RosterColumnW();
            var tray = new Rect(Pad, top, leftW, vh - Pad - top);
            float rightX = Pad * 2 + leftW;
            float by = vh - Pad - ButtonH;
            float areaW = vw - rightX - Pad, areaH = by - 12f - top - PlaceHintH;
            float unit = Mathf.Min(areaW / FieldConst.Width, areaH / FieldConst.Height);
            var field = new Rect(rightX + (areaW - FieldConst.Width * unit) / 2, top + PlaceHintH,
                                 FieldConst.Width * unit, FieldConst.Height * unit);

            HandlePlacementInput(g, tray, field, unit);

            Header($"캐릭터 배치  —  {stage.Name}");
            DrawPlacementTray(g, tray);
            GUI.Label(new Rect(rightX, top, areaW, PlaceHintH),
                "<size=16><color=#aaaaaa>카드를 파란 구역으로 끌어 놓으세요. 놓은 캐릭터는 다시 끌어 옮기고, 오른쪽 클릭하거나 구역 밖 대기 열로 끌면 빠집니다.</color></size>", center);
            DrawPlacementField(g, field, unit);

            float bw = (areaW - Pad * 3) / 4;
            if (GUI.Button(new Rect(rightX, by, bw, ButtonH), stage.IsTest ? "뒤로 (적 전술)" : "뒤로 (전술 · 스킬)", button))
                g.Go(stage.IsTest ? GameScreen.EnemySetup : GameScreen.Loadout);
            if (GUI.Button(new Rect(rightX + (bw + Pad), by, bw, ButtonH), "자동 배치", button)) g.Deploy.AutoPlace(g.SelectedParty);
            if (GUI.Button(new Rect(rightX + (bw + Pad) * 2, by, bw, ButtonH), "배치 초기화", button)) g.Deploy.Clear();
            int placed = 0;
            foreach (var id in g.SelectedParty) if (g.Deploy.IsPlaced(id)) placed++;
            bool ready = placed == g.SelectedParty.Count;
            GUI.enabled = ready;
            if (GUI.Button(new Rect(rightX + (bw + Pad) * 3, by, bw, ButtonH), ready ? "출전" : $"배치 {placed}/{g.SelectedParty.Count}", button))
                g.StartFieldStage();
            GUI.enabled = true;

            DrawDragGhost(g, field, unit);   // 맨 위에
        }

        /// <summary>왼쪽 대기 열: 편성 순서의 카드. 이미 놓은 캐릭터는 흐리게 '배치됨'.</summary>
        static void DrawPlacementTray(GameRoot g, Rect area)
        {
            int n = g.SelectedParty.Count;
            float cw = RosterCardW(), ch = cw * CardAspect;
            placeScroll = GUI.BeginScrollView(area, placeScroll, new Rect(0, 0, cw, n * (ch + RosterGap) - RosterGap));
            for (int i = 0; i < n; i++)
            {
                string id = g.SelectedParty[i];
                var r = new Rect(3f, 3f + i * (ch + RosterGap), cw - 6f, ch - 6f);
                bool placed = g.Deploy.IsPlaced(id);
                Fill(r, DetailBoxColor);
                DrawRosterCard(r, g.Loadout(id), i + 1, false, placed || dragId == id, null);
                if (placed)
                {
                    var strip = new Rect(r.x + CardPad, r.yMax - RosterTagH - CardPad - 18f, r.width - CardPad * 2, 18f);
                    Fill(strip, new Color(0f, 0f, 0f, 0.7f));
                    GUI.Label(strip, "<size=12><color=#8fd3ff>배치됨</color></size>", barText);
                }
            }
            GUI.EndScrollView();
        }

        static Vector2 ToField(Vector2 mouse, Rect field, float unit) => (mouse - field.position) / unit;

        /// <summary>끌어다 놓기 입력. 다른 컨트롤보다 먼저 처리한다.</summary>
        static void HandlePlacementInput(GameRoot g, Rect tray, Rect field, float unit)
        {
            Event e = Event.current;
            Vector2 mouse = e.mousePosition;

            if (e.type == EventType.MouseDown)
            {
                dragId = null;   // 창 밖에서 놓아 놓치고 남은 끌기는 버린다
                string onField = PlacedAt(g, ToField(mouse, field, unit));
                if (e.button == 1)
                {
                    if (onField != null) { g.Deploy.Remove(onField); e.Use(); }
                    return;
                }
                if (e.button != 0) return;

                if (onField != null && field.Contains(mouse))
                {
                    g.Deploy.TryGet(onField, out var at);
                    dragId = onField;
                    dragOffset = at - ToField(mouse, field, unit);
                    e.Use();
                    return;
                }

                float cw = RosterCardW(), ch = cw * CardAspect;
                if (!tray.Contains(mouse) || mouse.x > tray.x + cw) return;      // 스크롤바는 스크롤뷰에 맡긴다
                float y = mouse.y - tray.y + placeScroll.y;
                int i = Mathf.FloorToInt(y / (ch + RosterGap));
                if (i < 0 || i >= g.SelectedParty.Count || y - i * (ch + RosterGap) > ch) return;
                dragId = g.SelectedParty[i];
                dragOffset = Vector2.zero;
                e.Use();
            }
            else if (dragId != null && e.type == EventType.MouseDrag) e.Use();
            else if (dragId != null && e.rawType == EventType.MouseUp)
            {
                if (field.Contains(mouse))
                {
                    var drop = ToField(mouse, field, unit) + dragOffset;
                    if (Deployment.Accepts(drop)) g.Deploy.Place(dragId, drop);
                    else g.ShowToast("파란 배치 구역 안에만 놓을 수 있습니다.");     // 놓여 있던 자리는 그대로
                }
                else if (tray.Contains(mouse)) g.Deploy.Remove(dragId);
                dragId = null;
                e.Use();
            }
        }

        /// <summary>필드 좌표에 놓여 있는 캐릭터 (겹치면 중심이 가까운 쪽). 없으면 null.</summary>
        static string PlacedAt(GameRoot g, Vector2 p)
        {
            string found = null;
            float best = float.MaxValue;
            foreach (var id in g.SelectedParty)
            {
                if (!g.Deploy.TryGet(id, out var at)) continue;
                if (Mathf.Abs(p.x - at.x) > GrabHalfW || p.y < at.y - GrabUp || p.y > at.y + GrabDown) continue;
                float d = Vector2.Distance(p, at);
                if (d < best) { best = d; found = id; }
            }
            return found;
        }

        /// <summary>필드 미리보기: 바닥 · 눈금 · 배치 구역 · 첫 웨이브의 적 · 놓은 아군.</summary>
        static void DrawPlacementField(GameRoot g, Rect r, float unit)
        {
            var stage = g.PendingFieldStage;
            Fill(r, FieldBg);
            Color line = FieldLine;
            if (DrawGround(r, unit, stage)) line.a *= 0.3f;
            for (int i = 1; i < (int)FieldConst.Width; i++) Fill(new Rect(r.x + i * unit, r.y, 1, r.height), line);
            for (int i = 1; i < (int)FieldConst.Height; i++) Fill(new Rect(r.x, r.y + i * unit, r.width, 1), line);

            // 배치 구역: 유닛 중심 범위에 몸 반지름을 더한 영역
            var zone = new Rect(r.x, r.y, (Deployment.MaxX + FieldConst.Radius) * unit, r.height);
            Fill(zone, ZoneFill);
            Fill(new Rect(zone.xMax - 2f, zone.y, 2f, zone.height), ZoneLine);

            // 적 미리보기 (전투와 같은 시작 위치)
            if (stage.IsTest)
            {
                var list = g.DebugEnemies.Entries;
                for (int i = 0; i < list.Count; i++) DrawEnemyPreview(r, unit, list[i].Id, FieldBattle.StartPos(i, list.Count));
            }
            else
            {
                var ids = stage.Waves[0];
                for (int i = 0; i < ids.Length; i++) DrawEnemyPreview(r, unit, ids[i], FieldBattle.StartPos(i, ids.Length));
            }

            // 놓은 아군: 아래쪽에 있을수록 나중에(앞에) 그린다
            var order = new System.Collections.Generic.List<(float Y, int Index)>();
            for (int i = 0; i < g.SelectedParty.Count; i++)
                if (g.Deploy.TryGet(g.SelectedParty[i], out var p)) order.Add((p.y, i));
            order.Sort((a, b) => a.Y.CompareTo(b.Y));
            foreach (var (_, i) in order)
            {
                string id = g.SelectedParty[i];
                g.Deploy.TryGet(id, out var at);
                DrawDeployedUnit(r, unit, id, i + 1, at, dragId == id ? 0.3f : 1f);
            }
        }

        static void DrawEnemyPreview(Rect field, float unit, string enemyId, Vector2 pos)
        {
            float d = FieldConst.Radius * 2f * unit;
            var body = new Rect(field.x + pos.x * unit - d / 2, field.y + pos.y * unit - d / 2, d, d);
            Fill(body, new Color(EnemyColor.r, EnemyColor.g, EnemyColor.b, 0.65f));
            string name = GameData.Enemy(enemyId).Name;
            GUI.Label(body, name.Length > 4 ? name.Substring(name.Length - 4).Trim() : name, small);
        }

        static void DrawDeployedUnit(Rect field, float unit, string id, int order, Vector2 pos, float alpha)
        {
            float d = FieldConst.Radius * 2f * unit;
            float cx = field.x + pos.x * unit, cy = field.y + pos.y * unit;
            var body = new Rect(cx - d / 2, cy - d / 2, d, d);
            DrawFxRect(fxDisc, new Vector2(cx, body.yMax - d * 0.12f), d * 1.1f, d * 0.4f, PlayerColor, 0.6f * alpha);
            var sprite = GetFieldSprite(id);
            if (sprite != null) PaintFieldSprite(FieldSpriteRect(body), sprite.Art, false, new Color(1f, 1f, 1f, alpha));
            else Fill(body, new Color(PlayerColor.r, PlayerColor.g, PlayerColor.b, alpha));
            if (alpha >= 1f)
                GUI.Label(new Rect(cx - 70f, body.yMax - 2f, 140f, 18f), $"<size=13><color=#8fd3ff>{order}</color> {FieldData.Character(id).Name}</size>", small);
        }

        /// <summary>끄는 동안: 마우스를 따라가는 도트와 놓일 자리 표시 (초록 = 가능, 빨강 = 구역 밖).</summary>
        static void DrawDragGhost(GameRoot g, Rect field, float unit)
        {
            if (dragId == null) return;
            Vector2 mouse = Event.current.mousePosition;
            var at = ToField(mouse, field, unit) + dragOffset;
            bool over = field.Contains(mouse), ok = over && Deployment.Accepts(at);
            if (ok) at = Deployment.Clamp(at);
            if (over)
                DrawFxRect(fxDisc, field.position + at * unit, unit * Deployment.MinGap, unit * Deployment.MinGap, ok ? DropGood : DropBad, 1f);

            float d = FieldConst.Radius * 2f * unit;
            Vector2 c = over ? field.position + at * unit : mouse;
            var body = new Rect(c.x - d / 2, c.y - d / 2, d, d);
            var sprite = GetFieldSprite(dragId);
            if (sprite != null) PaintFieldSprite(FieldSpriteRect(body), sprite.Art, false, new Color(1f, 1f, 1f, 0.85f));
            else Fill(body, new Color(PlayerColor.r, PlayerColor.g, PlayerColor.b, 0.85f));
        }
    }
}
