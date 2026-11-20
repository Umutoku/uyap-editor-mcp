import json
import uuid
import zipfile
from collections import defaultdict
from xml.etree import ElementTree

from pypdf import PdfReader
from pypdf.generic import ContentStream


def pdf_text_rows(page):
    rows = defaultdict(list)

    def collect_text(value, transform, text_matrix, _font, _size):
        value = value.strip()
        if value:
            x, y = text_matrix[4:6]
            page_x = x * transform[0] + y * transform[2] + transform[4]
            page_y = x * transform[1] + y * transform[3] + transform[5]
            rows[round(page_y, 2)].append((page_x, value))

    page.extract_text(visitor_text=collect_text)
    return [(y, sorted(rows[y])) for y in sorted(rows, reverse=True)]


def pdf_filled_paths(reader, page):
    paths = []
    for form in page["/Resources"]["/XObject"].values():
        stream = form.get_object()
        if stream["/Subtype"] != "/Form":
            continue
        path = []
        for operands, operator in ContentStream(stream, reader).operations:
            if operator == b"m":
                path = [(operator, operands)]
            elif operator in (b"l", b"c"):
                path.append((operator, operands))
            elif operator in (b"f", b"f*"):
                paths.append(path)
                path = []
            elif operator in (b"n", b"S"):
                path = []
    return paths


def verify_mixed_bullet_shapes(reader, page, rows, shapes):
    paths = pdf_filled_paths(reader, page)
    for row_index, signature in shapes.items():
        baseline, row = rows[row_index]
        text_x = row[0][0]
        matching = []
        for path in paths:
            points = [point for _, coordinates in path
                      for point in zip(coordinates[::2], coordinates[1::2], strict=True)]
            if points and all(text_x - 26 <= x <= text_x - 5 and abs(baseline - y) < 7 for x, y in points):
                matching.append(tuple(operator for operator, _ in path[1:]))
        assert matching == [signature], (row_index, matching)


def accept_custom_list(tool, base):
    names = ("Kurgu Alfa", "Kurgu Beta", "Kurgu Gama", "Kurgu Delta", "Kurgu Epsilon", "Kurgu Zeta")
    lines = ("Duzenlenmeyen baslik", *names)
    text = "\n".join(lines)
    formats = [
        {"level": 1, "format": "NUMBER_TYPE_NUMBER_DOT"},
        {"level": 2, "format": "NUMBER_TYPE_CHAR_BIG_PARANTHESE"},
        {"level": 3, "format": "NUMBER_TYPE_ROMAN_SMALL_DOT"},
        {"level": 4, "format": "NUMBER_TYPE_CHAR_SMALL_DOT"},
        {"level": 5, "format": "NUMBER_TYPE_ROMAN_BIG_TRE"},
        {"level": 6, "format": "NUMBER_TYPE_NUMBER_D_PARANTHESE"},
    ]
    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)

    def select(first, last=None):
        final = last or first
        tool("uyap_select_text", documentId=document, start=text.index(first), end=text.index(final) + len(final))

    select(names[0], names[-1])
    tool("uyap_set_custom_list", documentId=document, levels=[formats[0], formats[0]],
         expected_error="LIST_LEVEL_FORMATS_INVALID")
    tool("uyap_set_custom_list", documentId=document, levels=[{"level": 2, "format": "unknown"}],
         expected_error="LIST_NUMBER_FORMAT_INVALID")
    created = tool("uyap_set_custom_list", documentId=document, levels=formats)
    assert created == {"itemCount": 6, "created": True}
    for level, name in enumerate(names, 1):
        select(name)
        assert tool("uyap_set_list_level", documentId=document, level=level)["level"] == level

    select(lines[0], names[0])
    tool("uyap_set_custom_list", documentId=document, levels=[formats[0]], expected_error="LIST_SELECTION_INVALID")
    select(names[2])
    formats[2] = {"level": 3, "format": "NUMBER_TYPE_ROMAN_SMALL_D_PARANTHESE"}
    updated = tool("uyap_set_custom_list", documentId=document, levels=[formats[2]])
    assert updated == {"itemCount": 6, "created": False}

    prefix = base / f"kurgu-ozel-cok-duzeyli-liste-{uuid.uuid4().hex[:8]}"
    path = prefix.with_suffix(".udf")
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    assert markup.findtext("content").strip() == text
    heading, *paragraphs = markup.findall("./elements/paragraph")
    assert heading.get("ListId") is None
    assert len(paragraphs) == 6
    ids = {paragraph.get("ListId") for paragraph in paragraphs}
    assert len(ids) == 1 and None not in ids
    for level, paragraph in enumerate(paragraphs, 1):
        assert paragraph.get("ListLevel") == str(level)
        assert [paragraph.get(f"SecListTypeLevel{index}") for index in range(1, 7)] == [
            level_format["format"] for level_format in formats
        ]

    document = tool("uyap_open", path=str(path))["documentId"]
    assert tool("uyap_read_text", documentId=document)["text"].strip() == text
    for level, name in enumerate(names, 1):
        select(name)
        item = tool("uyap_get_list_item", documentId=document)
        assert item["level"] == level and item["levelFormats"] == formats
    pdf = prefix.with_suffix(".pdf")
    tool("uyap_export_pdf", documentId=document, outputPath=str(pdf))
    pages = PdfReader(pdf, strict=True).pages
    assert len(pages) == 1
    rendered = [" ".join(value for _, value in row) for _, row in pdf_text_rows(pages[0])]
    labels = ("1.", "A)", "(i)", "a.", "I-", "(1)")
    assert rendered == [lines[0], *(f"{label} {name}" for label, name in zip(labels, names, strict=True))], rendered
    tool("uyap_close", documentId=document)
    print(json.dumps({"acceptance": "custom_list", "status": "passed", "udf": str(path),
                      "pdf": str(pdf), "levelFormats": formats, "pdfListLabels": labels}), flush=True)


def accept_mixed_list(tool, base):
    names = ("Kurgu Birinci", "Kurgu Ikinci", "Kurgu Ucuncu", "Kurgu Dorduncu",
             "Kurgu Besinci", "Kurgu Altinci", "Kurgu Sonuncu")
    text = "\n".join(names)
    levels = (1, 2, 3, 4, 5, 6, 1)
    bullet_formats = [dict(level=index, format=f"BULLET_TYPE_{shape}") for index, shape in enumerate(
        ("RECTANGLE_D", "ELLIPSE", "RECTANGLE", "DIAMOND", "ARROW", "TRIANGLE"), 1)]
    number_formats = [
        {"level": 1, "format": "NUMBER_TYPE_NUMBER_DOT"},
        {"level": 3, "format": "NUMBER_TYPE_CHAR_BIG_PARANTHESE"},
        {"level": 5, "format": "NUMBER_TYPE_ROMAN_BIG_TRE"},
    ]
    formats = bullet_formats.copy()
    for entry in number_formats:
        formats[entry["level"] - 1] = entry

    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)

    def select(name):
        start = text.index(name)
        tool("uyap_select_text", documentId=document, start=start, end=start + len(name))

    tool("uyap_select_text", documentId=document, start=0, end=len(text))
    tool("uyap_set_custom_list", documentId=document, levels=[{"level": 2, "format": "BULLET_TYPE_UNKNOWN"}],
         expected_error="LIST_NUMBER_FORMAT_INVALID")
    assert tool("uyap_set_custom_list", documentId=document, levels=bullet_formats) == {
        "itemCount": len(names), "created": True}
    for name, level in zip(names, levels, strict=True):
        select(name)
        item = tool("uyap_set_list_level", documentId=document, level=level)
        assert item["level"] == level and not item["numbered"] and item["levelFormats"] == bullet_formats
    select(names[1])
    assert tool("uyap_set_custom_list", documentId=document, levels=number_formats) == {
        "itemCount": len(names), "created": False}
    tool("uyap_set_numbering_start", documentId=document, startAt=4,
         expected_error="NUMBERED_LIST_ITEM_REQUIRED")
    tool("uyap_continue_numbering", documentId=document, expected_error="NUMBERED_LIST_ITEM_REQUIRED")

    prefix = base / f"kurgu-karisik-cok-duzeyli-liste-{uuid.uuid4().hex[:8]}"
    path = prefix.with_suffix(".udf")
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    paragraphs = markup.findall("./elements/paragraph")
    ids = {paragraph.get("ListId") for paragraph in paragraphs}
    assert len(ids) == 1 and next(iter(ids)) not in (None, "0")
    for paragraph, level in zip(paragraphs, levels, strict=True):
        assert paragraph.get("ListLevel") == str(level)
        assert [paragraph.get(f"SecListTypeLevel{index}") for index in range(1, 7)] == [
            entry["format"] for entry in formats
        ]

    document = tool("uyap_open", path=str(path))["documentId"]
    assert tool("uyap_read_text", documentId=document)["text"].strip() == text
    for name, level in zip(names, levels, strict=True):
        select(name)
        item = tool("uyap_get_list_item", documentId=document)
        assert item["level"] == level and item["levelFormats"] == formats
        assert item["numbered"] == (level in (1, 3, 5))

    pdf = prefix.with_suffix(".pdf")
    tool("uyap_export_pdf", documentId=document, outputPath=str(pdf))
    reader = PdfReader(pdf, strict=True)
    assert len(reader.pages) == 1
    markers = ("1.", None, "A)", None, "I-", None, "2.")
    rows = pdf_text_rows(reader.pages[0])
    rendered = [" ".join(value for _, value in row) for _, row in rows]
    assert rendered == [f"{marker} {name}" if marker else name
                        for marker, name in zip(markers, names, strict=True)], rendered
    verify_mixed_bullet_shapes(reader, reader.pages[0], rows, {1: (b"c",) * 4, 3: (b"l",) * 5, 5: (b"l",) * 2})
    tool("uyap_close", documentId=document)
    print(json.dumps({"acceptance": "mixed_list", "status": "passed", "udf": str(path),
                      "pdf": str(pdf), "levelFormats": formats, "pdfNumberLabels": markers,
                      "pdfBulletShapes": ["ELLIPSE", "DIAMOND", "TRIANGLE"]}), flush=True)


def accept_list_indents(tool, base):
    names = ("Duzenlenmeyen baslik", "Kurgu Birinci", "Kurgu Imli", "Kurgu Roma", "Kurgu Sonuncu")
    text = "\n".join(names)
    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)
    formats = [
        {"level": 1, "format": "NUMBER_TYPE_NUMBER_DOT"},
        {"level": 2, "format": "BULLET_TYPE_DIAMOND"},
        {"level": 3, "format": "NUMBER_TYPE_ROMAN_BIG_DOT"},
    ]
    levels = (1, 2, 3, 1)
    indents = {1: 48, 2: 88, 3: 135}

    def select(name):
        start = text.index(name)
        tool("uyap_select_text", documentId=document, start=start, end=start + len(name))

    def set_indents(changes, error=None):
        return tool("uyap_set_list_indents", documentId=document,
                    levels=[{"level": level, "indentPoints": indent} for level, indent in changes.items()],
                    expected_error=error)

    tool("uyap_select_text", documentId=document, start=text.index(names[1]), end=len(text))
    tool("uyap_set_custom_list", documentId=document, levels=formats)
    for name, level in zip(names[1:], levels, strict=True):
        select(name)
        tool("uyap_set_list_level", documentId=document, level=level)
    assert set_indents({**indents, 2: 80}) == {"itemCount": 4}
    assert set_indents({2: indents[2]}) == {"itemCount": 1}
    set_indents({1: -1}, "LIST_INDENT_INVALID")
    set_indents({1: 61, 6: 150}, "LIST_LEVEL_NOT_PRESENT")

    path = base / f"kurgu-cok-duzeyli-girinti-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    heading, *paragraphs = markup.findall("./elements/paragraph")
    assert heading.get("ListId") is None
    assert len({paragraph.get("ListId") for paragraph in paragraphs}) == 1
    for paragraph, level in zip(paragraphs, levels, strict=True):
        assert float(paragraph.get("LeftIndent")) == indents[level]

    document = tool("uyap_open", path=str(path))["documentId"]
    assert tool("uyap_read_text", documentId=document)["text"].strip() == text
    for name, level in zip(names[1:], levels, strict=True):
        select(name)
        item = tool("uyap_get_list_item", documentId=document)
        assert item["level"] == level and item["indentPoints"] == indents[level]
        assert item["levelFormats"] == formats

    pdf = path.with_suffix(".pdf")
    tool("uyap_export_pdf", documentId=document, outputPath=str(pdf))
    reader = PdfReader(pdf, strict=True)
    assert len(reader.pages) == 1
    page = reader.pages[0]
    rows = pdf_text_rows(page)
    assert [" ".join(value for _, value in row) for _, row in rows] == [
        names[0], f"1. {names[1]}", names[2], f"I. {names[3]}", f"2. {names[4]}"
    ]
    margin_x = rows[0][1][0][0]
    for (_, row), level in zip(rows[1:], levels, strict=True):
        text_x = next(x for x, value in row if value == "Kurgu")
        assert abs(text_x - margin_x - indents[level]) < 0.51
        if level != 2:
            assert abs(text_x - row[0][0] - 17) < 0.51
    verify_mixed_bullet_shapes(reader, page, rows, {2: (b"l",) * 5})
    tool("uyap_close", documentId=document)
    print(json.dumps({"acceptance": "list_indents", "status": "passed", "udf": str(path),
                      "pdf": str(pdf), "indentPoints": indents}), flush=True)
