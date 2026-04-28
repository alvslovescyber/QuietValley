namespace PixelHomestead.Game.UI;

public sealed class DialogueState
{
    public bool IsOpen { get; private set; }
    public string SpeakerName { get; private set; } = "";
    public string Text { get; private set; } = "";

    public void Open(string speakerName, string text)
    {
        SpeakerName = speakerName;
        Text = text;
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        SpeakerName = "";
        Text = "";
    }
}
