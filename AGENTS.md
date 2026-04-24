# AGENTS.md - Repository Instructions for Codex

## Project

GuideMaker is a local-first Windows desktop app for creating technical guides with follow-along screenshot capture, basic annotations, preview, and export to Markdown, HTML, and PDF.

The MVP is for one local author creating internal technical application guides for Region Midt.

## Required Reading

Before product changes, read:

1. `README.md`
2. `docs/spec.md`
3. `docs/questions.md`
4. `docs/architecture.md`
5. `docs/plan.md`
6. `docs/implement.md`
7. `docs/status.md`
8. This `AGENTS.md`

`windows_guide_tool_codex_package.md` is historical/research context. Use `docs/` as the current working documentation.

## Current Stack

- C#/.NET 8
- WPF desktop app
- xUnit tests
- Local project folder storage
- `guide.json` as the source of truth

## Repository Layout

- `src/GuideMaker.App` - WPF UI shell and later app coordination.
- `src/GuideMaker.Core` - domain model, schema constants, and project layout names.
- `src/GuideMaker.Storage` - guide project folder save/load.
- `src/GuideMaker.Export` - Markdown, HTML, and PDF export code.
- `tests/GuideMaker.Tests` - unit and smoke tests.
- `samples/starter-guide` - sample guide project.
- `scripts` - validation and test commands.
- `docs` - current spec, decisions, architecture, plan, implementation notes, and status.

## Scope Rules

MVP includes:

- local guide project folders
- `guide.json` save/load
- follow-along screenshot capture
- basic annotation: arrows, rectangles/highlights, labels, captions, and optional blur/redaction
- preview
- export to Markdown, HTML, and PDF

Do not implement these unless the project docs are updated first:

- cloud sync or cloud storage
- login, roles, permissions, or multi-user collaboration
- Confluence, SharePoint, ServiceNow, GitHub, Gitea, or other publishing integrations
- AI writing, AI summarization, or online language services
- video export with audio
- mobile app or web app
- telemetry or external diagnostics
- paid third-party dependencies

## Architecture Rules

Keep these concerns separate:

- UI in `GuideMaker.App`
- domain model in `GuideMaker.Core`
- file/project persistence in `GuideMaker.Storage`
- export logic in `GuideMaker.Export`

Storage and export logic must be testable without launching the WPF app.

Prefer free/open-source packages only when needed. Avoid adding dependencies for behavior that is straightforward to implement locally.

## Security And Privacy

Guide content may contain sensitive or internal data.

- Everything must work locally in MVP.
- Do not send guide content, screenshots, logs, analytics, crash reports, or diagnostics to external services.
- Local logs are allowed only when useful and should not include guide content by default.
- Export flows should remind users to review content for sensitive information.

## Validation

Preferred validation command:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```

If `.NET SDK` is unavailable, say so clearly and run any useful lightweight checks that are possible, such as:

- `git diff --check`
- XML/XAML parse checks
- sample `guide.json` parse checks

Record meaningful validation limitations in `docs/status.md`.

## Work Habits

- Keep changes milestone-sized and easy to review.
- Do not rewrite unrelated files.
- Update status/decision docs after meaningful changes.
- When adding project behavior, add focused tests at the same time.
