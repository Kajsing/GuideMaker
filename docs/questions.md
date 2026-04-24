# Open Questions And Decisions

## Status Legend

- BLOCKER: must be answered before implementation.
- IMPORTANT: should be answered before the relevant milestone.
- LATER: can wait until after MVP.
- DECIDED: answered and accepted.

## Blockers

There are currently no unresolved BLOCKER questions.

## Decisions

### Q-001 - What is the MVP success criterion?

Status: DECIDED

MVP is successful when a user can create a complete guide end to end in the program, including follow-along screenshot capture while the user performs the workflow being documented.

### Q-002 - Who is the first user?

Status: DECIDED

The first user is an author creating technical application guides for Region Midt. The app is optimized for one local author producing internal technical guides.

### Q-004 - Does MVP need login?

Status: DECIDED

No login in MVP. The app runs locally on the user's Windows machine.

### Q-006 - Is screenshot capture part of MVP?

Status: DECIDED

Yes. MVP includes follow-along screenshot capture while the user performs actions for the guide. Manual image import and clipboard paste may still be useful, but automatic/local screenshot capture is part of the MVP.

### Q-007 - Is annotation part of MVP?

Status: DECIDED

Yes, basic annotation is part of MVP. The initial scope should support practical guide annotations such as arrows, rectangles/highlights, step labels, captions, and optional blur/redaction for sensitive areas. Advanced image editing is outside MVP.

### Q-008 - What is the first export format?

Status: DECIDED

MVP exports to Markdown, HTML, and PDF. Video with audio is explicitly deferred to version 2.

### Q-009 - What is source of truth?

Status: DECIDED

Each guide is saved as a project folder containing the guide data file, images/screenshots, and exported outputs. The source of truth should be a structured project file, preferably `guide.json`, inside the project folder.

### Q-011 - Does MVP integrate with publishing or knowledge systems?

Status: DECIDED

No integrations in MVP. Confluence, SharePoint, ServiceNow, GitHub, Gitea, and other publishing integrations are deferred to version 2 or later.

### Q-012 - Which UI stack should be used?

Status: DECIDED

C#/.NET with WPF. Rationale: local Windows desktop app, no paid third-party runtime, mature screenshot/file/PDF workflow options, and good fit for a Region Midt Windows environment. Prefer free/open-source NuGet packages only where needed.

### Q-015 - Can guide content contain sensitive data?

Status: DECIDED

Guide content may contain sensitive or internal data; the user is responsible for content. The app should show a general warning before export reminding the user to review screenshots/text for sensitive information, and MVP should include basic redaction/blur if feasible.

### Q-016 - Is cloud usage allowed?

Status: DECIDED

Cloud usage is not allowed in MVP. All guide content, screenshots, project files, preview, and export must work locally.

### Q-017 - Is telemetry allowed?

Status: DECIDED

No telemetry in MVP. The app must not send usage data, screenshots, guide content, crash reports, analytics, or diagnostics to external services. Local logs for troubleshooting are allowed if they stay on the machine and contain no guide content by default.

### Q-020 - Which commands must pass before a milestone is accepted?

Status: DECIDED

Milestone acceptance should require automated checks appropriate to the current implementation stage. Initial examples:

- `dotnet build`
- `dotnet test`
- `./scripts/validate.ps1`
- export smoke test: create/load a sample guide and export Markdown, HTML, and PDF
- project format smoke test: create a guide project folder, save screenshots/assets, close, reopen, and verify content is intact

The exact commands should be captured in repo scripts as the codebase grows.

### Q-021 - Which tempting features are explicitly out of MVP?

Status: DECIDED

See `docs/spec.md` non-goals.

## Still Open But Not Blocking

- Q-003: personal/internal tool vs. formal product framing.
- Q-005: whether future versions need multiple roles.
- Q-010: exact Git-friendly conventions for guide project folders.
- Q-013: dark mode.
- Q-014: drag-and-drop reordering.
- Q-018: admin-free install.
- Q-019: packaging approach.
