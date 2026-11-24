using System;
using System.Collections.Generic;

namespace TextSlayers
{
    /// <summary>저장되는 진행 상황 (기획 v0.1 8장).</summary>
    [Serializable]
    public sealed class PlayerProgress
    {
        public int Level = 1;
        public int Exp;
        public int Gold;
        public int Memory;
        public List<string> ClearedStages = new List<string>();

        public bool IsCleared(string stageId) => ClearedStages.Contains(stageId);

        /// <summary>경험치를 더하고 오른 레벨 수를 돌려준다.</summary>
        public int AddExp(int amount)
        {
            int ups = 0;
            Exp += Math.Max(0, amount);
            while (Level < GameData.MaxLevel && Exp >= GameData.ExpToNext(Level))
            {
                Exp -= GameData.ExpToNext(Level);
                Level++;
                ups++;
            }
            if (Level >= GameData.MaxLevel) Exp = 0;
            return ups;
        }

        /// <summary>읽어 온 값이 깨졌을 때 보정.</summary>
        public void Sanitize()
        {
            if (ClearedStages == null) ClearedStages = new List<string>();
            Level = Math.Clamp(Level, 1, GameData.MaxLevel);
            Exp = Math.Max(0, Exp);
            Gold = Math.Max(0, Gold);
            Memory = Math.Max(0, Memory);
        }
    }
}
