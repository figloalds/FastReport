# Headless extraction inventory and baseline

Recorded 2026-09-09 against 48f4a91 plus the existing layout migration working tree.
The working tree is the agreed baseline; the next commit records that foundation.

| Compiled source / surface | Classification | Final owner and decision |
| --- | --- | --- |
| Compat/shared/Drawing, TypeConverters, DotNetClasses/{IGraphics,GdiGraphics,Color.Full} | Portable Skia rendering, geometry, fonts, images, converters | FastReport.Drawing; preserve namespace, rendering and resources |
| Compat/shared/Compiler | Portable Roslyn compilation | Core; preserve restrictions, references and compiler behavior |
| Compat/shared/WindowsForms: Padding, AnchorStyles, DockStyle, PictureBoxSizeMode | Portable layout duplicated by shims | Core FastReport.Layout; remove duplicates after symbol migration |
| BindingSource, ListBindingHelper | Schema discovery wrapped in a UI API | Internal core schema discovery; preserve descriptors and rows |
| Remaining WindowsForms files: controls, collections, input/event args, timers, MessageBox, Application, SystemInformation, ControlPaint, locale/global settings and UI enums | Replacement desktop scaffolding; no native desktop implementation | Delete; reject interactive scripts; native counterparts available only in optional Windows host |
| ReportComponentBase.Cursor | Passive serialized cursor name exposed through a fake native object | Core string metadata; Windows converts explicitly |
| ReportComponentBase mouse event strings, hyperlink metadata | Passive interaction metadata | Core; retain serialization for hosts |
| DotNetClasses/UITypeEditor and Editor attributes naming FastReport.TypeEditors or System.Design | Placeholder class and unavailable editors | Remove placeholder/attributes; Windows hosts can register real metadata explicitly |
| Drawing/DesignAndPrinting.Duplex, ReportPage paper/printer properties | Passive output metadata | Preserve portable enum, defaults and serialized values; never enumerate printers |
| DialogPage.Core, ReportEngine.Dialogs.OpenSource | Skeletal page and success-returning no-op | Fail on required dialogs before reader discards children and before preparation |
| OpenSource designer/preview partial hooks | Unimplemented extension scaffolding | No new UI behavior promised; host composition only for real capabilities |
| Config locale callback, DrawUtils SWFGlobals writes | Shim initialization | Delete along with controls |
| FRReader object registration and missing-object path | FRX loading | Detect dialog requirements before unsupported children disappear |
| Cs/VbCodeHelper, LegacyScriptNamespaces, MsAssemblyDescriptor and compiler stubs | Imports, symbol migration, reference strings and compiler cache | Core; keep legacy layout/drawing scripts, reject actual desktop use with location |
| Web IntelliSenseHelper and project references | Assembly discovery | Replace Compat with explicit drawing/core owners |
| Embedded resources in OpenSource project | Engine localization and serialization; desktop cursors already excluded | Retain portable resources; remove obsolete shim callback |
| HTML/image exporters and non-FRX importers | Portable implementations | Stay portable; optional package extraction deferred |

All files under Compat/shared are accounted for above. No implemented native printer,
preview, dialog or designer service exists to transplant. The first Windows adapter will
provide explicit layout/cursor conversion and metadata registration; it must not claim
these missing capabilities or automatically initialize Forms.

## Characterization evidence

Windows .NET 10 baseline: 130 core, 11 portable PDF and 12 Windows PDF tests pass.
The prior foundation also recorded these suites on Ubuntu (see headless-runtime.md);
Linux must be rerun after extraction. macOS execution is unavailable in this workspace.

Existing fixtures assert prepared bound values Alice/Bob, C#/VB script-set padding,
sync/async preparation, one-page layout round trips, anchors/docking geometry and PDF
fonts. TestReport.fpx exports four pages; Watermark.fpx exports three. These structural
contracts, not PDF byte identity, are the preservation baseline. Fixture fonts live in
Tools/FastReport.Tests.OpenSource/Fixtures. Later release fixtures extend this coverage.

ScriptMigrationTests.RuntimeDoesNotDeclareOrReferenceDesktopFrameworkTypes is the
initial assembly gate. It will be strengthened to walk the portable assembly graph and
reject Compat after ownership changes. Native Forms coexistence runs only on the
Windows target. Demo dialog templates (Dialog Elements, Dialog Events, Filtering with
CheckedListBox) become negative headless fixtures; mere legacy imports remain positive.
