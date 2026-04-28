using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PixelHomestead.Game.UI;

public sealed class ToastLog
{
    private readonly List<ToastMessage> _messages = [];

    public void Add(string text, string iconKey = "info")
    {
        _messages.Insert(0, new ToastMessage(text, iconKey, 3.2f));
        if (_messages.Count > 3)
        {
            _messages.RemoveAt(_messages.Count - 1);
        }
    }

    public void Update(float deltaSeconds)
    {
        for (int index = _messages.Count - 1; index >= 0; index--)
        {
            _messages[index] = _messages[index] with
            {
                RemainingSeconds = _messages[index].RemainingSeconds - deltaSeconds,
            };
            if (_messages[index].RemainingSeconds <= 0)
            {
                _messages.RemoveAt(index);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, GameUiRenderer ui)
    {
        for (int index = 0; index < _messages.Count; index++)
        {
            ToastMessage message = _messages[index];
            float alpha = Math.Clamp(message.RemainingSeconds / 0.45f, 0f, 1f);
            ui.DrawToast(spriteBatch, message.Text, message.IconKey, new Vector2(14, 282 - index * 28), alpha);
        }
    }

    private sealed record ToastMessage(string Text, string IconKey, float RemainingSeconds);
}
