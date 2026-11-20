import hashlib
import json
import uuid

from pypdf import PdfReader

from open_actions_acceptance import OpenActions


def accept_dirty_pdf(tool, base, source):
    original = hashlib.sha256(source.read_bytes()).hexdigest()
    suite = OpenActions(tool, base, source.parent)
    before = "SATIR 001"
    inserted = "CANLI 001 "
    with suite.opened(path=source):
        def render(label):
            path = base / f"kurgu-pdf-{label}-{uuid.uuid4().hex[:8]}.pdf"
            output = suite.call("export_pdf", outputPath=str(path))
            assert output["sourceUnchanged"] and output["pageCount"] == 3, output
            text = "".join(page.extract_text() for page in PdfReader(path, strict=True).pages)
            return output["editorCommand"], text

        _, initial = render("ilk")
        assert before in initial and inserted not in initial
        position = suite.call("read_text", limit=300)["text"].index(before)
        suite.call("replace_text", start=position, end=position, text=inserted)
        assert inserted + before in suite.call("read_text", limit=300)["text"]
        changed_route, changed = render("canli")
        assert inserted + before in changed, (changed_route, changed[:140])
        suite.call("close", expected_error="UNSAVED_CHANGES")
        suite.act("undo")
        assert before in suite.call("read_text", limit=300)["text"]
        undo_route, undone = render("geri-al")
        assert before in undone and inserted not in undone, (undo_route, undone[:140])
        assert any(route.startswith("ribbon:") for route in (changed_route, undo_route)), (changed_route, undo_route)
    assert hashlib.sha256(source.read_bytes()).hexdigest() == original
    print(json.dumps({"dirtyPdfRegression": "passed", "changedRoute": changed_route, "undoRoute": undo_route,
                      "sourceUnchanged": True}, ensure_ascii=True), flush=True)
