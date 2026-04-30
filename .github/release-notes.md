Self-contained QuietValley builds for macOS and Windows.

**macOS (Apple Silicon / Intel):** Download `QuietValley-osx-arm64.zip` (M1+) or `QuietValley-osx-x64.zip` (Intel), unzip it, and run `QuietValley.app` — or download the matching `.dmg`, double-click to mount it, drag `QuietValley.app` onto the `Applications` shortcut, then eject the disk and launch from Applications.

> **First-launch warning:** This build is not notarized by Apple, so macOS Gatekeeper may say "Apple cannot verify" or "QuietValley.app is damaged and can't be opened". The app is fine — macOS just doesn't trust it yet. Pick one:
>
> - **Easiest:** right-click `QuietValley.app` → **Open** → confirm **Open** in the dialog. Only needed once.
> - **If "damaged" persists:** open Terminal and run the command matching where you put it, then launch normally:
>
>   ```
>   xattr -dr com.apple.quarantine /Applications/QuietValley.app
>   # …or, if you didn't move it to Applications:
>   xattr -dr com.apple.quarantine ~/Downloads/QuietValley.app
>   ```
>
> The DMG also includes an `INSTALL.txt` with the same instructions.

**Windows (x64):** Download `QuietValley-win-x64.zip`, extract it, open the `QuietValley` folder, and run `QuietValley.Game.exe`. SmartScreen may warn the first time — click **More info** → **Run anyway**.

**Verify your download (optional):** `SHA256SUMS.txt` is published alongside the binaries. On macOS/Linux: `shasum -a 256 -c SHA256SUMS.txt`. On Windows: `certutil -hashfile QuietValley-win-x64.zip SHA256` and compare.
