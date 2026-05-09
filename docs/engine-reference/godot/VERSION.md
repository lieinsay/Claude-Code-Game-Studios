# Godot Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Godot 4.6.2 |
| **Release Date** | 2026-04-01 |
| **Project Pinned** | 2026-04-26 |
| **Last Docs Verified** | 2026-04-26 |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data likely covers Godot up to ~4.3. Versions 4.4, 4.5,
and 4.6 introduced significant changes that the model does NOT know about.
Always cross-reference this directory before suggesting Godot API calls.

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 4.4 | ~Mid 2025 | MEDIUM | Jolt physics option, FileAccess return types, shader texture type changes |
| 4.5 | ~Late 2025 | HIGH | Accessibility (AccessKit), variadic args, @abstract, shader baker, SMAA |
| 4.6 | Jan 2026 | HIGH | Jolt default, glow rework, D3D12 default on Windows, IK restored |
| 4.6.2 | Apr 2026 | HIGH | Maintenance release; compatible bug/stability fixes on the 4.6 branch |

## Project Configuration Notes

- Project language has pivoted to C# via Godot .NET. See `docs/architecture/adr-0019-desktop-csharp-platform-pivot.md`.
- Primary target is Windows desktop. Linux desktop is secondary after the first stable desktop build.
- Web is no longer an MVP target because Godot 4 C# projects cannot be exported to Web.
- Browser persistence, audio activation, fullscreen/mouse capture, IndexedDB, and background tab pause behavior are historical constraints from ADR-0006 and do not govern new implementation work.
- Desktop lifecycle, local filesystem persistence through `user://`, .NET build verification, and Godot .NET signal patterns must be validated before gameplay migration.

## Verified Sources

- Official docs: https://docs.godotengine.org/en/stable/
- Godot homepage/latest release: https://godotengine.org/
- 4.6.2 release: https://github.com/godotengine/godot/releases/tag/4.6.2-stable
- Release policy: https://docs.godotengine.org/en/stable/about/release_policy.html
- Web export documentation: https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html
- 4.5→4.6 migration: https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.6.html
- 4.4→4.5 migration: https://docs.godotengine.org/en/stable/tutorials/migrating/upgrading_to_godot_4.5.html
- Changelog: https://github.com/godotengine/godot/blob/master/CHANGELOG.md
- Release notes: https://godotengine.org/releases/4.6/
