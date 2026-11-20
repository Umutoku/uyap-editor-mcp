import hashlib
import uuid


def approval_cases(suite):
    return {
        "dss-save": lambda action: portal_review(suite, action),
        "set proxy": lambda action: proxy_review(suite, action),
        "set-tooltip-enable": lambda action: menu_review(suite, action, "help"),
        "update": lambda action: menu_review(suite, action, "update"),
    }


def request(suite, action, output=None, **options):
    document = {"documentId": suite.document}
    if output is not None:
        document["outputPath"] = str(output)
    return {"action": action, "documents": [document], **options}


def review(suite, action, output=None, **options):
    result = suite.tool("uyap_prepare_approval", request=request(suite, action, output, **options))
    assert result["action"] == action and result["userStep"], result
    return result


def portal_review(suite, action):
    marker = "KURGU PORTAL ONAY METNI"
    target = suite.base / f"kurgu-portal-{uuid.uuid4().hex[:8]}.udf"
    with suite.opened(marker):
        result = review(suite, action, output=target)
        assert result["status"] == "blocked_prerequisite", result
        assert result["missing"] == ["authorized_portal_session", "verified_portal_record"]
        assert result["workflow"]["editorCommand"] == "Sisteme Kaydet"
        assert result["details"] == [{"label": "portalCommandAvailable",
                                      "value": str(result["workflow"]["enabled"]).lower()}]
        (artifact,) = result["artifacts"]
        assert artifact["path"] == str(target)
        assert artifact["sha256"] == hashlib.sha256(target.read_bytes()).hexdigest().upper()
        assert suite.call("inspect_form")["state"] == "closed"
        suite.call("close", expected_error="UNSAVED_CHANGES")
        with suite.reopened(target):
            assert suite.read_text() == marker
    return {"reviewStatus": result["status"], "localSnapshot": True, "missing": result["missing"]}


def proxy_review(suite, action):
    with suite.opened("KURGU PROXY ONAYI"):
        missing = review(suite, action)
        assert missing["status"] == "input_required" and missing["missing"] == ["proxyAddress"], missing
        invalid = request(suite, action, proxyAddress="http://kurgu:ornek@proxy.example.invalid:3128")
        suite.tool("uyap_prepare_approval", request=invalid, expected_error="PROXY_ADDRESS_INVALID")
        result = review(suite, action, proxyAddress="http://proxy.example.invalid:3128")
        assert result["status"] == "awaiting_user_confirmation" and not result["missing"], result
        assert {item["label"]: item["value"] for item in result["details"]} == {
            "scheme": "http", "host": "proxy.example.invalid", "port": "3128"}
        assert not result["artifacts"] and suite.call("inspect_form")["state"] == "closed"
        profile = suite.input_root.parent / suite.document / ".uki"
        for file in profile.iterdir():
            if file.is_file() and file.suffix.lower() in (".xml", ".properties", ".ini"):
                assert b"proxy.example.invalid" not in file.read_bytes(), file.name
    return {"reviewStatus": result["status"], "proposalOnly": True}


def menu_review(suite, action, workflow):
    source = suite.input_root / "sentetik-3-sayfa.udf"
    with suite.opened(path=source):
        result = review(suite, action)
        assert result["status"] == "awaiting_user_confirmation", result
        assert result["workflow"]["workflow"] == workflow and result["workflow"]["enabled"]
        assert not result["artifacts"] and not result["missing"]
        details = {item["label"]: item["value"] for item in result["details"]}
        if workflow == "help":
            assert details == {"editorHelpAddress": "http://www.uyap.gov.tr/editoryardim/"}
            suite.tool("uyap_prepare_approval", request=request(suite, "encrypt"),
                       expected_error="APPROVAL_ACTION_UNAVAILABLE")
        else:
            assert details["installedEditorVersion"] == "5.4.20"
            assert details["updateCheck"] == "user_action_required"
        assert suite.call("inspect_form")["state"] == "closed"
        assert suite.call("get_page")["pageCount"] == 3
    return {"reviewStatus": result["status"], "details": details}
