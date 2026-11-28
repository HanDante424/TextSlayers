using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 전투 시작 전 아군 배치 (사용자 2026-11-28): 캐릭터 ID → 필드 좌표. 화면과 분리한 순수 규칙.
    /// 아군은 왼쪽 배치 구역 안에만 놓고, 서로 몸이 겹치지 않는다.
    /// </summary>
    public sealed class Deployment
    {
        // ── 배치 구역 [임시] (유닛 중심 좌표) ──
        public const float MinX = FieldConst.Radius;
        public const float MaxX = 7f;
        public const float MinY = FieldConst.Radius;
        public const float MaxY = FieldConst.Height - FieldConst.Radius;
        public const float MinGap = FieldConst.Radius * 2f;      // 중심 사이 최소 거리
        const float Slack = 0.001f;                              // 딱 최소 거리인 두 자리는 겹침이 아니다
        const float SearchStep = 0.5f;                           // 자동 배치가 빈자리를 찾는 간격

        readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();

        public bool IsPlaced(string id) => id != null && positions.ContainsKey(id);
        public bool TryGet(string id, out Vector2 pos) => positions.TryGetValue(id, out pos);
        public void Remove(string id) => positions.Remove(id);
        public void Clear() => positions.Clear();

        /// <summary>놓을 수 있는 지점인가: 구역에서 몸 반지름만큼의 여유까지는 받아 주고 안쪽으로 맞춘다.</summary>
        public static bool Accepts(Vector2 p) =>
            p.x >= MinX - FieldConst.Radius && p.x <= MaxX + FieldConst.Radius &&
            p.y >= MinY - FieldConst.Radius && p.y <= MaxY + FieldConst.Radius;

        public static Vector2 Clamp(Vector2 p) => new Vector2(Mathf.Clamp(p.x, MinX, MaxX), Mathf.Clamp(p.y, MinY, MaxY));

        /// <summary>
        /// 놓기. 다른 캐릭터와 겹치면 가장 가까운 캐릭터의 자리를 차지하고,
        /// 그 캐릭터는 옮긴 쪽의 원래 자리로 간다 (맞바꾸기). 대기 중이던 캐릭터를 놓은 것이면 대기로 돌아간다.
        /// </summary>
        public void Place(string id, Vector2 pos)
        {
            pos = Clamp(pos);
            string other = Overlapping(id, pos);
            if (other != null)
            {
                pos = positions[other];
                if (positions.TryGetValue(id, out var old)) positions[other] = old;
                else positions.Remove(other);
            }
            positions[id] = pos;
        }

        /// <summary>pos 에 놓으면 겹치는 다른 캐릭터 중 가장 가까운 것. 없으면 null.</summary>
        string Overlapping(string self, Vector2 pos)
        {
            string found = null;
            float best = MinGap - Slack;
            foreach (var pair in positions)
            {
                if (pair.Key == self) continue;
                float d = Vector2.Distance(pair.Value, pos);
                if (d < best) { best = d; found = pair.Key; }
            }
            return found;
        }

        /// <summary>편성에서 빠진 캐릭터의 자리를 지운다.</summary>
        public void Retain(IList<string> party)
        {
            var gone = new List<string>();
            foreach (var id in positions.Keys) if (!party.Contains(id)) gone.Add(id);
            foreach (var id in gone) positions.Remove(id);
        }

        public bool AllPlaced(IList<string> party)
        {
            foreach (var id in party) if (!positions.ContainsKey(id)) return false;
            return true;
        }

        /// <summary>배치를 정하지 않았을 때의 시작 위치 (편성 순서 i / count).</summary>
        public static Vector2 DefaultPos(int i, int count) =>
            new Vector2(FieldConst.PlayerStartX + (i % 2) * FieldConst.PlayerStartStagger, FieldConst.Height * (i + 1f) / (count + 1f));

        /// <summary>아직 안 놓은 캐릭터를 기본 시작 위치에 놓는다. 자리가 차 있으면 가장 가까운 빈자리.</summary>
        public void AutoPlace(IList<string> party)
        {
            for (int i = 0; i < party.Count; i++)
                if (!positions.ContainsKey(party[i]))
                    positions[party[i]] = NearestFree(party[i], DefaultPos(i, party.Count));
        }

        Vector2 NearestFree(string id, Vector2 want)
        {
            if (Overlapping(id, want) == null) return want;
            Vector2 best = want;
            float bestDist = float.MaxValue;
            for (float x = MinX; x <= MaxX; x += SearchStep)
                for (float y = MinY; y <= MaxY; y += SearchStep)
                {
                    var p = new Vector2(x, y);
                    float d = Vector2.Distance(p, want);
                    if (d < bestDist && Overlapping(id, p) == null) { best = p; bestDist = d; }
                }
            return best;
        }
    }
}
