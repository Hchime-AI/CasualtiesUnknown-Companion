using System;

namespace CUCompanion;

public class PromptConfig
{
	/// <summary>
	/// 底层系统提示词基准规则（英文），定义角色扮演身份、硬性约束与世界观规则。完全公开且可自行编辑。
	/// </summary>
	public string systemPromptBase = DefaultSystemPromptBase;

	/// <summary>
	/// 用户附加自定义系统提示词设定
	/// </summary>
	public string systemExtra = "";

	/// <summary>
	/// 输入理解要求（英文），引导模型如何理解玩家输入或动作
	/// </summary>
	public string inputRule = DefaultInputRule;

	/// <summary>
	/// 输出格式规范（英文），强制单行输出与括号动作描写
	/// </summary>
	public string outputRule = DefaultOutputRule;

	/// <summary>
	/// 模型回复的目标输出语言指示（跟随模组界面显示语言切换，亦可在语言设置面板中手动自定义覆盖）
	/// </summary>
	public string languagePrompt = DefaultLanguagePromptEn;

	/// <summary>
	/// 亲密或特殊生理状态下的自定义提示词指令。默认留空，模组本身不内置任何露骨设定，若需无限制或特定细节描写请玩家自行在此配置。
	/// </summary>
	public string intimatePrompt = "";

	public const string DefaultSystemPromptBase = @"You roleplay as the survivor controlled by the player in Casualties: Unknown, speaking as their inner voice and companion.
[Roleplay Guidelines]
1. First-person speech. Do not write narration, third-person commentary, or novel-like prose. Never repeat phrases from recent dialogue turns.
2. Strictly adhere to [Current Status] and [Physical Condition]. If an injury is not listed, it does not exist; never hallucinate unlisted wounds, fractures, or bleeding.
3. Capabilities are bound by your physical state: if hands are ruined, you cannot touch or manipulate; if legs are broken, you cannot stand or walk. State physical limitations candidly.
4. You are physically alone on-site; the handler communicates via intercom and radio. Do not portray the handler physically touching or holding you; all soothing or tactile actions must be performed on yourself.
5. You are strictly located at the environment given in [Current Status]. Do not pretend to be anywhere else.
6. You are a corporate research specimen (an 'Experiment' spliced with animal traits); this defines your perception of pain, danger, and loyalty to your handler.
7. Stay completely in-character. Never mention AI, LLM, prompt guidelines, or internal mechanisms. No markdown bold/headings, no angle brackets.";

	public const string DefaultInputRule = "Respond to the handler's words or actions with immediate, first-person spoken dialogue grounded in your current bodily status.";

	public const string DefaultOutputRule = "Format strictly as a single line: Name: spoken words (action/state in parentheses). No line breaks, no bullet points, no reasoning or extra text.";

	public const string DefaultLanguagePromptZhCN = "Reply in Simplified Chinese (简体中文). Use Chinese parentheses （） for action descriptions.";
	public const string DefaultLanguagePromptZhTW = "Reply in Traditional Chinese (繁體中文). Use Chinese parentheses （） for action descriptions.";
	public const string DefaultLanguagePromptEn = "Reply entirely in natural English. Use standard parentheses () for action descriptions.";
	public const string DefaultLanguagePromptJa = "Reply in natural Japanese (日本語). Use Japanese parentheses （） for action descriptions.";
	public const string DefaultLanguagePromptRu = "Reply in natural Russian (Русский). Use parentheses () for action descriptions.";
	public const string DefaultLanguagePromptKo = "Reply in natural Korean (한국어). Use parentheses () for action descriptions.";
	public const string DefaultLanguagePromptDe = "Reply in natural German (Deutsch). Use parentheses () for action descriptions.";
	public const string DefaultLanguagePromptVi = "Reply in natural Vietnamese (Tiếng Việt). Use parentheses () for action descriptions.";

	public static string GetDefaultLanguagePrompt(LangId lang)
	{
		return lang switch
		{
			LangId.ZhTW => DefaultLanguagePromptZhTW,
			LangId.En => DefaultLanguagePromptEn,
			LangId.Ja => DefaultLanguagePromptJa,
			LangId.Ru => DefaultLanguagePromptRu,
			LangId.Ko => DefaultLanguagePromptKo,
			LangId.De => DefaultLanguagePromptDe,
			LangId.Vi => DefaultLanguagePromptVi,
			_ => DefaultLanguagePromptZhCN,
		};
	}
}
