# GuideMaker Beta Testing

## Package

Unzip the beta package to a local folder and run:

```text
GuideMaker.App.exe
```

GuideMaker is local-only. Test projects, screenshots, preview files, and exports stay on the machine.

## Smoke Test Flow

1. Create a new guide project in an empty folder.
2. Add at least two steps.
3. Use Follow along to capture screenshots.
4. Add one image from the Pool to a step more than once.
5. Insert image references into step text.
6. Crop one step image.
7. Add at least one label, arrow, highlight, and redact annotation.
8. Save, close, reopen, and confirm content is still there.
9. Export Markdown, HTML, and PDF.
10. Open the exported files and confirm images, crop, and annotations match the guide.

## Things To Watch

- Export should create `guide.md`, `guide.html`, and `guide.pdf`.
- Redaction should be burned into exported images.
- Follow along should capture the screen under the mouse.
- Settings should not cover or break the workspace preview.
- No guide content should be sent to cloud services.

## Known Limitations

- Settings are local in-memory options for now and may reset when the app restarts.
- Annotation resize and rotate controls are still limited.
- Very large images may still need better zoom/pan controls.
- Packaging is a zip-based beta package, not an installer.
