"""Pack and execute isolated NuGet consumers. Requires Python 3, pypdf and .NET 10.

Run from any directory. Outputs and logs go under _nuget/headless-validation.
Windows also exercises the opt-in adapter. No desktop packs are requested on Linux.
"""
import datetime
import json
import os
from pathlib import Path
import shutil
import subprocess
import xml.etree.ElementTree as ET
import zipfile
from pypdf import PdfReader

root = Path(__file__).resolve().parents[1]
version = "1.0.0-headless." + datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d%H%M%S")
output = root / "_nuget" / "headless-validation" / version
output.mkdir(parents=True)
config = ET.Element("configuration")
sources = ET.SubElement(config, "packageSources")
ET.SubElement(sources, "clear")
ET.SubElement(sources, "add", key="local", value=str(output))
ET.SubElement(sources, "add", key="nuget.org", value="https://api.nuget.org/v3/index.json")
ET.ElementTree(config).write(output / "NuGet.Config", encoding="utf-8", xml_declaration=True)
environment = os.environ.copy()
environment.pop("DISPLAY", None)
environment.pop("WAYLAND_DISPLAY", None)


def run(label, *arguments):
    with (output / (label + ".log")).open("w", encoding="utf-8") as log:
        result = subprocess.run(arguments, cwd=root, env=environment, stdout=log, stderr=subprocess.STDOUT)
    if result.returncode:
        raise RuntimeError(f"{label} failed; see {output / (label + '.log')}")
    print(f"PASS: {label}", flush=True)


projects = ["FastReport.Drawing/FastReport.Drawing.csproj", "FastReport.OpenSource/FastReport.OpenSource.csproj",
            "Extras/OpenSource/FastReport.OpenSource.Export.PdfSimple/FastReport.OpenSource.Export.PdfSimple/FastReport.OpenSource.Export.PdfSimple.csproj"]
if os.name == "nt":
    projects.append("FastReport.OpenSource.Windows/FastReport.OpenSource.Windows.csproj")
for index, project in enumerate(projects):
    run(f"pack-{index}", "dotnet", "pack", project, "-c", "Release", "-o", str(output),
        f"-p:PackageVersion={version}", "--nologo", "-v", "quiet")

for package in output.glob("*.nupkg"):
    with zipfile.ZipFile(package) as archive:
        manifest = ET.fromstring(archive.read(next(n for n in archive.namelist() if n.endswith(".nuspec"))))
        portable = "OpenSource.Windows." not in package.name
        assert not any("FastReport.Compat" in name for name in archive.namelist())
        for element in manifest.iter():
            if element.tag.endswith("dependency"):
                assert element.get("id") != "FastReport.Compat"
            if portable and element.tag.endswith("frameworkReference"):
                assert "WindowsDesktop" not in element.get("name", "")

for windows in ([False, True] if os.name == "nt" else [False]):
    name = "windows" if windows else "portable"
    consumer = output / name
    consumer.mkdir()
    # Copy only consumer sources, fixtures and package references into an isolated directory.
    project = ET.parse(root / "Tools/HeadlessSmoke/HeadlessSmoke.csproj")
    for group in list(project.getroot()):
        if "UsePackages" in group.get("Condition", "") and "!= 'true'" in group.get("Condition", ""):
            project.getroot().remove(group)
        for item in list(group):
            if item.tag == "None":
                group.remove(item)
    files = ET.SubElement(project.getroot(), "ItemGroup")
    for pattern in ("*.frx", "Fixtures/*.ttf"):
        ET.SubElement(files, "None", Update=pattern, CopyToOutputDirectory="PreserveNewest")
    project.write(consumer / "HeadlessSmoke.csproj", encoding="utf-8", xml_declaration=True)
    shutil.copy(root / "Tools/HeadlessSmoke/Program.cs", consumer)
    shutil.copy(root / "Tools/HeadlessSmoke/Smoke.frx", consumer)
    shutil.copy(root / "Demos/Reports/Simple Matrix.frx", consumer / "Matrix.frx")
    (consumer / "Fixtures").mkdir()
    for font in (root / "Tools/FastReport.Tests.OpenSource/Fixtures").glob("*.ttf"):
        shutil.copy(font, consumer / "Fixtures")
    properties = ["-p:UsePackages=true", f"-p:SmokePackageVersion={version}", f"-p:WindowsHost={str(windows).lower()}"]
    csproj = str(consumer / "HeadlessSmoke.csproj")
    run(name + "-restore", "dotnet", "restore", csproj, *properties, "--packages", str(output / "packages"),
        "--configfile", str(output / "NuGet.Config"), "--nologo", "-v", "quiet")
    assets = json.loads((consumer / "obj/project.assets.json").read_text(encoding="utf-8"))
    for library, info in assets["libraries"].items():
        assert info["type"] == "package", (library, info["type"])
        assert not library.startswith("FastReport.Compat/")
        if not windows:
            assert not any(token in library for token in ("System.Drawing.Common/", "WindowsDesktop", "FastReport.OpenSource.Windows/")), library
    if not windows:
        for framework in assets["project"]["frameworks"].values():
            assert not any("WindowsDesktop" in key for key in framework.get("frameworkReferences", {}))
    run(name + "-build", "dotnet", "build", csproj, *properties, "-c", "Release", "--no-restore", "--nologo", "-v", "quiet")
    tfm = "net10.0-windows" if windows else "net10.0"
    run(name + "-execute", "dotnet", str(consumer / "bin/Release" / tfm / "HeadlessSmoke.dll"), str(consumer / "pdf"))
    for mode in ("sync", "async"):
        pdf = PdfReader(consumer / "pdf" / f"smoke-{mode}.pdf")
        assert len(pdf.pages) == 2
        first = " ".join(pdf.pages[0].extract_text().split())
        second = " ".join(pdf.pages[1].extract_text().split())
        assert first == "AAA A AA A AA AAA", first
        assert second == "AAA AAA", second
        assert len(pdf.pages[0].images) == 1
    print(f"PASS: {name} extracted PDF text and images", flush=True)

print(f"Validated packages and consumers: {output}")
