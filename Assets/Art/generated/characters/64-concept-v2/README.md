# 64x64 Character Concept Sheets v2

This version adjusts the character proportions for the 6IL runtime target:

- Smaller heads than the first 64-concept pass.
- More grounded body proportions.
- Each character is still designed around a single `64x64` sprite cell.
- These are concept sheets, not final sliced transparent runtime atlases.

## Files

| File | Contents |
|---|---|
| `animals_64_concept_v2_smaller_heads.png` | mammoth, deer, wolf, bear, rabbit, boar: idle/action 64x64 concepts |
| `humanoids_enemies_64_concept_v2_smaller_heads.png` | player, uncle, aunt, child, zombie: idle/action 64x64 concepts |

## Next Step

For runtime use, redraw or slice final frames as fixed-size transparent PNG atlas frames:

- Frame size: `64x64`
- Recommended layout: one atlas per character
- Background: transparent
- Pivot baseline: feet aligned near the lower edge
