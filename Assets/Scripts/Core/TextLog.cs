using System.Collections.Generic;

namespace TextSlayers
{
    public enum LogKind { System, Player, Enemy, Reward, Warning }

    /// <summary>전투 로그. 종류별 색을 리치 텍스트로 붙여 저장한다.</summary>
    public sealed class TextLog
    {
        public const int MaxLines = 200;

        readonly List<string> lines = new List<string>();

        public IReadOnlyList<string> Lines => lines;

        /// <summary>내용이 바뀔 때마다 증가 (화면 자동 스크롤용).</summary>
        public int Version { get; private set; }

        public void Add(string text, LogKind kind = LogKind.System)
        {
            lines.Add($"<color={ColorOf(kind)}>{text}</color>");
            if (lines.Count > MaxLines) lines.RemoveRange(0, lines.Count - MaxLines);
            Version++;
        }

        public void Clear()
        {
            lines.Clear();
            Version++;
        }

        static string ColorOf(LogKind kind) => kind switch
        {
            LogKind.Player => "#8fd3ff",  // 에른의 행동
            LogKind.Enemy => "#ff8f8f",   // 적의 행동
            LogKind.Reward => "#ffd76a",  // 보상
            LogKind.Warning => "#ffb347", // 예고 · 경고
            _ => "#d0d0d0",               // 시스템
        };
    }
}
