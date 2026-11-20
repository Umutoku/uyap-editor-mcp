from list_numbering_acceptance import verify_native_counter
from pypdf import PdfReader
import uuid


FONT_ACTIONS = {
    **{f"font-family-{family}": ("family", family) for family in ("Monospaced", "SansSerif", "Serif")},
    **{f"font-size-{size}": ("size", str(size)) for size in (8, 10, 12, 14, 16, 18, 24, 36, 48)},
}
INDENT_ACTIONS = {
    "decrease-hanging-indent": ("increase-hanging-indent", "Hanging"),
    "decrease-left-indent-step": ("increase-leftIndent", "LeftIndent"),
}
CAPITALIZATION_ACTIONS = {
    "first Letter Upper": "firstLetter",
    "To-Uppercase": "uppercase",
}


def open_action_cases(suite):
    setters = {name: lambda action: check_font(suite, action) for name in FONT_ACTIONS}
    setters.update({name: lambda action: check_indent(suite, action) for name in INDENT_ACTIONS})
    setters.update({name: lambda action: check_capitalization(suite, action) for name in CAPITALIZATION_ACTIONS})
    setters.update({
        "hf-startPage": lambda action: check_page_bounds(suite, action),
        "hf-stopPage": lambda action: check_page_bounds(suite, action),
        "format-brush-clear": lambda action: check_brush(suite, action),
        "startNumbering-action": lambda action: check_restart(suite, action),
    })
    return setters, {}


def check_font(suite, action):
    key, expected = FONT_ACTIONS[action]
    text = "Kurgu"

    def read_style(path):
        root = suite.read_udf(path)
        return root.find("./elements/paragraph/content[@startOffset='0']").attrib

    with suite.opened(text):
        suite.select(0, len(text))
        suite.call("set_font", family="Arial", size=17)
        before = read_style(suite.save("yazitipi-once"))
        assert before["family"] == "Arial" and before["size"] == "17", before
        suite.select(0, len(text))
        suite.act(action)
        path = suite.save(action)
        style = read_style(path)
        assert style.get(key) == expected, style
    with suite.opened(path=path):
        assert suite.read_text() == text
        reopened = read_style(suite.save(action + "-yeniden"))
        assert reopened.get(key) == expected, reopened
    return f"Kaydedilip yeniden açılan UDF'de {key}={expected}."


def check_indent(suite, action):
    increase, key = INDENT_ACTIONS[action]
    text = "Kurgu girinti"

    def read_indent(path):
        paragraph = suite.read_udf(path).find("./elements/paragraph")
        return float(paragraph.get(key, 0))

    with suite.opened(text):
        suite.select(0)
        suite.act(increase)
        before = read_indent(suite.save("girinti-once"))
        assert before > 0, before
        suite.select(0)
        suite.act(action)
        path = suite.save(action)
        after = read_indent(path)
        assert 0 <= after < before, (before, after)
    with suite.opened(path=path):
        assert suite.read_text() == text
        assert read_indent(suite.save(action + "-yeniden")) == after
    return f"Kaydedilip yeniden açılan UDF'de {key} {before:g} değerinden {after:g} değerine indi."


def check_capitalization(suite, action):
    text = "Kurgu harf denetimi"
    option = CAPITALIZATION_ACTIONS[action]
    configured, active = option + "Configured", option + "Active"
    with suite.opened(text):
        initial = suite.call("capitalization_state")
        assert initial[configured] == initial[active], initial
        path = suite.save("harf-denetimi")
        for expected in (not initial[configured], initial[configured]):
            form = suite.form(action)
            messages = [item["value"] for item in form["controls"] if item["role"] == "message"]
            assert any("yeniden başlat" in message.casefold() for message in messages), messages
            assert suite.confirm(form)["state"] == "closed"
            pending = suite.call("capitalization_state")
            assert pending == {**initial, configured: expected}, pending

            reopened = suite.tool("uyap_open", path=str(path),
                                  capitalizationFromDocumentId=suite.document)["documentId"]
            assert reopened != suite.document
            try:
                loaded = suite.tool("uyap_capitalization_state", documentId=reopened)
                assert loaded == {**pending, active: expected}, loaded
                assert suite.tool("uyap_read_text", documentId=reopened)["text"].rstrip("\n") == text
            finally:
                suite.tool("uyap_close", documentId=reopened)
    return "Yeniden başlatma bildirimi görüldü. Kurgu UDF yeni editörde açıldı ve yerel klavye işleyicisi ayara göre etkinleşip kapandı."


def check_page_bounds(suite, action):
    if action == "hf-startPage":
        region, arguments, expected = "header", {"firstPage": 2}, (2, None)
    else:
        region, arguments, expected = "footer", {"lastPage": 2}, (1, 2)

    def bounds(**change):
        result = suite.call("page_region_range", region=region, **change)
        assert result["region"] == region, result
        return result["firstPage"], result.get("lastPage")

    fixture = suite.input_root / "sentetik-3-sayfa.udf"
    distinct = "KURGU SAYFA BOLGESI 731"
    body = "KURGU BIRINCI SAYFA\nKURGU IKINCI SAYFA\nKURGU UCUNCU SAYFA"
    opening = suite.opened(path=fixture) if region == "header" else suite.opened(body)
    with opening:
        if region == "footer":
            suite.call("set_page_region", region="header", text="Kurgu üst yazı")
        suite.call("set_page_region", region=region, text=distinct)
        if region == "footer":
            for marker in ("KURGU UCUNCU SAYFA", "KURGU IKINCI SAYFA"):
                suite.select(suite.read_text().index(marker))
                suite.act("insert-pageBreak")
        assert bounds() == (1, None)
        assert bounds(**arguments) == expected
        path = suite.save(action)
    with suite.opened(path=path):
        assert bounds() == expected
        regions = suite.call("page_regions")["regions"]
        assert next(item for item in regions if item["region"] == region)["text"] == distinct
        pdf = suite.base / f"kurgu-aralik-{uuid.uuid4().hex[:8]}.pdf"
        output = suite.call("export_pdf", outputPath=str(pdf))
        assert output["pageCount"] == 3, output
        rendered = [distinct in (page.extract_text() or "") for page in PdfReader(pdf, strict=True).pages]
        assert rendered == ([False, True, True] if region == "header" else [True, True, False]), rendered
        if action == "hf-stopPage":
            assert bounds(lastPage=-1) == (1, None)
    visible = ", ".join(str(page) for page, shown in enumerate(rendered, start=1) if shown)
    return f"Aralık yeniden açılan UDF’de korundu. Yerel PDF’de {region} {visible}. sayfalarda göründü."


def check_brush(suite, action):
    source, target = "Kaynak", "Hedef"
    text = source + "\n" + target
    start = text.index(target)

    def assert_target(path, bold):
        root = suite.read_udf(path)
        run = root.find(f"./elements/paragraph/content[@startOffset='{start}']")
        assert (run.get("bold") == "true") == bold, run.attrib

    with suite.opened(text):
        suite.select(0, len(source))
        suite.act("font-bold")
        assert_target(suite.save("firca-once"), False)
        suite.call("apply_format_brush", sourceOffset=0, start=start, end=len(text))
        # A second capture would report FORMAT_BRUSH_ALREADY_ACTIVE if the native apply did not clear it.
        suite.call("apply_format_brush", sourceOffset=0, start=start, end=len(text))
        path = suite.save(action)
        assert_target(path, True)
    with suite.opened(path=path):
        assert suite.read_text() == text
        assert_target(suite.save(action + "-yeniden"), True)
    return "Yerel fırçanın uygulama ve temizleme adımından sonra yeniden fırça açıldı ve hedefin kalın biçimi UDF'de korundu."


def check_restart(suite, action):
    text = "Kurgu ilk kalem\nKurgu ikinci kalem"
    offset = text.index("Kurgu ikinci")
    with suite.opened(text):
        suite.select(0, len(text))
        suite.call("set_list_palette", preset="number_dot")
        suite.select(offset)
        assert suite.call("set_numbering_start", startAt=7)["explicitStart"] == 7
        suite.act(action)
        assert suite.call("get_list_item")["explicitStart"] == 1
        verify_native_counter(suite.tool, suite.document, 1)
        path = suite.save(action)
    with suite.opened(path=path):
        suite.select(offset)
        assert suite.call("get_list_item")["explicitStart"] == 1
        verify_native_counter(suite.tool, suite.document, 1)
    return "7 olarak ayarlanan yerel sayaç 1'e sıfırlandı ve yeniden açılan UDF'nin numara formunda 1 okundu."
