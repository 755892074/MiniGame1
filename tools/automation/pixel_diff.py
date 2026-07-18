# -*- coding: utf-8 -*-
"""
像素差分：输入 4 张截图路径，输出动画验证结论。
由 WorkBuddy 通过直连 MCP 拍完截图后调用。

用法： python pixel_diff.py frame0.png frame1.png frame2.png frame3.png
"""
import sys
from PIL import Image
import numpy as np


def pixel_diff(paths, threshold=25):
    """返回 (max_diff, bbox_str, anim_ok, details)"""
    imgs = []
    for p in paths:
        imgs.append(np.asarray(Image.open(p).convert("L")))

    max_diff = 0
    details = []
    for i in range(len(imgs) - 1):
        diff = np.abs(imgs[i].astype(int) - imgs[i + 1].astype(int))
        cnt = int((diff > threshold).sum())
        max_diff = max(max_diff, cnt)
        details.append(cnt)

    # 变化区域
    acc = np.zeros_like(imgs[0], dtype=int)
    for i in range(len(imgs) - 1):
        diff = np.abs(imgs[i].astype(int) - imgs[i + 1].astype(int))
        acc += (diff > threshold).astype(int)
    ys, xs = np.where(acc > 0)
    if len(xs):
        bbox = f"{xs.max()-xs.min()+1}x{ys.max()-ys.min()+1} @ ({xs.min()},{ys.min()})"
    else:
        bbox = "无变化"

    anim_ok = max_diff > 2000
    return max_diff, bbox, anim_ok, details


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("用法: python pixel_diff.py <帧0.png> <帧1.png> <帧2.png> <帧3.png>")
        sys.exit(1)

    paths = sys.argv[1:5]
    max_diff, bbox, anim_ok, details = pixel_diff(paths)

    print(f"帧间变化: {' → '.join(str(d) for d in details)} px")
    print(f"最大变化: {max_diff} px")
    print(f"变化区域: {bbox}")
    print(f"动画: {'✅ 在播' if anim_ok else '⚠️ 变化偏小，动画可能未播'}")
