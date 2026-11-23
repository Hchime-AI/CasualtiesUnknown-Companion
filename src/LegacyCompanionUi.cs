using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

public class LegacyCompanionUi : MonoBehaviour
{
	private static readonly Color BgColor = new Color(0.045f, 0.05f, 0.062f, 0.9f);

	private static readonly Color Accent = new Color(0.6f, 0.8f, 1f, 1f);

	private static readonly Color Dim = new Color(0.72f, 0.75f, 0.8f, 1f);

	private static readonly Color TextCol = new Color(0.94f, 0.95f, 0.97f, 1f);

	private static Font _font;

	private static Texture2D _px;

	private static bool _stylesReady;

	private static GUIStyle _header;

	private static GUIStyle _label;

	private static GUIStyle _small;

	private static GUIStyle _btn;

	private static GUIStyle _btnOn;

	private static GUIStyle _field;

	private static GUIStyle _area;

	private static GUIStyle _chip;

	private static GUIStyle _title;

	private static GUIStyle _histText;

	public static LegacyCompanionUi Instance;

	private bool settingsOpen;

	private bool personaOpen;

	private string apiTestMsg = "";

	private float apiTestMsgTime;

	private string inputText = "";

	private Vector2 histScroll;

	private Vector2 personaScroll;

	private Vector2 settingsScroll;

	private int lastHistCount;

	private float preRunScanTime;

	private PreRunScript preRun;

	private Rect inputRect = new Rect(-1f, -1f, 360f, 32f);

	private bool dragging;

	private Vector2 dragOffset;

	private static Font CjkFont
	{
		get
		{
			if ((Object)(object)_font == (Object)null)
			{
				try
				{
					_font = Font.CreateDynamicFontFromOSFont(new string[5] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "Microsoft YaHei UI", "Arial Unicode MS" }, 15);
				}
				catch
				{
				}
				if ((Object)(object)_font == (Object)null && (Object)(object)GUI.skin != (Object)null)
				{
					_font = GUI.skin.font;
				}
			}
			return _font;
		}
	}

	private static Texture2D Px
	{
		get
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_px == (Object)null)
			{
				_px = new Texture2D(1, 1);
				_px.SetPixel(0, 0, Color.white);
				_px.Apply();
				((Object)_px).hideFlags = (HideFlags)61;
			}
			return _px;
		}
	}

	private void Awake()
	{
		Instance = this;
	}

	public void SetInputText(string s)
	{
		inputText = s;
		if (CompanionPlugin.Cfg != null)
		{
			CompanionPlugin.Cfg.inputOpen = true;
		}
	}

	public static void OpenPersona()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			if ((Object)(object)SettingsEditorGui.Instance != (Object)null)
			{
				SettingsEditorGui.Instance.CloseMenu();
			}
			Instance.personaOpen = true;
		}
	}

	public static void CloseWindows()
	{
		if ((Object)(object)Instance != (Object)null)
		{
			Instance.settingsOpen = false;
		}
	}

	private void InitStyles()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Expected O, but got Unknown
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Expected O, but got Unknown
		//IL_01d0: Expected O, but got Unknown
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Expected O, but got Unknown
		//IL_021d: Expected O, but got Unknown
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Expected O, but got Unknown
		//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
		if (!_stylesReady)
		{
			Font cjkFont = CjkFont;
			if ((Object)(object)cjkFont != (Object)null && (Object)(object)GUI.skin != (Object)null)
			{
				GUI.skin.font = cjkFont;
			}
			_header = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 13,
				alignment = (TextAnchor)3,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_header.normal.textColor = Accent;
			_label = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 13,
				wordWrap = true,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_label.normal.textColor = TextCol;
			_small = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 11,
				wordWrap = true,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_small.normal.textColor = Dim;
			_title = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 15,
				alignment = (TextAnchor)3,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_title.normal.textColor = TextCol;
			_btn = new GUIStyle(GUI.skin.button)
			{
				font = cjkFont,
				fontSize = 12,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(6, 6, 2, 2)
			};
			_btn.normal.textColor = Dim;
			_btn.hover.textColor = TextCol;
			_btnOn = new GUIStyle(_btn)
			{
				fontStyle = (FontStyle)1,
				clipping = TextClipping.Overflow
			};
			_btnOn.normal.textColor = Accent;
			_field = new GUIStyle(GUI.skin.textField)
			{
				font = cjkFont,
				fontSize = 13,
				padding = new RectOffset(6, 6, 4, 4),
				clipping = TextClipping.Overflow
			};
			_field.normal.textColor = TextCol;
			_area = new GUIStyle(GUI.skin.textArea)
			{
				font = cjkFont,
				fontSize = 13,
				wordWrap = true,
				padding = new RectOffset(6, 6, 4, 4),
				clipping = TextClipping.Overflow
			};
			_area.normal.textColor = TextCol;
			_chip = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 11,
				wordWrap = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_chip.normal.textColor = new Color(0.62f, 0.66f, 0.72f);
			_histText = new GUIStyle(GUI.skin.label)
			{
				font = cjkFont,
				fontSize = 13,
				wordWrap = true,
				richText = true,
				clipping = TextClipping.Overflow,
				padding = new RectOffset(2, 2, 0, 2)
			};
			_histText.normal.textColor = TextCol;
			_stylesReady = true;
		}
	}

	private static void Fill(Rect r, Color c)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		Color color = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, (Texture)(object)Px);
		GUI.color = color;
	}

	private void OnGUI()
	{
		UiScale.Begin();
		try
		{
			DrawGui();
		}
		finally
		{
			UiScale.End();
		}
	}

	private void DrawGui()
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		CompanionHost instance = CompanionHost.Instance;
		if ((Object)(object)instance == (Object)null || CompanionPlugin.Cfg == null || ((Object)(object)NativeHudController.Instance != (Object)null && instance.HasBody))
		{
			MenuBlocker.Require("legacy", false);
			MenuBlocker.Sync();
			return;
		}
		InitStyles();
		ModConfig cfg = CompanionPlugin.Cfg;
		if (settingsOpen || personaOpen)
		{
			MenuBlocker.Paint();
		}
		ScanPreRun();
		HandleHotkeys(cfg, instance);
		if (instance.HasBody)
		{
			if (cfg.historyOpen)
			{
				DrawHistory(instance, cfg);
			}
			else
			{
				DrawMinimizedTab(instance, cfg);
			}
		}
		if (cfg.inputOpen && instance.HasBody)
		{
			DrawInputBox(instance, cfg);
		}
		if (settingsOpen)
		{
			DrawSettings(cfg);
		}
		if (personaOpen)
		{
			DrawPersona(cfg);
		}
		MenuBlocker.Require("legacy", settingsOpen || personaOpen);
		MenuBlocker.Sync();
		if (Time.unscaledTime - apiTestMsgTime < 6f && !string.IsNullOrEmpty(apiTestMsg))
		{
			Fill(new Rect((float)UiScale.SW * 0.5f - 220f, 14f, 440f, 28f), new Color(0f, 0f, 0f, 0.85f));
			GUI.Label(new Rect((float)UiScale.SW * 0.5f - 212f, 20f, 424f, 20f), apiTestMsg, _small);
		}
		if (cfg.debug)
		{
			string text = "screen " + UiScale.SW + "x" + UiScale.SH + "  body:" + (instance.HasBody ? Loc.T("有") : Loc.T("无")) + Loc.T("  历史框:") + (cfg.historyOpen ? Loc.T("开") : Loc.T("关")) + "  preRun:" + (((Object)(object)preRun != (Object)null) ? Loc.T("有") : Loc.T("无")) + "  talker:" + (instance.HasTalker ? Loc.T("有") : Loc.T("无"));
			Fill(new Rect(4f, 4f, 460f, 18f), new Color(0f, 0f, 0f, 0.6f));
			GUI.Label(new Rect(8f, 5f, 452f, 16f), text, _chip);
		}
		if (instance.busy)
		{
			Fill(new Rect((float)UiScale.SW - 132f, (float)UiScale.SH - 60f, 116f, 22f), new Color(0f, 0f, 0f, 0.6f));
			GUI.Label(new Rect((float)UiScale.SW - 126f, (float)UiScale.SH - 58f, 110f, 18f), Loc.T("同伴思考中…"), _small);
		}
	}

	private void HandleHotkeys(ModConfig cfg, CompanionHost host)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I4
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Invalid comparison between Unknown and I4
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Invalid comparison between Unknown and I4
		Event current = Event.current;
		if (current != null && (int)current.type == 4 && string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
		{
			if ((int)current.keyCode == 116)
			{
				cfg.inputOpen = !cfg.inputOpen;
				current.Use();
			}
			else if ((int)current.keyCode == 289)
			{
				cfg.historyOpen = !cfg.historyOpen;
				current.Use();
			}
			else if ((int)current.keyCode == 290)
			{
				personaOpen = !personaOpen;
				if (personaOpen && (Object)(object)SettingsEditorGui.Instance != (Object)null)
				{
					SettingsEditorGui.Instance.CloseMenu();
				}
				current.Use();
			}
			else if ((int)current.keyCode == 291)
			{
				if ((Object)(object)SettingsEditorGui.Instance != (Object)null)
				{
					SettingsEditorGui.Instance.OpenMenu();
				}
				else
				{
					settingsOpen = !settingsOpen;
				}
				current.Use();
			}
		}
	}

	private void ScanPreRun()
	{
		if (Time.unscaledTime < preRunScanTime)
		{
			return;
		}
		preRunScanTime = Time.unscaledTime + 2f;
		try
		{
			preRun = Object.FindObjectOfType<PreRunScript>();
		}
		catch
		{
			preRun = null;
		}
	}

	private void DrawPreRunButton()
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		Rect val = default(Rect);
		val = new Rect((float)UiScale.SW - 210f, (float)UiScale.SH - 96f, 196f, 32f);
		val.x = Mathf.Clamp(val.x, 8f, Mathf.Max(8f, (float)UiScale.SW - val.width - 8f));
		if (GUI.Button(val, Loc.T("人设 / 互动设置"), _btn))
		{
			personaOpen = true;
			GUI.FocusControl((string)null);
		}
		GUI.Label(new Rect(((Rect)val).x + 2f, ((Rect)val).y - 20f, 200f, 18f), Loc.T("为当前选中的角色编写"), _chip);
	}

	private void DrawHistory(CompanionHost host, ModConfig cfg)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0368: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		Rect r = default(Rect);
		r = new Rect(cfg.histX, cfg.histY, cfg.histW, cfg.histH);
		Fill(r, BgColor);
		UiPointerGuard.Mark(r);
		Fill(new Rect(((Rect)r).x, ((Rect)r).y, ((Rect)r).width, 1f), new Color(0.35f, 0.45f, 0.6f, 0.55f));
		Fill(new Rect(((Rect)r).x, ((Rect)r).y, 1f, ((Rect)r).height), new Color(0.35f, 0.45f, 0.6f, 0.35f));
		Rect val = default(Rect);
		val = new Rect(((Rect)(r)).x + 6f, ((Rect)(r)).y + 5f, ((Rect)(r)).width - 12f, 24f);
		Rect zone = default(Rect);
		zone = new Rect(((Rect)(r)).x, ((Rect)(r)).y, ((Rect)(r)).width - 150f, 28f);
		DoDrag(ref cfg.histX, ref cfg.histY, zone, cfg);
		float x = ((Rect)val).x;
		if (GUI.Button(new Rect(x, ((Rect)val).y, 54f, 22f), "API", _btn))
		{
			if ((Object)(object)SettingsEditorGui.Instance != (Object)null)
			{
				SettingsEditorGui.Instance.OpenMenu();
			}
			else
			{
				settingsOpen = !settingsOpen;
			}
			GUI.FocusControl((string)null);
		}
		float num = x + 58f;
		if (GUI.Button(new Rect(num, ((Rect)val).y, 54f, 22f), Loc.T("输入"), cfg.inputOpen ? _btnOn : _btn))
		{
			cfg.inputOpen = !cfg.inputOpen;
			GUI.FocusControl((string)null);
		}
		float num2 = num + 58f;
		if (GUI.Button(new Rect(num2, ((Rect)val).y, 54f, 22f), Loc.T("人设"), personaOpen ? _btnOn : _btn))
		{
			personaOpen = !personaOpen;
			GUI.FocusControl((string)null);
		}
		GUI.Label(new Rect(num2 + 60f, ((Rect)val).y, 160f, 22f), Loc.T("反应 ") + host.BudgetUsed() + "/" + cfg.reactionsPerWindow, _small);
		if (GUI.Button(new Rect(((Rect)r).xMax - 30f, ((Rect)val).y, 22f, 22f), "×", _btn))
		{
			cfg.historyOpen = false;
			CompanionPlugin.SaveAll();
		}
		if (GUI.Button(new Rect(((Rect)r).xMax - 54f, ((Rect)val).y, 22f, 22f), "—", _btn))
		{
			cfg.historyOpen = false;
			CompanionPlugin.SaveAll();
		}
		float num3 = ((Rect)val).yMax + 2f;
		Persona persona = host.CurrentPersona();
		GUI.Label(new Rect(((Rect)r).x + 10f, num3, ((Rect)r).width - 20f, 18f), HostName(host) + " · <color=#7f8b9a>" + persona.GenderText() + "</color>", _header);
		num3 += 20f;
		GUILayout.BeginArea(new Rect(((Rect)r).x + 8f, num3, ((Rect)r).width - 16f, ((Rect)r).yMax - num3 - 8f));
		histScroll = GUILayout.BeginScrollView(histScroll, false, false, Array.Empty<GUILayoutOption>());
		if (host.history.Count != lastHistCount)
		{
			lastHistCount = host.history.Count;
			histScroll.y = 100000f;
		}
		foreach (Line item in host.history)
		{
			if (item.kind == 1)
			{
				GUILayout.Label("<color=#6FC3FF>" + item.who + "</color>\u3000<color=#CFE4F5>" + item.text + "</color>", _histText, Array.Empty<GUILayoutOption>());
			}
			else if (item.player)
			{
				GUILayout.Label(Loc.T("<color=#8fb7d9>我</color>\u3000") + item.text, _histText, Array.Empty<GUILayoutOption>());
			}
			else
			{
				GUILayout.Label("<color=#d9c48f>" + item.who + "</color>\u3000" + item.text, _histText, Array.Empty<GUILayoutOption>());
			}
			GUILayout.Space(2f);
		}
		if (host.history.Count == 0)
		{
			GUILayout.Label(Loc.T("（还没有对话。左上角「输入」或按 T 可以说点什么。）"), _chip, Array.Empty<GUILayoutOption>());
		}
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	private void DoDrag(ref float px, ref float py, Rect zone, ModConfig cfg)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Invalid comparison between Unknown and I4
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		Event current = Event.current;
		if (current != null)
		{
			if ((int)current.type == 0 && current.button == 0 && ((Rect)zone).Contains(current.mousePosition))
			{
				dragging = true;
				dragOffset = current.mousePosition - new Vector2(px, py);
				current.Use();
			}
			else if (dragging && (int)current.type == 3)
			{
				px = current.mousePosition.x - dragOffset.x;
				py = current.mousePosition.y - dragOffset.y;
				px = Mathf.Clamp(px, 0f - cfg.histW + 140f, (float)UiScale.SW - 140f);
				py = Mathf.Clamp(py, 0f, (float)UiScale.SH - 46f);
				current.Use();
			}
			else if ((int)current.type == 1 && dragging)
			{
				dragging = false;
				CompanionPlugin.SaveAll();
			}
		}
	}

	private static string HostName(CompanionHost host)
	{
		string text = host.PersonaName();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return Loc.T("同伴");
	}

	private void DrawMinimizedTab(CompanionHost host, ModConfig cfg)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		Rect val = default(Rect);
		val = new Rect(10f, (float)UiScale.SH - 158f, 118f, 26f);
		if (GUI.Button(val, Loc.T("陪伴对话"), _btn))
		{
			cfg.historyOpen = true;
			CompanionPlugin.SaveAll();
		}
		if (host.BudgetLeft())
		{
			Fill(new Rect(((Rect)val).x - 3f, ((Rect)val).y + 4f, 2f, 18f), Accent);
		}
	}

	private void DrawInputBox(CompanionHost host, ModConfig cfg)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Invalid comparison between Unknown and I4
		//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Invalid comparison between Unknown and I4
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Invalid comparison between Unknown and I4
		if (((Rect)inputRect).x < 0f)
		{
			inputRect = FeetRect(host, 360f, 32f);
		}
		Rect r = inputRect;
		Fill(r, new Color(0.03f, 0.035f, 0.045f, 0.92f));
		Fill(new Rect(((Rect)r).x, ((Rect)r).y, ((Rect)r).width, 1f), new Color(0.4f, 0.55f, 0.7f, 0.8f));
		Fill(new Rect(((Rect)r).x, ((Rect)r).yMax - 1f, ((Rect)r).width, 1f), new Color(0.4f, 0.55f, 0.7f, 0.8f));
		GUI.SetNextControlName("cu_input");
		inputText = GUI.TextField(new Rect(((Rect)r).x + 6f, ((Rect)r).y + 3f, ((Rect)r).width - 80f, ((Rect)r).height - 6f), inputText, 200, _field);
		if (GUI.Button(new Rect(((Rect)r).xMax - 68f, ((Rect)r).y + 2f, 64f, ((Rect)r).height - 4f), Loc.T("说出口"), _btn))
		{
			Send(host);
		}
		Event current = Event.current;
		if (current != null && (int)current.type == 4 && ((int)current.keyCode == 13 || (int)current.keyCode == 271) && GUI.GetNameOfFocusedControl() == "cu_input")
		{
			Send(host);
			current.Use();
		}
		if (host.HasBody)
		{
			inputRect = FeetRect(host, ((Rect)r).width, ((Rect)r).height);
		}
	}

	private Rect FeetRect(CompanionHost host, float w, float h)
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		Body currentBody = host.CurrentBody;
		Camera main = Camera.main;
		float num = (float)UiScale.SW * 0.5f - w * 0.5f;
		float num2 = (float)UiScale.SH - 92f;
		if ((Object)(object)currentBody != (Object)null && (Object)(object)main != (Object)null)
		{
			Vector3 val = default(Vector3);
			val = new Vector3(((Component)currentBody).transform.position.x, ((Component)currentBody).transform.position.y - 1.7f, ((Component)currentBody).transform.position.z);
			Vector3 val2 = main.WorldToScreenPoint(val);
			if (val2.z > 0f)
			{
				num = val2.x - w * 0.5f;
				num2 = (float)UiScale.SH - val2.y + 14f;
			}
		}
		num = Mathf.Clamp(num, 8f, (float)UiScale.SW - w - 8f);
		num2 = Mathf.Clamp(num2, 8f, (float)UiScale.SH - h - 8f);
		return new Rect(num, num2, w, h);
	}

	private void Send(CompanionHost host)
	{
		string text = inputText;
		inputText = "";
		if (!string.IsNullOrEmpty(text))
		{
			host.SubmitPlayerInput(text);
		}
		GUI.FocusControl("cu_input");
	}

	private void DrawSettings(ModConfig cfg)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(450f, (float)UiScale.SW - 16f);
		float num2 = Mathf.Min(580f, (float)UiScale.SH - 16f);
		Rect r = default(Rect);
		r = new Rect((float)UiScale.SW - num - 12f, Mathf.Max(8f, ((float)UiScale.SH - num2) * 0.5f - 60f), num, num2);
		Fill(r, new Color(0.045f, 0.05f, 0.062f, 0.97f));
		UiPointerGuard.Mark(r);
		GUI.Label(new Rect(((Rect)r).x + 12f, ((Rect)r).y + 8f, 260f, 20f), Loc.T("LLM 接口 · 行为设置"), _title);
		if (GUI.Button(new Rect(((Rect)r).xMax - 30f, ((Rect)r).y + 8f, 22f, 20f), "×", _btn))
		{
			settingsOpen = false;
			CompanionPlugin.SaveAll();
		}
		GUILayout.BeginArea(new Rect(((Rect)r).x + 12f, ((Rect)r).y + 34f, ((Rect)r).width - 24f, ((Rect)r).height - 46f));
		settingsScroll = GUILayout.BeginScrollView(settingsScroll, Array.Empty<GUILayoutOption>());
		ApiSettings api = cfg.api;
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("协议"), _small, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(52f) });
		if (GUILayout.Toggle(api.provider == "openai", Loc.T("OpenAI 兼容"), _btnOn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(112f) }))
		{
			api.provider = "openai";
		}
		if (GUILayout.Toggle(api.provider == "anthropic", "Anthropic", _btnOn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(92f) }))
		{
			api.provider = "anthropic";
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(3f);
		GUILayout.Label(Loc.T("接口地址"), _small, Array.Empty<GUILayoutOption>());
		api.baseUrl = GUILayout.TextField(api.baseUrl ?? "", _field, Array.Empty<GUILayoutOption>());
		GUILayout.Space(3f);
		GUILayout.Label("API Key", _small, Array.Empty<GUILayoutOption>());
		api.apiKey = GUILayout.TextField(api.apiKey ?? "", _field, Array.Empty<GUILayoutOption>());
		GUILayout.Space(3f);
		GUILayout.Label(Loc.T("模型"), _small, Array.Empty<GUILayoutOption>());
		api.model = GUILayout.TextField(api.model ?? "", _field, Array.Empty<GUILayoutOption>());
		GUILayout.Space(6f);
		GUILayout.Label(Loc.T("温度 ") + api.temperature.ToString("F2"), _small, Array.Empty<GUILayoutOption>());
		api.temperature = GUILayout.HorizontalSlider(api.temperature, 0f, 1.5f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("回复长度上限 ") + api.maxTokens, _small, Array.Empty<GUILayoutOption>());
		api.maxTokens = (int)GUILayout.HorizontalSlider((float)api.maxTokens, 60f, 4000f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("超时 ") + api.timeoutSec + Loc.T(" 秒"), _small, Array.Empty<GUILayoutOption>());
		api.timeoutSec = (int)GUILayout.HorizontalSlider((float)api.timeoutSec, 5f, 120f, Array.Empty<GUILayoutOption>());
		GUILayout.Space(8f);
		GUILayout.Label(Loc.T("<color=#8fb7d9>── 行为 ──</color>"), _small, Array.Empty<GUILayoutOption>());
		cfg.autoTalk = GUILayout.Toggle(cfg.autoTalk, Loc.T(" 允许角色主动开口"), _btn, Array.Empty<GUILayoutOption>());
		cfg.showMoodles = GUILayout.Toggle(cfg.showMoodles, Loc.T(" 安抚时叠加「集中」状态"), _btn, Array.Empty<GUILayoutOption>());
		cfg.clearHorror = GUILayout.Toggle(cfg.clearHorror, Loc.T(" 安抚时清空恐惧值"), _btn, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("时间窗内最多反应 ") + cfg.reactionsPerWindow + Loc.T(" 次"), _small, Array.Empty<GUILayoutOption>());
		cfg.reactionsPerWindow = (int)GUILayout.HorizontalSlider((float)cfg.reactionsPerWindow, 1f, 10f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("时间窗 ") + (int)cfg.reactionWindowSec + Loc.T(" 秒"), _small, Array.Empty<GUILayoutOption>());
		cfg.reactionWindowSec = GUILayout.HorizontalSlider(cfg.reactionWindowSec, 30f, 900f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("自动开口最小间隔 ") + (int)cfg.autoCooldownSec + Loc.T(" 秒"), _small, Array.Empty<GUILayoutOption>());
		cfg.autoCooldownSec = GUILayout.HorizontalSlider(cfg.autoCooldownSec, 10f, 300f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("安抚强度 ") + cfg.reliefStrength.ToString("F2"), _small, Array.Empty<GUILayoutOption>());
		cfg.reliefStrength = GUILayout.HorizontalSlider(cfg.reliefStrength, 0.2f, 2f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("安抚持续 ") + (int)cfg.reliefDuration + Loc.T(" 秒"), _small, Array.Empty<GUILayoutOption>());
		cfg.reliefDuration = GUILayout.HorizontalSlider(cfg.reliefDuration, 5f, 60f, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("创伤阻尼 ") + cfg.traumaDampPerSec.ToString("F2") + Loc.T(" /秒"), _small, Array.Empty<GUILayoutOption>());
		cfg.traumaDampPerSec = GUILayout.HorizontalSlider(cfg.traumaDampPerSec, 0f, 3f, Array.Empty<GUILayoutOption>());
		GUILayout.Space(8f);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("保存并关闭"), _btn, Array.Empty<GUILayoutOption>()))
		{
			settingsOpen = false;
			CompanionPlugin.SaveAll();
		}
		if (GUILayout.Button(Loc.T("测试连接"), _btn, Array.Empty<GUILayoutOption>()))
		{
			apiTestMsg = Loc.T("正在测试…");
			apiTestMsgTime = Time.unscaledTime;
			TestApi();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
		GUILayout.EndArea();
	}

	private async void TestApi()
	{
		ApiSettings api = CompanionPlugin.Cfg.api;
		if (!Llm.HasKey(api))
		{
			apiTestMsg = Loc.T("还没有填 API Key。");
			apiTestMsgTime = Time.unscaledTime;
			return;
		}
		List<ChatMsg> list = new List<ChatMsg>();
		list.Add(new ChatMsg("user", Loc.T("只回两个字：在。")));
		string text = await Llm.ChatAsync(api, list, CancellationToken.None);
		apiTestMsg = (string.IsNullOrEmpty(text) ? Loc.T("连接失败，详情看 BepInEx 控制台。") : (Loc.T("连接成功：") + text));
		apiTestMsgTime = Time.unscaledTime;
	}

	private void DrawPersona(ModConfig cfg)
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		try
		{
			num = Mathf.Clamp(PreRunScript.characterSelected, 0, 2);
		}
		catch
		{
			num = 0;
		}
		if ((Object)(object)CompanionHost.Instance != (Object)null && (Object)(object)CompanionHost.Instance.CurrentBody != (Object)null)
		{
			num = Mathf.Clamp(CompanionHost.Instance.CurrentBody.charType, 0, 2);
		}
		Persona persona = CompanionPlugin.Store.Get(num);
		float num2 = Mathf.Min(600f, (float)UiScale.SW - 16f);
		float num3 = Mathf.Min(600f, (float)UiScale.SH - 16f);
		Rect r = default(Rect);
		r = new Rect(((float)UiScale.SW - num2) * 0.5f, ((float)UiScale.SH - num3) * 0.5f, num2, num3);
		Fill(r, new Color(0.045f, 0.05f, 0.062f, 0.97f));
		UiPointerGuard.Mark(r);
		GUI.Label(new Rect(((Rect)r).x + 12f, ((Rect)r).y + 8f, 400f, 20f), Loc.T("角色 ") + (num + 1) + Loc.T(" · 人设与互动"), _title);
		if (GUI.Button(new Rect(((Rect)r).xMax - 30f, ((Rect)r).y + 8f, 22f, 20f), "×", _btn))
		{
			personaOpen = false;
			CompanionPlugin.SaveAll();
		}
		GUILayout.BeginArea(new Rect(((Rect)r).x + 12f, ((Rect)r).y + 34f, ((Rect)r).width - 24f, ((Rect)r).height - 46f));
		personaScroll = GUILayout.BeginScrollView(personaScroll, Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("三个角色各自独立保存，只有选中的角色会用这一套。"), _chip, Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		GUILayout.Label(Loc.T("性别"), _label, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(56f) });
		if (GUILayout.Toggle(persona.gender == 0, Loc.T("女"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(52f) }))
		{
			persona.gender = 0;
		}
		if (GUILayout.Toggle(persona.gender == 1, Loc.T("男"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(52f) }))
		{
			persona.gender = 1;
		}
		if (GUILayout.Toggle(persona.gender == 2, Loc.T("无性"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(60f) }))
		{
			persona.gender = 2;
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("名字"), _small, Array.Empty<GUILayoutOption>());
		persona.name = GUILayout.TextField(persona.name ?? "", _field, Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("性格与说话方式（自由输入，直接决定说话的语气）"), _small, Array.Empty<GUILayoutOption>());
		persona.personality = GUILayout.TextArea(persona.personality ?? "", _area, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(70f) });
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("外貌（只进入 LLM 的认知；游戏贴图不会变，想让眼睛也相信就用下面的颜色滑块）"), _small, Array.Empty<GUILayoutOption>());
		persona.appearance = GUILayout.TextArea(persona.appearance ?? "", _area, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(60f) });
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("称呼玩家为"), _small, Array.Empty<GUILayoutOption>());
		persona.callPlayer = GUILayout.TextField(persona.callPlayer ?? Loc.T("主人"), _field, Array.Empty<GUILayoutOption>());
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("与玩家的关系（角色元数据，随角色切换）"), _small, Array.Empty<GUILayoutOption>());
		persona.relationship = GUILayout.TextArea(persona.relationship ?? "", _area, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Height(56f) });
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("可用互动（用 、或 , 分隔；点一下只填入输入框）"), _small, Array.Empty<GUILayoutOption>());
		string text = ((persona.interactions == null) ? "" : string.Join(Loc.ListSep, persona.interactions));
		string text2 = GUILayout.TextField(text, _field, Array.Empty<GUILayoutOption>());
		string[] array;
		if (text2 != text)
		{
			List<string> list = new List<string>();
			array = text2.Split(new string[3] { "、", ",", "，" }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text3 = array[i].Trim();
				if (text3.Length > 0)
				{
					list.Add(text3);
				}
			}
			persona.interactions = list.ToArray();
		}
		GUILayout.Space(6f);
		persona.bodyTint = GUILayout.Toggle(persona.bodyTint, Loc.T(" 身体颜色生效"), _btn, Array.Empty<GUILayoutOption>());
		if (persona.bodyTint)
		{
			GUILayout.Label(Loc.T("色相 ") + (int)(persona.bodyHue * 360f) + "°", _small, Array.Empty<GUILayoutOption>());
			persona.bodyHue = GUILayout.HorizontalSlider(persona.bodyHue, 0f, 1f, Array.Empty<GUILayoutOption>());
			GUILayout.Label(Loc.T("明度 ") + persona.bodyValue.ToString("F2"), _small, Array.Empty<GUILayoutOption>());
			persona.bodyValue = GUILayout.HorizontalSlider(persona.bodyValue, 0.3f, 1.2f, Array.Empty<GUILayoutOption>());
		}
		GUILayout.Space(4f);
		persona.eyeTint = GUILayout.Toggle(persona.eyeTint, Loc.T(" 眼睛颜色生效"), _btn, Array.Empty<GUILayoutOption>());
		if (persona.eyeTint)
		{
			GUILayout.Label(Loc.T("色相 ") + (int)(persona.eyeHue * 360f) + "°", _small, Array.Empty<GUILayoutOption>());
			persona.eyeHue = GUILayout.HorizontalSlider(persona.eyeHue, 0f, 1f, Array.Empty<GUILayoutOption>());
			GUILayout.Label(Loc.T("明度 ") + persona.eyeValue.ToString("F2"), _small, Array.Empty<GUILayoutOption>());
			persona.eyeValue = GUILayout.HorizontalSlider(persona.eyeValue, 0.3f, 1.2f, Array.Empty<GUILayoutOption>());
		}
		GUILayout.Space(6f);
		GUILayout.Label(Loc.T("可用互动（角色元数据 · 仅预览：点一下填进输入框，不会自动发出）"), _small, Array.Empty<GUILayoutOption>());
		string[] obj2 = ((persona.interactions != null && persona.interactions.Length != 0) ? persona.interactions : CompanionHost.Presets);
		int num4 = 0;
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		array = obj2;
		foreach (string text4 in array)
		{
			if (GUILayout.Button(text4, _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(180f) }))
			{
				inputText = text4;
				cfg.inputOpen = true;
			}
			num4++;
			if (num4 % 3 == 0)
			{
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.Space(6f);
		GUILayout.BeginHorizontal(Array.Empty<GUILayoutOption>());
		if (GUILayout.Button(Loc.T("保存"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
		{
			CompanionPlugin.SaveAll();
			apiTestMsg = Loc.T("人设已保存");
			apiTestMsgTime = Time.unscaledTime;
		}
		if (GUILayout.Button(Loc.T("恢复默认"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(90f) }))
		{
			CompanionPlugin.Store.slots[num] = PersonaStore.CreateDefault().slots[num];
			CompanionPlugin.SaveAll();
		}
		if (GUILayout.Button(Loc.T("关闭"), _btn, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.Width(80f) }))
		{
			personaOpen = false;
			CompanionPlugin.SaveAll();
		}
		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
		GUILayout.EndArea();
		CompanionPlugin.Store.slots[num] = persona;
	}
}
