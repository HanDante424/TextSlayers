using UnityEngine;

namespace TextSlayers
{
    /// <summary>캐릭터 선택 다음 화면: 캐릭터별 전술 · 패시브 1 · 액티브 3 선택 (사용자 2026-11-26).</summary>
    public static partial class GameView
    {
        const float LoadoutRowH = 34f;
        const float LoadoutLabelW = 110f;
        const float LoadoutBlockGap = 14f;
        static Vector2 loadoutScroll;

        static void DrawLoadout(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null || g.SelectedParty.Count == 0) { g.Go(GameScreen.PartySelect); return; }
            Header($"전술 · 스킬 선택  —  {stage.Name}");

            float top = 76f;
            float bottom = vh - Pad - ButtonH - Pad;
            var view = new Rect(Pad, top, vw - Pad * 2, bottom - top);
            float innerW = view.width - 20f;
            float blockH = LoadoutRowH * 4 + 12f;     // 이름 · 전술 · 패시브 · 액티브
            var content = new Rect(0, 0, innerW, g.SelectedParty.Count * (blockH + LoadoutBlockGap));

            Fill(view, Panel);
            loadoutScroll = GUI.BeginScrollView(view, loadoutScroll, content);
            float y = 6f;
            foreach (var id in g.SelectedParty)
            {
                DrawLoadoutBlock(g, id, new Rect(6f, y, innerW - 12f, blockH));
                y += blockH + LoadoutBlockGap;
            }
            GUI.EndScrollView();

            float by = vh - Pad - ButtonH;
            float bw = (vw - Pad * 3) / 2;
            if (GUI.Button(new Rect(Pad, by, bw, ButtonH), "뒤로 (캐릭터 선택)", button)) g.Go(GameScreen.PartySelect);
            bool ready = g.LoadoutReady(out string problem);
            string next = stage.IsTest ? "다음: 적 전술" : $"출전 ({g.SelectedParty.Count}명)";
            string go = ready ? next : $"<size=16><color=#ffb347>{problem}</color></size>";
            if (GUI.Button(new Rect(Pad * 2 + bw, by, bw, ButtonH), go, button)) g.NextFromLoadout();
        }

        // ── 디버그 맵: 더미별 종류 · 전술 (사용자 2026-11-26) ──
        const string DummyMeleeId = "test_dummy_melee", DummyRangedId = "test_dummy_ranged";
        const float EnemyRowH = 40f;
        static Vector2 enemyScroll;

        static void DrawEnemySetup(GameRoot g)
        {
            var stage = g.PendingFieldStage;
            if (stage == null) { g.Go(GameScreen.FieldStageSelect); return; }
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
            if (GUI.Button(new Rect(Pad * 2 + bw2, by, bw2, ButtonH), $"출전 (아군 {g.SelectedParty.Count}명 · 적 {setup.Total}명)", button)) g.StartFieldStage();
        }

        static void DrawLoadoutBlock(GameRoot g, string id, Rect r)
        {
            var slot = g.Loadout(id);
            var c = slot.Def;
            Fill(r, new Color(0.16f, 0.16f, 0.2f));
            float x = r.x + 8f, w = r.width - 16f, y = r.y + 4f;
            bool glutton = slot.Passive == ConceptCharacters.GluttonPassive;

            var role = FieldData.CurrentRole(c, slot.Strategy, glutton ? ConceptCharacters.GluttonSkills : slot.Skills, slot.Passive);
            GUI.Label(new Rect(x, y, w, LoadoutRowH), $"<b>{c.Name}</b>  <size=16><color=#aaaaaa>{c.Kind} · 현재 역할 {FieldData.RoleName(role)}</color></size>", label);
            y += LoadoutRowH;

            // 전술 4종
            GUI.Label(new Rect(x, y, LoadoutLabelW, LoadoutRowH), "<size=17>전술</size>", label);
            var strategies = FieldData.PlayerStrategies;
            float bw = (w - LoadoutLabelW) / strategies.Length;
            for (int i = 0; i < strategies.Length; i++)
            {
                var st = strategies[i];
                if (GUI.Button(new Rect(x + LoadoutLabelW + i * bw, y, bw - 4f, LoadoutRowH - 4f), Pick(FieldData.StrategyName(st), slot.Strategy == st), smallButton))
                    slot.Strategy = st;
            }
            y += LoadoutRowH;

            // 패시브 1 (3 중)
            GUI.Label(new Rect(x, y, LoadoutLabelW, LoadoutRowH), "<size=17>패시브</size>", label);
            int pn = Mathf.Max(1, c.Passives.Length);
            bw = (w - LoadoutLabelW) / pn;
            for (int i = 0; i < c.Passives.Length; i++)
            {
                var pid = c.Passives[i];
                var pd = FieldData.Passive(pid);
                string name = pd != null ? pd.Name : pid;
                if (GUI.Button(new Rect(x + LoadoutLabelW + i * bw, y, bw - 4f, LoadoutRowH - 4f), Pick(name, slot.Passive == pid), smallButton))
                    slot.Passive = pid;
            }
            y += LoadoutRowH;

            // 액티브 3 (6 중). 글러트니면 식 3종으로 고정
            string head = glutton ? "<size=17>액티브 (고정)</size>" : $"<size=17>액티브 {slot.Skills.Length}/{FieldData.MaxEquipActives}</size>";
            GUI.Label(new Rect(x, y, LoadoutLabelW, LoadoutRowH), head, label);
            var list = glutton ? ConceptCharacters.GluttonSkills : c.Skills;
            bw = (w - LoadoutLabelW) / Mathf.Max(1, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                var sd = FieldData.Skill(list[i]);
                string name = sd != null ? sd.Name : list[i];
                bool on = glutton || System.Array.IndexOf(slot.Skills, list[i]) >= 0;
                GUI.enabled = !glutton;
                if (GUI.Button(new Rect(x + LoadoutLabelW + i * bw, y, bw - 4f, LoadoutRowH - 4f), Pick(name, on), smallButton))
                    g.ToggleActive(id, list[i]);
                GUI.enabled = true;
            }
        }

        static string Pick(string text, bool on) => on ? $"<color=#ffd76a><b>[{text}]</b></color>" : $"<color=#bbbbbb>{text}</color>";
    }
}
