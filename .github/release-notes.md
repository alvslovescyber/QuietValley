Self-contained QuietValley builds for macOS and Windows.

**macOS (Apple Silicon / Intel):** Download the `osx-arm64` or `osx-x64` ZIP, unzip it, and run `QuietValley.app` directly — or open the DMG, drag `QuietValley.app` to Applications, and eject the disk.

> **Gatekeeper warning:** macOS may block the app on first launch because this build is not notarized by Apple. If you see "Apple cannot verify" or "damaged and can't be opened", run the following command in Terminal after unzipping/installing, then try again:
>
> ```
> xattr -dr com.apple.quarantine QuietValley.app
> ```
>
> Alternatively, right-click the app → Open → Open.

**Windows (x64):** Download the `win-x64` ZIP, open the `win-x64` folder, and run `QuietValley.Game.exe`.
