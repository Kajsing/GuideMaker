# Implementation Runbook

## Before Changing Code

Read:

1. `AGENTS.md`
2. `docs/spec.md`
3. `docs/questions.md`
4. `docs/architecture.md`
5. `docs/plan.md`
6. `docs/status.md`

Check current git status before editing.

## Default Workflow

1. Identify the current milestone in `docs/plan.md`.
2. Keep changes inside that milestone unless the user explicitly changes scope.
3. Update or add focused tests for behavior changes.
4. Run validation.
5. Update `docs/status.md` with changes, validation, and blockers.

## Coding Rules

- Keep Core free of UI, storage, and export dependencies.
- Keep Storage and Export testable without launching WPF.
- Prefer simple .NET APIs before adding dependencies.
- Do not add paid dependencies.
- Avoid cloud services and telemetry.
- Use project-relative asset paths in guide data.
- Treat exported files as generated outputs.

## Validation Commands

Preferred:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```

Fallback checks when .NET SDK is unavailable:

```powershell
git diff --check
Get-ChildItem -Recurse -Include *.csproj,*.props,*.xaml -File | ForEach-Object { [xml](Get-Content -Raw -Path $_.FullName) > $null; $_.FullName }
Get-Content -Raw -Path .\samples\starter-guide\guide.json | ConvertFrom-Json | Out-Null
```

## Status Updates

After meaningful changes, update `docs/status.md` with:

- what changed
- validation run
- blockers
- next recommended step

Until full .NET validation is available, keep the SDK/PATH limitation visible.
