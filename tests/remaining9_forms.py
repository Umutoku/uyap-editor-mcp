def remaining_cases(suite):
    return {
        "edit-hf": lambda action: combined_regions(suite, action),
        "insert-url-link": lambda action: embedded_link(suite, action),
        "table-properties-new": lambda action: new_table_properties(suite, action),
    }


def combined_regions(suite, action):
    body = "KURGU BELGE GOVDE"
    header = "KURGU YENI UST YAZI"
    footer = "KURGU YENI ALT YAZI"

    def regions():
        return {item["region"]: item for item in suite.call("page_regions")["regions"]}

    with suite.opened(body):
        suite.call("run_action", action=action, text=footer, expected_error="PAGE_REGION_MISSING")
        suite.call("set_page_region", region="footer", text="KURGU ESKI ALT")
        result = suite.call("run_action", action=action, text=footer)
        assert result.get("diagnostic") == "footer" and regions()["footer"]["text"] == footer, result
        suite.call("set_page_region", region="header", text="KURGU ESKI UST")
        start = suite.read_text().index(body)
        suite.select(start, start + len(body))
        result = suite.call("run_action", action=action, text=header)
        assert result.get("diagnostic") == "header", result
        updated = regions()
        assert updated["header"]["text"] == header and updated["footer"]["text"] == footer, updated
        start = suite.read_text().index(body)
        assert (result["selectionStart"], result["selectionEnd"]) == (start, start + len(body)), result
        suite.call("replace_text", start=start, end=start + len(body), text="KURGU GOVDE KORUNDU")
        assert regions()["header"]["text"] == header and regions()["footer"]["text"] == footer
        path = suite.save(action)
    with suite.opened(path=path):
        saved = regions()
        assert saved["header"]["text"] == header and saved["footer"]["text"] == footer, saved
        assert "KURGU GOVDE KORUNDU" in suite.read_text()
    return "Yerel komut önce mevcut alt yazıyı, üst yazı eklenince üst yazıyı düzenledi. Gövde seçimine dönüldü ve UDF yeniden açıldığında iki bölge korundu."


def embedded_link(suite, action):
    label = "KURGU KOPRU BAGLANTISI"
    tail = " VE GOVDE"
    url = "https://ornek.invalid/kurgu-uyap"

    def link_in(path):
        root = suite.read_udf(path)
        body = root.findtext("content")
        assert body.startswith(label + tail), body
        links = []
        for span in root.findall(".//content"):
            if span.get("urlLinkData") == url:
                start = int(span.get("startOffset"))
                links.append(body[start:start + int(span.get("length"))])
        assert "".join(links) == label, (links, [(item.tag, item.attrib) for item in root.findall(".//content")])

    with suite.opened(label + tail):
        suite.select(0)
        suite.call("open_form", action=action, expected_error="HYPERLINK_SELECTION_REQUIRED")
        suite.select(0, len(label))
        dialog = suite.form(action)
        assert dialog["title"] == "Köprü Ekle", dialog
        dialog = suite.input(dialog, "write", "text", value="file:///C:/kurgu-uyap")
        okay = next(item for item in dialog["controls"] if item["role"] == "button" and item["label"] == "Tamam")
        suite.call("form_input", revision=dialog["revision"], controlId=okay["id"], operation="press",
                   expected_error="HYPERLINK_URL_INVALID")
        dialog = suite.input(dialog, "write", "text", value=url)
        assert suite.confirm(dialog)["state"] == "closed"
        path = suite.save(action)
        link_in(path)
    with suite.opened(path=path):
        assert suite.read_text() == label + tail
        link_in(suite.save("kopru-yeniden"))
    return "Yerel Köprü Ekle formu seçili metne HTTPS hedefi yazdı. UDF yeniden açıldığında metin ve urlLinkData korundu. Dosya URI’si reddedildi."


def new_table_properties(suite, action):
    with suite.opened():
        suite.call("open_form", action=action, expected_error="TABLE_CELL_SELECTION_REQUIRED")
        suite.create_table()
        suite.select(0)
        baseline = suite.read_udf(suite.save("tablo-yeni-ozellik-once"))
        dialog = suite.form(action)
        assert dialog["title"].startswith("Tablo Özellikleri"), dialog
        dialog = suite.input(dialog, "write", "text", value="14")
        result = suite.confirm(dialog)
        if result["state"] == "open":
            result = suite.confirm(result)
        assert result["state"] == "closed", result
        path = suite.save(action)
        saved = suite.read_udf(path)
        assert saved.find(".//table").attrib != baseline.find(".//table").attrib
        assert suite.table_counts(saved) == suite.table_counts(baseline)
    with suite.opened(path=path):
        suite.select(0)
        dialog = suite.form(action)
        width = next(item["value"] for item in dialog["controls"] if item["role"] == "text")
        assert abs(float(width.replace(",", ".")) - 14) < 0.1, dialog
        assert suite.input(dialog, "cancel")["state"] == "closed"
    return "Yerel tablo özelliklerinin yeni eylemi 14 genişliğini kaydetti. Yeniden açılan tablonun formundan değer okundu ve hücre düzeni korundu."
