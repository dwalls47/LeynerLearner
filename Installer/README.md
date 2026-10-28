# Installer

Builds a Windows installer for Leyner Learner with [Inno Setup 6](https://jrsoftware.org/isinfo.php) (free).

    powershell -ExecutionPolicy Bypass -File Installer\build.ps1

`build.ps1` publishes the app into `publish\` (self-contained for 64-bit Windows: it includes the .NET runtime, so
users need nothing else installed) and compiles `LeynerLearner.iss` into
`output\LeynerLearner-Setup-<version>.exe`. The version is `<Version>` in `LeynerLearner\LeynerLearner.csproj`;
change it there for each release.

Needs the .NET 10 SDK and Inno Setup 6. To install Inno Setup: <https://jrsoftware.org/isdl.php>, or

    winget install JRSoftware.InnoSetup

The installer:

- installs for the current user (no administrator rights) or, if chosen, for all users in Program Files
- adds a Start menu shortcut, and a desktop shortcut if chosen
- shows the MIT license, and installs `LICENSE` and `README.md`; the data folders carry their own licenses
- uninstalls from Windows Settings > Apps; data updates downloaded with "Update data…"
  (in `%LOCALAPPDATA%\LeynerLearner`) are kept, like other user data
- upgrades in place: its `AppId` must never change

`publish\` and `output\` are build output and are not committed.
