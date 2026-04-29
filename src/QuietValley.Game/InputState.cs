using Microsoft.Xna.Framework.Input;

namespace QuietValley.Game;

public sealed class InputState
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public MouseState CurrentMouse => _currentMouse;

    public void Update()
    {
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
    }

    public bool Down(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key);
    }

    public bool Pressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    public bool LeftClick()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
    }

    public bool RightClick()
    {
        return _currentMouse.RightButton == ButtonState.Pressed && _previousMouse.RightButton == ButtonState.Released;
    }
}
