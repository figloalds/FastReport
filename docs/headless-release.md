# Headless runtime migration notes

This is a breaking source and binary change. Rebuild applications and extensions against
the new packages together; do not mix these assemblies with earlier Compat-based binaries.
These notes describe the source release, not a publication to a public package registry.

| Package | Target | Responsibility |
| --- | --- | --- |
| FastReport.OpenSource | net10.0 | FRX I/O, layout, data discovery, pagination, prepared pages, C#/VB compilation; assembly name remains FastReport |
| FastReport.Drawing | net10.0 | Skia drawing, HarfBuzz shaping, images, graphics abstractions and drawing converters |
| FastReport.OpenSource.Export.PdfSimple | net10.0 | PDF export from prepared pages |
| FastReport.OpenSource.Windows | net10.0-windows | Explicit native Forms layout/cursor conversions and scoped editor registration |

FastReport.Compat is retired. The portable package graph contains no native Forms, WPF,
System.Drawing.Common, WindowsDesktop framework, or Windows adapter reference. Drawing
namespaces remain `FastReport.Drawing`; compiler wrappers retain their namespaces but now
belong to the core assembly. Assembly-qualified type strings outside supported script
references need application migration; no binary type-forwarding promise is made.

Use `FastReport.Layout.Padding`, `AnchorStyles`, `DockStyle` and `ImageSizeMode` for report
properties on every OS. `ImageSizeMode` replaces the PictureBoxSizeMode API name. Layout
enum values, flags, defaults and invariant FRX strings are preserved. The optional adapter's
`ReportHostConversions.ToNative` and `ToReport` methods explicitly convert native Forms
values. Engine drawing remains Skia; native Graphics, Font and Image are not substitutes.

`ReportComponentBase.Cursor` is a string, defaulting to `Default`; for example, `Hand`.
Known passive metadata, including mouse-event names and printer/duplex hints, remains
serializable without opening a window or querying a printer. Native cursor lookup returns
a shared cursor that hosts must not dispose. Hosts own their UI thread and message loop.
`RegisterEditor` accepts a real native UITypeEditor and returns a disposable registration.
Core no longer carries attributes naming unavailable property editors.

Legacy C# and VB layout/drawing imports, aliases and references migrate at compilation.
The stored script, comments, literals and interpolation text remain unchanged. Legacy
Forms assembly references are ignored after migration; Compat script references resolve
to core and drawing. Actual desktop script APIs fail with a feature and line/column
diagnostic. Merely importing the old namespace does not require Windows. Compiler
restrictions continue to apply after migration.

Business-object discovery uses IListSource/ITypedList, TypeDescriptor and declared
IEnumerable<T>/array item types. It samples at most 32 indexed list entries and never
advances arbitrary enumerables. Use a typed enumerable or provide an explicit schema
for an untyped streaming source. Factory-hook instances are disposed after discovery;
caller-owned objects are not. Enumerators are disposed when row loading finishes.

Interactive DialogPage templates fail during FRX loading, before their child controls
can be discarded. Programmatically added dialog pages also fail before the engine runs.
Supply report parameters from the application for headless jobs. There is no implemented
native dialog, designer, preview or print service in this distribution, including in the
Windows adapter; those remain future host integrations. Unsupported desktop extensions
are not promised lossless round-tripping. A Windows application may freely use native
Forms around a portable report job, but that does not enable desktop APIs inside scripts.

## Release evidence (2026-09-09)

| Check | Result |
| --- | --- |
| Windows .NET 10 core tests | 144 passed |
| Windows portable / Windows-targeted PDF tests | 13 / 15 passed, including STA adapter integration and editor disposal |
| Windows full solution | Builds with zero errors; existing warnings remain |
| Both migrated ReportBuilder suites | 5 tests passed in each |
| Ubuntu WSL, .NET SDK 10.0.100, display variables unset | Clean restore/build; 144 core and 13 PDF tests passed |
| Ubuntu Web | Builds with zero errors using an isolated Node installation |
| Isolated package consumers | Windows portable + Windows adapter, and Linux portable, all passed |
| Package output contracts | FRX round trip, sync/async data rows, 0.01-unit geometry tolerance, table cells, matrix aggregation, barcode, image, fixed fonts, two-page PDF, exact extracted text and image count |
| Windows/Linux rendered comparison | Both fixed-font smoke pages matched pixel-for-pixel when rasterized with PDFium at 1.5 scale; gate allowed mean absolute error 0.5/255 and at most 0.5% of pixels differing by more than 16 |
| Portable dependency checks | Transitive assembly and NuGet graphs reject desktop/Compat; consumers have package references only |
| Linux SDK packs | NETCore and ASP.NET reference packs and Linux host only; no WindowsDesktop packs |
| macOS | Not executed; release support is not declared verified |

Run `dotnet test Tools/FastReport.Tests.OpenSource/FastReport.Tests.OpenSource.csproj`
and the PDF test project listed in [the plan](headless-runtime.md). Run
`python Tools/verify-headless-packages.py` with Python 3, `pypdf==6.10.0`, and .NET 10
to pack and test isolated consumers; results are written under `_nuget/headless-validation`.
The script removes DISPLAY/WAYLAND_DISPLAY from child environments. Linux additionally
requires fontconfig and a font family such as DejaVu; native Skia/HarfBuzz assets restore
from the drawing package. The smoke fixture supplies original regular/bold fonts for its
fixed-font contracts. The repository matrix fixture also checks normal font fallback.

The GitHub Actions headless workflow repeats Windows/Linux tests and package-consumer
checks. No printer or desktop runtime is required for the portable run. PDF byte identity
is deliberately not a cross-platform contract.
