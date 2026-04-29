#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/src/QuietValley.Game/QuietValley.Game.csproj"
TEST_PROJECT="$ROOT/tests/QuietValley.SmokeTests/QuietValley.SmokeTests.csproj"
PUBLISH_DIR="$ROOT/artifacts/publish"
RELEASE_DIR="$ROOT/artifacts/release"

cd "$ROOT"

dotnet tool restore
dotnet restore QuietValley.sln
dotnet csharpier check .
dotnet build "$PROJECT" --configuration Release --no-restore
dotnet test "$TEST_PROJECT" --configuration Release --no-restore --collect:"XPlat Code Coverage" --logger trx

rm -rf "$PUBLISH_DIR" "$RELEASE_DIR"
mkdir -p "$PUBLISH_DIR" "$RELEASE_DIR"

publish_runtime() {
    local runtime="$1"
    dotnet publish "$PROJECT" \
        --configuration Release \
        --runtime "$runtime" \
        --self-contained true \
        -p:PublishSingleFile=false \
        -p:DebugSymbols=false \
        -p:DebugType=none \
        -o "$PUBLISH_DIR/$runtime"

    find "$PUBLISH_DIR/$runtime" -name "*.pdb" -delete
}

for runtime in osx-arm64 osx-x64 win-x64; do
    publish_runtime "$runtime"
done

for runtime in osx-arm64 osx-x64; do
    zip_name="QuietValley-$runtime.zip"
    (cd "$PUBLISH_DIR" && zip -qr "$RELEASE_DIR/$zip_name" "$runtime")

    if command -v hdiutil >/dev/null 2>&1; then
        dmg_name="QuietValley-$runtime.dmg"
        hdiutil create \
            -volname "QuietValley" \
            -srcfolder "$PUBLISH_DIR/$runtime" \
            -ov \
            -format UDZO \
            "$RELEASE_DIR/$dmg_name" >/dev/null
    fi
done

(cd "$PUBLISH_DIR" && zip -qr "$RELEASE_DIR/QuietValley-win-x64.zip" "win-x64")

shasum -a 256 "$RELEASE_DIR"/* > "$RELEASE_DIR/SHA256SUMS.txt"
ls -lh "$RELEASE_DIR"
