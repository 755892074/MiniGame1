using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PetGameEditor
{
    /// <summary>
    /// 动画预览场景设置工具
    /// 菜单: 铲屎官疯了 -> 创建动画预览场景
    /// </summary>
    public class AnimationPreviewSceneSetup
    {
        [MenuItem("铲屎官疯了/创建动画预览场景")]
        public static void CreateAnimationPreviewScene()
        {
            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 创建Canvas
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(750, 1334);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 创建主面板（半透明白色背景）
            GameObject panelGO = CreatePanel("MainPanel", canvasGO.transform, new Color(1, 1, 1, 0.95f));
            RectTransform panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = new Vector2(20, 20);
            panelRT.offsetMax = new Vector2(-20, -20);

            // 标题
            GameObject titleGO = CreateText("Title", panelGO.transform, "动画预览", 48, TextAnchor.MiddleCenter);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -80);
            titleRT.sizeDelta = new Vector2(0, 60);

            // 宠物显示区域（居中）
            GameObject petAreaGO = new GameObject("PetArea", typeof(RectTransform));
            petAreaGO.transform.SetParent(panelGO.transform, false);
            RectTransform petAreaRT = petAreaGO.GetComponent<RectTransform>();
            petAreaRT.anchorMin = new Vector2(0.5f, 0.5f);
            petAreaRT.anchorMax = new Vector2(0.5f, 0.5f);
            petAreaRT.pivot = new Vector2(0.5f, 0.5f);
            petAreaRT.anchoredPosition = new Vector2(0, 100);
            petAreaRT.sizeDelta = new Vector2(300, 300);

            // 宠物Image
            GameObject petImageGO = new GameObject("PetImage", typeof(RectTransform));
            petImageGO.transform.SetParent(petAreaGO.transform, false);
            Image petImage = petImageGO.AddComponent<Image>();
            RectTransform petImageRT = petImageGO.GetComponent<RectTransform>();
            petImageRT.anchorMin = Vector2.zero;
            petImageRT.anchorMax = Vector2.one;
            petImageRT.sizeDelta = Vector2.zero;
            petImageRT.anchoredPosition = Vector2.zero;
            petImage.preserveAspect = true;
            petImage.color = Color.white;

            // 宠物图片背景（浅灰色，方便看清边界）
            GameObject bgGO = new GameObject("PetBg", typeof(RectTransform));
            bgGO.transform.SetParent(petAreaGO.transform, false);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.95f, 0.95f, 0.95f, 1);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = new Vector2(20, 20);
            bgRT.anchoredPosition = Vector2.zero;
            bgGO.transform.SetAsFirstSibling();

            // 控制区域（底部）
            GameObject controlGO = new GameObject("ControlArea", typeof(RectTransform));
            controlGO.transform.SetParent(panelGO.transform, false);
            RectTransform controlRT = controlGO.GetComponent<RectTransform>();
            controlRT.anchorMin = new Vector2(0, 0);
            controlRT.anchorMax = new Vector2(1, 0);
            controlRT.pivot = new Vector2(0.5f, 0);
            controlRT.anchoredPosition = new Vector2(0, 40);
            controlRT.sizeDelta = new Vector2(-40, 350);

            // 宠物选择
            GameObject petLabelGO = CreateText("PetLabel", controlGO.transform, "宠物:", 28, TextAnchor.MiddleRight);
            petLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 280);
            petLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject petDropdownGO = CreateDropdown("PetDropdown", controlGO.transform);
            petDropdownGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 280);
            petDropdownGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 50);

            // 动画选择
            GameObject animLabelGO = CreateText("AnimLabel", controlGO.transform, "动画:", 28, TextAnchor.MiddleRight);
            animLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 210);
            animLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject animDropdownGO = CreateDropdown("AnimDropdown", controlGO.transform);
            animDropdownGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 210);
            animDropdownGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 50);

            // 播放控制按钮
            GameObject playBtnGO = CreateButton("PlayBtn", controlGO.transform, "播放", new Color(0.4f, 0.8f, 0.4f));
            playBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 130);
            playBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject pauseBtnGO = CreateButton("PauseBtn", controlGO.transform, "暂停", new Color(0.8f, 0.6f, 0.3f));
            pauseBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130);
            pauseBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            // 帧控制按钮
            GameObject prevBtnGO = CreateButton("PrevBtn", controlGO.transform, "< 上一帧", new Color(0.6f, 0.6f, 0.6f));
            prevBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 60);
            prevBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject nextBtnGO = CreateButton("NextBtn", controlGO.transform, "下一帧 >", new Color(0.6f, 0.6f, 0.6f));
            nextBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 60);
            nextBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            // 帧信息
            GameObject frameInfoGO = CreateText("FrameInfo", controlGO.transform, "Frame: 1 / 4", 24, TextAnchor.MiddleCenter);
            frameInfoGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, 60);
            frameInfoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40);

            // 速度控制
            GameObject speedLabelGO = CreateText("SpeedLabel", controlGO.transform, "速度:", 24, TextAnchor.MiddleRight);
            speedLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, -20);
            speedLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject speedSliderGO = CreateSlider("SpeedSlider", controlGO.transform);
            speedSliderGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, -20);
            speedSliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            GameObject speedTextGO = CreateText("SpeedText", controlGO.transform, "1.0x", 24, TextAnchor.MiddleLeft);
            speedTextGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, -20);
            speedTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            // 循环切换
            GameObject loopToggleGO = CreateToggle("LoopToggle", controlGO.transform, "循环播放");
            loopToggleGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, -80);
            loopToggleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            // 返回按钮
            GameObject backBtnGO = CreateButton("BackBtn", controlGO.transform, "返回主菜单", new Color(0.5f, 0.5f, 0.5f));
            backBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            backBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);

            // 挂载控制器
            PetGame.AnimationPreviewController controller = canvasGO.AddComponent<PetGame.AnimationPreviewController>();
            controller.petImage = petImage;
            controller.animDropdown = animDropdownGO.GetComponent<Dropdown>();
            controller.playButton = playBtnGO.GetComponent<Button>();
            controller.pauseButton = pauseBtnGO.GetComponent<Button>();
            controller.prevButton = prevBtnGO.GetComponent<Button>();
            controller.nextButton = nextBtnGO.GetComponent<Button>();
            controller.speedSlider = speedSliderGO.GetComponent<Slider>();
            controller.speedText = speedTextGO.GetComponent<Text>();
            controller.frameText = frameInfoGO.GetComponent<Text>();
            controller.loopToggle = loopToggleGO.GetComponent<Toggle>();
            controller.backButton = backBtnGO.GetComponent<Button>();

            // 保存场景
            string scenePath = "Assets/Scenes/AnimationPreviewScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"动画预览场景已创建: {scenePath}");
            Debug.Log("请在场景中配置宠物动画资源引用，然后点击Play测试");
        }

        static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static GameObject CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return go;
        }

        static GameObject CreateButton(string name, Transform parent, string label, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            Button btn = go.AddComponent<Button>();

            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            Text txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.anchoredPosition = Vector2.zero;

            return go;
        }

        static GameObject CreateDropdown(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = Color.white;
            Dropdown dd = go.AddComponent<Dropdown>();

            // 这里简化了Dropdown的创建，实际项目中需要完整的Dropdown模板
            // 但为了简洁，这里只创建基本结构
            return go;
        }

        static GameObject CreateSlider(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0.1f;
            slider.maxValue = 3f;
            slider.value = 1f;

            // 简化了Slider的创建
            return go;
        }

        static GameObject CreateToggle(string name, Transform parent, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Toggle toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;

            GameObject textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            Text txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.color = new Color(0.2f, 0.2f, 0.2f, 1);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = new Vector2(0, 0);
            textRT.anchoredPosition = new Vector2(30, 0);

            return go;
        }
    }
}
