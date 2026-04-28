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
    private readonly ToastLog _toasts = new();
    private readonly DialogueState _dialogue = new();

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
    private GameSettings _settings = new();
    private GameScreen _screen = GameScreen.MainMenu;
    private GameScreen _returnFromSettings = GameScreen.MainMenu;
    private double _waterAnimation;
    private float _sleepFade;
    private float _sprintEnergyTimer;
    private float _oxygenSeconds = MaximumOxygenSeconds;
    private int? _draggedInventorySlotIndex;
    private bool _isFishing;
    private float _fishingTimer;
    private bool _fishBiting;
    private GridPosition _fishingTarget;

    private const float MaximumOxygenSeconds = 8f;

    public CozyGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true,
        };

        Window.Title = "Pixel Homestead";
        Window.AllowUserResizing = true;
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
        _settings = GameSettings.Load();
        ApplySettings();

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
        _toasts.Update(deltaSeconds);

        if (_screen == GameScreen.Playing)
        {
            UpdatePlaying(deltaSeconds);
        }
        else
        {
            UpdateMenus();
        }

        UpdateFishing(deltaSeconds);
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
                RequireUi().DrawSettings(_spriteBatch, mouse, _settings);
                break;
            case GameScreen.HomeInterior:
                RequireUi().DrawHomeInterior(_spriteBatch, mouse);
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

        if (_input.Pressed(Keys.F3))
        {
            _settings.ShowCollisionDebug = !_settings.ShowCollisionDebug;
            _settings.Save();
            _toasts.Add(_settings.ShowCollisionDebug ? "Collision debug on" : "Collision debug off");
        }

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
        if (TrySelectHotbarWithMouse(state))
        {
            return;
        }

        Vector2 movement = _bindings.ReadMovement(_input);
        bool sprinting = CanSprint(state, movement, deltaSeconds);
        bool spawnedDust = _player.Update(state.World, state.Player, movement, sprinting, deltaSeconds);
        UpdateSwimmingOxygen(state, deltaSeconds);
        if (spawnedDust)
        {
            _particles.SpawnDust(_player.Feet);
        }

        if (_bindings.InteractPressed(_input) || _bindings.ToolUsePressed(_input))
        {
            GridPosition? mouseTarget = _bindings.ToolUsePressed(_input) ? MouseTargetIfInRange() : null;
            if (
                _bindings.ToolUsePressed(_input)
                && mouseTarget is null
                && !RequireUi().HotbarSlotAt(VirtualMousePosition()).HasValue
            )
            {
                _toasts.Add("Too far away.", "warn");
                return;
            }

            UseSelectedAction(mouseTarget ?? _player.InteractionTarget());
        }

        _camera.Follow(_player.Center, state.World, deltaSeconds);
    }

    private void UpdateMenus()
    {
        Point mouse = VirtualMousePosition();
        if (_screen == GameScreen.MainMenu)
        {
            if (Clicked(mouse, GameUiRenderer.NewGameButton))
            {
                StartNewGame();
            }
            else if (Clicked(mouse, GameUiRenderer.LoadGameButton))
            {
                LoadGame();
            }
            else if (Clicked(mouse, GameUiRenderer.SettingsButton))
            {
                _returnFromSettings = GameScreen.MainMenu;
                _screen = GameScreen.Settings;
            }
            else if (Clicked(mouse, GameUiRenderer.CreditsButton))
            {
                _screen = GameScreen.Credits;
            }
            else if (Clicked(mouse, GameUiRenderer.QuitButton))
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
            HandleInventoryMouse(mouse);
            if (_bindings.CancelPressed(_input) || _bindings.InventoryPressed(_input))
            {
                _screen = GameScreen.Playing;
                _draggedInventorySlotIndex = null;
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
            else if (Clicked(mouse, new Rectangle(344, 136, 26, 22)))
            {
                ChangeMusicVolume(-0.1f);
            }
            else if (Clicked(mouse, new Rectangle(380, 136, 26, 22)))
            {
                ChangeMusicVolume(0.1f);
            }
            else if (Clicked(mouse, new Rectangle(344, 160, 26, 22)))
            {
                ChangeSfxVolume(-0.1f);
            }
            else if (Clicked(mouse, new Rectangle(380, 160, 26, 22)))
            {
                ChangeSfxVolume(0.1f);
            }
            else if (Clicked(mouse, new Rectangle(344, 184, 26, 22)))
            {
                ChangeWindowScale(-1);
            }
            else if (Clicked(mouse, new Rectangle(380, 184, 26, 22)))
            {
                ChangeWindowScale(1);
            }
            else if (Clicked(mouse, new Rectangle(344, 208, 62, 22)))
            {
                _settings.Fullscreen = !_settings.Fullscreen;
                ApplySettings();
                _settings.Save();
            }
            else if (Clicked(mouse, new Rectangle(344, 232, 62, 22)))
            {
                _settings.ShowCollisionDebug = !_settings.ShowCollisionDebug;
                _settings.Save();
            }
        }
        else if (_screen == GameScreen.Credits && (_bindings.CancelPressed(_input) || _input.LeftClick()))
        {
            _screen = GameScreen.MainMenu;
            _audio.PlaySfx("menu_close");
        }
        else if (_screen == GameScreen.HomeInterior)
        {
            if (
                _bindings.CancelPressed(_input)
                || _bindings.InteractPressed(_input)
                || Clicked(mouse, GameUiRenderer.ExitHomeButton)
            )
            {
                _screen = GameScreen.Playing;
                _audio.PlaySfx("door_close");
            }
            else if (Clicked(mouse, GameUiRenderer.SleepHomeButton))
            {
                RequireState().Sleep();
                SaveGame();
                _sleepFade = 1f;
                _screen = GameScreen.Playing;
                _toasts.Add("You slept in your cozy home.");
            }
        }
    }

    private void DrawGameplay(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        Vector2 camera = _camera.Position;
        RequireWorldRenderer().DrawTerrain(spriteBatch, state, camera, _waterAnimation);
        RequireWorldRenderer().DrawTargetHighlight(spriteBatch, _player.InteractionTarget(), camera);
        RequirePlayerRenderer().Draw(spriteBatch, _player, camera);
        if (_isFishing)
        {
            RequireWorldRenderer().DrawFishingBobber(spriteBatch, _fishingTarget, camera, _fishBiting);
        }
        _particles.Draw(spriteBatch, RequirePixel(), camera);
        RequireWorldRenderer().DrawWaterOverlay(spriteBatch, state.World, camera, _waterAnimation);
        if (_settings.ShowCollisionDebug)
        {
            RequireWorldRenderer()
                .DrawCollisionDebug(
                    spriteBatch,
                    state.World,
                    camera,
                    _player.CollisionBox,
                    _player.InteractionRectangle()
                );
        }
        RequireUi().DrawHud(spriteBatch, state, VirtualMousePosition());
        if (_player.IsSwimming)
        {
            RequireUi().DrawOxygen(spriteBatch, _oxygenSeconds / MaximumOxygenSeconds);
        }

        string? prompt = InteractionPrompt(state);
        if (prompt is not null)
        {
            RequireUi().DrawInteractionPrompt(spriteBatch, prompt, _player.Center, camera);
        }

        if (_screen == GameScreen.Inventory)
        {
            RequireUi().DrawInventory(spriteBatch, state, VirtualMousePosition(), _draggedInventorySlotIndex);
        }
        else if (_screen == GameScreen.Pause)
        {
            RequireUi().DrawPause(spriteBatch, VirtualMousePosition());
        }

        _toasts.Draw(spriteBatch, RequireUi());
        RequireUi().DrawDialogue(spriteBatch, _dialogue);
        RequireUi().DrawFade(spriteBatch, _sleepFade);
    }

    private void UseSelectedAction(GridPosition target)
    {
        GameState state = RequireState();
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
            _toasts.Add("A new day begins.");
            return;
        }

        if (IsHouseEntryTarget(target))
        {
            _screen = GameScreen.HomeInterior;
            _audio.PlaySfx("door_open");
            return;
        }

        if (state.World.GetTile(target).Type == TileType.ShippingBox)
        {
            if (state.Economy.ShipFromInventory(state.Inventory, state.Player.SelectedHotbarIndex, state.Content))
            {
                state.StatusMessage = "Item placed in shipping box.";
                _particles.SpawnPickup(_player.Center);
                _audio.PlaySfx("ship_item");
                _toasts.Add("Item placed in shipping box.", "item");
            }
            else
            {
                _toasts.Add(
                    selectedItem?.Type == ItemType.Tool ? "Tools cannot be shipped." : "Select a sellable item.",
                    "warn"
                );
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
            _toasts.Add("Harvested crop.", "item");
            return;
        }

        if (selectedItem is null)
        {
            state.StatusMessage = "Select a tool or seed.";
            _toasts.Add("Select a tool or seed.", "warn");
            return;
        }

        bool fishing = selectedItem.ToolKind == ToolKind.FishingRod;
        _player.TriggerToolUse(fishing);
        bool acted = selectedItem.ToolKind switch
        {
            ToolKind.Hoe => state.Farming.Till(state.World, target, state.Energy),
            ToolKind.WateringCan => state.Farming.Water(state.World, target, state.Energy),
            ToolKind.FishingRod => StartOrResolveFishing(state, target),
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
            _toasts.Add($"{selectedItem.DisplayName} used.");
        }
        else
        {
            state.StatusMessage = "Nothing happened.";
            _audio.PlaySfx("tool_fail");
            _toasts.Add(InvalidActionMessage(state, target, selectedItem), "warn");
        }
    }

    private bool StartOrResolveFishing(GameState state, GridPosition target)
    {
        if (_isFishing)
        {
            if (!_fishBiting)
            {
                _toasts.Add("Wait for a bite.", "fish");
                return false;
            }

            return TryFish(state);
        }

        bool nearWater =
            state.World.GetTile(target).IsWater
            || NeighborTargets(_player.InteractionTarget()).Any(position => state.World.GetTile(position).IsWater);
        if (!nearWater || !state.Energy.HasEnough(3))
        {
            return false;
        }

        _isFishing = true;
        _fishBiting = false;
        _fishingTimer = 1.2f + Random.Shared.NextSingle() * 1.8f;
        _fishingTarget = state.World.GetTile(target).IsWater
            ? target
            : NeighborTargets(_player.InteractionTarget()).First(position => state.World.GetTile(position).IsWater);
        state.StatusMessage = "Cast your line...";
        _toasts.Add("Cast your line.", "fish");
        return true;
    }

    private bool TryFish(GameState state)
    {
        Vector2 bobberCenter = new(
            _fishingTarget.X * GameConstants.TileSize + GameConstants.TileSize / 2f,
            _fishingTarget.Y * GameConstants.TileSize + GameConstants.TileSize / 2f
        );
        if (Vector2.Distance(_player.Center, bobberCenter) > 48f)
        {
            _isFishing = false;
            _fishBiting = false;
            _toasts.Add("Fishing cancelled.", "fish");
            return false;
        }

        string? fishId = state.Fishing.TryCatchFishAtWater(
            state.World,
            _fishingTarget,
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
        _toasts.Add($"Caught {state.Content.Items[fishId].DisplayName}!", "fish");
        _isFishing = false;
        _fishBiting = false;
        return true;
    }

    private void UpdateFishing(float deltaSeconds)
    {
        if (!_isFishing || _fishBiting)
        {
            return;
        }

        _fishingTimer -= deltaSeconds;
        if (_fishingTimer <= 0)
        {
            _fishBiting = true;
            _toasts.Add("Bite! Press E or click.", "fish");
            _audio.PlaySfx("fish_bite");
        }
    }

    private static IEnumerable<GridPosition> NeighborTargets(GridPosition position)
    {
        yield return position;
        yield return position.Neighbor(Direction.Up);
        yield return position.Neighbor(Direction.Down);
        yield return position.Neighbor(Direction.Left);
        yield return position.Neighbor(Direction.Right);
    }

    private static string InvalidActionMessage(GameState state, GridPosition target, ItemDefinition selectedItem)
    {
        int energyCost =
            state.Content.Tools.Values.FirstOrDefault(tool => tool.ItemId == selectedItem.Id)?.EnergyCost ?? 0;
        if (energyCost > 0 && !state.Energy.HasEnough(energyCost))
        {
            return "Not enough energy.";
        }

        if (selectedItem.Type == ItemType.Seed && state.World.GetTile(target).Type != TileType.Soil)
        {
            return "Seeds need tilled soil.";
        }

        if (selectedItem.ToolKind == ToolKind.WateringCan && state.World.GetCrop(target) is null)
        {
            return "Water planted crops.";
        }

        if (selectedItem.ToolKind == ToolKind.FishingRod)
        {
            return "Cast near water.";
        }

        return "Nothing happened.";
    }

    private string? InteractionPrompt(GameState state)
    {
        GridPosition target = _player.InteractionTarget();
        InventorySlot selectedSlot = state.Inventory[state.Player.SelectedHotbarIndex];
        ItemDefinition? selectedItem = selectedSlot.ItemId is null ? null : state.Content.Items[selectedSlot.ItemId];
        if (IsHouseEntryTarget(target))
        {
            return "Press E to Enter Home";
        }

        return state.World.GetTile(target).Type switch
        {
            TileType.SleepSpot => "Press E to Sleep",
            TileType.ShippingBox => "Press E to Ship",
            TileType.Water when selectedItem?.ToolKind == ToolKind.FishingRod => "Press E to Fish",
            TileType.Water => "Use Rod to Fish",
            TileType.Dirt or TileType.Grass when selectedItem?.ToolKind == ToolKind.Hoe => "Use Hoe to Till",
            TileType.Soil when state.World.GetCrop(target) is null && selectedItem?.Type == ItemType.Seed =>
                "Use Seed to Plant",
            TileType.Soil
                when state.World.GetCrop(target) is not null && selectedItem?.ToolKind == ToolKind.WateringCan =>
                "Water Crop",
            _ => state.World.GetCrop(target) is not null ? "Press E to Harvest" : null,
        };
    }

    private bool IsHouseEntryTarget(GridPosition target)
    {
        return target.Y is >= 11 and <= 13 && target.X is >= 6 and <= 9;
    }

    private void UpdateSwimmingOxygen(GameState state, float deltaSeconds)
    {
        if (_player.IsSwimming)
        {
            _oxygenSeconds = Math.Max(0, _oxygenSeconds - deltaSeconds);
            if (_oxygenSeconds <= 2f && (int)(_oxygenSeconds * 10) % 10 == 0)
            {
                _toasts.Add("Oxygen low!", "warn");
            }

            if (_oxygenSeconds <= 0)
            {
                state.Player.TilePosition = new GridPosition(14, 18);
                state.Player.WorldX = 14 * 16;
                state.Player.WorldY = 18 * 16;
                ResetPlayerFromState();
                _oxygenSeconds = MaximumOxygenSeconds;
                _toasts.Add("You washed ashore.", "warn");
            }
            return;
        }

        _oxygenSeconds = Math.Min(MaximumOxygenSeconds, _oxygenSeconds + deltaSeconds * 2.5f);
    }

    private void UpdateHotbarSelection(GameState state)
    {
        Keys[] keys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9];
        for (int index = 0; index < GameConstants.HotbarSlots; index++)
        {
            if (_input.Pressed(keys[index]))
            {
                state.Player.SelectedHotbarIndex = index;
                _toasts.Add(SelectedItemText(state));
            }
        }
    }

    private bool TrySelectHotbarWithMouse(GameState state)
    {
        if (!_input.LeftClick())
        {
            return false;
        }

        int? hotbarSlot = RequireUi().HotbarSlotAt(VirtualMousePosition());
        if (hotbarSlot is null)
        {
            return false;
        }

        state.Player.SelectedHotbarIndex = hotbarSlot.Value;
        _toasts.Add(SelectedItemText(state));
        _audio.PlaySfx("ui_select");
        return true;
    }

    private string SelectedItemText(GameState state)
    {
        InventorySlot slot = state.Inventory[state.Player.SelectedHotbarIndex];
        return slot.ItemId is null
            ? "Empty slot selected."
            : $"{state.Content.Items[slot.ItemId].DisplayName} selected.";
    }

    private bool CanSprint(GameState state, Vector2 movement, float deltaSeconds)
    {
        if (!_bindings.SprintDown(_input) || movement == Vector2.Zero || state.Energy.CurrentEnergy <= 0)
        {
            _sprintEnergyTimer = 0;
            return false;
        }

        _sprintEnergyTimer += deltaSeconds;
        if (_sprintEnergyTimer >= 0.75f)
        {
            _sprintEnergyTimer = 0;
            if (!state.Energy.Spend(1))
            {
                return false;
            }
        }

        return true;
    }

    private GridPosition? MouseTargetIfInRange()
    {
        Point mouse = VirtualMousePosition();
        Vector2 world = new(mouse.X + _camera.Position.X, mouse.Y + _camera.Position.Y);
        return _player.MouseTarget(world);
    }

    private void HandleInventoryMouse(Point mouse)
    {
        if (!_input.LeftClick())
        {
            return;
        }

        int? slotIndex = GameUiRenderer.InventorySlotAt(mouse);
        if (slotIndex is null)
        {
            _draggedInventorySlotIndex = null;
            return;
        }

        GameState state = RequireState();
        if (_draggedInventorySlotIndex is null)
        {
            if (slotIndex.Value < GameConstants.HotbarSlots)
            {
                state.Player.SelectedHotbarIndex = slotIndex.Value;
            }

            if (!state.Inventory[slotIndex.Value].IsEmpty)
            {
                _draggedInventorySlotIndex = slotIndex.Value;
                _audio.PlaySfx("ui_pickup");
            }

            return;
        }

        state.Inventory.MoveOrMerge(_draggedInventorySlotIndex.Value, slotIndex.Value, state.Content.Items);
        if (slotIndex.Value < GameConstants.HotbarSlots)
        {
            state.Player.SelectedHotbarIndex = slotIndex.Value;
        }

        _draggedInventorySlotIndex = null;
        _audio.PlaySfx("ui_drop");
    }

    private void ChangeMusicVolume(float delta)
    {
        _settings.MusicVolume = Math.Clamp(_settings.MusicVolume + delta, 0f, 1f);
        _audio.SetMusicVolume(_settings.MusicVolume);
        _settings.Save();
    }

    private void ChangeSfxVolume(float delta)
    {
        _settings.SfxVolume = Math.Clamp(_settings.SfxVolume + delta, 0f, 1f);
        _audio.SetSfxVolume(_settings.SfxVolume);
        _settings.Save();
    }

    private void ChangeWindowScale(int delta)
    {
        _settings.WindowScale = Math.Clamp(_settings.WindowScale + delta, 1, 4);
        ApplySettings();
        _settings.Save();
    }

    private void ApplySettings()
    {
        _audio.SetMusicVolume(_settings.MusicVolume);
        _audio.SetSfxVolume(_settings.SfxVolume);
        _graphics.PreferredBackBufferWidth = GameConstants.VirtualWidth * _settings.WindowScale;
        _graphics.PreferredBackBufferHeight = GameConstants.VirtualHeight * _settings.WindowScale;
        _graphics.IsFullScreen = _settings.Fullscreen;
        _graphics.ApplyChanges();
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
        _toasts.Add("Game saved.");
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
                _settings.WindowScale,
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
