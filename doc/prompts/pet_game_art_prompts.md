# 铲屎官疯了 — AI美术资产生成提示词全集
# 风格：2D扁平可爱卡通 + 暖色系 + 宠物主题
# 目标：抖音/微信小游戏，IAA变现
# 工具：Image2（文生图）+ ai-ui-asset-cutter.py（自动切图）
# 分辨率：1024x1024为主，部分合集1024x512

# ============================================================
# 一、美术风格设定（每次生图都加上）
# ============================================================
# 尾巴咒语 (贴在所有提示词末尾):
# "cute flat 2D game art, kawaii chibi style, warm colors, clean lines, 
#  no realistic textures, no 3D shading, simple cartoon, mobile game asset, 
#  isolated on transparent background, white background"

# ============================================================
# 二、宠物角色（6种 × 7表情 = 42张）
# ============================================================
# 策略：每种宠物生成1张合集 (4x2网格，7表情+宠物本体)，自动切分
# 尺寸：1024x512 (7个表情排列)

## 🐱 橘猫主子
```
A chubby orange tabby cat character in cute chibi kawaii style,
7 expressions in a row on plain white background, no background scene:
1. neutral face
2. happy with heart eyes
3. angry with fur standing up
4. eating with puffed cheeks and crumbs
5. begging with puppy eyes
6. disgusted with tongue out
7. sleepy/drowsy with half-closed eyes
cute flat 2D game art, kawaii chibi style, warm orange colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 🐶 柴犬狗子
```
A fluffy shiba inu dog character in cute chibi kawaii style,
7 expressions in a row on plain white background, no background scene:
1. neutral face
2. extremely happy with wagging tail visible
3. angry with furrowed brows
4. drooling with tongue hanging out
5. guilty/puppy eyes looking up
6. confused with tilted head and question mark
7. proud with chest puffed out
cute flat 2D game art, kawaii chibi style, warm golden brown colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 🐹 仓鼠小团子
```
A tiny round hamster character in cute chibi kawaii style, small pink nose,
7 expressions in a row on plain white background, no background scene:
1. neutral face with tiny paws together
2. happy with stars in eyes
3. scared/shaking with wide eyes
4. eating with cheeks stuffed super big
5. running on a tiny wheel (action pose)
6. sleeping curled into a fuzzy ball
7. angry but still cute (puffed up tiny)
cute flat 2D game art, kawaii chibi style, warm cream and brown colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 🦜 鹦鹉话痨
```
A green and yellow parrot character in cute chibi kawaii style, colorful feathers,
7 expressions in a row on plain white background, no background scene:
1. neutral face on a perch
2. talking with speech bubble shapes
3. laughing with tears
4. angry with ruffled feathers
5. curious with one eye bigger
6. singing with music notes
7. sleeping with one eye open
cute flat 2D game art, kawaii chibi style, bright green yellow red colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 🐟 金鱼呆子
```
A round orange goldfish character in cute chibi kawaii style, big bubble eyes,
7 expressions in a row on plain white background, no background scene:
1. neutral face swimming
2. happy blowing heart-shaped bubbles
3. surprised with even bigger eyes
4. eating with mouth wide open
5. dizzy with spiral eyes
6. grumpy with downturned mouth
7. deadpan/unimpressed expression
cute flat 2D game art, kawaii chibi style, bright orange gold colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 🐰 垂耳兔蹦蹦
```
A fluffy lop-eared rabbit character in cute chibi kawaii style, long droopy ears,
7 expressions in a row on plain white background, no background scene:
1. neutral face with ears relaxed
2. joyful jumping with ears flying up
3. angry with one ear up one down
4. munching with little mouth moving
5. scared with ears covering eyes
6. smug with half-closed eyes
7. sleepy snuggling into own fur
cute flat 2D game art, kawaii chibi style, soft white and gray colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

# ============================================================
# 三、宠物食物容器（6种）
# ============================================================
# 策略：横向6个容器合集，1024x512，自动切分

## 宠物饭碗合集
```
6 cute pet food bowls in a row, 2D flat cartoon game asset style:
1. orange cat bowl with fish bone pattern, labeled "猫主子的碗"
2. blue dog bowl with bone pattern, slightly chewed edge
3. tiny pink hamster bowl with cute paw print, very small
4. green parrot feeding perch with seed tray, wooden texture
5. round glass fishbowl-style feeding ring, water texture
6. white rabbit bowl with carrot pattern on the side
each bowl shown empty, front view, clean game asset on white background,
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 宠物碗满状态合集
```
6 cute pet food bowls in a row, 2D flat cartoon game asset style:
1. orange cat bowl overfilled with cat food, canned food visible on top, a paw print on bowl
2. blue dog bowl piled high with dog kibble and a bone sticking out, chewed edge
3. tiny pink hamster bowl filled to brim with seeds and tiny corn
4. green parrot perch overflowing with mixed seeds and a sunflower seed on top
5. round fishbowl ring with fish flakes floating, water texture
6. white rabbit bowl stuffed with hay and a carrot sticking out
each bowl shown completely full, front view, clean game asset on white background,
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

# ============================================================
# 四、食物物品图标（约30种，分批生成）
# ============================================================
# 策略：每批8个，生成4批合集 (4x2网格)，1024x1024

## 食物物品 - 第1批（猫用）
```
8 cute cartoon food item icons in a 4x2 grid on white background, 
each item large centered inside its cell, 2D flat game asset style:
- Canned cat food (silver can with fish label, opened)
- Cat kibble pieces (small brown star shapes in a pile)
- Dried fish snack (small silver fish, flat)
- Cat treat stick tube (red tube packaging)
- Catnip leaf (green leaf shape)
- Milk saucer (white saucer with milk)
- Tuna chunk (pink fish chunk)
- Salmon slice (orange salmon piece)
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 食物物品 - 第2批（狗用）
```
8 cute cartoon food item icons in a 4x2 grid on white background,
each item large centered inside its cell, 2D flat game asset style:
- Dog kibble pieces (round brown balls in a pile)
- Bone-shaped treat (white cartoon bone)
- Meat jerky strip (dark red flat strip)
- Dental chew stick (green toothbrush-shaped treat)
- Dog biscuit (square brown cookie)
- Sausage link (red cartoon sausage)
- Beef chunk (brown meat cube)
- Peanut butter jar (brown jar with label)
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 食物物品 - 第3批（小宠用：仓鼠/兔/鹦鹉）
```
8 cute cartoon food item icons in a 4x2 grid on white background,
each item large centered inside its cell, 2D flat game asset style:
- Sunflower seeds (black and white striped seeds, small pile)
- Dried corn kernels (yellow flat squares)
- Mealworms (small beige worm shapes in a pile)
- Millet spray (yellow spray of tiny seeds on stem)
- Carrot (orange cartoon carrot with green top)
- Hay bundle (green dried grass tied with string)
- Apple slice (red apple cut in half)
- Cuttlebone (white oval bone shape for birds)
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 食物物品 - 第4批（鱼用 + 特殊物品）
```
8 cute cartoon food item icons in a 4x2 grid on white background,
each item large centered inside its cell, 2D flat game asset style:
- Fish flakes (colorful tiny flakes scattered)
- Bloodworms (tiny red worm shapes)
- Brine shrimp (tiny pink shrimp dots)
- Algae wafer (green round disc)
- Mixed seeds bag (brown paper bag with seed label)
- Food pellet (small round brown pellet)
- Vitamin drop (blue drop shape with V label)
- Treat ball (colorful round ball with holes)
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

# ============================================================
# 五、场景背景图（4张）
# ============================================================
# 策略：每张单独生成，2048x2048

## 背景1 - 客厅宠物角
```
A cozy living room pet corner scene, 2D flat cartoon game background,
warm wooden floor, a pet mat on the floor, scattered toys (ball, rope, mouse toy),
a scratching post in the corner, soft curtains on a window with sunlight,
pet bowls area with feeding mat, some pet fur on the floor for realism,
no actual pets visible, designed as game level background,
warm cozy colors, flat vector art, 2048x2048, no UI elements,
cute flat 2D game art, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset
```

## 背景2 - 厨房喂食区
```
A bright kitchen feeding area scene, 2D flat cartoon game background,
checkered floor tiles, kitchen counter in background, feeding mat with bowls,
a refrigerator with pet photos and magnets, window with morning light,
some spilled kibble on the floor, measuring cup for pet food on counter,
designed as game level background, no actual pets visible,
warm bright colors, flat vector art, 2048x2048, no UI elements,
cute flat 2D game art, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset
```

## 背景3 - 后院宠物派对
```
A happy backyard pet party scene, 2D flat cartoon game background,
green grass lawn, white picket fence, colorful bunting flags hanging,
a small pet pool in corner, agility tunnel and hoop, scattered toys everywhere,
picnic blanket with pet snacks, trees with leaves, sunny sky with few clouds,
designed as game level background, no actual pets visible,
bright cheerful colors, flat vector art, 2048x2048, no UI elements,
cute flat 2D game art, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset
```

## 背景4 - 宠物店内部
```
A cute pet store interior scene, 2D flat cartoon game background,
shelves with pet food bags and cans, colorful toy display rack,
a grooming station with brush and shampoo bottles in background,
small pet beds and carriers on display, price tags hanging,
checkered floor, warm lighting from ceiling, "PET MART" sign,
designed as game level background, no actual pets visible,
warm inviting colors, flat vector art, 2048x2048, no UI elements,
cute flat 2D game art, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset
```

# ============================================================
# 六、UI元素（继承木质纸质感风格）
# ============================================================
# 策略：参考 universal_ui_prompts_v3.md 的风格，加入宠物元素

## 按钮合集（含宠物元素）
```
5 cute game UI buttons in a horizontal row, 2D flat cartoon style,
pet-themed game UI assets on white background:
1. green play button with paw print icon, labeled "开始投喂"
2. blue level select button with bone icon, labeled "关卡"  
3. orange settings button with cat bell icon
4. yellow hint button with lightbulb and tiny mouse
5. red pause button with two paw pause marks
rounded rectangle shape, soft shadow, wooden paper texture border,
each button 200x80px proportion, clean game asset,
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, simple cartoon, mobile game UI,
isolated on transparent background, white background
```

## 面板及对话框
```
4 cute game UI panels in a 2x2 grid, 2D flat cartoon style,
pet-themed game UI assets on white background:
1. top-left: level complete popup panel with confetti border, rounded corners
2. top-right: pause menu panel with paw print decoration
3. bottom-left: settings panel with slider for volume/sound
4. bottom-right: daily reward popup with gift box and dog bone icon
wooden paper textured frame, warm cream background color,
rounded corners 20px, soft inner shadow, game UI panels,
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, simple cartoon, mobile game UI,
isolated on transparent background, white background
```

## HUD元素合集
```
6 cute game HUD elements in a 3x2 grid, 2D flat cartoon style,
pet-themed game UI assets on white background:
1. top bar: wooden plank texture status bar background
2. star icon: golden paw-shaped star for ratings
3. heart icon: pink dog nose shaped heart for lives
4. coin icon: round coin with fish bone pattern
5. progress bar: bone-shaped progress bar fill
6. badge icon: circular badge with cat face silhouette
cute flat 2D game art, kawaii style, warm colors, clean lines,
no realistic textures, simple cartoon, mobile game UI,
isolated on transparent background, white background
```

## Logo与标题文字
```
Game title logo for a mobile game called "铲屎官疯了",
cute cartoon text design, Chinese characters,
a chubby orange cat and fluffy dog sitting on the letters,
paw prints scattered around the text, speech bubble with "汪汪" and "喵喵",
wooden signboard style background behind the text,
warm brown and orange colors, playful bouncy font style,
cute flat 2D game art, kawaii style, clean lines,
no realistic textures, simple cartoon, mobile game title,
white background
```

# ============================================================
# 七、游戏核心元素
# ============================================================

## 食物堆（多层叠放的食物）
```
A vertical stack of mixed pet food items piled on top of each other,
cartoon 2D game asset, showing layers from bottom to top:
layer 1: dog kibble pile at bottom
layer 2: cat canned food on top
layer 3: fish flakes layer
layer 4: sunflower seeds on top layer
each layer visually distinct with clear color separation,
slight wobble effect on edges, wooden base platform,
warm colors, clean cartoon style,
cute flat 2D game art, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

## 暂存碗（中转碗）
```
A cute wooden temporary holding bowl, 2D flat cartoon game asset,
labeled with a question mark sign, slightly smaller than pet bowls,
warm wood color with "暂存区" written in cute font on the side,
shown empty state, front view,
cute flat 2D game art, warm colors, clean lines,
no realistic textures, simple cartoon, mobile game asset,
isolated on transparent background, white background
```

# ============================================================
# 八、特殊效果
# ============================================================

## 粒子特效合集
```
6 cute particle effects for a pet feeding mobile game, 2D flat cartoon style,
in a 3x2 grid on white background:
1. heart burst effect (pink hearts exploding)
2. sparkle stars (yellow golden sparkles)
3. food crumbs scatter (brown crumbs flying)
4. level up glow (golden upward rays)
5. correct match effect (green checkmark with sparkles)
6. wrong match effect (red X with smoke puff)
cute flat 2D game art, warm colors, clean lines,
no realistic textures, simple cartoon, mobile game effects,
isolated on transparent background, white background
```

# ============================================================
# 九、素材清单汇总
# ============================================================

## 全部素材列表

| 类别 | 数量 | 生成方式 | 建议尺寸 | 预估文件 |
|------|------|---------|---------|---------|
| 宠物表情 | 6种×7表情=42张 | 6张合集 | 1024×512 | pets_expressions/ |
| 宠物碗(空) | 6张 | 1张合集 | 1024×512 | bowls/empty/ |
| 宠物碗(满) | 6张 | 1张合集 | 1024×512 | bowls/full/ |
| 食物物品 | 30种 | 4张合集 | 1024×1024 | foods/ |
| 场景背景 | 4张 | 单张生成 | 2048×2048 | backgrounds/ |
| 按钮 | 5张 | 1张合集 | 1024×512 | UI/buttons/ |
| 面板 | 4张 | 1张合集 | 1024×1024 | UI/panels/ |
| HUD元素 | 6张 | 1张合集 | 1024×1024 | UI/hud/ |
| Logo | 1张 | 单张生成 | 1024×512 | UI/logo/ |
| 食物堆 | 1张 | 单张生成 | 512×1024 | game/food_stack/ |
| 暂存碗 | 1张 | 单张生成 | 512×512 | game/temp_bowl/ |
| 粒子特效 | 6张 | 1张合集 | 1024×1024 | effects/ |

**总计：约18次Image2生成，产出约120个独立游戏素材**

## 生成优先级

```
第一优先（有这些就能开始开发）:
  1. 🐱🐶🐹🦜🐟🐰 宠物表情合集 (6张)   ← 核心角色
  2. 宠物碗合集 (2张)                     ← 核心容器
  3. 食物物品合集 (4张)                   ← 核心玩法
  4. 食物堆 (1张)                         ← 核心玩法
  5. 暂存碗 (1张)                         ← 核心玩法

第二优先（完善UI体验）:
  6. 按钮合集 (1张)
  7. 面板合集 (1张)
  8. HUD合集 (1张)
  9. Logo (1张)

第三优先（打磨品质）:
  10. 场景背景 (4张)
  11. 粒子特效 (1张)
```

# ============================================================
# 十、生成流程指南
# ============================================================

## 步骤

```
1. 打开 Image2 网页版
2. 复制上方对应提示词
3. 设置分辨率（见每节标注）
4. 生成 → 下载图片
5. 保存到: Assets/Art/PetGame/Raw/
6. 运行切分脚本:
   python tools/ai-ui-asset-cutter.py Raw/pets_cat.png --layout "4x2"
7. 切分结果保存到: Assets/Art/PetGame/
8. 在团结编辑器中导入，设置 Sprite 格式
```

## 目录结构

```
Assets/Art/PetGame/
├── Raw/                    ← 原始生成图（保留备份）
├── pets/                   ← 切分后的宠物表情
│   ├── cat/
│   ├── dog/
│   ├── hamster/
│   ├── parrot/
│   ├── fish/
│   └── rabbit/
├── bowls/                  ← 宠物碗
│   ├── empty/
│   └── full/
├── foods/                  ← 食物图标
├── backgrounds/            ← 场景背景
├── UI/                     ← 按钮/面板/HUD
├── game/                   ← 食物堆/暂存碗
└── effects/                ← 粒子特效
```
