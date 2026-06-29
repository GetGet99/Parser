---
name: sourcegen-debug
description: Use this skill if something goes wrong with source generated files, or quickmarkup. Will tell you commands to debug them
---

# Debugging C# Source Generators: Missing Generated Files

When debugging C# source generators, remember:

By default, generated `.g.cs` files are usually NOT written to disk.

They exist only inside the Roslyn compilation unless the project enables emission of compiler-generated files.

## Symptoms

Common misleading symptoms:

* "File not found"
* Generated folder missing
* Agent cannot inspect generated source
* Types appear unresolved
* Namespace/type errors from generated code
* Generator appears to produce no output

## Solution

```bash
dotnet build \
  -p:EmitCompilerGeneratedFiles=true \
  -p:CompilerGeneratedFilesOutputPath=Generated
```

## Important Notes

* Source generators can work correctly even when no files exist on disk.
* Roslyn keeps generated syntax trees in-memory unless emission is enabled.
* Agents should NOT assume generator failure solely because the filesystem lacks generated `.cs` files.
* Visual Studio may still show generated files under:

  Dependencies → Analyzers → `<Generator>` → Generated Files

even if files are absent from disk.

* Please remove generated files afterwards when you do not need them.

## Recommended Agent Behavior

Before diagnosing generator failure:

1. Check whether `EmitCompilerGeneratedFiles` is enabled.
2. If not enabled, either:

   * enable it temporarily, or
   * inspect generated syntax trees through Roslyn APIs/logging.
3. Only conclude generation failure after confirming emitted output is actually expected.
