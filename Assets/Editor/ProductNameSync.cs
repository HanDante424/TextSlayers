using UnityEditor;
using UnityEngine;

namespace TextSlayers.EditorTools
{
    /// <summary>
    /// 게임 이름 변경 (사용자 2026-11-28) 반영: 제품 이름이 옛 이름이면 새 이름으로 바꾼다.
    /// ProjectSettings 는 직접 편집하지 않고 에디터 API 로 바꾼다. 에디터가 스크립트를 다시 읽을 때 한 번 적용된다.
    /// 옛 저장 파일은 SaveSystem 이 새 저장 위치로 복사해 온다.
    /// </summary>
    static class ProductNameSync
    {
        [InitializeOnLoadMethod]
        static void Apply()
        {
            if (PlayerSettings.productName != SaveSystem.LegacyProductName) return;
            PlayerSettings.productName = SceneSetup.ProductName;
            AssetDatabase.SaveAssets();
            Debug.Log($"[TextSlayers] 제품 이름 변경: {SaveSystem.LegacyProductName} → {SceneSetup.ProductName}");
        }
    }
}
