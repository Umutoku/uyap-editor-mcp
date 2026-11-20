from contextlib import suppress

from remaining9_presentation import playback_feedback


def open_action_cases(suite):
    excluded = {
        "add-annotation": "Native U.a(ActionEvent) has an empty body. Notes have separate comment tools.",
        "show-annotation": "Native cT.a(ActionEvent) has an empty body. Notes open through show-all-annotation.",
        "attributes-changed-character": "dT is a type_notification callback with an empty body.",
        "attributes-changed-paragraph": "eJ is a type_notification callback with an empty body.",
        "element-changed-character": "dU is a type_notification callback with an empty body.",
        "element-changed-paragraph": "eK is a type_notification callback with an empty body.",
        "dirty-state-change": "aM is a type_notification callback with an empty body. Document events supply the state.",
        "encrypt": "The encryption method called by aU always throws UnsupportedOperationException in 5.4.20.",
        "test": "Native dp is a developer action that exports to the fixed path C:/deneme3.html. HTML export uses an explicit output path.",
    }

    def barcode_button(action):
        value = "12345098765"
        with suite.opened(text="KURGU BARKOD BELGESİ"):
            original = suite.read_text()
            form = suite.form("insert-barcode")
            form = suite.input(form, "write", "text", value=value)
            assert suite.input(form, "press", "button", action)["state"] == "closed"
            path = suite.save("barkod-onay" if action == "Tamam" else "barkod-vazgec")
        saved = suite.read_udf(path)
        barcode = saved.find(".//barcode")
        if action == "Tamam":
            assert barcode is not None and barcode.get("barcodeData") == value, saved
        else:
            assert barcode is None and saved.findtext("content").rstrip("\n") == original, saved
        with suite.opened(path=path):
            assert original in suite.read_text()
            again = suite.save("barkod-tekrar")
        reopened_barcode = suite.read_udf(again).find(".//barcode")
        if action == "Tamam":
            assert reopened_barcode is not None and reopened_barcode.get("barcodeData") == value
            return "The native barcode confirmation inserted the synthetic barcode. Its 11 digits persisted after reopening."
        assert reopened_barcode is None
        return "The native barcode cancel button closed the form and the reopened UDF contained no barcode."

    def logging_toggle(action):
        with suite.opened(text="KURGU LOG BELGESİ"):
            original = suite.read_text()
            for expected in ("başlad", "bitti"):
                form = suite.form(action)
                message = " ".join(item["value"] for item in form["controls"] if item["role"] == "message")
                confirmation = next(item["label"] for item in form["controls"]
                                    if item["role"] == "button" and item["label"] in {"Tamam", "OK"})
                suite.confirm(form, confirmation)
                assert "loglama" in message and expected in message, form
            log = suite.input_root.parent / suite.document / ".uki" / "logs" / "editor.log"
            assert log.is_file(), "Native logging output did not appear under the managed profile."
            contents = log.read_text(encoding="utf-8", errors="replace")
            assert 0 <= contents.rfind("RootLogger.level=TRACE") < contents.rfind("RootLogger.level=INFO")
            assert suite.read_text() == original
        return "The native dialog reported logging started and stopped. The isolated profile log recorded TRACE then INFO."

    def diagnostics(action):
        marker = "KURGU_DIAGNOSTIK"
        body = marker + " Türkçe denetim metni"
        with suite.opened(text=body):
            # dZ emits a measured rectangle only when selection starts inside the first view run.
            suite.select(1 if action == "debug-lvuMetrics" else 0, len(marker))
            result = suite.act(action)
            diagnostic = result.get("diagnostic")
            if action == "debug-lvuMetrics":
                assert diagnostic and "Partial [1," in diagnostic and marker[1:] in diagnostic, result
                assert "R=java.awt.geom.Rectangle2D" in diagnostic and "Label=" in diagnostic, result
            else:
                assert diagnostic and marker in diagnostic and "Türkçe" in diagnostic, result
                assert "paragraph" in diagnostic and "content" in diagnostic, result
            assert suite.read_text() == body
        return ("Native layout metrics included the selected synthetic text and measured labels."
                if action == "debug-lvuMetrics" else "Swing emitted the synthetic document's element model.")

    def delete_entry_group(action):
        group_name = "KURGU_GIRIS_GRUBU"
        other_group = "KURGU_KORUNAN_GRUP"
        grouped_text = "KURGU GRUPLU SATIR\nKURGU AYNI GRUPTAN SATIR"
        fixture_text = grouped_text + "\nKURGU BAGIMSIZ SATIR"
        suite.document = suite.tool("uyap_open_template")["documentId"]

        def add_group(start, end, name):
            suite.select(start, end)
            form = suite.form("add-paragraph-field-group")
            form = suite.input(form, "write", "text", value=name)
            form = suite.input(form, "toggle", "toggle", "Tekrarlayan Alan Grubu", "true")
            assert suite.confirm(form, "Ekle")["state"] == "closed"

        try:
            suite.call("replace_text", start=0, end=0, text=fixture_text)
            add_group(2, len(grouped_text), group_name)
            add_group(len(grouped_text) + 2, len(fixture_text), other_group)
            before = suite.save("giris-grubu-once")
            initial = suite.read_udf(before)
            assert len(initial.findall(f".//paragraph[@GroupName='{group_name}']")) == 2
            assert len(initial.findall(f".//paragraph[@GroupName='{other_group}']")) == 1
            suite.select(2, 4)
            suite.act(action)
            after = suite.save("giris-grubu-sonra")
        except Exception:
            with suppress(AssertionError, RuntimeError):
                suite.input(suite.call("inspect_form"), "cancel")
            with suppress(AssertionError, RuntimeError):
                suite.save("giris-grubu-test-artigi")
                suite.call("close")
            raise
        else:
            suite.call("close")
        suite.document = suite.tool("uyap_open_template", path=str(after))["documentId"]
        try:
            assert suite.read_text() == fixture_text
            reopened = suite.read_udf(suite.save("giris-grubu-tekrar"))
            assert reopened.find(f".//paragraph[@GroupName='{group_name}']") is None
            assert len(reopened.findall(f".//paragraph[@GroupName='{other_group}']")) == 1
        finally:
            suite.call("close")
        return "Reopening showed both paragraphs of the deleted group ungrouped. The unrelated group and all text were preserved."

    cases = {
        "Tamam": barcode_button,
        "Vazgeç": barcode_button,
        "logger-activator": logging_toggle,
        "debug-lvuMetrics": diagnostics,
        "dump-model": diagnostics,
        "beep": lambda action: playback_feedback(suite, action),
        "delete-entryfield-group": delete_entry_group,
    }
    return cases, excluded
