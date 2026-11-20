import hashlib
import re


def open_action_cases(suite):
    return {
        "last-opened-docs": lambda _: recent_documents(suite),
        "letter-mode-select-menu": lambda _: case_menu(suite),
        "release-notes-action-str": lambda action: release_notes(suite, action),
        "show-elementTreePanel": lambda action: element_tree(suite, action),
        "windows-menu": lambda action: activate_window(suite, action),
    }, {
        "file-toolbar": "Native aY only resolves the editor and returns. The action supplies the toolbar label and icon.",
        "layoutChanged-horizontal": "Native ew sets type_notification and has an empty handler. Native hj.M publishes the layout state.",
        "show-paragraph-properties": "Native cV copies the selected paragraph attributes and writes them to its diagnostic logger.",
        "style-toolbar": "Native dd only resolves the editor and returns. The action supplies the toolbar label and icon.",
    }


def menu_items(suite):
    entries = []
    while True:
        page = suite.call("menu_inventory", scope="menus", offset=len(entries), limit=100)
        assert page["offset"] == len(entries) and (page["entries"] or page["total"] == len(entries)), page
        entries.extend(page["entries"])
        if len(entries) == page["total"]:
            return entries


def recent_documents(suite):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    with suite.opened(path=source):
        menu = menu_items(suite)
        matching = [entry for entry in menu if entry["path"] == ["Uygulama menüsü", "Son Belgeler",
                                                                 "Son Açılan Belgeler"]]
        assert any(entry["label"] == source.name and entry["state"] == "enabled" for entry in matching), matching
        original = suite.read_text()
        assert original.strip()
        with suite.reopened(source):
            assert suite.read_text() == original
    return {"nativeRecentLabel": source.name, "reopenedThrough": "uyap_open",
            "sourceSha256": hashlib.sha256(source.read_bytes()).hexdigest()}


def case_menu(suite):
    source = "kurgu deneme"
    expected = "Kurgu Deneme"
    labels = {"Harf Modu Değiştir", "Her Sözcüğü Büyük Harfe Çevir", "küçük harf", "BÜYÜK HARF"}
    with suite.opened(source):
        suite.select(0, len(source))
        submenu = [entry for entry in menu_items(suite) if entry["label"] in labels
                   and "Font" in entry["path"] and entry["kind"] == "POPUP_ITEM"]
        assert {entry["label"] for entry in submenu} == labels, submenu
        suite.act("changeCase-firstUpper")
        assert suite.read_text() == expected, suite.read_text()
        saved = suite.save("harf-menu")
        with suite.reopened(saved):
            assert suite.read_text() == expected, suite.read_text()
    return {"nativePopupLabels": sorted(labels), "savedAndReopened": expected}


def release_notes(suite, action):
    with suite.opened(path=suite.input_root / "sentetik-3-sayfa.udf"):
        form = suite.form(action)
        assert "Son Sürümlerde Yapılan Güncellemeler" in form["title"], form
        messages = [control["value"] for control in form["controls"] if control["role"] == "message"]
        assert any("Sürüm: 5.4.19" in value and "Odt formatına çevirimde" in value
                   for value in messages), messages
        assert suite.input(form, "cancel")["state"] == "closed", form
        assert suite.read_text().strip()
    return {"nativeDialog": form["title"], "shippedNotesHeading": "5.4.19", "closed": True}


def element_tree(suite, action):
    with suite.opened("Kurgu eleman ağacı"):
        form = suite.form(action)
        assert form["title"] == "E.T.P.", form
        tree = next(control["value"] for control in form["controls"] if control["label"] == "Element tree")
        assert tree.startswith("root\n") and re.search(r"\bparagraph \d+-\d+", tree), tree
        assert suite.input(form, "cancel")["state"] == "closed", form
        assert suite.read_text() == "Kurgu eleman ağacı"
    return {"nativeDialog": form["title"], "treeNodes": len(tree.splitlines()), "closed": True}


def activate_window(suite, action):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    first = suite.tool("uyap_open", path=str(source))["documentId"]
    second = None
    try:
        second = suite.tool("uyap_open", path=str(source))["documentId"]
        windows = {window["documentId"] for window in suite.tool("uyap_list_windows")["windows"]}
        assert first != second and {first, second} <= windows, windows
        for target, position in ((first, 2), (second, 4), (first, 2)):
            before = suite.tool("uyap_select_text", documentId=target, start=position, end=position)
            assert before["start"] == before["end"] == position, before
            result = suite.tool("uyap_run_action", documentId=target, action=action)
            assert result["action"] == action and result["selectionStart"] == result["selectionEnd"] == position, result
    finally:
        if second is not None:
            suite.tool("uyap_close", documentId=second)
        suite.tool("uyap_close", documentId=first)
    return {"managedWindows": 2, "activationOrder": [1, 2, 1], "nativeActivationConfirmed": True}
