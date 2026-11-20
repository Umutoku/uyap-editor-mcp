import base64
import hashlib
import math
import re
import struct
import uuid
from xml.etree import ElementTree

SVG = "{http://www.w3.org/2000/svg}"
HREF = "{http://www.w3.org/1999/xlink}href"
COORDINATES = re.compile(r"-?\d+(?:\.\d+)?(?:E-?\d+)?")


def accept_svg(suite, action):
    root_source = suite.input_root / "sentetik-3-sayfa.udf"
    source_hash = hashlib.sha256(root_source.read_bytes()).hexdigest()
    selected_color = "#1268b3"
    with suite.opened(path=root_source):
        marker = "KURGU SVG CANLI"
        suite.call("replace_text", start=0, end=8, text=marker)
        suite.select(0, len(marker))
        suite.call("set_palette_color", target="foreground", color=selected_color)
        assert suite.call("get_page")["pageCount"] == 3
        pages = _export_pages(suite)
        assert len(pages) == 3
        assert all(len(_visible_outlines(page)) > 90 for page in pages)
        assert all(not page.findall(f".//{SVG}image") for page in pages)
        assert any(path.get("fill") == selected_color for path in _visible_outlines(pages[0]))
        assert all(path.get("fill") != selected_color for page in pages[1:]
                   for path in page.findall(f".//{SVG}path"))
        suite.call("close", expected_error="UNSAVED_CHANGES")
    assert hashlib.sha256(root_source.read_bytes()).hexdigest() == source_hash

    with suite.opened(text="KURGU SVG GORSEL"):
        suite.call("insert_image", path=str(suite.input_root / "sentetik-gorsel.png"))
        (page,) = _export_pages(suite)
        assert any(len(path.attrib["d"]) > 200 for path in _visible_outlines(page))
        (image,) = page.findall(f".//{SVG}image")
        assert image.attrib[HREF].startswith("data:image/png;base64,")
        pixels = base64.b64decode(image.attrib[HREF].split(",", 1)[1], validate=True)
        assert pixels.startswith(b"\x89PNG\r\n\x1a\n")
        fixture = (suite.input_root / "sentetik-gorsel.png").read_bytes()
        assert struct.unpack(">II", pixels[16:24]) == struct.unpack(">II", fixture[16:24])
    return {"pages": len(pages), "liveTextVectorColor": selected_color, "embeddedImages": 1,
            "sourceUnchanged": True}


def _visible_outlines(page):
    width, height = (float(page.attrib[name]) for name in ("width", "height"))
    outlines = []
    for path in page.findall(f".//{SVG}path"):
        if path.get("fill") is None:
            continue
        coordinates = list(map(float, COORDINATES.findall(path.attrib["d"])))
        xs, ys = coordinates[::2], coordinates[1::2]
        if 0 <= min(xs) < max(xs) <= width and 0 <= min(ys) < max(ys) <= height:
            outlines.append(path)
    return outlines


def _export_pages(suite):
    target = suite.base / f"kurgu-svg-{uuid.uuid4().hex[:8]}.svg"
    output = suite.call("export_document", outputPath=str(target))
    assert output["format"] == "svg" and output["editorCommand"] == "compat:uyap-printable/svg", output
    data = target.read_bytes()
    assert hashlib.sha256(data).hexdigest().upper() == output["sha256"]
    root = ElementTree.fromstring(data)
    assert root.tag == f"{SVG}svg"
    _, _, width, height = map(float, root.attrib["viewBox"].split())
    pages = root.findall(f"./{SVG}svg")
    offset = 0.0
    references = set()
    for number, page in enumerate(pages, start=1):
        assert page.attrib["id"] == f"page-{number}"
        assert math.isclose(float(page.attrib["y"]), offset)
        assert float(page.attrib["width"]) <= width
        offset += float(page.attrib["height"])
        for element in page.iter():
            if "id" in element.attrib:
                assert element.attrib["id"] not in references
                references.add(element.attrib["id"])
            if element.tag == f"{SVG}path":
                assert re.search(r"[LCQ]", element.attrib["d"]), element.attrib["d"][:50]
    assert height > 0 and math.isclose(height, offset)
    for node in root.iter():
        if node.get("clip-path"):
            assert node.get("clip-path").removeprefix("url(#").removesuffix(")") in references
    return pages
