using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PixelHomestead.Game.Input;

public sealed class InputBindings
{
    private readonly Dictionary<ControlAction, Keys[]> _keyboard = new()
    {
        [ControlAction.MoveUp] = [Keys.W, Keys.Up],
        [ControlAction.MoveDown] = [Keys.S, Keys.Down],
        [ControlAction.MoveLeft] = [Keys.A, Keys.Left],
        [ControlAction.MoveRight] = [Keys.D, Keys.Right],
        [ControlAction.Sprint] = [Keys.LeftShift, Keys.RightShift],
        [ControlAction.Interact] = [Keys.E],
        [ControlAction.Inventory] = [Keys.Tab, Keys.I],
        [ControlAction.Pause] = [Keys.Escape],
        [ControlAction.Cancel] = [Keys.Escape],
    };

    public Vector2 ReadMovement(InputState input)
    {
        Vector2 movement = Vector2.Zero;

        if (Down(input, ControlAction.MoveUp))
        {
            movement.Y -= 1;
        }

        if (Down(input, ControlAction.MoveDown))
        {
            movement.Y += 1;
        }

        if (Down(input, ControlAction.MoveLeft))
        {
            movement.X -= 1;
        }

        if (Down(input, ControlAction.MoveRight))
        {
            movement.X += 1;
        }

        return movement.LengthSquared() > 1 ? Vector2.Normalize(movement) : movement;
    }

    public bool InteractPressed(InputState input)
    {
        return Pressed(input, ControlAction.Interact);
    }

    public bool ToolUsePressed(InputState input)
    {
        return input.LeftClick();
    }

    public bool SprintDown(InputState input)
    {
        return Down(input, ControlAction.Sprint);
    }

    public bool CancelPressed(InputState input)
    {
        return Pressed(input, ControlAction.Cancel) || input.RightClick();
    }

    public bool InventoryPressed(InputState input)
    {
        return Pressed(input, ControlAction.Inventory);
    }

    public bool PausePressed(InputState input)
    {
        return Pressed(input, ControlAction.Pause);
    }

    public void Rebind(ControlAction action, params Keys[] keys)
    {
        if (keys.Length == 0)
        {
            throw new ArgumentException("At least one key is required.", nameof(keys));
        }

        _keyboard[action] = keys;
    }

    private bool Down(InputState input, ControlAction action)
    {
        return _keyboard.TryGetValue(action, out Keys[]? keys) && keys.Any(input.Down);
    }

    private bool Pressed(InputState input, ControlAction action)
    {
        return _keyboard.TryGetValue(action, out Keys[]? keys) && keys.Any(input.Pressed);
    }
}
