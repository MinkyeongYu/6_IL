# HUD Layout Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralize HUD layout and visual sizing while keeping the existing Unity IMGUI HUD and gameplay behavior intact.

**Architecture:** Add small `HudStyleConfig` and `HudLayout` helpers, then migrate `SimpleHud` to ask those helpers for dimensions instead of hard-coding screen positions. Keep `SimpleHud` as the single `MonoBehaviour` entry point for this pass.

**Tech Stack:** Unity 2022.3, C#, IMGUI, existing `UiTheme`, existing `Assets/Resources/UI` icon and portrait assets.

---

### Task 1: Add Central HUD Style Constants

**Files:**
- Create: `Assets/Scripts/UI/HudStyleConfig.cs`
- Modify: none
- Test: `git diff --check`

- [ ] **Step 1: Create `HudStyleConfig`**

Add a static class with dimensions used by the HUD. Keep colors in `UiTheme`; this class owns sizes, margins, row heights, and caps.

```csharp
using UnityEngine;

namespace IL6
{
    public static class HudStyleConfig
    {
        public const float Margin = 14f;
        public const float PanelPadding = 8f;
        public const float PanelGap = 8f;

        public const float TopStatusWidth = 248f;
        public const float TopStatusHeight = 98f;
        public const float ResourcePanelWidth = 176f;
        public const float ResourceCellHeight = 28f;

        public const float BottomVillageWidth = 300f;
        public const float BottomVillageHeight = 142f;

        public const float ContextPanelWidth = 390f;
        public const float ContextButtonHeight = 26f;
        public const int ContextActionLimit = 3;

        public const float BuildHotbarWidth = 520f;
        public const float BuildHotbarHeight = 70f;

        public const float RecruitDialogWidth = 380f;
        public const float RecruitDialogHeight = 110f;

        public const float ModalWidth = 720f;
        public const float ModalHeight = 420f;
        public const float SmallModalWidth = 520f;
        public const float SmallModalHeight = 320f;

        public const float IconSmall = 16f;
        public const float IconMedium = 20f;
        public const float IconLarge = 32f;

        public static float BottomSafeY(float height)
            => Mathf.Max(Margin, Screen.height - height - Margin);

        public static float ClampPanelX(float x, float width)
            => Mathf.Clamp(x, Margin, Mathf.Max(Margin, Screen.width - width - Margin));
    }
}
```

- [ ] **Step 2: Run whitespace check**

Run: `git diff --check`

Expected: exit code 0. CRLF warnings are acceptable; whitespace errors are not.

- [ ] **Step 3: Commit**

```powershell
git add Assets/Scripts/UI/HudStyleConfig.cs
git commit -m "feat: add HUD style config"
```

### Task 2: Add HUD Layout Helper

**Files:**
- Create: `Assets/Scripts/UI/HudLayout.cs`
- Test: `git diff --check`

- [ ] **Step 1: Create `HudLayout`**

Add a static helper that calculates stable rectangles for the HUD regions.

```csharp
using UnityEngine;

namespace IL6
{
    public static class HudLayout
    {
        public static Rect TopLeftStatus()
        {
            return new Rect(
                HudStyleConfig.Margin,
                HudStyleConfig.Margin,
                HudStyleConfig.TopStatusWidth,
                HudStyleConfig.TopStatusHeight);
        }

        public static Rect TopRightResources(int rows)
        {
            float h = HudStyleConfig.PanelPadding * 2f + rows * HudStyleConfig.ResourceCellHeight;
            return new Rect(
                Screen.width - HudStyleConfig.ResourcePanelWidth - HudStyleConfig.Margin,
                HudStyleConfig.Margin,
                HudStyleConfig.ResourcePanelWidth,
                h);
        }

        public static Rect BottomLeftVillage()
        {
            return new Rect(
                HudStyleConfig.Margin,
                HudStyleConfig.BottomSafeY(HudStyleConfig.BottomVillageHeight),
                HudStyleConfig.BottomVillageWidth,
                HudStyleConfig.BottomVillageHeight);
        }

        public static Rect BottomCenterContext(int visibleActions)
        {
            int count = Mathf.Clamp(visibleActions, 1, HudStyleConfig.ContextActionLimit);
            float h = HudStyleConfig.PanelPadding * 2f + count * HudStyleConfig.ContextButtonHeight + (count - 1) * HudStyleConfig.PanelGap;
            return new Rect(
                Screen.width / 2f - HudStyleConfig.ContextPanelWidth / 2f,
                HudStyleConfig.BottomSafeY(h),
                HudStyleConfig.ContextPanelWidth,
                h);
        }

        public static Rect BuildHotbar()
        {
            return new Rect(
                Screen.width / 2f - HudStyleConfig.BuildHotbarWidth / 2f,
                Screen.height - HudStyleConfig.BuildHotbarHeight - HudStyleConfig.Margin,
                HudStyleConfig.BuildHotbarWidth,
                HudStyleConfig.BuildHotbarHeight);
        }

        public static Rect RecruitDialog()
        {
            return new Rect(
                Screen.width / 2f - HudStyleConfig.RecruitDialogWidth / 2f,
                Screen.height - HudStyleConfig.RecruitDialogHeight - HudStyleConfig.Margin,
                HudStyleConfig.RecruitDialogWidth,
                HudStyleConfig.RecruitDialogHeight);
        }

        public static Rect CenterModal(float width = 0f, float height = 0f)
        {
            float w = width > 0f ? width : Mathf.Min(HudStyleConfig.ModalWidth, Screen.width - HudStyleConfig.Margin * 2f);
            float h = height > 0f ? height : Mathf.Min(HudStyleConfig.ModalHeight, Screen.height - HudStyleConfig.Margin * 2f);
            return new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
        }
    }
}
```

- [ ] **Step 2: Run whitespace check**

Run: `git diff --check`

Expected: exit code 0.

- [ ] **Step 3: Commit**

```powershell
git add Assets/Scripts/UI/HudLayout.cs
git commit -m "feat: add HUD layout helper"
```

### Task 3: Move Top HUD Regions To Layout Helper

**Files:**
- Modify: `Assets/Scripts/UI/SimpleHud.cs`
- Test: `git diff --check`

- [ ] **Step 1: Update `DrawStatCard`**

In `DrawStatCard`, replace hard-coded top-left panel rectangle with:

```csharp
var panel = HudLayout.TopLeftStatus();
```

Keep the internal row drawing logic unchanged except where values depend on the panel's `x`, `y`, `width`, and `height`.

- [ ] **Step 2: Update `DrawResourceBar`**

In `DrawResourceBar`, calculate resource rows first and replace the hard-coded panel rectangle with:

```csharp
ResourceKind[] kinds = { ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Meat, ResourceKind.Food, ResourceKind.Frostbloom };
var panel = HudLayout.TopRightResources(kinds.Length);
```

Draw each row with `HudStyleConfig.ResourceCellHeight` and `HudStyleConfig.IconMedium`.

- [ ] **Step 3: Run checks**

Run: `git diff --check`

Expected: exit code 0.

- [ ] **Step 4: Commit**

```powershell
git add Assets/Scripts/UI/SimpleHud.cs
git commit -m "refactor: anchor top HUD layout"
```

### Task 4: Consolidate Context Action Buttons

**Files:**
- Modify: `Assets/Scripts/UI/SimpleHud.cs`
- Test: `git diff --check`

- [ ] **Step 1: Add a small action model inside `SimpleHud`**

Add this private struct near the HUD mode fields:

```csharp
private readonly struct ContextAction
{
    public readonly int Priority;
    public readonly string Label;
    public readonly bool Enabled;
    public readonly System.Action Callback;

    public ContextAction(int priority, string label, bool enabled, System.Action callback)
    {
        Priority = priority;
        Label = label;
        Enabled = enabled;
        Callback = callback;
    }
}
```

- [ ] **Step 2: Add renderer method**

Add a renderer that receives an ordered list and draws at most three actions:

```csharp
private void DrawContextActions(System.Collections.Generic.List<ContextAction> actions)
{
    if (actions == null || actions.Count == 0) return;
    actions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    int count = Mathf.Min(actions.Count, HudStyleConfig.ContextActionLimit);
    var panel = HudLayout.BottomCenterContext(count);
    UiTheme.Panel(panel);

    float x = panel.x + HudStyleConfig.PanelPadding;
    float y = panel.y + HudStyleConfig.PanelPadding;
    float w = panel.width - HudStyleConfig.PanelPadding * 2f;
    for (int i = 0; i < count; i++)
    {
        var action = actions[i];
        var rect = new Rect(x, y, w, HudStyleConfig.ContextButtonHeight);
        if (UiTheme.Button(rect, action.Label, _smallBtn, action.Enabled))
        {
            action.Callback?.Invoke();
        }
        y += HudStyleConfig.ContextButtonHeight + HudStyleConfig.PanelGap;
    }
}
```

- [ ] **Step 3: Migrate one low-risk action first**

Start by replacing gather button placement with context action candidates for wood and stone. Preserve existing range and resource logic from `DrawGatherButton`.

- [ ] **Step 4: Migrate remaining world actions**

Move repair, upgrade, refuel, farm, and fence work into context action candidates. Keep recruit dialog separate for this pass because it has a portrait panel.

Priority order:

```csharp
// lower number draws first
recruit = 10;
repair/refuel = 20;
upgrade = 30;
fence = 40;
farm = 50;
gather = 60;
```

- [ ] **Step 5: Run checks**

Run: `git diff --check`

Expected: exit code 0.

- [ ] **Step 6: Commit**

```powershell
git add Assets/Scripts/UI/SimpleHud.cs
git commit -m "refactor: consolidate HUD context actions"
```

### Task 5: Move Modals And Recruit Dialog To Layout Helper

**Files:**
- Modify: `Assets/Scripts/UI/SimpleHud.cs`
- Test: `git diff --check`

- [ ] **Step 1: Update recruit dialog rectangle**

In `DrawRecruitDialog`, replace local `W`, `H`, and bottom-center rectangle calculation with:

```csharp
var panel = HudLayout.RecruitDialog();
float W = panel.width;
float H = panel.height;
```

Keep portrait and button internals unchanged.

- [ ] **Step 2: Update central modals**

For `DrawRecruitCutscene`, `DrawRuneModal`, `DrawPauseMenu`, and `DrawDeathOverlay`, replace center rectangle calculations with `HudLayout.CenterModal(...)` where practical.

- [ ] **Step 3: Run checks**

Run: `git diff --check`

Expected: exit code 0.

- [ ] **Step 4: Commit**

```powershell
git add Assets/Scripts/UI/SimpleHud.cs
git commit -m "refactor: route HUD modals through layout helper"
```

### Task 6: Verification And Documentation

**Files:**
- Modify: `docs/superpowers/specs/2026-06-05-hud-layout-design.md` if implementation meaningfully deviates from the design.
- Test: `npm test`, `npm run build`, Unity compile if available.

- [ ] **Step 1: Run whitespace check**

Run: `git diff --check`

Expected: exit code 0.

- [ ] **Step 2: Run reference tests**

Run: `npm test`

Expected: 12 test files and 60 tests pass.

- [ ] **Step 3: Run reference build**

Run: `npm run build`

Expected: Vite build exits 0. Chunk size warnings are acceptable.

- [ ] **Step 4: Try Unity C# project build**

Run: `dotnet build IL6.csproj`

Expected in the current local environment: this may fail before code compilation if .NET Framework 4.7.1 targeting pack is missing. If it fails with `MSB3644`, report it as an environment limitation, not a code verification success.

- [ ] **Step 5: Final status**

Run:

```powershell
git status --short
```

Expected: only intended code and documentation changes are present. Generated build output and executables are not staged.
