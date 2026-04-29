#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore
dotnet restore QuietValley.sln
dotnet csharpier check .
dotnet build src/QuietValley.Game/QuietValley.Game.csproj --no-restore
dotnet test tests/QuietValley.SmokeTests/QuietValley.SmokeTests.csproj --no-restore --collect:"XPlat Code Coverage"
