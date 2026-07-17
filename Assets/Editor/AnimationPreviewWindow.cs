using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PetGameEditor
{
    /// <summary>
    /// 动画预览编辑器窗口 - 在编辑器中直接预览动画
    /// 菜单: Window -> PetGame -> 动画预览
    /// </summary>
    public class AnimationPreviewWindow : EditorWindow
    {
        private string animDirectory = "Assets/Art/PetGame/Animations/cat_orange";
        private string currentAnim = "idle";
        private Sprite[] frames;
        private int currentFrame = 0;
        private bool isPlaying = false;
        private double lastTime;
        private float frameRate = 5f;
        private bool isLoop = true;
        private Vector2 scrollPosition;
        private float previewScale = 2f;

        [MenuItem("Window/PetGame/动画预览")]
        public static void ShowWindow()
        {
            GetWindow<AnimationPreviewWindow>("动画预览");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("动画预览工具", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            // 动画目录
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动画目录:", GUILayout.Width(80));
            animDirectory = EditorGUILayout.TextField(animDirectory);
            if (GUILayout.Button("浏览...", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("选择动画目录", "Assets/Art/PetGame/Animations", "");
                if (!string.IsNullOrEmpty(path))
                {
                    animDirectory = path;
                    LoadFrames();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 动画选择
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("动画类型:", GUILayout.Width(80));
            string[] animOptions = new string[] { "idle", "walk", "eat" };
            int selectedIndex = System.Array.IndexOf(animOptions, currentAnim);
            selectedIndex = EditorGUILayout.Popup(selectedIndex, animOptions);
            if (selectedIndex != System.Array.IndexOf(animOptions, currentAnim))
            {
                currentAnim = animOptions[selectedIndex];
                LoadFrames();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 预览区域
            DrawPreviewArea();

            EditorGUILayout.Space(10);

            // 控制区域
            DrawControls();

            EditorGUILayout.Space(10);

            // 信息
            DrawInfo();

            EditorGUILayout.EndScrollView();

            // 自动播放
            if (isPlaying)
            {
                double currentTime = EditorApplication.timeSinceStartup;
                if (currentTime - lastTime >= 1.0 / frameRate)
                {
                    lastTime = currentTime;
                    AdvanceFrame();
                }
                Repaint();
            }
        }

        private void DrawPreviewArea()
        {
            EditorGUILayout.LabelField("预览", EditorStyles.boldLabel);

            Rect previewRect = GUILayoutUtility.GetRect(position.width - 40, 300);
            previewRect.x += 20;
            previewRect.width -= 40;

            // 背景
            EditorGUI.DrawRect(previewRect, new Color(0.9f, 0.9f, 0.9f, 1));

            // 绘制当前帧
            if (frames != null && frames.Length > 0 && currentFrame < frames.Length)
            {
                Sprite sprite = frames[currentFrame];
                if (sprite != null)
                {
                    Rect spriteRect = sprite.rect;
                    float aspect = spriteRect.width / spriteRect.height;
                    float drawHeight = previewRect.height * 0.8f;
                    float drawWidth = drawHeight * aspect;

                    if (drawWidth > previewRect.width * 0.8f)
                    {
                        drawWidth = previewRect.width * 0.8f;
                        drawHeight = drawWidth / aspect;
                    }

                    Rect drawRect = new Rect(
                        previewRect.x + (previewRect.width - drawWidth) / 2,
                        previewRect.y + (previewRect.height - drawHeight) / 2,
                        drawWidth,
                        drawHeight
                    );

                    GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, 
                        new Rect(spriteRect.x / sprite.texture.width, 
                                spriteRect.y / sprite.texture.height,
                                spriteRect.width / sprite.texture.width,
                                spriteRect.height / sprite.texture.height));
                }
            }
            else
            {
                EditorGUI.LabelField(previewRect, "点击'加载帧'按钮加载动画", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(10);
        }

        private void DrawControls()
        {
            EditorGUILayout.LabelField("控制", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(isPlaying ? "暂停" : "播放", GUILayout.Width(80)))
            {
                TogglePlay();
            }

            if (GUILayout.Button("上一帧", GUILayout.Width(80)))
            {
                ShowPrevFrame();
            }

            if (GUILayout.Button("下一帧", GUILayout.Width(80)))
            {
                ShowNextFrame();
            }

            if (GUILayout.Button("加载帧", GUILayout.Width(80)))
            {
                LoadFrames();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 帧率
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("帧率:", GUILayout.Width(50));
            frameRate = EditorGUILayout.Slider(frameRate, 1f, 30f);
            EditorGUILayout.LabelField($"{frameRate:F1} FPS", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            // 循环
            isLoop = EditorGUILayout.Toggle("循环播放", isLoop);

            // 缩放
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预览缩放:", GUILayout.Width(80));
            previewScale = EditorGUILayout.Slider(previewScale, 0.5f, 5f);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInfo()
        {
            EditorGUILayout.LabelField("信息", EditorStyles.boldLabel);

            if (frames != null)
            {
                EditorGUILayout.LabelField($"总帧数: {frames.Length}");
                EditorGUILayout.LabelField($"当前帧: {currentFrame + 1}");
                EditorGUILayout.LabelField($"状态: {(isPlaying ? "播放中" : "已暂停")}");
            }
            else
            {
                EditorGUILayout.LabelField("总帧数: 0 (未加载)");
            }
        }

        private void LoadFrames()
        {
            string path = $"{animDirectory}/{currentAnim}";
            string[] guids = AssetDatabase.FindAssets("t:sprite", new[] { path });
            
            frames = new Sprite[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }

            currentFrame = 0;
            Debug.Log($"加载了 {frames.Length} 帧动画从 {path}");
        }

        private void TogglePlay()
        {
            if (isPlaying)
            {
                isPlaying = false;
            }
            else
            {
                if (frames == null || frames.Length == 0)
                {
                    LoadFrames();
                }
                isPlaying = true;
                lastTime = EditorApplication.timeSinceStartup;
            }
        }

        private void AdvanceFrame()
        {
            if (frames == null || frames.Length == 0) return;

            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                if (isLoop)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = frames.Length - 1;
                    isPlaying = false;
                }
            }
        }

        private void ShowPrevFrame()
        {
            isPlaying = false;
            currentFrame--;
            if (currentFrame < 0)
            {
                currentFrame = frames != null ? frames.Length - 1 : 0;
            }
            Repaint();
        }

        private void ShowNextFrame()
        {
            isPlaying = false;
            currentFrame++;
            if (frames != null && currentFrame >= frames.Length)
            {
                currentFrame = 0;
            }
            Repaint();
        }
    }
}
