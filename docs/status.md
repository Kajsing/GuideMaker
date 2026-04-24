# Project Status And Decision Log

## Current Phase

Phase: Implementation

Current milestone: Milestone 1 - Repo skeleton and core model

Status: Initial repo skeleton created; full build/test validation awaits .NET 8 SDK on PATH.

## Decision Log

| Date | Decision | Reason | Impact |
|---|---|---|---|
| 2026-04-25 | MVP is local-first | Reduces complexity and security risk | No cloud features in MVP |
| 2026-04-25 | MVP supports end-to-end guide creation with follow-along screenshot capture | This is the core success criterion | Screenshot capture and basic annotation are MVP scope |
| 2026-04-25 | MVP exports Markdown, HTML, and PDF | These are the required first export formats | Video with audio is deferred to version 2 |
| 2026-04-25 | Each guide is a project folder with structured source data and assets | Keeps images, data, and exports together | `guide.json` is the preferred source of truth |
| 2026-04-25 | UI stack is C#/.NET with WPF | Local Windows fit, no paid third-party runtime | Implementation should prefer free/open-source packages |
| 2026-04-25 | No telemetry and no cloud in MVP | Content may be sensitive/internal | Logs must remain local and integrations are deferred |
| 2026-04-25 | Initial solution skeleton created | Starts Milestone 1 with buildable project boundaries | App/Core/Storage/Export/Tests/Scripts/Samples are in place |
| 2026-04-25 | Root `AGENTS.md` added | Gives Codex stable repo-local instructions | Future sessions have concise scope, architecture, and validation guidance |
| 2026-04-25 | Docs split into `docs/` | Makes project guidance easier to maintain | Large package remains as historical context |

## Open Blockers

| ID | Question | Owner | Status |
|---|---|---|---|
| None | All blocker questions are decided | Human/Codex | Closed |

## Milestone Progress

| Milestone | Status | Notes |
|---|---|---|
| 0 Research and decisions | Complete | Blockers resolved |
| 1 Repo skeleton and core model | In progress | Initial skeleton created; awaiting .NET SDK validation |
| 2 Storage layer hardening | Not started | Depends on Milestone 1 validation |
| 3 Markdown and HTML export | Not started | Basic exporter classes exist; hardening later |
| 4 Basic UI shell | Not started | WPF shell exists; workflow not wired |
| 5 Images and screenshot capture | Not started | MVP scope |
| 6 Basic annotation | Not started | MVP scope |
| 7 PDF export and preview | Not started | PDF package decision needed |
| 8 MVP hardening | Not started | Final polish |

## Validation Log

| Date | Command | Result | Notes |
|---|---|---|---|
| 2026-04-25 | `git diff --check` | Pass | Whitespace check clean |
| 2026-04-25 | XML/XAML parse check | Pass | Project and XAML files parse as XML |
| 2026-04-25 | sample `guide.json` parse check | Pass | Sample project JSON parses |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Blocked | .NET SDK not found on PATH |
| 2026-04-25 | `git diff --check` | Pass | After adding `AGENTS.md` and updating repo structure |
| 2026-04-25 | `git diff --check` | Pass | After splitting current docs into `docs/` |
| 2026-04-25 | docs blocker scan | Pass | No unresolved blocker markers found in `docs/*.md` |

## Known Risks

| Risk | Severity | Mitigation |
|---|---|---|
| .NET SDK missing on current machine/PATH | High | Install .NET 8 SDK or add `dotnet.exe` to PATH, then run validation |
| PDF export package choice | Medium | Choose free/local library during PDF milestone |
| Screenshot capture complexity | Medium | Keep capture flow small and local-first |
| Sensitive screenshots | High | Add export warning and basic redaction/blur if feasible |
| Scope creep | High | Keep non-goals explicit in `docs/spec.md` and `AGENTS.md` |

## Next Recommended Step

Install or expose .NET 8 SDK on PATH, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```
