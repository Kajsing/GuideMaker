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
| 2026-04-25 | Annotation selection state model refactored | Asset selection still snapped back when Annotations was open | Image and annotation selection now use explicit IDs as source of truth instead of WPF list selection |
| 2026-04-25 | Asset selection moved to preview mouse-down | Focus-loss events from annotation controls could still run before image selection changed | Image clicks now apply selected asset before annotation text/slider events can refresh selection |
| 2026-04-25 | Image list hitbox stabilized | Layout movement during image selection could let the same click finish on another row | Image rows now have fixed height and handled preview clicks |
| 2026-04-25 | Workspace annotation drag added | Sliders are too clunky for practical placement | Existing annotations can be dragged in the Image workspace and stored as percentage bounds |
| 2026-04-25 | Annotation ordering controls added | Authors need to choose which mark appears above another | Selected image annotations can be moved up or down in render order |
| 2026-04-25 | Drag no longer reloads size controls | Width and height controls appeared to move while dragging annotations | Drag updates X/Y only while preserving width and height slider state |
| 2026-04-25 | Annotation ordering buttons compacted | Up/Down text buttons cluttered the add tools | Ordering controls are now arrow buttons beside the annotation list |
| 2026-04-25 | HTML redact preview made visible in WebBrowser | Redact overlays existed in `preview.html` but did not render visibly in WPF WebBrowser | HTML export now uses IE/Edge compatibility metadata, z-index, and solid redact fallback styling |
| 2026-04-25 | Export assets are rendered with annotations burned in | Markdown and HTML should not rely on fragile overlay CSS, and redaction must not leave original pixels visible in exported images | Exports now generate image files under `exports/assets` and Markdown, HTML, and PDF use those generated images |
| 2026-04-25 | Redaction made fully opaque | Semi-transparent redaction still revealed underlying text in preview | Redaction now renders as solid dark masking in app preview, HTML fallback, and generated export images |
| 2026-04-25 | Basic label styling added | Labels need size and color controls without turning the MVP into a full document editor | Label annotations now support size, bold, italic, text color, box color, and box opacity |
| 2026-04-25 | Advanced label styling deferred to v2 | Font family, presets, borders, shadows, and templates would expand scope | Keep M8 focused on practical label controls and export consistency |
| 2026-04-25 | Image pool and per-step image refs selected as next architecture step | Follow-along capture and crop need reusable originals and per-step usage data | Add migration-compatible `StepImageRef`, keep originals in the pool, and render crop/annotations into generated export assets |
| 2026-04-25 | Migration-compatible step image refs added | The app needs reusable original images before follow-along capture and crop editing | `GuideStep.ImageRefs`, optional crop bounds, per-ref annotations, and storage validation now coexist with legacy `AssetIds` |
| 2026-04-25 | Export rendering prefers step image refs | Cropped/reused images need their own generated output instead of mutating originals | Export assets now render per-step image refs with crop and annotations while legacy asset export remains readable |
| 2026-04-25 | App save/load bridges legacy images to step image refs | The UI still edits step image lists while storage needs the new per-step image reference model | Imported, pasted, and captured images now get `StepImageRef` entries; older projects migrate into refs when saved |
| 2026-04-25 | Step image ref body tokens map to rendered images | Smoke testing showed original images could export without annotations and then repeat later in the step | `[[image:...]]` tokens in steps with image refs now target the rendered image-ref asset and no longer force original legacy image export |
| 2026-04-25 | Step image list and workspace made roomier | Image handling became cramped once steps had multiple screenshots | Step images now get more vertical space, the workspace pane starts wider, and the Image workspace uses a wider fitting surface for annotations |
| 2026-04-25 | HTML export preserves blank body lines | Smoke testing showed empty lines in step text disappeared in export preview | Blank lines now render as spacing in HTML export and preview |

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
| 8 MVP hardening | In progress | Image pool model/storage/export and app save/load bridge slices are in place; visible image pool UI, follow-along capture, crop, annotation editor polish, rotate controls, and final polish remain |

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
| 2026-04-25 | `git diff --check` | Pass | Selection-state refactor whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Annotation selection state refactor build; Release tests passed; 24 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Preview mouse-down selection fix whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Preview mouse-down selection fix build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Image list hitbox stabilization build; Release tests passed; 24 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Workspace annotation drag whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Workspace annotation drag build; Release tests passed; 24 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Annotation ordering and drag-size fix whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Annotation ordering and drag-size fix build; Release tests passed; 24 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Compact annotation ordering buttons whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Compact annotation ordering buttons build; Release tests passed; 24 tests passed |
| 2026-04-25 | `git diff --check` | Pass | HTML redact preview styling whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | HTML redact preview styling build; Release tests passed; 24 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Rendered export assets build; Release tests passed; 25 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Opaque redaction whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Opaque redaction build; Release tests passed; 25 tests passed |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Basic label styling build; Release tests passed; 25 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Image pool architecture plan whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Step image ref model/storage validation build; Release tests passed; 28 tests passed after closing a running app process that locked build outputs |
| 2026-04-25 | `git diff --check` | Pass | Step image ref export migration whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Step image ref export migration build; Release tests passed; 29 tests passed |
| 2026-04-25 | `git diff --check` | Pass | App step image ref bridge whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | App step image ref bridge build; Release tests passed; 29 tests passed after closing a running app process that locked build outputs |
| 2026-04-25 | `git diff --check` | Pass | Step image ref body token export fix whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Step image ref body token export fix build; Release tests passed; 29 tests passed |
| 2026-04-25 | `git diff --check` | Pass | Roomier step image list and workspace whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | Roomier step image list and workspace build; Release tests passed; 29 tests passed |
| 2026-04-25 | `git diff --check` | Pass | HTML blank-line export fix whitespace check clean |
| 2026-04-25 | `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` | Pass | HTML blank-line export fix build; Release tests passed; 30 tests passed |

## Known Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Screenshot capture complexity | Medium | Keep capture flow small and local-first |
| Sensitive screenshots | High | Add hard redaction in Milestone 8 so exported output destroys or blurs pixels instead of only using visual overlays |
| Scope creep | High | Keep non-goals explicit in `docs/spec.md` and `AGENTS.md` |
| Dark mode placement | Low | Move the topbar toggle into Settings when a Settings surface exists |
| Image display sizing model | Medium | Introduce per-step image reference metadata before implementing size controls |
| Image pool migration complexity | Medium | Implement migration-compatible model first, then move storage/export/UI in small slices |
| Annotation placement still needs richer direct manipulation | Medium | Basic drag-to-move is added; resize handles and rotate controls remain |
| Annotation editor UI is getting crowded | Medium | Add hide/show or collapsible surfaces during M8 hardening |
| Image list navigation is cramped | Medium | Rework Images area during image pool UI so three or more images are easy to scan and the scrollbar feels natural |
| Image workspace does not fit large images well | Medium | Make the right-side Image workspace fit the available pane better before adding crop/resize handles |

## Known M8 Bugs

| Bug | Severity | Notes |
|---|---|---|
| Asset selection snaps back when Annotations is open | High | Refactored so explicit asset/annotation IDs are the source of truth instead of WPF list selection. Needs user smoke test with the annotation panel expanded. |
| Annotation expander arrow points the wrong way | Low | The hide/show arrow direction is visually confusing and should be corrected or replaced with clearer show/hide affordance. |
| Redact uses solid burn-in, not blur | Medium | Exported Markdown, HTML, and PDF now use generated images with annotations burned in. Redaction is solid dark masking; true blur can be added later if needed. |
| Annotation percentage sliders are confusing | Medium | X/Y/Width/Height values interact awkwardly and should be redesigned or replaced with direct manipulation handles. |
| Annotation controls are still clunky | Medium | Current compact buttons are better grouped but should become a cleaner tool strip or collapsible tool surface. |
| Arrow annotation previews as a plain line | Medium | Arrow should render with a clear arrow head in the app workspace and exported output. |
| Annotation X/Y behavior should use top-left origin | Medium | X/Y should consistently mean the annotation's top-left corner, with width/height changes preserving that anchor. |

## Next Recommended Step

Smoke test the app save/load bridge, then continue Milestone 8 image pool migration by refactoring the visible UI to separate the reusable image pool from selected-step image references.

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1
```
