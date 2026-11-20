def accept_comment_action(suite, action):
    if action in ("comment-next", "comment-previous"):
        return accept_comment_navigation(suite, action)

    with suite.opened("Birinci aciklama. Ikinci aciklama."):
        notes = [(0, 7, "Kurgu not bir"), (18, 24, "Kurgu not iki")]
        for start, end, note in notes:
            suite.call("add_comment", start=start, end=end, text=note)
        before = suite.call("view_state")
        if action == "comment-show":
            suite.act(action)
            after = suite.call("view_state")
            assert after["showComments"] != before["showComments"], (before, after)
            suite.act(action)
            assert suite.call("view_state")["showComments"] == before["showComments"]
            return "Yerel açıklama paneli iki çağrıda açılıp kapatıldı ve başlangıç durumuna döndü."

        if before["showComments"]:
            suite.act("comment-show")
        assert suite.call("view_state")["showComments"] is False
        form = suite.call("open_form", action=action)
        assert form["state"] == "completed" and suite.call("view_state")["showComments"] is True, form
        listed = suite.call("list_comments")["comments"]
        assert [(item["start"], item["end"], item["text"]) for item in listed] == notes, listed
        return "Yerel açıklama paneli açıldı. Belge modelinde iki kurgu açıklamanın aralığı ve metni doğrulandı."


def accept_comment_navigation(suite, action):
    with suite.opened(path=suite.input_root / "sentetik-3-sayfa.udf"):
        anchors = []
        for query, note in (("SATIR 003", "Kurgu ilk sayfa"), ("SATIR 110", "Kurgu son sayfa")):
            found = suite.call("find", query=query)
            assert found["found"], found
            suite.call("add_comment", start=found["start"], end=found["end"], text=note)
            anchors.append(found["start"])
        forward = action == "comment-next"
        start_page, target_page = (1, 3) if forward else (3, 1)
        target_anchor = anchors[-1 if forward else 0]
        suite.call("go_to_page", page=start_page)
        observed = []
        for _ in range(2):
            result = suite.act(action)
            page = suite.call("get_page")["page"]
            observed.append((page, result["selectionStart"]))
            if (page, result["selectionStart"]) == (target_page, target_anchor):
                return f"Sayfa {start_page} → {target_page}, açıklama başlangıcı {target_anchor}."
        raise AssertionError(f"Açıklama gezintisi {start_page} → {observed}, beklenen {(target_page, target_anchor)}.")
