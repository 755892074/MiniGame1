using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 关卡编辑器 — 可视化精灵预览 + 编辑每关宠物/食物/碗配置
/// 菜单：铲屎官疯了 → 关卡编辑器
/// </summary>
public class LevelEditorWindow : EditorWindow
{
    private Vector2 scroll;
    private int selectedLevel = 1;
    private string[] levelOptions;

    // 编辑中的关卡数据
    private string editName = "";
    private int editCapacity = 3;
    private int editExtraBowls = 1;
    private int editDifficulty;
    private int editTargetScore;

    private List<PetType> editPets = new List<PetType>();

    private class EditableBowl { public List<FoodType> foods = new List<FoodType>(); }
    private List<EditableBowl> editBowls = new List<EditableBowl>();

    private string generationResult = "";
    private int previewSeed = 137;

    // 精灵缓存
    private Dictionary<PetType, Sprite> petSprites = new Dictionary<PetType, Sprite>();
    private Dictionary<FoodType, Sprite> foodSprites = new Dictionary<FoodType, Sprite>();
    private bool spritesLoaded;

    private const float PET_ICON = 50f;
    private const float FOOD_ICON = 32f;

    [MenuItem("铲屎官疯了/关卡编辑器")]
    public static void ShowWindow()
    {
        var win = GetWindow<LevelEditorWindow>("关卡编辑器");
        win.minSize = new Vector2(500, 600);
        win.Show();
    }

    void OnEnable() { RefreshLevelList(); LoadSprites(); }
    void OnFocus() { LoadSprites(); }

    void LoadSprites()
    {
        if (!Application.isPlaying || spritesLoaded) return;
        petSprites.Clear();
        foodSprites.Clear();

        // 加载宠物精灵
        foreach (PetType pet in System.Enum.GetValues(typeof(PetType)))
        {
            var path = $"PetFaces/{pet.ToString().ToLower()}/neutral";
            var s = Resources.Load<Sprite>(path);
            if (s != null) petSprites[pet] = s;
        }

        // 加载食物精灵 — 与 PetGameUI.GetFoodSprite 一致
        foreach (FoodType food in System.Enum.GetValues(typeof(FoodType)))
        {
            int idx = (Mathf.Abs(food.GetHashCode()) % 15) + 1;
            var s = Resources.Load<Sprite>($"ArtFoods/food{idx:D2}");
            if (s != null) foodSprites[food] = s;
        }

        spritesLoaded = true;
    }

    void RefreshLevelList()
    {
        var gm = GameObject.FindObjectOfType<PetGameManager>();
        if (gm == null) { levelOptions = new[] { "— 请运行游戏加载关卡 —" }; return; }
        levelOptions = new string[gm.LevelCount];
        for (int i = 0; i < gm.LevelCount; i++)
            levelOptions[i] = $"关卡{i + 1}: {gm.GetLevelName(i + 1)}";
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("铲屎官疯了 — 关卡编辑器", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        // ===== 关卡选择 =====
        var gm = GameObject.FindObjectOfType<PetGameManager>();
        if (gm == null || !Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play Mode 再打开编辑器", MessageType.Info);
            if (GUILayout.Button("进入 Play Mode")) EditorApplication.isPlaying = true;
            EditorGUILayout.EndScrollView();
            return;
        }

        RefreshLevelList();
        int newSel = Mathf.Max(0, EditorGUILayout.Popup("关卡", selectedLevel - 1, levelOptions)) + 1;
        if (newSel != selectedLevel) { selectedLevel = newSel; LoadLevel(selectedLevel); }
        EditorGUILayout.Space(8);

        // ===== 参数 =====
        EditorGUILayout.LabelField("关卡参数", EditorStyles.boldLabel);
        editName = EditorGUILayout.TextField("名称", editName);
        EditorGUILayout.BeginHorizontal();
        editCapacity = EditorGUILayout.IntSlider("碗容量", editCapacity, 2, 6);
        editExtraBowls = EditorGUILayout.IntSlider("额外碗", editExtraBowls, 0, 4);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        editDifficulty = EditorGUILayout.IntSlider("难度", editDifficulty, 0, 2);
        editTargetScore = EditorGUILayout.IntField("目标分", editTargetScore);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(8);

        // ===== 宠物队列（带精灵 + 分配食物） =====
        EditorGUILayout.LabelField($"宠物队列 ({editPets.Count}只)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < editPets.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            DrawPetSprite(editPets[i], PET_ICON);
            editPets[i] = (PetType)EditorGUILayout.EnumPopup(editPets[i], GUILayout.Width(80));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(25)))
            { editPets.RemoveAt(i); EditorGUILayout.EndHorizontal(); break; }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        if (GUILayout.Button("+ 添加宠物")) editPets.Add(PetType.Cat);
        EditorGUILayout.Space(8);

        // ===== 食物-宠物分配（当前关卡实际使用的） =====
        EditorGUILayout.LabelField("宠物→食物分配", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        var distinctPets = editPets.Distinct().ToList();
        for (int i = 0; i < distinctPets.Count; i++)
        {
            var pet = distinctPets[i];
            var food = FoodPetMap.GetFoodForPet(pet);
            EditorGUILayout.BeginHorizontal();
            DrawPetSprite(pet, 36f);
            EditorGUILayout.LabelField(PetCN(pet), GUILayout.Width(55));
            EditorGUILayout.LabelField("→", GUILayout.Width(15));
            DrawFoodSprite(food, 32f);
            EditorGUILayout.LabelField(FoodLabel(food), GUILayout.Width(50));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(8);

        // ===== 碗配置 =====
        int calculatedTotal = distinctPets.Count + editExtraBowls;
        EditorGUILayout.LabelField($"碗配置 (应共{calculatedTotal}碗)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("- 碗")) { if (editBowls.Count > 0) editBowls.RemoveAt(editBowls.Count - 1); }
        if (GUILayout.Button("+ 碗")) editBowls.Add(new EditableBowl());
        if (GUILayout.Button("同步数量")) SyncBowlCount();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        for (int bi = 0; bi < editBowls.Count; bi++)
        {
            var bowl = editBowls[bi];
            EditorGUILayout.BeginVertical("box");

            // 碗标题行
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"碗 {bi + 1}", EditorStyles.miniBoldLabel, GUILayout.Width(40));
            // 显示食物图标行
            for (int fi = 0; fi < bowl.foods.Count; fi++)
                DrawFoodSprite(bowl.foods[fi], FOOD_ICON);
            // 空槽位占位
            for (int fi = bowl.foods.Count; fi < editCapacity; fi++)
            {
                var r = GUILayoutUtility.GetRect(FOOD_ICON, FOOD_ICON);
                EditorGUI.DrawRect(r, new Color(0.3f, 0.3f, 0.3f, 0.3f));
            }
            EditorGUILayout.LabelField($"{bowl.foods.Count}/{editCapacity}", GUILayout.Width(30));
            EditorGUILayout.EndHorizontal();

            // 操作按钮行
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 加食物", GUILayout.Width(60)) && bowl.foods.Count < editCapacity)
            {
                int petIdx = bi % distinctPets.Count;
                if (distinctPets.Count > 0)
                    bowl.foods.Add(FoodPetMap.GetFoodForPet(distinctPets[petIdx]));
                else bowl.foods.Add(FoodType.DriedFish);
            }
            if (GUILayout.Button("- 减食物", GUILayout.Width(60)) && bowl.foods.Count > 0)
                bowl.foods.RemoveAt(bowl.foods.Count - 1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空", GUILayout.Width(40))) bowl.foods.Clear();
            EditorGUILayout.EndHorizontal();

            // 食物详情编辑
            if (bowl.foods.Count > 0)
            {
                EditorGUI.indentLevel++;
                for (int fi = 0; fi < bowl.foods.Count; fi++)
                {
                    EditorGUILayout.BeginHorizontal();
                    DrawFoodSprite(bowl.foods[fi], 20f);
                    bowl.foods[fi] = (FoodType)EditorGUILayout.EnumPopup($"#{fi + 1}", bowl.foods[fi]);
                    if (fi > 0 && GUILayout.Button("↑", GUILayout.Width(22)))
                    { var tmp = bowl.foods[fi]; bowl.foods[fi] = bowl.foods[fi - 1]; bowl.foods[fi - 1] = tmp; }
                    if (GUILayout.Button("删", GUILayout.Width(28)))
                    { bowl.foods.RemoveAt(fi); EditorGUILayout.EndHorizontal(); break; }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(10);

        // ===== 操作区 =====
        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
        if (GUILayout.Button("保存关卡", GUILayout.Height(36))) SaveLevel();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("自动生成", GUILayout.Height(28))) PreviewGenerate();
        if (GUILayout.Button("填充默认", GUILayout.Height(28))) FillDefaultFoods();
        if (GUILayout.Button("打乱食物", GUILayout.Height(28))) ShuffleFoods();
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(generationResult))
            EditorGUILayout.HelpBox(generationResult, MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    // ===== 精灵绘制 =====
    void DrawPetSprite(PetType pet, float size)
    {
        var r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        if (petSprites.TryGetValue(pet, out var sprite) && sprite != null)
        {
            var tex = sprite.texture;
            var uv = new Rect(
                (float)sprite.textureRect.x / tex.width,
                (float)sprite.textureRect.y / tex.height,
                (float)sprite.textureRect.width / tex.width,
                (float)sprite.textureRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(r, tex, uv);
        }
        else
        {
            EditorGUI.DrawRect(r, Color.gray);
            GUI.Label(r, "?", EditorStyles.centeredGreyMiniLabel);
        }
    }

    void DrawFoodSprite(FoodType food, float size)
    {
        var r = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        if (foodSprites.TryGetValue(food, out var sprite) && sprite != null)
        {
            var tex = sprite.texture;
            var uv = new Rect(
                (float)sprite.textureRect.x / tex.width,
                (float)sprite.textureRect.y / tex.height,
                (float)sprite.textureRect.width / tex.width,
                (float)sprite.textureRect.height / tex.height);
            GUI.DrawTextureWithTexCoords(r, tex, uv);
        }
        else
        {
            EditorGUI.DrawRect(r, new Color(0.5f, 0.3f, 0.2f));
            GUI.Label(r, FoodLabel(food).Substring(0, 1), EditorStyles.centeredGreyMiniLabel);
        }
    }

    // ===== 数据方法 =====
    void LoadLevel(int id)
    {
        var gm = GameObject.FindObjectOfType<PetGameManager>();
        if (gm == null) return;
        var lv = gm.CurrentLevel;
        if (lv == null) { gm.currentLevelId = id; gm.StartLevel(id); lv = gm.CurrentLevel; }
        if (lv == null) return;

        editName = lv.levelName;
        editCapacity = lv.bowlCapacity;
        editDifficulty = lv.difficulty;
        editTargetScore = lv.targetScore;
        editExtraBowls = Mathf.Max(0, lv.bowlInits.Length - lv.petQueue.Distinct().Count());
        editPets.Clear(); editPets.AddRange(lv.petQueue);
        editBowls.Clear();
        foreach (var init in lv.bowlInits)
        {
            var eb = new EditableBowl();
            if (init.foodStack != null) eb.foods.AddRange(init.foodStack);
            editBowls.Add(eb);
        }
        Repaint();
    }

    void SyncBowlCount()
    {
        int target = editPets.Distinct().Count() + editExtraBowls;
        while (editBowls.Count < target) editBowls.Add(new EditableBowl());
        while (editBowls.Count > target && editBowls.Count > 0) editBowls.RemoveAt(editBowls.Count - 1);
    }

    void FillDefaultFoods()
    {
        editBowls.Clear();
        foreach (var pet in editPets.Distinct())
        {
            var food = FoodPetMap.GetFoodForPet(pet);
            var bowl = new EditableBowl();
            for (int i = 0; i < editCapacity; i++) bowl.foods.Add(food);
            editBowls.Add(bowl);
        }
        SyncBowlCount();
        generationResult = $"填充 {editPets.Distinct().Count()} 个满碗 + {editExtraBowls} 空碗";
    }

    void ShuffleFoods()
    {
        var allFoods = new List<FoodType>();
        foreach (var b in editBowls) allFoods.AddRange(b.foods);
        int total = allFoods.Count;
        var rng = new System.Random((int)System.DateTime.Now.Ticks);
        for (int i = allFoods.Count - 1; i > 0; i--)
        { int j = rng.Next(i + 1); var tmp = allFoods[i]; allFoods[i] = allFoods[j]; allFoods[j] = tmp; }
        foreach (var b in editBowls) b.foods.Clear();
        int fi = 0;
        int bowlCount = editBowls.Count;
        for (int bi = 0; bi < bowlCount && fi < total; bi++)
        {
            int remaining = total - fi;
            int bowlsLeft = bowlCount - bi;
            int maxFill = Mathf.Min(editCapacity, remaining);
            int minNeed = Mathf.Max(0, remaining - (bowlsLeft - 1) * editCapacity);
            int fill = rng.Next(minNeed, maxFill + 1);
            for (int k = 0; k < fill; k++) editBowls[bi].foods.Add(allFoods[fi++]);
        }
        generationResult = $"打乱完成：{total}→{fi} 分散";
    }


    void PreviewGenerate()
    {
        var pets = editPets.ToArray();
        var inits = LevelGenerator.Generate(pets, editCapacity, editExtraBowls, previewSeed);
        if (inits == null) { generationResult = "生成失败！"; return; }
        editBowls.Clear();
        foreach (var init in inits)
        {
            var eb = new EditableBowl();
            if (init.foodStack != null) eb.foods.AddRange(init.foodStack);
            editBowls.Add(eb);
        }
        editTargetScore = LevelGenerator.CalcTargetScore(pets.Distinct().Count());
        editName = LevelGenerator.GetLevelName(pets, selectedLevel);
        generationResult = $"✓ seed={previewSeed}: {pets.Distinct().Count()}宠×{editCapacity}, {inits.Count}碗";
        previewSeed++;
    }

    void ApplyPreview() { SaveLevel(); return; } // legacy
    void SaveLevel()
    {
        var gm = GameObject.FindObjectOfType<PetGameManager>();
        if (gm == null) return;
        var inits = new List<BowlInitData>();
        foreach (var eb in editBowls) inits.Add(new BowlInitData { foodStack = eb.foods.ToArray() });

        // 构建关卡对象
        var lv = ScriptableObject.CreateInstance<PetLevelConfigV2>();
        lv.levelId = selectedLevel; lv.levelName = editName; lv.bowlCapacity = editCapacity;
        lv.difficulty = editDifficulty; lv.targetScore = editTargetScore;
        lv.petQueue = editPets.ToArray(); lv.bowlInits = inits.ToArray();

        // 更新内存列表
        var existing = gm.levels.Find(l => l.levelId == selectedLevel);
        if (existing != null) gm.levels.Remove(existing);
        gm.levels.Add(lv);

        // 保存到磁盘（关键！让编辑在下次运行后仍然有效）
        string path = $"Assets/Resources/Levels/Level_{selectedLevel:D2}.asset";
        var oldAsset = AssetDatabase.LoadAssetAtPath<PetLevelConfigV2>(path);
        if (oldAsset != null) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(lv, path);
        AssetDatabase.SaveAssets();

        gm.StartLevel(selectedLevel);
        var ui = GameObject.FindObjectOfType<PetGameUI>();
        if (ui != null)
        {
            var buildLevel = typeof(PetGameUI).GetMethod("BuildLevel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            buildLevel?.Invoke(ui, null);
        }
        generationResult = $"✓ 关卡{selectedLevel}「{editName}」已保存到磁盘！";
    }

    // ===== 标签 =====
    string PetCN(PetType p) => p switch
    {
        PetType.Cat => "橘猫", PetType.Dog => "柴犬", PetType.Hamster => "仓鼠",
        PetType.Parrot => "鹦鹉", PetType.Fish => "金鱼", PetType.Rabbit => "垂耳兔", _ => p.ToString()
    };

    string FoodLabel(FoodType f) => f switch
    {
        FoodType.DriedFish => "鱼干", FoodType.BoneTreat => "骨头", FoodType.SunflowerSeed => "瓜子",
        FoodType.Millet => "小米", FoodType.FishFlake => "鱼片", FoodType.Carrot => "萝卜",
        FoodType.CatKibble => "猫粮", FoodType.DogKibble => "狗粮", FoodType.Apple => "苹果",
        FoodType.MeatJerky => "肉干", FoodType.Milk => "牛奶", FoodType.Hay => "干草",
        FoodType.CannedCatFood => "罐头", FoodType.Catnip => "猫薄荷", FoodType.CatTreatStick => "猫条",
        FoodType.DentalChew => "咬胶", FoodType.DogBiscuit => "饼干", FoodType.Sausage => "香肠",
        FoodType.Corn => "玉米", FoodType.Mealworm => "虫干", FoodType.Cuttlebone => "墨骨",
        FoodType.SeedBag => "种子", FoodType.Bloodworm => "红虫", FoodType.AlgaeWafer => "藻片",
        FoodType.Pellet => "颗粒", FoodType.PeanutButter => "花生", FoodType.TunaChunk => "金枪",
        FoodType.SalmonSlice => "三文", FoodType.TreatBall => "零食球", _ => f.ToString()
    };
}
