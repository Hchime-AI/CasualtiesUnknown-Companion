# -*- coding: utf-8 -*-
"""
B站直播弹幕 -> 游戏内角色（零依赖，只用 Python 自带库）
用法:  python danmaku.py [房间号]
默认房间号见下方 DEFAULT_ROOM，默认转发到 http://127.0.0.1:PORT/danmaku
"""
import json
import random
import socket
import ssl
import struct
import sys
import time
import urllib.parse
import urllib.request
import zlib

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

# 唯一来源：端口与 mod 端 src/Limits.cs 的 DefaultStreamPort 一致；
# 默认房间号与 Hub.cs 的 DefaultRoomShort、启动弹幕.bat 的 ROOM 一致。
DEFAULT_ROOM = "27415695"
PORT = 8768
ROOM = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_ROOM
GAME = "http://127.0.0.1:%d/danmaku" % PORT
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36")
MIXIN_TAB = [46,47,18,2,53,8,23,32,15,50,10,31,58,3,45,35,27,43,5,49,33,9,42,19,
             29,28,14,39,12,38,41,13,37,48,7,16,24,55,40,61,26,17,0,1,60,51,30,4,
             22,25,54,21,56,59,6,63,57,62,11,36,20,34,44,52]


def log(*a):
    print(time.strftime("[%H:%M:%S]"), *a, flush=True)


def http_get(url, cookie=""):
    req = urllib.request.Request(url, headers={
        "User-Agent": UA,
        "Referer": "https://live.bilibili.com/",
        "Cookie": cookie,
    })
    with urllib.request.urlopen(req, timeout=15) as r:
        return json.loads(r.read().decode("utf-8"))


def md5(s):
    import hashlib
    return hashlib.md5(s.encode()).hexdigest()


def wbi_keys():
    nav = http_get("https://api.bilibili.com/x/web-interface/nav")
    img = nav["data"]["wbi_img"]["img_url"]
    sub = nav["data"]["wbi_img"]["sub_url"]
    raw = img.rsplit("/", 1)[-1].split(".")[0] + sub.rsplit("/", 1)[-1].split(".")[0]
    return "".join(raw[i] for i in MIXIN_TAB)[:32]


def get_token(room):
    cookie = ""
    try:
        spi = http_get("https://api.bilibili.com/x/frontend/finger/spi")
        cookie = "buvid3=%s; buvid4=%s" % (spi["data"]["b_3"], spi["data"]["b_4"])
    except Exception as e:
        log("取 buvid 失败（继续）:", e)
    mixin = wbi_keys()
    wts = int(time.time())
    q = "id=%s&type=0&wts=%d" % (room, wts)
    w_rid = md5(q + mixin)
    di = http_get("https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?"
                  + q + "&w_rid=" + w_rid, cookie)
    if di.get("code") != 0:
        raise RuntimeError("getDanmuInfo 失败: %s" % di)
    host = di["data"]["host_list"][0]["host"]
    port = di["data"]["host_list"][0]["wss_port"]
    return host, port, di["data"]["token"], cookie


def pack(op, body: bytes) -> bytes:
    return struct.pack(">IHHII", 16 + len(body), 16, 1, op, 1) + body


def ws_connect(host, port, path, headers):
    sock = socket.create_connection((host, port), timeout=15)
    ctx = ssl.create_default_context()
    ctx.check_hostname = False
    ctx.verify_mode = ssl.CERT_NONE
    ss = ctx.wrap_socket(sock, server_hostname=host)
    key = __import__("base64").b64encode(bytes(random.getrandbits(8) for _ in range(16))).decode()
    req = ("GET %s HTTP/1.1\r\nHost: %s\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
           "Sec-WebSocket-Key: %s\r\nSec-WebSocket-Version: 13\r\n%s\r\n" % (path, host, key, headers))
    ss.sendall(req.encode())
    buf = b""
    while b"\r\n\r\n" not in buf:
        chunk = ss.recv(4096)
        if not chunk:
            raise RuntimeError("握手中连接被关闭")
        buf += chunk
    head, _, rest = buf.partition(b"\r\n\r\n")
    if b" 101 " not in head.split(b"\r\n")[0]:
        raise RuntimeError("握手失败: " + head.split(b"\r\n")[0].decode("latin1"))
    ss.settimeout(2)
    return ss, rest


def ws_send(sock, payload: bytes, opcode=2):
    mask = bytes(random.getrandbits(8) for _ in range(4))
    n = len(payload)
    if n < 126:
        head = bytes([0x80 | opcode, 0x80 | n])
    elif n < 65536:
        head = bytes([0x80 | opcode, 0x80 | 126]) + struct.pack(">H", n)
    else:
        head = bytes([0x80 | opcode, 0x80 | 127]) + struct.pack(">Q", n)
    body = bytes(b ^ mask[i % 4] for i, b in enumerate(payload))
    sock.sendall(head + mask + body)


def ws_recv(sock, buf: bytes):
    """返回 (opcode, payload, 剩余buf)；连接关闭返回 (None, None, buf)"""
    while True:
        if len(buf) < 2:
            data = sock.recv(65536)
            if not data:
                return None, None, buf
            buf += data
            continue
        b0, b1 = buf[0], buf[1]
        op = b0 & 0x0F
        masked = b1 & 0x80
        ln = b1 & 0x7F
        off = 2
        if ln == 126:
            if len(buf) < 4:
                data = sock.recv(65536)
                if not data:
                    return None, None, buf
                buf += data
                continue
            ln = struct.unpack(">H", buf[2:4])[0]
            off = 4
        elif ln == 127:
            if len(buf) < 10:
                data = sock.recv(65536)
                if not data:
                    return None, None, buf
                buf += data
                continue
            ln = struct.unpack(">Q", buf[2:10])[0]
            off = 10
        need = off + (4 if masked else 0) + ln
        if len(buf) < need:
            data = sock.recv(65536)
            if not data:
                return None, None, buf
            buf += data
            continue
        mk = buf[off:off + 4] if masked else b""
        start = off + (4 if masked else 0)
        payload = bytearray(buf[start:start + ln])
        if masked:
            for i in range(ln):
                payload[i] ^= mk[i % 4]
        return op, bytes(payload), buf[need:]


def handle_packet(data: bytes, on_danmaku):
    total, hlen, ver, op, _ = struct.unpack(">IHHII", data[:16])
    body = data[16:total]
    if op == 8:
        log("★ 鉴权回复:", body.decode("utf-8", "ignore")[:120])
        return
    if op == 3:
        return
    if op == 5:
        if ver == 2:
            try:
                body = zlib.decompress(body, -15)
            except Exception:
                try:
                    body = zlib.decompress(body)
                except Exception:
                    return
            _split(body, on_danmaku, nested=True)
            return
        if ver == 3:
            # brotli，没有第三方库就不解，交给 ver==2 的服务器
            return
        try:
            msg = json.loads(body.decode("utf-8", "ignore"))
        except Exception:
            return
        if isinstance(msg, dict) and str(msg.get("cmd", "")).startswith("DANMU_MSG"):
            info = msg.get("info") or []
            try:
                text = info[1]
                user = (info[2][1] if len(info) > 2 and isinstance(info[2], list) else "观众")
            except Exception:
                return
            if text:
                on_danmaku(user, text)


def _split(data: bytes, on_danmaku, nested=False):
    while len(data) >= 16:
        total = struct.unpack(">I", data[:4])[0]
        if total < 16 or total > len(data):
            break
        handle_packet(data[:total], on_danmaku)
        data = data[total:]


def post_game(user, text):
    body = json.dumps({"user": user, "text": text}, ensure_ascii=False).encode("utf-8")
    req = urllib.request.Request(GAME, data=body, headers={
        "Content-Type": "application/json; charset=utf-8"})
    try:
        with urllib.request.urlopen(req, timeout=5) as r:
            j = json.loads(r.read().decode())
            if j.get("ok"):
                log("→ 游戏", "%s: %s" % (user, text))
            else:
                log("× 被丢弃", "%s: %s (%s)" % (user, text, j.get("err")))
    except Exception as e:
        log("× 游戏端没响应:", e)


def run():
    while True:
        try:
            host, port, token, cookie = get_token(ROOM)
            log("房间 %s，弹幕服务器 %s:%s" % (ROOM, host, port))
            headers = ("User-Agent: %s\r\nOrigin: https://live.bilibili.com\r\n"
                       "Referer: https://live.bilibili.com/%s\r\n" % (UA, ROOM))
            if cookie:
                headers += "Cookie: %s\r\n" % cookie
            sock, rest = ws_connect(host, port, "/sub", headers)
            auth = json.dumps({"uid": 0, "roomid": int(ROOM), "protover": 2,
                               "platform": "web", "type": 2, "key": token}).encode()
            ws_send(sock, pack(7, auth))
            log("已发送鉴权，等待回复…")

            buf = rest
            last_hb = time.time()
            while True:
                try:
                    op, payload, buf = ws_recv(sock, buf)
                except socket.timeout:
                    ws_send(sock, pack(2, b'[object Object]'))
                    last_hb = time.time()
                    continue
                if op is None:
                    log("连接被关闭，2 秒后重连")
                    break
                if op == 9:
                    ws_send(sock, payload, 10)
                    continue
                if op == 8:
                    log("服务器要求关闭")
                    break
                if payload:
                    _split(payload, lambda u, t: post_game(u, t))
                if time.time() - last_hb > 20:
                    ws_send(sock, pack(2, b"[object Object]"))
                    last_hb = time.time()
            try:
                sock.close()
            except Exception:
                pass
        except Exception as e:
            log("出错:", repr(e))
        time.sleep(2)


if __name__ == "__main__":
    log("直播弹幕转发 启动，房间", ROOM)
    run()
