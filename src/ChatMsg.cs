namespace CUCompanion;

public class ChatMsg
{
	public string role;

	public string content;

	public ChatMsg(string role, string content)
	{
		this.role = role;
		this.content = content;
	}
}
