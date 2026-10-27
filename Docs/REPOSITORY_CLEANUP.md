# Repository cleanup record

Scope: `codex/repository-cleanup` from `main` at `c60ecd6`; `src/`, `tests/`, `Data/`, documentation and art authoring tools. The project remains .NET 10 with Core, CLI, Pixel and shared JSON content. No content or gameplay rule was moved into C#.

| Before | After | Reason |
|---|---|---|
| `AGENTS.md` existed only as an untracked working file; `CLAUDE.md` carried the full guide | `AGENTS.md` is the tracked, authoritative guide; `CLAUDE.md` points to it; `README.md` links it | One contributor contract for all agents and human maintainers |
| `ANTIGRAVITY.md` was absent | No replacement needed | Avoid another parallel instruction source |
| Act II/III plans appeared current beside canonical lore | Moved to `Docs/Archive/` and marked historical | Preserve design history without presenting it as current Data behavior |
| Research report was untracked under `reports/` | Tracked as `Docs/Research/PIXELORAMA_AUTOMATION.md` with implementation status | Keep evidence and future integration options discoverable |
| Shipped presentation defaults and state IDs lived in C# | Shipped looks and scene bindings now live in `Data/World/presentation.json`; C# retains generic fallback behavior | Enforce the content boundary and make future content changes data-driven |
| No runtime pictures or reproducible art authoring path | Three reviewed PNGs, editable Pixelorama masters, manifest, recipe and export/validation tools | Prove a safe asset workflow before expanding it |

The existing NuGet references are all used: Spectre.Console is CLI-only, Raylib-cs is Pixel-only, and the four test packages plus Spectre.Console.Testing serve xUnit and coverage. No package or build configuration was removed. The generated presentation schema was refreshed after the record change; the remaining generated schemas had no semantic changes. Local editor binaries and export staging are ignored; neither is a runtime dependency.

## Reproduce checks

```powershell
git status --short
git diff --check
dotnet build -c Release
dotnet test
pwsh -NoProfile -File Tools/ArtPipeline/Test-ArtAssets.ps1
pwsh -NoProfile -File Tools/ArtPipeline/Export-Pixelorama.ps1
```

For a schema record change, run `dotnet run --project src/RpgEngine.CLI -- --export-schemas` and check the schema diff. For flag, counter or quest changes, update `Docs/LORE.md` and run `dotnet test --filter LoreRegistryTests`. Use Pixelorama 1.2.3 for exact export reproduction or review another version's output before publishing.

## Migration and rollback

No save-file or game-content migration is required. The new presentation bindings are optional and default to zero/false. The three image paths were already present in JSON; adding their PNGs activates them. To roll back the visual change, revert the paired `.pxo`, PNG and presentation edits, or remove an image path to use the procedural fallback. To roll back contributor-guide consolidation, restore the previous `CLAUDE.md` from Git and keep the `AGENTS.md` content synchronized. Review the build, full test suite and a screenshot after either rollback.
