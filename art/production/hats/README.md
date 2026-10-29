# Engineer helmet color variants v2

Built-in image generation, 2026-10-28. Five new transparent pair atlases complement the original orange pair to provide six selectable colors. Each atlas has the right-facing helmet in its left half and the left-facing helmet in its right half. The cream plaque and readable `IoTE` lettering are part of each authored sprite; Unity must render them with a white Image color.

Source edit targets supplied by the user:

- `C:/Users/Arc/Downloads/CPS/Unity_Project/art/production/engineer-hat-v1.png`
- `C:/Users/Arc/Downloads/CPS/Unity_Project/art/production/engineer-hat-left-v1.png`

The orange sprites are retained unchanged rather than regenerating identical art. Original generated files remain under `C:/Users/Arc/.codex/generated_images/01a17798-314f-7ff3-8733-25cce340040b/`.

| Variant | Palette key | Generated source |
| --- | --- | --- |
| Blue | #3C78D8 | exec-17b65b6d-ebb0-40ee-a6e0-b9861299dea1.png |
| Green | #68A832 | exec-fca48896-c70b-487e-be6e-b248b6f48a22.png |
| Purple | #9B59B6 | exec-18e4bd5a-871e-4fe9-8e0b-1a6a7295c6e5.png |
| Yellow | #E6B422 | exec-b4d941bd-c07e-415c-9eb0-e4ece2685927.png |
| Pink | #D94A64 | exec-5355164a-3d4d-4207-95c8-5aa28c5f3f50.png |

## Prompt set

Use case: precise-object-edit. Asset: Unity sprite atlas, TWO views of the SAME engineer helmet in a single horizontal row, two equal square cells, ideally 1536x768. Reference 1 is the right-facing helmet: put that view in the LEFT cell. Reference 2 is the left-facing helmet: put that view in the RIGHT cell. Repaint ONLY orange shell and brim to the selected color, using deliberate color-specific shadows and pale highlights; do not apply a color filter over the entire hat. Keep the original cream identification plaque and dark text exactly "IoTE" (capital I, lowercase o, capital T, capital E), correctly readable in both views, not mirrored. Preserve reference helmet silhouette, stepped pixel outline, three dome ribs, tilted brim, proportions and shading style. Both views equal visible size with the same baseline, centered in their equal cells with transparent padding and a clear gutter. Outside helmets is fully transparent alpha: NO black background, NO glow, NO drop shadow outside silhouette, NO labels, NO extra objects. Production pixel-cartoony game art matching the original.

Variant descriptions: bright clean cornflower BLUE (#3C78D8), fresh leaf green (#68A832), clear violet purple (#9B59B6), golden yellow (#E6B422), raspberry pink (#D94A64).

## Unity consumption

Keep the original transparent PNGs unmodified. Import as Multiple Sprite, Point filtering, no mipmaps or compression. Determine each sprite rectangle from the opaque alpha bounds within its own half, avoiding transparent padding affecting apparent size. Preserve aspect ratio when placing the sprite. Do not mirror the plaque or apply material/Image tint to select a helmet color.
