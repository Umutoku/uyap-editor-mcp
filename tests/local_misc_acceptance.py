import json
import uuid
import zipfile
from xml.etree import ElementTree


def accept_local_misc(tool, base, input_root):
    document_id = tool("uyap_new")["documentId"]

    def call(name, **args):
        return tool(name, documentId=document_id, **args)

    def action(name):
        return call("uyap_run_action", action=name)

    def span(result):
        return result["selectionStart"], result["selectionEnd"]

    original = call("uyap_view_state")
    assert 100 in original["zoomOptions"] and 125 in original["zoomOptions"]
    changed = call("uyap_set_view", zoomPercent=125, showWhitespace=True)
    assert changed["zoomPercent"] == 125 and changed["showWhitespace"]
    action("resetfullscreen")
    assert call("uyap_view_state")["zoomPercent"] == 100
    assert not call("uyap_set_view", showWhitespace=False)["showWhitespace"]
    call("uyap_set_view", expected_error="ZOOM_OPTION_UNKNOWN", zoomPercent=33)

    shortcuts = call("uyap_list_shortcuts", offset=0, limit=100)
    assert shortcuts["total"] > 20
    assert any(row["action"] == "changeCase-switch" and "F3" in row["key"]
               for row in shortcuts["shortcuts"])
    assert call("uyap_list_shortcuts", offset=1, limit=1)["shortcuts"] == shortcuts["shortcuts"][1:2]

    sample = "Merhaba dünya\nİkinci cümle\nSon satır"
    call("uyap_replace_text", start=0, end=0, text=sample)
    assert span(action("caret-begin")) == (0, 0)
    assert span(action("caret-next-word")) == (8, 8)
    assert span(action("select-word")) == (8, 13)
    assert call("uyap_view_state")["selectionEnd"] == 13
    call("uyap_set_font", expected_error="FONT_FAMILY_UNKNOWN", family="KURGU_YAZI_TIPI_YOK")
    font = call("uyap_set_font", family="Arial", size=18)
    assert font == {"start": 8, "end": 13, "family": "Arial", "size": 18}
    assert call("uyap_set_font", size=17)["size"] == 17
    action("changeCase-switch")
    assert call("uyap_read_text")["text"] == sample.replace("dünya", "DÜNYA")
    assert span(action("caret-end-line")) == (13, 13)
    assert span(action("selection-begin-line")) == (0, 13)
    call("uyap_find", query="ÜNY")
    assert span(action("caret-begin-word")) == (8, 8)
    assert span(action("caret-end-word")) == (13, 13)
    assert span(action("caret-previous-word")) == (8, 8)
    assert span(action("select-line")) == (0, 13)
    assert span(action("caret-begin-line")) == (0, 0)
    assert span(action("selection-end-line")) == (0, 13)
    action("caret-begin")
    assert span(action("selection-next-word")) == (0, 8)
    action("caret-end-line")
    assert span(action("selection-previous-word")) == (8, 13)
    call("uyap_find", query="ÜNY")
    assert span(action("selection-begin-word")) == (8, 9)
    call("uyap_find", query="ÜNY")
    assert span(action("selection-end-word")) == (9, 13)

    action("caret-end")
    action("insert Tl symbol")
    assert call("uyap_read_text")["text"].rstrip("\n").endswith(("₺", "¨"))
    path = base / f"kurgu-gorunum-bicim-{uuid.uuid4().hex[:8]}.udf"
    with_indent = path.with_name("girintili-" + path.name)
    call("uyap_find", query="DÜNYA")
    action("increase-firstline-indent")
    call("uyap_save_copy", outputPath=str(with_indent))
    with zipfile.ZipFile(with_indent) as archive:
        root = ElementTree.fromstring(archive.read("content.xml"))
        assert float(root.find(".//elements/paragraph").attrib["FirstLineIndent"]) > 0
    action("decrease-firstline-indent")
    call("uyap_save_copy", outputPath=str(path))
    call("uyap_close")
    with zipfile.ZipFile(path) as archive:
        root = ElementTree.fromstring(archive.read("content.xml"))
        assert "DÜNYA" in root.findtext("content")
        assert any(item.get("family") == "Arial" and item.get("size") == "17" for item in root.iter())
        assert float(root.find(".//elements/paragraph").get("FirstLineIndent", 0)) == 0

    source = input_root / "sentetik-3-sayfa.udf"
    document_id = tool("uyap_open", path=str(source))["documentId"]
    action("caret-begin")
    start, end = span(action("page-down"))
    assert start == end and start > 0
    assert span(action("page-up")) == (0, 0)
    selection = action("selection-page-down")
    assert selection["selectionStart"] == 0 and selection["selectionEnd"] > 0
    assert span(action("selection-page-up")) == (0, 0)
    call("uyap_close")
    print(json.dumps({"localMiscPassed": True}, ensure_ascii=True), flush=True)
