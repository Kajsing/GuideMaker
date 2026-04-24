# GuideMaker Specification

## Product Summary

GuideMaker is a local-first Windows desktop app for creating technical step-by-step guides with text, screenshots, basic annotations, preview, and export.

The first audience is one local author creating internal technical application guides for Region Midt.

## MVP Success Criterion

MVP is successful when a user can create a complete guide end to end in the program:

1. Create a guide project.
2. Add and edit guide steps.
3. Capture screenshots while following the workflow being documented.
4. Add basic annotations to screenshots.
5. Save the guide as a local project folder.
6. Reopen the guide without losing content.
7. Preview the guide.
8. Export to Markdown, HTML, and PDF.

## MVP Scope

MVP includes:

- Windows desktop app using C#/.NET and WPF.
- Local-only storage.
- One project folder per guide.
- `guide.json` as the source of truth.
- `assets/` for screenshots and imported images.
- `exports/` for generated outputs.
- Follow-along screenshot capture.
- Basic annotation: arrows, rectangles/highlights, labels, captions, and optional blur/redaction.
- Export warning reminding the user to review sensitive content.
- Markdown, HTML, and PDF export.

## Non-Goals

Explicitly outside MVP:

- video export with audio
- cloud sync or cloud storage
- login, roles, permissions, or multi-user collaboration
- Confluence, SharePoint, ServiceNow, GitHub, Gitea, or other publishing integrations
- AI writing, AI summarization, or online language services
- advanced image editor features beyond simple annotations and optional blur/redaction
- template marketplace or shared template library
- mobile app or web app
- telemetry or external diagnostics
- paid third-party dependencies

## Privacy And Security

Guide content may contain sensitive or internal data. The user is responsible for guide content, but the app should help by warning before export and by supporting simple redaction/blur if feasible.

MVP must not send guide content, screenshots, logs, analytics, crash reports, or diagnostics to external services.

## Source Package

The original research and planning package is kept in `windows_guide_tool_codex_package.md` as historical context.
