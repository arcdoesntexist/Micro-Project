---
name: CPS Kitchen Catch
description: A warm, pixel-cartoony Unity kitchen game with a clear falling-object field.
colors:
  cocoa-ink: "#381A12"
  deep-brown: "#61331F"
  panel-cream: "#FCE8BA"
  light-cream: "#FFF7D9"
  action-orange: "#FA7D30"
  rank-letter-cream: "#FFF5D1"
  rank-accent-gold: "#FFE885"
  rank-d: "#735C4A"
  rank-c: "#F07324"
  rank-b: "#E03D24"
  rank-a: "#DE2E66"
  rank-s: "#F59C0D"
  rank-ss: "#B81A6E"
  rank-sss: "#05947A"
typography:
  display:
    fontSize: "78px"
    fontWeight: 400
  headline:
    fontSize: "53px"
    fontWeight: 400
  title:
    fontSize: "40px"
    fontWeight: 400
  body:
    fontSize: "27px"
    fontWeight: 400
  rank-mark:
    fontSize: "44px"
    fontWeight: 700
  rank-name:
    fontSize: "17px"
    fontWeight: 700
spacing:
  viewport-edge: "28px"
  viewport-top: "22px"
components:
  action-button:
    backgroundColor: "{colors.action-orange}"
    textColor: "{colors.cocoa-ink}"
  hud-panel:
    backgroundColor: "{colors.panel-cream}"
    textColor: "{colors.cocoa-ink}"
  hand-readout:
    backgroundColor: "{colors.deep-brown}"
    textColor: "{colors.light-cream}"
  rank-badge-d:
    backgroundColor: "{colors.rank-d}"
    textColor: "{colors.rank-letter-cream}"
  rank-badge-sss:
    backgroundColor: "{colors.rank-sss}"
    textColor: "{colors.rank-letter-cream}"
---

# Design System: CPS Kitchen Catch

## Overview

**Creative North Star: "The Warm Kitchen Stage"**

The shipped interface sits over an original, warm kitchen stage. The playfield is the main experience: a cream mascot runs along the front wooden counter while strongly outlined food and rubbish fall through a quiet central area. Menu, pause, and results temporarily take over that stage with large cream panels framed in wood. The persistent HUD is simpler and flatter so it stays readable during play.

The Unity implementation uses URP sprites for the scene and Unity uGUI for live labels, buttons, fields, rank text, and overlays. Concept art sets the visual direction; the six imported production PNGs and authored prefabs are the reusable materials. This document records the production UI visible in the final Task 7 captures, not the more ornate typography or central rank banner in the concept image.

**Key Characteristics:**

- A warm cream, honey wood, cocoa, and orange base with rank colors reserved for escalation.
- Heavy stepped outlines on the mascot and interactive items; softer, lower-contrast scenery behind them.
- A fixed 16:9 game and UI viewport with a clear central falling field.
- Live, compact rank lettering and short edge callouts, separate from the score and HP.

## Colors

Frontmatter colors are rounded sRGB equivalents of the serialized Unity `Color` values in `ProductionUISetup`, `ProductionArtSetup`, and `GameplayUI`. Raster artwork contains its own authored colors; do not recolor it by treating these UI tokens as an atlas palette.

### Primary

- **Engineer orange** (`action-orange`): the helmet cue and active action-button fill. The default gameplay callout begins here before the rank-specific plate color applies.
- **Cocoa ink** (`cocoa-ink`): readable text on pale UI surfaces and outlines on live rank lettering.

### Secondary

- **Rank escalation** (`rank-d` through `rank-sss`): seven distinct live plate colors, from muted D through orange, red, pink, gold, magenta, and teal. Color accompanies the rank mark, name, and stepped accent count; it never carries rank alone.
- **Rank cream and gold** (`rank-letter-cream`, `rank-accent-gold`): light letterforms and the small slanted accent bars on rank plates.

### Neutral

- **Panel cream** (`panel-cream`): the flat player and score HUD blocks and the initial base for major panels.
- **Light cream** (`light-cream`): text on dark brown HUD blocks and light menu fields.
- **Deep brown** (`deep-brown`): timer and hand-readout blocks.

**The Quiet Field Rule.** The background stays subordinate to the mascot and falling items; reserve stronger live color for actions, hazards, and rising ranks.

## Typography

The current Unity `Text` components use the built-in `LegacyRuntime.ttf` while a user-selected licensed production font is pending. That face is an implementation placeholder, not an approved brand font. The frontmatter records the observed 1920×1080 reference-canvas sizes without canonizing a replacement family or assuming Thai glyph coverage.

### Hierarchy

- **Display** (`display`): the menu title; the round countdown is a separate large centered status at 72 reference units.
- **Headline** (`headline`): the centered timer. Pause and result outcomes use nearby large sizes of 62 and 56 units.
- **Title** (`title`): score and hand readouts; profile and section headings use nearby 37–38 units.
- **Body** (`body`): instructions, source disclosure, buttons, and result statistics use 25–32 units according to fit.
- **Rank mark and name** (`rank-mark`, `rank-name`): bold live uGUI labels with a cocoa `Outline`. The mark shrinks from 44 to 40 for SS and 32 for SSS so the entire mark remains in its badge.

**The Live Label Rule.** Keep profile, score, difficulty, rank, instructions, and results as editable Unity text over artwork. Keep profile names plain text with rich-text interpretation disabled. Verify Thai combining marks only after the licensed font is supplied.

## Layout

The camera and screen-space-camera Canvas share a fixed 16:9 viewport. `GameplayViewport.Apply` letterboxes or pillarboxes other window shapes, uses a 1920×1080 Canvas Scaler reference with `Expand`, and enables `canvas.pixelPerfect` for legible serialized labels. The frontmatter spacing values are reference-canvas units, not browser CSS breakpoints. Final menu and gameplay captures were visually reviewed at 1920×1080 and 1280×720; the prior review records 1024×768, 1080×1920, and 2560×1080 framing.

Gameplay places profile/three HP upper left, timer top center, score/difficulty upper right, a 195×70 rank badge below the score, and Pause below that. Left and right hand readouts sit at the bottom corners with the input-source line centered beneath the counter. The rank callout is a separate 370×90 edge panel that appears for 1.1 seconds on promotion. The center stays open for falling objects and the mascot.

Menu uses two large nine-slice panels: player/profile/difficulty on the left and controls/disclosure on the right. Results dim the same scene, center a large outcome/score card, divide left and right statistics into two internal blocks, then place Force (N) disclosure above the separate Restart and Main Menu actions. Pause dims the scene and centers Resume.

**The Center-Clear Rule.** Keep persistent HUD and rank messages at the edges; the concept's large central rank banner is not a gameplay layout rule.

## Elevation & Depth

The kitchen image supplies pictorial depth. UI depth comes from tonal blocks, the illustrated wood rim of selected nine-slice panels, and dark translucent pause/result scrims. The authored uGUI panels and buttons do not use CSS-style drop shadows. The rank text uses a small Unity `Outline` for separation from its plate.

**The Selected-Panel Rule.** Use the wood-rim sprite for the two main menu cards, Pause card, and Results card. Keep inner statistics and compact gameplay HUD blocks simple; do not frame every field with wood.

## Shapes

Production sprites are pixel-cartoony with dark stepped outlines. The separate `ui-panel-v1.png` is cropped to its nontransparent bounds, imported with 62-pixel nine-slice borders, and shown on selected large uGUI `Image.Type.Sliced` panels with a 2× pixels-per-unit multiplier. Its transparent exterior and empty cream center leave all text live. Compact HUD blocks, input fields, readouts, and buttons are plain rectangular uGUI images. No numeric corner-radius scale is authored for them.

The mascot has a cream rounded body, orange feet, and a separate orange engineer hat. Body facing may flip; the hat and exact `IoTE` plate stay unmirrored. Item silhouettes must remain distinct without depending only on color. The Cake sprite, not the illustrative cheese in the gameplay concept, is the shipped third food item.

## Components

### Buttons and fields

Action buttons are orange rectangles with cocoa text. The authored uGUI `ColorBlock` uses lighter highlight, warmer pressed and selected tints, a muted disabled tint, and a 0.08-second fade. The menu provides nickname input, profile dropdown, difficulty choices, and Start; the selected difficulty has the stronger fill. Keyboard and mouse navigation are part of the authored interaction, with Resume focused on pause and Restart selected when Results opens. Fields use light cream fills with cocoa text.

### HUD and rank

Player and score blocks are flat cream rectangles. Timer and left/right input blocks are deep brown with light labels. The compact rank badge combines a large live D/C/B/A/S/SS/SSS mark, the exact rank name, a rank-specific plate, and zero to three slanted gold accents. The badge remains within 195×70 reference units. A promotion callout shares the current rank plate color and stays near the right edge for 1.1 seconds. The SSS final capture shows all five item silhouettes still unobscured.

### Stage and mascot motion

The production stage uses a background sprite, separate item sprites, an independently rendered hat, and separate ivy/whisk/ladle overlays. The mascot's authored pose atlas supports idle, run, eat, hurt, death, and triumph; sprite stepping is approximately 10–12 FPS while movement and visual reactions use elapsed time. Ambient props sway only a few degrees, with restrained steam at the edge. Death uses a brief red flash, fall, XX pose, four decorative food pieces, and square dissolve particles; Triumph lifts the mascot. Static captures establish appearance and placement, not complete browser animation timing.

## Do's and Don'ts

### Do:

- **Do** keep the warm kitchen raster and wooden running surface visible beneath the HUD.
- **Do** keep the rank mark, exact name, color, and stepped accents together inside the compact edge badge.
- **Do** keep `IoTE` exactly cased and readable when the mascot changes facing.
- **Do** label keyboard values as simulated input and unavailable force as `— / Sensor not connected`.

### Don't:

- **Don't** use a full concept mockup as a gameplay background or bake live labels into panel art.
- **Don't** cover falling food or rubbish with a central rank banner or oversized callout.
- **Don't** treat the temporary `LegacyRuntime.ttf`, its Thai shaping, or any unsupplied audio as production-approved.
- **Don't** claim WebGL interaction, browser frame rate, or full animation timing from static Unity captures.
