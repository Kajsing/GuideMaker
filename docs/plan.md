# Milestone Plan

## Current State

Current milestone: Milestone 8 - MVP Hardening.

Status: Milestone 8 is in progress. The first annotation editor controls are implemented.

## Milestone 0 - Research And Decisions

Status: complete.

Acceptance:

- BLOCKER questions are marked DECIDED.
- MVP scope is clear.
- Stack decision is captured.
- Non-goals are explicit.

## Milestone 1 - Repo Skeleton And Core Model

Status: complete.

Scope:

- solution and project layout
- WPF app shell
- core guide model
- project folder constants
- JSON save/load service
- starter sample guide
- initial smoke tests
- validation scripts

Acceptance:

- `dotnet build` passes.
- `dotnet test` passes.
- `powershell -ExecutionPolicy Bypass -File .\scripts\validate.ps1` passes.
- A guide can be serialized to and loaded from a project folder in tests.

## Milestone 2 - Storage Layer Hardening

Status: complete.

Scope:

- project creation/open/save workflow
- validation for malformed `guide.json`
- missing asset handling
- schema version checks
- relative path safety

Acceptance:

- storage tests cover create, load, save, missing files, and invalid JSON.
- sample guide can be opened by storage tests.

## Milestone 3 - Markdown And HTML Export

Status: complete.

Scope:

- deterministic Markdown export
- deterministic HTML export
- export file writing
- export smoke tests

Acceptance:

- sample guide exports to Markdown and HTML.
- exported files reference assets with correct relative paths.

## Milestone 4 - Basic UI Shell

Status: complete.

Scope:

- create/open/save guide project from UI
- edit guide title and steps
- show step list and selected step editor
- basic dirty-state handling

Acceptance:

- user can create a guide, add steps, save, close, reopen, and continue editing.

## Milestone 5 - Images And Screenshot Capture

Status: complete.

Scope:

- import image into project assets
- paste image from clipboard
- follow-along screenshot capture
- attach screenshots to steps

Acceptance:

- screenshot/image assets are copied into `assets/`.
- guide reload preserves image references.

## Milestone 6 - Basic Annotation

Status: complete.

Scope:

- rectangle/highlight
- arrow
- label/caption
- optional blur/redaction if feasible
- decide and implement display-size metadata for step image references if it fits cleanly with annotation/export

Acceptance:

- annotations are saved in `guide.json`.
- annotated screenshots can be previewed or exported.

## Milestone 7 - PDF Export And Preview

Status: complete.

Scope:

- choose free/local PDF approach
- preview guide in app
- export Markdown, HTML, and PDF from UI
- sensitive-content warning before export

Acceptance:

- sample and user-created guide export to all MVP formats.

## Milestone 8 - MVP Hardening

Status: in progress.

Scope:

- polish primary workflow
- introduce image pool plus per-step image references as the foundation for follow-along capture and crop
- keep `assetIds` and step-level `annotations` migration-compatible while the UI moves to `StepImageRef`
- build follow-along capture on top of the image pool
- add crop workflow that preserves original images and renders cropped export assets
- add a usable annotation editor: select, move, resize, and edit label text
- add annotation rotate controls
- review annotation percentage/slider behavior and direct manipulation
- reduce UI clutter with hide/show or collapsible editing surfaces
- fix remaining annotation panel bugs: asset selection snap-back and wrong expander arrow direction
- implement hard redaction for exported output instead of visual-only overlays
- error messages
- file safety
- final validation
- packaging decision

Acceptance:

- end-to-end guide creation works locally.
- images can be kept in a reusable pool and attached to steps without destroying originals.
- exported Markdown, HTML, and PDF use generated images with crop and annotations applied.
- validation scripts pass.
- known limitations are documented.

## Milestone 8 Image Pool Plan

Status: planned.

Implementation order:

1. Add migration-compatible core model types: `StepImageRef` and optional crop bounds.
2. Update storage validation and tests so old `assetIds`/step annotations and new `imageRefs` can coexist safely.
3. Update export rendering to prefer `imageRefs`, applying crop and annotations per step image reference.
4. Refactor app state to show an image pool and selected-step images separately.
5. Change import, paste, and capture so images enter the pool first, then attach to the selected step.
6. Add a crop editor that stores crop on the step image reference and preserves original pool images.
7. Build Follow Along Capture on top of the pool: start capture mode, collect screenshots into the pool, attach to the active step, and stop when GuideMaker is restored.
