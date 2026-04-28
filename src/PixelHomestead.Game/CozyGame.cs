using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;
using PixelHomestead.Game.UI;

namespace PixelHomestead.Game;

public sealed class CozyGame : Microsoft.Xna.Framework.Game
{
    private const int TileSize = 16;
    private const int VirtualWidth = 640;
    private const int VirtualHeight = 360;
    private const int HotbarSlots = 9;

    private readonly GraphicsDeviceManager _graphics;
    private readonly InputState _input = new();
    private SpriteBatch? _spriteBatch;
    private RenderTarget2D? _renderTarget;
    private Texture2D? _pixel;
    private PixelFont? _font;
    private ContentDatabase? _content;
    private GameState? _state;
    private SaveManager? _saveManager;
    private GameScreen _screen = GameScreen.MainMenu;
    private GameScreen _returnFromSettings = GameScreen.MainMenu;
    private Vector2 _camera;
    private Vector2 _playerVisualPosition;
    private double _moveCooldown;
    private double _waterAnimation;
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
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _font = new PixelFont(_pixel);

        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        _content = ContentDatabase.Load(dataDirectory);
        _saveManager = new SaveManager();
        _state = new GameState(_content);
        _playerVisualPosition = TileToWorld(_state.Player.TilePosition);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        double elapsedSeconds = gameTime.ElapsedGameTime.TotalSeconds;
        _waterAnimation += elapsedSeconds;

        if (_screen == GameScreen.Playing)
        {
            UpdatePlaying(elapsedSeconds);
        }
        else
        {
            UpdateMenus();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_spriteBatch is null || _renderTarget is null || _pixel is null)
        {
            return;
        }

        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Palette.Sky);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        switch (_screen)
        {
            case GameScreen.MainMenu:
                DrawMainMenu(_spriteBatch);
                break;
            case GameScreen.Credits:
                DrawCredits(_spriteBatch);
                break;
            case GameScreen.Settings:
                DrawSettings(_spriteBatch);
                break;
            default:
                DrawWorld(_spriteBatch);
                DrawHud(_spriteBatch);
                if (_screen == GameScreen.Inventory)
                {
                    DrawInventory(_spriteBatch);
                }
                else if (_screen == GameScreen.Pause)
                {
                    DrawPause(_spriteBatch);
                }

                break;
        }

        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Rectangle destination = GetScaledDestination();
        _spriteBatch.Draw(_renderTarget, destination, Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdatePlaying(double elapsedSeconds)
    {
        GameState state = RequireState();
        state.Time.Update(elapsedSeconds);

        if (_input.Pressed(Keys.Escape))
        {
            _screen = GameScreen.Pause;
            return;
        }

        if (_input.Pressed(Keys.Tab) || _input.Pressed(Keys.I))
        {
            _screen = GameScreen.Inventory;
            return;
        }

        Keys[] hotbarKeys =
        [
            Keys.D1,
            Keys.D2,
            Keys.D3,
            Keys.D4,
            Keys.D5,
            Keys.D6,
            Keys.D7,
            Keys.D8,
            Keys.D9,
        ];
        for (int hotbarIndex = 0; hotbarIndex < HotbarSlots; hotbarIndex++)
        {
            if (_input.Pressed(hotbarKeys[hotbarIndex]))
            {
                state.Player.SelectedHotbarIndex = hotbarIndex;
            }
        }

        _moveCooldown = Math.Max(0, _moveCooldown - elapsedSeconds);
        if (_moveCooldown <= 0)
        {
            Direction? direction = ReadMovementDirection();
            if (direction.HasValue)
            {
                state.Player.Facing = direction.Value;
                GridPosition nextPosition = state.Player.TilePosition.Neighbor(direction.Value);
                if (!state.World.BlocksMovement(nextPosition))
                {
                    state.Player.TilePosition = nextPosition;
                    _moveCooldown = 0.11;
                }
            }
        }

        if (_input.Pressed(Keys.E) || _input.LeftClick())
        {
            UseSelectedAction();
        }

        Vector2 targetPlayerPosition = TileToWorld(state.Player.TilePosition);
        _playerVisualPosition = Vector2.Lerp(_playerVisualPosition, targetPlayerPosition, 0.35f);

        Vector2 targetCamera =
            _playerVisualPosition
            - new Vector2(VirtualWidth / 2f - TileSize / 2f, VirtualHeight / 2f - TileSize / 2f);
        float maxCameraX = state.World.Width * TileSize - VirtualWidth;
        float maxCameraY = state.World.Height * TileSize - VirtualHeight;
        targetCamera.X = Math.Clamp(targetCamera.X, 0, Math.Max(0, maxCameraX));
        targetCamera.Y = Math.Clamp(targetCamera.Y, 0, Math.Max(0, maxCameraY));
        _camera = Vector2.Lerp(_camera, targetCamera, 0.15f);
    }

    private void UpdateMenus()
    {
        if (_screen == GameScreen.MainMenu)
        {
            if (ClickedButton(new Rectangle(250, 142, 140, 28)))
            {
                StartNewGame();
            }
            else if (ClickedButton(new Rectangle(250, 178, 140, 28)))
            {
                LoadGame();
            }
            else if (ClickedButton(new Rectangle(250, 214, 140, 28)))
            {
                _returnFromSettings = GameScreen.MainMenu;
                _screen = GameScreen.Settings;
            }
            else if (ClickedButton(new Rectangle(250, 250, 140, 28)))
            {
                _screen = GameScreen.Credits;
            }
            else if (ClickedButton(new Rectangle(250, 286, 140, 28)))
            {
                Exit();
            }
        }
        else if (_screen == GameScreen.Pause)
        {
            if (_input.Pressed(Keys.Escape) || ClickedButton(new Rectangle(250, 122, 140, 28)))
            {
                _screen = GameScreen.Playing;
            }
            else if (ClickedButton(new Rectangle(250, 158, 140, 28)))
            {
                SaveGame();
                _screen = GameScreen.Playing;
            }
            else if (ClickedButton(new Rectangle(250, 194, 140, 28)))
            {
                _returnFromSettings = GameScreen.Pause;
                _screen = GameScreen.Settings;
            }
            else if (ClickedButton(new Rectangle(250, 230, 140, 28)))
            {
                _screen = GameScreen.MainMenu;
            }
            else if (ClickedButton(new Rectangle(250, 266, 140, 28)))
            {
                Exit();
            }
        }
        else if (_screen == GameScreen.Inventory)
        {
            if (
                _input.Pressed(Keys.Escape)
                || _input.Pressed(Keys.Tab)
                || _input.Pressed(Keys.I)
                || _input.RightClick()
            )
            {
                _screen = GameScreen.Playing;
            }
        }
        else if (_screen == GameScreen.Settings)
        {
            if (_input.Pressed(Keys.Escape) || ClickedButton(new Rectangle(250, 266, 140, 28)))
            {
                _screen = _returnFromSettings;
            }
            else if (ClickedButton(new Rectangle(224, 158, 56, 24)))
            {
                _settingsScale = 2;
            }
            else if (ClickedButton(new Rectangle(292, 158, 56, 24)))
            {
                _settingsScale = 3;
            }
            else if (ClickedButton(new Rectangle(360, 158, 56, 24)))
            {
                _settingsScale = 4;
            }
        }
        else if (
            _screen == GameScreen.Credits
            && (_input.Pressed(Keys.Escape) || _input.LeftClick() || _input.RightClick())
        )
        {
            _screen = GameScreen.MainMenu;
        }
    }

    private void UseSelectedAction()
    {
        GameState state = RequireState();
        GridPosition target = state.Player.TilePosition.Neighbor(state.Player.Facing);
        InventorySlot selectedSlot = state.Inventory[state.Player.SelectedHotbarIndex];
        ItemDefinition? selectedItem = selectedSlot.ItemId is null
            ? null
            : state.Content.Items.GetValueOrDefault(selectedSlot.ItemId);

        if (state.World.GetTile(target).Type == TileType.SleepSpot)
        {
            state.Sleep();
            SaveGame();
            return;
        }

        if (state.World.GetTile(target).Type == TileType.ShippingBox)
        {
            if (state.Economy.ShipFromInventory(state.Inventory, state.Player.SelectedHotbarIndex))
            {
                state.StatusMessage = "Item placed in shipping box.";
            }
            return;
        }

        if (
            state.World.GetCrop(target) is not null
            && state.Farming.Harvest(state.World, target, state.Inventory, state.Content)
        )
        {
            state.StatusMessage = "Harvested crop.";
            return;
        }

        if (selectedItem is null)
        {
            state.StatusMessage = "Select a tool or seed.";
            return;
        }

        bool acted = selectedItem.ToolKind switch
        {
            ToolKind.Hoe => state.Farming.Till(state.World, target, state.Energy),
            ToolKind.WateringCan => state.Farming.Water(state.World, target, state.Energy),
            ToolKind.FishingRod => TryFish(state),
            ToolKind.Axe => ClearSimpleObstacle(state, target, TileType.Bush, 3),
            ToolKind.Pickaxe => false,
            _ => selectedItem.Type == ItemType.Seed
                && state.Farming.Plant(
                    state.World,
                    target,
                    state.Inventory,
                    selectedItem.Id,
                    state.Content
                ),
        };

        state.StatusMessage = acted ? $"{selectedItem.DisplayName} used." : "Nothing happened.";
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
        return true;
    }

    private static bool ClearSimpleObstacle(
        GameState state,
        GridPosition target,
        TileType obstacleType,
        int energyCost
    )
    {
        if (state.World.GetTile(target).Type != obstacleType || !state.Energy.Spend(energyCost))
        {
            return false;
        }

        state.World.SetTile(target, TileType.Grass);
        return true;
    }

    private Direction? ReadMovementDirection()
    {
        if (_input.Down(Keys.W) || _input.Down(Keys.Up))
        {
            return Direction.Up;
        }

        if (_input.Down(Keys.S) || _input.Down(Keys.Down))
        {
            return Direction.Down;
        }

        if (_input.Down(Keys.A) || _input.Down(Keys.Left))
        {
            return Direction.Left;
        }

        if (_input.Down(Keys.D) || _input.Down(Keys.Right))
        {
            return Direction.Right;
        }

        return null;
    }

    private void StartNewGame()
    {
        _state = new GameState(RequireContent());
        _playerVisualPosition = TileToWorld(_state.Player.TilePosition);
        _screen = GameScreen.Playing;
    }

    private void LoadGame()
    {
        _state = RequireSaveManager().Load(RequireContent());
        _playerVisualPosition = TileToWorld(_state.Player.TilePosition);
        _screen = GameScreen.Playing;
    }

    private void SaveGame()
    {
        RequireSaveManager().Save(RequireState());
        RequireState().StatusMessage = "Game saved.";
    }

    private void DrawWorld(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        foreach ((GridPosition position, Tile tile) in state.World.Tiles())
        {
            Rectangle destination = WorldRectangle(position);
            if (
                !destination.Intersects(
                    new Rectangle(
                        -TileSize,
                        -TileSize,
                        VirtualWidth + TileSize * 2,
                        VirtualHeight + TileSize * 2
                    )
                )
            )
            {
                continue;
            }

            DrawTile(spriteBatch, destination, tile.Type, position);
        }

        foreach ((GridPosition position, CropState cropState) in state.World.Crops)
        {
            DrawCrop(spriteBatch, WorldRectangle(position), cropState);
        }

        DrawPlayer(spriteBatch);
    }

    private void DrawTile(
        SpriteBatch spriteBatch,
        Rectangle destination,
        TileType type,
        GridPosition position
    )
    {
        Texture2D pixel = RequirePixel();
        Color baseColor = type switch
        {
            TileType.Grass => (
                (position.X * 7 + position.Y * 11) % 5 == 0 ? Palette.GrassLight : Palette.Grass
            ),
            TileType.Dirt => Palette.Dirt,
            TileType.Path => Palette.Path,
            TileType.Water => (
                (position.X + position.Y) % 3 == 0 ? Palette.Water : Palette.WaterDeep
            ),
            TileType.Soil => Palette.Soil,
            TileType.TallGrass => Palette.Grass,
            TileType.Flower => Palette.GrassLight,
            TileType.Tree => Palette.GrassDark,
            TileType.Bush => Palette.GrassDark,
            TileType.Mushroom => Palette.Grass,
            TileType.Stone => Palette.Path,
            TileType.Barrel => Palette.Path,
            TileType.Fence => Palette.Wood,
            TileType.House => Palette.Wood,
            TileType.ShippingBox => Palette.WoodDark,
            TileType.SleepSpot => Palette.Parchment,
            _ => Palette.Grass,
        };

        spriteBatch.Draw(pixel, destination, baseColor);
        DrawGroundDither(spriteBatch, destination, position, type);

        if (type == TileType.Water)
        {
            int waveOffset = ((int)(_waterAnimation * 8) + position.X * 3 + position.Y) % 12;
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + waveOffset, destination.Y + 4, 5, 1),
                Palette.WaterLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y + 10, 7, 1),
                new Color(140, 224, 238)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 12, destination.Y + 12, 2, 2),
                new Color(62, 165, 211)
            );
        }
        else if (type == TileType.Grass)
        {
            DrawGrassTufts(spriteBatch, destination, position, false);
        }
        else if (type == TileType.TallGrass)
        {
            DrawGrassTufts(spriteBatch, destination, position, true);
        }
        else if (type == TileType.Flower)
        {
            DrawGrassTufts(spriteBatch, destination, position, true);
            DrawFlower(spriteBatch, destination.X + 5, destination.Y + 6, Palette.FlowerPink);
            DrawFlower(spriteBatch, destination.X + 10, destination.Y + 10, Palette.FlowerWhite);
        }
        else if (type == TileType.Dirt)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y + 3, 3, 1),
                new Color(211, 151, 70)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 9, destination.Y + 12, 4, 1),
                new Color(130, 81, 40)
            );
        }
        else if (type == TileType.Path)
        {
            DrawPathStones(spriteBatch, destination, position);
        }
        else if (type == TileType.Soil)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y + 3, 12, 2),
                new Color(143, 86, 44)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 8, 14, 2),
                new Color(79, 48, 31)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 3, destination.Y + 13, 10, 1),
                new Color(143, 86, 44)
            );
        }
        else if (type == TileType.Tree)
        {
            DrawTree(spriteBatch, destination);
        }
        else if (type == TileType.Bush)
        {
            DrawBush(spriteBatch, destination);
        }
        else if (type == TileType.Mushroom)
        {
            DrawMushroom(spriteBatch, destination);
        }
        else if (type == TileType.Stone)
        {
            DrawStone(spriteBatch, destination);
        }
        else if (type == TileType.Barrel)
        {
            DrawBarrel(spriteBatch, destination);
        }
        else if (type == TileType.Fence)
        {
            DrawFence(spriteBatch, destination);
        }
        else if (type == TileType.House)
        {
            DrawHouseTile(spriteBatch, destination, position);
        }
        else if (type == TileType.ShippingBox)
        {
            DrawShippingBox(spriteBatch, destination);
        }
        else if (type == TileType.SleepSpot)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 5, 14, 8),
                Palette.ParchmentLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 12, 14, 2),
                Palette.ParchmentDark
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 3, destination.Y + 7, 4, 3),
                new Color(113, 156, 215)
            );
        }
    }

    private void DrawGroundDither(
        SpriteBatch spriteBatch,
        Rectangle destination,
        GridPosition position,
        TileType type
    )
    {
        Texture2D pixel = RequirePixel();
        int seed = position.X * 37 + position.Y * 73;
        if (type is TileType.Grass or TileType.TallGrass or TileType.Flower)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2 + seed % 4, destination.Y + 2 + seed % 5, 1, 1),
                new Color(162, 230, 93)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 10, destination.Y + 11, 1, 1),
                Palette.GrassDark
            );
        }
        else if (type is TileType.Path or TileType.Dirt)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1 + seed % 8, destination.Y + 3, 2, 1),
                Palette.PathLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 11, destination.Y + 9 + seed % 4, 2, 1),
                new Color(165, 116, 58)
            );
        }
    }

    private void DrawGrassTufts(
        SpriteBatch spriteBatch,
        Rectangle destination,
        GridPosition position,
        bool dense
    )
    {
        Texture2D pixel = RequirePixel();
        Color dark = Palette.GrassDark;
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 3, destination.Y + 12, 1, 3), dark);
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 4, destination.Y + 10, 1, 5), dark);
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 11, destination.Y + 5, 1, 4), dark);
        if (dense)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 7, destination.Y + 9, 1, 6),
                new Color(41, 154, 67)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 13, destination.Y + 8, 1, 5),
                new Color(41, 154, 67)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 6, 1, 5),
                new Color(41, 154, 67)
            );
        }
    }

    private void DrawFlower(SpriteBatch spriteBatch, int x, int y, Color petal)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 2, 1, 4), Palette.GrassDark);
        spriteBatch.Draw(pixel, new Rectangle(x, y, 1, 1), petal);
        spriteBatch.Draw(pixel, new Rectangle(x + 2, y, 1, 1), petal);
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 1, 1, 1), Palette.Highlight);
    }

    private void DrawPathStones(
        SpriteBatch spriteBatch,
        Rectangle destination,
        GridPosition position
    )
    {
        Texture2D pixel = RequirePixel();
        Color stone = new(182, 169, 126);
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 2, destination.Y + 4, 5, 3), stone);
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 9, destination.Y + 9, 5, 4),
            new Color(164, 151, 111)
        );
        if ((position.X + position.Y) % 2 == 0)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 5, destination.Y + 13, 4, 2),
                stone
            );
        }
    }

    private void DrawTree(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 6, destination.Y + 8, 4, 8),
            Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 7, destination.Y + 8, 2, 8),
            Palette.WoodLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 1, destination.Y + 4, 14, 8),
            Palette.GrassDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 3, destination.Y, 10, 8),
            new Color(30, 139, 75)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 6, destination.Y - 3, 6, 7),
            new Color(59, 177, 83)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 3, destination.Y + 5, 3, 2),
            new Color(87, 206, 89)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 11, destination.Y + 7, 2, 2),
            new Color(23, 108, 61)
        );
    }

    private void DrawBush(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 2, destination.Y + 6, 12, 7),
            new Color(24, 127, 72)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 3, 8, 7),
            new Color(45, 168, 82)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 2, destination.Y + 8, 3, 2),
            new Color(83, 207, 88)
        );
        DrawFlower(spriteBatch, destination.X + 10, destination.Y + 6, Palette.FlowerPink);
    }

    private void DrawMushroom(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 6, destination.Y + 7, 5, 9),
            Palette.MushroomStem
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 2, destination.Y + 3, 13, 6),
            Palette.MushroomCap
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 1, 9, 4),
            new Color(227, 102, 122)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 4, 2, 2),
            Palette.FlowerWhite
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 10, destination.Y + 3, 2, 2),
            Palette.FlowerWhite
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 12, destination.Y + 7, 2, 1),
            new Color(155, 50, 73)
        );
    }

    private void DrawStone(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 3, destination.Y + 7, 10, 6),
            new Color(119, 129, 128)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 5, 7, 4),
            Palette.Rock
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 6, destination.Y + 6, 4, 1),
            new Color(195, 199, 188)
        );
    }

    private void DrawBarrel(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 3, 8, 11),
            Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 3, 6, 11),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 5, 8, 1),
            Palette.ParchmentDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 11, 8, 1),
            Palette.ParchmentDark
        );
    }

    private void DrawFence(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 2, destination.Y + 2, 3, 13),
            Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 11, destination.Y + 2, 3, 13),
            Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 3, destination.Y + 3, 1, 9),
            Palette.WoodLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 12, destination.Y + 3, 1, 9),
            Palette.WoodLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X, destination.Y + 6, 16, 3),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X, destination.Y + 11, 16, 2),
            Palette.WoodLight
        );
    }

    private void DrawHouseTile(
        SpriteBatch spriteBatch,
        Rectangle destination,
        GridPosition position
    )
    {
        Texture2D pixel = RequirePixel();
        bool roof = position.Y <= 9;
        if (roof)
        {
            spriteBatch.Draw(pixel, destination, Palette.RoofRed);
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X, destination.Y + 3, 16, 2),
                new Color(226, 101, 41)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 8, 14, 2),
                new Color(145, 51, 35)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 4, destination.Y + 1, 5, 1),
                new Color(242, 133, 54)
            );
        }
        else
        {
            spriteBatch.Draw(pixel, destination, Palette.Wood);
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y, 2, 16),
                Palette.WoodLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 8, destination.Y, 1, 16),
                Palette.WoodDark
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 1, destination.Y + 5, 14, 1),
                Palette.WoodLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 4, destination.Y + 10, 7, 6),
                Palette.WoodDark
            );
        }
    }

    private void DrawShippingBox(SpriteBatch spriteBatch, Rectangle destination)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 2, destination.Y + 3, 12, 11),
            Palette.WoodDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 3, destination.Y + 4, 10, 9),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 4, destination.Y + 5, 8, 2),
            Palette.Highlight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 9, 6, 1),
            Palette.WoodLight
        );
    }

    private void DrawCrop(SpriteBatch spriteBatch, Rectangle destination, CropState cropState)
    {
        Texture2D pixel = RequirePixel();
        GameState state = RequireState();
        CropDefinition crop = state.Content.Crops[cropState.CropId];
        float growthRatio =
            crop.GrowthDays <= 0 ? 1 : cropState.GrowthProgress / (float)crop.GrowthDays;
        int height =
            growthRatio < 0.34f ? 4
            : growthRatio < 0.67f ? 8
            : 12;
        Color color = growthRatio >= 1 ? new Color(231, 82, 68) : new Color(59, 166, 72);

        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 13, 6, 2),
            new Color(68, 42, 26)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 7, destination.Y + 14 - height, 2, height),
            color
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 5, destination.Y + 12 - height / 2, 6, 3),
            color
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(destination.X + 10, destination.Y + 11 - height / 2, 3, 2),
            new Color(96, 203, 80)
        );
        if (growthRatio >= 1)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 6, destination.Y + 7, 4, 4),
                new Color(244, 113, 76)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 7, destination.Y + 8, 1, 1),
                Palette.Highlight
            );
        }

        if (cropState.WateredToday)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y + 2, 3, 2),
                Palette.WaterLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 12, destination.Y + 4, 2, 1),
                Palette.WaterLight
            );
        }
    }

    private void DrawPlayer(SpriteBatch spriteBatch)
    {
        Texture2D pixel = RequirePixel();
        Rectangle body = new(
            (int)(_playerVisualPosition.X - _camera.X) + 4,
            (int)(_playerVisualPosition.Y - _camera.Y) + 4,
            8,
            11
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(body.X + 2, body.Y + 13, 6, 2),
            new Color(40, 45, 48, 90)
        );
        spriteBatch.Draw(pixel, new Rectangle(body.X, body.Y - 3, 8, 3), new Color(218, 168, 58));
        spriteBatch.Draw(pixel, new Rectangle(body.X + 1, body.Y - 5, 6, 3), Palette.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(body.X + 2, body.Y, 4, 4), new Color(247, 190, 142));
        spriteBatch.Draw(pixel, new Rectangle(body.X + 1, body.Y + 1, 1, 1), new Color(80, 46, 38));
        spriteBatch.Draw(pixel, new Rectangle(body.X + 5, body.Y + 1, 1, 1), new Color(80, 46, 38));
        spriteBatch.Draw(pixel, new Rectangle(body.X, body.Y + 4, 8, 7), new Color(66, 112, 199));
        spriteBatch.Draw(
            pixel,
            new Rectangle(body.X + 1, body.Y + 5, 6, 2),
            new Color(93, 149, 224)
        );
        spriteBatch.Draw(pixel, new Rectangle(body.X + 1, body.Y + 11, 2, 3), Palette.WoodDark);
        spriteBatch.Draw(pixel, new Rectangle(body.X + 5, body.Y + 11, 2, 3), Palette.WoodDark);
    }

    private void DrawHud(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        PixelFont font = RequireFont();
        Texture2D pixel = RequirePixel();

        DrawPanel(spriteBatch, new Rectangle(10, 8, 174, 40));
        DrawLeafBadge(spriteBatch, new Rectangle(20, 17, 16, 16));
        font.Draw(
            spriteBatch,
            $"Energy {state.Energy.CurrentEnergy}/{EnergySystem.MaximumEnergy}",
            new Vector2(42, 17),
            Palette.Text,
            1
        );
        DrawBar(
            spriteBatch,
            new Rectangle(42, 31, 126, 8),
            state.Energy.CurrentEnergy / (float)EnergySystem.MaximumEnergy,
            Palette.Energy
        );

        DrawPanel(spriteBatch, new Rectangle(444, 8, 184, 50));
        DrawSunBadge(spriteBatch, new Rectangle(456, 19, 16, 16));
        font.Draw(
            spriteBatch,
            $"Day {state.Time.Day} {state.Time.Season}",
            new Vector2(480, 17),
            Palette.Text,
            1
        );
        DrawCoin(spriteBatch, 480, 34);
        font.Draw(
            spriteBatch,
            $"{state.Time.ClockText}  {state.Economy.Coins}G",
            new Vector2(496, 34),
            Palette.Text,
            1
        );

        DrawHotbar(spriteBatch);
        DrawInteractionPrompt(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(10, 316, 304, 30));
        DrawTinyFlower(spriteBatch, 22, 326);
        font.Draw(spriteBatch, state.StatusMessage, new Vector2(38, 327), Palette.Text, 1);
    }

    private void DrawHotbar(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        PixelFont font = RequireFont();
        Texture2D pixel = RequirePixel();
        int startX = VirtualWidth / 2 - HotbarSlots * 24 / 2;
        int y = 314;
        DrawPanel(spriteBatch, new Rectangle(startX - 8, y - 8, HotbarSlots * 24 + 14, 38));

        for (int slotIndex = 0; slotIndex < HotbarSlots; slotIndex++)
        {
            Rectangle slotRectangle = new(startX + slotIndex * 24, y, 22, 22);
            bool selected = slotIndex == state.Player.SelectedHotbarIndex;
            DrawSlot(spriteBatch, slotRectangle, selected);
            if (selected)
            {
                spriteBatch.Draw(
                    pixel,
                    new Rectangle(slotRectangle.X + 4, slotRectangle.Y - 3, 14, 2),
                    Palette.Highlight
                );
            }

            InventorySlot slot = state.Inventory[slotIndex];
            if (!slot.IsEmpty && slot.ItemId is not null)
            {
                ItemDefinition item = state.Content.Items[slot.ItemId];
                DrawItemIcon(
                    spriteBatch,
                    item,
                    new Rectangle(slotRectangle.X + 4, slotRectangle.Y + 4, 14, 14)
                );
                if (slot.Quantity > 1)
                {
                    font.Draw(
                        spriteBatch,
                        slot.Quantity.ToString(),
                        new Vector2(slotRectangle.X + 11, slotRectangle.Y + 13),
                        Palette.Text,
                        1
                    );
                }
            }
        }
    }

    private void DrawInventory(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        PixelFont font = RequireFont();
        DrawOverlay(spriteBatch);
        Rectangle panel = new(112, 54, 416, 252);
        DrawPanel(spriteBatch, panel);
        DrawTinyFlower(spriteBatch, 136, 78);
        font.Draw(spriteBatch, "Inventory", new Vector2(156, 76), Palette.Text, 2);
        DrawCoin(spriteBatch, 392, 82);
        font.Draw(spriteBatch, $"{state.Economy.Coins}G", new Vector2(408, 82), Palette.Text, 1);

        for (int slotIndex = 0; slotIndex < state.Inventory.Capacity; slotIndex++)
        {
            int column = slotIndex % 9;
            int row = slotIndex / 9;
            Rectangle slotRectangle = new(136 + column * 28, 112 + row * 28, 24, 24);
            DrawSlot(spriteBatch, slotRectangle, slotIndex == state.Player.SelectedHotbarIndex);
            InventorySlot slot = state.Inventory[slotIndex];
            if (!slot.IsEmpty && slot.ItemId is not null)
            {
                ItemDefinition item = state.Content.Items[slot.ItemId];
                DrawItemIcon(
                    spriteBatch,
                    item,
                    new Rectangle(slotRectangle.X + 5, slotRectangle.Y + 5, 14, 14)
                );
                if (slot.Quantity > 1)
                {
                    font.Draw(
                        spriteBatch,
                        slot.Quantity.ToString(),
                        new Vector2(slotRectangle.X + 12, slotRectangle.Y + 16),
                        Palette.Text,
                        1
                    );
                }
            }
        }

        InventorySlot selected = state.Inventory[state.Player.SelectedHotbarIndex];
        if (!selected.IsEmpty && selected.ItemId is not null)
        {
            ItemDefinition item = state.Content.Items[selected.ItemId];
            font.Draw(spriteBatch, item.DisplayName, new Vector2(136, 238), Palette.Text, 1);
            font.Draw(spriteBatch, item.Description, new Vector2(136, 254), Palette.Text, 1);
        }

        font.Draw(
            spriteBatch,
            "Tab/Esc closes. 1-9 selects hotbar.",
            new Vector2(136, 280),
            Palette.Text,
            1
        );
    }

    private void DrawPause(SpriteBatch spriteBatch)
    {
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(220, 78, 200, 232));
        RequireFont().Draw(spriteBatch, "Paused", new Vector2(284, 96), Palette.Text, 2);
        DrawButton(spriteBatch, new Rectangle(250, 122, 140, 28), "Resume");
        DrawButton(spriteBatch, new Rectangle(250, 158, 140, 28), "Save Game");
        DrawButton(spriteBatch, new Rectangle(250, 194, 140, 28), "Settings");
        DrawButton(spriteBatch, new Rectangle(250, 230, 140, 28), "Main Menu");
        DrawButton(spriteBatch, new Rectangle(250, 266, 140, 28), "Quit");
    }

    private void DrawMainMenu(SpriteBatch spriteBatch)
    {
        DrawTitleBackground(spriteBatch);

        PixelFont font = RequireFont();
        DrawPanel(spriteBatch, new Rectangle(184, 46, 272, 70));
        DrawTinyFlower(spriteBatch, 210, 74);
        DrawTinyFlower(spriteBatch, 420, 74);
        font.Draw(spriteBatch, "Pixel Homestead", new Vector2(220, 70), Palette.Text, 2);
        font.Draw(spriteBatch, "A cozy farm life", new Vector2(260, 96), Palette.Text, 1);
        DrawButton(spriteBatch, new Rectangle(250, 142, 140, 28), "New Game");
        DrawButton(spriteBatch, new Rectangle(250, 178, 140, 28), "Load Game");
        DrawButton(spriteBatch, new Rectangle(250, 214, 140, 28), "Settings");
        DrawButton(spriteBatch, new Rectangle(250, 250, 140, 28), "Credits");
        DrawButton(spriteBatch, new Rectangle(250, 286, 140, 28), "Quit");
    }

    private void DrawSettings(SpriteBatch spriteBatch)
    {
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(180, 88, 280, 216));
        PixelFont font = RequireFont();
        font.Draw(spriteBatch, "Settings", new Vector2(268, 110), Palette.Text, 2);
        font.Draw(spriteBatch, "Pixel scale", new Vector2(224, 140), Palette.Text, 1);
        DrawButton(spriteBatch, new Rectangle(224, 158, 56, 24), "2x");
        DrawButton(spriteBatch, new Rectangle(292, 158, 56, 24), "3x");
        DrawButton(spriteBatch, new Rectangle(360, 158, 56, 24), "4x");
        font.Draw(
            spriteBatch,
            $"Current {_settingsScale}x",
            new Vector2(224, 196),
            Palette.Text,
            1
        );
        font.Draw(
            spriteBatch,
            "Audio hooks are ready for assets.",
            new Vector2(224, 222),
            Palette.Text,
            1
        );
        DrawButton(spriteBatch, new Rectangle(250, 266, 140, 28), "Back");
    }

    private void DrawCredits(SpriteBatch spriteBatch)
    {
        DrawMainMenu(spriteBatch);
        DrawOverlay(spriteBatch);
        DrawPanel(spriteBatch, new Rectangle(140, 116, 360, 128));
        PixelFont font = RequireFont();
        font.Draw(spriteBatch, "Credits", new Vector2(276, 136), Palette.Text, 2);
        font.Draw(
            spriteBatch,
            "Original placeholder pixel art generated in code.",
            new Vector2(166, 172),
            Palette.Text,
            1
        );
        font.Draw(
            spriteBatch,
            "No copyrighted game assets included.",
            new Vector2(166, 190),
            Palette.Text,
            1
        );
        font.Draw(spriteBatch, "Click or Esc to return.", new Vector2(230, 220), Palette.Text, 1);
    }

    private void DrawInteractionPrompt(SpriteBatch spriteBatch)
    {
        GameState state = RequireState();
        GridPosition target = state.Player.TilePosition.Neighbor(state.Player.Facing);
        string? prompt = state.World.GetTile(target).Type switch
        {
            TileType.SleepSpot => "Press E to Sleep",
            TileType.ShippingBox => "Press E to Ship",
            TileType.Water => "Use rod to Fish",
            TileType.Soil when state.World.GetCrop(target) is null => "Use seed to Plant",
            _ => state.World.GetCrop(target) is not null ? "Press E to Harvest" : null,
        };

        if (prompt is null)
        {
            return;
        }

        Vector2 promptPosition =
            TileToWorld(state.Player.TilePosition) - _camera + new Vector2(-30, -18);
        DrawPanel(
            spriteBatch,
            new Rectangle((int)promptPosition.X, (int)promptPosition.Y, 112, 18)
        );
        RequireFont()
            .Draw(spriteBatch, prompt, promptPosition + new Vector2(6, 6), Palette.Text, 1);
    }

    private void DrawPanel(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 4, rectangle.Y + 4, rectangle.Width, rectangle.Height),
            Palette.PanelShadow
        );
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 2,
                rectangle.Y + 2,
                rectangle.Width - 4,
                rectangle.Height - 4
            ),
            Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 5,
                rectangle.Y + 5,
                rectangle.Width - 10,
                rectangle.Height - 10
            ),
            Palette.ParchmentDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 8,
                rectangle.Y + 8,
                rectangle.Width - 16,
                rectangle.Height - 16
            ),
            Palette.Parchment
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 10, rectangle.Y + 10, rectangle.Width - 20, 1),
            Palette.ParchmentLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Y + 5, 4, 4),
            Palette.Highlight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.Right - 9, rectangle.Y + 5, 4, 4),
            Palette.Highlight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Bottom - 9, 4, 4),
            Palette.WoodLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.Right - 9, rectangle.Bottom - 9, 4, 4),
            Palette.WoodLight
        );
    }

    private void DrawButton(SpriteBatch spriteBatch, Rectangle rectangle, string label)
    {
        Texture2D pixel = RequirePixel();
        PixelFont font = RequireFont();
        bool hovered = MouseInVirtualRectangle(rectangle);
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 3, rectangle.Width, rectangle.Height),
            Palette.PanelShadow
        );
        spriteBatch.Draw(pixel, rectangle, hovered ? Palette.Highlight : Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 2,
                rectangle.Y + 2,
                rectangle.Width - 4,
                rectangle.Height - 4
            ),
            hovered ? new Color(246, 198, 102) : Palette.Wood
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Y + 5, rectangle.Width - 10, 1),
            hovered ? Palette.ParchmentLight : Palette.WoodLight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 6,
                rectangle.Y + rectangle.Height - 6,
                rectangle.Width - 12,
                2
            ),
            Palette.WoodDark
        );
        int textWidth = font.MeasureWidth(label, 1);
        font.Draw(
            spriteBatch,
            label,
            new Vector2(rectangle.Center.X - textWidth / 2, rectangle.Y + 10),
            Palette.Text,
            1
        );
    }

    private void DrawSlot(SpriteBatch spriteBatch, Rectangle rectangle, bool selected)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, rectangle, selected ? Palette.Highlight : Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 2,
                rectangle.Y + 2,
                rectangle.Width - 4,
                rectangle.Height - 4
            ),
            selected ? Palette.ParchmentLight : Palette.Parchment
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 3,
                rectangle.Y + rectangle.Height - 4,
                rectangle.Width - 6,
                1
            ),
            Palette.ParchmentDark
        );
    }

    private void DrawItemIcon(SpriteBatch spriteBatch, ItemDefinition item, Rectangle rectangle)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, rectangle, new Color(74, 51, 37, 35));
        if (item.Type == ItemType.Seed)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 4, rectangle.Y + 6, 6, 5),
                new Color(126, 82, 36)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 7, rectangle.Y + 3, 3, 4),
                new Color(72, 172, 72)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 9, rectangle.Y + 4, 3, 2),
                new Color(110, 210, 91)
            );
            return;
        }

        if (item.Type == ItemType.Crop)
        {
            Color cropColor =
                item.Id.Contains("carrot", StringComparison.Ordinal) ? new Color(238, 126, 45)
                : item.Id.Contains("turnip", StringComparison.Ordinal) ? new Color(239, 230, 218)
                : item.Id.Contains("potato", StringComparison.Ordinal) ? new Color(181, 126, 64)
                : new Color(230, 73, 67);
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 4, rectangle.Y + 5, 7, 7),
                cropColor
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 6, rectangle.Y + 2, 4, 4),
                new Color(65, 176, 73)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 5, rectangle.Y + 6, 2, 1),
                Palette.ParchmentLight
            );
            return;
        }

        if (item.Type == ItemType.Fish)
        {
            Color fishColor = item.Id.Contains("golden", StringComparison.Ordinal)
                ? Palette.Highlight
                : Palette.WaterLight;
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 2, rectangle.Y + 6, 9, 5),
                fishColor
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 10, rectangle.Y + 5, 3, 7),
                Palette.WaterDeep
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 4, rectangle.Y + 7, 1, 1),
                Palette.Text
            );
            return;
        }

        Color color = item.ToolKind switch
        {
            ToolKind.Hoe => Palette.WoodDark,
            ToolKind.WateringCan => Palette.Water,
            ToolKind.Axe => new Color(166, 171, 160),
            ToolKind.Pickaxe => Palette.Rock,
            ToolKind.FishingRod => Palette.WoodLight,
            _ => Palette.Text,
        };

        if (item.ToolKind == ToolKind.Hoe)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 6, rectangle.Y + 2, 2, 11),
                Palette.WoodLight
            );
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 4, rectangle.Y + 2, 6, 2), color);
        }
        else if (item.ToolKind == ToolKind.WateringCan)
        {
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 3, rectangle.Y + 6, 8, 6), color);
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 9, rectangle.Y + 4, 4, 3), color);
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + rectangle.Width - 4, rectangle.Y + 3, 3, 3),
                Palette.WaterLight
            );
        }
        else if (item.ToolKind == ToolKind.FishingRod)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 6, rectangle.Y + 1, 1, rectangle.Height - 2),
                color
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 7, rectangle.Y + 4, 5, 1),
                Palette.TextLight
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 11, rectangle.Y + 7, 2, 2),
                Palette.WaterLight
            );
        }
        else if (item.ToolKind == ToolKind.Axe)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 7, rectangle.Y + 3, 2, 10),
                Palette.WoodLight
            );
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 4, rectangle.Y + 2, 6, 5), color);
        }
        else if (item.ToolKind == ToolKind.Pickaxe)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 7, rectangle.Y + 3, 2, 10),
                Palette.WoodLight
            );
            spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 3, rectangle.Y + 2, 10, 2), color);
        }
    }

    private void DrawTitleBackground(SpriteBatch spriteBatch)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, new Rectangle(0, 0, VirtualWidth, VirtualHeight), Palette.Grass);
        for (int tileY = 0; tileY < VirtualHeight; tileY += 16)
        {
            for (int tileX = 0; tileX < VirtualWidth; tileX += 16)
            {
                GridPosition position = new(tileX / 16, tileY / 16);
                Rectangle tile = new(tileX, tileY, 16, 16);
                spriteBatch.Draw(
                    pixel,
                    tile,
                    (
                        (position.X * 7 + position.Y * 11) % 5 == 0
                            ? Palette.GrassLight
                            : Palette.Grass
                    )
                );
                DrawGroundDither(spriteBatch, tile, position, TileType.Grass);
                if ((position.X + position.Y) % 7 == 0)
                {
                    DrawGrassTufts(spriteBatch, tile, position, false);
                }
            }
        }

        spriteBatch.Draw(pixel, new Rectangle(0, 238, VirtualWidth, 122), Palette.WaterDeep);
        for (int x = 0; x < VirtualWidth; x += 16)
        {
            Rectangle waterTile = new(x, 238 + (x / 16 % 2) * 4, 16, 18);
            spriteBatch.Draw(pixel, waterTile, Palette.Water);
            spriteBatch.Draw(
                pixel,
                new Rectangle(x + 3, waterTile.Y + 8, 8, 1),
                Palette.WaterLight
            );
        }

        spriteBatch.Draw(pixel, new Rectangle(0, 218, VirtualWidth, 28), Palette.Path);
        for (int x = 0; x < VirtualWidth; x += 22)
        {
            DrawPathStones(
                spriteBatch,
                new Rectangle(x, 218, 16, 16),
                new GridPosition(x / 16, 14)
            );
        }

        DrawTitleHouse(spriteBatch, 70, 118);
        DrawWindmill(spriteBatch, 196, 82);
        DrawMushroom(spriteBatch, new Rectangle(468, 178, 64, 64));
        DrawMushroom(spriteBatch, new Rectangle(538, 206, 56, 56));
        DrawTree(spriteBatch, new Rectangle(560, 128, 32, 32));
        DrawBush(spriteBatch, new Rectangle(38, 198, 24, 24));
        DrawBush(spriteBatch, new Rectangle(408, 196, 24, 24));
        DrawFence(spriteBatch, new Rectangle(92, 202, 24, 24));
        DrawFence(spriteBatch, new Rectangle(116, 202, 24, 24));
    }

    private void DrawTitleHouse(SpriteBatch spriteBatch, int x, int y)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, new Rectangle(x, y + 32, 104, 70), Palette.Wood);
        spriteBatch.Draw(pixel, new Rectangle(x + 8, y + 42, 88, 4), Palette.WoodLight);
        spriteBatch.Draw(pixel, new Rectangle(x - 10, y + 16, 124, 28), Palette.RoofRed);
        spriteBatch.Draw(pixel, new Rectangle(x - 4, y + 22, 112, 6), new Color(230, 103, 40));
        spriteBatch.Draw(pixel, new Rectangle(x + 42, y + 62, 22, 40), Palette.WoodDark);
        spriteBatch.Draw(pixel, new Rectangle(x + 14, y + 52, 18, 16), Palette.WaterLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 72, y + 52, 18, 16), Palette.WaterLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 46, y + 80, 3, 3), Palette.Highlight);
    }

    private void DrawWindmill(SpriteBatch spriteBatch, int x, int y)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, new Rectangle(x + 30, y + 76, 44, 92), Palette.Wood);
        spriteBatch.Draw(pixel, new Rectangle(x + 34, y + 82, 36, 4), Palette.WoodLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 22, y + 54, 60, 34), Palette.WoodDark);
        spriteBatch.Draw(pixel, new Rectangle(x + 30, y + 42, 44, 22), Palette.WoodLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 48, y + 20, 8, 72), Palette.ParchmentLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 16, y + 52, 72, 8), Palette.ParchmentLight);
        spriteBatch.Draw(pixel, new Rectangle(x + 48, y + 52, 8, 8), Palette.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(x + 42, y + 126, 20, 42), Palette.WoodDark);
    }

    private void DrawBar(SpriteBatch spriteBatch, Rectangle rectangle, float normalized, Color fill)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 2,
                rectangle.Y + 2,
                rectangle.Width - 4,
                rectangle.Height - 4
            ),
            new Color(80, 57, 42)
        );
        int width = (int)((rectangle.Width - 4) * Math.Clamp(normalized, 0f, 1f));
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 2, rectangle.Y + 2, width, rectangle.Height - 4),
            fill
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 3, rectangle.Y + 3, Math.Max(0, width - 2), 1),
            new Color(184, 252, 123)
        );
    }

    private void DrawLeafBadge(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 2,
                rectangle.Y + 2,
                rectangle.Width - 4,
                rectangle.Height - 4
            ),
            Palette.GrassDark
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 5, rectangle.Y + 4, 7, 5),
            Palette.Energy
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 7, rectangle.Y + 9, 2, 4),
            Palette.Energy
        );
    }

    private void DrawSunBadge(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, rectangle, Palette.WoodDark);
        spriteBatch.Draw(
            pixel,
            new Rectangle(
                rectangle.X + 3,
                rectangle.Y + 3,
                rectangle.Width - 6,
                rectangle.Height - 6
            ),
            Palette.Highlight
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle(rectangle.X + 6, rectangle.Y + 6, 4, 4),
            Palette.PathLight
        );
    }

    private void DrawCoin(SpriteBatch spriteBatch, int x, int y)
    {
        Texture2D pixel = RequirePixel();
        spriteBatch.Draw(pixel, new Rectangle(x, y + 2, 10, 10), new Color(179, 112, 30));
        spriteBatch.Draw(pixel, new Rectangle(x + 1, y + 1, 8, 8), Palette.Highlight);
        spriteBatch.Draw(pixel, new Rectangle(x + 3, y + 3, 3, 2), Palette.PathLight);
    }

    private void DrawTinyFlower(SpriteBatch spriteBatch, int x, int y)
    {
        DrawFlower(spriteBatch, x, y + 2, Palette.FlowerPink);
        DrawFlower(spriteBatch, x + 7, y, Palette.FlowerWhite);
    }

    private void DrawOverlay(SpriteBatch spriteBatch)
    {
        RequirePixel();
        spriteBatch.Draw(
            RequirePixel(),
            new Rectangle(0, 0, VirtualWidth, VirtualHeight),
            new Color(30, 20, 18, 150)
        );
    }

    private Rectangle WorldRectangle(GridPosition position)
    {
        return new Rectangle(
            (int)(position.X * TileSize - _camera.X),
            (int)(position.Y * TileSize - _camera.Y),
            TileSize,
            TileSize
        );
    }

    private static Vector2 TileToWorld(GridPosition position)
    {
        return new Vector2(position.X * TileSize, position.Y * TileSize);
    }

    private bool ClickedButton(Rectangle rectangle)
    {
        return _input.LeftClick() && MouseInVirtualRectangle(rectangle);
    }

    private bool MouseInVirtualRectangle(Rectangle rectangle)
    {
        Point mouse = VirtualMousePosition();
        return rectangle.Contains(mouse);
    }

    private Point VirtualMousePosition()
    {
        Rectangle destination = GetScaledDestination();
        if (destination.Width <= 0 || destination.Height <= 0)
        {
            return Point.Zero;
        }

        int x = (int)(
            (_input.CurrentMouse.X - destination.X) * (VirtualWidth / (float)destination.Width)
        );
        int y = (int)(
            (_input.CurrentMouse.Y - destination.Y) * (VirtualHeight / (float)destination.Height)
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
                    GraphicsDevice.PresentationParameters.BackBufferWidth / VirtualWidth,
                    GraphicsDevice.PresentationParameters.BackBufferHeight / VirtualHeight
                )
            )
        );
        int width = VirtualWidth * scale;
        int height = VirtualHeight * scale;
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

    private GameState? RequireStateOrNull()
    {
        return _state;
    }

    private SaveManager RequireSaveManager()
    {
        return _saveManager
            ?? throw new InvalidOperationException("Save manager is not initialized.");
    }

    private Texture2D RequirePixel()
    {
        return _pixel ?? throw new InvalidOperationException("Pixel texture is not loaded.");
    }

    private PixelFont RequireFont()
    {
        return _font ?? throw new InvalidOperationException("Pixel font is not loaded.");
    }
}
