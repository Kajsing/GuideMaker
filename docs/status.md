# Project Status And Decision Log

## Current Phase

Phase: Implementation

Current milestone: Milestone 8 - MVP hardening

Status: Milestone 8 MVP hardening is in progress. The first annotation editor controls have been added.

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
| 2026-04-25 | Basic annotations added | Supports practical marking without a full image editor | Highlight, label, arrow, and redaction annotations are saved and exported as HTML overlays |
| 2026-04-25 | Validation tests run in Release again | Smart App Control was disabled and Release test assemblies are no longer blocked | `validate.ps1` now builds and tests Release |
| 2026-04-25 | PDFsharp selected for MVP PDF export | Free/open-source MIT package with local .NET 8 PDF generation | `GuideMaker.Export` now writes `exports/guide.pdf` |
| 2026-04-25 | In-app preview added | Authors need to review guide output before export | Preview writes local `exports/preview.html` and displays it in the app |
| 2026-04-25 | Sensitive-content export warning added | Guide text and screenshots may contain internal data | Export asks the user to review content before writing Markdown, HTML, and PDF |
| 2026-04-25 | Hard redaction moved to Milestone 8 | Visual overlays are not enough for sensitive content | Redaction should burn or blur pixels in exported output during hardening |
| 2026-04-25 | Annotation editor moved to Milestone 8 | Current annotation buttons only add default-position overlays | Users need to select, move, resize, and edit labels before MVP hardening is complete |
| 2026-04-25 | Initial annotation editor added | Authors need control over where highlights, arrows, labels, and redactions land | Selected image annotations can now be selected and adjusted with text and bounds controls |
| 2026-04-25 | Annotation editor usability improved | Slider edits were losing selection and the preview was too small | Selection is preserved, X/Y respect object size, label editing no longer reverses text, and a larger live preview was added |
| 2026-04-25 | Annotation editor follow-up items captured | Smoke testing showed remaining UX issues | M8 should revisit percentage sliders, width/height interaction, rotate controls, and hide/show UI organization |
| 2026-04-25 | Annotation preview fit corrected | Editor preview used cropped image coordinates while export used fitted image coordinates | Thumbnail and editor overlays now use fitted image geometry; label text commits on Enter or focus lost |
| 2026-04-25 | Workspace preview tabs added | The editor area was getting crowded and the annotation preview needed more room | Right preview pane now switches between guide preview and image annotation preview |
| 2026-04-25 | Annotation tools moved into annotation panel | Image toolbar mixed asset actions and annotation actions | Import/paste/capture stay under Images; highlight/label/arrow/redact/remove mark are grouped under Annotations |
| 2026-04-25 | Guide preview auto-refresh added | Guide preview could show stale annotation output while Image preview was live | Dirty changes regenerate `preview.html` after a short debounce; HTML redaction uses blur CSS where supported |
| 2026-04-25 | Annotation panel made collapsible | Annotation controls could crowd the step editor when many marks existed | Annotation editor now has hide/show behavior and the annotation list scrolls |
| 2026-04-25 | Annotation panel defaults closed | The main editing flow should not be dominated by annotation controls | Annotation tools start hidden and use more compact buttons/list sizing |
| 2026-04-25 | Workspace pane made resizable | Guide and image previews need more space during review | A splitter now lets the author resize editor and workspace width |
| 2026-04-25 | Asset selection preservation fixed | Committing annotation text on focus loss could force the old image selection back | Lost-focus text commits no longer refresh the asset list |
| 2026-04-25 | Annotation text edits no longer refresh lists | Asset selection still snapped back when the annotation panel was open | Text edits update the selected annotation in memory without rebuilding asset or annotation lists |
| 2026-04-25 | Asset clicks made authoritative | Pending annotation events could still restore the previous selected image | Mouse-down on an asset records the intended selection and prevents stale annotation refreshes from overriding it |

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
| 6 Basic annotation | Complete | Highlight, label, arrow, redaction overlays wired |
| 7 PDF export and preview | Complete | PDFsharp export, local preview, and export warning added |
| 8 MVP hardening | In progress | Annotation editor polish, rotate controls, hard redaction, hide/show UI, and final polish remain |

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
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Release build and Debug tests passed; 21 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | PDF export and preview build; 24 tests passed |
| 2026-04-25 | `dotnet test .\GuideMaker.sln --configuration Release --no-restore` | Pass | Smart App Control no longer blocks Release tests; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Initial annotation editor build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Annotation editor usability fixes; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Annotation preview fit correction; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Workspace preview tabs build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Annotation toolbar move and guide preview auto-refresh; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Collapsible annotation panel build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Compact closed annotation panel build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Resizable workspace pane build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Asset selection fix build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Quiet annotation text edit build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Authoritative asset click fix; Release tests passed; 24 tests passed |

## Known Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Screenshot capture complexity | Medium | Keep capture flow small and local-first |
| Sensitive screenshots | High | Add hard redaction in Milestone 8 so exported output destroys or blurs pixels instead of only using visual overlays |
| Scope creep | High | Keep non-goals explicit in `docs/spec.md` and `AGENTS.md` |
| Dark mode placement | Low | Move the topbar toggle into Settings when a Settings surface exists |
| Image display sizing model | Medium | Introduce per-step image reference metadata before implementing size controls |
| Annotation placement still needs richer direct manipulation | Medium | Initial select/edit/bounds controls are added; direct drag handles can follow if needed |
| Annotation editor UI is getting crowded | Medium | Add hide/show or collapsible surfaces during M8 hardening |

## Known M8 Bugs

| Bug | Severity | Notes |
|---|---|---|
| Asset selection snaps back when Annotations is open | High | Clicking another image can still jump back to the previous image while the annotation panel is expanded. Likely caused by annotation editor selection/refresh/focus events competing with asset selection. |
| Annotation expander arrow points the wrong way | Low | The hide/show arrow direction is visually confusing and should be corrected or replaced with clearer show/hide affordance. |
| Redact is not true blur in the app workspace | Medium | Current UI preview uses a dark redact overlay. HTML may use CSS blur where supported, but MVP hardening still needs hard redaction/blur of exported pixels. |
| Annotation percentage sliders are confusing | Medium | X/Y/Width/Height values interact awkwardly and should be redesigned or replaced with direct manipulation handles. |
| Annotation controls are still clunky | Medium | Current compact buttons are better grouped but should become a cleaner tool strip or collapsible tool surface. |

## Next Recommended Step

Continue Milestone 8 by fixing the known annotation editor bugs, then implement hard redaction and final MVP polish.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```
