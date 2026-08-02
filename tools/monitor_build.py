#!/usr/bin/env python3
"""Monitor the Douyin package build progress until completion."""
import time, os, sys

PROJ = r"F:\WorkBuddy\H5MiniGame\MiniGame1\MiniGame1_Project"
GAME_JS = os.path.join(PROJ, "doc", "douyin_package", "tt-minigame", "game.js")
WEBGL_DIR = os.path.join(PROJ, "doc", "douyin_package", "webgl")
LOG = r"C:\Users\Lionel\AppData\Local\Tuanjie\Editor\Editor.log"

START = time.time()

def last_autobuild_line():
    try:
        with open(LOG, "r", encoding="utf-8", errors="replace") as f:
            lines = f.readlines()
        for ln in reversed(lines):
            if "[AutoBuild]" in ln:
                return ln.rstrip("\n")[:120]
    except Exception as e:
        return "ERR " + str(e)
    return "(none)"

def gamejs_mtime():
    if not os.path.exists(GAME_JS):
        return None
    return os.path.getmtime(GAME_JS)

print("monitor start: %s" % time.strftime("%H:%M:%S", time.localtime(START)))
done = False
for i in range(1, 30):
    time.sleep(30)
    el = int(time.time() - START)
    m = gamejs_mtime()
    webgl = os.path.isdir(WEBGL_DIR)
    line = last_autobuild_line()
    mstr = time.strftime("%H:%M:%S", time.localtime(m)) if m else "N/A"
    print("[%2d] +%4ds game.js=%s webgl=%s | %s" % (i, el, mstr, webgl, line))
    if "全部完成" in line:
        print(">>> BUILD DONE (shim injected)")
        done = True
        break
    # also done if game.js regenerated fresh and webgl present (fallback)
    if m and m > START + 5 and webgl:
        # give a couple more checks to catch 全部完成 log
        if i >= 3:
            print(">>> package regenerated; waiting for final log...")
done_mark = "DONE" if done else "TIMED_OUT_OR_RUNNING"
print("monitor end: %s (%s)" % (time.strftime("%H:%M:%S"), done_mark))
