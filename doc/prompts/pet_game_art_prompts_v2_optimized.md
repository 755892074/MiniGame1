# 铲屎官疯了 — AI美术资产生成提示词（6次生成版）
# 风格：2D扁平可爱卡通 + 暖色系 + 宠物主题
# 工具：Image2 → ai-ui-asset-cutter.py 自动切分
# 
# ⚡ 6次Image2生成 = 全部美术素材（约120个文件）
#
# 风格咒语（每张图末尾都加）:
# "cute flat 2D game art, kawaii chibi style, warm colors, 
#  clean lines, no realistic textures, no 3D shading, 
#  simple cartoon, mobile game asset, white background"

# ============================================================
# 第1次生成：猫🐱 + 狗🐶 + 仓鼠🐹 全部表情
# 布局：7列×3行 = 21格
# 第1行=猫7表情, 第2行=狗7表情, 第3行=仓鼠7表情
# 尺寸：1792×768
# 切分命令: python tools/ai-ui-asset-cutter.py raw/pets_top.png --layout pets_top
# ============================================================

```
Top row (猫-橘猫): 7 cute chibi orange tabby cat expressions in a row, show from left to right:
neutral face, happy with heart eyes, angry fur standing up, eating with puffed cheeks, 
begging with puppy eyes, disgusted with tongue out, sleepy half-closed eyes.
Middle row (狗-柴犬): 7 cute chibi shiba inu dog expressions in a row:
neutral face, extremely happy wagging tail, angry furrowed brows, drooling tongue out,
guilty puppy eyes looking up, confused tilted head with question mark, proud chest puffed out.
Bottom row (仓鼠-小团子): 7 cute chibi round hamster expressions in a row:
neutral tiny paws together, happy stars in eyes, scared shaking wide eyes,
eating cheeks stuffed super big, running on tiny wheel, sleeping curled into fuzzy ball,
angry but cute puffed up tiny.
Arrange as 3 rows of 7 columns, grid layout with even spacing between each character,
each character isolated in its own cell, consistent cute cartoon style throughout,
cute flat 2D game art, kawaii chibi style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 第2次生成：鹦鹉🦜 + 金鱼🐟 + 垂耳兔🐰 全部表情
# 布局：7列×3行 = 21格
# 尺寸：1792×768
# 切分命令: python tools/ai-ui-asset-cutter.py raw/pets_bottom.png --layout pets_bottom
# ============================================================

```
Top row (鹦鹉-话痨): 7 cute chibi green parrot expressions in a row on perches:
neutral face, talking with speech bubble, laughing with tears, angry ruffled feathers,
curious one eye bigger, singing with music notes, sleeping one eye open.
Middle row (金鱼-呆子): 7 cute chibi round orange goldfish expressions in a row:
neutral swimming, happy blowing heart bubbles, surprised huge eyes, eating mouth wide open,
dizzy spiral eyes, grumpy downturned mouth, deadpan unimpressed.
Bottom row (垂耳兔-蹦蹦): 7 cute chibi lop-eared white rabbit expressions in a row:
neutral ears relaxed, joyful jumping ears flying, angry one ear up one down,
munching tiny mouth, scared ears covering eyes, smug half-closed eyes, sleepy snuggling.
Arrange as 3 rows of 7 columns, grid layout with even spacing,
each character isolated in its own cell, consistent cute cartoon style throughout,
cute flat 2D game art, kawaii chibi style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 第3次生成：全部宠物碗（6空碗 + 6满碗）
# 布局：4列×3行 = 12格
# 上2行=6空碗, 下1行=6满碗（或者每行4碗）
# 尺寸：1024×768
# 切分命令: python tools/ai-ui-asset-cutter.py raw/bowls_all.png --layout bowls_all
# ============================================================

```
12 cute pet food bowls in a 4x3 grid layout, 2D flat cartoon game assets, white background:
Top row (empty bowls, 4 per row):
1. orange cat bowl with fish bone pattern, empty, front view
2. blue dog bowl with bone pattern, chewed edge, empty, front view
3. tiny pink hamster bowl with paw print, very small, empty, front view
4. green parrot feeding perch with seed tray, empty, wooden texture
Second row (empty bowls continued):
5. round fishbowl feeding ring, water texture, empty
6. white rabbit bowl with carrot pattern, empty, front view
7. wooden temporary holding bowl with question mark, labeled "暂存", empty
8. (leave blank or make a smaller spare bowl)
Third row (full bowls):
9. cat bowl overfilled with canned food and kibble, paw print on bowl
10. dog bowl piled high with kibble and a bone sticking out
11. hamster bowl filled to brim with seeds, tiny corn visible
12. parrot perch overflowing with mixed seeds, sunflower seed on top
Bottom row (full bowls continued):
13. fishbowl ring with fish flakes floating
14. rabbit bowl stuffed with hay and carrot
15. temporary bowl half-filled with mixed food
16. treat bowl with colorful treats and bone shape
Arrange exactly 4 columns x 4 rows = 16 cells, BUT the first 12 cells have content as listed above,
each bowl isolated in its own cell with clear spacing,
consistent cute cartoon style, warm colors,
cute flat 2D game art, kawaii style, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 第4次生成：全部食物物品（30种）
# 布局：6列×5行 = 30格
# 尺寸：1200×1000
# 切分命令: python tools/ai-ui-asset-cutter.py raw/foods_all.png --layout foods_all
# ============================================================

```
30 cute cartoon pet food item icons in a 6x5 grid on white background,
each item large centered in its cell, 2D flat game asset style, consistent size:

Row 1 (猫用): canned cat food(silver can fish label), cat kibble(brown star pile), 
dried fish snack(silver flat fish), cat treat stick(red tube), catnip leaf(green), milk saucer(white)

Row 2 (狗用): dog kibble(round brown balls), bone-shaped treat(white bone), 
meat jerky(dark red strip), dental chew stick(green toothbrush shape), 
dog biscuit(square cookie), sausage link(red sausage)

Row 3 (小宠用): sunflower seeds(black white striped pile), corn kernels(yellow squares), 
mealworms(beige worm pile), millet spray(yellow seed stem), carrot(orange with green top), 
hay bundle(green dried grass tied)

Row 4 (鱼用): fish flakes(colorful tiny scattered), bloodworms(red worm shapes), 
brine shrimp(pink dots), algae wafer(green disc), apple slice(red cut half), 
cuttlebone(white oval bone)

Row 5 (更多): mixed seeds bag(brown paper bag), food pellet(round brown), 
tuna chunk(pink cube), salmon slice(orange piece), peanut butter jar(brown jar), 
treat ball(colorful ball with holes)

cute flat 2D game art, kawaii chibi style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 第5次生成：UI全套（15个元素）
# 布局：5列×3行 = 15格
# 尺寸：1250×750
# 切分命令: python tools/ai-ui-asset-cutter.py raw/ui_petgame.png --layout ui_petgame
# ============================================================

```
15 cute pet-themed game UI elements in a 5x3 grid on white background,
2D flat cartoon style, wooden paper texture where applicable:

Row 1 (按钮): 
1. green play button with paw print icon, label "开始投喂", rounded rectangle, wood border
2. blue level select button with bone icon, label "关卡", rounded rectangle
3. orange settings button with cat bell icon, rounded rectangle
4. yellow hint button with lightbulb and tiny mouse, rounded rectangle
5. red pause button with two paw pause marks, rounded rectangle

Row 2 (HUD元素):
6. wooden plank style progress bar (horizontal bar with bone-shaped fill)
7. golden paw-shaped star icon
8. pink dog nose shaped heart icon  
9. coin icon with fish bone pattern
10. circular badge with cat face silhouette

Row 3 (面板/弹窗):
11. level complete popup panel with confetti border, rounded corners, cream background
12. pause menu panel with paw print decorations, cream background
13. settings menu panel with sliders, cream background
14. daily reward popup with gift box and dog bone, cream background
15. game title logo: "铲屎官疯了" with a chubby orange cat sitting on the letters

cute flat 2D game art, kawaii chibi style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 第6次生成：特殊元素合集（8个）
# 布局：4列×2行 = 8格
# 尺寸：1024×512
# 切分命令: python tools/ai-ui-asset-cutter.py raw/special_8.png --layout special_8
# ============================================================

```
8 special game elements for a pet feeding mobile game in a 4x2 grid, white background, 2D flat cartoon:

Row 1:
1. vertical stack of mixed pet food layers (dog kibble bottom, cat food middle, seeds top), 
   colorful cartoon layered stack, slight wobble, wooden base platform
2. empty temporary holding bowl, wooden texture, question mark label, small size
3. temporary bowl half-filled with assorted pet food
4. heart burst effect: pink hearts exploding outward, 2D cartoon particles

Row 2:
5. sparkle effect: golden yellow stars sparkling, 2D cartoon particles
6. food crumbs scatter effect: brown crumbs flying, 2D cartoon particles
7. correct match effect: large green checkmark with sparkles, 2D cartoon
8. wrong match effect: red X with smoke puff, 2D cartoon

cute flat 2D game art, kawaii chibi style, warm colors, clean lines,
no realistic textures, no 3D shading, simple cartoon, mobile game asset, white background
```

# ============================================================
# 生成后切分命令一览
# ============================================================

```bash
cd F:/WorkBuddy/H5MiniGame/MiniGame1/MiniGame1_Project

# 1. 宠物上半
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/pets_top.png --layout pets_top

# 2. 宠物下半
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/pets_bottom.png --layout pets_bottom

# 3. 宠物碗
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/bowls_all.png --layout bowls_all

# 4. 食物物品
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/foods_all.png --layout foods_all

# 5. UI套装
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/ui_petgame.png --layout ui_petgame

# 6. 特殊元素
python tools/ai-ui-asset-cutter.py Assets/Art/PetGame/Raw/special_8.png --layout special_8
```

# ============================================================
# 目录结构（切分后自动生成）
# ============================================================

```
Assets/Art/PetGame/
├── Raw/                          ← 原始大图（备份）
├── pets/
│   ├── cat/      (7张表情)
│   ├── dog/      (7张表情)
│   ├── hamster/  (7张表情)
│   ├── parrot/   (7张表情)
│   ├── fish/     (7张表情)
│   └── rabbit/   (7张表情)
├── bowls/
│   ├── empty/    (6张空碗)
│   └── full/     (6张满碗)
├── foods/        (30张食物图标)
├── UI/           (15张UI元素)
├── game/         (食物堆+暂存碗)
└── effects/      (5张特效)
```
