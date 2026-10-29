# Home reference fidelity

Status: approved by the user on 2026-10-24. Selected approach: create separate transparent assets matching the supplied UI reference.

## Intent and authority

Rebuild the Unity MainMenu presentation to match the previously approved Design Playground composition and the team's illustrated UI sheet. The sheet specifies materials and component shapes; it is not a screen layout to copy wholesale. Preserve the warm kitchen, existing mascot and readable IoTE helmet.

Visual reference: `C:/Users/Arc/.codex/codex-remote-attachments/01a111f5-90f5-7bb2-8a78-08affb17e859/0E44F9D2-B29F-41E1-9147-7BED3E6761A4/1-รูปภาพ-1.jpg`.
Composition reference: `playground/index.html`, including any recoverable saved browser layout. Inspect saved layout before falling back to source defaults; report which was used.

## Approaches

- Selected: produce separate transparent wooden plaques, cream panels, rounded raised buttons, fields, stars and icons based on the supplied sheet. Supports readable live text and scalable Unity UI.
- Rejected: cut production pieces directly from the small JPG. Its white background, baked text and limited resolution make clean scalable controls harder.

## Scope and layout

- Home remains a landscape 16:9 composition. Use the Playground's 1600 x 900 coordinates and convert consistently to the Unity reference canvas.
- Left: wooden KITCHEN CATCH plaque above a cream panel with wooden rim and restrained corner leaves. Inside: player selector and pencil, Create Profile, difficulty choices, then a prominent Start Cooking button.
- Use one, two and three stars plus Easy, Normal and Hard labels. Green, gold and coral star colors follow the Playground; selected and keyboard-focused states remain visibly distinct.
- Create Profile opens a small dialog, as in the Playground. Do not permanently fill Home with a second nickname form.
- Right: a short upper practice panel with A LITTLE PRACTICE?, Game Rules and movement hints. A separate cream-backed catch-food hint sits below. Leave the lower kitchen visible for the mascot to run across; do not enclose practice in a tall opaque card.
- Pencil opens the existing right-side profile manager. Preserve profile selection, rename, deletion confirmation and stable IDs. Practice pauses while editing or while a modal is open.
- One gear-shaped Settings control at the upper right opens existing global settings.
- Game Rules remains an overlay with food images and actual scores, rubbish images and penalties, and the D/C/B/A/S/SS/SSS strip with streak thresholds. Preserve established gameplay rules.

## Assets and constraints

Create only the assets used by these Home states now, not every HUD component in the reference sheet. Separate decorative foliage from stretchable panel centers where needed. Use nine-slice borders for scalable controls and native alpha for cutouts. Keep dynamic labels editable in Unity using the existing FF Spoken Trial font, sized for readability; do not bake player names, scores or button text into art. Keep source assets and provenance in the CPS documentation workspace and import runtime assets into Unity.

Reuse the existing kitchen and mascot artwork. No new gameplay rules, scoring changes, profile-storage redesign or gameplay HUD redesign. Fix duplicate Settings creation and the practice input incompatibility as part of making this Home function correctly.

## Verification and acceptance

Compare the approved browser composition and Unity Play Mode at the same 16:9 size. Check logo, controls, stars, pencil, gear, panel proportions, text scale and open practice area, rather than accepting approximate placement. Verify creation, selection, editing, cancellation, settings, rules and keyboard practice; practice must not generate scores. Check narrow and wide window letterboxing without clipping controls. Deliver actual Unity captures of Home and relevant overlays, and record remaining limitations honestly.

## Builder decisions

The builder selects texture resolution, sprite slicing, exact icon rendering and reusable UI helpers. Those choices must preserve the reference's silhouettes, material, layout and existing application behavior. Perform one batched visual inspection, fix its findings together, then one confirmation pass.
