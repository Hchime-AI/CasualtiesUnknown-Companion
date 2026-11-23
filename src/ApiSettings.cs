namespace CUCompanion;

public class ApiSettings
{
	public string provider = "openai";

	public string baseUrl = "https://api.openai.com/v1/chat/completions";

	public string apiKey = "";

	public string model = "gpt-4o-mini";

	public float temperature = 0.9f;

	public int maxTokens = 300;

	public int timeoutSec = 30;

	public bool showThinking;

	public string extraHeaders = "";

	public ApiSettings Clone()
	{
		return new ApiSettings
		{
			provider = provider,
			baseUrl = baseUrl,
			apiKey = apiKey,
			model = model,
			temperature = temperature,
			maxTokens = maxTokens,
			timeoutSec = timeoutSec,
			showThinking = showThinking,
			extraHeaders = extraHeaders
		};
	}
}
