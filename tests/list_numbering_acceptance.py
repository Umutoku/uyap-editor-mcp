import json
import uuid
import zipfile
from xml.etree import ElementTree


def verify_native_counter(tool, document, expected):
    form = tool("uyap_open_form", documentId=document, action="changeListNo-action")
    assert form["title"] == "Numaralandırma Değeri Değiştirme", form
    fields = [control["value"] for control in form["controls"] if control["role"] == "text"]
    assert fields == [str(expected)], form
    tool("uyap_form_input", documentId=document, revision=form["revision"],
         controlId=-1, operation="cancel")


def accept_list_numbering(tool, base):
    lines = (
        "Kurgu liste düzeyi",
        "Üst kalem",
        "Alt kalem",
        "Alt izleyen",
        "Kurgu liste sayacı",
        "Sayaç ilk",
        "Sayaç ikinci",
        "Sayaç üçüncü",
        "Kurgu sıfırlama",
        "Varsayılan kalem",
        "Kurgu madde imi",
        "Elmas kalem",
    )
    text = "\n".join(lines)
    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)

    def select(first, last=None):
        start = text.index(first)
        end = text.index(last or first) + len(last or first)
        tool("uyap_select_text", documentId=document, start=start, end=end)

    def read_item():
        return tool("uyap_get_list_item", documentId=document)

    select(lines[0])
    assert read_item().get("level") is None
    tool("uyap_set_list_level", documentId=document, level=2, expected_error="LIST_ITEM_REQUIRED")
    tool("uyap_set_numbering_start", documentId=document, startAt=7,
         expected_error="NUMBERED_LIST_ITEM_REQUIRED")
    select("Üst kalem", "Alt izleyen")
    tool("uyap_set_list_palette", documentId=document, preset="number_outline")
    tool("uyap_set_list_level", documentId=document, level=2, expected_error="LIST_SINGLE_ITEM_REQUIRED")
    select("Alt kalem")
    assert read_item()["level"] == 1
    for level in (2, 3, 4, 5, 6, 1, 3):
        assert tool("uyap_set_list_level", documentId=document, level=level)["level"] == level
    tool("uyap_set_list_level", documentId=document, level=7, expected_error="LIST_LEVEL_INVALID")
    select("Alt izleyen")
    assert tool("uyap_set_list_level", documentId=document, level=3)["level"] == 3
    select("Alt kalem")
    assert tool("uyap_set_numbering_start", documentId=document, startAt=5)["explicitStart"] == 5

    select("Sayaç ilk", "Sayaç üçüncü")
    tool("uyap_set_list_palette", documentId=document, preset="number_dot")
    select("Sayaç ikinci")
    tool("uyap_set_numbering_start", documentId=document, startAt=0,
         expected_error="NUMBERING_START_INVALID")
    assert tool("uyap_set_numbering_start", documentId=document, startAt=7)["explicitStart"] == 7

    select("Varsayılan kalem")
    tool("uyap_set_list_palette", documentId=document, preset="number_dot")
    assert tool("uyap_set_numbering_start", documentId=document, startAt=9)["explicitStart"] == 9
    assert tool("uyap_set_numbering_start", documentId=document, startAt=1)["explicitStart"] == 1

    select("Elmas kalem")
    tool("uyap_set_list_palette", documentId=document, preset="diamond")
    bullet = tool("uyap_set_list_level", documentId=document, level=2)
    assert bullet["level"] == 2 and not bullet["numbered"]
    tool("uyap_set_numbering_start", documentId=document, startAt=7,
         expected_error="NUMBERED_LIST_ITEM_REQUIRED")

    path = base / f"kurgu-liste-duzeyi-ve-baslangic-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    assert markup.findtext("content").strip() == text
    stored = dict(zip(lines, markup.findall("./elements/paragraph"), strict=True))
    assert stored["Üst kalem"].get("ListLevel") == "1"
    assert stored["Alt kalem"].get("ListLevel") == "3"
    assert stored["Alt kalem"].get("NumberSetted") == "4"
    assert stored["Sayaç ikinci"].get("NumberSetted") == "6"

    document = tool("uyap_open", path=str(path), editable=True)["documentId"]
    assert tool("uyap_read_text", documentId=document)["text"].strip() == text
    select("Üst kalem")
    assert read_item()["level"] == 1
    select("Alt kalem")
    assert read_item()["level"] == 3
    assert read_item()["explicitStart"] == 5
    select("Elmas kalem")
    bullet = read_item()
    assert bullet["level"] == 2 and not bullet["numbered"]
    counters = {"Alt kalem": 5, "Alt izleyen": 6, "Sayaç ilk": 1, "Sayaç ikinci": 7,
                "Sayaç üçüncü": 8, "Varsayılan kalem": 1}
    for title, expected in counters.items():
        select(title)
        verify_native_counter(tool, document, expected)
    tool("uyap_close", documentId=document)
    print(json.dumps({"acceptance": "list_numbering", "status": "passed", "udf": str(path),
                      "savedLevels": [1, 3, 2], "reopenedCounters": counters}), flush=True)


def accept_continue_numbering(tool, base):
    lines = (
        "Önceki ilk öğe",
        "Önceki son öğe",
        "Aradaki düz paragraf",
        "Aradaki madde imi",
        "Devam eden ilk öğe",
        "Devam eden son öğe",
    )
    text = "\n".join(lines)
    document = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)

    def select(first, last=None):
        start = text.index(lines[first])
        tail = lines[first if last is None else last]
        tool("uyap_select_text", documentId=document, start=start, end=text.index(tail) + len(tail))

    select(0, 1)
    tool("uyap_set_list_palette", documentId=document, preset="number_dot")
    select(0)
    assert tool("uyap_set_numbering_start", documentId=document, startAt=4)["explicitStart"] == 4
    tool("uyap_continue_numbering", documentId=document, expected_error="PREVIOUS_NUMBERED_LIST_REQUIRED")
    select(2)
    tool("uyap_continue_numbering", documentId=document, expected_error="NUMBERED_LIST_ITEM_REQUIRED")
    select(3)
    tool("uyap_set_list_palette", documentId=document, preset="diamond")
    tool("uyap_continue_numbering", documentId=document, expected_error="NUMBERED_LIST_ITEM_REQUIRED")

    select(4, 5)
    tool("uyap_set_list_palette", documentId=document, preset="number_dot")
    tool("uyap_continue_numbering", documentId=document, expected_error="LIST_SINGLE_ITEM_REQUIRED")
    select(4)
    assert tool("uyap_set_numbering_start", documentId=document, startAt=20)["explicitStart"] == 20
    verify_native_counter(tool, document, 20)
    for _ in range(2):
        continued = tool("uyap_continue_numbering", documentId=document)
        assert continued["numbered"] and continued.get("explicitStart") is None
    verify_native_counter(tool, document, 6)

    path = base / f"kurgu-numaralandirma-devam-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document, outputPath=str(path))
    tool("uyap_close", documentId=document)
    with zipfile.ZipFile(path) as archive:
        markup = ElementTree.fromstring(archive.read("content.xml"))
    assert markup.findtext("content").strip() == text
    paragraphs = markup.findall("./elements/paragraph")
    assert len(paragraphs) == len(lines)
    numbered = [paragraphs[index] for index in (0, 1, 4, 5)]
    list_ids = {paragraph.get("ListId") for paragraph in numbered}
    assert len(list_ids) == 1 and next(iter(list_ids)) not in (None, "0")
    assert numbered[0].get("NumberSetted") == "3"
    assert numbered[2].get("NumberSetted") in (None, "-1")

    document = tool("uyap_open", path=str(path), editable=True)["documentId"]
    assert tool("uyap_read_text", documentId=document)["text"].strip() == text
    counters = {0: 4, 1: 5, 4: 6, 5: 7}
    for index, expected in counters.items():
        select(index)
        verify_native_counter(tool, document, expected)
    for index in (2, 3):
        select(index)
        item = tool("uyap_get_list_item", documentId=document)
        assert not item["numbered"] and item.get("level") == (1 if index == 3 else None)
    tool("uyap_close", documentId=document)
    print(json.dumps({"acceptance": "continue_numbering", "status": "passed", "udf": str(path),
                      "reopenedCounters": list(counters.values())}), flush=True)
