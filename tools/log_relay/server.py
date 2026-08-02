#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
抖音小游戏运行时日志收集器（本机中继）。
游戏内的日志中继 shim 会把 console/报错用 POST 发到 http://127.0.0.1:18765/log，
本服务收到即追加到 doc/douyin_runtime_log.txt，便于 AI 直接读取，无需人工复制。

用法:
    python server.py            # 默认 127.0.0.1:18765
    python server.py --port 9xxx
停止: Ctrl+C
"""
import sys, os, json, datetime, threading
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import urlparse, parse_qs

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(os.path.dirname(HERE))          # MiniGame1_Project
LOG_PATH = os.path.join(PROJECT_ROOT, "doc", "douyin_runtime_log.txt")
MAX_BYTES = 10 * 1024 * 1024                                  # 超过 10MB 轮转
_lock = threading.Lock()

os.makedirs(os.path.dirname(LOG_PATH), exist_ok=True)

def _stamp():
    return datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]

def _write_line(line):
    with _lock:
        # 简单轮转
        if os.path.exists(LOG_PATH) and os.path.getsize(LOG_PATH) > MAX_BYTES:
            try:
                os.replace(LOG_PATH, LOG_PATH + ".1")
            except OSError:
                pass
        with open(LOG_PATH, "a", encoding="utf-8") as f:
            f.write(line + "\n")
            f.flush()

def _ingest(raw_body: bytes):
    text = raw_body.decode("utf-8", errors="replace").strip()
    if not text:
        return
    try:
        data = json.loads(text)
        if isinstance(data, dict) and isinstance(data.get("entries"), list):
            for e in data["entries"]:
                ts = _stamp()
                lvl = str(e.get("level", "log")).upper()
                msg = e.get("msg", "")
                if isinstance(msg, (dict, list)):
                    msg = json.dumps(msg, ensure_ascii=False)
                _write_line(f"[{ts}] [{lvl}] {msg}")
            return
    except Exception:
        pass
    # 非 JSON：原样写
    for ln in text.splitlines():
        _write_line(f"[{_stamp()}] [RAW] {ln}")

class Handler(BaseHTTPRequestHandler):
    def _cors(self):
        self.send_header("Access-Control-Allow-Origin", "*")
        self.send_header("Access-Control-Allow-Methods", "POST, GET, OPTIONS")
        self.send_header("Access-Control-Allow-Headers", "content-type")

    def do_OPTIONS(self):
        self.send_response(204)
        self._cors()
        self.end_headers()

    def do_GET(self):
        if self.path.startswith("/healthz"):
            self.send_response(200)
            self._cors()
            self.end_headers()
            self.wfile.write(b"ok")
            return
        if self.path.startswith("/log"):
            # 来自 shim 的 GET(query) 上报：/log?level=...&msg=...
            try:
                params = parse_qs(urlparse(self.path).query)
                level = (params.get("level") or ["log"])[0]
                msg = (params.get("msg") or [""])[0]
                _write_line(f"[{_stamp()}] [{level.upper()}] {msg}")
            except Exception as e:
                sys.stderr.write("GET /log parse error: %s\n" % e)
            self.send_response(200)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self._cors()
            self.end_headers()
            self.wfile.write(b"ok")
            return
        if self.path.startswith("/tail"):
            # 返回最近 200 行，便于快速查看
            try:
                with open(LOG_PATH, "r", encoding="utf-8", errors="replace") as f:
                    lines = f.readlines()[-200:]
                body = "".join(lines).encode("utf-8")
            except FileNotFoundError:
                body = b"(no log yet)"
            self.send_response(200)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self._cors()
            self.end_headers()
            self.wfile.write(body)
            return
        self.send_response(404)
        self.end_headers()

    def do_POST(self):
        length = int(self.headers.get("Content-Length", 0) or 0)
        body = self.rfile.read(length) if length else b""
        try:
            _ingest(body)
            self.send_response(200)
            self.send_header("Content-Type", "text/plain; charset=utf-8")
            self._cors()
            self.end_headers()
            self.wfile.write(b"ok")
        except Exception as e:
            self.send_response(500)
            self.end_headers()
            sys.stderr.write("ingest error: %s\n" % e)

    def log_message(self, fmt, *args):
        # 静默默认访问日志，避免刷屏
        return

def main():
    port = 18765
    for i, a in enumerate(sys.argv):
        if a == "--port" and i + 1 < len(sys.argv):
            try: port = int(sys.argv[i + 1])
            except ValueError: pass
    srv = ThreadingHTTPServer(("127.0.0.1", port), Handler)
    print(f"[LogRelay] listening on http://127.0.0.1:{port}/log")
    print(f"[LogRelay] writing to {LOG_PATH}")
    print(f"[LogRelay] tail: http://127.0.0.1:{port}/tail  health: http://127.0.0.1:{port}/healthz")
    try:
        srv.serve_forever()
    except KeyboardInterrupt:
        print("\n[LogRelay] stopped")
        srv.shutdown()

if __name__ == "__main__":
    main()
