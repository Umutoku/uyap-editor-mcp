import shutil
import uuid
import zipfile
from xml.etree import ElementTree


def accept_local_forms(tool, base, input_root):
    document = tool("uyap_new")["documentId"]

    def fields(action):
        return tool("uyap_open_form", documentId=document, action=action)

    def choose(form, role, operation, label=None, value=None):
        item = next(item for item in form["controls"]
                    if item["role"] == role and (label is None or item["label"] == label))
        parameters = dict(documentId=document, revision=form["revision"], controlId=item["id"], operation=operation)
        if value is not None:
            parameters["value"] = value
        return tool("uyap_form_input", **parameters)

    def read_regions():
        result = tool("uyap_page_regions", documentId=document)
        return {entry["region"]: entry for entry in result["regions"]}

    stamp = uuid.uuid4().hex[:8]
    body = "KURGU BELGE GÖVDESİ"
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=body)
    tool("uyap_run_action", documentId=document, action="caret-end")

    symbol = fields("insert symbol")
    symbol = choose(symbol, "text", "write", value="§")
    assert choose(symbol, "button", "press", "Ekle")["state"] == "closed"
    assert "§" in tool("uyap_read_text", documentId=document)["text"]

    barcode = fields("insert-barcode")
    assert not any(item["label"] == "Tamam" for item in barcode["controls"])
    barcode_input = next(item for item in barcode["controls"] if item["role"] == "text")
    tool("uyap_form_input", expected_error="BARCODE_VALUE_INVALID", documentId=document,
         revision=barcode["revision"], controlId=barcode_input["id"], operation="write", value="abc")
    barcode = choose(barcode, "text", "write", value="12345678901")
    assert choose(barcode, "button", "press", "Tamam")["state"] == "closed"

    assert all(not entry["present"] for entry in read_regions().values())
    header = "KURGU ÜST YAZI"
    footer = "KURGU ALT YAZI"
    tool("uyap_set_page_region", documentId=document, region="header", text="İlk üst yazı")
    updated = tool("uyap_set_page_region", documentId=document, region="header", text=header)
    assert updated["present"] and updated["start"] == 0 and updated["end"] == len(header)
    tool("uyap_set_page_region", documentId=document, region="footer", text=footer)
    assert {key: entry["text"] for key, entry in read_regions().items()} == {"header": header, "footer": footer}

    number = fields("insert-pageNumber")
    number = choose(number, "toggle", "toggle", "Sağ", "true")
    number = choose(number, "text", "write", value="KURGU SAYFA ")
    assert choose(number, "button", "press", "Tamam")["state"] == "closed"

    layout = fields("page-setup")
    layout = choose(layout, "choice", "choose", value="iso-a5")
    layout = choose(layout, "toggle", "toggle", "Yatay", "true")
    assert choose(layout, "button", "press", "Tamam")["state"] == "closed"

    path = base / f"kurgu-formlar-{stamp}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    with zipfile.ZipFile(path) as archive:
        saved = ElementTree.fromstring(archive.read("content.xml"))
    assert saved.find("elements/header") is not None
    saved_footer = saved.find("elements/footer")
    assert saved_footer.get("pageNumber-foreStr") == "KURGU SAYFA "
    assert saved_footer.get("pageNumber-spec") == "BSP32_72"
    assert saved.find(".//barcode").get("barcodeData") == "12345678901"
    page_format = saved.find(".//pageFormat")
    assert page_format.get("mediaSizeName") == "2" and page_format.get("paperOrientation") == "0"
    assert body + "§" in saved.findtext("content")
    tool("uyap_close", documentId=document)

    copied = shutil.copyfile(path, input_root / path.name)
    document = tool("uyap_open", path=str(copied), editable=True)["documentId"]
    assert {key: entry["text"] for key, entry in read_regions().items()} == {"header": header, "footer": footer}
    number = fields("insert-pageNumber")
    number = choose(number, "toggle", "toggle", "Kaldır", "true")
    assert choose(number, "button", "press", "Tamam")["state"] == "closed"
    assert fields("insert-header")["state"] == "completed"
    assert not read_regions()["header"]["present"]
    removed = base / f"kurgu-ust-yazi-kaldirildi-{stamp}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(removed))
    with zipfile.ZipFile(removed) as archive:
        saved = ElementTree.fromstring(archive.read("content.xml"))
    assert saved.find("elements/header") is None
    assert saved.find("elements/footer").get("pageNumber-spec") == "BSP32_0"
    tool("uyap_close", documentId=document)

    document = tool("uyap_new")["documentId"]
    table = fields("table-insert")
    table = choose(table, "button", "press", "Tamam")
    if table["state"] == "open":
        table = choose(table, "button", "press", "Ekle")
    assert table["state"] == "closed"
    tool("uyap_run_action", documentId=document, action="caret-begin")
    row = fields("table-row-properties")
    row = choose(row, "text", "write", value="1.25")
    assert choose(row, "button", "press", "Tamam")["state"] == "closed"
    column = fields("table-col-properties")
    column = choose(column, "text", "write", value="80")
    assert choose(column, "button", "press", "Tamam")["state"] == "closed"
    height = fields("table-height")
    height = choose(height, "toggle", "toggle", "Düzenle", "true")
    height = choose(height, "text", "write", value="4")
    assert choose(height, "button", "press", "Tamam")["state"] == "closed"
    border = fields("edit-table-border")
    border = choose(border, "toggle", "toggle", "Yok", "true")
    assert choose(border, "button", "press", "Tamam")["state"] == "closed"

    table_path = base / f"kurgu-tablo-ozellikleri-{stamp}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(table_path))
    with zipfile.ZipFile(table_path) as archive:
        saved = ElementTree.fromstring(archive.read("content.xml"))
    native_table = saved.find(".//table")
    assert native_table.get("columnSpans").split(",")[0] == "80"
    assert native_table.get("height_min") == "4.0"
    assert native_table.get("border") == "borderNone"
    assert native_table.find("row").get("height_min") == "1.25"

    for action, cell_counts in (
        ("remove-tablerow", [3, 3]),
        ("remove-tablecolumn", [2, 2]),
        ("remove-tablecell", [1, 2]),
        ("remove-table", []),
    ):
        tool("uyap_run_action", documentId=document, action="caret-begin")
        assert fields(action)["state"] == "completed"
        deleted = base / f"kurgu-{action}-{stamp}.udf"
        tool("uyap_save_copy", documentId=document, outputPath=str(deleted))
        with zipfile.ZipFile(deleted) as archive:
            updated = ElementTree.fromstring(archive.read("content.xml"))
        assert [len(row.findall("cell")) for row in updated.findall(".//table/row")] == cell_counts
    tool("uyap_close", documentId=document)

    return {"localFormsPassed": True, "localFormsUdf": str(path), "tablePropertiesUdf": str(table_path)}
