# GuideMaker Architecture

## Architecture Goals

- Keep guide data separate from UI.
- Keep storage and export logic testable without launching the desktop app.
- Keep project format stable and documented.
- Keep MVP local-only.
- Avoid paid third-party dependencies.

## Solution Layout

- `src/GuideMaker.App` - WPF UI shell and later app coordination.
- `src/GuideMaker.Core` - domain model, schema constants, and project layout names.
- `src/GuideMaker.Storage` - project folder save/load.
- `src/GuideMaker.Export` - Markdown, HTML, and PDF export.
- `tests/GuideMaker.Tests` - unit and smoke tests.
- `samples/starter-guide` - sample guide project.
- `scripts` - validation and test entry points.

## Layer Rules

`GuideMaker.Core` must not reference UI, storage, or export projects.

`GuideMaker.Storage` may reference `GuideMaker.Core`.

`GuideMaker.Export` may reference `GuideMaker.Core`.

`GuideMaker.App` may reference Core, Storage, and Export.

Tests may reference all non-app projects. UI tests can be added later if needed.

## Project Folder Format

Each guide is saved as a folder:

```text
MyGuide/
  guide.json
  assets/
  exports/
```

`guide.json` is the source of truth. Exported Markdown, HTML, and PDF files are generated outputs and should not be treated as canonical guide data.

## Domain Model

The initial model contains:

- `GuideDocument`
- `GuideMetadata`
- `GuideStep`
- `GuideAsset`
- `GuideAnnotation`
- `AnnotationBounds`

Assets are referenced by ID from steps. Asset files are stored by relative path inside the project folder.

## Export Strategy

Markdown and HTML export should be deterministic and easy to test.

PDF export is MVP scope, but the concrete implementation package must be chosen carefully:

- no paid dependency
- local-only
- usable from tests or smoke checks
- acceptable license for internal use

Decision: use `PDFsharp` for MVP PDF generation. It is a free/open-source MIT-licensed NuGet package, runs locally, supports .NET 8, and keeps PDF generation inside `GuideMaker.Export`.

## Screenshot And Annotation Strategy

Follow-along screenshot capture belongs in or below the app layer because it interacts with Windows desktop APIs.

Annotation data belongs in Core. Rendering/editing annotations belongs in App. Applying annotations to exported images may later require a small image-processing component, but it should remain testable without launching the UI.
