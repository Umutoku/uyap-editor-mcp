import json

from open_actions_acceptance import OpenActions


GROUPS = {
    "application-freemem-level1": "managed",
    "beep": "presentation",
    "edit-hf": "forms",
    "export-svg": "formats",
    "fullscreen": "presentation",
    "insert Tl symbol": "presentation",
    "insert-url-link": "forms",
    "save-doc": "formats",
    "table-properties-new": "forms",
}


def clear_managed_history(suite, action):
    original = "KURGU PANO"
    selected = "KURGU"
    with suite.opened(original):
        suite.select(0, len(selected))
        suite.act("copy-to-clipboard")
        suite.select(len(original))
        suite.act("paste-from-clipboard")
        expected = original + selected
        assert suite.read_text() == expected
        suite.act("undo")
        assert suite.read_text() == original
        suite.act("redo")
        assert suite.read_text() == expected
        suite.call("replace_text", start=len(expected), end=len(expected), text=" GECICI")
        suite.act("undo")
        assert suite.read_text() == expected
        suite.act(action)
        for command in ("undo", "redo"):
            suite.act(command)
            assert suite.read_text() == expected
        suite.call("run_action", action="paste-from-clipboard", expected_error="CLIPBOARD_EMPTY")
        assert suite.read_text() == expected
        suite.call("close", expected_error="UNSAVED_CHANGES")
        saved = suite.save("hafiza-bosalt")
        with suite.reopened(saved):
            assert suite.read_text() == expected
    return "Native undo/redo history and the managed clipboard were cleared. Unsaved edits were kept and reopened."


def accept_remaining_actions(tool, base, input_root, requested):
    suite = OpenActions(tool, base, input_root)
    selected = requested or list(GROUPS)
    assert len(set(selected)) == len(selected) and set(selected) <= GROUPS.keys(), selected
    cases = {"application-freemem-level1": lambda action: clear_managed_history(suite, action)}
    for group in set(GROUPS[action] for action in selected) - {"managed"}:
        module = __import__(f"remaining9_{group}")
        discovered = module.remaining_cases(suite)
        assert discovered.keys() == {action for action, domain in GROUPS.items() if domain == group}, (group, discovered)
        cases.update(discovered)
    for action in selected:
        try:
            evidence = cases[action](action)
            outcome = {"status": "passed", "evidence": evidence}
        except (AssertionError, KeyError, LookupError, RuntimeError, ValueError) as error:
            outcome = {"status": "blocked", "detail": str(error) or type(error).__name__}
        print(json.dumps({"remainingAction": action, **outcome}, ensure_ascii=True), flush=True)
    print(json.dumps({"remainingActionsAttempted": len(selected)}), flush=True)
