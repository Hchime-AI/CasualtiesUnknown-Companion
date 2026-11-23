namespace CUCompanion;

internal static class Limits
{
	public const int TokensMin = 60;

	public const int TokensMax = 4000;

	public const int TokensDefault = 300;

	public const int SpeakDefaultChars = 500;

	public const int LineMaxChars = 160;

	public const float TypingCharsPerSec = 22f;

	public const float TypingLineGap = 0.45f;

	public const float TypingSoundGrace = 0.05f;

	public const float TypingVolumeDefault = 0.22f;

	public const int DanmakuNameMax = 20;

	public const int DanmakuTextMax = 160;

	public const string DanmakuNameColor = "#6FC3FF";

	public const string DanmakuTextColor = "#CFE4F5";

	public const float DanmakuDisplayWindowSec = 1f;

	public const float DanmakuDedupSec = 30f;

	public const float DanmakuResponseWindowSec = 60f;

	public const int TalkHistoryMax = 220;

	public const int DanmakuHistoryMaxDefault = 300;

	public const int CtxMonitor = 4;

	public const int CtxChat = 7;

	public const int CtxMast = 4;

	public const int CtxPrompt = 10;

	public const float HudPanelW = 900f;

	public const float HudPanelH = 560f;

	public const float HudHistoryTopDefault = 200f;

	public const float HudHistoryRightDefault = 0f;

	public const float HudHistoryAlphaDefault = 0.93f;

	public const float HudTabBottomDefault = 380f;

	public static readonly int[] MonitorTokenSteps = new int[5] { 120, 200, 300, 500, 800 };

	public const int MonitorTokensDefault = 300;

	public const int MonitorSlotMinutes = 3;

	public const float MastActingAt = 20f;

	public const float MastFasterAt = 40f;

	public const float MastPeakAt = 60f;

	public const float MastEndAt = 600f;

	public const float MastBeatEvery = 30f;

	public const float MastLingerSec = 30f;

	public const int MastCharsDefault = 420;

	public const int MastStreamCharsDefault = 20;

	public const float MastHealScaleDefault = 2.3f;

	public const float HealTraumaPerSec = 2.2f;

	public const float HealImmunityPerSec = 0.8f;

	public const float HealTemperaturePerSec = 0.02f;

	public const float HealWetnessPerSec = 0.15f;

	public const float HealConsciousnessPerSec = 0.05f;

	public const float HealSicknessPerSec = 2.4f;

	public const float HealPainPerSec = 5f;

	public const float HealInfectionPerSec = 4f;

	public const float HealBleedPerSec = 2f;

	public const float HealSkinPerSec = 1.8f;

	public const float HealMusclePerSec = 1.2f;

	public const float HealInternalBleedPerSec = 1.6f;

	public const float HealHemothoraxPerSec = 1.6f;

	public const float HealStrokePerSec = 1.2f;

	public const float HealSepticShockPerSec = 1.2f;

	public const float BoostClottingSpeed = 1f;

	public const float BoostBleedingMultMax = 0.15f;

	public const float BoostImmunityMin = 150f;

	public const float BoostInfectionMultMax = 0.25f;

	public const float BoostPainCap = 8f;

	public const float BoostHeartRateMin = 60f;

	public const float BoostBloodOxygenMin = 60f;

	public const float BoostBloodPressureMin = 85f;

	public const float BoostBloodVolumeMin = 35f;

	public const float BoostTemperatureMin = 29f;

	public const int DebtCalorieNeed = 200;

	public const float DebtSleepNeed = 60f;

	public const float ReactionWindowMinSec = 30f;

	public const int HurtReactionsMax = 3;

	public const float HurtWindowSec = 60f;

	public const int InjuryReactionsMax = 2;

	public const float InjuryWindowSec = 60f;

	public const float IntroQuietSec = 20f;

	public const float IntroGraceSec = 6f;

	public const int CraftItemReactionsMax = 2;

	public const float CraftItemWindowSec = 300f;

	public const float CraftPanelRecentSec = 15f;

	public const string CodeNamePlayer = "P1Alloy";

	public const string CodeNamePartner = "P2EXpie";

	public const float DualPlayerSpeedRatio = 1f;

	public const float DualPartnerSpeedRatio = 1f;

	public const int DualPlayerSkillLevel = 15;

	public const float DualFollowDistance = 2.2f;

	public const float DualRecallDistance = 14f;

	public const float DualFarDistance = 8f;

	public const float DualFarSpeedMult = 2.4f;

	public const float DualCarryOffsetY = 0.42f;

	public const float DualCarryBack = 0.55f;

	public const float DualStatusX = 40f;

	public const float DualStatusY = 40f;

	public const int DualSwitchKey = 298;

	public const float DualWhiteTint = 0.82f;

	public const int DualHumanMeds = 3;

	public const int DefaultStreamPort = 8768;
}
