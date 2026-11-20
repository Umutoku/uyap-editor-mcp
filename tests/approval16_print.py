import hashlib
import uuid
from pathlib import Path

from pypdf import PdfReader


PRINTER_INTENT = "KURGU YAZICI"


def approval_cases(suite):
    return {"document-print": lambda action: single_print_approval(suite, action),
            "print-batch": lambda action: batch_print_approval(suite, action)}


def check_preview(artifact, target, page_count, marker):
    assert Path(artifact["path"]) == target and artifact["format"] == "pdf", artifact
    data = target.read_bytes()
    assert artifact["bytes"] == len(data) and artifact["sha256"] == hashlib.sha256(data).hexdigest().upper()
    preview = PdfReader(target, strict=True)
    assert len(preview.pages) == page_count and marker in preview.pages[0].extract_text()


def single_print_approval(suite, action):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    before = source.read_bytes()
    marker = "KURGU YAZDIRMA ONIZLEME"
    target = suite.base / f"kurgu-onay-yazdirma-{uuid.uuid4().hex[:8]}.pdf"
    with suite.opened(path=source):
        suite.call("replace_text", start=0, end=0, text=marker + " ")
        request = {"action": action, "documents": [{"documentId": suite.document, "outputPath": str(target)}]}
        missing = suite.tool("uyap_prepare_approval", request=request)
        assert missing["status"] == "input_required", missing
        assert set(missing["missing"]) == {"printer", "copies", "pageRange"}, missing
        assert not missing["artifacts"] and not target.exists(), missing
        request.update(printer=PRINTER_INTENT, copies=2, pageRange="2,1")
        suite.tool("uyap_prepare_approval", expected_error="PRINT_PAGE_RANGE_INVALID", request=request)
        assert not target.exists()
        request["pageRange"] = "1, 2-3"
        result = suite.tool("uyap_prepare_approval", request=request)
        assert result["status"] == "awaiting_user_confirmation" and result["action"] == action, result
        assert result["workflow"]["editorCommand"] == "Yazdır" and result["workflow"]["enabled"], result
        assert not result["missing"] and len(result["artifacts"]) == 1, result
        assert "yazdırma" in result["userStep"].lower() and "gönderin" in result["userStep"], result
        details = {item["label"]: item["value"] for item in result["details"]}
        assert [details[name] for name in ("Yazıcı", "Kopya", "Sayfalar", "Belge 1 sayfa sayısı")] == [
            PRINTER_INTENT, "2", "1,2-3", "3"], details
        assert "Kullanıcının girdiği" in details["Yazıcı doğrulaması"], details
        check_preview(result["artifacts"][0], target, 3, marker)
        assert source.read_bytes() == before
        suite.call("close", expected_error="UNSAVED_CHANGES")
        assert marker in suite.call("read_text")["text"]
        assert suite.call("inspect_form")["state"] == "closed"
    return {"status": result["status"], "previewPages": 3, "copies": 2, "pageRange": "1,2-3",
            "nativePrintCommand": result["workflow"]["editorCommand"], "userStep": result["userStep"]}


def batch_print_approval(suite, action):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    before = source.read_bytes()
    second_marker = "KURGU IKINCI YAZDIRMA BELGESI"
    with suite.opened(second_marker):
        second = suite.save("onay-toplu-yazdirma")
    targets = [suite.base / f"kurgu-onay-toplu-{index}-{uuid.uuid4().hex[:8]}.pdf" for index in (1, 2)]
    with suite.opened(path=source):
        second_id = suite.tool("uyap_open", path=str(second))["documentId"]
        try:
            documents = [{"documentId": doc, "outputPath": str(target)}
                         for doc, target in zip((suite.document, second_id), targets)]
            request = {"action": action, "documents": documents, "printer": PRINTER_INTENT, "copies": 1,
                       "pageRange": "2"}
            prior_staging = set(suite.base.glob(".uyap-*"))
            suite.tool("uyap_prepare_approval", expected_error="PRINT_PAGE_RANGE_OUT_OF_BOUNDS", request=request)
            assert not any(path.exists() for path in targets)
            assert set(suite.base.glob(".uyap-*")) == prior_staging
            request["pageRange"] = "all"
            result = suite.tool("uyap_prepare_approval", request=request)
            assert result["status"] == "awaiting_user_confirmation" and result["action"] == action, result
            assert result["workflow"]["editorCommand"] == "Toplu Yazdır" and result["workflow"]["enabled"], result
            assert not result["missing"] and len(result["artifacts"]) == 2, result
            details = {item["label"]: item["value"] for item in result["details"]}
            assert [details[name] for name in ("Yazıcı", "Kopya", "Sayfalar", "Belge 1 sayfa sayısı",
                                               "Belge 2 sayfa sayısı")] == [
                PRINTER_INTENT, "1", "all", "3", "1"], details
            check_preview(result["artifacts"][0], targets[0], 3, "SATIR 001")
            check_preview(result["artifacts"][1], targets[1], 1, second_marker)
            assert source.read_bytes() == before
            assert set(suite.base.glob(".uyap-*")) == prior_staging
            assert suite.call("inspect_form")["state"] == "closed"
        finally:
            suite.tool("uyap_close", documentId=second_id)
    return {"status": result["status"], "previewPagesInOrder": [3, 1], "pageRange": "all",
            "nativePrintCommand": result["workflow"]["editorCommand"], "userStep": result["userStep"]}
