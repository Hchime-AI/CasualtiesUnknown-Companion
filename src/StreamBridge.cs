using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;

namespace CUCompanion;

public class StreamBridge : MonoBehaviour
{
	public static StreamBridge Instance;

	private TcpListener listener;

	private float retryAt;

	private Thread acceptThread;

	private volatile bool running;

	private readonly Queue<Action> mainQueue = new Queue<Action>();

	private readonly object qLock = new object();

	private readonly List<float> danmakuStamps = new List<float>();

	private readonly List<float> displayStamps = new List<float>();

	private string lastText = "";

	private float lastTextAt = -99f;

	private float lastDropLog = -99f;

	private Process hub;

	private static bool hubTried;

	private decimal balanceTotal = -1m;

	private string balanceCurrency = "CNY";

	private string balanceNote = Loc.T("未查询");

	private decimal sessionSpent;

	private float nextBalanceAt;

	private bool querying;

	private static long promptTokens;

	private static long completionTokens;

	private RectTransform panel;

	private TextMeshProUGUI lineBalance;

	private TextMeshProUGUI lineSpent;

	private TextMeshProUGUI lineNote;

	public int Accepted { get; private set; }

	public int Dropped { get; private set; }

	private void Awake()
	{
		Instance = this;
		Llm.OnUsage = OnUsage;
	}

	private void OnDestroy()
	{
		Llm.OnUsage = null;
		StopServer();
		KillHub();
	}

	private void Update()
	{
		bool flag = CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.streamMode;
		if (flag && !running)
		{
			StartServer();
		}
		if (!flag && running)
		{
			StopServer();
		}
		if (flag && !hubTried)
		{
			hubTried = true;
			LaunchHub();
		}
		if (!flag)
		{
			hubTried = false;
		}
		lock (qLock)
		{
			int num = 0;
			while (mainQueue.Count > 0 && num++ < 20)
			{
				Action action = mainQueue.Dequeue();
				try
				{
					action();
				}
				catch (Exception ex)
				{
					CompanionPlugin.Log.LogWarning((object)("[stream] " + ex.Message));
				}
			}
		}
		EnsurePanel();
		if (flag)
		{
			RefreshPanel();
		}
		if ((Object)(object)panel != (Object)null && ((Component)panel).gameObject.activeSelf != flag)
		{
			((Component)panel).gameObject.SetActive(flag);
		}
		if (!running && flag && Time.unscaledTime >= retryAt)
		{
			StartServer();
		}
		TickBalanceQuery();
	}

	private void StartServer()
	{
		int num = ((CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.streamPort > 0) ? CompanionPlugin.Cfg.streamPort : 8768);
		for (int i = 0; i < 6; i++)
		{
			if (running)
			{
				break;
			}
			try
			{
				int num2 = num + i;
				listener = new TcpListener(IPAddress.Loopback, num2);
				listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optionValue: true);
				listener.Start();
				num = num2;
				running = true;
				acceptThread = new Thread(AcceptLoop)
				{
					IsBackground = true
				};
				acceptThread.Start();
				CompanionPlugin.Log.LogInfo((object)("[stream] 弹幕端口已开启 127.0.0.1:" + num));
			}
			catch (Exception ex)
			{
				running = false;
				retryAt = Time.unscaledTime + 5f;
				CompanionPlugin.Log.LogWarning((object)("[stream] 端口开启失败（5 秒后重试）：" + ex.Message));
			}
		}
	}

	private void StopServer()
	{
		running = false;
		try
		{
			if (listener != null)
			{
				listener.Stop();
			}
		}
		catch
		{
		}
		listener = null;
		danmakuStamps.Clear();
	}

	private void AcceptLoop()
	{
		while (running)
		{
			TcpClient tcpClient = null;
			try
			{
				tcpClient = listener.AcceptTcpClient();
			}
			catch
			{
				break;
			}
			try
			{
				HandleClient(tcpClient);
			}
			catch (Exception ex)
			{
				CompanionPlugin.Log.LogWarning((object)("[stream] " + ex.Message));
			}
			finally
			{
				try
				{
					tcpClient.Close();
				}
				catch
				{
				}
			}
		}
	}

	private void HandleClient(TcpClient c)
	{
		c.ReceiveTimeout = 4000;
		NetworkStream stream = c.GetStream();
		stream.ReadTimeout = 4000;
		MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[4096];
		int num = -1;
		while (memoryStream.Length < 262144)
		{
			int num2 = stream.Read(array, 0, array.Length);
			if (num2 <= 0)
			{
				break;
			}
			memoryStream.Write(array, 0, num2);
			num = IndexOf(memoryStream.GetBuffer(), (int)memoryStream.Length, new byte[4] { 13, 10, 13, 10 });
			if (num >= 0)
			{
				break;
			}
		}
		byte[] array2 = memoryStream.ToArray();
		if (num < 0)
		{
			Respond(stream, 400, "{\"ok\":false,\"err\":\"bad request\"}");
			return;
		}
		string[] array3 = Encoding.UTF8.GetString(array2, 0, num).Split('\n');
		string[] array4 = ((array3.Length != 0) ? array3[0].Trim() : "").Split(' ');
		string text = ((array4.Length != 0) ? array4[0] : "GET");
		string text2 = ((array4.Length > 1) ? array4[1] : "/");
		int result = 0;
		for (int i = 1; i < array3.Length; i++)
		{
			string text3 = array3[i].Trim();
			int num3 = text3.IndexOf(':');
			if (num3 > 0 && text3.Substring(0, num3).Trim().ToLower() == "content-length")
			{
				int.TryParse(text3.Substring(num3 + 1).Trim(), out result);
			}
		}
		int num4 = num + 4;
		for (int num5 = array2.Length - num4; num5 < result; num5 = array2.Length - num4)
		{
			int num6 = stream.Read(array, 0, array.Length);
			if (num6 <= 0)
			{
				break;
			}
			byte[] array5 = new byte[array2.Length + num6];
			Buffer.BlockCopy(array2, 0, array5, 0, array2.Length);
			Buffer.BlockCopy(array, 0, array5, array2.Length, num6);
			array2 = array5;
		}
		string text4 = ((result > 0 && array2.Length >= num4) ? Encoding.UTF8.GetString(array2, num4, Math.Min(result, array2.Length - num4)) : "");
		if (text == "GET" && (text2 == "/status" || text2 == "/ping"))
		{
			Respond(stream, 200, StatusJson());
		}
		else if (text == "POST" && text2.StartsWith("/danmaku"))
		{
			string user = Loc.T("观众");
			string text5 = "";
			try
			{
				JObject val = JObject.Parse(text4);
				if (val["user"] != null)
				{
					user = ((object)val["user"]).ToString();
				}
				if (val["text"] != null)
				{
					text5 = ((object)val["text"]).ToString();
				}
			}
			catch
			{
				text5 = text4;
			}
			text5 = (text5 ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (text5.Length == 0)
			{
				Respond(stream, 200, "{\"ok\":false,\"err\":\"empty\"}");
				return;
			}
			bool flag = AcceptDanmaku(user, text5);
			Respond(stream, 200, flag ? "{\"ok\":true}" : "{\"ok\":false,\"err\":\"rate\"}");
		}
		else
		{
			Respond(stream, 404, "{\"ok\":false}");
		}
	}

	private static int IndexOf(byte[] hay, int len, byte[] needle)
	{
		for (int i = 0; i + needle.Length <= len; i++)
		{
			bool flag = true;
			for (int j = 0; j < needle.Length; j++)
			{
				if (hay[i + j] != needle[j])
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				return i;
			}
		}
		return -1;
	}

	private static void Respond(NetworkStream ns, int code, string json)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		string s = "HTTP/1.1 " + code + " OK\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: " + bytes.Length + "\r\nConnection: close\r\n\r\n";
		byte[] bytes2 = Encoding.ASCII.GetBytes(s);
		ns.Write(bytes2, 0, bytes2.Length);
		ns.Write(bytes, 0, bytes.Length);
		ns.Flush();
	}

	private string StatusJson()
	{
		return "{\"ok\":true,\"accepted\":" + Accepted + ",\"dropped\":" + Dropped + ",\"limit\":" + PerMin() + ",\"balance\":\"" + balanceTotal.ToString("0.00", CultureInfo.InvariantCulture) + "\",\"spent\":\"" + sessionSpent.ToString("0.0000", CultureInfo.InvariantCulture) + "\"}";
	}

	private static int PerMin()
	{
		int num = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.danmakuPerMin : 3);
		if (num > 0)
		{
			return num;
		}
		return 3;
	}

	private static float WindowOf(float value, float fallback)
	{
		if (value <= 0f)
		{
			value = fallback;
		}
		return Mathf.Clamp(value, 0.1f, 3600f);
	}

	private static float DisplayWindow()
	{
		return WindowOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.danmakuDisplayWindowSec : 0f, 1f);
	}

	private static float DedupSec()
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		if (cfg == null)
		{
			return 30f;
		}
		return Mathf.Clamp(cfg.danmakuDedupSec, 0f, 3600f);
	}

	private static float ResponseWindow()
	{
		return WindowOf((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.danmakuResponseWindowSec : 0f, 60f);
	}

	private bool AcceptDanmaku(string user, string text)
	{
		ModConfig cfg = CompanionPlugin.Cfg;
		float unscaledTime = Time.unscaledTime;
		user = CompanionHost.CleanDanmaku(user, 20);
		text = CompanionHost.CleanDanmaku(text, 160);
		if (text.Length == 0)
		{
			Dropped++;
			return false;
		}
		if (user.Length == 0)
		{
			user = Loc.T("观众");
		}
		int num = cfg?.danmakuMaxPerSec ?? 5;
		if (num <= 0)
		{
			num = 5;
		}
		for (int num2 = displayStamps.Count - 1; num2 >= 0; num2--)
		{
			if (unscaledTime - displayStamps[num2] > DisplayWindow())
			{
				displayStamps.RemoveAt(num2);
			}
		}
		if ((text == lastText && unscaledTime - lastTextAt < DedupSec()) || displayStamps.Count >= num)
		{
			Dropped++;
			if (unscaledTime - lastDropLog > 10f)
			{
				lastDropLog = unscaledTime;
				CompanionPlugin.Log.LogInfo((object)("[stream] 弹幕太密，已丢弃 " + Dropped + " 条（显示上限 " + num + " 条/秒）"));
			}
			return false;
		}
		displayStamps.Add(unscaledTime);
		lastText = text;
		lastTextAt = unscaledTime;
		bool toAi = false;
		if (cfg == null || cfg.danmakuToAi)
		{
			for (int num3 = danmakuStamps.Count - 1; num3 >= 0; num3--)
			{
				if (unscaledTime - danmakuStamps[num3] > ResponseWindow())
				{
					danmakuStamps.RemoveAt(num3);
				}
			}
			if (danmakuStamps.Count < PerMin())
			{
				danmakuStamps.Add(unscaledTime);
				toAi = true;
			}
		}
		Accepted++;
		lock (qLock)
		{
			mainQueue.Enqueue(delegate
			{
				CompanionHost instance = CompanionHost.Instance;
				if (!((Object)(object)instance == (Object)null))
				{
					instance.SubmitDanmaku(user, text, toAi);
				}
			});
		}
		if (toAi || (cfg != null && cfg.debug))
		{
			CompanionPlugin.Log.LogInfo((object)("[stream] 弹幕转入" + (toAi ? "（并交给角色回应）" : "（仅显示）") + "：" + user + "：" + text));
		}
		return true;
	}

	private static void OnUsage(int prompt, int completion)
	{
		promptTokens += prompt;
		completionTokens += completion;
	}

	private decimal CostIn()
	{
		decimal num = ((CompanionPlugin.Cfg != null) ? ((decimal)CompanionPlugin.Cfg.priceInPerM) : 2m);
		return (decimal)promptTokens / 1000000m * num;
	}

	private decimal CostOut()
	{
		decimal num = ((CompanionPlugin.Cfg != null) ? ((decimal)CompanionPlugin.Cfg.priceOutPerM) : 8m);
		return (decimal)completionTokens / 1000000m * num;
	}

	private void TickBalanceQuery()
	{
		if (CompanionPlugin.Cfg != null && CompanionPlugin.Cfg.streamMode && !querying && !(Time.unscaledTime < nextBalanceAt))
		{
			nextBalanceAt = Time.unscaledTime + Mathf.Max(30f, CompanionPlugin.Cfg.balanceRefreshSec);
			QueryBalance();
		}
	}

	private async void QueryBalance()
	{
		ApiSettings api = CompanionPlugin.Cfg.api;
		if (api == null || string.IsNullOrEmpty(api.apiKey))
		{
			balanceNote = Loc.T("未配置 API Key");
			return;
		}
		string text = BalanceUrl(api);
		if (text == null)
		{
			balanceNote = Loc.T("该接口不提供余额查询");
			return;
		}
		querying = true;
		try
		{
			HttpClient hc = new HttpClient();
			try
			{
				hc.Timeout = TimeSpan.FromSeconds(15.0);
				HttpRequestMessage val = new HttpRequestMessage(HttpMethod.Get, text);
				((HttpHeaders)val.Headers).TryAddWithoutValidation("Authorization", "Bearer " + api.apiKey);
				((HttpHeaders)val.Headers).TryAddWithoutValidation("Accept", "application/json");
				HttpResponseMessage resp = await hc.SendAsync(val);
				string text2 = await resp.Content.ReadAsStringAsync();
				if (!resp.IsSuccessStatusCode)
				{
					balanceNote = Loc.T("查询失败 HTTP ") + (int)resp.StatusCode;
					return;
				}
				JToken val2 = JObject.Parse(text2)["balance_infos"];
				if (val2 != null && val2.HasValues)
				{
					JToken val3 = val2[(object)0];
					balanceCurrency = ((val3[(object)"currency"] != null) ? ((object)val3[(object)"currency"]).ToString() : "CNY");
					if (decimal.TryParse((val3[(object)"total_balance"] != null) ? ((object)val3[(object)"total_balance"]).ToString() : "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
					{
						balanceTotal = result;
					}
					balanceNote = Loc.T("已刷新 ") + DateTime.Now.ToString("HH:mm:ss");
				}
				else
				{
					balanceNote = Loc.T("返回里没有余额字段");
				}
			}
			finally
			{
				((IDisposable)hc)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			balanceNote = Loc.T("查询异常：") + ex.Message;
		}
		finally
		{
			querying = false;
		}
	}

	private static string BalanceUrl(ApiSettings s)
	{
		if (s == null || string.IsNullOrEmpty(s.baseUrl))
		{
			return null;
		}
		string text = s.baseUrl.ToLowerInvariant();
		if (text.Contains("deepseek"))
		{
			return "https://api.deepseek.com/user/balance";
		}
		if (text.Contains("moonshot"))
		{
			return "https://api.moonshot.cn/v1/users/me/balance";
		}
		return null;
	}

	private void EnsurePanel()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)panel != (Object)null) && CompanionPlugin.Cfg != null)
		{
			GameObject val = new GameObject("CUStreamBalance", new Type[3]
			{
				typeof(RectTransform),
				typeof(CanvasRenderer),
				typeof(Image)
			});
			val.transform.SetParent(NativeHudController.CanvasTransform, false);
			panel = val.GetComponent<RectTransform>();
			panel.anchorMin = new Vector2(1f, 0f);
			panel.anchorMax = new Vector2(1f, 0f);
			panel.pivot = new Vector2(1f, 0f);
			panel.anchoredPosition = new Vector2(-24f, 112f);
			panel.sizeDelta = new Vector2(300f, 116f);
			Image component = val.GetComponent<Image>();
			((Graphic)component).color = new Color(0.045f, 0.05f, 0.062f, 0.92f);
			((Graphic)component).raycastTarget = false;
			Line(panel, Loc.T("API 余额"), 15f, -10f, new Color(0.62f, 0.75f, 0.95f));
			lineBalance = Line(panel, "¥ --", 26f, -32f, Color.white);
			lineSpent = Line(panel, Loc.T("本场消耗 ¥0.0000"), 14f, -66f, new Color(0.8f, 0.8f, 0.8f));
			lineNote = Line(panel, Loc.T("等待查询"), 12f, -88f, new Color(0.55f, 0.6f, 0.68f));
			Frame(panel);
		}
	}

	private static void Frame(RectTransform rt)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		float x = rt.sizeDelta.x;
		float y = rt.sizeDelta.y;
		Box(rt, 0f, 0f, x, 1.6f);
		Box(rt, 0f, 0f - y + 1.6f, x, 1.6f);
		Box(rt, 0f, 0f, 1.6f, y);
		Box(rt, x - 1.6f, 0f, 1.6f, y);
	}

	private static void Box(RectTransform parent, float x, float y, float w, float h)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("l", new Type[3]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer),
			typeof(Image)
		});
		val.transform.SetParent((Transform)(object)parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		Vector2 val2 = default(Vector2);
		val2 = new Vector2(0f, 1f);
		component.anchorMax = val2;
		component.anchorMin = val2;
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = new Vector2(x, y);
		component.sizeDelta = new Vector2(w, h);
		Image component2 = val.GetComponent<Image>();
		((Graphic)component2).color = new Color(1f, 1f, 1f, 0.85f);
		((Graphic)component2).raycastTarget = false;
	}

	private static TextMeshProUGUI Line(RectTransform parent, string text, float size, float y, Color color)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = new GameObject("t", new Type[2]
		{
			typeof(RectTransform),
			typeof(CanvasRenderer)
		});
		val.transform.SetParent((Transform)(object)parent, false);
		RectTransform component = val.GetComponent<RectTransform>();
		Vector2 val2 = default(Vector2);
		val2 = new Vector2(0f, 1f);
		component.anchorMax = val2;
		component.anchorMin = val2;
		component.pivot = new Vector2(0f, 1f);
		component.anchoredPosition = new Vector2(14f, y);
		component.sizeDelta = new Vector2(272f, size + 10f);
		TextMeshProUGUI val3 = val.AddComponent<TextMeshProUGUI>();
		((TMP_Text)val3).text = text;
		((TMP_Text)val3).fontSize = size;
		((Graphic)val3).color = color;
		((TMP_Text)val3).alignment = (TextAlignmentOptions)257;
		((Graphic)val3).raycastTarget = false;
		((TMP_Text)val3).enableWordWrapping = false;
		if ((Object)(object)NativeHudController.GameFont != (Object)null)
		{
			((TMP_Text)val3).font = NativeHudController.GameFont;
		}
		return val3;
	}

	private void RefreshPanel()
	{
		if (!((Object)(object)lineBalance == (Object)null))
		{
			if (balanceTotal < 0m)
			{
				((TMP_Text)lineBalance).text = "¥ --";
			}
			else
			{
				((TMP_Text)lineBalance).text = Symbol() + balanceTotal.ToString("0.00", CultureInfo.InvariantCulture);
			}
			decimal num = (sessionSpent = CostIn() + CostOut());
			if ((Object)(object)lineSpent != (Object)null)
			{
				((TMP_Text)lineSpent).text = Loc.T("本场消耗 ¥") + num.ToString("0.0000", CultureInfo.InvariantCulture);
			}
			if ((Object)(object)lineNote != (Object)null)
			{
				decimal num2 = ((balanceTotal < 0m) ? (-1m) : (balanceTotal - num));
				string text = ((num2 < 0m) ? "" : (Loc.T("\u3000余 ¥") + num2.ToString("0.00", CultureInfo.InvariantCulture)));
				((TMP_Text)lineNote).text = balanceNote + text;
			}
		}
	}

	private static string Symbol()
	{
		if (CompanionPlugin.Cfg == null || CompanionPlugin.Cfg.currencySymbol == null)
		{
			return "¥";
		}
		return CompanionPlugin.Cfg.currencySymbol;
	}

	private void LaunchHub()
	{
		try
		{
			string text = ((CompanionPlugin.Cfg != null) ? CompanionPlugin.Cfg.streamHubPath : null);
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				string assmDir = Path.GetDirectoryName(typeof(StreamBridge).Assembly.Location);
				string c1 = Path.Combine(assmDir ?? ".", "CUStreamHub.exe");
				string c2 = Path.Combine(BepInEx.Paths.PluginPath ?? ".", "CUCompanion", "CUStreamHub.exe");
				string c3 = Path.Combine(BepInEx.Paths.GameRootPath ?? ".", "CUStreamHub.exe");
				if (File.Exists(c1)) text = c1;
				else if (File.Exists(c2)) text = c2;
				else if (File.Exists(c3)) text = c3;
			}
			CompanionPlugin.Log.LogInfo((object)("[stream] 控制台路径 = " + (text ?? "(空)")));
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				CompanionPlugin.Log.LogInfo((object)"[stream] 未找到直播控制台程序（streamHubPath），只开本地弹幕端口");
				return;
			}
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text);
			processStartInfo.UseShellExecute = false;
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(text);
			hub = Process.Start(processStartInfo);
			CompanionPlugin.Log.LogInfo((object)("[stream] 已启动直播控制台：" + text));
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log.LogWarning((object)("[stream] 启动控制台失败：" + ex.Message));
		}
	}

	private void KillHub()
	{
		try
		{
			if (hub != null && !hub.HasExited)
			{
				hub.Kill();
			}
		}
		catch
		{
		}
		hub = null;
	}
}
