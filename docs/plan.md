# Milestone Plan

## Current State

Current milestone: Milestone 3 - Markdown And HTML Export.

Status: Milestone 2 is complete. Storage now validates guide projects, reports missing assets, and has smoke coverage for create/save/load and invalid project files.

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

Status: ready.

Scope:

- deterministic Markdown export
- deterministic HTML export
- export file writing
- export smoke tests

Acceptance:

- sample guide exports to Markdown and HTML.
- exported files reference assets with correct relative paths.

## Milestone 4 - Basic UI Shell

Scope:

- create/open/save guide project from UI
- edit guide title and steps
- show step list and selected step editor
- basic dirty-state handling

Acceptance:

- user can create a guide, add steps, save, close, reopen, and continue editing.

## Milestone 5 - Images And Screenshot Capture

Scope:

- import image into project assets
- paste image from clipboard
- follow-along screenshot capture
- attach screenshots to steps

Acceptance:

- screenshot/image assets are copied into `assets/`.
- guide reload preserves image references.

## Milestone 6 - Basic Annotation

Scope:

- rectangle/highlight
- arrow
- label/caption
- optional blur/redaction if feasible

Acceptance:

- annotations are saved in `guide.json`.
- annotated screenshots can be previewed or exported.

## Milestone 7 - PDF Export And Preview

Scope:

- choose free/local PDF approach
- preview guide in app
- export Markdown, HTML, and PDF from UI
- sensitive-content warning before export

Acceptance:

- sample and user-created guide export to all MVP formats.

## Milestone 8 - MVP Hardening

Scope:

- polish primary workflow
- error messages
- file safety
- final validation
- packaging decision

Acceptance:

- end-to-end guide creation works locally.
- validation scripts pass.
- known limitations are documented.
