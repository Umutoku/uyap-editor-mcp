import hashlib
import uuid
import zipfile
from pathlib import Path
from xml.etree import ElementTree

from pypdf import PdfReader


ATTACHMENTS = {
    "attachto-mail": "udf",
    "attachto-mail-doc": "doc",
    "attachto-mail-odt": "odt",
    "attachto-mail-pdf": "pdf",
}

HANDOFFS = {
    "document-print": ("print", "Yazdır", "Fiziksel yazıcı, sayfa ve kopya seçimi kullanıcıya ait."),
    "print-batch": ("batch_print", "Toplu Yazdır", "Klasör kapsamı, çıktı sırası ve fiziksel yazıcı kullanıcıya ait."),
    "dss-save": ("portal_save", "Sisteme Kaydet", "Yetkili UYAP oturumu ve portal kayıt bağlamı gerekiyor."),
    "document-export-signedddoc": ("signed_export", "Dışa Aktar",
                                  "Yerel imza dışa aktarıcısı geçerli imza bölümü bulunan bir belge gerektiriyor."),
    "update": ("update", "Editorü Güncelle", "Güncelleme ağ bağlantısı ve kullanıcı kurulum kararı gerektiriyor."),
    "set proxy": ("proxy", "Proxy", "Ağ adresi ve varsa proxy kimlik bilgileri kullanıcı veya yönetici tarafından giriliyor."),
}


def residual_cases(suite):
    candidates = {action: lambda action: prepare_attachment(suite, action) for action in ATTACHMENTS}
    candidates.update({action: lambda action: prepare_workflow(suite, action) for action in HANDOFFS})
    return candidates


def prepare_attachment(suite, action):
    extension = ATTACHMENTS[action]
    marker = "KURGUEKŞĞİıÇÖÜ"
    file = suite.base / f"kurgu-ek-{uuid.uuid4().hex[:8]}.{extension}"
    with suite.opened(marker):
        if extension == "udf":
            mismatch = file.with_suffix(".pdf")
            suite.call("prepare_attachment", action=action, outputPath=str(mismatch),
                       expected_error="ATTACHMENT_FORMAT_MISMATCH")
            assert not mismatch.exists()
        result = suite.call("prepare_attachment", action=action, outputPath=str(file))
        assert result["status"] == "user_action_required" and result["action"] == action
        assert result["format"] == extension and Path(result["path"]) == file
        assert suite.read_text() == marker
        suite.call("close", expected_error="UNSAVED_CHANGES")
        content = file.read_bytes()
        assert len(content) == result["bytes"] and hashlib.sha256(content).hexdigest().upper() == result["sha256"]
        if extension in ("udf", "odt"):
            with zipfile.ZipFile(file) as package:
                if extension == "odt":
                    assert package.read("mimetype") == b"application/vnd.oasis.opendocument.text"
                document = ElementTree.fromstring(package.read("content.xml"))
                assert marker in "".join(document.itertext())
            if extension == "udf":
                with suite.reopened(file):
                    assert suite.read_text() == marker
                    unchanged = suite.base / f"kurgu-ek-degismemis-{uuid.uuid4().hex[:8]}.udf"
                    copied = suite.call("prepare_attachment", action=action, outputPath=str(unchanged))
                    assert copied["editorCommand"] == "copy:managed-udf" and unchanged.read_bytes() == content
        elif extension == "pdf":
            reader = PdfReader(file, strict=True)
            assert len(reader.pages) == 1 and marker in reader.pages[0].extract_text()
        else:
            assert content.startswith(bytes.fromhex("D0 CF 11 E0 A1 B1 1A E1"))
            assert "WordDocument\0".encode("utf-16le") in content
            assert marker.encode("utf-16le") in content
    return {"coverage": "prepared", "artifact": str(file), "format": extension,
            "command": result["editorCommand"], "reason": "E-posta istemcisinde ekleme ve gönderme kullanıcıya ait."}


def prepare_workflow(suite, action):
    workflow, label, reason = HANDOFFS[action]
    source = suite.input_root / "sentetik-3-sayfa.udf"
    with suite.opened(path=source):
        handoff = suite.call("prepare_workflow", workflow=workflow)
        assert handoff["workflow"] == workflow and handoff["editorCommand"] == label, handoff
        assert handoff["status"] == (
            "user_action_required" if handoff["enabled"] else "unavailable_in_current_mode"), handoff
        if action == "dss-save":
            assert not handoff["enabled"], handoff
        else:
            assert handoff["enabled"], handoff
        if action == "document-print":
            form = suite.form("print-preview")
            assert form["title"] == "Yazdırma Önizlemesi", form
            assert suite.input(form, "press", "button", "Kapat")["state"] == "closed"
        if action == "document-export-signedddoc":
            form = suite.form("show-signatures")
            assert any("imza yok" in control["value"].casefold() for control in form["controls"]
                       if control["role"] == "message"), form
            assert suite.confirm(form)["state"] == "closed"
        assert suite.call("get_page")["pageCount"] == 3
    if action != "print-batch":
        return {"coverage": "prepared", "command": handoff["editorCommand"],
                "enabled": handoff["enabled"], "status": handoff["status"], "reason": reason}
    with suite.opened("KURGU TOPLU YAZDIRMA"):
        second = suite.save("toplu-yazdirma")
    first = suite.base / f"kurgu-yazdirma-{uuid.uuid4().hex[:8]}.pdf"
    last = suite.base / f"kurgu-yazdirma-{uuid.uuid4().hex[:8]}.pdf"
    batch = suite.tool("uyap_batch_export", files=[{"path": str(source), "outputPath": str(first)},
                                                  {"path": str(second), "outputPath": str(last)}])
    assert batch["completed"] == 2 and all(item["succeeded"] for item in batch["files"]), batch
    assert len(PdfReader(first, strict=True).pages) == 3
    assert "KURGU TOPLU YAZDIRMA" in PdfReader(last, strict=True).pages[0].extract_text()
    return {"coverage": "prepared", "files": [str(first), str(last)], "reason": reason}
