# NuGet package icon — TODO

Replace this file with `icon.png` before the first NuGet release.

**Requirements (per nuget.org):**
- 128 × 128 pixels minimum, 256 × 256 recommended
- PNG with transparent background
- Under 1 MB
- File path must be `docs/nuget/icon.png` (referenced from `Directory.Build.props` as
  `<PackageIcon>icon.png</PackageIcon>`)

**Quick options:**

1. **Generate a text-based logo** — use a tool like <https://favicon.io/> or
   <https://www.canva.com> to render the text "RT" or "Rig.TUnit" with a colour scheme that
   matches the project (suggested: dark navy `#1A2332` background, lime accent `#A2EEEF`).
2. **Commission a designer** — typical OSS-icon brief: 256×256, two-letter monogram, single
   accent colour, no gradients.
3. **Use a placeholder for now** — any 256×256 PNG will satisfy `dotnet pack`. Drop it in as
   `icon.png` and replace later.

Once `icon.png` is in this folder, **delete this `icon.placeholder.md` file** and the next
`dotnet pack` run will embed it in every package.
