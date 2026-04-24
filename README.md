# GuideMaker

GuideMaker is a local-first Windows desktop tool for creating technical guides with screenshots, annotations, preview, and export.

## MVP direction

- Windows desktop app using C#/.NET and WPF.
- Local project folder per guide.
- `guide.json` is the source of truth.
- Screenshots and exports live beside the guide in `assets/` and `exports/`.
- No cloud and no telemetry in MVP.

## Repository layout

- `src/GuideMaker.App` - WPF desktop shell.
- `src/GuideMaker.Core` - guide model and project format constants.
- `src/GuideMaker.Storage` - project folder save/load.
- `src/GuideMaker.Export` - Markdown/HTML/PDF export surface.
- `tests/GuideMaker.Tests` - smoke tests for model, storage, and export.
- `samples/starter-guide` - sample guide project.
- `scripts` - validation and test entry points.
- `docs` - current project specification, decisions, architecture, plan, implementation notes, and status.

The original research package is kept in `windows_guide_tool_codex_package.md` as historical context. Use `docs/` for current working documentation.

## Validation

Install the .NET 8 SDK, then run:

```powershell
.\scripts\validate.ps1
```
