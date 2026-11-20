import json
from xml.etree import ElementTree


def residual_cases(suite):
    return {
        "apdu-action": lambda action: apdu(suite, action),
        "Set Sign Libary": lambda action: inspect_library(suite, action),
        "certificate-owned-validate": lambda action: signing_handoff(suite, action, "certificates", "Sertifikalar"),
        "sign": lambda action: signing_handoff(suite, action, "sign", "İmzala"),
        "Mobile-Sign": lambda action: signing_handoff(suite, action, "mobile_sign", "Mobil İmzala"),
        "sign-batch": lambda action: signing_handoff(suite, action, "batch_sign", "Toplu İmzala"),
    }


def apdu(suite, action):
    text = "KURGU APDU BELGESİ"
    with suite.opened(text):
        config = suite.input_root.parent / suite.document / ".uki" / "acilisDegerleri.xml"

        def configured():
            element = ElementTree.parse(config).find(".//propertyGroup[@name='initValues']/property[@name='APDU_ACTIVE']")
            assert element is not None and element.text in ("true", "false")
            return element.text == "true"

        original = configured()
        suite.select(6, 10)
        for expected in (not original, original):
            result = suite.act(action)
            assert (result["selectionStart"], result["selectionEnd"]) == (6, 10), result
            assert json.loads(result["diagnostic"]) == {"apduEnabled": expected}, result
            assert configured() == expected
        assert suite.read_text() == text
    return {"apduStates": [not original, original], "profileSettingVerified": True, "bodyUnchanged": True}


def inspect_library(suite, action):
    text = "KURGU İMZA KÜTÜPHANESİ BELGESİ"
    with suite.opened(text):
        form = suite.form(action)
        assert form["title"] == "İmza Kütüphanesi", form
        messages = [str(item["value"]) for item in form["controls"] if item["role"] == "message"]
        assert any("Kütüphaneniz" in value and "Otomatik" in value for value in messages), form
        suite.call("form_input", revision=form["revision"], controlId=-1, operation="press",
                   expected_error="SIGNING_LIBRARY_READ_ONLY")
        assert suite.input(form, "cancel")["state"] == "closed"
        assert suite.read_text() == text
    return {"coverage": "prepared", "dialog": "İmza Kütüphanesi", "configured": "automatic",
            "userReason": "Gerçek kart için doğru PKCS#11 sağlayıcısı veya DLL kullanıcı tarafından seçilir."}


def signing_handoff(suite, action, workflow, command):
    text = "KURGU İMZA İŞ AKIŞI"
    with suite.opened(text):
        prepared = suite.call("prepare_workflow", workflow=workflow)
        assert prepared["workflow"] == workflow and prepared["editorCommand"] == command, prepared
        assert prepared["enabled"] and prepared["status"] == "user_action_required", prepared
        assert suite.call("inspect_form")["state"] == "closed"
        assert suite.read_text() == text
    return {"coverage": "prepared", "workflow": workflow, "tab": prepared["tab"], "command": command,
            "status": prepared["status"]}
