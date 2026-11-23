using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace CUStreamHub
{
    /// <summary>
    /// 未知伤亡 · 直播控制台（进程外窗口）
    /// 作用：配置游戏端弹幕接收地址、查看限流与余额状态、手动测试推送；
    /// 并给出 B站 / 抖音 / OBS 等外部工具把弹幕推到游戏里的现成模板。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length >= 2 && args[0] == "--test")
            {
                RunTest(args[1], args.Length >= 3 ? int.Parse(args[2]) : 30);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new HubForm());
        }

        private static void RunTest(string room, int seconds)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }
            string logPath = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "test.log");
            File.WriteAllText(logPath, "", Encoding.UTF8);
            Action<string> W = delegate(string m) { try { File.AppendAllText(logPath, m + (char)13 + (char)10, Encoding.UTF8); } catch { } };
            BiliDanmaku b = new BiliDanmaku();
            b.Verbose = true;
            b.OnLog += delegate(string m) { W("[log] " + m); };
            b.OnState += delegate(bool on) { W("[state] connected=" + on); };
            b.OnDanmaku += delegate(string u, string t) { W("[danmaku] " + u + " : " + t); };
            b.Start(room);
            DateTime end = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < end) { System.Threading.Thread.Sleep(500); }
            W("[done] 收到 " + b.Received + " 个包");
            b.Stop();
        }
    }

    internal sealed class HubForm : Form
    {
        private readonly TextBox urlBox = new TextBox();
        private readonly Label statusLabel = new Label();
        private readonly ComboBox platform = new ComboBox();
        private readonly TextBox guide = new TextBox();
        private readonly TextBox userBox = new TextBox();
        private readonly TextBox msgBox = new TextBox();
        private readonly ListBox log = new ListBox();
        private readonly Timer poll = new Timer();

        private readonly TextBox roomBox = new TextBox();
        private readonly Label biliLabel = new Label();
        private readonly BiliDanmaku bili = new BiliDanmaku();
        private Button biliBtn;
        private bool biliOn;

        // 唯一来源：端口必须与 mod 端 src/Limits.cs 的 DefaultStreamPort 一致，
        // 房间号与 启动弹幕.bat / danmaku.py 的默认值一致。
        private const int DefaultPort = 8768;
        private const string DefaultRoomShort = "27415695";
        private static readonly string DefaultUrl = "http://127.0.0.1:" + DefaultPort + "/danmaku";

        public HubForm()
        {
            Text = "未知伤亡 · 直播控制台";
            ClientSize = new Size(760, 664);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(24, 26, 32);
            ForeColor = Color.Gainsboro;
            Font = new Font("Microsoft YaHei UI", 9f);

            // ── 游戏端 ──
            Section("游戏端接收地址（必须与游戏内「直播模式」的端口一致）", 24, 12);
            urlBox.SetBounds(24, 44, 560, 26);
            urlBox.Text = DefaultUrl;
            urlBox.BackColor = Color.FromArgb(38, 41, 49);
            urlBox.ForeColor = Color.White;
            urlBox.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(urlBox);

            Button copy = Button2("复制地址", 594, 43, 80);
            copy.Click += delegate { Clipboard.SetText(urlBox.Text.Trim()); Say("已复制：" + urlBox.Text.Trim()); };
            Controls.Add(copy);

            statusLabel.SetBounds(24, 74, 700, 22);
            statusLabel.ForeColor = Color.FromArgb(140, 200, 255);
            statusLabel.Text = "状态：等待查询…";
            Controls.Add(statusLabel);

            // ── 弹幕来源 ──
            Section("弹幕来源（选一个，按下面的模板把弹幕 POST 到上面的地址）", 24, 112);

            platform.SetBounds(24, 150, 220, 26);
            platform.DropDownStyle = ComboBoxStyle.DropDownList;
            platform.Items.AddRange(new object[] { "B站（blivedm 脚本）", "抖音（网页采集脚本）", "OBS / Streamer.bot", "其他任意脚本 / 自己写的", "手动测试（无需弹幕源）" });
            platform.SelectedIndex = 0;
            platform.BackColor = Color.FromArgb(38, 41, 49);
            platform.ForeColor = Color.White;
            platform.SelectedIndexChanged += delegate { RefreshGuide(); };
            Controls.Add(platform);

            Button apply = Button2("把模板写到下面的输入框", 254, 149, 190);
            apply.Click += delegate { RefreshGuide(); };
            Controls.Add(apply);

            guide.SetBounds(24, 184, 700, 132);
            guide.Multiline = true;
            guide.ScrollBars = ScrollBars.Vertical;
            guide.ReadOnly = false;
            guide.BackColor = Color.FromArgb(18, 20, 25);
            guide.ForeColor = Color.FromArgb(190, 196, 210);
            guide.BorderStyle = BorderStyle.FixedSingle;
            guide.Font = new Font("Consolas", 9f);
            Controls.Add(guide);

            // ── 手动发送 ──
            Section("手动发送（相当于本地观众发一条，不受每分钟上限影响）", 24, 344);
            userBox.SetBounds(24, 372, 160, 26);
            userBox.Text = "测试观众";
            Style(userBox);
            Controls.Add(userBox);

            msgBox.SetBounds(194, 372, 420, 26);
            msgBox.Text = "主人，今天也要活着回来";
            Style(msgBox);
            Controls.Add(msgBox);

            Button send = Button2("发送到游戏", 622, 371, 100);
            send.Click += delegate { SendDanmaku(userBox.Text, msgBox.Text); };
            Controls.Add(send);
            // ── 日志 ──
            // ── B站直播间（内置接收，不需装 Python） ──
            Section("B站直播间（内置接收，不需装 Python）", 24, 446);

            roomBox.SetBounds(24, 474, 130, 26);
            roomBox.Text = DefaultRoomShort;
            roomBox.TextChanged += delegate { if (platform.SelectedIndex == 0) RefreshGuide(); };
            Style(roomBox);
            Controls.Add(roomBox);

            biliBtn = Button2("连接弹幕", 164, 473, 110);
            biliBtn.Click += delegate { ToggleBili(); };
            Controls.Add(biliBtn);

            biliLabel.SetBounds(272, 478, 452, 22);
            biliLabel.ForeColor = Color.FromArgb(150, 160, 178);
            biliLabel.Text = "弹幕：未连接";
            Controls.Add(biliLabel);

            Section("日志", 24, 512);
            log.SetBounds(24, 540, 700, 104);
            log.BackColor = Color.FromArgb(18, 20, 25);
            log.ForeColor = Color.FromArgb(200, 205, 215);
            log.BorderStyle = BorderStyle.FixedSingle;
            log.Font = new Font("Consolas", 8.5f);
            Controls.Add(log);

            bili.OnLog += delegate(string m) { BeginInvoke((Action)delegate { Say(m); }); };
            bili.OnDanmaku += delegate(string u, string t) { BeginInvoke((Action)delegate { Say("弹幕 " + u + "：" + t); SendDanmaku(u, t); }); };
            bili.OnState += delegate(bool on) { BeginInvoke((Action)delegate { biliLabel.Text = on ? ("弹幕：已连接直播间 " + roomBox.Text.Trim()) : "弹幕：未连接"; biliBtn.Text = on ? "断开" : "连接弹幕"; }); };

            poll.Interval = 2000;
            poll.Tick += delegate { Poll(); };
            poll.Start();

            RefreshGuide();
            Say("控制台已启动。游戏里打开「直播模式」后，这里的状态会变成在线。");
            // 启动即自动连接：以前要手点「连接弹幕」，用户忘了点就一直 0 条弹幕
            if (!string.IsNullOrEmpty((roomBox.Text ?? "").Trim()))
            {
                var auto = new Timer();
                auto.Interval = 800;
                auto.Tick += delegate { auto.Stop(); auto.Dispose(); ToggleBili(); };
                auto.Start();
            }
        }

        private void ToggleBili()
        {
            if (biliOn)
            {
                bili.Stop();
                biliOn = false;
                biliBtn.Text = "连接弹幕";
                biliLabel.Text = "弹幕：未连接";
                return;
            }
            string room = (roomBox.Text ?? "").Trim();
            if (room.Length == 0) { Say("先填直播间房号"); return; }
            biliOn = true;
                biliBtn.Text = "连接中…";
            Say("正在连接 B站直播间 " + room);
            bili.Start(room);
        }

        private void Section(string title, int x, int y)
        {
            Label l = new Label();
            l.Text = title;
            l.SetBounds(x, y, 700, 20);
            l.ForeColor = Color.FromArgb(150, 160, 178);
            Controls.Add(l);
        }

        private static void Style(TextBox t)
        {
            t.BackColor = Color.FromArgb(38, 41, 49);
            t.ForeColor = Color.White;
            t.BorderStyle = BorderStyle.FixedSingle;
        }

        private static Button Button2(string text, int x, int y, int w)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(x, y, w, 28);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(90, 100, 120);
            b.BackColor = Color.FromArgb(44, 48, 58);
            b.ForeColor = Color.Gainsboro;
            return b;
        }

        private void Say(string line)
        {
            log.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            while (log.Items.Count > 200) log.Items.RemoveAt(log.Items.Count - 1);
        }

        private string Url()
        {
            string u = urlBox.Text.Trim();
            if (u.Length == 0) u = DefaultUrl;
            return u;
        }

        private string StatusUrl()
        {
            string u = Url();
            int i = u.IndexOf("/danmaku", StringComparison.OrdinalIgnoreCase);
            string root = i > 0 ? u.Substring(0, i) : u;
            return root.TrimEnd('/') + "/status";
        }

        private void Poll()
        {
            try
            {
                string json = Get(StatusUrl(), 1500);
                if (json == null) { statusLabel.Text = "状态：游戏未运行 / 直播模式未开"; return; }
                string acc = Pick(json, "accepted");
                string drop = Pick(json, "dropped");
                string limit = Pick(json, "limit");
                string bal = Pick(json, "balance");
                string spent = Pick(json, "spent");
                statusLabel.Text = string.Format("状态：在线　已接入 {0} 条　丢弃 {1} 条　上限 {2}/分钟　余额 ¥{3}　本场消耗 ¥{4}",
                    acc ?? "?", drop ?? "?", limit ?? "?", bal ?? "?", spent ?? "?");
            }
            catch
            {
                statusLabel.Text = "状态：游戏未运行 / 直播模式未开";
            }
        }

        private static string Pick(string json, string key)
        {
            Match m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"?([-0-9.]+)\"?");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Get(string url, int timeoutMs)
        {
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = timeoutMs;
                req.ReadWriteTimeout = timeoutMs;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch { return null; }
        }

        private void SendDanmaku(string user, string text)
        {
            if (string.IsNullOrEmpty(text)) { Say("内容为空，没发。"); return; }
            try
            {
                string body = "{\"user\":" + Json(user) + ",\"text\":" + Json(text) + "}";
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url());
                req.Method = "POST";
                req.ContentType = "application/json; charset=utf-8";
                req.Timeout = 3000;
                req.ContentLength = bytes.Length;
                using (Stream s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string r = sr.ReadToEnd();
                    if (r.Contains("\"ok\":true")) Say("已发送 · " + user + "：" + text);
                    else Say("被游戏端拒绝（多半是超过每分钟上限）：" + r);
                }
            }
            catch (Exception e)
            {
                Say("发送失败：" + e.Message);
            }
        }

        private static string Json(string s)
        {
            if (s == null) return "\"\"";
            return "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }

        private string RoomOr(string fallback)
        {
            string r = (roomBox.Text ?? "").Trim();
            return r.Length > 0 ? r : fallback;
        }

        private void RefreshGuide()
        {
            string u = Url();
            string host = u;
            switch (platform.SelectedIndex)
            {
                case 0:
                    guide.Text =
"# B站：用 blivedm 监听弹幕，再转发给游戏\r\n" +
"pip install blivedm requests\r\n" +
"# 房间号换成你自己的，下面这段把弹幕 POST 过来\r\n" +
"import asyncio, blivedm, requests\r\n" +
"ROOM = " + RoomOr("12345") + "\r\n" +
"async def main():\r\n" +
"    c = blivedm.BLiveClient(ROOM)\r\n" +
"    c.set_handler(Handler())\r\n" +
"    await c.start()\r\n" +
"class Handler(blivedm.BaseHandler):\r\n" +
"    def _on_danmaku(self, client, msg):\r\n" +
"        requests.post(\"" + u + "\", json={\"user\": msg.uname, \"text\": msg.msg}, timeout=3)\r\n";
                    break;
                case 1:
                    guide.Text =
"# 抖音：网页端采集（DouyinLiveWebFetcher 之类），把拿到的 (昵称, 内容) 转发过来\r\n" +
"# 抖音没有公开的开放平台弹幕接口，需要自己带 cookie 采集\r\n" +
"import requests\r\n" +
"def push(user, text):\r\n" +
"    requests.post(\"" + u + "\", json={\"user\": user, \"text\": text}, timeout=3)\r\n";
                    break;
                case 2:
                    guide.Text =
"# OBS / Streamer.bot：用「执行 HTTP 请求」动作\r\n" +
"URL:    " + u + "\r\n" +
"Method: POST\r\n" +
"Body:   {\"user\":\"%user%\",\"text\":\"%message%\"}\r\n" +
"Content-Type: application/json\r\n";
                    break;
                case 3:
                    guide.Text =
"# 任意脚本：POST 一个 JSON 就行\r\n" +
"URL:  " + u + "\r\n" +
"Body: {\"user\":\"昵称\",\"text\":\"弹幕内容\"}\r\n\r\n" +
"curl -X POST \"" + u + "\" -H \"Content-Type: application/json\" -d \"{\\\"user\\\":\\\"a\\\",\\\"text\\\":\\\"b\\\"}\"\r\n\r\n" +
"# 查询状态：GET " + StatusUrl() + "\r\n" +
"# 每分钟超过上限的会被直接丢弃，本地手打不受限";
                    break;
                default:
                    guide.Text =
"# 手动测试：不用任何直播平台\r\n" +
"# 下面的「手动发送」按钮会直接 POST 到 " + u + "\r\n" +
"# 游戏里应当立刻出现一条训练员发言的弹幕文本";
                    break;
            }
        }
    }
}
