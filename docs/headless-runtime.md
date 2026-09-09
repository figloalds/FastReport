# Headless report runtime

The supported direction for this fork is:

```text
FRX template + application data -> report layout and pagination -> prepared pages
prepared pages -> PDF exporter / other exporters / future preview hosts
```

The runtime should target `net10.0` on every OS, use Skia for drawing, and require no Windows
Forms, WPF, GDI+, designer, preview window, or printer installation. Desktop applications may
host it, but should not determine which types the engine uses. Windows-specific features belong
in an optional library targeting `net10.0-windows`, using native Windows APIs directly.

This document records the architecture and execution plan. The layout extraction is implemented;
the project split and removal of the remaining compatibility dependencies are still pending.
The first milestone is a clean separation between the portable FRX engine
and capabilities composed on top of it, proven by running FRX-to-PDF jobs on Linux. Preserve
implemented behavior as it moves, but rebuilding missing preview, printing, dialog, or designer
features is not a prerequisite for delivering the Linux engine.

## Completed foundation: isolate legacy namespaces and own report layout

The former `System.Windows.Forms` replacements are now in `FastReport.Compatibility.Forms`.
No replacement types are exported in Microsoft's Forms namespace. The obsolete
`WindowsFormsReplacement` build switch was removed: replacing these report dependencies with
native WinForms would reintroduce platform coupling and incompatible drawing types.

New report scripts use FastReport namespaces. When an old FRX script is compiled, syntax-aware
C# and VB migration translates desktop namespace references to their FastReport equivalents.
Migration preserves comments and literal text, including literal portions of interpolated
strings. Legacy `System.Windows.Forms` assembly references resolve to the compatibility assembly;
new reports no longer list the desktop assembly by default. The report's stored script is unchanged.

This is an API and binary compatibility change. Applications and extensions using these types
must recompile against the new namespace. For example, `TextObject.Padding` takes
`FastReport.Layout.Padding`, owned by the engine, instead of a compatibility or native Forms type. Ordinary FRX layout
values such as `Padding="2, 1, 2, 1"` and `Anchor="Top, Left"` keep their existing format.
Arbitrary assembly-qualified type names and strings containing desktop names are not rewritten.

The compatibility assembly remains required because it also owns `FastReport.Drawing`, graphics
abstractions, type converters, and the Roslyn report compiler. Renaming its Forms types does not
make their implementations the long-term runtime API.

The first implementation slice gives the reporting engine its own layout vocabulary under
`FastReport.Base/Layout`, compiled into the `FastReport` assembly:

| Type | Reporting responsibility |
| --- | --- |
| `Padding` | Insets between a report object's bounds and its content; invariant FRX conversion. |
| `AnchorStyles` | Preserve distances to parent edges as report geometry changes. |
| `DockStyle` | Allocate edges or the remaining area of a report container. |
| `ImageSizeMode` | Fit images inside report content bounds, without a `PictureBox` API identity. |

Text, pictures, barcodes, tables, bands, containers, watermarks, importers and HTML export now
consume these engine-owned values. New C#/VB scripts import `FastReport.Layout`. Legacy layout
references from `System.Windows.Forms` and `FastReport.Compatibility.Forms` are migrated by
symbol, including namespace/type aliases and unqualified names; unrelated user-defined types
are preserved. Desktop types are not converted to layout types. Existing compatibility control
types remain transitional; their removal is not claimed by this slice.

Validation on 2026-09-09: 130 core tests and 11 portable PDF tests passed in a clean Ubuntu WSL
source copy with .NET SDK 10.0.100 and `DISPLAY`/`WAYLAND_DISPLAY` unset. Windows passed 130 core
tests, 11 portable PDF tests and 12 Windows-targeted PDF tests. Added coverage exercises FRX
round-tripping, report geometry, layout API ownership, invariant conversion and real C#/VB
script preparation. This verifies the current Linux pipeline, not completion of the dependency split.

## Proposed project boundary

Arrows mean project references. Names for new projects are provisional. The first slice retains
the existing package and `FastReport` assembly name; further source/project reorganization and
breaking API changes are acceptable where they establish clear reporting-engine ownership.

```text
FastReport.OpenSource.Windows (net10.0-windows)
    -> FastReport.OpenSource (net10.0, assembly FastReport)
        -> FastReport.Drawing (net10.0, Skia/HarfBuzz)

FastReport.OpenSource.Export.PdfSimple (net10.0)
    -> FastReport.OpenSource

Web hosts and portable extensions -> FastReport.OpenSource
Windows hosts using desktop features -> FastReport.OpenSource.Windows
Windows hosts running headless jobs -> FastReport.OpenSource
```

The core never references the Windows library, directly or transitively. It has one `net10.0`
implementation on every OS. The Windows library references core and uses native Forms through
the desktop framework; it does not compile another copy of the engine or the Forms shims.
The proposed minimal Windows project configuration is:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../FastReport.OpenSource/FastReport.OpenSource.csproj" />
  </ItemGroup>
</Project>
```

This follows the [.NET Desktop SDK configuration](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props-desktop).
An OS-specific target can consume the corresponding portable target; see
[target framework compatibility](https://learn.microsoft.com/en-us/dotnet/standard/frameworks).
WPF is unnecessary for the proposed Forms adapter. Set `EnableWindowsTargeting` only where
cross-compiling the Windows project is required; that enables a build, not execution on Linux.
See [NETSDK1100](https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1100).

Start with one Windows adapter library. Keep Roslyn compilation with the engine initially;
create a separate compilation assembly only if an actual consumer requires that separation.
Drawing is a useful independent portable owner because both the engine and exporters consume it.

## What the repository currently contains

| Area | Evidence and consequence |
| --- | --- |
| Core compilation | `FastReport.Base/FastReport.Base.csproj` imports all base C# files into `FastReport.OpenSource`; it is not an independently built core assembly. Moving files requires updating the source inclusion boundary. |
| Mixed compatibility assembly | `FastReport.Compat/shared` owns drawing, Roslyn wrappers, converters, Forms-shaped layout values, binding, and control shims. Moving this entire project to Windows would also move essential engine dependencies. |
| Layout | `ComponentBase`, `ContainerObject`, text, pictures, barcodes and table objects now consume `FastReport.Layout` values from core. Their semantics are needed for headless pagination. |
| Data discovery | `Data/BusinessObjectConverter.cs` constructs `BindingSource` and calls `ListBindingHelper`. This is schema discovery, not a need for a UI control. |
| Desktop surface | `ReportComponentBase.cs` exposes a shim `Cursor` and mouse event metadata; many objects carry editor attributes referencing a placeholder `UITypeEditor`. |
| Dialogs | `FastReport.OpenSource/Dialog/DialogPage.Core.cs` is a skeletal page; `RunDialogs` and `RunDialogsAsync` return success without interaction. |
| Printing | `Drawing/DesignAndPrinting.cs` contains only a `Duplex` enum. `ReportPage` serializes paper and printer-related values, but this is not a native print implementation. |
| Designer | OpenSource partial hooks are often unimplemented. The README states that the Community Edition designer source is not supplied in this repository. |
| Script migration | `Code/LegacyScriptNamespaces.cs` resolves legacy namespaces, then maps layout type symbols to core. Remaining desktop references still require the compatibility layer until that feature boundary is extracted. |
| FRX loading | `Utils/FRReader.cs` can record an unsupported-object validation error and return no object. Feature detection must run before required dialog nodes can be lost. |

These findings are an initial inventory, not a claim that every desktop feature exists here.
In particular, a new Windows project alone cannot deliver a designer, preview, or printing.

## Ownership decisions

| Keep portable | Move to the Windows adapter | Remove when unused |
| --- | --- | --- |
| Report model, FRX I/O, data discovery, expressions and script compilation | Native Forms/control creation and lifetime | Replacement `Form`, `Control`, `MessageBox`, `Application`, timers and collection shims |
| Padding, anchoring, docking, alignment and image sizing semantics | Native cursor and keyboard/mouse translation | Forms-style binding after schema discovery replaces its callers |
| Layout, pagination, prepared pages and page cache | Preview controls, print dialogs, printer enumeration and print execution where implemented | Placeholder editor types after external metadata registration exists |
| Skia drawing, font shaping, image decoding and PDF support | Designer adapters and property editors where source is available | No-op desktop hooks with no supported consumer |
| Paper dimensions, margins, orientation, page selection and output-affecting values | Native printer settings and conversions to/from portable values | Unused compatibility enums after checking scripts and serialized values |

Use the report-owned namespace `FastReport.Layout` for portable layout types.
Preserve existing FRX names, enum numeric values, flag combinations, defaults, converters, and
serialization. Report properties use those types on Windows too; adapters explicitly convert
to native `System.Windows.Forms.Padding`, `AnchorStyles`, etc. A native `Graphics`, `Font`, or
`Image` must not replace the engine's Skia drawing types. Define conversion and disposal at the
adapter boundary and keep text measurement and pagination in the same portable renderer.

Retain `BeforePrint`, `AfterPrint`, `PrintOn`, and other preparation/output semantics even though
their names mention printing. Preserve serialized printer hints such as duplex and raw paper size
as portable values or metadata, without querying an installed printer during preparation.
Cursor names and interaction metadata can remain portable serialized data when needed for FRX
round-tripping or web consumers; native cursor objects and desktop event arguments stay outside core.

Non-FRX importers and HTML/image exporters are a separate modularity decision. Keep portable
implementations portable; do not put them in the Windows library simply because they are optional.
Their package extraction does not need to block removal of Forms dependencies.

## Extension and compatibility strategy

Partial classes cannot span assemblies. Replace desktop partial hooks with composition or narrow
portable service interfaces only where core must invoke a host capability. Prefer Windows-side
services accepting a `Report` or prepared pages for preview, printing, and designer integration.
Do not introduce a universal platform service containing unrelated desktop methods.

Register optional services explicitly from the host, with per-report scope where practical.
Any core-facing contract uses portable types and defines cancellation, ownership, and disposal.
The Windows implementation owns UI thread dispatch, STA requirements, and the message loop
integration. Referencing the package must not silently initialize a UI or change headless behavior.
Attach desktop property-editor metadata from the adapter using descriptor providers or equivalent
registration; keep serialization converters required for FRX inside the portable graph.

Separate legacy script references by meaning:

- Map layout and supported drawing references to their final portable owners for C# and VB.
  Recognize both original desktop namespaces and the interim compatibility namespace.
- Resolve imports, aliases, and unqualified names deliberately. A script can use both `Padding`
  and `MessageBox` from the same old Forms import; a blanket namespace replacement cannot handle
  that split. Preserve trivia, literals, interpolation text, and the stored script.
- Resolve actual desktop APIs only through an explicitly configured Windows execution path.
  Headless jobs receive a diagnostic identifying the unsupported feature and script location.
  Do not silently recreate no-op controls to make those scripts compile.
- Replace the blanket legacy Forms assembly redirect with the references required by the selected
  compilation path. Update generated imports, assembly discovery and compiler caches together;
  keep existing script restrictions when changing reference resolution.

Detect required interaction while reading FRX, before unsupported nodes can be discarded, and
validate capability requirements before preparation. A parameterized report without dialogs
continues normally. A report requiring a dialog fails clearly in the headless path; a Windows
host can execute it only when a real implementation is registered. Passive metadata such as
printer hints does not require a desktop capability. Merely importing a legacy namespace must
not make an otherwise portable script require Windows.

Treat the public type changes as a breaking release requiring recompilation. Do not promise
binary compatibility with native Forms types. Preserve ordinary FRX layout values and supported
legacy scripts; document unsupported interactive templates. Type forwarding is useful only for
types moved without changing their namespace/name and with an acyclic assembly graph. It cannot
solve the layout namespace change or preserve fake Forms behavior.

## Execution plan, in dependency order

Each step should leave the core and PDF projects buildable and have its own reviewable change.
The baseline is the current working tree, which already contains compatibility migration work.

1. **Inventory and capture behavior.** Enumerate compiled desktop-related types, public signatures,
   reflection strings, resources, registrations, and template/script usage. Classify each as
   portable semantics, implemented desktop behavior, or unused/no-op scaffolding. Run existing
   core/PDF tests and record representative FRX output before changes. Add dependency checks for
   native desktop references and select Windows/Linux fixtures. Exit: every extraction candidate
   has an owner and a preservation/removal decision, with the existing baseline recorded.
2. **Extract portable layout values (implemented).** Introduce the final layout API and its converters in core;
   migrate component, text, image, table, importer, and exporter consumers. Update C#/VB migration,
   default script imports, and assembly resolution in the same change. Test FRX load/save/reload,
   defaults and flags, mixed legacy imports, aliases, and script-set padding. Exit: layout has no
   dependency on Forms compatibility types and page geometry matches the baseline.
3. **Replace binding with schema discovery (implemented).** Add an internal data service using `TypeDescriptor`,
   `ITypedList`, `IListSource`, declared enumerable item types, and bounded item inspection as
   appropriate. Cover empty typed lists, arrays, non-generic/nested collections, custom descriptors,
   and existing instance/property-filter hooks. Define precedence and disposal; do not consume a
   one-shot sequence and lose its first row. Migrate all binding-helper callers. Exit: business
   object registration and prepared row values pass without `BindingSource` or `ListBindingHelper`.

   Implemented precedence: unwrap `IListSource`, prefer `ITypedList`, then custom instance
   descriptors, declared array/`IEnumerable<T>` item types, and indexed non-generic list
   samples (at most 32 slots). Arbitrary enumerables are never advanced during discovery;
   untyped streaming sources need an explicit schema or a typed enumerable. Factory-hook
   instances are disposed by discovery; caller-owned instances/lists are not. Row loading
   disposes its enumerator. Windows validation: 135 core tests, 11 portable and 12 Windows PDF tests.
4. **Create the Windows boundary and remove desktop coupling (implemented).** Add the Windows project without
   importing base/shared engine sources. Replace required partial hooks, native cursor exposure,
   and editor metadata dependencies; add early unsupported-feature diagnostics. Move implemented
   desktop behavior to native APIs and delete unused shims. Audit `Config`, `FRReader`, registrations,
   script generation, and web consumers along with obvious UI files. Add a Windows host integration
   fixture for each retained real capability. Missing desktop implementations remain documented
   follow-up work rather than blocking Linux support. Exit: core has no Forms-shaped control/runtime API,
   the adapter uses native Forms, and core/PDF load without the adapter installed.

   The adapter provides native layout/cursor conversion and scoped editor registration,
   verified on an STA thread including provider removal. No native dialog, preview,
   designer or print implementation exists in this source distribution. Dialog FRX nodes
   fail during reading; programmatic dialogs fail before preparation. C#/VB desktop API
   references fail with script line/column diagnostics. Legacy imports alone remain valid.
   Cursor is now a string (default `Default`); mouse-event strings and duplex metadata
   round-trip. Removed all Forms replacements and placeholder editor attributes.
   Windows validation: 143 core, 11 portable PDF and 13 Windows PDF tests.
5. **Retire Compat with explicit owners.** Move Skia drawing, its renderer abstractions and drawing
   converters into `FastReport.Drawing`; place Roslyn wrappers and required references with core.
   Keep drawing namespaces stable where possible. Update project/package references, signing,
   resources, solution/build scripts, compiler assembly resolution, tests and dependent extensions.
   Delete Compat only after all consumers have migrated. Exit: a clean restore/build/package has
   no Compat dependency and scripts compile after assembly ownership changes.
6. **Verify and document the release.** Run packaged consumer tests and the OS matrix below, check
   Web/PDF/extensions compile, and publish the breaking API/FRX migration notes. Verify the portable
   build from a clean environment with no Windows targeting/runtime packs. Exit: dependency and
   runtime gates pass and any unsupported desktop capability is documented explicitly.

The first implementation slice establishes the baseline and layout values from steps 1 and 2.
The next slice is data discovery, followed by the desktop service boundary and physical project
ownership changes. The engine must own prepared reporting output; exporters and future preview
hosts compose on top of it. Do not
begin by switching Compat to `net10.0-windows`: core still needs its drawing and compiler code.

Working proposals still open for discussion: the new package/type names and whether full
round-tripping of unsupported desktop template extensions is needed beyond preserving known
passive metadata. The implementation inventory determines what can actually move into the first
Windows library. These do not prevent the dependency inventory and characterization work.

## Verification

```sh
dotnet test Tools/FastReport.Tests.OpenSource/FastReport.Tests.OpenSource.csproj
dotnet test Extras/OpenSource/FastReport.OpenSource.Export.PdfSimple/FastReport.OpenSource.Export.PdfSimple.Tests/FastReport.OpenSource.Export.PdfSimple.Tests.csproj
```

On Windows, the PDF suite runs against both `net10.0` and `net10.0-windows`, with native Forms
enabled for the latter. On other operating systems it runs against `net10.0` only. The tests
cover legacy C#/VB scripts, synchronous/asynchronous preparation, bound row values, script-set
padding, and PDF page/font output. A Windows-only compile check uses native `Form`, `BindingSource`,
`Padding`, and `AnchorStyles` alongside FastReport without aliases. Core tests reject exported
desktop replacement types and references to native Forms or `System.Drawing.Common`.

The later removals should be gated by representative production FRX fixtures on both Windows
and Linux, including text and font layout, images, tables/matrices, barcodes, page breaks, and
script expressions. A Windows run of a `net10.0` test is not a substitute for a Linux runtime test.

For the extraction, extend that baseline with these release gates. Linux execution is the primary
acceptance gate; the macOS check below is follow-up coverage for the broader multiplatform claim.

| Environment | Required evidence |
| --- | --- |
| Windows, `net10.0` | Core and PDF suites pass without loading the Windows adapter; synchronous/asynchronous jobs retain pagination and output. |
| Linux, `net10.0`, no display server | Clean restore/build and FRX-to-PDF execution using packaged native Skia/HarfBuzz assets and controlled fonts; no desktop runtime or printer dependency. |
| macOS, `net10.0` | FRX-to-PDF smoke fixtures and native asset/font-loading checks before declaring macOS release support verified. |
| Windows, `net10.0-windows` | Native Forms types coexist with report layout types; retained desktop services work with correct UI threading and disposal. A type-resolution test alone does not prove printing or preview works. |
| Packaged consumers | A portable application restores only the portable dependency graph; a Windows application can opt into the adapter. Inspect transitive packages, framework references, assembly references and public signatures. |

Reject native Forms, WPF, `System.Drawing.Common`, WindowsDesktop framework references, and the
Windows adapter anywhere in the portable dependency graph. After step 5 also reject Compat and
exported Forms replacements. Source scans should distinguish executable dependencies from legacy
namespace strings used by migration tests and script restrictions.

Use fixed fonts and compare page counts, prepared geometry, bound values, extracted PDF text,
fonts and images. Use tolerances for rendered comparisons; PDF byte equality is not a useful
cross-platform layout contract. Add negative cases for required dialogs and desktop-only scripts,
and positive cases showing that passive desktop metadata and legacy layout imports remain usable.
