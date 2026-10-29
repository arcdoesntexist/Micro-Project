# Authored mascot footwear atlases v2

Created with the built-in imagegen tool on 2026-10-28. Edit target: `C:/Users/Arc/Downloads/CPS/Unity_Project/art/production/mascot-body-atlas-v1.png`. Five transparent 1448x1086 PNGs preserve the original four-column/three-row pose layout. Original orange is reused unchanged. Only footwear is repainted; cream bodies and separate engineer helmets remain distinct assets.

Palette: Orange #F57C32, Blue #3C78D8, Green #68A832, Purple #9B59B6, Yellow #E6B422, Pink #D94A64.

Exact tool prompts and original generated paths are recorded in `generation-prompts.json`. Selected final files are `mascot-body-blue-v2.png`, `mascot-body-green-v2.png`, `mascot-body-purple-v2.png`, `mascot-body-yellow-v2.png` and `mascot-body-pink-v2.png`. Keep generated alpha and pixel dimensions intact.

Unity imports reuse the original twelve sprite rectangles, names, pivots and point-filter settings. The Resources `MascotBodyPalette` maps a chosen helmet color to its matching body animation sprites. Mode Select, Versus Setup and Versus Gameplay use these authored sprites with white UI color/default UI material. Ordinary Single Player keeps its original orange atlas. Hat and shoes are now one linked character-color choice.
