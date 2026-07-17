using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace PetGame
{
    /// <summary>
    /// 动画预览控制器 - 用于在编辑器中查看和测试宠物动画
    /// 挂载在AnimationPreviewScene的Canvas上
    /// </summary>
    public class AnimationPreviewController : MonoBehaviour
    {
        [Header("UI References")]
        public Image petImage;
        public Dropdown petDropdown;
        public Dropdown animDropdown;
        public Button playButton;
        public Button pauseButton;
        public Button prevButton;
        public Button nextButton;
        public Slider speedSlider;
        public Text speedText;
        public Text frameText;
        public Toggle loopToggle;
        public Button backButton;

        [Header("Animation Data")]
        public List<PetAnimationData> petAnimations = new List<PetAnimationData>();

        private AnimationManager animManager;
        private int currentFrameIndex = 0;
        private bool isPlaying = false;
        private float playTimer = 0f;
        private float frameInterval = 0.2f;

        [System.Serializable]
        public class PetAnimationData
        {
            public string petName;
            public List<AnimationClip> animations = new List<AnimationClip>();
        }

        [System.Serializable]
        public class AnimationClip
        {
            public string animName;
            public List<Sprite> frames = new List<Sprite>();
            public bool isLoop = true;
        }

        void Start()
        {
            animManager = AnimationManager.Instance;
            if (animManager == null)
            {
                GameObject go = new GameObject("AnimationManager");
                animManager = go.AddComponent<AnimationManager>();
            }

            SetupUI();
            LoadPetAnimations();
        }

        void SetupUI()
        {
            // 宠物下拉
            petDropdown.ClearOptions();
            List<string> petNames = new List<string>();
            foreach (var pet in petAnimations)
            {
                petNames.Add(pet.petName);
            }
            petDropdown.AddOptions(petNames);
            petDropdown.onValueChanged.AddListener(OnPetChanged);

            // 动画下拉
            animDropdown.ClearOptions();
            animDropdown.onValueChanged.AddListener(OnAnimChanged);

            // 按钮
            playButton.onClick.AddListener(PlayAnimation);
            pauseButton.onClick.AddListener(PauseAnimation);
            prevButton.onClick.AddListener(ShowPrevFrame);
            nextButton.onClick.AddListener(ShowNextFrame);
            backButton.onClick.AddListener(() => SceneManager.LoadScene("MenuScene"));

            // 速度滑块
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            speedSlider.value = 1f;
            OnSpeedChanged(1f);

            // 循环
            loopToggle.onValueChanged.AddListener(OnLoopChanged);
            loopToggle.isOn = true;
        }

        void LoadPetAnimations()
        {
            // 这里会从Resources加载实际的动画数据
            // 目前先手动设置橘猫的数据
            PetAnimationData catOrange = new PetAnimationData();
            catOrange.petName = "橘猫";

            // Idle动画
            AnimationClip idle = new AnimationClip();
            idle.animName = "Idle";
            idle.isLoop = true;
            catOrange.animations.Add(idle);

            // Walk动画
            AnimationClip walk = new AnimationClip();
            walk.animName = "Walk";
            walk.isLoop = true;
            catOrange.animations.Add(walk);

            // Eat动画
            AnimationClip eat = new AnimationClip();
            eat.animName = "Eat";
            eat.isLoop = false;
            catOrange.animations.Add(eat);

            petAnimations.Add(catOrange);

            // 更新下拉
            OnPetChanged(0);
        }

        void OnPetChanged(int index)
        {
            if (index < 0 || index >= petAnimations.Count) return;

            animDropdown.ClearOptions();
            List<string> animNames = new List<string>();
            foreach (var anim in petAnimations[index].animations)
            {
                animNames.Add(anim.animName);
            }
            animDropdown.AddOptions(animNames);

            OnAnimChanged(0);
        }

        void OnAnimChanged(int index)
        {
            currentFrameIndex = 0;
            isPlaying = false;
            UpdateFrameDisplay();
        }

        void Update()
        {
            if (!isPlaying) return;

            playTimer += Time.deltaTime;
            if (playTimer >= frameInterval)
            {
                playTimer = 0f;
                AdvanceFrame();
            }
        }

        void AdvanceFrame()
        {
            var currentPet = petAnimations[petDropdown.value];
            var currentAnim = currentPet.animations[animDropdown.value];

            currentFrameIndex++;
            if (currentFrameIndex >= currentAnim.frames.Count)
            {
                if (currentAnim.isLoop && loopToggle.isOn)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = currentAnim.frames.Count - 1;
                    isPlaying = false;
                }
            }

            UpdateFrameDisplay();
        }

        void UpdateFrameDisplay()
        {
            var currentPet = petAnimations[petDropdown.value];
            var currentAnim = currentPet.animations[animDropdown.value];

            if (currentFrameIndex < currentAnim.frames.Count)
            {
                petImage.sprite = currentAnim.frames[currentFrameIndex];
            }

            frameText.text = $"Frame: {currentFrameIndex + 1} / {currentAnim.frames.Count}";
        }

        public void PlayAnimation()
        {
            isPlaying = true;
            playTimer = 0f;
        }

        public void PauseAnimation()
        {
            isPlaying = false;
        }

        public void ShowPrevFrame()
        {
            isPlaying = false;
            currentFrameIndex--;
            if (currentFrameIndex < 0)
            {
                var currentPet = petAnimations[petDropdown.value];
                var currentAnim = currentPet.animations[animDropdown.value];
                currentFrameIndex = currentAnim.frames.Count - 1;
            }
            UpdateFrameDisplay();
        }

        public void ShowNextFrame()
        {
            isPlaying = false;
            currentFrameIndex++;
            var currentPet = petAnimations[petDropdown.value];
            var currentAnim = currentPet.animations[animDropdown.value];
            if (currentFrameIndex >= currentAnim.frames.Count)
            {
                currentFrameIndex = 0;
            }
            UpdateFrameDisplay();
        }

        void OnSpeedChanged(float value)
        {
            frameInterval = 1f / (value * 5f); // 1x = 0.2s per frame (5 fps)
            speedText.text = $"Speed: {value:F1}x";
        }

        void OnLoopChanged(bool isLoop)
        {
            // 循环状态已更新
        }
    }
}
