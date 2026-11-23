using UnityEngine;

namespace CUCompanion;

public class Persona
{
	public int gender;

	public string name = "";

	public string personality = "";

	public string appearance = "";

	public string callPlayer = Loc.T("主人");

	public string relationship = "";

	public string traits = "";

	public string[] interactions = new string[0];

	public bool bodyTint;

	public float bodyHue = 0.08f;

	public float bodyValue = 0.85f;

	public bool eyeTint;

	public float eyeHue = 0.55f;

	public float eyeValue = 0.9f;

	public string GenderText()
	{
		return gender switch
		{
			1 => Loc.T("男"), 
			2 => Loc.T("无性"), 
			_ => Loc.T("女"), 
		};
	}

	public Color BodyColor()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return Color.HSVToRGB(Mathf.Repeat(bodyHue, 1f), 0.55f, Mathf.Clamp(bodyValue, 0.05f, 1.4f));
	}

	public Color EyeColor()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return Color.HSVToRGB(Mathf.Repeat(eyeHue, 1f), 0.9f, Mathf.Clamp(eyeValue, 0.05f, 1.4f));
	}
}
