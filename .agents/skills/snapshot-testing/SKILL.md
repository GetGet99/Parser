---
name: snapshot-testing
description: How to run, update, or add snapshot tests for source generator output. Snapshot tests verify generated C# code (`.g.cs`) against stored reference files to catch regressions.
---

# Snapshot Testing for Source Generators

Snapshot tests are in `Get.SourceGenerator.Test/`. They compile a small C# source, run the generator in-process via Roslyn APIs, and compare output against stored `.cs.snap` files.

## Quick Reference

| Action | Command (from Parser/) |
|--------|------------------------|
| Run all snapshot tests | `dotnet test Get.SourceGenerator.Test` |
| Update all snapshots | `$env:UPDATE_SNAPSHOTS=1; dotnet test Get.SourceGenerator.Test` |
| Add a new snapshot test | see below |

## How It Works

- `SnapshotTestBase` (`Get.SourceGenerator.Test/SnapshotTestBase.cs`) provides:
  - `CreateCompilation(source)` — builds a Roslyn `CSharpCompilation`
  - `LoadParserGenerator()` / `LoadLexerGenerator()` — loads generator DLL via reflection
  - `RunGenerator(compilation, generator)` — runs generator, returns results
  - `MatchSnapshot(actual, name)` — compares or writes `Snapshots/{name}.cs.snap`

- Snapshots stored at `Get.SourceGenerator.Test/Snapshots/*.cs.snap`
  - `.cs.snap` extension prevents MSBuild from compiling them
  - Line endings normalized (CRLF → LF) before comparison

## Updating Snapshots

```powershell
$env:UPDATE_SNAPSHOTS=1
dotnet test Get.SourceGenerator.Test
Remove-Item Env:\UPDATE_SNAPSHOTS
```

With `UPDATE_SNAPSHOTS=1`, `MatchSnapshot` writes the file instead of asserting. Run again without the env var to confirm.

## Adding a New Snapshot Test

1. In `ParserGeneratorSnapshotTests.cs` or `LexerGeneratorSnapshotTests.cs`:
   - Write inline C# source using the generator attribute
   - Create compilation, run generator
   - Assert 0 diagnostics, exactly 1 source
   - Call `MatchSnapshot(generatedCode, "UniqueName")`

2. Run with `UPDATE_SNAPSHOTS=1` to create the `.cs.snap` file

3. Run without env var to confirm the test passes

## CI

`UPDATE_SNAPSHOTS` is unset on CI, so any output mismatch fails the test. Commit `.cs.snap` changes to update.
