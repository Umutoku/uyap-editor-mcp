import json
import uuid
import zipfile
from xml.etree import ElementTree


def accept_automatic_text(tool, base, input_root):
    stamp = uuid.uuid4().hex[:8]
    rich_text = "KURGU ZENGİN METİN"
    source_id = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=source_id, start=0, end=0, text=rich_text)
    tool("uyap_select_text", documentId=source_id, start=0, end=len(rich_text))
    tool("uyap_run_action", documentId=source_id, action="font-bold")
    rich_path = base / f"kurgu-otomatik-zengin-{stamp}.udf"
    tool("uyap_save_copy", documentId=source_id, outputPath=str(rich_path))
    tool("uyap_close", documentId=source_id)

    doc = tool("uyap_new")["documentId"]
    assert tool("uyap_list_automatic_text", documentId=doc)["entries"] == []
    tool("uyap_set_automatic_text", documentId=doc, key="../kurgu", text="Geçersiz",
         expected_error="AUTO_TEXT_KEY_INVALID")
    plain = tool("uyap_set_automatic_text", documentId=doc, key="kurgu", text="İlk düz metin")["entries"]
    assert plain == [{"key": "kurgu", "kind": "plain", "text": "İlk düz metin"}]
    replacement = "Güncellenmiş düz metin\n"
    plain = tool("uyap_set_automatic_text", documentId=doc, key="kurgu", text=replacement)["entries"]
    assert plain[0]["text"] == replacement
    tool("uyap_replace_text", documentId=doc, start=0, end=0, text="Kurgu kod")
    tool("uyap_select_text", documentId=doc, start=0, end=len("Kurgu kod"))
    inserted = tool("uyap_insert_automatic_text", documentId=doc, key="kurgu")
    assert inserted == {"key": "kurgu", "kind": "plain", "start": 0, "end": len(replacement)}
    assert tool("uyap_read_text", documentId=doc)["text"] == replacement

    tool("uyap_set_automatic_text", documentId=doc, key="kurgu", kind="rich", richSourcePath=str(rich_path),
         expected_error="AUTO_TEXT_KEY_CONFLICT")
    rich = tool("uyap_set_automatic_text", documentId=doc, key="zengin", kind="rich",
                richSourcePath=str(rich_path))["entries"]
    assert {"key": "zengin", "kind": "rich", "text": rich_text} in rich
    other = tool("uyap_new")["documentId"]
    assert tool("uyap_list_automatic_text", documentId=other)["entries"] == []
    tool("uyap_close", documentId=other)
    tool("uyap_run_action", documentId=doc, action="caret-end")
    inserted = tool("uyap_insert_automatic_text", documentId=doc, key="zengin", kind="rich")
    assert inserted["start"] == len(replacement) and inserted["end"] >= len(replacement) + len(rich_text)
    assert rich_text in tool("uyap_read_text", documentId=doc)["text"]
    key = "ctrl shift pressed F12"
    canonical = "shift ctrl pressed F12"
    registered = tool("uyap_set_automatic_text", documentId=doc, kind="shortcut", key=key,
                      richSourcePath=str(rich_path))["entries"]
    assert {"key": canonical, "kind": "shortcut", "text": rich_text} in registered
    shortcut_text = "KURGU TUŞ KISAYOLU"
    shortcut_id = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=shortcut_id, start=0, end=0, text=shortcut_text)
    shortcut_path = base / f"kurgu-otomatik-kisayol-{stamp}.udf"
    tool("uyap_save_copy", documentId=shortcut_id, outputPath=str(shortcut_path))
    tool("uyap_close", documentId=shortcut_id)
    registered = tool("uyap_set_automatic_text", documentId=doc, kind="shortcut", key=key,
                      richSourcePath=str(shortcut_path))["entries"]
    assert [entry for entry in registered if entry["kind"] == "shortcut"] == [
        {"key": canonical, "kind": "shortcut", "text": shortcut_text}]
    tool("uyap_run_action", documentId=doc, action="caret-end")
    shortcut = tool("uyap_insert_automatic_text", documentId=doc, kind="shortcut", key=key)
    assert shortcut["key"] == canonical and shortcut["end"] > shortcut["start"]
    assert shortcut_text in tool("uyap_read_text", documentId=doc)["text"]
    tool("uyap_set_automatic_text", documentId=doc, kind="shortcut", key=key, operation="delete")
    tool("uyap_insert_automatic_text", documentId=doc, kind="shortcut", key=key, expected_error="AUTO_TEXT_NOT_FOUND")
    tool("uyap_set_automatic_text", documentId=doc, key="kurgu", operation="delete")
    assert tool("uyap_set_automatic_text", documentId=doc, key="zengin", kind="rich",
                operation="delete")["entries"] == []
    tool("uyap_insert_automatic_text", documentId=doc, key="kurgu", expected_error="AUTO_TEXT_NOT_FOUND")
    tool("uyap_insert_automatic_text", documentId=doc, key="zengin", kind="rich",
         expected_error="AUTO_TEXT_NOT_FOUND")

    result = base / f"kurgu-otomatik-metin-{stamp}.udf"
    tool("uyap_save_copy", documentId=doc, outputPath=str(result))
    tool("uyap_close", documentId=doc)
    with zipfile.ZipFile(result) as archive:
        data = ElementTree.fromstring(archive.read("content.xml"))
    content = data.findtext("content")
    assert replacement.strip() in content and rich_text in content and shortcut_text in content
    formatted = (node for node in data.find("elements").iter("content") if node.get("bold") == "true")
    assert any(rich_text in content[int(node.get("startOffset")):
                                   int(node.get("startOffset")) + int(node.get("length"))]
               for node in formatted)
    reopened = tool("uyap_open", path=str(result))["documentId"]
    assert rich_text in tool("uyap_read_text", documentId=reopened)["text"]
    tool("uyap_close", documentId=reopened)
    print(json.dumps({"automaticTextPassed": True, "automaticTextUdf": str(result)}, ensure_ascii=True), flush=True)
