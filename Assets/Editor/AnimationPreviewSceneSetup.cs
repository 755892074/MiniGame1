using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PetGameEditor
{
    /// <summary>
    /// 动画预览场景设置工具
    /// 菜单: 铲屎官疯了 -> 创建动画预览场景
    /// </summary>
    public class AnimationPreviewSceneSetup
    {
        // 宠物文件夹名 -> 显示名（决定下拉顺序）
        static readonly string[] PetOrder = { "cat", "dog", "parrot", "fish", "hamster", "rabbit" };
        static readonly Dictionary<string, string> PetDisplay = new Dictionary<string, string>
        {
            { "cat", "橘猫" }, { "dog", "柴犬" }, { "parrot", "鹦鹉" },
            { "fish", "金鱼" }, { "hamster", "仓鼠" }, { "rabbit", "垂耳兔" }
        };

        const string AnimRoot = "Assets/Art/PetGame/Animations/cat_orange";
        const string PetRoot = "Assets/Art/PetGame/pets";
        const string BgPath = "Assets/Art/PetGame/backgrounds/bg_room_cozy.png";

        [MenuItem("铲屎官疯了/创建动画预览场景")]
        public static void CreateAnimationPreviewScene()
        {
            // 空场景（ScreenSpaceOverlay 不需要相机）
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ===== EventSystem（关键：否则所有 UI 点不动） =====
            GameObject esGO = new GameObject("EventSystem", typeof(EventSystem));
            esGO.AddComponent<StandaloneInputModule>();

            // ===== Canvas =====
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(750, 1334);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // ===== 全屏背景图（拉入新生成的背景美术） =====
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            GameObject bgGO = new GameObject("BgImage", typeof(RectTransform));
            bgGO.transform.SetParent(canvasGO.transform, false);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.preserveAspect = false;
            bgImg.color = Color.white;
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.anchoredPosition = Vector2.zero;

            // ===== 标题 =====
            GameObject titleGO = CreateText("Title", canvasGO.transform, "动画预览", 44, TextAnchor.MiddleCenter, Color.white);
            RectTransform titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.anchoredPosition = new Vector2(0, -50);
            titleRT.sizeDelta = new Vector2(0, 60);

            // ===== 底部控制栏（半透明深色，提升可读性） =====
            GameObject controlGO = CreatePanel("ControlBar", canvasGO.transform, new Color(0.1f, 0.1f, 0.12f, 0.72f));
            RectTransform controlRT = controlGO.GetComponent<RectTransform>();
            controlRT.anchorMin = new Vector2(0, 0);
            controlRT.anchorMax = new Vector2(1, 0);
            controlRT.pivot = new Vector2(0.5f, 0);
            controlRT.anchoredPosition = Vector2.zero;
            controlRT.sizeDelta = new Vector2(0, 420);

            // 宠物显示区域（居中偏上）
            GameObject petAreaGO = new GameObject("PetArea", typeof(RectTransform));
            petAreaGO.transform.SetParent(canvasGO.transform, false);
            RectTransform petAreaRT = petAreaGO.GetComponent<RectTransform>();
            petAreaRT.anchorMin = new Vector2(0.5f, 0.5f);
            petAreaRT.anchorMax = new Vector2(0.5f, 0.5f);
            petAreaRT.pivot = new Vector2(0.5f, 0.5f);
            petAreaRT.anchoredPosition = new Vector2(0, 140);
            petAreaRT.sizeDelta = new Vector2(320, 380);

            GameObject petImageGO = new GameObject("PetImage", typeof(RectTransform));
            petImageGO.transform.SetParent(petAreaGO.transform, false);
            Image petImage = petImageGO.AddComponent<Image>();
            petImage.preserveAspect = true;
            petImage.color = Color.white;
            RectTransform petImageRT = petImageGO.GetComponent<RectTransform>();
            petImageRT.anchorMin = Vector2.zero;
            petImageRT.anchorMax = Vector2.one;
            petImageRT.sizeDelta = Vector2.zero;
            petImageRT.anchoredPosition = Vector2.zero;

            // ===== 控制栏控件 =====
            GameObject petLabelGO = CreateText("PetLabel", controlGO.transform, "宠物:", 26, TextAnchor.MiddleRight, Color.white);
            petLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 360);
            petLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject petDropdownGO = CreateDropdown("PetDropdown", controlGO.transform);
            petDropdownGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 360);
            petDropdownGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 50);

            GameObject animLabelGO = CreateText("AnimLabel", controlGO.transform, "动画:", 26, TextAnchor.MiddleRight, Color.white);
            animLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 290);
            animLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject animDropdownGO = CreateDropdown("AnimDropdown", controlGO.transform);
            animDropdownGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 290);
            animDropdownGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 50);

            GameObject playBtnGO = CreateButton("PlayBtn", controlGO.transform, "播放", new Color(0.27f, 0.7f, 0.36f));
            playBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 210);
            playBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject pauseBtnGO = CreateButton("PauseBtn", controlGO.transform, "暂停", new Color(0.85f, 0.6f, 0.2f));
            pauseBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 210);
            pauseBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject prevBtnGO = CreateButton("PrevBtn", controlGO.transform, "< 上一帧", new Color(0.4f, 0.45f, 0.5f));
            prevBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150, 140);
            prevBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject nextBtnGO = CreateButton("NextBtn", controlGO.transform, "下一帧 >", new Color(0.4f, 0.45f, 0.5f));
            nextBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 140);
            nextBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);

            GameObject frameInfoGO = CreateText("FrameInfo", controlGO.transform, "Frame: 1 / 1", 22, TextAnchor.MiddleCenter, Color.white);
            frameInfoGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(150, 140);
            frameInfoGO.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 40);

            GameObject speedLabelGO = CreateText("SpeedLabel", controlGO.transform, "速度:", 22, TextAnchor.MiddleRight, Color.white);
            speedLabelGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200, 70);
            speedLabelGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject speedSliderGO = CreateSlider("SpeedSlider", controlGO.transform);
            speedSliderGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 70);
            speedSliderGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            GameObject speedTextGO = CreateText("SpeedText", controlGO.transform, "1.0x", 22, TextAnchor.MiddleLeft, Color.white);
            speedTextGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(200, 70);
            speedTextGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 40);

            GameObject loopToggleGO = CreateToggle("LoopToggle", controlGO.transform, "循环播放");
            loopToggleGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, 10);
            loopToggleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 40);

            GameObject backBtnGO = CreateButton("BackBtn", controlGO.transform, "返回菜单", new Color(0.4f, 0.4f, 0.45f));
            backBtnGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
            backBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);

            // ===== 控制器 + 真正加载精灵数据 =====
            PetGame.AnimationPreviewController controller = canvasGO.AddComponent<PetGame.AnimationPreviewController>();
            controller.petImage = petImage;
            controller.petDropdown = petDropdownGO.GetComponent<Dropdown>();
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

            // 关键：把真实精灵帧塞进控制器，保存后运行时直接可用
            controller.petAnimations = BuildPetAnimationData();

            // 保存场景
            string scenePath = "Assets/Scenes/AnimationPreviewScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            AssetDatabase.SaveAssets();
            Debug.Log($"动画预览场景已创建: {scenePath}，已加载 {controller.petAnimations.Count} 只宠物的动画数据");
        }

        /// <summary>
        /// 从磁盘加载真实精灵，构建预览数据：
        /// - 每只宠物都有「表情」clip（7 张表情图）
        /// - 橘猫额外有 Idle / Walk / Eat 三套动画帧
        /// </summary>
        static List<PetGame.AnimationPreviewController.PetAnimationData> BuildPetAnimationData()
        {
            var list = new List<PetGame.AnimationPreviewController.PetAnimationData>();

            foreach (var key in PetOrder)
            {
                string petFolder = $"{PetRoot}/{key}";
                if (!AssetDatabase.IsValidFolder(petFolder)) continue;

                var pad = new PetGame.AnimationPreviewController.PetAnimationData();
                pad.petName = PetDisplay[key];

                // 表情合集
                var expr = new PetGame.AnimationPreviewController.AnimationClip();
                expr.animName = "表情";
                expr.isLoop = true;
                foreach (var f in Directory.GetFiles(petFolder, "*.png").OrderBy(x => x))
                {
                    var s = LoadSprite(f);
                    if (s != null) expr.frames.Add(s);
                }
                pad.animations.Add(expr);

                // 橘猫动画
                if (key == "cat" && AssetDatabase.IsValidFolder(AnimRoot))
                {
                    foreach (var sub in AssetDatabase.GetSubFolders(AnimRoot))
                    {
                        string animName = Path.GetFileName(sub);
                        var clip = new PetGame.AnimationPreviewController.AnimationClip();
                        clip.animName = animName; // Idle / Walk / Eat
                        clip.isLoop = !string.Equals(animName, "Eat", System.StringComparison.OrdinalIgnoreCase);
                        foreach (var f in Directory.GetFiles(sub, "*.png").OrderBy(x => x))
                        {
                            var s = LoadSprite(f);
                            if (s != null) clip.frames.Add(s);
                        }
                        pad.animations.Add(clip);
                    }
                }

                list.Add(pad);
            }
            return list;
        }

        static Sprite LoadSprite(string path)
        {
            // 兜底：确保是 Sprite 导入（已是 Sprite，这里基本走不到）
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ============ UI 辅助方法 ============

        static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        static GameObject CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text txt = go.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            SetFont(txt);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        static void SetFont(Text txt)
        {
            // 优先用内置 Arial；拿不到则保留 AddComponent 时赋的默认字体，避免字体为 null 导致文字不显示
            Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (arial != null) txt.font = arial;
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
            SetFont(txt);
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
            Image img = go.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 1);
            Dropdown dd = go.AddComponent<Dropdown>();

            GameObject labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            Text labelTxt = labelGO.AddComponent<Text>();
            labelTxt.text = "Select...";
            labelTxt.fontSize = 24;
            labelTxt.color = new Color(0.15f, 0.15f, 0.15f, 1);
            SetFont(labelTxt);
            RectTransform labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(10, 5);
            labelRT.offsetMax = new Vector2(-25, -5);
            dd.captionText = labelTxt;

            GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
            arrowGO.transform.SetParent(go.transform, false);
            Image arrowImg = arrowGO.AddComponent<Image>();
            arrowImg.color = new Color(0.2f, 0.2f, 0.2f, 1);
            RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1, 0.5f);
            arrowRT.anchorMax = new Vector2(1, 0.5f);
            arrowRT.pivot = new Vector2(1, 0.5f);
            arrowRT.anchoredPosition = new Vector2(-10, 0);
            arrowRT.sizeDelta = new Vector2(20, 20);
            dd.targetGraphic = arrowImg;

            GameObject templateGO = new GameObject("Template", typeof(RectTransform));
            templateGO.transform.SetParent(go.transform, false);
            Image templateImg = templateGO.AddComponent<Image>();
            templateImg.color = new Color(1, 1, 1, 1);
            RectTransform templateRT = templateGO.GetComponent<RectTransform>();
            templateRT.anchorMin = new Vector2(0, 0);
            templateRT.anchorMax = new Vector2(1, 0);
            templateRT.pivot = new Vector2(0.5f, 1);
            templateRT.anchoredPosition = new Vector2(0, 2);
            templateRT.sizeDelta = new Vector2(0, 150);
            dd.template = templateRT;

            GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(templateGO.transform, false);
            RectTransform viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.sizeDelta = Vector2.zero;
            viewportRT.anchoredPosition = Vector2.zero;
            Image viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(1, 1, 1, 1);

            GameObject contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            RectTransform contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.sizeDelta = new Vector2(0, 150);
            contentRT.anchoredPosition = Vector2.zero;

            GameObject itemGO = new GameObject("Item", typeof(RectTransform));
            itemGO.transform.SetParent(contentGO.transform, false);
            RectTransform itemRT = itemGO.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0, 1);
            itemRT.anchorMax = new Vector2(1, 1);
            itemRT.pivot = new Vector2(0.5f, 1);
            itemRT.anchoredPosition = new Vector2(0, 0);
            itemRT.sizeDelta = new Vector2(0, 40);
            Toggle itemToggle = itemGO.AddComponent<Toggle>();

            GameObject itemBgGO = new GameObject("Item Background", typeof(RectTransform));
            itemBgGO.transform.SetParent(itemGO.transform, false);
            Image itemBgImg = itemBgGO.AddComponent<Image>();
            itemBgImg.color = new Color(1, 1, 1, 1);
            RectTransform itemBgRT = itemBgGO.GetComponent<RectTransform>();
            itemBgRT.anchorMin = Vector2.zero;
            itemBgRT.anchorMax = Vector2.one;
            itemBgRT.sizeDelta = Vector2.zero;
            itemBgRT.anchoredPosition = Vector2.zero;
            itemToggle.targetGraphic = itemBgImg;

            GameObject itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            Text itemLabelTxt = itemLabelGO.AddComponent<Text>();
            itemLabelTxt.text = "Option";
            itemLabelTxt.fontSize = 22;
            itemLabelTxt.color = new Color(0.15f, 0.15f, 0.15f, 1);
            SetFont(itemLabelTxt);
            RectTransform itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.offsetMin = new Vector2(10, 5);
            itemLabelRT.offsetMax = new Vector2(-10, -5);

            dd.itemText = itemLabelTxt;

            templateGO.SetActive(false);
            return go;
        }

        static GameObject CreateSlider(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            Slider slider = go.AddComponent<Slider>();
            slider.minValue = 0.1f;
            slider.maxValue = 3f;
            slider.value = 1f;
            slider.wholeNumbers = false;

            GameObject bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(go.transform, false);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.9f, 0.9f, 0.9f, 1);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = new Vector2(0, 6);
            bgRT.offsetMax = new Vector2(0, -6);

            GameObject fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(go.transform, false);
            RectTransform fillAreaRT = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = Vector2.zero;
            fillAreaRT.anchorMax = Vector2.one;
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-15, 0);

            GameObject fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            Image fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(0.3f, 0.7f, 0.9f, 1);
            RectTransform fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.sizeDelta = Vector2.zero;
            fillRT.anchoredPosition = Vector2.zero;

            slider.fillRect = fillRT;

            GameObject handleAreaGO = new GameObject("Handle Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(go.transform, false);
            RectTransform handleAreaRT = handleAreaGO.GetComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(5, 0);
            handleAreaRT.offsetMax = new Vector2(-15, 0);

            GameObject handleGO = new GameObject("Handle", typeof(RectTransform));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            Image handleImg = handleGO.AddComponent<Image>();
            handleImg.color = new Color(1, 1, 1, 1);
            RectTransform handleRT = handleGO.GetComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0.5f, 0.5f);
            handleRT.anchorMax = new Vector2(0.5f, 0.5f);
            handleRT.pivot = new Vector2(0.5f, 0.5f);
            handleRT.sizeDelta = new Vector2(20, 20);
            handleRT.anchoredPosition = Vector2.zero;

            slider.handleRect = handleRT;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;

            go.AddComponent<CanvasGroup>();
            return go;
        }

        static GameObject CreateToggle(string name, Transform parent, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(go.transform, false);
            Image bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.85f, 0.85f, 0.85f, 1);
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.anchoredPosition = Vector2.zero;

            Toggle toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;
            toggle.targetGraphic = bgImg;

            GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform));
            checkGO.transform.SetParent(go.transform, false);
            Image checkImg = checkGO.AddComponent<Image>();
            checkImg.color = new Color(0.27f, 0.7f, 0.36f, 1);
            RectTransform checkRT = checkGO.GetComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0, 0.5f);
            checkRT.anchorMax = new Vector2(0, 0.5f);
            checkRT.pivot = new Vector2(0, 0.5f);
            checkRT.anchoredPosition = new Vector2(5, 0);
            checkRT.sizeDelta = new Vector2(24, 24);

            GameObject textGO = new GameObject("Label", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            Text txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 24;
            txt.color = Color.white;
            SetFont(txt);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(34, 0);
            textRT.offsetMax = new Vector2(0, 0);

            return go;
        }
    }
}
