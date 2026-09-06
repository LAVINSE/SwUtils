# Changelog

[English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

All notable changes to this project will be documented in this file.

Release notes are grouped by version and date. The latest release is summarized first for package-manager checks and Git tag verification.

| Latest | Date | Focus |
| --- | --- | --- |
| `v1.2.1` | 2026-09-07 | Consistent code section names and clearer comments. |

## [Unreleased]

No changes are currently scheduled.

## [v1.2.1] - 2026-09-07

### Fixed

- Reviewed all scripts and aligned 15 common code section names, such as Fields and Properties, in SWTimer, SWCooldown, and SWSaveDataManager with the existing Korean naming convention.
- Updated matching end-region comments. Executable code behavior is unchanged.
- Replaced vague helper labels and coroutine descriptions with their actual behavior, and removed duplicate comments and historical migration notes.
- Clarified that SWTimer.Reset also clears the paused state.

## [v1.2.0] - 2026-09-07

### Added
- Added the `SW.Quest` quest and achievement module with task groups, conditions, rewards, and progress reports.
- Added sequential task groups, category and target based reports, replace, additive, positive, negative, and continuous progress strategies.
- Added extensible acceptance and cancellation conditions, rewards, automatic achievement registration, duplicate prevention, local events, and `SWEventBus` events.
- Added complete task-group persistence through `SWQuestSystemSaveData`, replaceable `ISWQuestSaveStore`, and an encrypted `SWPlayerPrefs` default store.
- Split quest and achievement definitions into dedicated databases with independent collection and validation.
- Added `SWQuestSystemWindow` for creating, duplicating, deleting, searching, editing, synchronizing, and validating quest assets.
- Added a project-defined score reward sample.

### Fixed
- Corrected negative progress handling so negative reports decrease progress instead of increasing it.
- Restored task groups and tasks by code name so reordered definitions cannot receive the wrong progress, while preventing duplicate completed rewards and inactive newly added tasks.

### Caution
- The quest and achievement system is experimental and has not completed its full design review or real-project validation.
- Its public API and save-data format may change following further review.

## [v1.1.1] - 2026-08-27

### Changed
- Reworked the English and Korean READMEs around feature guides, code examples, visual previews, and quick navigation links.
- Declared the Unity Physics and Physics 2D modules as package dependencies.
- Updated the package version, README badges, and installation URLs to `v1.1.1`.

### Fixed
- Fixed player builds failing because `SWIODatabase` referenced a Unity Editor API.
- Restricted `SWVibration` fallback calls to supported platforms so unsupported targets no longer call `Handheld.Vibrate`.

## [v1.1.0] - 2026-07-22

### Added
- Added `SWStateMachine<TContext>` with multiple layers, condition transitions, command transitions, any-state transitions, and state messages.
- Added `SWMonoStateMachine<TContext>` for Unity update integration and a multi-layer usage sample.
- Added `SWStackStateMachine<TContext>` with push, pop, replace, clear, pause, and resume lifecycles.
- Added a Unity stack state machine driver and a gameplay, pause, and settings flow sample.
- Added a Unity 6 `GraphView` state machine graph asset and node editor.
- Redesigned the state machine graph editor with a Shader Graph-inspired full canvas, floating information and inspector panels, searchable node creation, and transition summaries.
- Added an editor settings tab for panel, transition summary, shared sizing, and grid snapping preferences, and fixed node creation coordinates.
- Added Blackboard state and transition lists, independently resizable panels, and an expandable Graph Validation issue list.
- Added Alt-drag transition summary placement, In/Out port labels, and Layer terminology.
- Moved Graph Type into the Blackboard and added searchable, foldable state and transition data lists.
- Added StateMachine node descriptions, project asset switching, cross-asset copy, paste, duplication, and navigation shortcuts.
- Added a Play Mode Runtime Inspector for graph-factory instances with active state duration and recent transition history.
- Expanded validation for duplicate Any State and Return State nodes and unreachable states.
- Added a Behaviour Tree runtime with Action, Composite, Decorator nodes, a typed Blackboard, abort propagation, and runtime cloning.
- Added a Unity 6 GraphView Behaviour Tree editor with node search, Node Inspector, root selection, validation, and Play Mode status highlighting.
- Moved the Behaviour Tree runtime API to the `SW.BehaviourTree` namespace.
- Added SubTree, Timeout, Succeeder, Failure, Breakpoint, Random Failure, and Set Integer nodes plus the `Aborted` status.
- Added `SWBehaviourNodeProperty<T>`, custom Blackboard entries, and per-Runner Blackboard overrides.
- Added copy, paste, duplicate, SubTree selection, keyboard navigation, automatic layout, asset switching, and node script generation to the Behaviour Tree graph.
- Added Behaviour Tree project settings, panel and graph-view persistence, and runtime node and active-path highlighting.
- Added generic Set Property and Compare Property nodes, external Blackboard access, cached keys, and custom key overrides.
- Added editable script templates and first-open asset selection screens to the Behaviour Tree and State Machine editors.
- Added layer-based automatic layout, per-asset view restoration, and Project Settings integration to the State Machine graph.
- Fixed a Unity 6 conflict between empty-canvas double-click handling and the GraphView rectangle selector.
- Fixed Behaviour node selection and deletion exceptions caused by null Blackboard entries from earlier serialized data.
- Added a shared Graph List panel to both editors with search, creation, deletion, selection, and left-side collapsing.
- Unified toolbar sizing and styling and removed redundant graph selection, Open, and New Graph controls.
- Added a Behaviour Tree Runtime Debug tab for Runner selection, node statuses, and Blackboard values, plus a console-style validation panel.
- Added `SWStateMachineNodeCategory` and `SWBehaviourNodeCategory` for slash-delimited custom node categories.
- Added the consolidated `SWGraphAssetsExample` containing State Machine, Stack State Machine, and Behaviour Tree sample types.
- Added ready-to-run Layered, Stack, and Behaviour Tree sample graph assets.
- Added `SWStateMachineGraphTypeResolver` to recover graph types by full type name after an assembly name changes.
- Fixed transition summary placement while moving nodes and ambiguous duplicate-looking nested state names, and adopted the standard Any State and Return State labels.
- Added state node creation, removal, movement, port connections, layer and initial-state editing, commands, priorities, stack operations, and graph validation.
- Added a runtime factory and code-based graph conditions for creating layered state machines and stack state machine controllers from graph assets.

### Changed
- Changed the minimum supported Editor version to Unity 6.0.
- Unified the State Machine and Behaviour Tree editor structure, Graph List, Inspector, Runtime Debug, and toolbar design.
- Removed the unused minimap and the State Machine `Offline` indicator from the final graph editor design.
- Consolidated duplicate State Machine and Behaviour Tree scripts into `SWGraphAssetsExample`.
- Updated package metadata and README installation URLs to `v1.1.0`.

### Fixed
- Replaced the unsupported UI Toolkit `:first-child` selector with an explicit style class.
- Fixed `VisualElement is not my child` exceptions during graph rectangle-selection cleanup.
- Fixed a null reference while rebuilding the Behaviour Tree Blackboard after node deletion.
- Fixed State Machine sample types failing to resolve after moving into a different assembly.
- Fixed transition summaries failing to follow moved nodes or retain custom positions.

## [v1.0.16] - 2026-07-10

### Added
- Added an editor-only sprite icon field to `SWIdentifiedObject` and displayed it in inherited object lists such as the Stat System Editor.
- Added quick sort buttons and selected asset renaming to `SWTools/Utils/Data/Stat System Editor`.
- Added Stat System Editor display options and a preview for list row height, icon size, delete button size, and text size.

### Changed
- Split `SWTools/Debug` and `SWTools/Utils` editor windows into more specific submenu groups.
- Reworked the Stat System Editor list rows so the delete button keeps a fixed visible area.
- Updated package metadata and README installation URLs to `v1.0.16`.

## [v1.0.15] - 2026-07-08

### Added
- Added debug console settings for open-key modifiers, startup overlay display, overlay metrics, and optional Input System input.

### Changed
- Reworked the debug console settings window into focused tabs for status, input, overlay, and play-mode controls.
- Removed the mandatory Input System package dependency while keeping optional Input System console input through cached reflection.
- Updated package metadata and README installation URLs to `v1.0.15`.

## [v1.0.14] - 2026-07-07

### Added
- Added `SWCondition` handling to `SWSubClassSelector` fields so conditional disabled and hidden states work on `SerializeReference` fields and collections.

### Changed
- Grouped inherited `SWIdentifiedObject` definition fields under a `데이터 정의` foldout using the existing `SWGroup` inspector design.
- Updated package metadata and README installation URLs to `v1.0.14`.

## [v1.0.13] - 2026-07-07

### Changed
- Updated package metadata and README installation URLs to `v1.0.13`.

## [v1.0.12] - 2026-07-07

### Changed
- Renamed conflict-prone namespaces to clearer feature namespace names: `SW.Attributes`, `SW.Coroutines`, `SW.Debugging`, `SW.ScreenResolution`, and `SW.EditorTools.Attributes`.
- Updated package metadata and README installation URLs to `v1.0.12`.

## [v1.0.11] - 2026-07-06

### Changed
- Reorganized runtime namespaces into feature-oriented `SW.*` namespaces and aligned Runtime and Editor folders with those namespaces.
- Renamed remaining public `SWUtils...` types to the consistent `SW...` form while preserving saved-data keys.
- Standardized Korean XML documentation and corrected malformed attribute examples.
- Added English and Korean README and Changelog documents with language navigation links.
- Updated the package version and README installation URL to `v1.0.11`.

## [v1.0.10] - 2026-07-02

### Changed
- Updated the package version and README installation URL to `v1.0.10`.

### Fixed
- Corrected the folder `.meta` file names for `Runtime/SWMonoBehaviour`, `Runtime/SWScriptableObject`, `Editor/SWMonobehaviour`, and `Editor/SWScriptableObject` so immutable package installations can import them.

## [v1.0.9] - 2026-07-02

### Added
- Added `SWScriptableObject` with the same grouped fields, inspector buttons, and repaint attributes provided by `SWMonoBehaviour`.
- Added a `Unity PlayerPrefs` tab to `SWTools/Debug/PlayerPrefs Viewer` for viewing, editing, and deleting standard Unity PlayerPrefs.
- Added search-target filters and an option to delete all standard Unity PlayerPrefs from `SWTools/Debug/PlayerPrefs Viewer`.
- Added `long` and `double` support to `SWEncrypt<T>`.
- Added `SetLong`, `GetLong`, `SetDouble`, and `GetDouble` to `SWPlayerPrefs`.
- Added `SWTime.ToDateTime` for converting saved time strings to `DateTime` values.

### Changed
- Updated the Entries list in `SWTools/Debug/PlayerPrefs Viewer` to support inline value editing and saving.
- Added `IsLogOutputEnabled` to control log output from `SWEventBus`.
- Added the `shouldOutputLog` parameter to `SWEventBus.Publish` for per-publication log control.
- Renamed `SWUtilsRefillTimer` to `SWRefillTimer` while preserving its saved-data keys.
- Extended `SWTableSheet` importing to support ordinary class fields in addition to lists and arrays.
- Added a vertical field-and-value layout to `SWTools/Utils/Excel Table Importer`.
- Renamed the MonoBehaviour and ScriptableObject implementation folders to match their SWUtils type names.
- Updated the package version to `v1.0.9`.
- Expanded the README with setup steps and practical examples for the major runtime and editor workflows.

## [v1.0.8] - 2026-06-16

### Added
- Added `SWAmountFormat` for formatting large numbers with suffixes such as K, M, B, and T.
- Added `SWAmountFormatProfile` for managing number suffixes and decimal settings in a Resources preset asset.
- Added `SWTools/Utils/Amount Format Window` for automatically creating, editing, and previewing preset assets.
- Added `SWRectDummy`, a mesh-free UI Graphic that provides a raycast area without an Image.
- Added the `GameObject/UI/SW Rect Dummy` menu item and a dedicated Inspector.

### Changed
- Updated the package version to `v1.0.8`.
- Added number format preset and Rect Dummy usage instructions to the README.

## [v1.0.7] - 2026-06-08

### Added
- Added `SWTools/Debug/EventBus Debugger Window`.
- Added per-event listener counts, publication counts, latest publication times, and latest data snapshots to `SWEventBus`.
- Added `SWTools/Debug/Pool Monitor Window`.
- Added per-prefab snapshots for created, active, inactive, spawned, returned, destroyed, and delayed-return counts to `SWPool`.

### Changed
- Separated EditorWindow menu paths into `SWTools/Debug` and `SWTools/Utils`.
- Documented the actual EditorWindow menu paths in the README.
- Updated the package version to `v1.0.7`.

### Fixed
- Guarded the Steamworks.NET branch with `!UNITY_EDITOR` to prevent Unity build issues in runtime scripts.
- Removed an unused `UnityEditor` reference from `SWConditionAttribute`.

## [v1.0.6] - 2026-06-02

### Added
- Added `SWSubClassSelector`.
- Added a searchable dropdown for selecting implementations of abstract classes or interfaces on `SerializeReference` fields.
- Added `SWAddTypeMenu` for defining type selection menu paths.
- Added `SWHideInTypeMenu` for hiding specific types from the type selection menu.
- Added the `SWSubClassSelectorExample` sample with single-field, array, and `List<T>` examples.

### Changed
- Updated the package version to `v1.0.6`.
- Applied the `SWExample` namespace to sample scripts.

## [v1.0.5] - 2026-05-29

### Added
- Added a TextMeshPro font asset performance tab to `SWTools/TMP Font Asset Manager`.
- Added inspection of atlas memory, glyphs, characters, fallback chains, dynamic atlases, and material presets.
- Added a TextMeshPro font asset performance guide to the README.

### Changed
- Updated README and Changelog version references to v1.0.5.

## [1.0.0] - 2026-03-19

### Added
- Initial SWUtils release.
