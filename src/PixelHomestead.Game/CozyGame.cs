using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Saving;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;
using PixelHomestead.Game.Audio;
using PixelHomestead.Game.Effects;
using PixelHomestead.Game.Input;
using PixelHomestead.Game.Player;
using PixelHomestead.Game.Rendering;
using PixelHomestead.Game.UI;

namespace PixelHomestead.Game;

public sealed class CozyGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private readonly InputBindings _bindings = new();
    private readonly Camera2D _camera = new();
    private readonly PlayerController _player = new();
    private readonly ParticleSystem _particles = new();
    private readonly AudioSystem _audio = new();

    private SpriteBatch? _spriteBatch;
    private RenderTarget2D? _renderTarget;
    private Texture2D? _pixel;
    private PixelFont? _font;
    private ArtAssets? _art;
    private WorldRenderer? _worldRenderer;
    private PlayerRenderer? _playerRenderer;
    private GameUiRenderer? _ui;
    private ContentDatabase? _content;
    private GameState? _state;
    private SaveManager? _saveManager;
    private GameScreen _screen = GameScreen.MainMenu;
    private GameScreen _returnFromSettings = GameScreen.MainMenu;
    private double _waterAnimation;
    private float _sleepFade;
    private int _settingsScale = 2;

    public CozyGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };

        Window.Title = "Pixel Homestead";
        IsMouseVisible = true;
        Content.RootDirectory = "Content";
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, GameConstants.VirtualWidth, GameConstants.VirtualHeight);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _font = new PixelFont(_pixel);
        _art = new ArtAssets(GraphicsDevice);
        _worldRenderer = new WorldRenderer(_art, _pixel);
        _playerRenderer = new PlayerRenderer(_art, _pixel);
        _ui = new GameUiRenderer(_pixel, _font, _art);

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        _content = ContentDatabase.Load(dataDirectory);
        _saveManager = new SaveManager();
        _state = new GameState(_content);
        ResetPlayerFromState();
    }

    protected override void UnloadContent()
    {
        _art?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _waterAnimation += deltaSeconds;
        _sleepFade = Math.Max(0, _sleepFade - deltaSeconds * 1.8f);

        if (_screen == GameScreen.Playing)
        {
            UpdatePlaying(deltaSeconds);
        }
        else
        {
            UpdateMenus();
        }

        _particles.Update(deltaSeconds);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_spriteBatch is null || _renderTarget is null)
        {
            return;
        }

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Palette.Sky);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        Point mouse = VirtualMousePosition();
        switch (_screen)
        {
            case GameScreen.MainMenu:
                RequireUi().DrawMainMenu(_spriteBatch, mouse);
                break;
            case GameScreen.Credits:
                RequireUi().DrawCredits(_spriteBatch);
                break;
            case GameScreen.Settings:
                RequireUi().DrawSettings(_spriteBatch, mouse, _settingsScale);
                break;
            default:
                DrawGameplay(_spriteBatch);
                break;
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_renderTarget, GetScaledDestination(), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdatePlaying(float deltaSeconds)
    {
        GameState state = RequireState();
        state.Time.Update(deltaSeconds);

        if (_bindings.PausePressed(_input))
        {
            _screen = GameScreen.Pause;
            _audio.PlaySfx("menu_open");
            return;
        }

        if (_bindings.InventoryPressed(_input))
        {
            _screen = GameScreen.Inventory;
            _audio.PlaySfx("inventory_open");
            return;
        }

        UpdateHotbarSelection(state);

        bool spawnedDust = _player.Update(state.World, state.Player, _bindings.ReadMovement(_input), deltaSeconds);
        if (spawnedDust)
        {
            _particles.SpawnDust(_player.Feet);
        }

        if (_bindings.InteractPressed(_input) || _bindings.ToolUsePressed(_input))
        {
            UseSelectedAction();
        }

        _camera.Follow(_player.Center, state.World, deltaSeconds);
    }

    private void UpdateMenus()
    {
        Point mouse = VirtualMousePosition();
        if (_screen == GameScreen.MainMenu)
        {
            if (Clicked(mouse, new Rectangle(252, 140, 136, 26)))
            {
                StartNewGame();
            }
            else if (Clicked(mouse, new Rectangle(252, 174, 136, 26)))
            {
                LoadGame();
            }
            else if (Clicked(mouse, new Rectangle(252, 208, 136, 26)))
            {
                _returnFromSettings = GameScreen.MainMenu;
                _screen = GameScreen.Settings;
            }
            else if (Clicked(mouse, new Rectangle(252, 242, 136, 26)))
            {
                _screen = GameScreen.Credits;
            }
            else if (Clicked(mouse, new Rectangle(252, 276, 136, 26)))
            {
                Exit();
            }
        }
        else if (_screen == GameScreen.Pause)
        {
            if (_bindings.PausePressed(_input) || Clicked(mouse, new Rectangle(252, 123, 136, 25)))
            {
                _screen = GameScreen.Playing;
                _audio.PlaySfx("menu_close");
            }
            else if (Clicked(mouse, new Rectangle(252, 156, 136, 25)))
            {
                SaveGame();
                _audio.PlaySfx("save");
                _screen = GameScreen.Playing;
            }
            else if (Clicked(mouse, new Rectangle(252, 189, 136, 25)))
            {
                _returnFromSettings = GameScreen.Pause;
                _screen = GameScreen.Settings;
            }
            else if (Clicked(mouse, new Rectangle(252, 222, 136, 25)))
            {
                _screen = GameScreen.MainMenu;
            }
            else if (Clicked(mouse, new Rectangle(252, 255, 136, 25)))
            {
                Exit();
            }
        }
        else if (_screen == GameScreen.Inventory)
        {
            if (_bindings.CancelPressed(_input) || _bindings.InventoryPressed(_input))
            {
                _screen = GameScreen.Playing;
                _audio.PlaySfx("inventory_close");
            }
        }
        else if (_screen == GameScreen.Settings)
        {
            if (_bindings.CancelPressed(_input) || Clicked(mouse, new Rectangle(252, 260, 136, 25)))
            {
                _screen = _returnFromSettings;
                _audio.PlaySfx("menu_close");
            }
            else if (Clicked(mouse, new Rectangle(226, 160, 54, 24)))
            {
                _settingsScale = 2;
            }
            else if (Clicked(mouse, new Rectangle(294, 160, 54, 24)))
            {
                _settingsScale = 3;
            }
            else if (Clicked(mouse, new Rectangle(362, 160, 54, 24)))
            {
                _settingsScale = 4;
            }
        }
        else if (_screen == GameScreen.Credits && (_bindings.CancelPressed(_input) || _input.LeftClick()))
        {
            _screen = GameScreen.MainMenu;
            _audio.PlaySfx("menu_close");
        }
    }

    private void DrawGameplay(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        Vector2 camera = _camera.Position;
        RequireWorldRenderer().DrawTerrain(spriteBatch, state, camera, _waterAnimation);
        RequirePlayerRenderer().Draw(spriteBatch, _player, camera);
        _particles.Draw(spriteBatch, RequirePixel(), camera);
        RequireWorldRenderer().DrawWaterOverlay(spriteBatch, state.World, camera, _waterAnimation);
        RequireUi().DrawHud(spriteBatch, state);

        string? prompt = InteractionPrompt(state);
        if (prompt is not null)
        {
            RequireUi().DrawInteractionPrompt(spriteBatch, prompt, _player.Center, camera);
        }

        if (_screen == GameScreen.Inventory)
        {
            RequireUi().DrawInventory(spriteBatch, state);
        }
        else if (_screen == GameScreen.Pause)
        {
            RequireUi().DrawPause(spriteBatch, VirtualMousePosition());
        }

        RequireUi().DrawFade(spriteBatch, _sleepFade);
    }

    private void UseSelectedAction()
    {
        GameState state = RequireState();
        GridPosition target = _player.InteractionTarget();
        InventorySlot selectedSlot = state.Inventory[state.Player.SelectedHotbarIndex];
        ItemDefinition? selectedItem = selectedSlot.ItemId is null
            ? null
            : state.Content.Items.GetValueOrDefault(selectedSlot.ItemId);

        if (state.World.GetTile(target).Type == TileType.SleepSpot)
        {
            state.Sleep();
            SaveGame();
            _sleepFade = 1f;
            _particles.SpawnPickup(_player.Center);
            _audio.PlaySfx("sleep");
            return;
        }

        if (state.World.GetTile(target).Type == TileType.ShippingBox)
        {
            if (state.Economy.ShipFromInventory(state.Inventory, state.Player.SelectedHotbarIndex))
            {
                state.StatusMessage = "Item placed in shipping box.";
                _particles.SpawnPickup(_player.Center);
                _audio.PlaySfx("ship_item");
            }
            return;
        }

        if (
            state.World.GetCrop(target) is not null
            && state.Farming.Harvest(state.World, target, state.Inventory, state.Content)
        )
        {
            state.StatusMessage = "Harvested crop.";
            _particles.SpawnPickup(_player.Center);
            _audio.PlaySfx("item_pickup");
            return;
        }

        if (selectedItem is null)
        {
            state.StatusMessage = "Select a tool or seed.";
            return;
        }

        bool fishing = selectedItem.ToolKind == ToolKind.FishingRod;
        _player.TriggerToolUse(fishing);
        bool acted = selectedItem.ToolKind switch
        {
            ToolKind.Hoe => state.Farming.Till(state.World, target, state.Energy),
            ToolKind.WateringCan => state.Farming.Water(state.World, target, state.Energy),
            ToolKind.FishingRod => TryFish(state),
            ToolKind.Axe => ClearSimpleObstacle(state, target, TileType.Bush, 3),
            ToolKind.Pickaxe => ClearSimpleObstacle(state, target, TileType.Stone, 3),
            _ => selectedItem.Type == ItemType.Seed
                && state.Farming.Plant(state.World, target, state.Inventory, selectedItem.Id, state.Content),
        };

        if (acted)
        {
            state.StatusMessage = $"{selectedItem.DisplayName} used.";
            _particles.SpawnDust(_player.Feet);
            _audio.PlaySfx(
                state.Content.Tools.Values.FirstOrDefault(tool => tool.ItemId == selectedItem.Id)?.FeedbackCue
                    ?? "tool_use"
            );
        }
        else
        {
            state.StatusMessage = "Nothing happened.";
            _audio.PlaySfx("tool_fail");
        }
    }

    private bool TryFish(GameState state)
    {
        string? fishId = state.Fishing.TryCatchFish(
            state.World,
            state.Player.TilePosition,
            state.Player.Facing,
            state.Inventory,
            state.Content,
            state.Energy
        );
        if (fishId is null)
        {
            return false;
        }

        state.StatusMessage = $"Caught {state.Content.Items[fishId].DisplayName}!";
        _particles.SpawnPickup(_player.Center);
        _audio.PlaySfx("fish_catch");
        return true;
    }

    private string? InteractionPrompt(GameState state)
    {
        GridPosition target = _player.InteractionTarget();
        return state.World.GetTile(target).Type switch
        {
            TileType.SleepSpot => "Press E to Sleep",
            TileType.ShippingBox => "Press E to Ship",
            TileType.Water => "Use rod to Fish",
            TileType.Soil when state.World.GetCrop(target) is null => "Use seed to Plant",
            _ => state.World.GetCrop(target) is not null ? "Press E to Harvest" : null,
        };
    }

    private void UpdateHotbarSelection(GameState state)
    {
        Keys[] keys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9];
        for (int index = 0; index < GameConstants.HotbarSlots; index++)
        {
            if (_input.Pressed(keys[index]))
            {
                state.Player.SelectedHotbarIndex = index;
            }
        }
    }

    private void StartNewGame()
    {
        _state = new GameState(RequireContent());
        ResetPlayerFromState();
        _screen = GameScreen.Playing;
    }

    private void LoadGame()
    {
        _state = RequireSaveManager().Load(RequireContent());
        ResetPlayerFromState();
        _screen = GameScreen.Playing;
    }

    private void ResetPlayerFromState()
    {
        GameState state = RequireState();
        _player.ResetFromState(state.Player);
        _camera.SnapTo(
            _player.Center - new Vector2(GameConstants.VirtualWidth / 2f, GameConstants.VirtualHeight / 2f),
            state.World
        );
    }

    private void SaveGame()
    {
        RequireSaveManager().Save(RequireState());
        RequireState().StatusMessage = "Game saved.";
    }

    private static bool ClearSimpleObstacle(GameState state, GridPosition target, TileType obstacleType, int energyCost)
    {
        if (state.World.GetTile(target).Type != obstacleType || !state.Energy.Spend(energyCost))
        {
            return false;
        }

        state.World.SetTile(target, TileType.Grass);
        return true;
    }

    private bool Clicked(Point mouse, Rectangle rectangle)
    {
        return _input.LeftClick() && rectangle.Contains(mouse);
    }

    private Point VirtualMousePosition()
    {
        Rectangle destination = GetScaledDestination();
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            return Point.Zero;
        }

        int x = (int)(
            (_input.CurrentMouse.X - destination.X) * (GameConstants.VirtualWidth / (float)destination.Width)
        );
        int y = (int)(
            (_input.CurrentMouse.Y - destination.Y) * (GameConstants.VirtualHeight / (float)destination.Height)
        );
        return new Point(x, y);
    }

    private Rectangle GetScaledDestination()
    {
        int scale = Math.Max(
            1,
            Math.Min(
                _settingsScale,
                Math.Min(
                    GraphicsDevice.PresentationParameters.BackBufferWidth / GameConstants.VirtualWidth,
                    GraphicsDevice.PresentationParameters.BackBufferHeight / GameConstants.VirtualHeight
                )
            )
        );
        int width = GameConstants.VirtualWidth * scale;
        int height = GameConstants.VirtualHeight * scale;
        int x = (GraphicsDevice.PresentationParameters.BackBufferWidth - width) / 2;
        int y = (GraphicsDevice.PresentationParameters.BackBufferHeight - height) / 2;
        return new Rectangle(x, y, width, height);
    }

    private ContentDatabase RequireContent()
    {
        return _content ?? throw new InvalidOperationException("Content database is not loaded.");
    }

    private GameState RequireState()
    {
        return _state ?? throw new InvalidOperationException("Game state is not initialized.");
    }

    private SaveManager RequireSaveManager()
    {
        return _saveManager ?? throw new InvalidOperationException("Save manager is not initialized.");
    }

    private Texture2D RequirePixel()
    {
        return _pixel ?? throw new InvalidOperationException("Pixel texture is not loaded.");
    }

    private WorldRenderer RequireWorldRenderer()
    {
        return _worldRenderer ?? throw new InvalidOperationException("World renderer is not loaded.");
    }

    private PlayerRenderer RequirePlayerRenderer()
    {
        return _playerRenderer ?? throw new InvalidOperationException("Player renderer is not loaded.");
    }

    private GameUiRenderer RequireUi()
    {
        return _ui ?? throw new InvalidOperationException("UI renderer is not loaded.");
    }
}
