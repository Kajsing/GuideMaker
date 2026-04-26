# GuideMaker

GuideMaker is a local-first Windows desktop tool for creating technical guides with screenshots, annotations, preview, and export.

The current build is an MVP/beta candidate for one local author creating internal technical application guides.

## Quick Start

For beta testers:

1. Unzip the beta package.
2. Run `GuideMaker.App.exe`.
3. Create a new guide project in an empty folder.
4. Add steps, capture/import screenshots, insert image references, preview, save, and export.

See [docs/manual.md](docs/manual.md) for the user manual and [docs/beta-testing.md](docs/beta-testing.md) for the beta smoke-test checklist.

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

The original research package is kept in `archive/windows_guide_tool_codex_package.md` as historical context. Use `docs/` for current working documentation.

## Build And Run

Install the .NET 8 SDK, then run:

```powershell
dotnet run --project .\src\GuideMaker.App\GuideMaker.App.csproj --configuration Release
```

## Beta Package

Create a self-contained Windows x64 beta zip:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-beta.ps1
```

The package is written under `artifacts/`.

## Validation

Install the .NET 8 SDK, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```
