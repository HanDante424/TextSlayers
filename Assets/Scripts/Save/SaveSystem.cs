using System;
using System.IO;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>persistentDataPath/save.json 에 JsonUtility 로 저장.</summary>
    public static class SaveSystem
    {
        const string FileName = "save.json";

        static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static PlayerProgress Load()
        {
            string path = FilePath;
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
        }
    }
}
