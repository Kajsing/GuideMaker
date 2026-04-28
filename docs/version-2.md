# Version 2 Backlog

This document parks ideas that are useful, but should not block the MVP/beta build.

Version 2 should be shaped by beta feedback first. Items below are candidates, not commitments.

## Priority 1 - Likely After Beta

- Resizable authoring layout with a larger Steps pane mode for long-form editing.
- Detachable image windows (dock/undock) so step images can be reviewed in separate windows and gathered back into the main workspace.
- Layout reset and simple workspace presets (for example Write, Image Review, and Default) to recover quickly after pane/window changes.
- Real installer or MSIX packaging.
- Persist user settings outside `guide.json`.
- Better workspace image viewing: zoom, pan, fit-to-width, and fit-to-window.
- Better direct manipulation for annotations: resize handles, rotate, and consistent top-left X/Y behavior.
- Better crop editing: resize handles, clear crop affordance, and more precise mouse interaction.
- Cleaner Step/Pool image workflow if beta users find the current tabs confusing.
- More robust Follow along controls: hotkey/manual capture, pause/resume, and clearer capture state.
- Pool based on asset-folder scan with clearer missing-file and untracked-file handling.
- True blur redaction in addition to solid redaction.
- Better beta/error reporting that stays local and does not include guide content by default.

## Priority 2 - Workflow Polish

- Better step text editor with lightweight rich-text capabilities: bold, color, highlight, and fenced code block authoring.
- Inline formatting toolbar and keyboard shortcuts for common text styles.
- Markdown-first editing assist so formatting remains deterministic for Markdown/HTML/PDF export.
- Rename images inline from Step/Pool lists.
- Add image display-size controls per step image reference.
- Add annotation style presets for common guide marks.
- Add font family, border, shadow, and template options for labels.
- Add guide templates for common internal guide types.
- Add export profile options, such as title page, table of contents, page size, and image sizing.
- Add a compact review/checklist before export for sensitive data.
- Add keyboard shortcuts for common authoring actions.
- Add undo/redo for text, image, crop, and annotation edits.
- Improve validation and repair tools for broken project folders.

## Priority 3 - Larger Features

- Video export with audio narration.
- Local screen recording session capture.
- Mouse and keyboard bookmarks during recording.
- Extract screenshots from recording bookmarks.
- Optional publishing integrations, for example Confluence, SharePoint, ServiceNow, GitHub, or Gitea.
- Optional template library for internal guide patterns.
- Optional multi-author or role-aware workflow if Region Midt actually needs it.
- Admin-friendly installation and update flow.
- More advanced PDF layout and branding.

## Wishlist / Experimental

- Narrated video generation from guide steps, recording bookmarks, and speaker notes.
- Local-first text-to-speech for generated narration if quality and licensing are acceptable.
- Cloud/AI voice generation only if privacy, cost, and policy are explicitly re-decided.
- Timeline editor that can align voice, captions, highlights, zoom/crop, and bookmarks.
- Automatic draft video from a recorded workflow, with editable segments before export.
- Optional generated captions/subtitles for video output.
- Optional voice profiles for internal guide videos, if licensing and privacy allow it.

## Still Out Unless Re-Decided

- Cloud storage or sync.
- Telemetry or external diagnostics.
- AI writing/summarization, online language services, or cloud voice generation.
- Paid third-party dependencies.
- Mobile app or web app.

## Notes From MVP Work

- Keep original images reusable and non-destructive.
- Keep `guide.json` as the source of truth.
- Keep export assets generated and disposable.
- Keep everything local-first unless the docs are explicitly updated.
