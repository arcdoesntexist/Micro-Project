# Kitchen Catch cafe environment pack v1

Created 25 October 2026 with the built-in image_gen tool. User approved direct asset delivery without a Playground step. The original kitchen remains the Easy environment.

## Files

- `cafe-day-background-v1.png`: Normal / Day Cafe, opaque 1672 x 941 PNG.
- `cafe-night-background-v1.png`: Hard / Night Cafe, opaque 1672 x 941 PNG. Same room composition and counter geometry, relit with cool window light and warm off-frame practical lighting.
- `cafe-props-atlas-v1.png`: six separate prop silhouettes on one genuinely transparent 1536 x 1024 RGBA PNG. This is an atlas, not six individual files. Use the custom sprite rectangles below rather than equal-cell slicing.
- `sprite-rects.json`: padded sprite rectangles in top-left image and Unity bottom-left coordinates, plus pivot suggestions.
- `generation-prompts.json`: exact prompts and original generated source paths.

The backgrounds match the existing kitchen dimensions. The counter begins approximately 78% down the image; the central falling-object area stays open. The night scene keeps a visible counter and mid-value wall. No mascot, UI, collectible food or rubbish is baked into either background. Shelf plants and furniture are static scene art; supplied overlay props are optional additions.

## Sprite rectangles

Four pixels of padding surround the measured alpha > 8 bounds. Unity coordinates use bottom-left origin.

| Sprite | Image x | Image top y | Width | Height | Unity y | Pivot |
|---|---:|---:|---:|---:|---:|---|
| Curtain | 83 | 36 | 377 | 527 | 461 | top center |
| PendantLamp | 617 | 44 | 303 | 484 | 496 | top center |
| HangingIvy | 1104 | 37 | 369 | 586 | 401 | top center |
| CoffeeCup | 91 | 729 | 324 | 212 | 83 | bottom center |
| PottedFern | 563 | 604 | 400 | 362 | 58 | bottom center |
| Cookbooks | 1063 | 716 | 441 | 242 | 66 | bottom center |

Curtain, lamp and hanging ivy can sway gently from top pivots. Place cup, fern and books on rear shelves or side tables, not the running counter. Keep moving decorations near scene edges. The lamp has a painted warm bulb without an external light field; a soft light layer may be added during integration. Steam and dynamic rain are not supplied.

## Verification and scope

Both backgrounds were inspected for room continuity, clear counter and open center. The atlas has all six requested prop types; corner and gutter alpha samples are zero. Object alpha bounds were measured separately. PNGs were copied unchanged; hidden RGB in transparent pixels is preserved rather than chroma-keyed.

No Unity scene, prefab, switching code or animation was modified. Scene alignment, sprite scales, gameplay contrast and animation remain to be verified inside Unity at integration. Still images do not establish FPS.

## Subsequent Unity integration

The user subsequently requested Unity integration. The scene's `LevelEnvironment` inspector holds background/decoration bindings, and `CafeAmbient` holds the six positioned props. The authoring command reapplies default positions; normal play does not reauthor them. The source files in this folder are the Version 2.0.0 canonical copy; the duplicate asset-only ZIP is omitted from the repository.
