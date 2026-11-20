import uuid

from remaining_svg_acceptance import accept_svg


def word_document(suite, action):
    marker = "KURGUŞĞİıÇÖÜ"
    original = "KURGUESKIMETIN"
    with suite.opened(original):
        source = suite.save("doc-kaynak")
    target = suite.base / f"kurgu-kalan-{uuid.uuid4().hex[:8]}.doc"
    with suite.opened(path=source):
        suite.call("replace_text", start=0, end=len(original), text=marker)
        assert suite.read_text() == marker
        before = set(suite.base.glob(".uyap-*"))
        result = suite.call("export_document", outputPath=str(target))
        assert set(suite.base.glob(".uyap-*")) == before
        assert result["editorCommand"] in ("action:save-doc", "compat:Microsoft Word/DOC"), result
        assert suite.read_text() == marker
    data = target.read_bytes()
    assert data.startswith(bytes.fromhex("D0 CF 11 E0 A1 B1 1A E1"))
    assert "WordDocument\0".encode("utf-16le") in data
    assert marker.encode("utf-16le") in data
    assert original.encode("utf-16le") not in data
    return {"editorCommand": result["editorCommand"], "liveUnicodeText": marker, "binaryDoc": True,
            "wordRoundTrip": result["editorCommand"] == "compat:Microsoft Word/DOC"}


def remaining_cases(suite):
    return {"export-svg": lambda action: accept_svg(suite, action),
            "save-doc": lambda action: word_document(suite, action)}
