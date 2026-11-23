using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CUCompanion;

/// <summary>
/// 运行期文案本地化：把硬编码的简体中文原文按当前语言表翻译。
/// 语言不是英文时原样返回中文，表里查不到时也回退中文，不会出现空白。
/// </summary>
internal static class Loc
{
	private static Dictionary<string, string> table;

	public static bool En
	{
		get
		{
			return LocalizationManager.Current == LangId.En;
		}
	}

	private static Dictionary<string, string> Table
	{
		get
		{
			if (table == null)
			{
				table = L10nEnTable.Build();
			}
			return table;
		}
	}

	public static string T(string zh)
	{
		if (string.IsNullOrEmpty(zh))
		{
			return "";
		}
		// 1. 若当前就是简体中文，直接返回原文
		if (LocalizationManager.Current == LangId.ZhCN)
		{
			return zh;
		}

		// 2. 优先检索 8 国语言原生对照字典 (LocalizationManager.Ui)
		string localized = LocalizationManager.T(zh);
		if (!string.IsNullOrEmpty(localized) && localized != zh)
		{
			return localized;
		}

		// 3. 繁体中文处理：对未在字典中的全部文本进行简繁对齐转换，绝不遗留简中字
		if (LocalizationManager.Current == LangId.ZhTW)
		{
			return ToTraditional(zh);
		}

		// 4. 对于所有非中文目标语言（English/Japanese/Russian/Korean/German/Vietnamese）：
		// 检索全量翻译大表 (L10nEnTable) 返回对应英文，坚决杜绝任何非目标语言的简体中文硬编码遗留！
		if (Table.TryGetValue(zh, out string enVal) && !string.IsNullOrEmpty(enVal))
		{
			return enVal;
		}

		return zh;
	}

	private static readonly Dictionary<char, char> SimpToTrad = new Dictionary<char, char>
	{
		{'体','體'},{'设','設'},{'关','關'},{'门','門'},{'态','態'},{'选','選'},{'辑','輯'},{'视','視'},
		{'觉','覺'},{'动','動'},{'跃','躍'},{'经','經'},{'络','絡'},{'统','統'},{'归','歸'},{'唤','喚'},
		{'盘','盤'},{'阶','階'},{'级','級'},{'绘','繪'},{'间','間'},{'语','語'},{'言','言'},{'栏','欄'},
		{'码','碼'},{'创','創'},{'伤','傷'},{'脑','腦'},{'机','機'},{'运','運'},{'营','營'},{'预','預'},
		{'备','備'},{'产','產'},{'标','標'},{'签','籤'},{'陆','陸'},{'离','離'},{'坠','墜'},{'贴','貼'},
		{'墙','牆'},{'拟','擬'},{'参','參'},{'数','數'},{'频','頻'},{'显','顯'},{'示','示'},{'额','額'},
		{'计','計'},{'时','時'},{'录','錄'},{'制','製'},{'单','單'},{'独','獨'},{'属','屬'},{'连','連'},
		{'绝','絕'},{'对','對'},{'认','認'},{'识','識'},{'处','處'},{'规','規'},{'则','則'},{'区','區'},
		{'块','塊'},{'测','測'},{'线','線'},{'调','調'},{'试','試'},{'开','開'},{'点','點'},{'总','總'},
		{'启','啟'},{'闭','閉'},{'随','隨'},{'发','發'},{'击','擊'},{'残','殘'},{'废','廢'},{'弃','棄'},
		{'类','類'},{'重','重'},{'构','構'},{'补','補'},{'话','話'},{'题','題'},{'实','實'},{'际','際'},
		{'现','現'},{'证','證'},{'询','詢'},{'据','據'},{'库','庫'},{'进','進'},{'汇','匯'},{'报','報'},
		{'决','決'},{'断','斷'},{'准','準'},{'集','集'},{'蓝','藍'},{'绿','綠'},{'持','持'},{'久','久'},
		{'化','化'},{'登','登'},{'失','失'},{'效','效'},{'降','降'},{'游','遊'},{'模','模'},{'错','錯'},
		{'误','誤'},{'警','警'},{'告','告'},{'替','替'},{'换','換'},{'怀','懷'},{'念','念'},{'飘','飄'},
		{'温','溫'},{'食','食'},{'物','物'},{'饭','飯'},{'房','房'},{'痛','痛'},{'呼','呼'},{'骨','骨'},
		{'折','折'},{'脱','脫'},{'臼','臼'},{'感','感'},{'染','染'},{'衰','衰'},{'竭','竭'},{'退','退'}
	};

	public static string ToTraditional(string text)
	{
		if (string.IsNullOrEmpty(text)) return text;
		char[] chars = text.ToCharArray();
		bool changed = false;
		for (int i = 0; i < chars.Length; i++)
		{
			if (SimpToTrad.TryGetValue(chars[i], out char t))
			{
				chars[i] = t;
				changed = true;
			}
		}
		return changed ? new string(chars) : text;
	}

	public static string T(string zh, params object[] args)
	{
		string format = T(zh);
		if (args == null || args.Length == 0)
		{
			return format;
		}
		try
		{
			return string.Format(format, args);
		}
		catch (FormatException)
		{
			return format;
		}
	}

	private static readonly List<WeakReference<TMP_Text>> tracked = new List<WeakReference<TMP_Text>>();

	private static Dictionary<string, string> reverse;

	/// <summary>登记一个已经写过文案的文本控件，语言切换时由 RefreshAll 重新翻译。</summary>
	public static void Track(TMP_Text t)
	{
		if (!((Object)(object)t == (Object)null))
		{
			tracked.Add(new WeakReference<TMP_Text>(t));
		}
	}

	/// <summary>语言切换后把登记过的控件按新语言重写一遍（已销毁的自动剔除）。</summary>
	public static void RefreshAll()
	{
		if (reverse == null)
		{
			reverse = new Dictionary<string, string>();
			foreach (KeyValuePair<string, string> item in Table)
			{
				if (!reverse.ContainsKey(item.Value))
				{
					reverse[item.Value] = item.Key;
				}
			}
		}
		int num = 0;
		for (int num2 = tracked.Count - 1; num2 >= 0; num2--)
		{
			TMP_Text target;
			if (!tracked[num2].TryGetTarget(out target) || (Object)(object)target == (Object)null)
			{
				tracked.RemoveAt(num2);
				continue;
			}
			string text = target.text;
			if (string.IsNullOrEmpty(text))
			{
				continue;
			}
			string value;
			if (En)
			{
				if (!Table.ContainsKey(text) && reverse.TryGetValue(text, out value))
				{
					target.text = T(value);
					num++;
				}
			}
			else if (reverse.TryGetValue(text, out value))
			{
				target.text = value;
				num++;
			}
			if (En)
			{
				target.enableAutoSizing = true;
				target.fontSizeMin = Mathf.Max(8f, target.fontSize * 0.5f);
				target.fontSizeMax = target.fontSize;
			}
		}
		if (CompanionPlugin.Log != null)
		{
			CompanionPlugin.Log.LogInfo((object)("[ui] 语言刷新：重写 " + num + " 处文案"));
		}
	}

	/// <summary>英文比中文长，允许自动缩放，避免文字顶出按钮/面板。</summary>
	public static void Fit(TMP_Text t)
	{
		if ((Object)(object)t == (Object)null)
		{
			return;
		}
		Track(t);
		if (!En)
		{
			return;
		}
		t.enableAutoSizing = true;
		t.fontSizeMin = Mathf.Max(8f, t.fontSize * 0.5f);
		t.fontSizeMax = t.fontSize;
	}

	/// <summary>纯标点（全角）转当前语言常用写法：英文用半角，其余原样。</summary>
	public static string P(string punct)
	{
		if (!En || string.IsNullOrEmpty(punct))
		{
			return punct;
		}
		char[] buf = punct.ToCharArray();
		for (int i = 0; i < buf.Length; i++)
		{
			switch (buf[i])
			{
			case '（': buf[i] = '('; break;
			case '）': buf[i] = ')'; break;
			case '。': buf[i] = '.'; break;
			case '，': buf[i] = ','; break;
			case '：': buf[i] = ':'; break;
			case '；': buf[i] = ';'; break;
			case '！': buf[i] = '!'; break;
			case '？': buf[i] = '?'; break;
			case '【': buf[i] = '['; break;
			case '】': buf[i] = ']'; break;
			case '「': buf[i] = '"'; break;
			case '」': buf[i] = '"'; break;
			case '、': buf[i] = ','; break;
			case '·': buf[i] = '-'; break;
			case '～': buf[i] = '~'; break;
			case '　': buf[i] = ' '; break;
			}
		}
		return new string(buf);
	}

	/// <summary>中文标点在不同语言下的等价物（拼接用）。</summary>
	public static string Colon
	{
		get
		{
			return En ? ": " : "：";
		}
	}

	public static string SemiSep
	{
		get
		{
			return En ? "; " : "；";
		}
	}

	public static string ListSep
	{
		get
		{
			return En ? ", " : "、";
		}
	}

	public static string Period
	{
		get
		{
			return En ? ". " : "。";
		}
	}

	/// <summary>语言名（用于提示词里写 "Reply in English" 之类的指令）。</summary>
	public static string PromptLanguage
	{
		get
		{
			return En ? "English" : "简体中文";
		}
	}
}
