---
name: fix-resharper-warnings
description: Check and fix ReSharper warnings after editing this project or when investigating Rider warnings.
---

# Fix ReSharper warnings

After every editing task, including documentation/configuration changes, run full-solution analysis.
Fix warnings and errors in edited source files and findings introduced elsewhere; leave unrelated existing findings out of scope.
Rerun after fixes and report any remaining applicable findings or blocked analysis.
You may suppress warnings when the warning is not a risk or fixing it would reduce readability, most often in tests.

Run the helper from the repository root:

```powershell
powershell -NoProfile -File .agents/skills/fix-resharper-warnings/scripts/inspect-code.ps1
```

The helper uses the version pinned in `.config/dotnet-tools.json`, restores it only if its package is missing, and runs the cached executable **through `dotnet`**. On Windows, launching `inspectcode.exe` directly starts .NET Framework instead of the .NET host used by `dotnet jb`; do not use that fallback.

On this Codex Windows host, run the helper with `sandbox_permissions: require_escalated`. InspectCode also writes global JetBrains shell/shared caches outside the repository; `--caches-home` alone does not make a sandboxed run reliable. A sandbox cache-access failure is an environment failure, not a reason to discard the solution cache.

The helper retains full-solution SWEA, the explicit `.DotSettings`, `BaseOutputPath=bin/agent/`, and the build required for generated code. It stores reports/logs in unique run directories, but reuses `artifacts/resharper/cache/<tool-version>-dotnet<runtime-major>` across tasks. Its file lock prevents concurrent runs from opening extra cold cache slots. If another run owns the lock, do independent work and retry after it finishes; do not create a separate cache to bypass it.

Keep the cache for normal source edits and warning fixes. Do not put it in a timestamped report directory or clear it after each task. The tool/runtime profile automatically separates incompatible launch environments.

If a reused cache reports widespread unresolved symbols despite a successful build, read the saved InspectCode/MSBuild logs and check the runtime and build settings first. A successful process exit alone does not prove the findings are valid. If the model remains incorrect, archive only that inactive cache profile and rebuild it once, then verify a second run reuses it with consistent results. Do not delete caches held by another process or reset unrelated Rider caches.

Cache reuse saves loading/indexing work; InspectCode still performs the requested full inspection. Do not narrow scope, disable SWEA, or skip the build merely to make the check faster.
