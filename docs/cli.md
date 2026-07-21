# Command-Line Interface (`keeneyes`)

The `keeneyes` command-line tool (`KeenEyes.Cli`) manages editor plugins, configures the NuGet package sources they are installed from, and batch-upgrades save files to the latest component versions. It shares its plugin/source infrastructure with the [KeenEyes Editor](editor.md), so plugins installed from the CLI are available in the editor and vice versa.

## Running

The tool builds to an executable named `keeneyes`:

```bash
# From a build of this repo
dotnet run --project editor/KeenEyes.Cli -- <command> [options]

# Or, once published / installed on PATH
keeneyes <command> [options]
```

Running `keeneyes` with no command (or `--help`) prints the command list; `keeneyes <command> --help` prints help for a specific command.

### Global options

| Option | Description |
|--------|-------------|
| `-h`, `--help` | Show help information |
| `-v`, `--version` | Show version information |
| `--verbose` | Show verbose output (includes stack traces on error) |
| `-q`, `--quiet` | Suppress non-essential output |

The process exit code is `0` on success, `1` on error, and `130` if cancelled with `Ctrl+C`.

## Plugin management

`keeneyes plugin <subcommand>` manages editor plugins installed from NuGet.

| Subcommand | Description | Usage |
|------------|-------------|-------|
| `install` | Install a plugin from NuGet | `plugin install <package-id> [--version <ver>] [--source <url>]` |
| `list` | List installed plugins | `plugin list [--all \| --enabled \| --disabled]` |
| `search` | Search for plugins on NuGet | `plugin search <query> [--source <url>] [--take <n>]` |
| `uninstall` | Uninstall a plugin | `plugin uninstall <package-id>` |
| `update` | Update installed plugins | `plugin update [<package-id>] [--all]` |

```bash
# Find and install a plugin
keeneyes plugin search physics
keeneyes plugin install Acme.KeenEyes.Physics --version 1.2.0

# Review what's installed, then update everything
keeneyes plugin list --all
keeneyes plugin update --all
```

## Package sources

`keeneyes sources <subcommand>` configures the NuGet feeds that `plugin install`/`search` resolve against. (`sources` is an alias for `plugin sources`.)

| Subcommand | Description | Usage |
|------------|-------------|-------|
| `add` | Add a NuGet package source | `sources add <name> <url> [--default]` |
| `list` | List configured NuGet sources | `sources list` |
| `remove` | Remove a NuGet package source | `sources remove <name>` |

```bash
keeneyes sources add studio-feed https://nuget.example.com/v3/index.json --default
keeneyes sources list
keeneyes sources remove studio-feed
```

## Migrating save files

`keeneyes migrate` batch-upgrades saved worlds to the latest component schema versions — the offline, bulk counterpart to the runtime migration system described in the [Migrations guide](migrations.md).

**Usage:** `migrate --path <dir> [--pattern <glob>] [--dry-run] [--backup] [--output <dir>] [--verbose] [--continue-on-error]`

| Option | Description |
|--------|-------------|
| `--path <dir>` | Directory of save files to process (required) |
| `--pattern <glob>` | Filename glob to match (e.g. `*.kesave`) |
| `--dry-run` | Report what would change without writing |
| `--backup` | Write a backup copy before upgrading each file |
| `--output <dir>` | Write upgraded files to a separate directory |
| `--continue-on-error` | Keep going if an individual file fails |

```bash
# Preview upgrades, then apply them with backups
keeneyes migrate --path ./saves --pattern "*.kesave" --dry-run
keeneyes migrate --path ./saves --pattern "*.kesave" --backup
```

## Next Steps

- [KeenEyes Editor](editor.md) - The editor that consumes these plugins
- [Migrations](migrations.md) - How component schema migrations work at runtime
- [Editor Plugin Dependencies](editor-plugin-dependencies.md) - How plugin dependencies resolve
- [SDK](sdk.md) - Building plugins that the CLI can install
