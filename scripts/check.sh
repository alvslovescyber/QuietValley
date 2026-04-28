#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore
dotnet restore PixelHomestead.sln
dotnet csharpier check .
dotnet build src/PixelHomestead.Game/PixelHomestead.Game.csproj --no-restore
dotnet test tests/PixelHomestead.SmokeTests/PixelHomestead.SmokeTests.csproj --no-restore --collect:"XPlat Code Coverage"
