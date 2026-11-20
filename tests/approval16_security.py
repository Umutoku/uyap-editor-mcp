import hashlib
import uuid
import zipfile
from xml.etree import ElementTree


def approval_cases(suite):
    return {
        "certificate-owned-validate": lambda action: certificate_review(suite, action),
        "document-export-signedddoc": lambda action: signed_export_review(suite, action),
        "Set Sign Libary": lambda action: library_review(suite, action),
        "sign": lambda action: single_signing(suite, action, "sign"),
        "Mobile-Sign": lambda action: single_signing(suite, action, "mobile_sign"),
        "sign-batch": lambda action: batch_signing(suite, action),
    }


def request(suite, action, documents, **kwargs):
    return suite.tool("uyap_prepare_approval", request={"action": action, "documents": documents, **kwargs})


def artifact(suite, action):
    return suite.base / f"kurgu-{action}-{uuid.uuid4().hex[:8]}.udf"


def check_artifact(item, path, marker):
    data = path.read_bytes()
    assert item["path"] == str(path) and item["format"] == "udf", item
    assert item["bytes"] == len(data) and item["sha256"] == hashlib.sha256(data).hexdigest().upper(), item
    with zipfile.ZipFile(path) as archive:
        assert "sign.sgn" not in archive.namelist()
        content = ElementTree.fromstring(archive.read("content.xml"))
        assert marker in "".join(content.itertext()), content


def certificate_review(suite, action):
    with suite.opened("KURGU SERTIFIKA INCELEMESI"):
        review = request(suite, action, [{"documentId": suite.document}])
        assert review["status"] == "blocked_prerequisite", review
        assert "smart_card_and_certificate" in review["missing"] and review["artifacts"] == [], review
        assert review["workflow"]["editorCommand"] == "Sertifikalar"
        assert suite.call("inspect_form")["state"] == "closed"
    return {"status": review["status"], "missing": review["missing"], "workflow": review["workflow"]["workflow"]}


def signed_export_review(suite, action):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    target = suite.base / f"kurgu-imz-{uuid.uuid4().hex[:8]}.imz"
    before = source.read_bytes()
    with suite.reopened(source):
        review = request(suite, action, [{"documentId": suite.document, "outputPath": str(target)}])
        assert review["status"] == "blocked_prerequisite", review
        assert review["missing"] == ["signed_udf_required"] and review["artifacts"] == [], review
        assert review["workflow"]["editorCommand"] == "Dışa Aktar"
        assert suite.call("inspect_form")["state"] == "closed"
    assert not target.exists() and source.read_bytes() == before
    return {"status": review["status"], "missing": review["missing"], "imzCreated": False}


def library_review(suite, action):
    with suite.opened("KURGU KUTUPHANE INCELEMESI"):
        document = [{"documentId": suite.document}]
        missing = request(suite, action, document)
        assert missing["status"] == "input_required" and missing["missing"] == ["library_mode"], missing
        review = request(suite, action, document, libraryMode="automatic")
        assert review["status"] == "awaiting_user_confirmation" and review["missing"] == [], review
        assert review["artifacts"] == []
        details = {item["label"]: item["value"] for item in review["details"]}
        assert details == {"Mevcut seçim": "Otomatik", "Önerilen seçim": "Otomatik"}, review
        assert suite.call("inspect_form")["state"] == "closed"
        assert suite.read_text() == "KURGU KUTUPHANE INCELEMESI"
    return {"status": review["status"], "current": details["Mevcut seçim"], "proposed": details["Önerilen seçim"]}


def single_signing(suite, action, workflow):
    marker = "GÜNCEL KURGU İMZA METNİ"
    output = artifact(suite, workflow)
    with suite.opened("KURGU İMZA METNİ"):
        suite.call("replace_text", start=0, end=0, text="GÜNCEL ")
        review = request(suite, action, [{"documentId": suite.document, "outputPath": str(output)}])
        assert review["status"] == "awaiting_user_confirmation", review
        assert review["workflow"]["workflow"] == workflow and review["missing"] == [], review
        (item,) = review["artifacts"]
        check_artifact(item, output, marker)
        assert suite.read_text() == marker and suite.call("inspect_form")["state"] == "closed"
        suite.call("close", expected_error="UNSAVED_CHANGES")
    return {"status": review["status"], "workflow": workflow, "sha256": item["sha256"], "signed": False}


def batch_signing(suite, action):
    markers = ("BİRİNCİ KURGU İMZA BELGESİ", "İKİNCİ KURGU İMZA BELGESİ")
    outputs = [artifact(suite, "imza-toplu") for _ in markers]
    with suite.opened(markers[0]):
        first = suite.document
        second = suite.tool("uyap_new")["documentId"]
        try:
            suite.tool("uyap_replace_text", documentId=second, start=0, end=0, text=markers[1])
            documents = [{"documentId": identity, "outputPath": str(path)}
                         for identity, path in zip((first, second), outputs)]
            review = request(suite, action, documents)
            assert review["status"] == "awaiting_user_confirmation", review
            assert review["workflow"]["workflow"] == "batch_sign" and review["missing"] == [], review
            assert len(review["artifacts"]) == 2, review
            for item, path, marker in zip(review["artifacts"], outputs, markers):
                check_artifact(item, path, marker)
            for doc in (first, second):
                suite.tool("uyap_close", expected_error="UNSAVED_CHANGES", documentId=doc)
                assert suite.tool("uyap_inspect_form", documentId=doc)["state"] == "closed"
        finally:
            close_path = artifact(suite, "imza-toplu-temizlik")
            suite.tool("uyap_save_copy", documentId=second, outputPath=str(close_path))
            suite.tool("uyap_close", documentId=second)
    return {"status": review["status"], "documents": len(review["artifacts"]),
            "hashes": [item["sha256"] for item in review["artifacts"]], "signed": False}
