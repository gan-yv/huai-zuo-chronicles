# Repository validation

Run from the repository root:

```powershell
pwsh ./demo02/tools/validation/validate-repository.ps1
```

The validation covers:

- `version.json`, ProjectSettings, README and CHANGELOG consistency.
- Runtime references to editor code or direct process argument reads.
- Required runtime, editor and EditMode test assemblies.
- Broken local Markdown links.

GitHub Actions runs the same script for changes targeting `main`.
