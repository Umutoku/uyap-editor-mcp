import hashlib
import uuid
import zipfile
from email import policy
from email.parser import BytesParser
from pathlib import Path
from xml.etree import ElementTree

from pypdf import PdfReader


FORMATS = {
    "attachto-mail": ("udf", "application/octet-stream"),
    "attachto-mail-doc": ("doc", "application/msword"),
    "attachto-mail-odt": ("odt", "application/vnd.oasis.opendocument.text"),
    "attachto-mail-pdf": ("pdf", "application/pdf"),
}


def approval_cases(suite):
    return {action: lambda action: mail_approval(suite, action) for action in FORMATS}


def mail_approval(suite, action):
    extension, mime = FORMATS[action]
    token = uuid.uuid4().hex[:8]
    attachment = suite.base / f"İnceleme için kurgu belge {token}.{extension}"
    draft = suite.base / f"kurgu-e-posta-{token}.eml"
    marker = "KURGUEKŞĞİıÇÖÜ"
    recipients = ["kurgu.bir@example.invalid", "kurgu.iki@example.invalid"]
    subject = "Kurgu öneri ve onay içeriği — " * 3 + "ÇŞĞÜÖı"
    message = "Kurgu ileti gövdesi.\nEk bölümde test belgesi bulunuyor. ŞĞİı"
    with suite.opened(marker):
        documents = [{"documentId": suite.document, "outputPath": str(attachment)}]
        valid = {"action": action, "documents": documents, "draftPath": str(draft), "recipients": recipients,
                 "subject": subject, "message": message}

        def request(parameters, expected_error=None):
            return suite.tool("uyap_prepare_approval", expected_error=expected_error, request=parameters)

        absent = request({name: value for name, value in valid.items() if name not in ("recipients", "message")})
        assert absent["status"] == "input_required" and set(absent["missing"]) == {"recipients", "message"}, absent
        assert not absent["artifacts"] and not attachment.exists() and not draft.exists()
        if extension == "udf":
            request({**valid, "recipients": ["kurgu@example.invalid\r\nBcc: other@example.invalid"]},
                    expected_error="MAIL_RECIPIENT_INVALID")
            request({**valid, "subject": "Konu\r\nCc: other@example.invalid"},
                    expected_error="MAIL_SUBJECT_INVALID")
            collision = suite.base / f"kurgu-cakisma-{token}.eml"
            collision.write_bytes(b"existing draft")
            request({**valid, "draftPath": str(collision)}, expected_error="OUTPUT_EXISTS")
            assert collision.read_bytes() == b"existing draft" and not attachment.exists()

        result = request(valid)
        assert result["status"] == "awaiting_user_confirmation" and result["action"] == action, result
        assert not result["missing"] and result["userStep"]
        settings = {item["label"]: item["value"] for item in result["details"]}
        assert settings["Alıcılar"] == ", ".join(recipients) and settings["Konu"] == subject
        assert settings["İleti"] == message and settings["Ek"] == attachment.name
        assert suite.read_text() == marker
        suite.call("close", expected_error="UNSAVED_CHANGES")

    files = {item["format"]: item for item in result["artifacts"]}
    assert set(files) == {extension, "eml"}, files
    for kind, path in ((extension, attachment), ("eml", draft)):
        artifact = files[kind]
        assert Path(artifact["path"]) == path
        data = path.read_bytes()
        assert artifact["bytes"] == len(data) and artifact["sha256"] == hashlib.sha256(data).hexdigest().upper()

    parsed = BytesParser(policy=policy.default).parsebytes(draft.read_bytes())
    assert parsed["X-Unsent"] == "1"
    assert [address.addr_spec for address in parsed["To"].addresses] == recipients
    assert str(parsed["Subject"]) == subject
    assert parsed.get_body(preferencelist=("plain",)).get_content().replace("\r\n", "\n") == message
    attachments = list(parsed.iter_attachments())
    assert len(attachments) == 1 and attachments[0].get_filename() == attachment.name
    assert attachments[0].get_content_type() == mime
    payload = attachment.read_bytes()
    assert attachments[0].get_payload(decode=True) == payload
    assert not parsed.defects and not attachments[0].defects

    if extension in ("udf", "odt"):
        with zipfile.ZipFile(attachment) as package:
            if extension == "odt":
                assert package.read("mimetype").decode() == mime
            assert marker in "".join(ElementTree.fromstring(package.read("content.xml")).itertext())
    elif extension == "doc":
        assert payload.startswith(bytes.fromhex("D0 CF 11 E0 A1 B1 1A E1"))
        assert "WordDocument\0".encode("utf-16le") in payload and marker.encode("utf-16le") in payload
    else:
        assert marker in PdfReader(attachment, strict=True).pages[0].extract_text()
    assert not list(suite.base.glob(".uyap-*"))
    return {"status": result["status"], "draft": str(draft), "format": extension,
            "attachment": str(attachment), "checkedRecipients": len(recipients), "exactAttachmentBytes": True}
