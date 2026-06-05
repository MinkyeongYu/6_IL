# 6_IL Project Instructions

Develop this project as a Unity project only.

Authoritative project areas:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- Unity scenes, prefabs, C# scripts, editor scripts, assets, and build settings
- Unity build output from `IL6.EditorBuild.BuildScript.BuildWindows`

Do not implement gameplay, UI, VFX, build behavior, or runtime behavior in the Vite/npm web app path unless the user explicitly asks for a temporary prototype. Existing Vite/npm files are reference material or non-authoritative tooling only.

When applying Unity resources such as sprites, textures, audio, VFX, UI art, prefabs, materials, or animations, replace the currently referenced in-game resource instead of only adding a new asset beside it. Prefer preserving the existing authoritative path and `.meta` import settings when safe. If a new path is required, update all scene, prefab, C# `Resources.Load`, serialized field, atlas, addressable, material, animator, and planning-document references so the old resource is no longer used. Delete or archive unused legacy resources when safe, and verify the running/build output references the replacement.

For Windows executable refreshes:

1. Use Unity `2022.3.62f3` unless the project version changes.
2. Prefer `IL6.EditorBuild.BuildScript.BuildWindows`.
3. Verify `Build/Windows/IL6.exe` and the root `InisLand_v*_portable.exe`.
4. Rename or copy the final user-facing portable executable to a project-numbered filename such as `6IL_v*_portable.exe`.
5. Delete stale user-facing duplicate executables, but keep Unity/runtime helper executables required by the build.
6. If Unity is already open and blocks batch mode, report that and do not claim the executable is current.

After successful implementation and executable refresh:

1. Update the planning document when gameplay, UX, UI, VFX, balance, systems, or scope changed.
2. Copy the final root project-numbered portable executable, such as `6IL_v*_portable.exe`, to the configured Google Drive distribution folder when available.
3. Commit and push Unity source, project settings, scripts, assets, planning documents, and small required files to GitHub.
4. Do not commit generated Unity `Build/` output, portable `.exe`, crash handlers, or other generated executable artifacts to normal Git history unless the user explicitly asks for a release artifact workflow.
5. Verify there are no unpushed commits or unintended uncommitted changes before reporting completion.
