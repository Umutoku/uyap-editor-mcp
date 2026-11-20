import hashlib
import json
import uuid
import zipfile
from xml.etree import ElementTree


def accept_individual_diff(tool, base, input_root):
    baseline_text = "Kurgu başlangıç.\nBirinci alan KARA bitiş.\nSabit ara.\nİkinci alan MAVİ bitiş.\nKurgu son.\n"
    current_text = baseline_text.replace("KARA", "PEMBE").replace("MAVİ", "TURUNCU")
    stamp = uuid.uuid4().hex[:8]

    def create(text, suffix):
        path = base / f"kurgu-tekil-fark-{suffix}-{stamp}.udf"
        document = tool("uyap_new")["documentId"]
        tool("uyap_replace_text", documentId=document, start=0, end=0, text=text)
        tool("uyap_save_copy", documentId=document, outputPath=str(path))
        return document, path

    baseline_document, baseline = create(baseline_text, "eski")
    tool("uyap_close", documentId=baseline_document)
    altered_document, altered = create(baseline_text.replace("Sabit ara.", "Başka bir ara."), "eski-degisti")
    tool("uyap_close", documentId=altered_document)
    document, current = create(current_text, "yeni")
    originals = {path: hashlib.sha256(path.read_bytes()).digest() for path in (baseline, current)}

    arguments = dict(documentId=document, baselinePath=str(baseline))
    preview = tool("uyap_compare_udf", **arguments, limit=50)
    second_page = tool("uyap_compare_udf", **arguments, offset=2, limit=1)
    assert second_page["total"] == preview["total"] and second_page["offset"] == 2
    assert second_page["nextOffset"] == 3 and second_page["changes"] == preview["changes"][2:3]
    tool("uyap_compare_udf", **arguments, offset=-1, expected_error="COMPARISON_OFFSET_INVALID")
    group_id = next(change["changeId"] for change in preview["changes"]
                    if change["kind"] == "INSERT" and "PEMBE" in change["text"])
    chosen = [change for change in preview["changes"] if change["changeId"] == group_id]
    assert {change["kind"] for change in chosen} == {"INSERT", "DELETE"}, preview
    assert all(change["groupStart"] == min(item["start"] for item in chosen)
               and change["groupEnd"] == max(item["end"] for item in chosen) for change in chosen)
    assert len({change["changeId"] for change in preview["changes"]}) > 1, preview

    stale = base / f"kurgu-tekil-fark-bayat-{stamp}.udf"
    tool("uyap_compare_udf", documentId=document, baselinePath=str(altered), decision="accept_one",
         changeId=group_id, outputPath=str(stale), expected_error="COMPARISON_CHANGE_STALE")
    assert not stale.exists()

    outputs = {}
    for decision, expected in (("accept_one", baseline_text.replace("KARA", "PEMBE")),
                               ("reject_one", current_text.replace("PEMBE", "KARA"))):
        target = base / f"kurgu-tekil-fark-{decision}-{stamp}.udf"
        resolution = tool("uyap_compare_udf", **arguments, changeId=group_id, decision=decision,
                          outputPath=str(target))
        assert resolution["decision"] == decision and resolution["output"]["path"] == str(target)
        assert resolution["otherGroupsFrom"] == ("baseline" if decision == "accept_one" else "current")
        with zipfile.ZipFile(target) as archive:
            content = ElementTree.fromstring(archive.read("content.xml")).findtext("content")
            assert content.strip() == expected.strip(), content
        reopened = tool("uyap_open", path=str(target))["documentId"]
        assert tool("uyap_read_text", documentId=reopened)["text"].strip() == expected.strip()
        tool("uyap_close", documentId=reopened)
        assert tool("uyap_read_text", documentId=document)["text"].strip() == current_text.strip()
        outputs[decision] = str(target)
    assert all(hashlib.sha256(path.read_bytes()).digest() == original for path, original in originals.items())
    tool("uyap_close", documentId=document)
    print(json.dumps(dict(individualDiffPassed=True, **outputs)), flush=True)
