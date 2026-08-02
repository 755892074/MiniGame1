#!/usr/bin/env python3
"""Minimal MCP Streamable-HTTP client for the local unity-mcp server at 8080.
Usage:
  python mcp_client.py list
  python mcp_client.py <tool_name> '<json_arguments>'
It speaks initialize -> initialized -> tools/list|tools/call over SSE.
"""
import sys, json, urllib.request

URL = "http://127.0.0.1:8080/mcp"
PROTOCOL = "2024-11-05"


def _post(payload, session_id=None):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(URL, data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    req.add_header("Accept", "application/json, text/event-stream")
    if session_id:
        req.add_header("mcp-session-id", session_id)
    resp = urllib.request.urlopen(req, timeout=1200)
    sid = resp.headers.get("mcp-session-id")
    body = resp.read().decode("utf-8", "replace")
    return sid, body


def _parse_sse(body):
    out = []
    for line in body.splitlines():
        line = line.strip()
        if line.startswith("data:"):
            chunk = line[5:].strip()
            try:
                out.append(json.loads(chunk))
            except Exception:
                pass
    return out


def _session():
    sid, _ = _post({
        "jsonrpc": "2.0", "id": 1, "method": "initialize",
        "params": {"protocolVersion": PROTOCOL, "capabilities": {},
                   "clientInfo": {"name": "wb-cli", "version": "1.0"}},
    })
    try:
        _post({"jsonrpc": "2.0", "method": "notifications/initialized", "params": {}}, sid)
    except Exception:
        pass
    return sid


def main():
    if len(sys.argv) < 2:
        print("usage: mcp_client.py list|<tool> [json_args]")
        return
    sid = _session()
    if sys.argv[1] == "list":
        _, body = _post({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}}, sid)
        for o in _parse_sse(body):
            if "result" in o:
                for t in o["result"].get("tools", []):
                    print("==", t.get("name"))
                    print("   ", (t.get("description") or "")[:120].replace("\n", " "))
                    sch = t.get("inputSchema", {}).get("properties", {})
                    req = t.get("inputSchema", {}).get("required", [])
                    print("    props:", ", ".join(sorted(sch.keys())) or "(none)")
                    if req:
                        print("    required:", ", ".join(req))
        return
    tool = sys.argv[1]
    args = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    _, body = _post({"jsonrpc": "2.0", "id": 3, "method": "tools/call",
                     "params": {"name": tool, "arguments": args}}, sid)
    for o in _parse_sse(body):
        if "result" in o:
            for c in o["result"].get("content", []):
                if c.get("type") == "text":
                    print(c.get("text", ""))
                else:
                    print(json.dumps(c, ensure_ascii=False))
        elif "error" in o:
            print("ERROR:", json.dumps(o["error"], ensure_ascii=False))


if __name__ == "__main__":
    main()
