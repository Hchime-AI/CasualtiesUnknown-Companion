using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace CUStreamHub
{
    /// <summary>
    /// 极简 WebSocket 客户端（RFC6455，仅客户端侧，binary/text 收发 + ping/pong + close）。
    /// 为什么不用 ClientWebSocket：net48 里它禁止设置 User-Agent / Referer / Origin，
    /// 而 B 站弹幕服务器会因此直接断开连接。
    /// </summary>
    internal sealed class WsClient : IDisposable
    {
        private TcpClient tcp;
        private Stream stream;
        public bool IsOpen { get; private set; }
        public string LastClose = "";

        public void Connect(string host, int port, string path, string extraHeaders)
        {
            tcp = new TcpClient();
            tcp.Connect(host, port);
            tcp.NoDelay = true;
            tcp.ReceiveTimeout = 5000;   // 卡住时能让上层继续走心跳，而不是永远阻塞在读取上
            SslStream ssl = new SslStream(tcp.GetStream(), false, delegate { return true; });
            ssl.AuthenticateAsClient(host);
            stream = ssl;

            string key = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            var sb = new StringBuilder();
            sb.Append("GET ").Append(path).Append(" HTTP/1.1\r\n");
            sb.Append("Host: ").Append(host).Append(':').Append(port).Append("\r\n");
            sb.Append("Upgrade: websocket\r\n");
            sb.Append("Connection: Upgrade\r\n");
            sb.Append("Sec-WebSocket-Key: ").Append(key).Append("\r\n");
            sb.Append("Sec-WebSocket-Version: 13\r\n");
            if (!string.IsNullOrEmpty(extraHeaders)) sb.Append(extraHeaders);
            sb.Append("\r\n");
            byte[] req = Encoding.UTF8.GetBytes(sb.ToString());
            stream.Write(req, 0, req.Length);
            stream.Flush();

            // 读响应头
            var head = new StringBuilder();
            while (!head.ToString().EndsWith("\r\n\r\n"))
            {
                int b = stream.ReadByte();
                if (b < 0) throw new IOException("握手时连接被关闭");
                head.Append((char)b);
                if (head.Length > 8192) throw new IOException("握手响应过长");
            }
            string h = head.ToString();
            if (h.IndexOf(" 101 ", StringComparison.Ordinal) < 0)
                throw new IOException("握手失败：" + h.Split('\r')[0]);
            IsOpen = true;
        }

        public void Send(byte[] payload, int opcode)
        {
            if (!IsOpen) throw new IOException("连接未打开");
            using (var ms = new MemoryStream())
            {
                ms.WriteByte((byte)(0x80 | opcode));      // FIN + opcode
                int len = payload.Length;
                if (len < 126) ms.WriteByte((byte)(0x80 | len));
                else if (len <= 0xFFFF)
                {
                    ms.WriteByte(0x80 | 126);
                    ms.WriteByte((byte)(len >> 8));
                    ms.WriteByte((byte)(len & 0xFF));
                }
                else
                {
                    ms.WriteByte(0x80 | 127);
                    for (int i = 7; i >= 0; i--) ms.WriteByte((byte)((long)len >> (8 * i)));
                }
                byte[] mask = new byte[4];
                using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(mask);
                ms.Write(mask, 0, 4);
                for (int i = 0; i < len; i++) ms.WriteByte((byte)(payload[i] ^ mask[i & 3]));
                byte[] frame = ms.ToArray();
                stream.Write(frame, 0, frame.Length);
                stream.Flush();
            }
        }

        /// <summary>
        /// 收一条完整消息（自动拼 RFC6455 分片帧），返回负载；opcode 通过 out 返回。遇到 close 返回 null。
        /// 关键点：B 站服务器会把大包拆成多个 FIN=0 的分片，老实现把每个分片当独立数据塞进解包缓冲，
        /// 结果整条流被 3 字节的残片污染，读出来的 total 变成天文数字 → 永远拼不出包（表现为 0 个包）。
        /// ping/pong 在这里内部消化，不再返回给上层。
        /// </summary>
        public byte[] Receive(out int opcode)
        {
            opcode = 0;
            MemoryStream acc = null;
            int firstOp = 0;
            while (true)
            {
                int b0;
                try { b0 = stream.ReadByte(); }
                catch (IOException ex) { if (IsTimeout(ex)) { opcode = 3; return new byte[0]; } throw; }
                if (b0 < 0) { IsOpen = false; return null; }
                int b1;
                try { b1 = stream.ReadByte(); }
                catch (IOException ex) { if (IsTimeout(ex)) { opcode = 3; return new byte[0]; } throw; }
                if (b1 < 0) { IsOpen = false; return null; }

                bool fin = (b0 & 0x80) != 0;
                int op = b0 & 0x0F;
                bool masked = (b1 & 0x80) != 0;
                long len = b1 & 0x7F;
                if (len == 126) len = (ReadByteOrThrow() << 8) | ReadByteOrThrow();
                else if (len == 127)
                {
                    len = 0;
                    for (int i = 0; i < 8; i++) len = (len << 8) | (byte)ReadByteOrThrow();
                }
                byte[] mask = new byte[4];
                if (masked) for (int i = 0; i < 4; i++) mask[i] = (byte)ReadByteOrThrow();

                byte[] data = new byte[len];
                int got = 0;
                while (got < len)
                {
                    int n = stream.Read(data, got, (int)len - got);
                    if (n <= 0) { IsOpen = false; return null; }
                    got += n;
                }
                if (masked) for (int i = 0; i < len; i++) data[i] ^= mask[i & 3];

                if (op == 8)
                {
                    LastClose = "code=" + (data.Length >= 2 ? ((data[0] << 8) | data[1]).ToString() : "?") +
                                " reason=" + (data.Length > 2 ? Encoding.UTF8.GetString(data, 2, data.Length - 2) : "");
                    IsOpen = false;
                    return null;
                }
                if (op == 9) { try { Send(data, 10); } catch { } continue; }
                if (op == 10) continue;

                if (acc == null) { acc = new MemoryStream(); firstOp = (op == 0) ? 2 : op; }
                acc.Write(data, 0, data.Length);
                if (fin) { opcode = firstOp; return acc.ToArray(); }
            }
        }

        private static bool IsTimeout(Exception ex)
        {
            for (Exception e = ex; e != null; e = e.InnerException)
            {
                var se = e as SocketException;
                if (se != null && se.SocketErrorCode == SocketError.TimedOut) return true;
                if (e is TimeoutException) return true;
            }
            return false;
        }

        private int ReadByteOrThrow()
        {
            int b = stream.ReadByte();
            if (b < 0) throw new IOException("连接已关闭");
            return b;
        }

        public void Close()
        {
            try { if (IsOpen) Send(new byte[0], 8); } catch { }
            IsOpen = false;
            try { if (stream != null) stream.Dispose(); } catch { }
            try { if (tcp != null) tcp.Close(); } catch { }
        }

        public void Dispose() { Close(); }
    }
}
