[![Visits Badge](https://badges.pufler.dev/visits/FastReports/FastReport)](https://github.com/FastReports/FastReport) [![Created Badge](https://badges.pufler.dev/created/FastReports/FastReport)](https://github.com/FastReports/FastReport) [![Twitter Follow](https://img.shields.io/twitter/follow/fastreports?style=social)](https://twitter.com/FastReports)  [![Channel on Telegram](https://img.shields.io/badge/Channel%20on-Telegram-brightgreen.svg)](https://t.me/fastreport_open_source) [![Chat on Telegram](https://img.shields.io/badge/Chat%20on-Telegram-brightgreen.svg)](https://t.me/joinchat/hs87tfi79Rg3OGQy)

[![FastReport Open Source](https://fastreports.github.io/FastReport.Documentation/images/fros-youtube-title.jpg)](https://youtu.be/Js78gl_xAOU)

## What is FastReport?

FastReport provides a free open source report generator for .NET 10. You can use FastReport Open Source in MVC, Web API, and console applications on Windows, Linux, and macOS.

[![Image of FastReport](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot2-small.png)](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot2.png)

## Features

FastReport is written in C# and targets .NET 10. Its cross-platform graphics pipeline is implemented with SkiaSharp and does not depend on `System.Drawing`. Extendable FastReport architecture allows creating your own objects, export filters, wizards and DB engines.

[![Image of FastReport](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot1-small.png)](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot1.png)

### Report Objects

- FastReport is a band-oriented report generator. There are 13 types of bands available: Report Title, Report Summary, Page Header, Page Footer, Column Header, Column Footer, Data Header, Data, Data Footer, Group Header, Group Footer, Child and Overlay. In addition, sub-reports are fully supported. 

- A wide range of band types allows creating any kind of report: list, master-detail, group, multi-column, master-detail-detail and many more.

- Wide range of available report objects : text, picture, line, shape, barcode, matrix, table, checkbox.

- Reports can consist of several design pages, which allows reports to contain a cover, the data and a back cover, all in one file.

- The Table object allows building a tabular report with variable number of rows and/or columns, just like in MS Excel. Aggregate functions are also available.

- Powerful, fully configurable Matrix object that can be used to print pivot tables.

- Report inheritance. For creating many reports with common elements such as titles, logos or footers you can place all the common elements in a base report and inherit all other reports from this base.

### Data Sources

- You can get data from XML, CSV, Json, MS SQL, MySql, Oracle, Postgres, MongoDB, Couchbase, RavenDB, SQLite.

- FastReport has ability to get data from business objects of IEnumerable type. 

- Report can contain data sources (tables, queries, DB connections). 

- Thus you can not only use application-defined datasets but also connect to any database and use tables and queries directly within the report.

### Internal Scripting

FastReport has a built-in script engine that supports two .NET languages, C# and VB.NET. You can use all of the .NET power in your reports to perform complex data handling and much more.

## Working with report templates

You can make a report template in several ways:

- Creating report from code.

- Developing report template as XML file.

- Using the [FastReport Online Designer](https://fast-report.com/en/product/fast-report-online-designer/).

- Using the FastReport Designer Community Edition (freeware). It can be downloaded from [FastReport releases page](https://github.com/FastReports/FastReport/releases).

[![Image of FastReport](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot3-small.png)](https://raw.githubusercontent.com/FastReports/FastReport.Documentation/master/images/FastReport-screenshot3.png)

## Exporting

FastReport Open Source can save documents in HTML and Skia-supported raster image formats.

**PDF** export is available through [PdfExportSimple](https://github.com/FastReports/FastReport/tree/master/Extras/OpenSource/FastReport.OpenSource.Export.PdfSimple). It writes vector text, paths, gradients, and embedded images through SkiaSharp. You can see an example of its use [here](https://github.com/FastReports/FastReport/tree/master/Demos/OpenSource/Console%20apps/PdfExport).

Existing report code keeps the familiar load, register, prepare, and export flow:

```csharp
using FastReport;
using FastReport.Export.PdfSimple;

using var report = new Report();
report.Load("invoice.frx");
report.RegisterData(dataSet, "Data");
report.Prepare();
report.Export(new PDFSimpleExport(), "invoice.pdf");
```

On Linux, install `fontconfig` and at least one TrueType or OpenType font family in the runtime image. For example, Debian-based containers can install `fontconfig` and `fonts-dejavu-core`.

### Headless runtime and Windows Forms hosts

The reporting engine owns FRX loading, data discovery, expressions, layout, pagination and
prepared pages on `net10.0`. Exporters and future preview hosts consume that engine. PDF export
is provided by its separate exporter project. The engine can also be referenced by a
`net10.0-windows` application with `UseWindowsForms` enabled.

Report layout types now belong to `FastReport.Layout` in the reporting engine: `Padding`,
`AnchorStyles`, `DockStyle`, and `ImageSizeMode` (formerly `PictureBoxSizeMode`). Application code
must use these types and recompile; native WinForms values require explicit conversion at the
host boundary. Existing FRX layout names and values remain unchanged.

`FastReport.Drawing` supplies portable Skia drawing and font shaping; script compilation
belongs to core. `FastReport.Compat` and all Forms replacements have been removed.
The optional `FastReport.OpenSource.Windows` package supplies native layout/cursor
conversions and scoped editor registration. It does not provide a designer, preview,
printing service or interactive dialog implementation.

Legacy C# and VB layout/drawing scripts migrate during compilation, preserving stored
script text, literals and comments. Actual desktop APIs receive line/column diagnostics.
Dialog templates fail clearly in the headless path; application-supplied parameters remain
supported. Cursor names, mouse-event strings and printer hints remain passive metadata.

Windows and Linux core/PDF suites and isolated package consumers pass. macOS execution
has not been verified. See the [completed plan](docs/headless-runtime.md) and
[breaking release migration notes](docs/headless-release.md) for API changes, unsupported
features, the verification matrix and repeatable package-consumer tests.

## Report Designer Community Edition

To edit reports, we made a special report designer build - [FastReport Designer Community Edition](https://github.com/FastReports/FastReport/releases/latest). The program is intended for use in the Windows operating system and contains all the limitations of the Open Source version. We do not supply the source code of the editor because it is part of the commercial product [FastReport .NET](https://www.fast-report.com/en/product/fast-report-net/). Publishing this program is our good will and our wish. The MIT license does not cover its source code.

## Installation

FastReport can be compiled from sources or installed from [NuGet packages](https://www.nuget.org/profiles/FastReports).

### Compilation

1. Install the .NET 10 SDK for your OS from https://dotnet.microsoft.com/download/dotnet/10.0
2. Follow the commands

```sh
# for windows users
git clone https://github.com/FastReports/FastReport.git
cd FastReport
pack.bat
```

```sh
# for linux users
git clone https://github.com/FastReports/FastReport.git
cd FastReport
chmod 777 pack.sh && ./pack.sh
```

The package is located at `fr_packages` directory.

### NuGet

You can add FastReport to your current project via NuGet package manager:
```
Install-Package FastReport.OpenSource
Install-Package FastReport.OpenSource.Web
Install-Package FastReport.OpenSource.Export.PdfSimple
```

## Extras

The Extras folder contains additional modules that extend FastReport functionality:

- [Core/FastReport.Data](https://github.com/FastReports/FastReport/tree/master/Extras/Core/FastReport.Data) - connectors to various databases;
- [OpenSource/FastReport.OpenSource.Export.PdfSimple](https://github.com/FastReports/FastReport/tree/master/Extras/OpenSource/FastReport.OpenSource.Export.PdfSimple)  - simple export in PDF format;
- [ReportBuilder](https://github.com/FastReports/FastReport/tree/master/Extras/ReportBuilder) - a simple report builder from code without using templates.

## Examples

In the [Demos](https://github.com/FastReports/FastReport/tree/master/Demos) folder you can see examples of using FastReport.

## Bug Reports

See the [Issues](https://github.com/FastReports/FastReport/issues) section of website. When describing the issue, please attach screenshots or examples to help reproduce the problem.

## Contributors

This project exists because of all the people who have contributed and continue to work on the project:

[@ATZ-FR](https://github.com/ATZ-FR), [@Detrav](https://github.com/Detrav), [@fediachov](https://github.com/fediachov), [@8VAid8](https://github.com/8VAid8), 
 [@KirillKornienko](https://github.com/KirillKornienko), [@mandrookin](https://github.com/mandrookin), [@ekondur](https://github.com/ekondur), [@Gromozekaster](https://github.com/Gromozekaster), 
[@daviddesmet](https://github.com/daviddesmet), [@mjftechnology](https://github.com/mjftechnology), [@jonny-xhl](https://github.com/jonny-xhl), [@radiodeer](https://github.com/radiodeer), [@Des1re7](https://github.com/Des1re7), [@araujofrancisco](https://github.com/araujofrancisco), [@conqu1stador](https://github.com/conqu1stador), [@pietro29](https://github.com/pietro29).

## Contributing

Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct, and the process for submitting pull requests to us.

## Documentation

You can read the [FastReport Open Source Documentation](https://fastreports.github.io/FastReport.Documentation/) on the github site or you can read the [documentation for the commercial product](https://www.fast-report.com/public_download/docs/FRNet/online/en/index.html), amending the [functionality limitations](https://opensource.fast-report.com/p/the-feature-comparison-table-for.html).

## License

Licensed under the MIT license. See [LICENSE.md](LICENSE.md) for details. The MIT license does not cover the FastReport Designer Community Edition.

## Resources

- [FastReport Open Source Blog with Articles and How-Tos](https://opensource.fast-report.com/)
- [The Feature Comparison Table for FastReport Open Source, FastReport Core, FastReport .NET](https://opensource.fast-report.com/p/the-feature-comparison-table-for.html "FastReport Open Source vs FastReport Core vs FastReport .NET")
- [FastReport Core Online Demo](https://www.fast-report.com:2018 "Click to view FastReport Online Demo")
- [FastReport Online Designer](https://www.fast-report.com/en/product/fast-report-online-designer/ "Click to view FastReport Online Designer Home Page")
- [Fast Reports Home Page](https://www.fast-report.com "Click for visiting the Fast Reports Home Page")


