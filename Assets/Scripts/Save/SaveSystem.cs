using System;
using System.IO;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>persistentDataPath/save.json 에 JsonUtility 로 저장.</summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";
        public const string LegacyProductName = "TextSlayers";   // 이름 변경 전 제품 이름 (저장 폴더 이름)

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static PlayerProgress Load()
        {
            string path = FilePath;
            if (!File.Exists(path)) CopyLegacySave(Application.persistentDataPath);
            if (!File.Exists(path)) return new PlayerProgress();
            try
            {
                var p = JsonUtility.FromJson<PlayerProgress>(File.ReadAllText(path));
                if (p != null)
                {
                    p.Sanitize();
                    return p;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TextSlayers] 저장 파일 읽기 실패: {e.Message}");
            }

            // 깨진 파일은 덮어쓰기 전에 남겨 둔다.
            try { File.Copy(path, path + ".broken", true); } catch (Exception) { /* 백업 실패는 무시 */ }
            return new PlayerProgress();
        }

        public static bool Save(PlayerProgress progress)
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(progress, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TextSlayers] 저장 실패: {e.Message}");
                return false;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); }
            catch (Exception e) { Debug.LogWarning($"[TextSlayers] 저장 삭제 실패: {e.Message}"); }
            // 옛 저장이 남아 있으면 다음 실행에 다시 복사되므로, 초기 상태를 새 위치에 써 둔다.
            if (File.Exists(LegacyPath(Application.persistentDataPath))) Save(new PlayerProgress());
        }

        /// <summary>옛 저장 파일 위치: 지금 저장 폴더의 옆, 옛 제품 이름 폴더.</summary>
        public static string LegacyPath(string dataDir)
        {
            string parent = Path.GetDirectoryName(dataDir.TrimEnd('/', '\\'));
            return string.IsNullOrEmpty(parent) ? "" : Path.Combine(parent, LegacyProductName, FileName);
        }

        /// <summary>
        /// 새 위치에 저장이 없을 때만 옛 저장을 복사한다 (제품 이름 변경, 2026-11-28). 옛 파일은 지우지 않는다.
        /// PC 처럼 저장 폴더 이름이 제품 이름인 곳에 해당한다. 복사했으면 true.
        /// </summary>
        public static bool CopyLegacySave(string dataDir)
        {
            try
            {
                string target = Path.Combine(dataDir, FileName), legacy = LegacyPath(dataDir);
                if (File.Exists(target) || !File.Exists(legacy)) return false;
                Directory.CreateDirectory(dataDir);
                File.Copy(legacy, target);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[TextSlayers] 옛 저장 가져오기 실패: {e.Message}");
                return false;
            }
        }
    }
}
