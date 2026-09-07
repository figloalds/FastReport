"""Generate original, minimal test fonts. Requires fontTools; not needed to run tests.

Only A and space have outlines/metrics. Distinct advances make regular/bold face
selection observable without depending on system fonts. No third-party font data.
"""
from pathlib import Path
from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

for style, advance, weight in [("Regular", 600, 400), ("Bold", 800, 700)]:
    builder = FontBuilder(1000, isTTF=True)
    builder.setupGlyphOrder([".notdef", "space", "A"])
    builder.setupCharacterMap({32: "space", 65: "A"})
    glyphs = {}
    for name in [".notdef", "space", "A"]:
        pen = TTGlyphPen(None)
        if name != "space":
            pen.moveTo((50, 0))
            pen.lineTo((advance // 2, 700))
            pen.lineTo((advance - 50, 0))
            pen.closePath()
        glyphs[name] = pen.glyph()
    builder.setupGlyf(glyphs)
    builder.setupHorizontalMetrics({name: (advance, 0) for name in glyphs})
    builder.setupHorizontalHeader(ascent=800, descent=-200)
    builder.setupNameTable({
        "familyName": "FastReport Test Sans", "styleName": style,
        "uniqueFontIdentifier": "FastReportTestSans-" + style,
        "fullName": "FastReport Test Sans " + style,
        "psName": "FastReportTestSans-" + style,
    })
    builder.setupOS2(sTypoAscender=800, sTypoDescender=-200, usWinAscent=800,
                     usWinDescent=200, usWeightClass=weight,
                     fsSelection=32 if style == "Bold" else 64)
    builder.setupPost()
    builder.setupMaxp()
    builder.font["head"].macStyle = 1 if style == "Bold" else 0
    builder.font["head"].created = builder.font["head"].modified = 2082844800
    builder.save(Path(__file__).with_name("TestSans-" + style + ".ttf"))
