using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class PetGameSceneSetup
{
    [MenuItem("Tools/铲屎官疯了/搭建游戏场景")]
    static void SetupGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "PetGameScene";

        var cam = new GameObject("MainCamera", typeof(Camera)).GetComponent<Camera>();
        cam.transform.position = new Vector3(0, 0, -10);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.96f, 0.94f, 0.90f, 1f);
        cam.orthographic = true; cam.orthographicSize = 5;

        new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));

        var ge = new GameObject("GameEntry"); ge.AddComponent<GameEntry>();
        var mgr = new GameObject("PetGameManager"); mgr.AddComponent<PetGameManager>();

        // PetGameUI 自动创建 Canvas 和所有 UI
        new GameObject("PetGameUI").AddComponent<PetGameUI>();

        EnsureBuildSettings("Assets/Scenes/PetGameScene.unity");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PetGameScene.unity");
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("完成",
            "v2 游戏场景已创建\n包含: Camera + EventSystem + GameEntry + PetGameManager + PetGameUI\nUI 由 PetGameUI 运行时自动构建", "好的");
    }

    static void EnsureBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == path))
            list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
