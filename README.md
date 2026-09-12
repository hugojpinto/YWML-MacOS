# YWML (macOS / cross-platform port)

An **unofficial macOS and cross-platform port of [SuperTavor/YWML](https://github.com/SuperTavor/YWML)**, the mod
loader for the **Yo-kai Watch** series on 3DS.

Upstream is a .NET 8 **Windows Forms** app. This fork keeps all of the archive, merge and extension-library logic
byte-for-byte identical and replaces only the UI layer with [Avalonia](https://avaloniaui.net), so the same app runs
on macOS, Linux and Windows.

For how to actually *use* the app (extension library, loading mods, migrating an existing FA mod, making a YWML mod
from scratch) see the [upstream README](https://github.com/SuperTavor/YWML#readme) — the workflow and all the windows
are unchanged.

## Requirements

* .NET SDK 8.0 or newer (`dotnet --version`)
  * macOS: `brew install --cask dotnet-sdk` (needs sudo), or the no-sudo option
    `curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0`, which installs into `~/.dotnet`.
    With that option the SDK is not on `PATH`, so add this to your shell profile (or prefix the commands below):

    ```sh
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
    ```

## Build and run

```sh
dotnet build            # or: dotnet build -c Release
dotnet run
```

To produce a standalone binary:

```sh
dotnet publish -c Release -r osx-arm64 --self-contained false
```

Per-user data (config, downloaded extensions, cached library) lives in:

| OS      | Path                                    |
| ------- | --------------------------------------- |
| macOS   | `~/Library/Application Support/YWML`    |
| Windows | `%APPDATA%\YWML`                        |
| Linux   | `$XDG_CONFIG_HOME/YWML` or `~/.config/YWML` |

## What changed versus upstream

### UI

* **Windows Forms → Avalonia 11.** The four forms were rewritten as Avalonia windows with the same controls,
  labels and behaviour:
  * `MainForm` → `Src/Views/MainWindow.axaml`
  * `LoadForm` → `Src/Views/LoadWindow.axaml`
  * `ExtensionLibraryForm` → `Src/Views/ExtensionLibraryWindow.axaml`
  * `MigrateModForm` → `Src/Views/MigrateModWindow.axaml`
* The loader's mod list was a `TreeView` with flat nodes; it is now a `ListBox` (same ordering semantics: top of the
  list is the most important mod). The extension library keeps a real `TreeView` (categories → extensions).
* `FolderBrowserDialog` → Avalonia's `StorageProvider.OpenFolderPickerAsync`.
* The ~27 `MessageBox.Show` calls go through `Src/Utils/Dialogs/CDialogs.cs`, backed by a small
  `MessageBoxWindow`. Avalonia has no blocking modal dialog, so these are **async**; call sites were awaited
  accordingly (`CConfigManager.InitializeAsync`, `CExtensionLibrary.FetchDataAsync` / `LoadInstalledListAsync`).
* Layout uses Avalonia panels rather than the designer's absolute pixel coordinates, and the Windows-only fonts
  (Yu Gothic UI, Arial Rounded MT, Consolas) fall back to the platform default / `Menlo`.

### De-Windows-ing

* `CGeneralUtils.YWMLDataDir` used `%APPDATA%/YWML`; it now uses
  `Environment.GetFolderPath(SpecialFolder.ApplicationData)` so it resolves correctly on every OS.
* `LoadForm.genModDirBtn_Click` hardcoded `C:/Users/{user}/AppData/Roaming/{platform}/load/mods/{titleId}`.
  `Src/Utils/Platform/CPlatformUtils.cs` now builds that per OS — `~/Library/Application Support/{Platform}/...`
  on macOS, `$XDG_DATA_HOME`/`~/.local/share/{platform}/...` on Linux, the original path on Windows. The
  Modded3DS branch (user picks the SD card root, `/Volumes/...` on macOS) is unchanged apart from stripping
  either kind of trailing separator.
* `CLoader.ModifyFA` filtered raw files with `file.Contains("include\\")`, which never matched on macOS and
  would have copied the whole `include` tree as loose files. It now compares the first path segment relative to
  the mod folder, separator-agnostically.
* `VirtualDirectory.Reorganize` built virtual archive paths with a literal `\` (via `Path.Combine`) and then
  replaced them with `/`. It now uses `/` directly — identical behaviour, but correct on non-Windows.
* `MainForm.configOpenBtn_Click` launched `explorer.exe /select`. Now `open -R` on macOS, `explorer /select` on
  Windows, `xdg-open` of the containing directory on Linux.
* Removed the unused `AllocConsole` `kernel32.dll` `DllImport` from `MigrateModForm` (commented-out debugging).
* Removed the `[STAThread]` / `ApplicationConfiguration.Initialize()` / `Application.Run` entry point in favour of
  the Avalonia `AppBuilder`. `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` is kept, and the
  first-boot / config bootstrap moved from `Program.Main` to `MainWindow.Opened` (unchanged logic) because
  Avalonia dialogs need a running UI thread.
* `System.Windows.Forms.Timer` → `Avalonia.Threading.DispatcherTimer`.

### Non-UI code that had to move

The archive/merge logic is untouched, but three APIs took WinForms types and were refactored to plain data:

* `CLoader.ModifyFA(TreeView, ...)` → `CLoader.ModifyFA(IEnumerable<string> modNamesMostImportantFirst, ...)`.
* `CExtension.InstallAsync/UninstallAsync(List<Button>, Label, Label, ...)` →
  `(Action<bool> setBusy, IProgress<string> status, IProgress<string> percentage, ...)`.
  `Progress<T>` captures the UI `SynchronizationContext`, replacing the old `Label.Invoke` marshalling.
* `CFAMerger.GetDiffs()` showed its own message box on an FA mismatch; it now just returns `1` and the window
  shows `CFAMerger.FA_MISMATCH_MESSAGE`, so the merge runs UI-free on a background thread.

### Small fixes

* Guarded the loader's "remember install directory per game" handler against a null selection (upstream threw).
* Cancelling the output-folder picker in the migrate window no longer proceeds with an empty path.
* `CExtension.UninstallAsync` no longer throws if the extension directory is already gone.

## Credits

All credit for YWML itself goes to [SuperTavor](https://github.com/SuperTavor) and contributors; the bundled
Level-5 archive library under `Src/Utils/Tinifan` is Tinifan's.
