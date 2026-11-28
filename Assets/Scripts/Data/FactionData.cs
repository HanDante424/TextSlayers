namespace TextSlayers
{
    /// <summary>소속 하나. Members 는 캐릭터 ID.</summary>
    public sealed class Faction
    {
        public string Name;
        public string[] Members;
    }

    /// <summary>
    /// 소속 표 (기획서 v0.2 5장 "소속 시너지" 표, 사용자 확정). 복수 소속 캐릭터는 소속마다 들어간다.
    /// 지금은 출전 선택 화면의 분류에만 쓴다 (시너지 효과는 미구현). 순서는 기획서 표 순서.
    /// </summary>
    public static class FactionData
    {
        public static readonly Faction[] All =
        {
            F("마법기사단", "ern", "c24", "c25", "c26", "c27", "c28", "c29", "c30", "c31", "c32", "c34", "c35", "c36", "c37", "c48"),
            F("길드", "c06", "c07", "c08", "c09", "c10", "c11", "c12"),
            F("우로보로스", "paracelsus", "c42", "c43", "c44", "c45", "c46", "c47"),
            F("프로토게노이", "c13", "c14", "c15", "c16", "c17", "c18"),
            F("이클립스", "c19", "c20", "c21", "c22", "c23"),
            F("아르스 마그나", "c43", "c44", "c45", "c46", "c47"),
            F("아비스카", "c34", "c35", "c36", "c37", "c48"),
            F("오스렐르크", "c38", "c39", "c40", "c41"),
            F("용병", "stium", "mel", "vendetta"),
            F("성기사단", "c32", "c33"),
            F("양의문", "c23"),
        };

        // 선택 화면에서는 한 캐릭터를 한 번만 보여 준다 (사용자 2026-11-28). 복수 소속이면 아래 소속을 먼저 쓰고,
        // 그 밖에는 표에서 앞선 소속을 쓴다 (분진: 보류 중인 양의문 대신 이클립스).
        static readonly string[] DisplayFirst = { "아비스카", "아르스 마그나", "성기사단" };

        /// <summary>선택 화면에서 이 캐릭터를 보여 줄 소속. 소속이 없으면 null.</summary>
        public static Faction DisplayFaction(string characterId)
        {
            foreach (var name in DisplayFirst)
                foreach (var f in All)
                    if (f.Name == name && System.Array.IndexOf(f.Members, characterId) >= 0) return f;
            foreach (var f in All)
                if (System.Array.IndexOf(f.Members, characterId) >= 0) return f;
            return null;
        }

        static Faction F(string name, params string[] members) => new Faction { Name = name, Members = members };
    }
}
