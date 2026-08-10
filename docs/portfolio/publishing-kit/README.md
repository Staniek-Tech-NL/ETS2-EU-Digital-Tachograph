# Platform-neutral portfolio publishing kit

This directory is the source for a portable portfolio package. It is not tied
to a specific marketplace, website builder, or professional network.

## Included source files

- [English descriptions](descriptions-en.md) — short, medium, and extended copy
  for all four portfolio entries.
- [Polish descriptions](descriptions-pl.md) — matching Polish variants.
- [Public links](links.md) — stable repository and case-study URLs.
- [Asset manifest](asset-manifest.md) — cover and screenshot selection with
  recommended gallery order.
- `build-package.ps1` — creates a complete local folder and ZIP archive.

## Build the portable package

From the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File docs/portfolio/publishing-kit/build-package.ps1
```

The command creates:

```text
output/portfolio-publishing-kit/
output/portfolio-publishing-kit.zip
```

The output folder contains copied text files and images arranged into `covers`
and `screenshots` directories. `output/` is intentionally ignored by Git, so the
source images remain stored only once in the repository.

## Recommended use

1. Start with the full-application entry.
2. Add the three module entries only where multiple projects are useful.
3. Pick the description length required by the destination.
4. Upload the cover first, followed by screenshots in manifest order.
5. Link each module directly to its case-study page, not only to the repository
   root.
