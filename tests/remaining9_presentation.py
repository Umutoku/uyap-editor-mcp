import json
import uuid

from pypdf import PdfReader


def remaining_cases(suite):
    return {
        "fullscreen": lambda action: fit_page(suite, action),
        "insert Tl symbol": lambda action: insert_lira(suite, action),
        "beep": lambda action: playback_feedback(suite, action),
    }


def fit_page(suite, action):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    with suite.opened(path=source):
        assert suite.call("go_to_page", page=3)["page"] == 3
        assert suite.call("set_view", zoomPercent=200)["zoomPercent"] == 200
        before = suite.call("view_state")
        response = suite.act(action)
        geometry = json.loads(response["diagnostic"])
        after = suite.call("view_state")
        assert geometry["page"] == suite.call("get_page")["page"] == 3, geometry
        assert after["zoomPercent"] == geometry["zoomPercent"] < before["zoomPercent"], (before, geometry, after)
        assert after["renderScale"] < before["renderScale"]
        assert (after["selectionStart"], after["selectionEnd"]) == (before["selectionStart"], before["selectionEnd"])
        assert geometry["pageWidth"] <= geometry["viewportWidth"] and geometry["pageHeight"] <= geometry["viewportHeight"], geometry
        assert geometry["viewportTop"] <= geometry["pageTop"] < geometry["pageBottom"] <= (
            geometry["viewportTop"] + geometry["viewportHeight"]
        ), geometry
        suite.act("resetfullscreen")
        assert suite.call("view_state")["zoomPercent"] == 100
    return geometry


def insert_lira(suite, action):
    source = "KURGU TUTAR 127 TL!"
    expected = "KURGU TUTAR 127 ₺!"
    symbol_offset = source.index("TL")
    with suite.opened(text=source):
        suite.select(symbol_offset, symbol_offset + 2)
        result = suite.act(action)
        assert result["selectionStart"] == result["selectionEnd"] == symbol_offset + 1, result
        assert suite.read_text() == expected
        saved = suite.save("tl-simgesi")
        assert expected in suite.read_udf(saved).findtext("content")
    with suite.opened(path=saved):
        assert suite.read_text() == expected
        pdf = suite.base / f"kurgu-tl-{uuid.uuid4().hex[:8]}.pdf"
        assert suite.call("export_pdf", outputPath=str(pdf))["pageCount"] == 1
        page = PdfReader(pdf, strict=True).pages[0]
        rendered = page.extract_text()
        assert expected in rendered, rendered
        assert "/Identity-H" in font_encodings(page["/Resources"])
        suite.call("replace_text", start=symbol_offset, end=symbol_offset + 1, text="TL")
        assert suite.read_text() == source
        later_pdf = suite.base / f"kurgu-sonraki-pdf-{uuid.uuid4().hex[:8]}.pdf"
        assert suite.call("export_pdf", outputPath=str(later_pdf))["pageCount"] == 1
        later = PdfReader(later_pdf, strict=True).pages[0]
        assert source in later.extract_text(), later.extract_text()
        assert "/Identity-H" not in font_encodings(later["/Resources"])
    return {"unicode": "U+20BA", "reopened": expected, "nativePdf": str(pdf), "fontMappingRestored": True}


def font_encodings(resources):
    encodings = {str(font.get_object().get("/Encoding")) for font in resources.get("/Font", {}).values()}
    for ref in resources.get("/XObject", {}).values():
        nested = ref.get_object().get("/Resources")
        if nested is not None:
            encodings.update(font_encodings(nested))
    return encodings


def playback_feedback(suite, action):
    with suite.opened(text="KURGU SES BELGESI"):
        suite.select(2, 4)
        response = suite.act(action)
        assert (response["selectionStart"], response["selectionEnd"]) == (2, 4), response
        assert suite.read_text() == "KURGU SES BELGESI"
        playback = json.loads(response["diagnostic"])
        assert playback["backend"] == "Java Sound Clip", playback
        assert playback["sampleRate"] == 44100 and playback["framesSubmitted"] == 5292, playback
        assert playback["framesPlayed"] >= playback["framesSubmitted"], playback
    return playback
