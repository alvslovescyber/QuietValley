using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuietValley.Core.Energy;
using QuietValley.Core.Items;
using QuietValley.Core.Systems;
using QuietValley.Game.Rendering;

namespace QuietValley.Game.UI;

public sealed class GameUiRenderer(Texture2D pixel, PixelFont font, ArtAssets art)
{
    public static readonly Rectangle NewGameButton = new(70, 278, 96, 34);
    public static readonly Rectangle LoadGameButton = new(184, 278, 96, 34);
    public static readonly Rectangle SettingsButton = new(298, 278, 96, 34);
    public static readonly Rectangle CreditsButton = new(412, 278, 96, 34);
    public static readonly Rectangle QuitButton = new(526, 278, 84, 34);
    public static readonly Rectangle ExitHomeButton = new(488, 306, 112, 26);
    public static readonly Rectangle SleepHomeButton = new(368, 306, 104, 26);

    public void DrawMainMenu(SpriteBatch spriteBatch, Point mouse)
    {
        DrawTitleScene(spriteBatch);
        DrawTitleSign(spriteBatch);

        DrawMenuButton(spriteBatch, NewGameButton, "New Farm", mouse);
        DrawMenuButton(spriteBatch, LoadGameButton, "Continue", mouse);
        DrawMenuButton(spriteBatch, SettingsButton, "Settings", mouse);
        DrawMenuButton(spriteBatch, CreditsButton, "Credits", mouse);
        DrawMenuButton(spriteBatch, QuitButton, "Quit", mouse);
    }

    public void DrawHud(SpriteBatch spriteBatch, GameState state, Point mouse)
    {
        DrawPanel(spriteBatch, new Rectangle(10, 8, 156, 34), PanelStyle.Compact);
        DrawLeafBadge(spriteBatch, new Rectangle(18, 16, 14, 14));
        font.Draw(
            spriteBatch,
            $"{state.Energy.CurrentEnergy}/{EnergySystem.MaximumEnergy}",
            new Vector2(38, 17),
            Palette.Text,
            1
        );
        DrawBar(
            spriteBatch,
            new Rectangle(78, 19, 74, 7),
            state.Energy.CurrentEnergy / (float)EnergySystem.MaximumEnergy,
            Palette.Energy
        );

        DrawPanel(spriteBatch, new Rectangle(470, 8, 158, 42), PanelStyle.Compact);
        DrawSunBadge(spriteBatch, new Rectangle(480, 18, 14, 14));
        font.Draw(spriteBatch, $"Day {state.Time.Day} {state.Time.Season}", new Vector2(500, 16), Palette.Text, 1);
        DrawCoin(spriteBatch, 500, 31);
        font.Draw(spriteBatch, $"{state.Time.ClockText} {state.Economy.Coins}G", new Vector2(516, 31), Palette.Text, 1);

        DrawHotbar(spriteBatch, state, mouse);

        InventorySlot selected = state.Inventory[state.Player.SelectedHotbarIndex];
        string selectedText = selected.ItemId is null
            ? "Empty hands"
            : state.Content.Items.GetValueOrDefault(selected.ItemId)?.DisplayName ?? selected.ItemId;
        DrawPanel(spriteBatch, new Rectangle(12, 320, 156, 24), PanelStyle.Compact);
        font.Draw(spriteBatch, selectedText, new Vector2(24, 329), Palette.Text, 1);
    }

    public void DrawOxygen(SpriteBatch spriteBatch, float normalized)
    {
        DrawPanel(spriteBatch, new Rectangle(214, 10, 212, 24), PanelStyle.Compact);
        DrawBubbleIcon(spriteBatch, 225, 18);
        font.Draw(spriteBatch, "Oxygen", new Vector2(242, 18), Palette.Text, 1);
        DrawBar(spriteBatch, new Rectangle(294, 18, 112, 7), normalized, Palette.WaterLight);
    }

    public void DrawHomeInterior(SpriteBatch spriteBatch, Point mouse)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, GameConstants.VirtualWidth, GameConstants.VirtualHeight),
            new Color(42, 25, 18)
        );
        spriteBatch.Draw(art.InteriorTown, new Rectangle(40, 20, 560, 300), ArtAssets.LivingRoomSource, Color.White);
        DrawPanel(spriteBatch, new Rectangle(38, 20, 564, 304), PanelStyle.Compact);
        spriteBatch.Draw(art.InteriorTown, new Rectangle(52, 32, 536, 276), ArtAssets.LivingRoomSource, Color.White);
        DrawPanel(spriteBatch, new Rectangle(60, 22, 220, 30), PanelStyle.Compact);
        font.Draw(spriteBatch, "Your Living Room", new Vector2(74, 34), Palette.Text, 1);
        DrawButton(spriteBatch, SleepHomeButton, "Sleep", mouse);
        DrawButton(spriteBatch, ExitHomeButton, "Exit Home", mouse);
        font.Draw(spriteBatch, "Press E or Esc to leave.", new Vector2(68, 316), Palette.TextLight, 1);
    }

    public void DrawHotbar(SpriteBatch spriteBatch, GameState state, Point mouse)
    {
        int startX = HotbarStartX();
        int y = HotbarY;
        DrawPanel(
            spriteBatch,
            new Rectangle(
                startX - 8,
                y - 8,
                GameConstants.HotbarSlots * HotbarStride - (HotbarStride - HotbarSlotSize) + 16,
                HotbarSlotSize + 16
            ),
            PanelStyle.Compact
        );

        for (int slotIndex = 0; slotIndex < GameConstants.HotbarSlots; slotIndex++)
        {
            Rectangle slotRectangle = HotbarSlotRectangle(slotIndex);
            bool selected = slotIndex == state.Player.SelectedHotbarIndex;
            bool hovered = slotRectangle.Contains(mouse);
            DrawSlot(spriteBatch, slotRectangle, selected, hovered);

            InventorySlot slot = state.Inventory[slotIndex];
            if (slot.IsEmpty || slot.ItemId is null)
            {
                continue;
            }

            ItemDefinition item = state.Content.Items[slot.ItemId];
            DrawItemIcon(spriteBatch, item, new Rectangle(slotRectangle.X + 4, slotRectangle.Y + 4, 20, 20));
            if (slot.Quantity > 1)
            {
                font.Draw(
                    spriteBatch,
                    slot.Quantity.ToString(),
                    new Vector2(slotRectangle.X + 17, slotRectangle.Y + 20),
                    Palette.Text,
                    1
                );
            }
        }

        int? hoveredSlot = HotbarSlotAt(mouse);
        if (hoveredSlot is not null)
        {
            InventorySlot slot = state.Inventory[hoveredSlot.Value];
            if (!slot.IsEmpty && slot.ItemId is not null)
            {
                ItemDefinition item = state.Content.Items[slot.ItemId];
                DrawTooltip(spriteBatch, mouse, item.DisplayName, item.Description);
            }
        }
    }

    public void DrawInventory(SpriteBatch spriteBatch, GameState state, Point mouse, int? draggedSlotIndex)
    {
        DrawOverlay(spriteBatch);
        Rectangle panel = new(122, 58, 396, 238);
        DrawPanel(spriteBatch, panel, PanelStyle.Parchment);
        DrawTinyFlower(spriteBatch, 146, 80);
        font.Draw(spriteBatch, "Inventory", new Vector2(166, 77), Palette.Text, 2);
        DrawCoin(spriteBatch, 400, 84);
        font.Draw(spriteBatch, $"{state.Economy.Coins}G", new Vector2(416, 84), Palette.Text, 1);

        for (int slotIndex = 0; slotIndex < state.Inventory.Capacity; slotIndex++)
        {
            Rectangle slotRectangle = InventorySlotRectangle(slotIndex);
            DrawSlot(
                spriteBatch,
                slotRectangle,
                slotIndex == state.Player.SelectedHotbarIndex,
                slotRectangle.Contains(mouse)
            );

            InventorySlot slot = state.Inventory[slotIndex];
            if (slot.IsEmpty || slot.ItemId is null)
            {
                continue;
            }

            ItemDefinition item = state.Content.Items[slot.ItemId];
            DrawItemIcon(spriteBatch, item, new Rectangle(slotRectangle.X + 4, slotRectangle.Y + 4, 15, 15));
            if (slot.Quantity > 1)
            {
                font.Draw(
                    spriteBatch,
                    slot.Quantity.ToString(),
                    new Vector2(slotRectangle.X + 12, slotRectangle.Y + 15),
                    Palette.Text,
                    1
                );
            }
        }

        InventorySlot selected = state.Inventory[state.Player.SelectedHotbarIndex];
        if (!selected.IsEmpty && selected.ItemId is not null)
        {
            ItemDefinition item = state.Content.Items[selected.ItemId];
            font.Draw(spriteBatch, item.DisplayName, new Vector2(146, 235), Palette.Text, 1);
            font.Draw(spriteBatch, item.Description, new Vector2(146, 252), Palette.Text, 1);
        }

        font.Draw(spriteBatch, "Tab/Esc closes. Click or drag items.", new Vector2(146, 278), Palette.Text, 1);

        int? hovered = InventorySlotAt(mouse);
        if (hovered is not null)
        {
            InventorySlot slot = state.Inventory[hovered.Value];
            if (!slot.IsEmpty && slot.ItemId is not null)
            {
                ItemDefinition item = state.Content.Items[slot.ItemId];
                DrawTooltip(spriteBatch, mouse, item.DisplayName, $"{item.Type}  Sell {item.SellPrice}G");
            }
        }

        if (draggedSlotIndex is not null)
        {
            font.Draw(spriteBatch, "Dragging", new Vector2(mouse.X + 12, mouse.Y + 12), Palette.TextLight, 1);
        }
    }

    public void DrawPause(SpriteBatch spriteBatch, Point mouse)
    {
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(224, 78, 192, 222), PanelStyle.Parchment);
        font.Draw(spriteBatch, "Paused", new Vector2(286, 96), Palette.Text, 2);
        DrawButton(spriteBatch, new Rectangle(252, 123, 136, 25), "Resume", mouse);
        DrawButton(spriteBatch, new Rectangle(252, 156, 136, 25), "Save Game", mouse);
        DrawButton(spriteBatch, new Rectangle(252, 189, 136, 25), "Settings", mouse);
        DrawButton(spriteBatch, new Rectangle(252, 222, 136, 25), "Main Menu", mouse);
        DrawButton(spriteBatch, new Rectangle(252, 255, 136, 25), "Quit", mouse);
    }

    public void DrawSettings(SpriteBatch spriteBatch, Point mouse, GameSettings settings)
    {
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(188, 92, 264, 204), PanelStyle.Parchment);
        font.Draw(spriteBatch, "Settings", new Vector2(272, 112), Palette.Text, 2);
        font.Draw(spriteBatch, $"Music {settings.MusicVolume:0.0}", new Vector2(226, 142), Palette.Text, 1);
        DrawButton(spriteBatch, new Rectangle(344, 136, 26, 22), "-", mouse);
        DrawButton(spriteBatch, new Rectangle(380, 136, 26, 22), "+", mouse);
        font.Draw(spriteBatch, $"SFX {settings.SfxVolume:0.0}", new Vector2(226, 166), Palette.Text, 1);
        DrawButton(spriteBatch, new Rectangle(344, 160, 26, 22), "-", mouse);
        DrawButton(spriteBatch, new Rectangle(380, 160, 26, 22), "+", mouse);
        font.Draw(spriteBatch, $"Scale {settings.WindowScale}x", new Vector2(226, 190), Palette.Text, 1);
        DrawButton(spriteBatch, new Rectangle(344, 184, 26, 22), "-", mouse);
        DrawButton(spriteBatch, new Rectangle(380, 184, 26, 22), "+", mouse);
        font.Draw(
            spriteBatch,
            settings.Fullscreen ? "Fullscreen On" : "Fullscreen Off",
            new Vector2(226, 214),
            Palette.Text,
            1
        );
        DrawButton(spriteBatch, new Rectangle(344, 208, 62, 22), "Toggle", mouse);
        font.Draw(
            spriteBatch,
            settings.ShowCollisionDebug ? "Debug On" : "Debug Off",
            new Vector2(226, 238),
            Palette.Text,
            1
        );
        DrawButton(spriteBatch, new Rectangle(344, 232, 62, 22), "Toggle", mouse);
        DrawButton(spriteBatch, new Rectangle(252, 260, 136, 25), "Back", mouse);
    }

    public void DrawCredits(SpriteBatch spriteBatch)
    {
        DrawTitleScene(spriteBatch);
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(140, 116, 360, 128), PanelStyle.Parchment);
        font.Draw(spriteBatch, "Credits", new Vector2(276, 136), Palette.Text, 2);
        font.Draw(spriteBatch, "Original generated pixel art assets.", new Vector2(178, 172), Palette.Text, 1);
        font.Draw(spriteBatch, "No copyrighted game assets included.", new Vector2(178, 190), Palette.Text, 1);
        font.Draw(spriteBatch, "Click or Esc to return.", new Vector2(230, 220), Palette.Text, 1);
    }

    public void DrawInteractionPrompt(SpriteBatch spriteBatch, string prompt, Vector2 worldPosition, Vector2 camera)
    {
        int width = Math.Clamp(font.MeasureWidth(prompt, 1) + 28, 128, 312);
        Vector2 screenPosition = worldPosition - camera;
        int x = Math.Clamp((int)screenPosition.X - width / 2, 6, GameConstants.VirtualWidth - width - 6);
        int y = Math.Clamp((int)screenPosition.Y - 58, 8, GameConstants.VirtualHeight - 38);
        Rectangle panel = new(x, y, width, 28);

        spriteBatch.Draw(
            pixel,
            new Rectangle(panel.X + 4, panel.Y + 5, panel.Width, panel.Height),
            new Color(24, 18, 14, 130)
        );
        spriteBatch.Draw(pixel, panel, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(panel.X + 3, panel.Y + 3, panel.Width - 6, panel.Height - 6),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(panel.X + 7, panel.Y + 7, panel.Width - 14, panel.Height - 14),
            Palette.ParchmentLight
        );
        spriteBatch.Draw(pixel, new Rectangle(panel.X + 10, panel.Y + 10, panel.Width - 20, 1), Color.White * 0.55f);
        spriteBatch.Draw(pixel, new Rectangle((int)screenPosition.X - 3, panel.Bottom - 1, 6, 5), Palette.WoodDark);
        font.Draw(spriteBatch, prompt, new Vector2(panel.X + 14, panel.Y + 11), Palette.Text, 1);
    }

    public void DrawToast(SpriteBatch spriteBatch, string text, string iconKey, Vector2 position, float alpha)
    {
        int width = Math.Clamp(font.MeasureWidth(text, 1) + 34, 96, 260);
        Rectangle rectangle = new((int)position.X, (int)position.Y, width, 22);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 3, rectangle.Width, rectangle.Height),
            Palette.PanelShadow * alpha
        );
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark * alpha);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
            Palette.Parchment * alpha
        );
        DrawToastIcon(spriteBatch, rectangle.X + 8, rectangle.Y + 7, iconKey, alpha);
        font.Draw(spriteBatch, text, new Vector2(rectangle.X + 24, rectangle.Y + 8), Palette.Text * alpha, 1);
    }

    public void DrawDialogue(SpriteBatch spriteBatch, DialogueState dialogue)
    {
        if (!dialogue.IsOpen)
        {
            return;
        }

        Rectangle panel = new(42, 252, 556, 92);
        DrawPanel(spriteBatch, panel, PanelStyle.Parchment);
        spriteBatch.Draw(pixel, new Rectangle(62, 270, 48, 48), Palette.WoodDark);
        spriteBatch.Draw(pixel, new Rectangle(66, 274, 40, 40), Palette.ParchmentLight);
        DrawTinyFlower(spriteBatch, 82, 288);
        font.Draw(spriteBatch, dialogue.SpeakerName, new Vector2(126, 270), Palette.Text, 1);
        font.Draw(spriteBatch, dialogue.Text, new Vector2(126, 292), Palette.Text, 1);
        font.Draw(spriteBatch, "E / Click", new Vector2(520, 322), Palette.Text, 1);
    }

    public void DrawFade(SpriteBatch spriteBatch, float alpha)
    {
        if (alpha <= 0)
        {
            return;
        }

        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, GameConstants.VirtualWidth, GameConstants.VirtualHeight),
            Color.Black * alpha
        );
    }

    public void DrawPanel(SpriteBatch spriteBatch, Rectangle rectangle, PanelStyle style)
    {
        Color fill = style == PanelStyle.Compact ? new Color(255, 225, 163, 230) : Palette.Parchment;
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 4, rectangle.Width, rectangle.Height),
            Palette.PanelShadow
        );
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Y + 5, rectangle.Width - 10, rectangle.Height - 10),
            Palette.ParchmentDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 7, rectangle.Y + 7, rectangle.Width - 14, rectangle.Height - 14),
            fill
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 9, rectangle.Y + 9, rectangle.Width - 18, 1),
            Palette.ParchmentLight
        );
    }

    public void DrawButton(SpriteBatch spriteBatch, Rectangle rectangle, string label, Point mouse)
    {
        bool hovered = rectangle.Contains(mouse);
        bool pressed =
            hovered
            && Microsoft.Xna.Framework.Input.Mouse.GetState().LeftButton
                == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
        int yOffset = pressed ? 1 : 0;
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 3, rectangle.Width, rectangle.Height),
            Palette.PanelShadow
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Y + yOffset, rectangle.Width, rectangle.Height),
            hovered ? Palette.Highlight : Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2 + yOffset, rectangle.Width - 4, rectangle.Height - 4),
            hovered ? new Color(246, 198, 102) : Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Y + 5 + yOffset, rectangle.Width - 10, 1),
            Palette.WoodLight
        );
        int textWidth = font.MeasureWidth(label, 1);
        font.Draw(
            spriteBatch,
            label,
            new Vector2(rectangle.Center.X - textWidth / 2, rectangle.Y + 9 + yOffset),
            Palette.Text,
            1
        );
    }

    public int? HotbarSlotAt(Point mouse)
    {
        for (int slotIndex = 0; slotIndex < GameConstants.HotbarSlots; slotIndex++)
        {
            if (HotbarSlotRectangle(slotIndex).Contains(mouse))
            {
                return slotIndex;
            }
        }

        return null;
    }

    public static int? InventorySlotAt(Point mouse)
    {
        for (int slotIndex = 0; slotIndex < 36; slotIndex++)
        {
            if (InventorySlotRectangle(slotIndex).Contains(mouse))
            {
                return slotIndex;
            }
        }

        return null;
    }

    private void DrawTitleScene(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            art.MenuBackground,
            new Rectangle(0, 0, GameConstants.VirtualWidth, GameConstants.VirtualHeight),
            Color.White
        );
    }

    private void DrawTitleSign(SpriteBatch spriteBatch)
    {
        Rectangle sign = new(164, 46, 312, 106);
        DrawCenteredText(
            spriteBatch,
            "QUIETVALLEY",
            new Rectangle(sign.X, sign.Y + 50, sign.Width, 24),
            Palette.WoodDark,
            3
        );
    }

    private void DrawMenuButton(SpriteBatch spriteBatch, Rectangle rectangle, string label, Point mouse)
    {
        bool hovered = rectangle.Contains(mouse);
        int yOffset = hovered ? -1 : 0;
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 4, rectangle.Width, rectangle.Height),
            Palette.PanelShadow
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X, rectangle.Y + yOffset, rectangle.Width, rectangle.Height),
            hovered ? Palette.Highlight : Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 3 + yOffset, rectangle.Width - 6, rectangle.Height - 6),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 7, rectangle.Y + 7 + yOffset, rectangle.Width - 14, rectangle.Height - 14),
            hovered ? Palette.ParchmentLight : Palette.Parchment
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 10, rectangle.Y + 10 + yOffset, rectangle.Width - 20, 1),
            Palette.ParchmentLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 10, rectangle.Bottom - 9 + yOffset, rectangle.Width - 20, 2),
            Palette.ParchmentDark
        );

        if (hovered)
        {
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 6, rectangle.Y + 6, 3, 3), Palette.Highlight);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.Right - 9, rectangle.Y + 6, 3, 3), Palette.Highlight);
        }

        DrawCenteredText(
            spriteBatch,
            label.ToUpperInvariant(),
            new Rectangle(rectangle.X + 6, rectangle.Y + 14 + yOffset, rectangle.Width - 12, 12),
            Palette.WoodDark,
            1
        );
    }

    private void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle bounds, Color color, int scale)
    {
        int width = font.MeasureWidth(text, scale);
        Vector2 position = new(bounds.Center.X - width / 2, bounds.Y);
        font.Draw(spriteBatch, text, position + new Vector2(scale, scale), new Color(103, 59, 35), scale);
        font.Draw(spriteBatch, text, position, color, scale);
    }

    private void DrawLeafCluster(SpriteBatch spriteBatch, int x, int y)
    {
        spriteBatch.Draw(pixel, new Rectangle(x + 8, y + 9, 2, 12), Palette.GrassDark);
        spriteBatch.Draw(pixel, new Rectangle(x, y + 8, 10, 5), Palette.GrassLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 9, y + 2, 9, 6), Palette.Energy);
        spriteBatch.Draw(pixel, new Rectangle(x + 5, y + 15, 10, 5), Palette.GrassLight);
    }

    private void DrawSlot(SpriteBatch spriteBatch, Rectangle rectangle, bool selected, bool hovered)
    {
        spriteBatch.Draw(
            pixel,
            rectangle,
            selected ? Palette.Highlight
                : hovered ? Palette.ParchmentLight
                : Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
            selected || hovered ? Palette.ParchmentLight : Palette.Parchment
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Bottom - 4, rectangle.Width - 6, 1),
            Palette.ParchmentDark
        );
    }

    private void DrawItemIcon(SpriteBatch spriteBatch, ItemDefinition item, Rectangle rectangle)
    {
        spriteBatch.Draw(art.Icons, rectangle, ArtAssets.IconSource(item), Color.White);
    }

    private void DrawTooltip(SpriteBatch spriteBatch, Point mouse, string title, string description)
    {
        int width = Math.Max(font.MeasureWidth(title, 1), font.MeasureWidth(description, 1)) + 18;
        Rectangle rectangle = new(
            Math.Min(mouse.X + 12, GameConstants.VirtualWidth - width - 6),
            Math.Max(8, mouse.Y - 42),
            width,
            38
        );
        DrawPanel(spriteBatch, rectangle, PanelStyle.Compact);
        font.Draw(spriteBatch, title, new Vector2(rectangle.X + 9, rectangle.Y + 9), Palette.Text, 1);
        font.Draw(spriteBatch, description, new Vector2(rectangle.X + 9, rectangle.Y + 23), Palette.Text, 1);
    }

    private void DrawToastIcon(SpriteBatch spriteBatch, int x, int y, string iconKey, float alpha)
    {
        Color color = iconKey switch
        {
            "warn" => new Color(230, 73, 67),
            "item" => Palette.Highlight,
            "fish" => Palette.WaterLight,
            _ => Palette.GrassDark,
        };
        spriteBatch.Draw(pixel, new Rectangle(x, y, 8, 8), color * alpha);
        spriteBatch.Draw(pixel, new Rectangle(x + 2, y + 2, 4, 2), Palette.ParchmentLight * alpha);
    }

    private void DrawBubbleIcon(SpriteBatch spriteBatch, int x, int y)
    {
        spriteBatch.Draw(pixel, new Rectangle(x, y, 9, 9), Palette.WaterLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 2, y + 2, 5, 5), new Color(255, 255, 255, 145));
        spriteBatch.Draw(pixel, new Rectangle(x + 5, y + 1, 2, 2), Color.White);
    }

    private void DrawOverlay(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            pixel,
            new Rectangle(0, 0, GameConstants.VirtualWidth, GameConstants.VirtualHeight),
            new Color(30, 20, 18, 150)
        );
    }

    private void DrawBar(SpriteBatch spriteBatch, Rectangle rectangle, float normalized, Color fill)
    {
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
            new Color(80, 57, 42)
        );
        int width = (int)((rectangle.Width - 4) * Math.Clamp(normalized, 0f, 1f));
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 2, rectangle.Y + 2, width, rectangle.Height - 4), fill);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 3, Math.Max(0, width - 2), 1),
            new Color(184, 252, 123)
        );
    }

    private void DrawLeafBadge(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, rectangle.Width - 4, rectangle.Height - 4),
            Palette.GrassDark
        );
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 4, rectangle.Y + 4, 7, 5), Palette.Energy);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 6, rectangle.Y + 9, 2, 3), Palette.Energy);
    }

    private void DrawSunBadge(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 3, rectangle.Width - 6, rectangle.Height - 6),
            Palette.Highlight
        );
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 6, rectangle.Y + 6, 3, 3), Palette.PathLight);
    }

    private void DrawCoin(SpriteBatch spriteBatch, int x, int y)
    {
        spriteBatch.Draw(pixel, new Rectangle(x, y + 2, 10, 10), new Color(179, 112, 30));
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 1, 8, 8), Palette.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(x + 3, y + 3, 3, 2), Palette.PathLight);
    }

    private void DrawTinyFlower(SpriteBatch spriteBatch, int x, int y)
    {
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 4, 1, 4), Palette.GrassDark);
        spriteBatch.Draw(pixel, new Rectangle(x, y + 2, 1, 1), Palette.FlowerPink);
        spriteBatch.Draw(pixel, new Rectangle(x + 2, y + 2, 1, 1), Palette.FlowerPink);
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 3, 1, 1), Palette.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(x + 9, y + 2, 1, 4), Palette.GrassDark);
        spriteBatch.Draw(pixel, new Rectangle(x + 8, y, 1, 1), Palette.FlowerWhite);
        spriteBatch.Draw(pixel, new Rectangle(x + 10, y, 1, 1), Palette.FlowerWhite);
        spriteBatch.Draw(pixel, new Rectangle(x + 9, y + 1, 1, 1), Palette.Highlight);
    }

    private static int HotbarStartX()
    {
        return GameConstants.VirtualWidth / 2 - GameConstants.HotbarSlots * HotbarStride / 2;
    }

    private static Rectangle HotbarSlotRectangle(int slotIndex)
    {
        return new Rectangle(HotbarStartX() + slotIndex * HotbarStride, HotbarY, HotbarSlotSize, HotbarSlotSize);
    }

    private static Rectangle InventorySlotRectangle(int slotIndex)
    {
        int column = slotIndex % 9;
        int row = slotIndex / 9;
        return new Rectangle(146 + column * 27, 112 + row * 27, 23, 23);
    }

    private const int HotbarY = 304;
    private const int HotbarSlotSize = 28;
    private const int HotbarStride = 32;
}

public enum PanelStyle
{
    Compact,
    Parchment,
}
