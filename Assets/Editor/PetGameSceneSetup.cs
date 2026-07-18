using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class PetGameSceneSetup
{
    [MenuItem("铲屎官疯了/搭建游戏场景")]
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

        // 动画管理器（橘猫 Idle/Walk/Eat）
        var animMgr = new GameObject("AnimationManager").AddComponent<PetGame.AnimationManager>();
        var catSet = new PetGame.AnimationManager.PetAnimationSet();
        catSet.petName = "cat_orange";
        string animRoot = "Assets/Art/PetGame/Animations/cat_orange";
        if (AssetDatabase.IsValidFolder(animRoot))
        {
            foreach (var sub in AssetDatabase.GetSubFolders(animRoot))
            {
                string animName = Path.GetFileName(sub);
                var clip = new PetGame.AnimationManager.AnimationClip();
                clip.animName = animName;
                clip.isLoop = !string.Equals(animName, "Eat", System.StringComparison.OrdinalIgnoreCase);
                foreach (var f in Directory.GetFiles(sub, "*.png").OrderBy(x => x))
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(f);
                    if (s != null) clip.frames.Add(s);
                }
                catSet.animations.Add(clip);
            }
        }
        animMgr.petAnimationSets.Add(catSet);

        // PetGameUI 自动创建 Canvas 和所有 UI
        var petUI = new GameObject("PetGameUI").AddComponent<PetGameUI>();

        // 场景背景图（作为 PetGameUI 第一个子物体，运行时随其 Canvas 渲染，置于 UI 底层）
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(petUI.transform, false);
        bg.transform.SetAsFirstSibling();
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one; bgRT.sizeDelta = Vector2.zero;
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PetGame/backgrounds/bg_garden.png");
        if (bgSprite != null) { bg.GetComponent<Image>().sprite = bgSprite; bg.GetComponent<Image>().preserveAspect = false; }
        else bg.GetComponent<Image>().color = new Color(0.96f, 0.94f, 0.9f);

        EnsureBuildSettings("Assets/Scenes/PetGameScene.unity");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/PetGameScene.unity");
        AssetDatabase.Refresh();

        PetGameGenUtil.Success(
            "v2 游戏场景已创建\n包含: Camera + EventSystem + GameEntry + PetGameManager + PetGameUI\nUI 由 PetGameUI 运行时自动构建");
    }

    static void EnsureBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == path))
            list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
