# Windows 11 Guide Tool — Codex-klar research- og designpakke

## 0. Executive summary

Projektet er et Windows 11-værktøj til at lave, redigere, organisere og eksportere guides. En guide forstås her som en trinvis vejledning med tekst, billeder/skærmbilleder, markeringer, noter, metadata og eventuelt eksport til formater som Markdown, HTML eller PDF.

Målet er at bygge et praktisk desktopværktøj, der gør det nemt at dokumentere arbejdsgange, fejlretning, supportprocedurer eller interne how-to-guides. Projektet bør starte som en lokal-first MVP uden cloud-afhængighed, så det kan bygges, testes og bruges hurtigt på Windows 11.

Den vigtigste åbne beslutning er scope: Skal værktøjet primært være en guide-editor, et screenshot-/annotation-værktøj, en vidensbase, eller en eksportpipeline? Før implementering skal projektet afklare kerneflowet: “opret guide → tilføj trin → indsæt skærmbilleder → annotér → preview → eksportér”.

### Fakta

- Produktidéen er: “et Windows 11 tool til at lave guides med”.
- Værktøjet skal kunne bygges systematisk af Codex ud fra markdown-dokumenter.
- Dokumentpakken skal hjælpe Codex med at arbejde milestone-baseret uden at drive væk fra målet.

### Antagelser

- Første version er et Windows 11 desktopværktøj.
- Første version gemmer data lokalt.
- Første version skal fokusere på guideproduktion frem for avanceret vidensstyring.
- Brugeren har brug for et praktisk værktøj til tekniske eller administrative guides, ikke et tungt enterprise-CMS.
- Projektet bør have automatiserbare tests og valideringskommandoer.

### Åbne spørgsmål

De åbne spørgsmål er samlet i `docs/questions.md`. Implementering bør ikke starte, før spørgsmål markeret som `BLOCKER` er afklaret.

---

# 1. Komplet afklaringsliste

## 1.1 Forretningsmål

### Skal afklares

1. Hvad er det primære problem, værktøjet skal løse?
   - Hurtigere guideproduktion?
   - Mere ensartede guides?
   - Bedre screenshot/annotation-flow?
   - Bedre eksport til eksisterende videnssystemer?

2. Hvem er første målgruppe?
   - Kun dig selv?
   - IT-drift/support?
   - Kollegaer i en afdeling?
   - Bredere tekniske teams?

3. Hvad er succeskriteriet for MVP?
   - “Jeg kan lave en komplet guide på 10 minutter.”
   - “Jeg kan eksportere en guide til Markdown.”
   - “Jeg kan dokumentere en supportprocedure med skærmbilleder.”

4. Hvilke guides skal værktøjet være bedst til?
   - IT-supportprocedurer.
   - Software-installationsguides.
   - Fejlfindingsrunbooks.
   - Brugervejledninger.
   - Interne driftsprocedurer.

5. Skal værktøjet være personligt, internt eller kommercielt?

### Forslag til MVP-beslutning

MVP skal være et lokalt Windows 11 desktopværktøj til at oprette trinbaserede guides med tekst, screenshots, simple annotations og eksport til Markdown.

---

## 1.2 Brugere og roller

### Mulige roller

- **Guide Author**: Opretter og redigerer guides.
- **Reviewer**: Læser og kommenterer guides, men redigerer ikke nødvendigvis.
- **Guide Consumer**: Bruger den færdige guide.
- **Admin**: Konfigurerer templates, eksportformater og placeringer.

### Skal afklares

1. Skal MVP kun have én brugerrolle?
2. Skal der være login?
3. Skal guides kunne deles mellem brugere?
4. Skal flere kunne redigere samme guide?
5. Skal reviewer-kommentarer være del af første version?

### Forslag til MVP-beslutning

MVP har kun én rolle: lokal `Author`. Ingen login, ingen multi-user, ingen real-time collaboration.

---

## 1.3 Kerneflows

### Primært kerneflow

1. Bruger opretter en ny guide.
2. Bruger angiver titel, beskrivelse og kategori.
3. Bruger tilføjer trin.
4. Hvert trin kan have:
   - titel
   - beskrivelse
   - skærmbillede/billede
   - annotationer
   - advarsel/tip/notefelt
5. Bruger kan omarrangere trin.
6. Bruger kan previewe guide.
7. Bruger eksporterer guide.
8. Bruger gemmer projektfil lokalt.

### Sekundære flows

- Åbn eksisterende guide.
- Dupliker guide.
- Gem som template.
- Eksportér til Markdown.
- Eksportér til HTML.
- Eksportér til PDF.
- Importér billeder fra clipboard eller fil.
- Tag screenshot direkte fra værktøjet.

### Skal afklares

1. Er screenshot capture en del af MVP?
2. Skal annotationer laves inde i appen, eller kan billeder importeres færdigannoteret?
3. Hvilket eksportformat er vigtigst først?
4. Skal guides kunne importeres igen efter eksport?
5. Skal der være en projektfil med alle assets samlet?

### Forslag til MVP-beslutning

MVP inkluderer:

- opret/åbn/gem guideprojekt
- trinliste
- tekst pr. trin
- billeder pr. trin via fil eller clipboard
- simpel preview
- eksport til Markdown

MVP ekskluderer avanceret screenshot capture og avancerede annotationer, medmindre de er absolut nødvendige.

---

## 1.4 Data og integrationer

### Mulige dataobjekter

- Guide
- Step
- Asset
- Annotation
- ExportProfile
- Template
- Tag/Category

### Skal afklares

1. Skal data gemmes som JSON, SQLite eller Markdown-first?
2. Skal billeder gemmes i en assets-mappe?
3. Skal projektformatet være menneskelæsbart?
4. Skal guides kunne versioneres med Git?
5. Skal der senere integreres med:
   - Confluence
   - SharePoint
   - GitHub/Gitea
   - ServiceNow
   - local folder sync
   - PDF-printer

### Forslag til MVP-beslutning

Brug et mappebaseret projektformat:

```text
my-guide/
  guide.json
  assets/
    step-001.png
    step-002.png
  exports/
    guide.md
```

`guide.json` er source of truth i MVP. Markdown er eksportformat, ikke primær lagring.

---

## 1.5 UI/UX

### Skal afklares

1. Skal UI være desktop-native eller webbaseret desktop shell?
2. Skal appen have mørkt tema?
3. Skal den ligne en editor, en wizard eller et dokumentværktøj?
4. Skal der være drag-and-drop for trin?
5. Skal billeder kunne paste direkte fra clipboard?
6. Skal preview være live?
7. Skal keyboard shortcuts prioriteres?

### Forslag til MVP-layout

Desktop UI med tre områder:

```text
+----------------------------------------------------+
| Toolbar: New Open Save Export Preview              |
+-------------------+--------------------------------+
| Step list         | Step editor                    |
| 1. Intro          | Title                          |
| 2. Open system    | Body text                      |
| 3. Verify result  | Image / asset area             |
|                   | Notes / warning / tip          |
+-------------------+--------------------------------+
| Status bar: saved / unsaved / export result         |
+----------------------------------------------------+
```

---

## 1.6 Sikkerhed og compliance

### Skal afklares

1. Kan guides indeholde persondata?
2. Kan skærmbilleder indeholde CPR-numre, patientdata, tokens eller interne systemnavne?
3. Skal appen have advarsler ved eksport?
4. Skal der være metadata-scrubbing af billeder?
5. Skal projektfiler kunne krypteres?
6. Skal appen kunne bruges i en offentlig/kommunal/regional IT-kontekst?
7. Må appen bruge cloudtjenester?
8. Skal den fungere offline?

### Forslag til MVP-beslutning

MVP er offline og lokal-only. Ingen cloudsync. Ingen telemetry. Ingen login.

MVP bør dog have en eksport-advarsel:

> “Kontrollér at guiden ikke indeholder persondata, hemmelige nøgler eller interne oplysninger, før den deles.”

---

## 1.7 Drift og deployment

### Skal afklares

1. Skal appen være portable eller installerbar?
2. Skal den kunne installeres uden adminrettigheder?
3. Skal den pakkes som `.exe`, MSIX eller zip?
4. Skal den auto-opdatere?
5. Skal den køre i miljøer med begrænset internet?
6. Skal den kunne bruges på arbejds-pc’er med restriktive policies?

### Forslag til MVP-beslutning

MVP leveres som lokal dev-build først og senere som portable zip eller installer uden admin, hvis teknologistakken tillader det.

---

## 1.8 Test og validering

### Skal afklares

1. Hvilke kommandoer skal altid virke?
2. Skal UI testes automatisk?
3. Skal eksport-output snapshot-testes?
4. Skal projektfiler valideres mod schema?
5. Skal der være sample guides som testdata?

### Forslag til MVP-test

- Unit tests for guide data model.
- Unit tests for Markdown export.
- Schema validation for `guide.json`.
- Smoke test for app startup.
- Manual UX checklist for create/save/open/export.

---

## 1.9 Non-goals

### Foreslåede non-goals for MVP

- Ingen cloudsync.
- Ingen multi-user collaboration.
- Ingen login/rollebaseret adgang.
- Ingen Confluence/SharePoint/ServiceNow-integration i MVP.
- Ingen avanceret billedredigering.
- Ingen AI-generering af guides i første version.
- Ingen mobilapp.
- Ingen browser-extension.
- Ingen fuld CMS-funktionalitet.

---

# 2. Foreslået repo-struktur

```text
windows-guide-tool/
  AGENTS.md
  README.md
  docs/
    spec.md
    questions.md
    architecture.md
    plan.md
    implement.md
    status.md
  skills/
    codex-docpack-from-software-idea/
      SKILL.md
  src/
    app/
    core/
    export/
    storage/
    ui/
  tests/
    unit/
    integration/
    fixtures/
      sample-guide/
  scripts/
    validate.ps1
    test.ps1
    package.ps1
  .gitignore
```

Hvis projektet senere vælger en specifik stack, kan `src/` tilpasses. Eksempler:

```text
src/GuideTool.App/          # C#/.NET/WPF eller WinUI
src/GuideTool.Core/
src/GuideTool.Tests/
```

eller:

```text
src/app/                    # Electron/Tauri frontend
src/core/
src-tauri/                  # Hvis Tauri vælges
```

---

# 3. docs/spec.md

```md
# docs/spec.md — Windows 11 Guide Tool Specification

## 1. Product summary

Windows 11 Guide Tool is a desktop application for creating structured step-by-step guides with text, images/screenshots, notes, and exportable output.

The MVP focuses on local guide authoring and Markdown export.

## 2. Goals

- Create a new guide from scratch.
- Add, edit, reorder, and delete guide steps.
- Attach one or more images to a step.
- Paste images from clipboard or import from file.
- Preview the guide before export.
- Export a guide to Markdown.
- Save and reopen guide projects locally.
- Keep project data in a predictable, testable format.

## 3. Non-goals for MVP

- Cloud sync.
- Multi-user editing.
- Login/authentication.
- Confluence/SharePoint/ServiceNow publishing.
- Advanced image editing.
- AI-assisted guide writing.
- Browser extension.
- Mobile support.

## 4. Target platform

- Windows 11.
- Local desktop usage.
- Offline-first.

## 5. Primary user

Guide Author: a technical or semi-technical user who needs to document a process, support workflow, installation, troubleshooting path, or repeatable procedure.

## 6. Core objects

### Guide

Fields:

- id
- title
- description
- category
- tags
- createdAt
- updatedAt
- steps

### Step

Fields:

- id
- order
- title
- body
- noteType: none | info | warning | danger | success
- noteText
- assets

### Asset

Fields:

- id
- type: image
- filePath
- altText
- caption

## 7. Core user stories

### US-001: Create guide

As a Guide Author, I want to create a new guide so I can document a process.

Acceptance criteria:

- User can create a new empty guide.
- User can enter title and description.
- Unsaved changes are visible in the UI.
- User can save the guide to a local folder.

### US-002: Add steps

As a Guide Author, I want to add steps so the guide can explain a process in order.

Acceptance criteria:

- User can add a step.
- User can edit step title and body.
- User can reorder steps.
- User can delete a step after confirmation.

### US-003: Add images

As a Guide Author, I want to add screenshots or images to a step.

Acceptance criteria:

- User can import an image from file.
- User can paste an image from clipboard if available.
- Image is copied into the guide project's assets folder.
- Image remains available after closing and reopening the guide.

### US-004: Preview guide

As a Guide Author, I want to preview the guide before exporting.

Acceptance criteria:

- Preview shows guide title, description, steps, notes, and images.
- Step order in preview matches step list.
- Missing images are shown as warnings, not crashes.

### US-005: Export Markdown

As a Guide Author, I want to export the guide to Markdown.

Acceptance criteria:

- Export produces a `.md` file.
- Markdown includes title, description, all steps, notes, and image references.
- Exported image paths are relative.
- Export can be repeated without corrupting the project.

## 8. Project format

A saved guide is a folder:

```text
my-guide/
  guide.json
  assets/
  exports/
```

`guide.json` is the source of truth.

## 9. Security requirements

- App must not send guide content to external services in MVP.
- App must not include telemetry in MVP.
- App should warn users before export that screenshots may contain sensitive information.
- App should handle malformed project files gracefully.

## 10. Performance requirements

- App startup should be fast enough for daily use.
- Guides with at least 100 steps and 100 images should remain usable.
- Markdown export for a 100-step guide should complete without noticeable delay on a normal Windows 11 machine.

## 11. Done when

MVP is done when a user can create, save, reopen, preview, and export a guide with at least 5 steps and images, and the validation/test commands pass.
```

---

# 4. docs/questions.md

```md
# docs/questions.md — Open Questions and Decisions

## Status legend

- BLOCKER: Must be answered before implementation.
- IMPORTANT: Should be answered before the relevant milestone.
- LATER: Can wait until after MVP.
- DECIDED: Answered and accepted.

## 1. Business goals

### Q-001 — What is the MVP success criterion?

Status: BLOCKER  
Default assumption: A user can create, save, reopen, preview, and export a guide to Markdown.

Decision:

```text
TBD
```

### Q-002 — Who is the first user?

Status: BLOCKER  
Default assumption: Single local author.

Decision:

```text
TBD
```

### Q-003 — Is this personal, internal work tool, or commercial product?

Status: IMPORTANT  
Default assumption: Personal/internal tool first.

Decision:

```text
TBD
```

## 2. Users and roles

### Q-004 — Does MVP need login?

Status: BLOCKER  
Default assumption: No.

Decision:

```text
TBD
```

### Q-005 — Does MVP need multiple roles?

Status: IMPORTANT  
Default assumption: No. Only Author.

Decision:

```text
TBD
```

## 3. Core flows

### Q-006 — Is screenshot capture part of MVP?

Status: BLOCKER  
Default assumption: No direct capture in MVP; image import and clipboard paste only.

Decision:

```text
TBD
```

### Q-007 — Is annotation part of MVP?

Status: BLOCKER  
Default assumption: Not advanced annotation. Captions and alt text only.

Decision:

```text
TBD
```

### Q-008 — What is the first export format?

Status: BLOCKER  
Default assumption: Markdown.

Decision:

```text
TBD
```

## 4. Data and integrations

### Q-009 — What is source of truth?

Status: BLOCKER  
Default assumption: `guide.json`.

Decision:

```text
TBD
```

### Q-010 — Should project files be Git-friendly?

Status: IMPORTANT  
Default assumption: Yes.

Decision:

```text
TBD
```

### Q-011 — Does MVP integrate with Confluence, SharePoint, ServiceNow, GitHub, or Gitea?

Status: BLOCKER  
Default assumption: No integrations in MVP.

Decision:

```text
TBD
```

## 5. UI/UX

### Q-012 — Which UI stack should be used?

Status: BLOCKER  
Options:

- C#/.NET + WPF
- C#/.NET + WinUI 3
- Electron
- Tauri
- Other

Default assumption: C#/.NET desktop stack unless there is a strong reason to use web UI.

Decision:

```text
TBD
```

### Q-013 — Should dark mode be included in MVP?

Status: IMPORTANT  
Default assumption: Use system theme if easy; otherwise defer.

Decision:

```text
TBD
```

### Q-014 — Should drag-and-drop reordering be included in MVP?

Status: IMPORTANT  
Default assumption: Yes, if supported cheaply by the chosen UI stack.

Decision:

```text
TBD
```

## 6. Security and compliance

### Q-015 — Can guide content contain sensitive data?

Status: BLOCKER  
Default assumption: Yes, screenshots may contain sensitive/internal data.

Decision:

```text
TBD
```

### Q-016 — Is cloud usage allowed?

Status: BLOCKER  
Default assumption: No cloud in MVP.

Decision:

```text
TBD
```

### Q-017 — Is telemetry allowed?

Status: BLOCKER  
Default assumption: No telemetry in MVP.

Decision:

```text
TBD
```

## 7. Drift and deployment

### Q-018 — Does the app need admin-free install?

Status: IMPORTANT  
Default assumption: Prefer portable/admin-free.

Decision:

```text
TBD
```

### Q-019 — How should the MVP be packaged?

Status: IMPORTANT  
Default assumption: Dev build first; packaging later.

Decision:

```text
TBD
```

## 8. Test and validation

### Q-020 — Which commands must pass before a milestone is accepted?

Status: BLOCKER  
Default assumption:

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

Decision:

```text
TBD
```

## 9. Non-goals

### Q-021 — Which tempting features are explicitly out of MVP?

Status: BLOCKER  
Default assumption:

- cloud sync
- multi-user editing
- publishing integrations
- AI writing
- advanced image editing

Decision:

```text
TBD
```
```

---

# 5. docs/architecture.md

```md
# docs/architecture.md — Windows 11 Guide Tool Architecture

## 1. Architecture goals

- Keep guide data separate from UI.
- Keep export logic testable without launching the desktop app.
- Keep project format stable and documented.
- Make MVP small enough to build safely.
- Avoid cloud dependencies.
- Enable future integrations without baking them into the core model.

## 2. Proposed logical architecture

```text
UI Layer
  ↓
Application Layer
  ↓
Core Domain Model
  ↓              ↓
Storage Layer    Export Layer
```

## 3. Components

### 3.1 UI Layer

Responsibilities:

- Display guide list/step list/editor/preview.
- Collect user input.
- Handle file picker and clipboard paste.
- Show save/export status.
- Show validation errors.

Must not:

- Own guide serialization rules.
- Own Markdown generation.
- Contain business logic that cannot be tested separately.

### 3.2 Application Layer

Responsibilities:

- Coordinate user actions.
- Create/open/save guide projects.
- Add/reorder/delete steps.
- Attach assets.
- Request export.
- Track dirty/clean state.

### 3.3 Core Domain Model

Responsibilities:

- Define Guide, Step, Asset, NoteType.
- Validate required fields.
- Maintain step order.
- Provide model-level invariants.

Invariants:

- Guide must have an id.
- Guide must have a title before export.
- Step ids must be unique within a guide.
- Step order must be deterministic.
- Asset references must resolve or be reported as warnings.

### 3.4 Storage Layer

Responsibilities:

- Read/write `guide.json`.
- Create expected folder structure.
- Copy imported images into `assets/`.
- Resolve relative paths.
- Handle malformed project files safely.

### 3.5 Export Layer

Responsibilities:

- Convert Guide model to Markdown.
- Copy or reference assets correctly.
- Produce deterministic output for tests.

## 4. Project file format

```text
my-guide/
  guide.json
  assets/
    <asset-id>.<ext>
  exports/
    guide.md
```

Example `guide.json`:

```json
{
  "schemaVersion": 1,
  "id": "guide_01H...",
  "title": "Example Guide",
  "description": "A short guide.",
  "category": "Support",
  "tags": ["windows", "support"],
  "createdAt": "2026-04-24T10:00:00Z",
  "updatedAt": "2026-04-24T10:10:00Z",
  "steps": [
    {
      "id": "step_01H...",
      "order": 1,
      "title": "Open settings",
      "body": "Open Windows Settings.",
      "noteType": "info",
      "noteText": "Use Win+I as shortcut.",
      "assets": [
        {
          "id": "asset_01H...",
          "type": "image",
          "filePath": "assets/settings.png",
          "altText": "Windows Settings",
          "caption": "Windows Settings start page"
        }
      ]
    }
  ]
}
```

## 5. Export format

Markdown export should be deterministic:

```md
# Guide title

Guide description.

## 1. Step title

Step body.

> [!NOTE]
> Note text.

![Alt text](assets/image.png)

_Caption_
```

## 6. Error handling

- Invalid JSON should show a readable error.
- Missing asset should show warning in preview/export.
- Export failure should not corrupt project.
- Save should be atomic where possible.

## 7. Future extension points

- `IExporter`: Markdown, HTML, PDF, Confluence.
- `IAssetImporter`: file, clipboard, screenshot capture.
- `IProjectStorage`: local folder, zip, database.
- `ITemplateProvider`: built-in and user templates.

## 8. Recommended MVP stack decision

Default recommendation: C#/.NET with WPF or WinUI 3.

Reasoning:

- Native Windows fit.
- Good file/clipboard integration.
- Good packaging path.
- Strong testability for core/export/storage libraries.

Alternative: Tauri if web UI flexibility is more important than native .NET familiarity.

## 9. Done when

Architecture is ready when:

- Data model is documented.
- Project format is documented.
- Components have clear responsibilities.
- Export flow can be tested without UI.
- Open stack decisions are resolved in `docs/questions.md`.
```

---

# 6. docs/plan.md

```md
# docs/plan.md — Milestone Plan

## Guiding principle

Each milestone must produce a working, reviewable increment. Do not build speculative features before the MVP path works end-to-end.

## Validation commands

Default commands:

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

If the selected stack uses different commands, update this section before implementation starts.

Example .NET commands:

```powershell
dotnet format --verify-no-changes
dotnet build
dotnet test
```

## Milestone 0 — Research and decisions

### Goal

Resolve blockers and choose the implementation stack.

### Tasks

- Review `docs/questions.md`.
- Mark all BLOCKER questions as DECIDED.
- Confirm MVP scope.
- Confirm target stack.
- Confirm validation commands.

### Acceptance criteria

- No BLOCKER questions remain unresolved.
- `docs/spec.md` reflects confirmed MVP scope.
- `docs/architecture.md` reflects chosen stack.
- `docs/plan.md` validation commands are accurate.

### Done when

Research phase is done when:

- The MVP user flow is clear.
- The stack is selected.
- The data format is selected.
- The first export format is selected.
- Security boundaries are explicit.
- Codex can start implementation without guessing.

---

## Milestone 1 — Repo skeleton and core model

### Goal

Create the project structure and core guide model.

### Tasks

- Create application solution/project skeleton.
- Create core domain model.
- Add schema/version concept.
- Add unit tests for guide/step/asset model.
- Add validation scripts.

### Acceptance criteria

- Repo builds.
- Core model tests pass.
- Validation command exists and runs.
- No UI work is required for this milestone.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

---

## Milestone 2 — Storage layer

### Goal

Save and load guide projects from local folders.

### Tasks

- Implement project folder creation.
- Implement `guide.json` serialization.
- Implement load from folder.
- Implement asset folder conventions.
- Add tests for save/load roundtrip.
- Add tests for malformed file handling.

### Acceptance criteria

- A guide can be saved to disk.
- The same guide can be loaded back with matching content.
- Invalid project files fail gracefully.
- Paths are relative where appropriate.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

---

## Milestone 3 — Markdown export

### Goal

Export a guide to Markdown deterministically.

### Tasks

- Implement Markdown exporter.
- Include guide metadata, steps, notes, and images.
- Use relative image paths.
- Add snapshot/golden-file tests.
- Add missing asset warning behavior.

### Acceptance criteria

- Export creates `exports/guide.md`.
- Output is deterministic.
- Test fixture exports match expected Markdown.
- Missing images do not crash export.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

---

## Milestone 4 — Basic UI shell

### Goal

Create a desktop UI that can display and edit guide data.

### Tasks

- Create main window.
- Add guide metadata editor.
- Add step list.
- Add step editor.
- Add save/open/new actions.
- Track unsaved state.

### Acceptance criteria

- User can create a new guide in UI.
- User can add/edit/delete steps.
- User can save and reopen a guide.
- App does not crash on common empty states.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

Manual smoke test:

```text
1. Launch app.
2. Create new guide.
3. Add 3 steps.
4. Save guide.
5. Close app.
6. Reopen guide.
7. Confirm content is preserved.
```

---

## Milestone 5 — Images and clipboard import

### Goal

Allow users to attach images to steps.

### Tasks

- Add image import from file.
- Add paste from clipboard if image exists.
- Copy image into project assets folder.
- Show image in step editor.
- Preserve image references on save/load.

### Acceptance criteria

- User can import an image for a step.
- User can paste an image from clipboard.
- Image is copied into assets folder.
- Image remains visible after reopen.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

Manual smoke test:

```text
1. Create guide.
2. Add step.
3. Paste screenshot from clipboard.
4. Save guide.
5. Reopen guide.
6. Confirm image appears.
```

---

## Milestone 6 — Preview and export UI

### Goal

Expose preview and Markdown export through UI.

### Tasks

- Add preview panel or preview window.
- Add export action.
- Show export success/failure.
- Add sensitive-content reminder before export.

### Acceptance criteria

- Preview shows current guide.
- Export button creates Markdown file.
- Export status is visible.
- Sensitive-data reminder appears before export.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

Manual smoke test:

```text
1. Create guide with 5 steps and images.
2. Preview guide.
3. Export guide.
4. Open exported Markdown.
5. Confirm content and image links are correct.
```

---

## Milestone 7 — MVP hardening

### Goal

Make the app reliable enough for real use.

### Tasks

- Improve empty states.
- Add user-friendly errors.
- Add unsaved-changes prompts.
- Add sample guide fixture.
- Review accessibility basics.
- Update README.
- Update status log.

### Acceptance criteria

- MVP flow works end-to-end.
- Known errors are handled gracefully.
- README explains how to run, test, and use the app.
- `docs/status.md` is up to date.

### Validation

```powershell
./scripts/validate.ps1
./scripts/test.ps1
```

## Implementation phase done when

Implementation is done when:

- User can create a guide.
- User can add at least 5 steps.
- User can attach images.
- User can save and reopen the project.
- User can preview the guide.
- User can export to Markdown.
- All validation commands pass.
- README contains run/test/build instructions.
- `docs/status.md` contains final MVP decision log.
```

---

# 7. docs/implement.md

```md
# docs/implement.md — Codex Implementation Runbook

## 1. Purpose

This runbook tells Codex how to implement the project safely and systematically.

Codex must treat this repository as a milestone-based software project. Do not jump ahead. Do not add speculative features. Do not silently change scope.

## 2. Required reading order

Before changing code, read:

1. `AGENTS.md`
2. `docs/spec.md`
3. `docs/questions.md`
4. `docs/architecture.md`
5. `docs/plan.md`
6. `docs/status.md`

## 3. Before implementation

Codex must check `docs/questions.md`.

If any `BLOCKER` question is not marked `DECIDED`, Codex must not implement product code. Instead, Codex should produce a concise list of unresolved blockers and propose default decisions.

## 4. Milestone workflow

For each milestone:

1. Read the milestone in `docs/plan.md`.
2. Confirm the milestone goal.
3. Identify files expected to change.
4. Implement the smallest useful increment.
5. Add or update tests.
6. Run validation commands.
7. Update `docs/status.md`.
8. Summarize:
   - what changed
   - tests run
   - decisions made
   - risks or follow-ups

## 5. Coding rules

- Keep domain logic out of UI where practical.
- Prefer deterministic output.
- Prefer small files and clear names.
- Do not introduce cloud services in MVP.
- Do not add telemetry in MVP.
- Do not add authentication unless explicitly decided.
- Do not add integrations unless explicitly planned.
- Handle file errors gracefully.
- Keep project format backward-compatible where possible.

## 6. Testing rules

Every implementation milestone should include tests unless impossible.

Required test categories:

- Model validation tests.
- Save/load roundtrip tests.
- Markdown export tests.
- Error handling tests for malformed projects.

UI behavior may use manual smoke tests until automated UI tests are introduced.

## 7. Validation commands

Run the commands listed in `docs/plan.md`.

If commands fail:

1. Fix the failure if it is caused by the current changes.
2. If failure is unrelated, document it in `docs/status.md`.
3. Never claim success unless validation actually passed or the limitation is clearly documented.

## 8. Status log rules

After each milestone or meaningful change, update `docs/status.md` with:

- date
- milestone
- summary
- decisions
- validation result
- open issues

## 9. Scope control

Codex must not implement these without explicit approval:

- cloud sync
- login/auth
- multi-user collaboration
- Confluence/SharePoint/ServiceNow publishing
- AI guide generation
- advanced image editor
- auto-update system

## 10. Completion format

At the end of each task, Codex should report:

```text
Summary:
- ...

Files changed:
- ...

Validation:
- command: result

Decisions:
- ...

Follow-ups:
- ...
```
```

---

# 8. docs/status.md

```md
# docs/status.md — Project Status and Decision Log

## Current phase

Phase: Research / clarification  
Current milestone: Milestone 0  
Status: Not started

## Decision log

| Date | Decision | Reason | Impact |
|---|---|---|---|
| TBD | MVP is local-first | Reduces complexity and security risk | No cloud features in MVP |
| TBD | Markdown is first export format | Easy to test and version | PDF/HTML deferred |
| TBD | `guide.json` is source of truth | Stable app model | Markdown is export only |

## Open blockers

| ID | Question | Owner | Status |
|---|---|---|---|
| Q-001 | MVP success criterion | Human | Open |
| Q-002 | First user | Human | Open |
| Q-006 | Screenshot capture in MVP? | Human | Open |
| Q-007 | Annotation in MVP? | Human | Open |
| Q-008 | First export format | Human | Open |
| Q-009 | Source of truth | Human | Open |
| Q-012 | UI stack | Human | Open |
| Q-015 | Sensitive data assumptions | Human | Open |
| Q-016 | Cloud allowed? | Human | Open |
| Q-017 | Telemetry allowed? | Human | Open |
| Q-020 | Validation commands | Human/Codex | Open |
| Q-021 | MVP non-goals | Human | Open |

## Milestone progress

| Milestone | Status | Notes |
|---|---|---|
| 0 Research and decisions | Not started | Resolve blockers first |
| 1 Repo skeleton and core model | Not started | Waiting for stack decision |
| 2 Storage layer | Not started | Depends on model |
| 3 Markdown export | Not started | Depends on storage/model |
| 4 Basic UI shell | Not started | Depends on stack |
| 5 Images and clipboard import | Not started | Depends on UI/storage |
| 6 Preview and export UI | Not started | Depends on exporter/UI |
| 7 MVP hardening | Not started | Final polish |

## Validation log

| Date | Command | Result | Notes |
|---|---|---|---|
| TBD | TBD | TBD | TBD |

## Known risks

| Risk | Severity | Mitigation |
|---|---|---|
| Scope creep | High | Keep MVP non-goals explicit |
| UI stack uncertainty | High | Decide before implementation |
| Sensitive screenshots | High | Local-only MVP and export warning |
| Export complexity | Medium | Start with Markdown only |
| Data format churn | Medium | Version `guide.json` from start |

## Notes

Use this file as the source of truth for decisions made during implementation. If Codex makes a decision, it must be recorded here.
```

---

# 9. AGENTS.md

```md
# AGENTS.md — Repository Instructions for Codex

## Project

This repository contains a Windows 11 desktop tool for creating step-by-step guides with text, images, preview, local save/load, and Markdown export.

## Required reading

Before making changes, read these files in order:

1. `docs/spec.md`
2. `docs/questions.md`
3. `docs/architecture.md`
4. `docs/plan.md`
5. `docs/implement.md`
6. `docs/status.md`

## Blocker rule

If `docs/questions.md` contains unresolved `BLOCKER` questions, do not implement product code. Instead, list the blockers and propose concrete default answers.

## Scope rule

Do not add features outside the current milestone in `docs/plan.md`.

Do not implement the following unless explicitly approved in `docs/status.md`:

- cloud sync
- login/authentication
- multi-user collaboration
- Confluence/SharePoint/ServiceNow publishing
- AI guide generation
- advanced image editing
- telemetry
- auto-update

## Architecture rule

Keep these concerns separate:

- UI
- application coordination
- domain model
- storage
- export

Export and storage logic must be testable without launching the UI.

## Testing rule

Run the validation commands listed in `docs/plan.md` before reporting completion.

If validation cannot be run, say why and record it in `docs/status.md`.

## Status rule

After meaningful changes, update `docs/status.md` with:

- what changed
- what was decided
- what validation was run
- what remains open

## Response format

When finishing a task, respond with:

```text
Summary:
- ...

Files changed:
- ...

Validation:
- ...

Decisions:
- ...

Follow-ups:
- ...
```
```

---

# 10. Genbrugelig SKILL.md

```md
---
name: codex-docpack-from-software-idea
description: Use this skill only when the user asks to turn a software idea, rough product concept, unclear feature request, or early project description into a Codex-ready markdown documentation package for implementation. The skill creates structured docs such as spec.md, questions.md, architecture.md, plan.md, implement.md, status.md, AGENTS.md, and milestone-based acceptance criteria. Do not use this skill for normal coding tasks, bug fixes, code review, general product brainstorming, or implementation after the documentation package already exists.
---

# Skill: Codex-ready documentation package from software idea

## Purpose

Convert an unclear or early-stage software idea into a structured documentation package that Codex can use to implement the project safely and milestone by milestone.

The output must separate facts, assumptions, and open questions. It must reduce ambiguity before implementation. It must favor buildable, testable, maintainable software over vague product language.

## Trigger rules

Use this skill when the user asks for any of the following:

- Turn a software idea into a Codex-ready project.
- Create a markdown documentation package for a coding agent.
- Create `AGENTS.md` plus project docs for Codex.
- Convert a rough feature description into implementation-ready specs.
- Make a milestone plan with acceptance criteria for a software project.
- Create a reusable project structure for agentic software development.
- Identify clarification questions before Codex implements a project.

Typical trigger phrases:

- “lav en Codex-klar pakke”
- “lav docs til Codex”
- “omdan den her softwareidé til markdown-filer”
- “lav en skill.md til at beskrive softwareprojekter”
- “hjælp mig med at gøre den her idé klar til implementering”
- “lav spec, questions, architecture, plan og AGENTS.md”

## Non-trigger rules

Do not use this skill when:

- The user asks for a direct code change.
- The user asks to debug a specific error.
- The user asks for a code review only.
- The user asks for general architecture advice but not a Codex-ready doc package.
- The user asks for product naming, marketing copy, or UI copy only.
- The repository already has adequate docs and the task is implementation.
- The user only wants a quick answer or opinion.

## Inputs to collect

If missing, infer cautiously and mark assumptions explicitly.

Required project inputs:

- Project name or working title.
- Target platform.
- Primary user.
- Main problem being solved.
- Core user flow.
- Expected data/storage needs.
- External integrations.
- Security/compliance constraints.
- Deployment expectations.
- Preferred technology stack, if any.
- Non-goals.

If the project is underspecified, produce a structured clarification list before implementation planning.

## Output structure

Produce the following sections:

1. Executive summary.
2. Facts, assumptions, and open questions.
3. Clarification list grouped by:
   - business goals
   - users and roles
   - core flows
   - data and integrations
   - UI/UX
   - security and compliance
   - operations and deployment
   - testing and validation
   - non-goals
4. Recommended repository structure.
5. `docs/spec.md`.
6. `docs/questions.md`.
7. `docs/architecture.md`.
8. `docs/plan.md`.
9. `docs/implement.md`.
10. `docs/status.md`.
11. `AGENTS.md`.
12. Risks and unknowns.
13. Recommended work order.

## Documentation rules

- Use concrete headings.
- Make each generated document ready to save as a `.md` file.
- Use clear acceptance criteria.
- Include “Done when” for research and implementation phases.
- Clearly mark blockers.
- Prefer explicit non-goals over silent assumptions.
- Avoid fluffy product language.
- Avoid implementing code in this skill unless explicitly requested.
- Keep Codex instructions operational and enforceable.

## Codex-readiness rules

The generated package must ensure Codex can answer:

- What am I building?
- What is out of scope?
- What decisions are still open?
- What is the current milestone?
- What files should I read before coding?
- What validation commands should I run?
- What acceptance criteria define completion?
- Where should I record decisions and progress?

## Recommended document responsibilities

### `docs/spec.md`

Defines product scope, user stories, functional requirements, non-goals, and acceptance criteria.

### `docs/questions.md`

Lists unresolved questions and marks them as `BLOCKER`, `IMPORTANT`, `LATER`, or `DECIDED`.

### `docs/architecture.md`

Defines system components, data model, boundaries, storage format, integration points, and future extension points.

### `docs/plan.md`

Defines milestones, tasks, acceptance criteria, and validation commands.

### `docs/implement.md`

Acts as the Codex runbook. Defines how Codex should work through milestones, update status, run tests, and avoid scope creep.

### `docs/status.md`

Tracks phase, milestone progress, decisions, validation results, open blockers, and known risks.

### `AGENTS.md`

Provides concise repo-level instructions for Codex. It must stay short and point to the detailed docs.

## Quality checklist

Before finalizing, verify:

- All documents are internally consistent.
- Blockers are explicit.
- Assumptions are visible.
- Non-goals are visible.
- Milestones are ordered logically.
- Each milestone has acceptance criteria.
- Validation commands are included or marked TBD.
- `AGENTS.md` is concise.
- The skill trigger is narrow enough to avoid accidental use.

## Final response style

When returning the package to the user:

- Start with the most important decision points.
- Then provide the document package.
- Keep implementation advice separate from documentation output.
- Do not pretend unresolved questions are resolved.
```

---

# 11. Største risici og ukendte faktorer

## 11.1 Scope creep

Risiko: Værktøjet kan hurtigt vokse fra “guide editor” til CMS, screenshotværktøj, Confluence-klient, AI-writer og dokumentportal.

Mitigation:

- Hold MVP til create/save/open/preview/export.
- Skriv non-goals tydeligt.
- Brug milestone-planen som låge.

## 11.2 Uklar UI-stack

Risiko: Stack-valg påvirker alt: packaging, clipboard, screenshot capture, UI-test, Windows integration.

Mitigation:

- Beslut stack i Milestone 0.
- Start ikke implementation før Q-012 er DECIDED.

## 11.3 Sensitive screenshots

Risiko: Guides kan indeholde persondata, patientdata, interne systemnavne, tokens eller fejlbeskeder.

Mitigation:

- Local-only MVP.
- Ingen telemetry.
- Eksport-advarsel.
- Eventuelt senere: OCR/sensitive-data warning, metadata scrub, redaction tools.

## 11.4 Annotation-kompleksitet

Risiko: Et godt annotationværktøj er næsten et separat produkt.

Mitigation:

- MVP bruger captions/alt text og image import.
- Avancerede pile, bokse, sløring og markeringer udskydes.

## 11.5 Eksportformat-spredning

Risiko: PDF, HTML, Markdown, Confluence og Word kan hver især blive store opgaver.

Mitigation:

- Markdown først.
- Byg `IExporter`-lignende separation.
- Test eksport deterministisk.

## 11.6 Projektformat-churn

Risiko: Hvis dataformatet ændres for ofte, bliver gemte guides ustabile.

Mitigation:

- Brug `schemaVersion` fra start.
- Hold `guide.json` simpelt.
- Tilføj migration senere.

## 11.7 Codex driver væk fra målet

Risiko: Coding agenten bygger “smarte” features i stedet for MVP.

Mitigation:

- Kort AGENTS.md.
- Tydelig implementerings-runbook.
- BLOCKER-regel.
- Status-log efter hver milestone.

---

# 12. Anbefalet rækkefølge

## 12.1 Research

Formål:

- Afklare platform, stack, scope og sikkerhedsgrænser.

Done when:

- Alle BLOCKER-spørgsmål i `docs/questions.md` er markeret DECIDED.
- MVP-flowet er bekræftet.
- Stack er valgt.
- Validation commands er valgt.

## 12.2 Afklaring

Formål:

- Omsætte åbne spørgsmål til konkrete beslutninger.

Output:

- Opdateret `docs/questions.md`.
- Opdateret `docs/status.md`.

Done when:

- Codex ikke længere skal gætte på MVP-scope.

## 12.3 Design

Formål:

- Låse arkitektur, dataformat og komponentgrænser.

Output:

- Opdateret `docs/spec.md`.
- Opdateret `docs/architecture.md`.

Done when:

- Data model, storage og export boundaries er klare.

## 12.4 Plan

Formål:

- Bryde arbejdet ned i milestones.

Output:

- Opdateret `docs/plan.md`.
- Opdateret valideringsstrategi.

Done when:

- Hver milestone har mål, tasks, acceptkriterier og validering.

## 12.5 Implementering

Formål:

- Bygge MVP trinvist.

Output:

- Kode.
- Tests.
- Opdateret `docs/status.md`.

Done when:

- Implementation phase done-kriterier i `docs/plan.md` er opfyldt.

## 12.6 Review

Formål:

- Kontrollere at produktet matcher spec og ikke bare “virker på min maskine”.

Checklist:

- Matcher MVP spec?
- Er non-goals overholdt?
- Passer dataformat og eksport?
- Er tests gode nok?
- Er README brugbar?
- Er status-log ærlig?
- Hvad skal ind i næste version?

Done when:

- MVP kan bruges end-to-end.
- Kendte begrænsninger er dokumenteret.
- Næste version kan planlægges uden at rode MVP’en til.

