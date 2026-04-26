# GuideMaker Manual

GuideMaker is a local Windows app for creating technical step-by-step guides with screenshots, annotations, preview, and export.

Everything in the MVP is local. Guide data, screenshots, preview files, and exports stay on the machine.

## Start The App

If you received a beta package:

1. Unzip the package to a local folder.
2. Run `GuideMaker.App.exe`.

If Windows warns about the app, this is expected for an unsigned beta build.

## Create Or Open A Guide

- `New` creates a guide project in a folder you choose.
- `Open` opens an existing `guide.json`.
- `Save` writes the current guide back to the project folder.

A guide project stores:

- `guide.json` as the editable source of truth.
- `assets/` for original screenshots and imported images.
- `exports/` for generated Markdown, HTML, PDF, preview, and rendered export images.

## Edit Steps

Use the left `Steps` list to select a step.

- `Add` creates a new step.
- `Delete` removes the selected step.
- Arrow buttons move the selected step up or down.

The middle editor changes the guide title, selected step title, and selected step text.

## Add Images

Under `Step images`:

- `Import` copies image files into the project.
- `Paste` adds an image from the clipboard.
- `Capture` takes a screenshot.
- `Follow along` starts automatic screenshot capture while you perform a workflow.
- `Insert ref` inserts the selected step image into the step text.
- `Remove` detaches the selected image from the step.

Images live in the project pool. A pool image can be attached to a step more than once, with different crop and annotations.

## Step And Pool Tabs

- `Step` shows images attached to the selected step.
- `Pool` shows reusable images in the project.

Use `Attach to step` from the Pool tab to reuse an image in the selected step.

## Follow Along Capture

1. Select the step you want screenshots attached to.
2. Click `Follow along`.
3. GuideMaker minimizes.
4. Click through the workflow you are documenting.
5. Restore GuideMaker to stop capture.

Follow along settings are under `Settings`:

- Capture before or after click.
- After-click delay.
- Show or hide the cursor in screenshots.

## Crop Images

Open the `Crop` panel for a selected step image.

- Drag on the Image workspace to draw a crop.
- Use sliders for fine tuning.
- Crop is non-destructive: the original pool image stays unchanged.

## Annotate Images

Open the `Annotations` panel for a selected step image.

Available marks:

- Box/highlight
- Text label
- Arrow
- Redact

You can select annotations from the list or directly in the Image workspace. Drag selected annotations in the workspace to move them. Use the arrow buttons beside the list to change annotation order.

Redaction is burned into exported images.

## Preview

The Workspace pane has:

- `Guide` preview for the generated guide.
- `Image` preview for the selected image, crop, and annotations.

Use `Refresh` if preview auto-refresh is disabled or seems stale.

Preview settings are under `Settings`.

## Export

Click `Export` to generate:

- `exports/guide.md`
- `exports/guide.html`
- `exports/guide.pdf`

Review the export warning before confirming. Guide text and screenshots may contain sensitive data.

## Settings

Settings currently include:

- Dark mode
- Preview auto-refresh
- Preview refresh delay
- Follow along timing
- Follow along delay
- Show cursor in screenshots

Settings are beta-level local UI options and may reset when the app restarts.

## Known Limitations

- The beta package is a zip, not an installer.
- Settings are not persisted yet.
- Large images need better zoom/pan controls.
- Annotation resize/rotate controls are limited.
- Redaction is solid masking; true blur is planned later.
- No cloud, telemetry, AI services, or publishing integrations are included in MVP.
