using System.Collections.Generic;
using UnityEngine;

namespace PetGame
{
    /// <summary>
    /// 动画管理器 - 全局单例，管理所有宠物的动画播放
    /// </summary>
    public class AnimationManager : MonoBehaviour
    {
        public static AnimationManager Instance { get; private set; }

        [Header("Animation Settings")]
        [Tooltip("默认动画播放速度 (帧/秒)")]
        public float defaultFrameRate = 5f;

        [Tooltip("是否使用平滑插值")]
        public bool useSmoothInterpolation = false;

        [Header("Pet Animations")]
        public List<PetAnimationSet> petAnimationSets = new List<PetAnimationSet>();

        // 运行时动画状态
        private Dictionary<string, AnimationPlayer> activePlayers = new Dictionary<string, AnimationPlayer>();

        [System.Serializable]
        public class PetAnimationSet
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
            public float frameRate = 5f;
        }

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 注册动画播放器
        /// </summary>
        public void RegisterPlayer(string playerId, AnimationPlayer player)
        {
            if (!activePlayers.ContainsKey(playerId))
            {
                activePlayers[playerId] = player;
            }
        }

        /// <summary>
        /// 注销动画播放器
        /// </summary>
        public void UnregisterPlayer(string playerId)
        {
            if (activePlayers.ContainsKey(playerId))
            {
                activePlayers.Remove(playerId);
            }
        }

        /// <summary>
        /// 获取宠物动画集
        /// </summary>
        public PetAnimationSet GetPetAnimationSet(string petName)
        {
            return petAnimationSets.Find(p => p.petName == petName);
        }

        /// <summary>
        /// 获取动画片段
        /// </summary>
        public AnimationClip GetAnimationClip(string petName, string animName)
        {
            var petSet = GetPetAnimationSet(petName);
            if (petSet == null) return null;
            // 容忍大小写差异：搭建场景时按文件夹名存成 "idle"/"walk"/"eat"，
            // 而运行时用的是 Play("Idle"/"Walk"/"Eat")，必须不区分大小写匹配。
            return petSet.animations.Find(a => string.Equals(a.animName, animName, System.StringComparison.OrdinalIgnoreCase));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>
    /// 动画播放器组件 - 挂载在需要播放动画的GameObject上
    /// </summary>
    public class AnimationPlayer : MonoBehaviour
    {
        [Header("Settings")]
        public string petName;
        public string currentAnimName;
        public float frameRate = 5f;
        public bool isLoop = true;
        public bool autoPlay = false;

        [Header("References")]
        public SpriteRenderer spriteRenderer;
        public UnityEngine.UI.Image uiImage;

        private AnimationManager.AnimationClip currentClip;
        private int currentFrameIndex = 0;
        private float playTimer = 0f;
        private bool isPlaying = false;
        private bool isPaused = false;

        void Start()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (uiImage == null) uiImage = GetComponent<UnityEngine.UI.Image>();

            AnimationManager.Instance?.RegisterPlayer(gameObject.name, this);

            if (autoPlay && !string.IsNullOrEmpty(currentAnimName))
            {
                Play(currentAnimName);
            }
        }

        void Update()
        {
            if (!isPlaying || isPaused) return;

            playTimer += Time.deltaTime;
            float frameInterval = 1f / frameRate;

            if (playTimer >= frameInterval)
            {
                playTimer = 0f;
                AdvanceFrame();
            }
        }

        /// <summary>
        /// 播放指定动画
        /// </summary>
        public void Play(string animName)
        {
            if (AnimationManager.Instance == null) return;

            currentClip = AnimationManager.Instance.GetAnimationClip(petName, animName);
            if (currentClip == null)
            {
                Debug.LogWarning($"Animation not found: {petName}.{animName}");
                return;
            }

            currentAnimName = animName;
            currentFrameIndex = 0;
            isLoop = currentClip.isLoop;
            frameRate = currentClip.frameRate > 0 ? currentClip.frameRate : AnimationManager.Instance.defaultFrameRate;
            isPlaying = true;
            isPaused = false;
            playTimer = 0f;

            UpdateFrameDisplay();
        }

        /// <summary>
        /// 暂停动画
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// 恢复动画
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// 停止动画
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;
            currentFrameIndex = 0;
            UpdateFrameDisplay();
        }

        void AdvanceFrame()
        {
            if (currentClip == null) return;

            currentFrameIndex++;
            if (currentFrameIndex >= currentClip.frames.Count)
            {
                if (isLoop)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = currentClip.frames.Count - 1;
                    isPlaying = false;
                    OnAnimationComplete();
                }
            }

            UpdateFrameDisplay();
        }

        void UpdateFrameDisplay()
        {
            if (currentClip == null || currentClip.frames.Count == 0) return;

            Sprite frame = currentClip.frames[currentFrameIndex];

            if (spriteRenderer != null)
                spriteRenderer.sprite = frame;

            if (uiImage != null)
                uiImage.sprite = frame;
        }

        void OnAnimationComplete()
        {
            // 动画完成回调，可以在这里触发事件
            Debug.Log($"Animation complete: {currentAnimName}");
        }

        void OnDestroy()
        {
            AnimationManager.Instance?.UnregisterPlayer(gameObject.name);
        }
    }
}
