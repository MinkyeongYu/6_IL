# KPM-Style Character Sheets

이 폴더는 `C:\Development\0_KPM\src\sprites`의 기존 리소스를 6IL용 참고/후보 리소스로 모아둔 곳입니다.

## Animals

| File | Source |
|---|---|
| `animal_mammoth_anim.png` | copied from `0_KPM/src/sprites/32_animal_04_mammoth_anim.png` |
| `animal_deer_anim.png` | copied from `0_KPM/src/sprites/29_animal_01_deer_anim.png` |
| `animal_wolf_anim.png` | copied from `0_KPM/src/sprites/31_animal_03_wolf_anim.png` |
| `animal_rabbit_anim.png` | copied from `0_KPM/src/sprites/30_animal_02_rabbit_anim.png` |
| `animal_boar_anim.png` | copied from `0_KPM/src/sprites/33_animal_05_boar_anim.png` |
| `animal_bear_anim.png` | generated to match the KPM animal sheet style |

## Humanoids and Enemies

| File | Source |
|---|---|
| `player_warrior_side.png` | copied from `0_KPM/src/sprites/16_char_warrior_side.png` |
| `companion_uncle_anim.png` | copied from `0_KPM/src/sprites/36_companion_01_uncle_anim.png` |
| `companion_aunt_anim.png` | copied from `0_KPM/src/sprites/37_companion_02_aunt_anim.png` |
| `companion_child_anim.png` | copied from `0_KPM/src/sprites/38_companion_05_child_anim.png` |
| `enemy_zombie_basic.png` | copied from `0_KPM/src/sprites/27_enemy_01_mecha_zombie.png` |
| `boss_frost_zombie.png` | copied from `0_KPM/src/sprites/boss_01_mecha_zombie.png` |
| `boss_winter_knight.png` | copied from `0_KPM/src/sprites/boss_02_winter_knight.png` |
| `boss_iron_giant.png` | copied from `0_KPM/src/sprites/boss_03_iron_giant.png` |
| `boss_frost_lich.png` | copied from `0_KPM/src/sprites/boss_04_frost_lich.png` |

## Notes

- These are not yet wired into the game runtime.
- Final runtime assets should be copied or exported into `public/assets/characters/` when the loading pipeline is ready.
- Some copied sheets have white backgrounds and may need background removal or atlas slicing before direct Phaser use.
