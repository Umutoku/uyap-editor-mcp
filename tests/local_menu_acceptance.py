import json
import re
import uuid
from xml.etree import ElementTree


def accept_menu_inventory(tool, base, source):
    document_id = tool("uyap_open", path=str(source))["documentId"]
    template_id = tool("uyap_open_template")["documentId"]
    windows = tool("uyap_list_windows")["windows"]
    assert {(window["documentId"], window["mode"]) for window in windows} == {
        (document_id, "document"), (template_id, "template")}
    table = {}
    for mode, current in (("document", document_id), ("template", template_id)):
        table[mode] = {}
        for scope in ("menus", "actions"):
            rows = []
            while True:
                page = tool("uyap_menu_inventory", documentId=current, scope=scope, offset=len(rows), limit=100)
                assert page["offset"] == len(rows) and page["mode"] == mode and page["scope"] == scope
                assert page["entries"] or len(rows) == page["total"]
                rows.extend(page["entries"])
                if len(rows) == page["total"]:
                    break
            table[mode][scope] = rows
        assert len({row["action"] for row in table[mode]["actions"]}) >= 180
        assert any("Dosya" in row["path"] and row["label"] == "PDF Olarak Kaydet"
                   for row in table[mode]["menus"])
        tool("uyap_close", documentId=current)
    assert any("Şablon" in row["path"] for row in table["template"]["menus"])
    table_id = tool("uyap_new")["documentId"]
    form = tool("uyap_open_form", documentId=table_id, action="table-insert")
    for _ in range(2):
        if form["state"] != "open":
            break
        button = next(item for item in form["controls"] if item["role"] == "button"
                      and item["label"] in ("Tamam", "Ekle"))
        form = tool("uyap_form_input", documentId=table_id, revision=form["revision"],
                    controlId=button["id"], operation="press")
    assert form["state"] == "closed"
    tool("uyap_run_action", documentId=table_id, action="caret-begin")
    rows = []
    while True:
        page = tool("uyap_menu_inventory", documentId=table_id, scope="menus", offset=len(rows), limit=100)
        assert page["entries"] or len(rows) == page["total"]
        rows.extend(page["entries"])
        if len(rows) == page["total"]:
            break
    table["tableDocument"] = {"menus": rows}
    fixture = base / f"kurgu-envanter-tablo-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=table_id, outputPath=str(fixture))
    tool("uyap_close", documentId=table_id)
    assert tool("uyap_list_windows")["windows"] == []
    path = base / "uyap-native-catalog.json"
    path.write_text(json.dumps(table, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(dict(menuInventoryPassed=True, catalog=str(path),
                          counts={mode: {scope: len(items) for scope, items in scopes.items()}
                                  for mode, scopes in table.items()})), flush=True)


def accept_native_renderers(tool, base):
    document_id = tool("uyap_new")["documentId"]
    first, second = "KURGUBICIMILK", "KURGUBICIMIKINCI"
    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text=first)
    tool("uyap_run_action", documentId=document_id, action="caret-end")
    tool("uyap_run_action", documentId=document_id, action="insert-pageBreak")
    end = tool("uyap_read_text", documentId=document_id)["totalCharacters"]
    tool("uyap_replace_text", documentId=document_id, start=end, end=end, text=second)
    stamp = uuid.uuid4().hex[:8]
    html_outputs = []
    for variant in ("rtf", "ps", "xsl", "html", "html-with-breaks"):
        extension = variant.split("-")[0]
        path = base / f"kurgu-render-{stamp}-{variant}.{extension}"
        result = tool("uyap_export_document", documentId=document_id, outputPath=str(path),
                      htmlPageBreaks=variant == "html-with-breaks")
        assert result["editorCommand"] == f"renderer:export-{variant}"
        data = path.read_bytes()
        if variant == "rtf":
            assert data.startswith(b"{\\rtf") and first.encode() in data and second.encode() in data
        elif variant == "ps":
            assert data.startswith(b"%!PS-Adobe") and data.rstrip().endswith(b"%%EOF")
            pages = re.findall(rb"^%%Page:", data, re.MULTILINE)
            assert pages and f"%%Pages: {len(pages)}".encode() in data
            assert first.encode() in data and second.encode() in data
        elif variant == "xsl":
            root = ElementTree.fromstring(data)
            assert root.tag == "{http://www.w3.org/1999/XSL/Format}root"
            assert first in "".join(root.itertext()) and second in "".join(root.itertext())
        else:
            assert b"</html>" in data.lower()
            assert first.encode() in data and second.encode() in data
            html_outputs.append(data)
    svg = base / f"kurgu-render-{stamp}.svg"
    result = tool("uyap_export_document", documentId=document_id, outputPath=str(svg))
    assert result["editorCommand"] == "compat:uyap-printable/svg", result
    svg_namespace = "{http://www.w3.org/2000/svg}"
    svg_root = ElementTree.parse(svg).getroot()
    assert svg_root.tag == svg_namespace + "svg" and svg_root.find(".//" + svg_namespace + "path") is not None
    tool("uyap_save_copy", documentId=document_id, outputPath=str(base / f"kurgu-render-{stamp}.udf"))
    tool("uyap_close", documentId=document_id)
    print(json.dumps({"nativeRenderersPassed": True, "htmlPageBreakVariantDiffers": html_outputs[0] != html_outputs[1],
                      "postScriptPages": len(pages)}), flush=True)


def accept_local_clipboard(tool, base):
    document_id = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text="Kurgu Metin")
    tool("uyap_run_action", expected_error="CLIPBOARD_EMPTY", documentId=document_id,
         action="paste-from-clipboard")
    tool("uyap_select_text", documentId=document_id, start=0, end=5)
    tool("uyap_run_action", documentId=document_id, action="copy-to-clipboard")
    tool("uyap_select_text", documentId=document_id, start=6, end=11)
    tool("uyap_run_action", documentId=document_id, action="paste-from-clipboard")
    assert tool("uyap_read_text", documentId=document_id)["text"] == "Kurgu Kurgu"
    tool("uyap_select_text", documentId=document_id, start=5, end=11)
    tool("uyap_run_action", documentId=document_id, action="cut-to-clipboard")
    assert tool("uyap_read_text", documentId=document_id)["text"] == "Kurgu"
    tool("uyap_run_action", documentId=document_id, action="paste-from-clipboard")
    assert tool("uyap_read_text", documentId=document_id)["text"] == "Kurgu Kurgu"
    fixture = base / f"kurgu-pano-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document_id, outputPath=str(fixture))
    tool("uyap_close", documentId=document_id)
    print(json.dumps({"sessionClipboardPassed": True}), flush=True)


def accept_print_preview(tool, source):
    document_id = tool("uyap_open", path=str(source), editable=True)["documentId"]
    form = tool("uyap_open_form", documentId=document_id, action="print-preview")
    assert form["title"] == "Yazdırma Önizlemesi"
    zoom = next(item for item in form["controls"] if "%50" in item["options"])
    form = tool("uyap_form_input", documentId=document_id, revision=form["revision"],
                controlId=zoom["id"], operation="choose", value="%50")
    assert any(item["role"] == "choice" and item["value"] == "%50" for item in form["controls"])
    close = next(item for item in form["controls"] if item["role"] == "button" and item["label"] == "Kapat")
    form = tool("uyap_form_input", documentId=document_id, revision=form["revision"],
                controlId=close["id"], operation="press")
    assert form["state"] == "closed"
    tool("uyap_close", documentId=document_id)
    print(json.dumps({"printPreviewPassed": True}), flush=True)


def accept_format_actions(tool, base):
    import zipfile

    document_id = tool("uyap_new")["documentId"]
    names = ["Italik", "Altcizgi", "Ustcizgi", "AltSimge", "UstSimge", "Boyut", "BosBicim",
             "Sola", "Orta", "Saga", "IkiYana"]
    sample = "\n".join(names)
    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text=sample)

    def action(command, selection=None):
        if selection:
            match = tool("uyap_find", documentId=document_id, query=selection)
            assert match["found"]
        return tool("uyap_run_action", documentId=document_id, action=command)

    commands = [("font-italic", "Italik"), ("font-underline", "Altcizgi"), ("Strike-through", "Ustcizgi"),
                ("sub-script", "AltSimge"), ("super-script", "UstSimge"), ("increase-font-size", "Boyut"),
                ("font-bold", "BosBicim"), ("clear-format", "BosBicim"), ("center-justify", "Orta"),
                ("right-justify", "Saga"), ("both-justify", "IkiYana"), ("right-justify", "Sola"),
                ("left-justify", "Sola")]
    for command, label in commands:
        action(command, label)
    tool("uyap_find", documentId=document_id, query="Sola")
    tool("uyap_set_font", documentId=document_id, size=15)
    action("decrease-font-size")
    action("select-all")
    assert tool("uyap_view_state", documentId=document_id)["selectionEnd"] == len(sample)
    tool("uyap_replace_text", documentId=document_id, start=len(sample), end=len(sample), text=" SON")
    action("undo")
    assert tool("uyap_read_text", documentId=document_id)["text"] == sample
    action("redo")
    assert tool("uyap_read_text", documentId=document_id)["text"] == sample + " SON"
    path = base / f"kurgu-bicim-eylemleri-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document_id, outputPath=str(path))
    tool("uyap_close", documentId=document_id)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    elements = markup.find("elements")
    by_name = {}
    for node in elements.findall("paragraph"):
        text = "".join(sample[int(item.get("startOffset")):
                              int(item.get("startOffset")) + int(item.get("length"))]
                       for item in node if item.get("startOffset") is not None and item.get("length") is not None)
        if text.strip() in names:
            by_name[text.strip()] = node
    for name, attribute in (("Italik", "italic"), ("Altcizgi", "underline"), ("Ustcizgi", "strikethrough"),
                            ("AltSimge", "subscript"), ("UstSimge", "superscript")):
        assert any(key.casefold() == attribute and value == "true"
                   for item in by_name[name] for key, value in item.attrib.items()), ElementTree.tostring(by_name[name])
    assert int(by_name["Orta"].get("Alignment", "0")) == 1
    assert int(by_name["Saga"].get("Alignment", "0")) == 2
    assert int(by_name["IkiYana"].get("Alignment", "0")) == 3
    assert int(by_name["Sola"].get("Alignment", "0")) == 0
    assert not any(item.get("bold") == "true" for item in by_name["BosBicim"])
    assert any(int(item.get("size", "12")) > 12 for item in by_name["Boyut"])
    assert any(0 < int(item.get("size", "12")) < 15 for item in by_name["Sola"])
    print(json.dumps({"formatActionsPassed": True}), flush=True)
