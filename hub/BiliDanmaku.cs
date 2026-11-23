using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CUStreamHub
{
    /// <summary>
    /// B站直播弹幕接收：WBI 签名取 token → 自建 WS 连 wss → 解包（含 zlib）→ 回调。
    /// 只用 net48 自带：SslStream + DeflateStream + JavaScriptSerializer。
    /// </summary>
    internal sealed class BiliDanmaku : IDisposable
    {
        private const string Ua = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";
        private static readonly int[] MixTab = {
            46,47,18,2,53,8,23,32,15,50,10,31,58,3,45,35,27,43,5,49,33,9,42,19,29,28,14,39,12,38,41,13,
            37,48,7,16,24,55,40,61,26,17,0,1,60,51,30,4,22,25,54,21,56,59,6,63,57,62,11,36,20,34,44,52 };

        public event Action<string, string> OnDanmaku;
        public event Action<string> OnLog;
        public event Action<bool> OnState;

        private WsClient ws;
        private CancellationTokenSource cts;
        private string cookie = "";
        private string buvid = "";
        private volatile bool running;

        public long Received;
        public long RealRoomId;
        public string RoomTitle = "";
        /// <summary>诊断开关：把每个 op=5 的 cmd、心跳回复都写进日志。</summary>
        public bool Verbose;
        /// <summary>登录态 Cookie（SESSDATA 等），空则匿名。可写在 exe 同目录 cookie.txt。</summary>
        public string Session = "";
        /// <summary>登录态 UID（DedeUserID），鉴权包里用，0 表示游客。</summary>
        public long Uid;
        public bool Connected { get { return running && ws != null && ws.IsOpen; } }

        public void Start(string roomId)
        {
            Stop();
            if (string.IsNullOrEmpty(Session)) LoadSession();
            cts = new CancellationTokenSource();
            running = true;
            Task t = Task.Run(() => Supervise(roomId, cts.Token));
            t.ContinueWith(x =>
            {
                if (x.Exception != null)
                    Log("[fault] " + x.Exception.GetBaseException().GetType().Name + ": " + x.Exception.GetBaseException().Message);
            });
        }

        /// <summary>从 exe 同目录 cookie.txt 读取登录态（整行 Cookie，或只有 SESSDATA 值）。</summary>
        private void LoadSession()
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(BiliDanmaku).Assembly.Location);
                string f = Path.Combine(dir, "cookie.txt");
                if (!File.Exists(f)) return;
                string s = File.ReadAllText(f).Trim();
                if (s.Length == 0) return;
                if (s.IndexOf('=') < 0) s = "SESSDATA=" + s;
                Session = s;
                int i = s.IndexOf("DedeUserID=", StringComparison.OrdinalIgnoreCase);
                if (i >= 0)
                {
                    int j = i + "DedeUserID=".Length;
                    int k = s.IndexOf(';', j);
                    string v = k > 0 ? s.Substring(j, k - j) : s.Substring(j);
                    long u; if (long.TryParse(v.Trim(), out u)) Uid = u;
                }
                Log("已载入登录态 Cookie（" + (Uid > 0 ? "UID " + Uid : "UID 未知") + "）");
            }
            catch (Exception ex) { Log("读取 cookie.txt 失败：" + ex.Message); }
        }

        /// <summary>断线兜底重连：3→6→12→24→30 秒退避，只有用户主动 Stop 才真正停。</summary>
        private void Supervise(string roomId, CancellationToken ct)
        {
            int attempt = 0;
            while (!ct.IsCancellationRequested)
            {
                RunLoop(roomId, ct);
                if (ct.IsCancellationRequested) break;
                attempt++;
                int wait = Math.Min(30, 3 * (1 << Math.Min(attempt - 1, 4)));
                Log("连接中断，" + wait + " 秒后自动重连（第 " + attempt + " 次）");
                for (int i = 0; i < wait * 10 && !ct.IsCancellationRequested; i++) Thread.Sleep(100);
            }
        }

        public void Stop()
        {
            running = false;
            try { if (cts != null) cts.Cancel(); } catch { }
            try { if (ws != null) ws.Close(); } catch { }
            ws = null;
            Log("弹幕已断开");
            RaiseState(false);
        }

        public void Dispose() { Stop(); }

        private void Log(string s) { var h = OnLog; if (h != null) h(s); }
        private void RaiseState(bool on) { var h = OnState; if (h != null) h(on); }

        private void RunLoop(string roomId, CancellationToken ct)
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                Log("正在获取直播间信息（房间 " + roomId + "）…");

                Dictionary<string, object> info = GetDanmuInfo(roomId);
                if (info == null) { Log("获取弹幕地址失败，结束"); return; }
                string token = Str(info, "data.token");
                if (string.IsNullOrEmpty(token)) { Log("没拿到 token，结束"); return; }

                string host = Str(info, "data.host_list.0.host");
                int port = int.Parse(Str(info, "data.host_list.0.wss_port"));
                Log("房间标题：" + RoomTitle);
                Log("弹幕服务器：wss://" + host + ":" + port + "/sub");

                string headers = "User-Agent: " + Ua + "\r\n"
                               + "Origin: https://live.bilibili.com\r\n"
                               + "Referer: https://live.bilibili.com/" + roomId + "\r\n"
                               + (cookie.Length > 0 ? "Cookie: " + cookie + "\r\n" : "");

                ws = new WsClient();
                ws.Connect(host, port, "/sub", headers);
                Log("已连上弹幕服务器，发送鉴权…");

                // support_ack / queue_uuid / scene 是新版 comet 订阅必需字段：
                // 缺了它们服务器照样回 {"code":0}，但不会把该房间的弹幕推给这条连接（表现为 0 包、连心跳回复都没有）
                string auth = "{\"uid\":" + (Uid > 0 ? Uid : 0)
                            + ",\"roomid\":" + AuthRoom(roomId)
                            + ",\"protover\":2"
                            + ",\"buvid\":\"" + buvid + "\""
                            + ",\"support_ack\":true"
                            + ",\"queue_uuid\":\"" + QueueUuid() + "\""
                            + ",\"scene\":\"room\""
                            + ",\"platform\":\"web\",\"type\":2"
                            + ",\"key\":\"" + token + "\"}";
                Log("鉴权房间号 = " + AuthRoom(roomId) + "（protover 2 / zlib）");
                ws.Send(Pack(7, Encoding.UTF8.GetBytes(auth)), 2);

                DateTime nextHb = DateTime.UtcNow;   // 网页端连上就立刻发一次心跳
                var acc = new List<byte>();
                while (!ct.IsCancellationRequested && ws.IsOpen)
                {
                    int op;
                    byte[] payload = ws.Receive(out op);
                    if (payload == null) { Log("服务器关闭了连接 " + ws.LastClose); break; }
                    if (Verbose && payload.Length > 0)
                    {
                        var hx = new StringBuilder();
                        for (int i = 0; i < Math.Min(24, payload.Length); i++) hx.Append(payload[i].ToString("x2"));
                        Log("[raw] " + payload.Length + " 字节 " + hx);
                    }
                    if (payload.Length > 0)
                    {
                        acc.AddRange(payload);
                        byte[] all = acc.ToArray();
                        int used = 0;
                        while (all.Length - used >= 4)
                        {
                            int total = ReadI32(all, used);
                            // 服务器偶尔会夹带几字节残片，硬按 total 走会让整条流错位；
                            // 这里用「头部自洽性」校验，不合法就只丢一个字节重新对齐。
                            int hl = all.Length - used >= 6 ? ReadI16(all, used + 4) : 0;
                            int ver = all.Length - used >= 8 ? ReadI16(all, used + 6) : 0;
                            int opv = all.Length - used >= 12 ? ReadI32(all, used + 8) : -1;
                            bool sane = total >= 16 && total <= 16777216 && hl >= 12 && hl <= total
                                        && ver >= 0 && ver <= 3 && (opv == 2 || opv == 3 || opv == 5 || opv == 8);
                            if (!sane) { used++; continue; }
                            if (all.Length - used < total) break;
                            HandlePacket(all, used, total);
                            used += total;
                        }
                        if (used > 0)
                        {
                            byte[] keep = new byte[all.Length - used];
                            Buffer.BlockCopy(all, used, keep, 0, keep.Length);
                            acc = new List<byte>(keep);
                        }
                    }
                    if (DateTime.UtcNow >= nextHb)
                    {
                        try { ws.Send(Pack(2, Encoding.UTF8.GetBytes("[object Object]")), 2); } catch { }
                        nextHb = DateTime.UtcNow.AddSeconds(28);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("弹幕异常：" + ex.GetType().Name + " " + ex.Message);
            }
            finally
            {
                // running 由 Supervise 掌住，只有 Stop() 才置 false；这里只报状态
                RaiseState(false);
            }
        }

        private void HandlePacket(byte[] all, int off, int total)
        {
            short ver = (short)ReadI16(all, off + 6);
            int op = ReadI32(all, off + 8);
            int hl = ReadI16(all, off + 4);
            if (hl < 12 || hl > total) hl = 16;
            int bodyLen = total - hl;
            if (bodyLen <= 0) return;
            byte[] body = new byte[bodyLen];
            Buffer.BlockCopy(all, off + hl, body, 0, bodyLen);

            if (op == 8) { Log("★ 鉴权回复：" + Encoding.UTF8.GetString(body)); RaiseState(true); return; }
            if (op == 3) { if (Verbose) Log("[hb] 心跳回复 " + bodyLen + " 字节"); return; }

            if (op == 5)
            {
                if (ver == 3)
                {
                    Log("收到 brotli 压缩包（ver=3），本程序只解 zlib；protover 请保持 2");
                    return;
                }
                if (ver == 2 || ver == 3)
                {
                    byte[] inf;
                    try { inf = Inflate(body); } catch { try { inf = Gzip(body); } catch { return; } }
                    int u = 0;
                    while (inf.Length - u >= 4)
                    {
                        int t2 = ReadI32(inf, u);
                        if (t2 <= 0 || inf.Length - u < t2) break;
                        HandlePacket(inf, u, t2);
                        u += t2;
                    }
                    return;
                }
                if (ver == 0 || ver == 1)
                {
                    Interlocked.Increment(ref Received);
                    Dispatch(Encoding.UTF8.GetString(body));
                }
            }
        }

        private void Dispatch(string json)
        {
            if (Verbose)
            {
                int ci = json.IndexOf("\"cmd\":\"", StringComparison.Ordinal);
                string cmd = "?";
                if (ci >= 0)
                {
                    int s0 = ci + 7, e0 = json.IndexOf('"', s0);
                    if (e0 > s0) cmd = json.Substring(s0, Math.Min(40, e0 - s0));
                }
                Log("[pkt] " + cmd + " (" + json.Length + " 字节)");
            }
            if (json.IndexOf("DANMU_MSG", StringComparison.Ordinal) < 0) return;
            try
            {
                var ser = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
                var o = ser.DeserializeObject(json) as Dictionary<string, object>;
                if (o == null || !o.ContainsKey("info")) return;
                var info = o["info"] as object[];
                if (info == null || info.Length < 3) return;
                string text = info[1] as string;
                var user = info[2] as object[];
                string uname = (user != null && user.Length > 1) ? (user[1] as string) : "观众";
                var h = OnDanmaku;
                if (h != null && !string.IsNullOrEmpty(text)) h(uname, text);
            }
            catch { }
        }

        private static byte[] Inflate(byte[] data)
        {
            using (var input = new MemoryStream(data, 2, data.Length - 2))
            using (var ds = new DeflateStream(input, CompressionMode.Decompress))
            using (var outp = new MemoryStream())
            {
                byte[] tmp = new byte[16384];
                int n;
                while ((n = ds.Read(tmp, 0, tmp.Length)) > 0) outp.Write(tmp, 0, n);
                return outp.ToArray();
            }
        }

        private static byte[] Gzip(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var gz = new GZipStream(input, CompressionMode.Decompress))
            using (var outp = new MemoryStream())
            {
                byte[] tmp = new byte[16384];
                int n;
                while ((n = gz.Read(tmp, 0, tmp.Length)) > 0) outp.Write(tmp, 0, n);
                return outp.ToArray();
            }
        }

        // B站弹幕协议是**大端**（>IHHII），BitConverter 在小端机器上会组出废包、被服务器直接断线
        private static void WriteI16(byte[] b, int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
        private static void WriteI32(byte[] b, int o, int v)
        { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
        private static int ReadI16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; }
        private static int ReadI32(byte[] b, int o)
        { return (b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]; }

        private static byte[] Pack(int op, byte[] body)
        {
            int total = 16 + body.Length;
            byte[] p = new byte[total];
            WriteI32(p, 0, total);
            WriteI16(p, 4, 16);
            WriteI16(p, 6, 1);
            WriteI32(p, 8, op);
            WriteI32(p, 12, 1);
            Buffer.BlockCopy(body, 0, p, 16, body.Length);
            return p;
        }

        // ── HTTP：buvid → nav(wbi) → 签名 → getDanmuInfo → 房间信息 ──
        private Dictionary<string, object> GetDanmuInfo(string room)
        {
            try
            {
                var spi = GetJson("https://api.bilibili.com/x/frontend/finger/spi");
                string b3 = Str(spi, "data.b_3");
                string b4 = Str(spi, "data.b_4");
                if (!string.IsNullOrEmpty(b3)) { cookie = "buvid3=" + b3 + "; buvid4=" + b4; buvid = b3; }
                if (!string.IsNullOrEmpty(Session))
                    cookie = Session + (cookie.Length > 0 ? "; " + cookie : "");

                var nav = GetJson("https://api.bilibili.com/x/web-interface/nav");
                string isLogin = Str(nav, "data.isLogin");
                string navCode = Str(nav, "code");
                if (navCode != "0" || isLogin != "True")
                {
                    if (Uid > 0)
                    {
                        Log("提示：Cookie 登录态已过期失效，自动降级为公开匿名游客模式 (UID 0)，确保稳定接收弹幕");
                    }
                    Uid = 0;
                    cookie = "buvid3=" + b3 + "; buvid4=" + b4;
                }
                string img = Str(nav, "data.wbi_img.img_url");
                string sub = Str(nav, "data.wbi_img.sub_url");
                if (string.IsNullOrEmpty(img)) { Log("取 wbi key 失败"); return null; }
                string raw = Stem(img) + Stem(sub);
                var sb = new StringBuilder();
                foreach (int i in MixTab) sb.Append(raw[i]);
                string mixin = sb.ToString().Substring(0, 32);

                string wts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                string query = "id=" + Uri.EscapeDataString(room) + "&type=0&wts=" + wts;
                string wRid = Md5(query + mixin);
                var di = GetJson("https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?" + query + "&w_rid=" + wRid);
                if (Str(di, "data.token") == null)
                {
                    Log("getDanmuInfo 返回异常：code=" + Str(di, "code") + " " + Str(di, "message"));
                    return null;
                }

                var ri = GetJson("https://api.live.bilibili.com/room/v1/Room/get_info?room_id=" + Uri.EscapeDataString(room));
                RoomTitle = Str(ri, "data.title") ?? "(未取到标题)";
                // 真实房间号：短号鉴权会被服务器直接断线，这里以接口返回的 room_id 为准
                string rid = Str(di, "data.room_id") ?? Str(ri, "data.room_id");
                long real;
                if (!string.IsNullOrEmpty(rid) && long.TryParse(rid, out real) && real > 0) RealRoomId = real;
                return di;
            }
            catch (Exception ex) { Log("取 token 失败：" + ex.Message); return null; }
        }

        /// <summary>鉴权必须用真实房间号（接口返回的 room_id），短号会被服务器直接断线。</summary>
        private long AuthRoom(string roomId)
        {
            if (RealRoomId > 0) return RealRoomId;
            long v;
            return long.TryParse(roomId, out v) && v > 0 ? v : 0;
        }

        /// <summary>新版 comet 鉴权里的队列标识，网页端是随机短串，每次连接现生成即可。</summary>
        private static string QueueUuid()
        {
            const string abc = "abcdefghijklmnopqrstuvwxyz0123456789";
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var sb = new StringBuilder(8);
            for (int i = 0; i < 8; i++) sb.Append(abc[rnd.Next(abc.Length)]);
            return sb.ToString();
        }

        private static string Stem(string url)
        {
            int i = url.LastIndexOf('/');
            string f = i >= 0 ? url.Substring(i + 1) : url;
            int d = f.IndexOf('.');
            return d > 0 ? f.Substring(0, d) : f;
        }

        private static string Md5(string s)
        {
            using (var md5 = MD5.Create())
            {
                byte[] h = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder();
                foreach (byte b in h) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string Str(Dictionary<string, object> root, string path)
        {
            if (root == null) return null;
            object cur = root;
            foreach (string seg in path.Split('.'))
            {
                if (cur is Dictionary<string, object>)
                {
                    var d = (Dictionary<string, object>)cur;
                    if (!d.ContainsKey(seg)) return null;
                    cur = d[seg];
                }
                else if (cur is object[])
                {
                    int idx; int.TryParse(seg, out idx);
                    var a = (object[])cur;
                    if (idx < 0 || idx >= a.Length) return null;
                    cur = a[idx];
                }
                else if (cur is ArrayList)
                {
                    int idx; int.TryParse(seg, out idx);
                    var a = (ArrayList)cur;
                    if (idx < 0 || idx >= a.Count) return null;
                    cur = a[idx];
                }
                else return null;
            }
            return cur == null ? null : cur.ToString();
        }

        private Dictionary<string, object> GetJson(string url)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.UserAgent = Ua;
            req.Referer = "https://live.bilibili.com/";
            req.Timeout = 12000;
            req.ReadWriteTimeout = 12000;
            if (cookie.Length > 0) req.Headers["Cookie"] = cookie;
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                string text = sr.ReadToEnd();
                var ser = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
                return ser.DeserializeObject(text) as Dictionary<string, object>;
            }
        }
    }
}
