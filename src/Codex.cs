using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace CUCompanion;

/// <summary>图鉴里的一行数值。模板对所有角色一致，只有值不同。</summary>
internal sealed class CodexStat
{
	internal string Zh;

	internal string En;

	internal string Value;

	/// <summary>true = 由玩家改（性别），值存在 codex.json。</summary>
	internal bool Editable;
}

/// <summary>一条角色档案。字段集由模板固定，不允许条目自带专属栏目。</summary>
internal sealed class CodexEntry
{
	internal string Id;

	internal string Slot;

	internal string Code;

	internal string Prefab;

	internal string SpeciesZh;

	internal string SpeciesEn;

	/// <summary>图鉴档案正文。只有这一栏，不允许再拆出角色专属小节。</summary>
	internal string LoreZh;

	internal string LoreEn;

	internal CodexStat[] Stats;

	/// <summary>人设槽位（0..2）；-1 = 该角色不设人设。</summary>
	internal int PersonaSlot = -1;
}

/// <summary>玩家改过的部分单独存 codex.json，不动 settings.json / personas.json。</summary>
internal sealed class CodexStore
{
	public int version = 2;

	public Dictionary<string, int> gender = new Dictionary<string, int>();

	public Dictionary<string, string> note = new Dictionary<string, string>();

	public Dictionary<string, int> slot = new Dictionary<string, int>();

	public string selected = "p1";

	public int mode;

	private static CodexStore cached;

	internal static string FilePath => Path.Combine(StorageManager.Dir, "codex.json");

	internal static CodexStore Get()
	{
		if (cached != null)
		{
			if (cached.gender == null)
			{
				cached.gender = new Dictionary<string, int>();
			}
			if (cached.note == null)
			{
				cached.note = new Dictionary<string, string>();
			}
			if (cached.slot == null)
			{
				cached.slot = new Dictionary<string, int>();
			}
			return cached;
		}
		try
		{
			if (File.Exists(FilePath))
			{
				cached = JsonConvert.DeserializeObject<CodexStore>(File.ReadAllText(FilePath));
			}
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log?.LogWarning("[Codex] 读取 codex.json 失败：" + ex.Message);
		}
		if (cached == null)
		{
			cached = new CodexStore();
		}
		if (cached.version < 2)
		{
			cached = new CodexStore();
		}
		if (cached.gender == null)
		{
			cached.gender = new Dictionary<string, int>();
		}
		if (cached.note == null)
		{
			cached.note = new Dictionary<string, string>();
		}
		if (cached.slot == null)
		{
			cached.slot = new Dictionary<string, int>();
		}
		return cached;
	}

	internal static void Save()
	{
		try
		{
			File.WriteAllText(FilePath, JsonConvert.SerializeObject(Get(), Formatting.Indented));
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log?.LogWarning("[Codex] 写入 codex.json 失败：" + ex.Message);
		}
	}

	internal int Gender(string id)
	{
		int value;
		if (gender.TryGetValue(id, out value))
		{
			return Mathf.Clamp(value, 0, 2);
		}
		return 0;
	}

	internal string Note(string id)
	{
		string value;
		if (note.TryGetValue(id, out value) && value != null)
		{
			return value;
		}
		return "";
	}

	internal int SlotOf(CodexEntry entry)
	{
		int value;
		if (slot.TryGetValue(entry.Id, out value))
		{
			return Mathf.Clamp(value, 0, 2);
		}
		return (entry.PersonaSlot < 0) ? 0 : entry.PersonaSlot;
	}
}

internal static class Codex
{
	/// <summary>数值行的固定顺序：模板对所有角色一致。</summary>
	private static readonly string[] StatZh = { "身高", "体重", "年龄", "生日", "编号", "力量", "智力", "韧性", "性别" };

	private static readonly string[] StatEn = { "Height", "Weight", "Age", "Birth", "File no.", "Force", "Intellect", "Resilience", "Sex" };

	internal static readonly CodexEntry[] Entries = Build();

	private static CodexEntry Make(string id, string slot, string code, string prefab, string speciesZh, string speciesEn, string[] values, string loreZh, string loreEn, int personaSlot)
	{
		CodexStat[] array = new CodexStat[StatZh.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new CodexStat
			{
				Zh = StatZh[i],
				En = StatEn[i],
				Value = ((i < values.Length) ? values[i] : "—"),
				Editable = (i == array.Length - 1)
			};
		}
		return new CodexEntry
		{
			Id = id,
			Slot = slot,
			Code = code,
			Prefab = prefab,
			SpeciesZh = speciesZh,
			SpeciesEn = speciesEn,
			Stats = array,
			LoreZh = loreZh,
			LoreEn = loreEn,
			PersonaSlot = personaSlot
		};
	}

	private static CodexEntry[] Build()
	{
		List<CodexEntry> list = new List<CodexEntry>();
		// P1 · 与 Milky 同数据，仅体重 +3kg（52 → 55）。
		list.Add(Make("p1", "P1", "ALLOY", "Special/Characters/Milky", "Milky 系 · 军事化改造", "Milky line · militarised",
			new string[] { "158 cm", "55 kg", "19", "—", "CU-ALLOY-01", "10", "10", "10", "女" },
			"...",
			"...",
			-1));
		list.Add(Make("p2", "P2", "EXPIE", "Special/Characters/Experiment", "SAW-1 实验体", "SAW-1 specimen",
			new string[] { "162 cm", "54 kg", "21", "—", "SAW-01", "10", "12", "9", "女" },
			"...",
			"...",
			0));
		list.Add(Make("p3", "P3", "MILKY", "Special/Characters/Milky", "SAW-03 标准个体", "SAW-03 standard",
			new string[] { "158 cm", "52 kg", "19", "—", "SAW-03", "10", "10", "10", "女" },
			"官方档案暂缺：本条只登记已确认的物种基线，未登记的内容不进入生成。",
			"No official file yet: this entry records only the confirmed species baseline; anything unrecorded stays out of generation.",
			1));
		list.Add(Make("p4", "P4", "DUNE", "Special/Characters/Dune", "Dune 系 · 荒漠适应", "Dune line · arid-adapted",
			new string[] { "170 cm", "60 kg", "22", "—", "DUNE-02", "12", "9", "11", "女" },
			"官方档案暂缺：本条只登记已确认的物种基线，未登记的内容不进入生成。",
			"No official file yet: this entry records only the confirmed species baseline; anything unrecorded stays out of generation.",
			2));
		return list.ToArray();
	}

	internal static CodexEntry Find(string id)
	{
		for (int i = 0; i < Entries.Length; i++)
		{
			if (Entries[i].Id == id)
			{
				return Entries[i];
			}
		}
		return Entries[0];
	}

	internal static string GenderText(int g)
	{
		switch (g)
		{
		case 1:
			return Loc.T("男");
		case 2:
			return Loc.T("无性");
		default:
			return Loc.T("女");
		}
	}
}

/// <summary>图鉴 / 设定 页面。分两栏：左物种索引，右主体；主体分「图鉴档案」与「设定」两种模式。</summary>
internal static class CodexView
{
	private const float RailWidth = 226f;

	private const float RailRow = 54f;

	private const float PortraitWidth = 316f;

	private const float Pad = 16f;

	private static Vector2 scroll;

	private static Vector2 railScroll;

	private static GUIStyle stTitle;

	private static GUIStyle stTitleDim;

	private static GUIStyle stSpecies;

	private static GUIStyle stBody;

	private static GUIStyle stDim;

	private static GUIStyle stValue;

	private static GUIStyle stField;

	private static GUIStyle stFieldArea;

	private static GUIStyle stSeg;

	private static GUIStyle stSegOn;

	private static GUIStyle stHint;

	private static readonly Color Ink = new Color(0.84f, 0.9f, 0.88f);

	private static readonly Color Dim = new Color(0.46f, 0.55f, 0.54f);

	private static readonly Color Accent = new Color(0.56f, 0.89f, 0.78f);

	private static readonly Color Line = new Color(0.15f, 0.21f, 0.21f);

	private static readonly Color Panel = new Color(0.047f, 0.063f, 0.067f);

	private static readonly Color PanelHi = new Color(0.075f, 0.105f, 0.109f);

	private static readonly Color RailOn = new Color(0.098f, 0.145f, 0.149f);

	private static float areaW = 0f;

	private static float areaH = 0f;

	internal static void SetArea(float width, float height)
	{
		areaW = width;
		areaH = height;
	}

	internal static void Draw(GUIStyle head, GUIStyle label, GUIStyle small, GUIStyle field, GUIStyle btn)
	{
		EnsureStyles(head, label, small, field);
		CodexStore store = CodexStore.Get();
		CodexEntry entry = Codex.Find(store.selected);
		float totalW = ((areaW > 200f) ? areaW : Mathf.Min(1576f, (float)Screen.width - 84f));
		float totalH = ((areaH > 200f) ? areaH : Mathf.Max(420f, (float)Screen.height - 120f));
		float rightW = Mathf.Clamp(totalW * 0.2f, 250f, 330f);
		float leftW = Mathf.Max(420f, totalW - rightW - 18f);
		float topH = 92f;
		float bodyH = Mathf.Max(220f, totalH - topH - 6f);
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical(GUILayout.Width(leftW), GUILayout.Height(topH + bodyH));
		GUILayout.Label(entry.Slot + " · " + entry.Code, stTitle, GUILayout.Height(28f));
		GUILayout.Label((Loc.En ? entry.SpeciesEn : entry.SpeciesZh) + "    " + Loc.T("图鉴档案给人看，设定给模型读"), stSpecies, GUILayout.Height(16f));
		GUILayout.Space(8f);
		GUILayout.BeginHorizontal();
		if (SegButton(Loc.T("图鉴档案"), store.mode == 0, 108f))
		{
			store.mode = 0;
			scroll = Vector2.zero;
			CodexStore.Save();
		}
		GUILayout.Space(2f);
		if (SegButton(Loc.T("设定"), store.mode == 1, 80f))
		{
			store.mode = 1;
			scroll = Vector2.zero;
			CodexStore.Save();
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		GUILayout.BeginVertical(Box(Panel));
		scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(bodyH - 18f));
		DrawIndex(store);
		if (store.mode == 0)
		{
			DrawArchive(entry, store);
		}
		else
		{
			DrawPersona(entry, store);
		}
		GUILayout.Space(6f);
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		GUILayout.EndVertical();
		GUILayout.Space(18f);
		GUILayout.BeginVertical(GUILayout.Width(rightW), GUILayout.Height(topH + bodyH));
		float num = Mathf.Min(rightW, (float)Screen.height * 0.3f);
		Rect rect = GUILayoutUtility.GetRect(num, num, GUILayout.Height(Mathf.Round(num * 1.18f)));
		CodexPortrait.Draw(rect, entry.Prefab, entry.Slot + " " + entry.Code);
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("快照 · 直接读预制体，不改动游戏资源"), stHint);
		GUILayout.FlexibleSpace();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button((store.mode == 1) ? Loc.T("保存人设") : Loc.T("保存"), stValue, GUILayout.Height(30f)))
		{
			if (store.mode == 1)
			{
				SavePersona(entry);
			}
			else
			{
				CodexStore.Save();
			}
		}
		if (GUILayout.Button((store.mode == 1) ? Loc.T("恢复模板") : Loc.T("清空补充"), stValue, GUILayout.Height(30f)))
		{
			if (store.mode == 1)
			{
				ResetPersona(entry);
			}
			else
			{
				store.note[entry.Id] = "";
				CodexStore.Save();
			}
		}
		GUILayout.EndHorizontal();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
	}

	internal static void SavePersona(CodexEntry entry)
	{
		try
		{
			if (CompanionPlugin.Store != null)
			{
				CompanionPlugin.Store.Save();
				CompanionPlugin.Log?.LogInfo("[Codex] 人设已保存：槽 " + (entry.PersonaSlot + 1));
			}
		}
		catch (Exception ex)
		{
			CompanionPlugin.Log?.LogWarning("[Codex] 人设保存失败：" + ex.Message);
		}
	}

	internal static void ResetPersona(CodexEntry entry)
	{
		int num = CodexStore.Get().SlotOf(entry);
		PersonaStore personaStore = CompanionPlugin.Store;
		if (personaStore != null && personaStore.slots != null && num >= 0 && num < personaStore.slots.Length)
		{
			PersonaStore personaStore2 = PersonaStore.CreateDefault();
			personaStore.slots[num] = personaStore2.slots[num];
			personaStore.Save();
		}
	}

	private static GUIStyle panelStyle;

	private static readonly Dictionary<int, Texture2D> texCache = new Dictionary<int, Texture2D>();

	private static GUIStyle Box(Color color)
	{
		if (panelStyle != null)
		{
			return panelStyle;
		}
		panelStyle = new GUIStyle();
		panelStyle.normal.background = MakeTex(color);
		panelStyle.border = new RectOffset(1, 1, 1, 1);
		panelStyle.padding = new RectOffset((int)Pad, (int)Pad, (int)Pad, (int)Pad);
		panelStyle.margin = new RectOffset(0, 0, 0, 0);
		return panelStyle;
	}

	private static Texture2D MakeTex(Color color)
	{
		int key = ((int)(color.r * 255f) << 24) | ((int)(color.g * 255f) << 16) | ((int)(color.b * 255f) << 8) | (int)(color.a * 255f);
		Texture2D value;
		if (texCache.TryGetValue(key, out value) && (object)value != null && value != null)
		{
			return value;
		}
		value = new Texture2D(1, 1);
		value.SetPixel(0, 0, color);
		value.Apply();
		value.hideFlags = HideFlags.HideAndDontSave;
		texCache[key] = value;
		return value;
	}

	private static void EnsureStyles(GUIStyle head, GUIStyle label, GUIStyle small, GUIStyle field)
	{
		if (stTitle != null)
		{
			return;
		}
		stTitle = new GUIStyle(head);
		stTitle.fontSize = Mathf.Max(15, head.fontSize);
		stTitle.fontStyle = FontStyle.Bold;
		stTitle.wordWrap = false;
		stTitle.clipping = TextClipping.Overflow;
		stTitle.normal.textColor = Ink;
		stTitle.hover.textColor = Ink;
		stTitle.active.textColor = Accent;
		stTitle.focused.textColor = Accent;
		stTitleDim = new GUIStyle(stTitle);
		stTitleDim.normal.textColor = Dim;
		stTitleDim.clipping = TextClipping.Overflow;
		stSpecies = new GUIStyle(small);
		stSpecies.fontSize = Mathf.Max(10, small.fontSize);
		stSpecies.clipping = TextClipping.Overflow;
		stSpecies.normal.textColor = Dim;
		stBody = new GUIStyle(label);
		stBody.wordWrap = true;
		stBody.richText = false;
		stBody.clipping = TextClipping.Overflow;
		stBody.fontSize = Mathf.Max(11, label.fontSize);
		stBody.normal.textColor = new Color(0.74f, 0.82f, 0.8f);
		stDim = new GUIStyle(small);
		stDim.fontSize = Mathf.Max(10, small.fontSize);
		stDim.normal.textColor = Dim;
		stDim.wordWrap = true;
		stDim.clipping = TextClipping.Overflow;
		stValue = new GUIStyle(label);
		stValue.fontSize = Mathf.Max(12, label.fontSize);
		stValue.normal.textColor = Ink;
		stValue.clipping = TextClipping.Overflow;
		stField = new GUIStyle(field);
		stField.fontSize = Mathf.Max(11, field.fontSize);
		stField.clipping = TextClipping.Overflow;
		stFieldArea = new GUIStyle(field);
		stFieldArea.fontSize = Mathf.Max(11, field.fontSize);
		stFieldArea.wordWrap = true;
		stFieldArea.clipping = TextClipping.Overflow;
		stSeg = new GUIStyle();
		if (head != null && head.font != null)
		{
			stSeg.font = head.font;
		}
		stSeg.alignment = TextAnchor.MiddleCenter;
		stSeg.fontSize = Mathf.Max(11, small.fontSize);
		stSeg.clipping = TextClipping.Overflow;
		stSeg.normal.textColor = Dim;
		stSeg.hover.textColor = Ink;
		stSegOn = new GUIStyle(stSeg);
		stSegOn.normal.textColor = Accent;
		stSegOn.hover.textColor = Accent;
		stSegOn.fontStyle = FontStyle.Bold;
		stSegOn.clipping = TextClipping.Overflow;
		stHint = new GUIStyle(small);
		stHint.fontSize = Mathf.Max(9, small.fontSize - 1);
		stHint.normal.textColor = new Color(0.36f, 0.45f, 0.44f);
		stHint.wordWrap = true;
		stHint.clipping = TextClipping.Overflow;
	}

	private static void Fill(Rect rect, Color color)
	{
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, (Texture)Texture2D.whiteTexture);
		GUI.color = color2;
	}

	private static void DrawIndex(CodexStore store)
	{
		GUILayout.Label(Loc.T("角色索引"), stDim, GUILayout.Height(16f));
		GUILayout.Space(4f);
		for (int i = 0; i < Codex.Entries.Length; i++)
		{
			CodexEntry entry = Codex.Entries[i];
			Rect rect = GUILayoutUtility.GetRect(0f, 4000f, GUILayout.Height(34f), GUILayout.ExpandWidth(true));
			bool flag = store.selected == entry.Id;
			bool flag2 = rect.Contains(Event.current.mousePosition);
			if (Event.current.type == EventType.MouseDown && flag2 && Event.current.button == 0)
			{
				store.selected = entry.Id;
				CodexStore.Save();
				scroll = Vector2.zero;
				Event.current.Use();
			}
			Fill(rect, flag ? RailOn : (flag2 ? PanelHi : new Color(0.031f, 0.043f, 0.047f)));
			Fill(new Rect(rect.x, rect.y, flag ? 3f : 1f, rect.height), flag ? Accent : Line);
			GUI.Label(new Rect(rect.x + 14f, rect.y + 7f, rect.width - 24f, 22f), entry.Slot + " · " + entry.Code, flag ? stTitle : stTitleDim);
			GUILayout.Space(3f);
		}
	}

	private static bool SegButton(string text, bool on, float width)
	{
		Rect rect = GUILayoutUtility.GetRect(width, width, GUILayout.Height(26f));
		bool flag = rect.Contains(Event.current.mousePosition);
		bool flag2 = false;
		if (Event.current.type == EventType.MouseDown && flag && Event.current.button == 0)
		{
			Event.current.Use();
			flag2 = true;
		}
		Fill(rect, on ? RailOn : (flag ? PanelHi : Panel));
		if (on)
		{
			Fill(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), Accent);
		}
		GUI.Label(rect, text, on ? stSegOn : stSeg);
		return flag2 && !on;
	}

	private static void Divider(string title, string hint)
	{
		GUILayout.Space(12f);
		GUILayout.BeginHorizontal();
		GUILayout.Label(title, stTitle, GUILayout.Width(150f), GUILayout.Height(20f));
		GUILayout.Label(hint, stHint, GUILayout.ExpandWidth(true), GUILayout.Height(20f));
		GUILayout.EndHorizontal();
		Rect rect = GUILayoutUtility.GetRect(0f, 4000f, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
		Fill(rect, Line);
		GUILayout.Space(6f);
	}

	private static void DrawArchive(CodexEntry entry, CodexStore store)
	{
		Divider(Loc.T("数值"), Loc.T("硬编码 · 不随人设变化"));
		for (int i = 0; i < entry.Stats.Length; i++)
		{
			DrawStatRow(entry.Stats[i], entry, store);
		}
		GUILayout.Space(4f);
		GUILayout.Label(Loc.T("数值为本模组登记的物种基线；游戏内个体会在此上下浮动。"), stHint);
		Divider(Loc.T("档案记录"), Loc.T("不参与生成，只给玩家和作者看"));
		GUILayout.Label(Loc.En ? entry.LoreEn : entry.LoreZh, stBody);
		Divider(Loc.T("补充记录"), Loc.T("唯一允许自行填写的段落"));
		string text = store.Note(entry.Id);
		string text2 = GUILayout.TextArea(text, stFieldArea, GUILayout.Height(96f), GUILayout.ExpandWidth(true));
		if (text2 != text)
		{
			store.note[entry.Id] = text2;
			CodexStore.Save();
		}
	}

	private static void DrawStatRow(CodexStat stat, CodexEntry entry, CodexStore store)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Label(Loc.En ? stat.En : stat.Zh, stHint, GUILayout.Width(130f), GUILayout.Height(20f));
		if (stat.Editable)
		{
			int num = store.Gender(entry.Id);
			if (GUILayout.Button("‹ " + Codex.GenderText(num) + " ›", stValue, GUILayout.Width(104f), GUILayout.Height(20f)))
			{
				store.gender[entry.Id] = (num + 1) % 3;
				CodexStore.Save();
			}
		}
		else
		{
			GUILayout.Label(stat.Value, stValue, GUILayout.Width(104f), GUILayout.Height(20f));
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		Rect rect = GUILayoutUtility.GetRect(0f, 4000f, GUILayout.Height(1f), GUILayout.ExpandWidth(true));
		Fill(rect, new Color(0.11f, 0.15f, 0.15f));
	}

	private static void DrawPersona(CodexEntry entry, CodexStore store)
	{
		if (entry.PersonaSlot < 0)
		{
			GUILayout.Space(4f);
			GUILayout.Label(Loc.T("该角色不设人设。"), stValue);
			GUILayout.Label(Loc.T("Alloy 以玩家代言人的身份生成，不读取任何设定槽；此页保持为空是预期状态。"), stHint);
			GUILayout.Space(10f);
			GUILayout.Label(Loc.T("名字"), stHint, GUILayout.Height(13f));
			GUI.enabled = false;
			GUILayout.TextField("", stField, GUILayout.Height(20f));
			GUILayout.Space(6f);
			GUILayout.Label(Loc.T("性格"), stHint, GUILayout.Height(13f));
			GUILayout.TextArea("", stFieldArea, GUILayout.Height(56f));
			GUI.enabled = true;
			GUILayout.Space(8f);
			return;
		}
		int num = store.SlotOf(entry);
		GUILayout.BeginHorizontal();
		GUILayout.Label(Loc.T("人设槽位"), stHint, GUILayout.Width(64f), GUILayout.Height(20f));
		if (GUILayout.Button((num + 1).ToString() + " ›", stValue, GUILayout.Width(48f), GUILayout.Height(20f)))
		{
			store.slot[entry.Id] = (num + 1) % 3;
			CodexStore.Save();
		}
		GUILayout.Label(Loc.T("设定即人设，LLM 生成直接读取这里的字段。"), stHint, GUILayout.ExpandWidth(true));
		GUILayout.EndHorizontal();
		GUILayout.Space(8f);
		Persona persona = null;
		PersonaStore personaStore = CompanionPlugin.Store;
		if (personaStore != null && personaStore.slots != null && num < personaStore.slots.Length)
		{
			persona = personaStore.slots[num];
		}
		if (persona == null)
		{
			GUILayout.Label(Loc.T("（人设尚未载入）"), stHint);
			return;
		}
		persona.name = Field(Loc.T("名字"), persona.name, 1);
		persona.callPlayer = Field(Loc.T("称呼玩家"), persona.callPlayer, 1);
		persona.personality = Field(Loc.T("性格"), persona.personality, 4);
		persona.appearance = Field(Loc.T("外观"), persona.appearance, 3);
		persona.relationship = Field(Loc.T("关系"), persona.relationship, 3);
		persona.traits = Field(Loc.T("特征"), persona.traits, 3);
		string text = (persona.interactions == null) ? "" : string.Join("，", persona.interactions);
		string text2 = Field(Loc.T("互动选项（用逗号分隔）"), text, 1);
		if (text2 != text)
		{
			string[] array = text2.Split(new char[] { '，', ',', '、', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
			List<string> list = new List<string>();
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
	}

	private static string Field(string label, string value, int lines)
	{
		GUILayout.Space(6f);
		GUILayout.Label(label, stHint, GUILayout.Height(13f));
		string text;
		if (lines <= 1)
		{
			text = GUILayout.TextField(value ?? "", stField, GUILayout.Height(22f), GUILayout.ExpandWidth(true));
		}
		else
		{
			text = GUILayout.TextArea(value ?? "", stFieldArea, GUILayout.Height(14f * (float)lines), GUILayout.ExpandWidth(true));
		}
		return text;
	}
}
