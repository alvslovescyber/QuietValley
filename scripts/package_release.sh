#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/src/QuietValley.Game/QuietValley.Game.csproj"
TEST_PROJECT="$ROOT/tests/QuietValley.SmokeTests/QuietValley.SmokeTests.csproj"
PUBLISH_DIR="$ROOT/artifacts/publish"
RELEASE_DIR="$ROOT/artifacts/release"
MAC_APP_NAME="QuietValley"
MAC_EXECUTABLE_NAME="QuietValley.Game"
MAC_BUNDLE_IDENTIFIER="${MAC_BUNDLE_IDENTIFIER:-com.quietvalley.game}"
if [[ "${GITHUB_REF_NAME:-}" =~ ^v[0-9]+\.[0-9] ]]; then
    MAC_VERSION="${GITHUB_REF_NAME#v}"
else
    MAC_VERSION="${MAC_VERSION:-0.0.0}"
fi
MAC_BUNDLE_VERSION="${MAC_BUNDLE_VERSION:-$MAC_VERSION}"

cd "$ROOT"

rm -rf "$PUBLISH_DIR" "$RELEASE_DIR"
mkdir -p "$PUBLISH_DIR" "$RELEASE_DIR"

dotnet tool restore
dotnet restore QuietValley.sln
dotnet csharpier check . --ignore-path .csharpierignore --no-msbuild-check
dotnet build "$PROJECT" --configuration Release --no-restore
dotnet test "$TEST_PROJECT" --configuration Release --no-restore --collect:"XPlat Code Coverage" --logger trx

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

create_macos_app_bundle() {
    local runtime="$1"
    local bundle_root="$PUBLISH_DIR/$runtime-app"
    local app_path="$bundle_root/$MAC_APP_NAME.app"
    local contents_path="$app_path/Contents"
    local macos_path="$contents_path/MacOS"
    local resources_path="$contents_path/Resources"
    local entitlements_path="$bundle_root/QuietValley.entitlements.plist"

    rm -rf "$bundle_root"
    mkdir -p "$macos_path" "$resources_path"
    rsync -a "$PUBLISH_DIR/$runtime/" "$macos_path/"
    chmod +x "$macos_path/$MAC_EXECUTABLE_NAME"

    cat > "$contents_path/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>$MAC_APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>$MAC_EXECUTABLE_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>$MAC_BUNDLE_IDENTIFIER</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$MAC_APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>$MAC_BUNDLE_VERSION</string>
    <key>CFBundleVersion</key>
    <string>$MAC_BUNDLE_VERSION</string>
    <key>LSApplicationCategoryType</key>
    <string>public.app-category.games</string>
    <key>LSMinimumSystemVersion</key>
    <string>12.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST
    printf "APPL????" > "$contents_path/PkgInfo"
    cat > "$entitlements_path" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
</dict>
</plist>
PLIST

    if command -v codesign >/dev/null 2>&1; then
        if [[ -n "${MACOS_CODESIGN_IDENTITY:-}" ]]; then
            codesign \
                --force \
                --deep \
                --options runtime \
                --timestamp \
                --entitlements "$entitlements_path" \
                --sign "$MACOS_CODESIGN_IDENTITY" \
                "$app_path"
        else
            codesign --force --deep --sign - --timestamp=none "$app_path"
        fi
    fi

    (cd "$bundle_root" && zip -qry "$RELEASE_DIR/QuietValley-$runtime.zip" "$MAC_APP_NAME.app")

    if command -v hdiutil >/dev/null 2>&1; then
        local dmg_tmp dmg_rw dmg_out size_mb dev
        dmg_tmp="$(mktemp -d)"
        dmg_rw="$dmg_tmp/QuietValley-$runtime-rw.dmg"
        dmg_out="$RELEASE_DIR/QuietValley-$runtime.dmg"
        size_mb=$(( $(du -sm "$app_path" | awk '{print $1}') + 32 ))

        # Create a writable intermediate image
        hdiutil create \
            -megabytes "$size_mb" \
            -volname "$MAC_APP_NAME" \
            -fs HFS+ \
            -format UDRW \
            -ov \
            -o "${dmg_rw%.dmg}"

        # Mount, copy app + Applications symlink, then unmount cleanly
        dev="$(hdiutil attach "$dmg_rw" -readwrite -noverify -nobrowse \
                | grep '/Volumes/' | awk '{print $NF}')"
        [[ -n "$dev" ]] || { echo "ERROR: hdiutil attach produced no mount point for $dmg_rw" >&2; exit 1; }
        cp -R "$app_path" "$dev/"
        ln -s /Applications "$dev/Applications"
        hdiutil detach "$dev" -quiet

        # Convert to compressed read-only and verify integrity
        hdiutil convert "$dmg_rw" \
            -format UDZO \
            -imagekey zlib-level=9 \
            -o "${dmg_out%.dmg}" \
            -ov
        rm -rf "$dmg_tmp"

        hdiutil verify "$dmg_out"
        notarize_dmg_if_configured "$dmg_out"
    fi
}

notarize_dmg_if_configured() {
    local dmg_path="$1"
    if [[
        -z "${MACOS_CODESIGN_IDENTITY:-}"
        || -z "${APPLE_ID:-}"
        || -z "${APPLE_TEAM_ID:-}"
        || -z "${APPLE_APP_SPECIFIC_PASSWORD:-}"
    ]]; then
        echo "Skipping notarization for $dmg_path because Apple signing credentials are not configured."
        return
    fi

    if ! command -v xcrun >/dev/null 2>&1; then
        echo "Skipping notarization for $dmg_path because xcrun is unavailable."
        return
    fi

    xcrun notarytool submit "$dmg_path" \
        --apple-id "$APPLE_ID" \
        --team-id "$APPLE_TEAM_ID" \
        --password "$APPLE_APP_SPECIFIC_PASSWORD" \
        --wait
    xcrun stapler staple "$dmg_path"
}

for runtime in osx-arm64 osx-x64; do
    create_macos_app_bundle "$runtime"
done

(cd "$PUBLISH_DIR" && zip -qr "$RELEASE_DIR/QuietValley-win-x64.zip" "win-x64")

(cd "$RELEASE_DIR" && shasum -a 256 -- * > SHA256SUMS.txt)
ls -lh "$RELEASE_DIR"
