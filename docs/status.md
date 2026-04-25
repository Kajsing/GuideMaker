# Project Status And Decision Log

## Current Phase

Phase: Implementation

Current milestone: Milestone 6 - Basic annotation

Status: Milestone 5 image and screenshot capture is complete. Next step is basic annotation.

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
| 2026-04-25 | Original research package moved to `archive/` | Keeps repo root focused on active project files | `docs/` remains the source of current guidance |
| 2026-04-25 | .NET 8 SDK installed and validation scripts hardened | Enables real build/test validation | `validate.ps1` now passes and opts out of .NET CLI telemetry |
| 2026-04-25 | Storage layer hardened | UI can rely on clearer project load/save behavior | Missing files, invalid JSON, schema errors, unsafe asset paths, and missing assets are covered |
| 2026-04-25 | Markdown and HTML export hardened | Guides can be exported to deterministic files | `exports/guide.md` and `exports/guide.html` are covered by smoke tests |
| 2026-04-25 | Basic WPF UI shell wired | Gives the app a usable create/open/edit/save loop | Users can manage basic guide projects before image capture is added |
| 2026-04-25 | Dark mode included in app shell | User requested it during UI smoke testing | Theme switching stays local and dependency-free |
| 2026-04-25 | Image import, paste, and screenshot capture added | Starts the visual guide authoring workflow | Images are stored in `assets/` and attached to selected steps |
| 2026-04-25 | Export image paths and body image references fixed | Exported files live in `exports/` and need correct relative links | HTML/Markdown use `../assets/...`; `[[image:...]]` can place images inside step text |
| 2026-04-25 | Multi-image import added | Users may collect several screenshots before attaching them | Import dialog can attach multiple selected files to the current step |
| 2026-04-25 | Image display sizing deferred to annotation/export work | Size needs per-step image reference metadata, not only asset metadata | Revisit during Milestone 6 or 7 before final export polish |

## Open Blockers

| ID | Question | Owner | Status |
|---|---|---|---|
| None | All blocker questions are decided | Human/Codex | Closed |

## Milestone Progress

| Milestone | Status | Notes |
|---|---|---|
| 0 Research and decisions | Complete | Blockers resolved |
| 1 Repo skeleton and core model | Complete | `validate.ps1` passes |
| 2 Storage layer hardening | Complete | Validation passes with 10 tests |
| 3 Markdown and HTML export | Complete | Validation passes with 14 tests |
| 4 Basic UI shell | Complete | Create/open/edit/save/reopen loop wired |
| 5 Images and screenshot capture | Complete | Import, paste, capture, attach, and preview wired |
| 6 Basic annotation | Ready | MVP scope |
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
| 2026-04-25 | `git diff --check` | Pass | After moving research package to `archive/` |
| 2026-04-25 | `winget install --id Microsoft.DotNet.SDK.8 --exact` | Pass | Installed .NET SDK 8.0.420 |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Restore/build/test passed; 2 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Restore/build/test passed; 10 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Restore/build/test passed; 14 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Basic UI shell builds; 14 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Button sizing and dark mode build; 14 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Image storage/UI capture build; 17 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Export image path/reference fix; 19 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Multi-image import build; 19 tests passed |

## Known Risks

| Risk | Severity | Mitigation |
|---|---|---|
| PDF export package choice | Medium | Choose free/local library during PDF milestone |
| Screenshot capture complexity | Medium | Keep capture flow small and local-first |
| Sensitive screenshots | High | Add export warning and basic redaction/blur if feasible |
| Scope creep | High | Keep non-goals explicit in `docs/spec.md` and `AGENTS.md` |
| Dark mode placement | Low | Move the topbar toggle into Settings when a Settings surface exists |
| Image display sizing model | Medium | Introduce per-step image reference metadata before implementing size controls |

## Next Recommended Step

Start Milestone 6: add basic annotation tools for rectangles/highlights, arrows, labels, and optional blur/redaction.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```
