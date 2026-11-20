import uuid


def accept_exit(suite):
    tool = suite.tool
    other_id = tool("uyap_new")["documentId"]
    target_id = None
    saved = suite.base / f"kurgu-cikis-{uuid.uuid4().hex[:8]}.udf"
    marker = "KURGU YONETILEN PENCERE"
    try:
        target_id = tool("uyap_new")["documentId"]
        tool("uyap_replace_text", documentId=target_id, start=0, end=0, text=marker)
        tool("uyap_close", expected_error="UNSAVED_CHANGES", documentId=target_id)
        assert tool("uyap_read_text", documentId=target_id)["text"].rstrip("\n") == marker
        tool("uyap_save_copy", documentId=target_id, outputPath=str(saved))
        closed = tool("uyap_close", documentId=target_id)
        assert closed == {"documentId": target_id, "closed": True}, closed
        tool("uyap_read_text", expected_error="DOCUMENT_UNKNOWN", documentId=target_id)
        tool("uyap_get_page", documentId=other_id)
        target_id = None
        reopened_id = tool("uyap_open", path=str(saved))["documentId"]
        try:
            assert tool("uyap_read_text", documentId=reopened_id)["text"].rstrip("\n") == marker
        finally:
            tool("uyap_close", documentId=reopened_id)
    finally:
        if target_id is not None:
            try:
                tool("uyap_close", documentId=target_id)
            except AssertionError:
                tool("uyap_save_copy", documentId=target_id,
                     outputPath=str(suite.base / f"kurgu-cikis-temizlik-{uuid.uuid4().hex[:8]}.udf"))
                tool("uyap_close", documentId=target_id)
        tool("uyap_close", documentId=other_id)
    return {"tool": "uyap_close", "retainsUnsavedChanges": True, "otherSessionOpen": True}


def accept_read_only(suite):
    with suite.opened("KURGU SALT OKUNUR"):
        start = suite.call("edit_mode")
        assert start == {"editable": True, "nativeEditable": True, "canEnableEditing": True}, start
        mode = suite.call("edit_mode", editable=False)
        assert mode == {"editable": False, "nativeEditable": False, "canEnableEditing": True}, mode
        try:
            assert suite.call("edit_mode") == mode
            suite.call("replace_text", expected_error="READ_ONLY_SESSION", start=0, end=0, text="HATA")
            assert suite.read_text() == "KURGU SALT OKUNUR"
        finally:
            suite.call("edit_mode", editable=True)
    return {"tool": "uyap_edit_mode", "nativeEditable": False, "mutationsRejected": True}


def accept_writable(suite):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    tool = suite.tool
    denied = tool("uyap_open", path=str(source))["documentId"]
    try:
        tool("uyap_edit_mode", expected_error="READ_ONLY_SESSION", documentId=denied, editable=True)
        still_denied = tool("uyap_edit_mode", documentId=denied)
        assert still_denied["editable"] is False and still_denied["canEnableEditing"] is False, still_denied
    finally:
        tool("uyap_close", documentId=denied)
    with suite.opened("KURGU ILK"):
        assert suite.call("edit_mode", editable=False)["nativeEditable"] is False
        state = suite.call("edit_mode", editable=True)
        assert state == {"editable": True, "nativeEditable": True, "canEnableEditing": True}, state
        suite.call("replace_text", start=6, end=9, text="YENI")
        path = suite.save("yazilabilir")
        with suite.reopened(path):
            assert suite.read_text() == "KURGU YENI"
    return {"tool": "uyap_edit_mode", "nativeEditable": True, "permissionEscalationDenied": True}


def residual_cases(suite):
    return {
        "exit": lambda action: accept_exit(suite),
        "set-read-only": lambda action: accept_read_only(suite),
        "set-writable": lambda action: accept_writable(suite),
        "set-tooltip-enable": lambda action: accept_help(suite),
    }


def accept_help(suite):
    text = "KURGU YARDIM BELGESI"
    with suite.opened(text):
        handoff = suite.call("prepare_workflow", workflow="help")
        assert handoff["tab"] == "Görünüm" and handoff["editorCommand"] == "Yardım Özelliklerini Aç", handoff
        assert handoff["enabled"] and handoff["status"] == "user_action_required", handoff
        assert suite.read_text() == text
    return {"coverage": "prepared", "workflow": "help", "command": handoff["editorCommand"]}
