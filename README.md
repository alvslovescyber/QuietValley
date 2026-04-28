# Pixel Homestead

Pixel Homestead is a macOS-first C# cozy farming/life-sim foundation built with MonoGame DesktopGL.

## Run

```bash
dotnet run --project src/PixelHomestead.Game/PixelHomestead.Game.csproj
```

## Validate

```bash
dotnet build src/PixelHomestead.Game/PixelHomestead.Game.csproj
dotnet test tests/PixelHomestead.SmokeTests/PixelHomestead.SmokeTests.csproj
```

## Developer Workflow

```bash
dotnet tool restore
dotnet csharpier format .
dotnet csharpier check .
./scripts/check.sh
```

- VS Code: use `Run and Debug` → `Run Pixel Homestead`.
- JetBrains Rider: use the checked-in `.run/Pixel Homestead.run.xml` run configuration.
- Tests use xUnit with `Microsoft.NET.Test.Sdk` and `coverlet.collector`.
- Formatting uses CSharpier. ESLint is intentionally not configured yet because this repository has no JavaScript or TypeScript surface; add it when a web/tooling package exists.
- CI runs on GitHub Actions for restore, CSharpier, build, and xUnit tests with coverage.

## Controls

- `WASD` / arrow keys: move
- `E` / left click: interact or use selected item
- `Tab` / `I`: inventory
- `Esc`: pause or close menu
- `1-9`: select hotbar slot

## Current Foundation

- Custom pixel-rendered MonoGame window with point-clamped scaling
- Main menu, pause menu, settings screen, credits, HUD, hotbar, and inventory panel
- Bright cozy pixel-art direction with parchment/wood UI, warm button states, hand-drawn-style item icons, flowers, fences, stones, barrels, oversized mushrooms, and animated water details
- Tile-based starter world with house, farm plot, pond, paths, fences, trees, bushes, props, and shipping box
- Player movement, facing direction, smooth camera follow, collision, and prompts
- Data-driven items and crops in JSON
- Inventory stacking, tools, farming, fishing placeholder, day/time, energy, economy, sleep, and JSON save/load

## Architecture

- `src/PixelHomestead.Core/Core`: shared primitives
- `src/PixelHomestead.Core/Player`: player state
- `src/PixelHomestead.Core/World`: tile map, collision, crop state
- `src/PixelHomestead.Core/Items`: item, crop, inventory, and tool models
- `src/PixelHomestead.Core/Systems`: farming, fishing, time, energy, economy, save systems
- `src/PixelHomestead.Core/Data`: JSON data loading
- `src/PixelHomestead.Game`: MonoGame rendering, input, menus, HUD, and app entrypoint
- `src/PixelHomestead.Game/Data`: item and crop definitions copied to output
- `src/PixelHomestead.Game/Assets`: replacement-ready sprite, tile, font, and audio folders

## Save Location

Saves are written to the current user's application data folder under `PixelHomestead/savegame.json`.
