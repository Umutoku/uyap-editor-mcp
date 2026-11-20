import json
import uuid
import zipfile
from contextlib import contextmanager
from xml.etree import ElementTree as ET
from comment_actions_acceptance import accept_comment_action


CASES = (
    "changeCase-firstUpper", "changeCase-lower", "changeCase-upper", "combine-table-cell",
    "comment-next", "comment-previous", "comment-show", "control_image_size",
    "decrease-leftIndent", "divide-table-cell", "edit-pageNumber", "font-action",
    "increase-hanging-indent", "increase-leftIndent", "info-selection", "ini-designer",
    "insert-column-left", "insert-column-right", "insert-paragraph-aftertable",
    "insert-tablecell-after", "insert-tablecell-before", "insert-tablerow-after", "insert-tablerow-before",
    "next-changes", "paragraph-action", "prepareTable", "previous-changes", "rotate-text",
    "selection-begin", "selection-end", "selection-word-info", "show-all-annotation",
    "show-signatures", "switch-letters", "table-properties", "table-row-heights", "view-showWhiteSpace",
)


def accept_pending_actions(tool, base, input_root, requested):
    suite = PendingActions(tool, base, input_root)
    cases = requested or CASES
    assert len(set(cases)) == len(cases) and set(cases) <= set(CASES), cases
    for action in cases:
        try:
            evidence = suite.run(action)
            outcome = {"status": "passed", "evidence": evidence}
        except (AssertionError, KeyError, LookupError, RuntimeError, ValueError) as error:
            outcome = {"status": "blocked", "detail": str(error) or type(error).__name__}
        print(json.dumps({"pendingAction": action, **outcome}, ensure_ascii=True), flush=True)
    print(json.dumps({"pendingActionsAttempted": len(cases)}), flush=True)


class PendingActions:
    def __init__(self, tool, base, input_root):
        self.tool = tool
        self.base = base
        self.input_root = input_root
        self.table_seed = None
        self.image_path = input_root / "sentetik-gorsel.png"

    def call(self, name, **args):
        return self.tool("uyap_" + name, documentId=self.document, **args)

    @contextmanager
    def opened(self, text=None, path=None):
        self.document = (self.tool("uyap_open", path=str(path), editable=True) if path
                         else self.tool("uyap_new"))["documentId"]
        try:
            if text:
                self.call("replace_text", start=0, end=0, text=text)
            yield
        finally:
            try:
                form = self.call("inspect_form")
                if form["state"] == "open":
                    self.input(form, "cancel")
                try:
                    self.call("close")
                except AssertionError as error:
                    if "UNSAVED" not in str(error):
                        raise
                    self.save("ara")
                    self.call("close")
            except (AssertionError, RuntimeError) as error:
                print(json.dumps({"pendingCleanup": str(error)}), flush=True)

    def select(self, start, end=None):
        return self.call("select_text", start=start, end=start if end is None else end)

    def act(self, name):
        return self.call("run_action", action=name)

    def form(self, name):
        result = self.call("open_form", action=name)
        assert result["state"] == "open", result
        return result

    def input(self, form, operation, role=None, label=None, value=None, index=0):
        controls = [item for item in form["controls"] if
                    (role is None or item["role"] == role) and (label is None or item["label"] == label)]
        assert operation == "cancel" or len(controls) > index, (operation, role, label, form)
        arguments = dict(revision=form["revision"], controlId=-1 if operation == "cancel" else controls[index]["id"],
                         operation=operation)
        if value is not None:
            arguments["value"] = value
        return self.call("form_input", **arguments)

    def confirm(self, form, label="Tamam"):
        return self.input(form, "press", "button", label)

    def save(self, action):
        path = self.base / f"kurgu-{action}-{uuid.uuid4().hex[:8]}.udf"
        self.call("save_copy", outputPath=str(path))
        return path

    @staticmethod
    def read_udf(path):
        with zipfile.ZipFile(path) as archive:
            return ET.fromstring(archive.read("content.xml"))

    def read_text(self):
        return self.call("read_text")["text"].rstrip("\n")

    def run(self, action):
        if action in CASE_OUTPUTS:
            return self.case_transform(action)
        if action in ("decrease-leftIndent", "increase-leftIndent", "increase-hanging-indent"):
            return self.indent(action)
        if action.startswith("selection-") and action != "selection-word-info":
            return self.selection(action)
        if action == "view-showWhiteSpace":
            return self.whitespace(action)
        if action in ("comment-next", "comment-previous", "comment-show", "show-all-annotation"):
            return accept_comment_action(self, action)
        if action in ("next-changes", "previous-changes"):
            return self.change_navigation(action)
        if action in TABLE_CASES:
            return self.table(action)
        if action in ("control_image_size", "rotate-text"):
            return self.image(action)
        return self.document_form(action)

    def case_transform(self, action):
        source, expected = CASE_OUTPUTS[action]
        with self.opened(source):
            self.select(0, len(source))
            self.act(action)
            assert self.read_text() == expected, self.read_text()
            path = self.save(action)
        with self.opened(path=path):
            assert self.read_text() == expected, self.read_text()
        return f"Metin kayıttan sonra {expected!r} olarak okundu."

    def indent(self, action):
        with self.opened("Kurgu girinti"):
            if action == "decrease-leftIndent":
                self.act("increase-leftIndent")
            before = self.read_udf(self.save("girinti-once")).find("./elements/paragraph")
            self.act(action)
            path = self.save(action)
            after = self.read_udf(path).find("./elements/paragraph")
            key = "Hanging" if action == "increase-hanging-indent" else "LeftIndent"
            initial, final = float(before.get(key, 0)), float(after.get(key, 0))
            if action.startswith("increase-"):
                assert final > initial, (before.attrib, after.attrib)
            else:
                assert final < initial, (before.attrib, after.attrib)
        with self.opened(path=path):
            assert self.read_text() == "Kurgu girinti"
        return f"Kaydedilen UDF'de {key} {initial:g} → {final:g}."

    def selection(self, action):
        text = "Birinci paragraf\nIkinci paragraf"
        with self.opened(text):
            self.select(8)
            self.act(action)
            state = self.call("view_state")
            selection = state["selectionStart"], state["selectionEnd"]
            expected = (0, 8) if action == "selection-begin" else (8, len(text))
            assert selection == expected, state
        return f"Editörde doğrulanan seçim aralığı {selection}."

    def whitespace(self, action):
        with self.opened("Kurgu  boşluk\nİkinci satır"):
            before = self.call("view_state")["showWhitespace"]
            self.act(action)
            assert self.call("view_state")["showWhitespace"] != before
            self.act(action)
            assert self.call("view_state")["showWhitespace"] == before
        return "Boşluk görünümü iki çağrıda açılıp geri kapatıldı."

    def change_navigation(self, action):
        from comparison_navigation_acceptance import verify_comparison_navigation
        return verify_comparison_navigation(self, action)

    def create_table(self, action="table-insert"):
        form = self.confirm(self.form(action))
        if form["state"] == "open":
            form = self.confirm(form, "Ekle")
        assert form["state"] == "closed", form

    def table(self, action):
        if action == "prepareTable":
            with self.opened():
                grid = self.form(action)
                assert self.confirm(grid, "2x3")["state"] == "closed"
                path = self.save(action)
            with self.opened(path=path):
                assert self.table_counts(self.read_udf(self.save("tablo-yeniden"))) == [3, 3]
            return "Yerel ızgaradan 2x3 seçildi. Kaydedilip açılan tabloda iki satır ve her satırda üç hücre görüldü."
        if not self.table_seed:
            with self.opened():
                self.create_table()
                self.table_seed = self.save("tablo-temel")
        with self.opened(path=self.table_seed):
            self.select(0)
            if action == "combine-table-cell":
                self.call("replace_text", start=0, end=1, text="A")
                self.call("replace_text", start=2, end=3, text="B")
                self.select(0, 3)
            if action in ("table-properties", "table-row-heights"):
                return self.table_form(action)
            anchor = TABLE_INSERT_ANCHORS.get(action)
            if anchor:
                self.call("replace_text", start=8, end=9, text="K")
                self.select(8)
            if action == "divide-table-cell":
                for offset, marker in ((0, "A"), (2, "B"), (4, "C")):
                    self.call("replace_text", start=offset, end=offset + 1, text=marker)
                self.select(0)
                form = self.form(action)
                form = self.input(form, "write", "text", index=0, value="2")
                form = self.input(form, "write", "text", index=1, value="1")
                assert self.confirm(form)["state"] == "closed"
            else:
                self.act(action)
            path = self.save(action)
            saved = self.read_udf(path)
            if action == "divide-table-cell":
                self.assert_divided_cell(saved)
            rows = self.table_counts(saved)
            expected = [3, 4, 3] if action.startswith("insert-tablecell-") else TABLE_CASES[action]
            assert rows == expected, (rows, expected, ET.tostring(saved.find(".//table"), encoding="unicode")[:420])
            if anchor:
                assert self.table_anchor(saved) == anchor, self.table_anchor(saved)
            if action == "combine-table-cell":
                self.assert_cell_contains(saved, "A", "B")
            if action == "insert-paragraph-aftertable":
                assert len(saved.findall("./elements/paragraph")) > 1, ET.tostring(saved.find("elements"), encoding="unicode")[-350:]
        with self.opened(path=path):
            reopened = self.read_udf(self.save("tablo-yeniden"))
            rows_reopened = self.table_counts(reopened)
            assert rows_reopened == rows, rows_reopened
            if action == "divide-table-cell":
                self.assert_divided_cell(reopened)
            if anchor:
                assert self.table_anchor(reopened) == anchor
            if action == "combine-table-cell":
                self.assert_cell_contains(reopened, "A", "B")
        if anchor:
            return f"Satır hücre sayıları {rows}. İşaretli hücrenin yeni konumu {anchor[0]+1}. satır ve {anchor[1]+1}. sütun."
        if action == "divide-table-cell":
            return "Seçili hücrenin içinde yerel bölmeyle 1x2 tablo oluştu. Yeniden açılan UDF'de komşu hücre metinleri korundu."
        return f"Kaydedilip açılan tabloda satır başına hücre sayıları {rows}."

    @staticmethod
    def table_counts(markup):
        return [len(row.findall("cell")) for row in markup.findall(".//table/row")]

    @staticmethod
    def assert_cell_contains(markup, *expected):
        text = markup.findtext("content")
        cell = markup.find(".//table/row/cell")
        fragments = []
        for run in cell.findall(".//content"):
            start = int(run.get("startOffset"))
            fragments.append(text[start:start + int(run.get("length"))])
        merged = "".join(fragments)
        assert all(value in merged for value in expected), merged

    @staticmethod
    def assert_divided_cell(markup):
        outer = markup.find("./elements/table")
        sizes = [len(row.findall("cell")) for row in outer.findall("row")]
        assert sizes == [3, 3, 3], sizes
        inner = outer.find("row/cell/table")
        assert inner is not None, "Seçili hücrede iç tablo bulunamadı."
        sizes = [len(row.findall("cell")) for row in inner.findall("row")]
        assert sizes == [2], sizes
        text = markup.findtext("content")
        row = outer.find("row")
        for cell, marker in zip(row.findall("cell"), ("A", "B", "C")):
            content = "".join(text[int(run.get("startOffset")):int(run.get("startOffset")) + int(run.get("length"))]
                              for run in cell.findall(".//content"))
            assert marker in content, (marker, content)

    @staticmethod
    def table_anchor(markup):
        content = markup.findtext("content")
        for row_index, row in enumerate(markup.findall(".//table/row")):
            for col_index, cell in enumerate(row.findall("cell")):
                for run in cell.findall(".//content"):
                    start, length = int(run.get("startOffset")), int(run.get("length"))
                    if "K" in content[start:start + length]:
                        return row_index, col_index
        return None

    def table_form(self, action):
        before = self.read_udf(self.save("tablo-ozellik-once")).find(".//table") if action == "table-properties" else None
        form = self.form(action)
        assert form["title"].startswith("Tablo Özellikleri"), form
        if action == "table-row-heights":
            form = self.input(form, "write", "text", value="1.25")
            result = self.confirm(form)
            assert result["state"] == "closed", result
            row = self.read_udf(self.save(action)).find(".//table/row")
            assert float(row.get("height_min", 0)) == 1.25, row.attrib
            return "Tablo satırının en az yüksekliği UDF'de 1,25 olarak kaydedildi."
        form = self.input(form, "write", "text", value="14")
        result = self.confirm(form)
        if result["state"] == "open":
            result = self.confirm(result)
        assert result["state"] == "closed", result
        after = self.read_udf(self.save(action)).find(".//table")
        assert after.attrib != before.attrib, after.attrib
        self.select(0)
        readback = self.form(action)
        text = next(control["value"] for control in readback["controls"] if control["role"] == "text")
        assert abs(float(text) - 14) < 0.1, text
        self.input(readback, "cancel")
        return "Tablo genişliği değişti ve özellikler formundan 14 olarak geri okundu."

    def image(self, action):
        with self.opened():
            self.call("insert_image", path=str(self.image_path))
            before = self.read_udf(self.save("gorsel-once")).find(".//image")
            assert before is not None
            self.select(0, 1)
            if action == "control_image_size":
                form = self.form(action)
                form = self.input(form, "toggle", "toggle", "En boy oranını koru", "false")
                form = self.input(form, "write", "text", index=0, value="50")
                form = self.input(form, "write", "text", index=1, value="50")
                assert self.confirm(form)["state"] == "closed"
                expected = tuple(float(before.get(key)) / 2 for key in ("width", "height"))
            else:
                self.act(action)
                expected = tuple(float(before.get(key)) for key in ("height", "width"))
            path = self.save(action)
            after = self.read_udf(path).find(".//image")
            assert after is not None
            actual = tuple(float(after.get(key)) for key in ("width", "height"))
            assert actual == expected, (actual, expected)
        with self.opened(path=path):
            after = self.read_udf(self.save("gorsel-yeniden")).find(".//image")
            assert tuple(float(after.get(key)) for key in ("width", "height")) == expected
        return f"Kaydedilip açılan görselin boyutu {expected[0]:g}×{expected[1]:g}."

    def document_form(self, action):
        with self.opened("Kurgu secili kelime"):
            if action == "ini-designer":
                self.save("stil-profili-belge")
            if action == "edit-pageNumber":
                form = self.form("insert-pageNumber")
                form = self.input(form, "toggle", "toggle", "Sağ", "true")
                self.confirm(self.input(form, "write", "text", value="Kurgu Eski "))
                baseline = self.read_udf(self.save("pageNumber-temel"))
                assert any(tag.get("pageNumber-foreStr") == "Kurgu Eski " for tag in baseline.iter())
            else:
                self.select(0, 5)
            form = self.form(action)
            if action == "font-action":
                control = next(item for item in form["controls"] if item["role"] == "choice" and "16" in item["options"])
                form = self.call("form_input", revision=form["revision"], controlId=control["id"],
                                 operation="choose", value="16")
                form = self.input(form, "toggle", "toggle", "Koyu", "true")
                assert self.confirm(form)["state"] == "closed"
                path = self.save(action)
                markup = self.read_udf(path)
                runs = [run.attrib for run in markup.findall("./elements/paragraph/content")]
                assert any(run.get("size") == "16" and run.get("bold") == "true" and run.get("startOffset") == "0"
                           and run.get("length") == "5" for run in runs), runs
                return "Yazı tipi formundan seçili beş karaktere uygulanan 16 punto ve koyu biçim UDF'de okundu."
            if action == "paragraph-action":
                form = self.input(form, "write", "text", index=0, value="2")
                assert self.confirm(form)["state"] == "closed"
                markup = self.read_udf(self.save(action))
                paragraph = markup.find("./elements/paragraph")
                assert float(paragraph.get("LineSpacing", 0)) == 1, paragraph.attrib
                return "Paragraf formundan uygulanan çift satır aralığı UDF'de okunabildi."
            if action == "edit-pageNumber":
                continued = self.confirm(form, "Düzenle")
                assert continued["state"] == "open" and continued["title"] == "Sayfa Numarası", continued
                continued = self.input(continued, "write", "text", value="Kurgu Yeni ")
                result = self.confirm(continued)
                if result["state"] == "open":
                    self.confirm(result, "Kapat")
                saved = self.read_udf(self.save(action))
                assert any(tag.get("pageNumber-foreStr") == "Kurgu Yeni " for tag in saved.iter())
                return "Sayfa numarası ön eki düzenlendi ve kurgu UDF'de doğrulandı."
            if action == "ini-designer":
                assert form["title"] == "İni Dosyası Düzenleme"
                mode = next(item for item in form["controls"] if item["role"] == "choice" and " Düzenle" in item["options"])
                addition = next(option for option in mode["options"] if "Ekle" in option)
                form = self.input(form, "choose", "choice", value=addition, index=1)
                assert any(item["role"] == "choice" and item["value"] == addition for item in form["controls"])
                form = self.confirm(form)
                assert form["title"] == "Stil Bilgisi", form
                name = f"Kurgu Stil {uuid.uuid4().hex[:8]}"
                form = self.confirm(self.input(form, "write", "text", value=name))
                assert any(name in item["options"] for item in form["controls"]), form
                self.input(form, "press", "button", "Tamam", index=1)
                reopened = self.form(action)
                assert any(name in item["options"] for item in reopened["controls"]), reopened
                self.input(reopened, "cancel")
                preferences = self.input_root.parent / self.document / ".uki" / "ayarlar.ini"
                assert f"description={name}" in preferences.read_text(encoding="utf-8")
                return "Kurgu stil yeniden açılan formdan ve yönetilen profilin ayarlar.ini kaydından okundu."
            if action == "show-signatures":
                assert any("imza yok" in control["value"].casefold() for control in form["controls"]
                           if control["role"] == "message"), form
                self.confirm(form)
                return "İmzasız kurgu belge için yerel “imza yok” uyarısı okundu ve kapatıldı."
            if action == "selection-word-info":
                words = [control["value"] for control in form["controls"] if control["role"] == "message"]
                assert "Kelime Özellikleri" == form["title"] and any("Kurgu" in value for value in words), words
                self.input(form, "cancel")
                return "Kelime özellikleri görünümünde kurgu metni ve yerel özellik sonucu okundu."
            if action == "info-selection":
                values = [control["value"] for control in form["controls"]]
                assert any("Pozisyon" in value for value in values)
                assert any(control["role"] == "text" and float(control["value"]) >= 0
                           for control in form["controls"]), form
                self.input(form, "cancel")
                return "Yerel seçim penceresindeki konum değeri okundu."
            raise AssertionError(f"Form {form['title']!r}. Denetlenebilir alanlar: "
                                 f"{[(i['role'], i['label'], i['value']) for i in form['controls']]}")


CASE_OUTPUTS = {
    "changeCase-firstUpper": ("kurgu belge", "Kurgu Belge"),
    "changeCase-lower": ("KURGU BELGE", "kurgu belge"),
    "changeCase-upper": ("kurgu belge", "KURGU BELGE"),
    "switch-letters": ("KuRgU bElGe", "kUrGu BeLgE"),
}
TABLE_CASES = {
    "combine-table-cell": [2, 3, 3],
    "divide-table-cell": [3, 3, 3, 2],
    "insert-column-left": [4, 4, 4],
    "insert-column-right": [4, 4, 4],
    "insert-paragraph-aftertable": [3, 3, 3],
    "insert-tablecell-after": [4, 3, 3],
    "insert-tablecell-before": [4, 3, 3],
    "insert-tablerow-after": [3, 3, 3, 3],
    "insert-tablerow-before": [3, 3, 3, 3],
    "prepareTable": None,
    "table-properties": None,
    "table-row-heights": None,
}
TABLE_INSERT_ANCHORS = {
    "insert-column-left": (1, 2),
    "insert-column-right": (1, 1),
    "insert-tablecell-before": (1, 2),
    "insert-tablecell-after": (1, 1),
    "insert-tablerow-before": (2, 1),
    "insert-tablerow-after": (1, 1),
}
