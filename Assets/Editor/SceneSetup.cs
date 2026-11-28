using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TextSlayers.EditorTools
{
    /// <summary>
    /// 씬 · 빌드 설정을 코드로 만든다 (씬 파일 직접 편집 금지 규칙).
    /// 메뉴: TextSlayers / 씬 · 빌드 설정 초기화
    /// 배치 모드: -executeMethod TextSlayers.EditorTools.SceneSetup.Setup
    /// </summary>
    public static class SceneSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        internal const string ProductName = "슬레이어즈 워밴드";   // 표시 이름 '슬레이어즈 : 워밴드'. 파일 이름에 ':' 를 쓸 수 없어 뺀다
        static readonly Color BackgroundColor = new Color(0.07f, 0.07f, 0.09f);

        [MenuItem("TextSlayers/씬 · 빌드 설정 초기화")]
        public static void Setup()
        {
            if (File.Exists(ScenePath) && !Application.isBatchMode &&
                !EditorUtility.DisplayDialog("TextSlayers", $"{ScenePath} 를 새로 만듭니다. 계속할까요?", "만들기", "취소"))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            cam.orthographic = true;

            new GameObject("GameRoot").AddComponent<GameRoot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = ProductName;
            AssetDatabase.SaveAssets();

            Debug.Log($"[TextSlayers] SceneSetup 완료: {ScenePath}");
        }
    }
}
