# Crop Cold Balance Update

Date: 2026-06-08

## Summary

Farm crops now have different cold-weather risk and reward profiles.

- Turnip: low yield, fastest growth, strongest cold survival, smallest cold yield penalty.
- Potato: default middle option, moderate cold survival and moderate yield penalty.
- Wheat: high yield, longest growth, weakest cold survival, largest cold yield penalty.

## Runtime Rules

- Each night, the active crop checks survival against the current farm temperature.
- If the crop fails the survival check, the crop cycle withers, growth progress resets, farm workers are released, and the farm must begin the crop cycle again.
- Surviving crops accumulate the worst cold yield multiplier seen during their growth cycle.
- Harvest and estimated harvest both apply that stored cold yield multiplier.
- Farm upgrades reduce cold pressure by treating the farm as 2C warmer per level above level 1.

## Balance Intent

The crop choice should become situational instead of strictly linear:

- Turnip is the safe winter crop when food stability matters.
- Potato is the reliable default.
- Wheat is the high-risk, high-output option for warmer nights or upgraded farms.
