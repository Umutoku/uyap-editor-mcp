import json
import uuid
import zipfile
from xml.etree import ElementTree


def signed_rgb(color):
    return str(int(color[1:], 16) - 0x1000000)


def accept_format_palettes(tool, base, input_root):
    document = tool("uyap_new")["documentId"]
    text = "Renkli kurgu\nİkinci paragraf\nÜçüncü satır"
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)
    tool("uyap_set_palette_color", documentId=document, target="foreground", color="#1268B3",
         expected_error="PALETTE_SELECTION_REQUIRED")
    tool("uyap_select_text", documentId=document, start=0, end=len("Renkli kurgu"))
    for target, color in (("foreground", "#1268B3"), ("highlight", "#F7D159"),
                          ("paper", "#F7F2DF"), ("canvas", "#28394A")):
        actual = tool("uyap_set_palette_color", documentId=document, target=target, color=color)
        assert actual["color"] == color and actual["target"] == target
        if target in ("paper", "canvas"):
            assert actual["scope"] == "editor_profile"
            assert actual.get("start") is None and actual.get("end") is None
        else:
            assert actual["scope"] == "document"
            assert (actual["start"], actual["end"]) == (0, len("Renkli kurgu"))
    tool("uyap_set_palette_color", documentId=document, target="foreground", color="invalid",
         expected_error="PALETTE_COLOR_INVALID")
    tool("uyap_select_text", documentId=document, start=0, end=len(text))
    spacing = tool("uyap_set_line_spacing", documentId=document, multiplier=2.5)
    assert spacing["multiplier"] == 2.5
    assert (spacing["start"], spacing["end"]) == (0, len(text))
    tool("uyap_set_line_spacing", documentId=document, multiplier=1.7, expected_error="LINE_SPACING_UNAVAILABLE")
    target_start = text.index("İkinci")
    tool("uyap_select_text", documentId=document, start=0, end=len("Renkli kurgu"))
    tool("uyap_run_action", documentId=document, action="font-bold")
    tool("uyap_set_line_spacing", documentId=document, multiplier=2)
    character_brush = tool("uyap_apply_format_brush", documentId=document, sourceOffset=0,
                           start=target_start, end=target_start + len("İkinci"))
    assert not character_brush["includesParagraph"]
    paragraph_brush = tool("uyap_apply_format_brush", documentId=document, sourceOffset=1,
                           start=text.index("Üçüncü"), end=len(text), includeParagraph=True)
    assert paragraph_brush["includesParagraph"]

    def edit_colors():
        dialog = tool("uyap_open_form", documentId=document, action="edit-colors")
        assert dialog["title"] == "Renkler"
        choices = next(control for control in dialog["controls"] if control["role"] == "choice")
        dialog = tool("uyap_form_input", documentId=document, revision=dialog["revision"],
                      controlId=choices["id"], operation="choose", value="Alan Sınırı")
        swatch = next(control for control in dialog["controls"] if control["role"] == "color")
        return dialog, swatch

    dialog, swatch = edit_colors()
    former = swatch["value"]
    changed = "#2D83C7" if former != "#2D83C7" else "#296197"
    dialog = tool("uyap_form_input", documentId=document, revision=dialog["revision"],
                  controlId=swatch["id"], operation="color", value=changed)
    assert any(control["role"] == "color" and control["value"] == changed for control in dialog["controls"])
    tool("uyap_form_input", documentId=document, revision=dialog["revision"], controlId=-1, operation="cancel")
    dialog, swatch = edit_colors()
    assert swatch["value"] == changed
    tool("uyap_form_input", documentId=document, revision=dialog["revision"], controlId=-1, operation="cancel")

    path = base / f"kurgu-format-paletleri-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    assert markup.findtext("content").strip() == text
    runs = markup.findall("./elements/paragraph/content")
    assert any(run.get("foreground") == signed_rgb("#1268B3")
               and run.get("background") == signed_rgb("#F7D159") for run in runs)
    paragraphs = markup.findall("./elements/paragraph")
    assert [float(item.get("LineSpacing", "0")) for item in paragraphs] == [1, 1.5, 1]
    for paragraph in paragraphs[1:]:
        assert any(run.get("foreground") == signed_rgb("#1268B3")
                   and run.get("background") == signed_rgb("#F7D159") and run.get("bold") == "true"
                   for run in paragraph.findall("content"))
    reopened = tool("uyap_open", path=str(path))["documentId"]
    assert tool("uyap_read_text", documentId=reopened)["text"].strip() == text
    tool("uyap_close", documentId=reopened)
    lists = accept_list_palettes(tool, base)
    print(json.dumps({"formatPalettesPassed": True, "paletteUdf": str(path), "listsUdf": str(lists)}), flush=True)


def accept_list_palettes(tool, base):
    document = tool("uyap_new")["documentId"]
    styles = (
        ("Parantez", "number_parenthesis", "NUMBER_TYPE_NUMBER_PARANTHESE_2"),
        ("Nokta", "number_dot", "NUMBER_TYPE_NUMBER_DOT_2"),
        ("Büyük Harf", "uppercase", "NUMBER_TYPE_CHAR_BIG_2"),
        ("Alt düzey", "number_outline", "NUMBER_TYPE_NUMBER_DOT_3"),
        ("Elmas", "diamond", "BULLET_TYPE_DIAMOND_2"),
        ("Düz Paragraf", "none", None),
    )
    text = "\n".join(row[0] for row in styles)
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)
    for title, preset, expected in styles:
        selection = tool("uyap_find", documentId=document, query=title)
        assert selection["found"]
        if preset == "none":
            tool("uyap_set_list_palette", documentId=document, preset="number_dot")
            assert tool("uyap_find", documentId=document, query=title)["found"]
        result = tool("uyap_set_list_palette", documentId=document, preset=preset)
        assert result["preset"] == preset and result.get("nativeStyle") == expected
    tool("uyap_set_list_palette", documentId=document, preset="unknown", expected_error="LIST_PALETTE_OPTION_INVALID")
    path = base / f"kurgu-liste-paletleri-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    assert markup.findtext("content").strip() == text
    paragraphs = markup.findall("./elements/paragraph")
    assert len(paragraphs) == len(styles)
    for paragraph, (_, _, style) in zip(paragraphs, styles):
        if style is not None:
            assert style in paragraph.attrib.values()
        else:
            assert not any(value.startswith(("NUMBER_TYPE_", "BULLET_TYPE_")) for value in paragraph.attrib.values())
    reopened = tool("uyap_open", path=str(path))["documentId"]
    assert tool("uyap_read_text", documentId=reopened)["text"].strip() == text
    tool("uyap_close", documentId=reopened)
    return path
