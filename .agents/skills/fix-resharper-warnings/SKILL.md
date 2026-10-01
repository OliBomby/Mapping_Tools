---
name: fix-resharper-warnings
description: Check and fix ReSharper warnings after editing this project or when investigating Rider warnings.
---

# Fix ReSharper warnings

After every editing task, including documentation/configuration changes, run full-solution analysis. 
Fix warnings and errors in edited source files and findings introduced elsewhere; leave unrelated existing findings out of scope. 
Rerun after fixes and report any remaining applicable findings or blocked analysis. 
You may suppress warnings if you deem the warning to not be a risk or fixing it reduces the code readability more than it's worth, most often in test projects.

Run from the repository root:

```powershell
dotnet tool restore
dotnet jb inspectcode Mapping_Tools.slnx --output=artifacts/resharper/<run-id>/warnings.sarif --severity=WARNING --swea --settings=Mapping_Tools.sln.DotSettings '--disable-settings-layers=GlobalAll;GlobalPerProduct;SolutionPersonal;ProjectPersonal' --caches-home=artifacts/resharper/<run-id>/cache --properties=BaseOutputPath=bin/agent/ --no-updates --verbosity=WARN
```

Use a new timestamp or UUID for `<run-id>` on every run: reused caches have returned stale inspection settings here. The explicit `.DotSettings` path is required for this `.slnx` solution.

