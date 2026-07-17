#!/usr/bin/env python3
"""水排序关卡生成器 + BFS求解器 - 快速验证算法"""
import random
import time
from collections import deque

def generate_level(num_pets, capacity, extra_bowls, seed):
    """生成一关：随机分配食物到碗中（完全随机，不按颜色聚类）"""
    rng = random.Random(seed)
    num_bowls = num_pets + extra_bowls
    total_foods = num_pets * capacity
    
    # 食物池：0,1,2...num_pets-1 每种 capacity 个
    food_pool = []
    for i in range(num_pets):
        food_pool.extend([i] * capacity)
    
    for attempt in range(200):
        s = seed + attempt * 31
        rng = random.Random(s)
        
        # Fisher-Yates 洗牌
        shuffled = food_pool[:]
        rng.shuffle(shuffled)
        
        # 完全随机分配到碗中（每碗最多 capacity 个）
        bowls = [[] for _ in range(num_bowls)]
        ok = True
        for food in shuffled:
            # 随机选一个有余量的碗
            candidates = [i for i in range(num_bowls) if len(bowls[i]) < capacity]
            if not candidates:
                ok = False
                break
            idx = rng.choice(candidates)
            bowls[idx].append(food)
        
        if not ok:
            continue
        
        # 检查没有初始已完成碗
        has_complete = any(
            len(b) == capacity and len(set(b)) == 1 
            for b in bowls
        )
        if has_complete:
            continue
        
        # BFS 验证可解性
        min_steps = bfs_min_steps(bowls, capacity, num_pets, timeout=3.0)
        if min_steps > 0:
            return bowls, min_steps, s
    
    return None, 0, seed

def bfs_min_steps(initial_bowls, capacity, required, timeout=3.0):
    """BFS 求最少步数解"""
    start = time.time()
    
    def canonicalize(bowls):
        parts = []
        for b in bowls:
            if not b:
                parts.append("_")
            else:
                parts.append(",".join(str(x) for x in b))
        parts.sort()
        return "|".join(parts)
    
    def count_complete(bowls):
        count = 0
        for b in bowls:
            if len(b) == capacity and len(set(b)) == 1:
                count += 1
        return count
    
    def clone(bowls):
        return [b[:] for b in bowls]
    
    initial = tuple(tuple(b) for b in initial_bowls)
    init_list = [list(b) for b in initial_bowls]
    
    if count_complete(init_list) >= required:
        return 0
    
    visited = {canonicalize(init_list)}
    queue = deque([(init_list, 0)])
    max_depth = 40
    max_states = 100000
    
    while queue:
        if len(queue) % 500 == 0 and time.time() - start > timeout:
            return -1
        
        state, depth = queue.popleft()
        
        if count_complete(state) >= required:
            return depth
        
        if depth >= max_depth:
            continue
        if len(visited) >= max_states:
            continue
        
        n = len(state)
        for frm in range(n):
            src = state[frm]
            if not src:
                continue
            # 跳过已完成碗
            if len(src) == capacity and len(set(src)) == 1:
                continue
            
            top = src[-1]
            # 顶层连续同色数
            pick = 0
            for i in range(len(src) - 1, -1, -1):
                if src[i] == top:
                    pick += 1
                else:
                    break
            
            for to in range(n):
                if frm == to:
                    continue
                dst = state[to]
                if len(dst) >= capacity:
                    continue
                if dst and dst[-1] != top:
                    continue
                
                pour = min(pick, capacity - len(dst))
                if pour <= 0:
                    continue
                
                new_state = clone(state)
                for _ in range(pour):
                    new_state[frm].pop()
                for _ in range(pour):
                    new_state[to].append(top)
                
                canon = canonicalize(new_state)
                if canon not in visited:
                    visited.add(canon)
                    queue.append((new_state, depth + 1))
    
    return -1

# === 测试 ===
print("=" * 60)
print("水排序关卡生成器测试")
print("=" * 60)

test_cases = [
    # (关卡ID, 宠物数, 容量, 空碗数, 描述)
    (1, 2, 3, 2, "新手"),
    (5, 3, 3, 2, "新手"),
    (10, 3, 3, 1, "入门·卡点"),
    (15, 4, 4, 1, "进阶·卡点"),
    (20, 4, 4, 1, "中难·卡点"),
    (25, 4, 4, 1, "困难·卡点"),
    (30, 5, 4, 1, "困难·卡点"),
    (35, 5, 5, 1, "挑战·卡点"),
    (40, 5, 5, 1, "挑战·卡点"),
    (45, 6, 5, 1, "挑战·卡点"),
    (50, 6, 5, 1, "地狱·终极"),
]

for lv_id, pets, cap, extra, label in test_cases:
    seed = lv_id * 1000
    t0 = time.time()
    bowls, steps, used_seed = generate_level(pets, cap, extra, seed)
    elapsed = time.time() - t0
    
    if bowls:
        print(f"Lv{lv_id:2d} [{label:8s}] {pets}宠 cap={cap} extra={extra} → ✓ minSteps={steps} seed={used_seed} ({elapsed:.1f}s)")
        # 打印碗内容
        for i, b in enumerate(bowls):
            print(f"       碗{i}: {b}")
    else:
        print(f"Lv{lv_id:2d} [{label:8s}] {pets}宠 cap={cap} extra={extra} → ✗ 失败 ({elapsed:.1f}s)")
