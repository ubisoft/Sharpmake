# Sharpmake.PublicApiTests

This project is dedicated to ensuring that the public API of Sharpmake remains unchanged across versions. It uses the PublicApiGenerator and Verify libraries to capture the API surface and compare it against a verified snapshot. This helps detect unintended changes to the API, maintaining compatibility and stability for users.

## Running the Tests

To run the tests, use your preferred test runner for .NET projects (e.g., Visual Studio Test Explorer, `dotnet test`).

## Snapshots management

Tests generate output files (received) and compare them against reference files (verified snapshots). When a test fails due to an API change, the received and verified files must be diffed and reconciled. There are several ways to manage this workflow:

- **[Verify.Terminal](#verifyterminal)** — a dotnet CLI tool for reviewing, accepting, or rejecting snapshots directly in the terminal, without a GUI.
- **[DiffEngine Preferences](#diffengine-preferences)** — configure your preferred GUI diff tool to open automatically when a snapshot mismatch occurs.
- **[DiffEngineTray](#using-diffenginetray-on-windows)** (Windows only) — a tray application that coordinates diff operations triggered by test runs.

### Verify.Terminal

`verify.tool` is a dotnet CLI tool for reviewing, accepting, and rejecting pending Verify snapshots directly from the terminal, without needing a GUI diff tool.

The tool is already registered in [`dotnet-tools.json`](../dotnet-tools.json). Restore it by running:

```powershell
dotnet tool restore
```

#### Review pending snapshots

Interactively review each pending diff in the terminal:

```powershell
dotnet verify review
```

#### Accept all pending snapshots

Accept all pending received files, overwriting the verified snapshots:

```powershell
dotnet verify accept
```

Use `--yes` (or `-y`) to skip confirmation prompts.

#### Reject all pending snapshots

Delete all pending received files (discard the changes):

```powershell
dotnet verify reject
```

Use `--yes` (or `-y`) to skip confirmation prompts.

Refer to the [Verify.Terminal documentation](https://github.com/VerifyTests/Verify.Terminal) for more details.

### DiffEngine Preferences

Developers can specify their preferred diff tool by setting the `DiffEngine` configuration. This can be done via environment variables.

Set the `DiffEngine_ToolOrder` environment variable to your preferred tool (e.g., `AraxisMerge`, `VisualStudioCode`, `WinMerge`):

```powershell
$env:DiffEngine_ToolOrder = 'VisualStudioCode'
```

By default, when a diff is opened, the temp (received) file is on the left and the target (verified) file is on the right.

This value can be changed by setting the `DiffEngine_TargetOnLeft` environment variable to `true`:

```powershell
$env:DiffEngine_TargetOnLeft = 'true'
```

Refer to the [DiffEngine documentation](https://github.com/VerifyTests/DiffEngine) for supported tools and configuration options.

### Using DiffEngineTray on Windows

On Windows, you can use the DiffEngineTray tool for enhanced diff management. DiffEngineTray runs in the background and helps coordinate diff operations triggered by tests.

#### Installation

Download DiffEngineTray from the [official repository](https://github.com/VerifyTests/DiffEngineTray) and run the executable.

#### Usage

When a test requires a diff, DiffEngineTray will prompt you to review differences using your configured diff tool.

---

For more information, see the [PublicApiGenerator documentation](https://github.com/PublicApiGenerator/PublicApiGenerator) and [Verify documentation](https://github.com/VerifyTests/Verify).