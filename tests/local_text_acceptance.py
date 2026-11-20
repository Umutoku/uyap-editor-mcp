import json
import shutil
import uuid
import zipfile
from xml.etree import ElementTree


def accept_local_text(tool, base, input_root):
    body = "Birinci bölüm. İkinci bölüm."
    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=body)
    assert tool("uyap_select_text", documentId=document, start=15, end=21) == {"start": 15, "end": 21}
    tool("uyap_select_text", documentId=document, start=21, end=15, expected_error="TEXT_RANGE_INVALID")
    tool("uyap_select_text", documentId=document, start=0, end=1)
    action = tool("uyap_run_action", documentId=document, action="font-bold")
    assert (action["selectionStart"], action["selectionEnd"]) == (0, 1)

    first = tool("uyap_add_comment", documentId=document, start=0, end=6, text="İlk kurgu yorum")
    second = tool("uyap_add_comment", documentId=document, start=15, end=21, text="İkinci kurgu yorum", type=2)

    def change(note, operation, expected_error=None):
        return tool("uyap_change_comment", documentId=document, index=note["index"],
                    expectedStart=note["start"], expectedEnd=note["end"], expectedType=note["type"],
                    expectedText=note["text"], operation=operation, expected_error=expected_error)

    toggled = change(first, "toggle_type")
    assert toggled["type"] == 2 and not toggled["deleted"] and toggled["total"] == 2
    change(first, "toggle_type", expected_error="COMMENT_STALE")
    deleted = change(second, "delete")
    assert deleted["deleted"] and deleted["total"] == 1
    remaining = dict(first, type=2)
    assert tool("uyap_list_comments", documentId=document)["comments"] == [remaining]

    path = base / f"kurgu-yorum-tur-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        comments = ElementTree.fromstring(archive.read("comments.xml"))
        assert [(note.get("strtOffs"), note.get("endOffs"), note.get("type"))
                for note in comments.findall(".//comment")] == [("0", "6", "2")]
        assert "İlk kurgu yorum" in comments.findtext("content")
        assert "İkinci kurgu yorum" not in comments.findtext("content")

    copy = shutil.copyfile(path, input_root / path.name)
    document = tool("uyap_open", path=str(copy), editable=True)["documentId"]
    assert tool("uyap_list_comments", documentId=document)["comments"] == [remaining]
    tool("uyap_add_comment", documentId=document, start=15, end=21, text="Toplu silinecek yorum")
    tool("uyap_run_action", documentId=document, action="comment-delete-all")
    assert tool("uyap_list_comments", documentId=document)["total"] == 0
    empty = base / f"kurgu-yorum-silinmis-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(empty))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(empty) as archive:
        assert "comments.xml" not in archive.namelist()
        assert ElementTree.fromstring(archive.read("content.xml")).findtext("content").strip() == body

    print(json.dumps({"localTextPassed": True, "commentTypeAndDeletionUdf": str(path)}), flush=True)
    accept_search_replace(tool, base)


def accept_search_replace(tool, base):
    document = tool("uyap_new")["documentId"]
    original = "Deneme önce. Deneme orta. Deneme son."
    changed = "Kurgu önce. Kurgu orta. Kurgu son."
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=original)
    tool("uyap_select_text", documentId=document, start=0, end=0)

    def field(form, index):
        return [control for control in form["controls"] if control["role"] == "text"][index]["id"]

    def press(form, label):
        button = next(item for item in form["controls"] if item["role"] == "button" and item["label"] == label)
        return tool("uyap_form_input", documentId=document, operation="press", revision=form["revision"],
                    controlId=button["id"])

    def write(form, index, text):
        return tool("uyap_form_input", documentId=document, operation="write", revision=form["revision"],
                    controlId=field(form, index), value=text)

    find = tool("uyap_open_form", documentId=document, action="find")
    assert find["title"] == "Bul ve Değiştir"
    find = press(write(find, 0, "Deneme"), "Sonrakini Bul")
    assert press(find, "Kapat")["state"] == "closed"
    replace = tool("uyap_open_form", documentId=document, action="replace")
    replace = write(write(replace, 0, "Deneme"), 1, "Kurgu")
    replace = press(replace, "Sonrakini Bul")
    replace = press(replace, "Değiştir")
    assert replace["title"] == "Bul ve Değiştir"
    partial = tool("uyap_read_text", documentId=document)["text"]
    assert partial.count("Kurgu") == 1 and partial.count("Deneme") == 2
    result = press(replace, "Hepsini Değiştir")
    assert result["title"] == "Bilgi"
    assert press(press(result, "Tamam"), "Kapat")["state"] == "closed"
    assert tool("uyap_read_text", documentId=document)["text"] == changed
    path = base / f"kurgu-bul-degistir-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        assert ElementTree.fromstring(archive.read("content.xml")).findtext("content").strip() == changed
    print(json.dumps({"nativeFindAndReplacePassed": True}), flush=True)


def accept_comparison(tool, base, input_root):
    old = "Birinci satır.\nEski kelime.\nSon satır.\n"
    new = "Birinci satır.\nYeni kelime.\nSon satır.\n"
    stamp = uuid.uuid4().hex[:8]

    def create(content, path):
        document = tool("uyap_new")["documentId"]
        tool("uyap_replace_text", documentId=document, start=0, end=0, text=content)
        tool("uyap_save_copy", documentId=document, outputPath=str(path))
        return document

    baseline = base / f"kurgu-eski-{stamp}.udf"
    current = base / f"kurgu-yeni-{stamp}.udf"
    tool("uyap_close", documentId=create(old, baseline))
    document = create(new, current)
    reference = {"documentId": document, "baselinePath": str(baseline)}
    comparison = tool("uyap_compare_udf", **reference, limit=5)
    assert comparison["decision"] == "inspect" and comparison["total"] >= 2
    assert comparison.get("output") is None
    assert {"DELETE", "INSERT"} <= {item["kind"] for item in comparison["changes"]}
    page = tool("uyap_compare_udf", **reference, limit=1)
    assert page["total"] == comparison["total"] and len(page["changes"]) == 1
    for decision, expected in (("accept_all", new), ("reject_all", old)):
        path = base / f"kurgu-{decision}-{stamp}.udf"
        disposition = tool("uyap_compare_udf", **reference, decision=decision, outputPath=str(path))
        assert disposition["total"] == comparison["total"] and disposition["output"]["path"] == str(path)
        with zipfile.ZipFile(path) as archive:
            assert ElementTree.fromstring(archive.read("content.xml")).findtext("content").strip() == expected.strip()
        copied = shutil.copyfile(path, input_root / path.name)
        reopened = tool("uyap_open", path=str(copied))["documentId"]
        assert tool("uyap_read_text", documentId=reopened)["text"].strip() == expected.strip()
        tool("uyap_close", documentId=reopened)
        assert tool("uyap_read_text", documentId=document)["text"].strip() == new.strip()
    identical = tool("uyap_compare_udf", documentId=document, baselinePath=str(current))
    assert identical["total"] == 0 and identical["changes"] == []
    tool("uyap_close", documentId=document)
    print(json.dumps({"nativeVersionComparisonPassed": True}), flush=True)
