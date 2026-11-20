import json

from open_actions_acceptance import OpenActions


GROUPS = ("core", "mail", "print", "security")
ACTIONS = {
    "attachto-mail", "attachto-mail-doc", "attachto-mail-odt", "attachto-mail-pdf",
    "certificate-owned-validate", "document-export-signedddoc", "document-print", "dss-save",
    "Mobile-Sign", "print-batch", "set proxy", "Set Sign Libary", "set-tooltip-enable",
    "sign", "sign-batch", "update",
}


def accept_approval_actions(tool, base, input_root, groups, requested):
    suite = OpenActions(tool, base, input_root)
    cases = {}
    for group in groups or GROUPS:
        discovered = __import__(f"approval16_{group}").approval_cases(suite)
        assert not cases.keys() & discovered.keys(), group
        cases.update(discovered)
    if not groups:
        assert cases.keys() == ACTIONS, cases.keys() ^ ACTIONS
    selected = requested or list(cases)
    assert len(set(selected)) == len(selected) and set(selected) <= cases.keys(), selected
    for action in selected:
        try:
            evidence = cases[action](action)
            outcome = {"status": "passed", "evidence": evidence}
        except (AssertionError, KeyError, LookupError, RuntimeError, ValueError) as error:
            outcome = {"status": "blocked", "detail": str(error) or type(error).__name__}
        print(json.dumps({"approvalAction": action, **outcome}, ensure_ascii=True), flush=True)
    print(json.dumps({"approvalActionsAttempted": len(selected)}), flush=True)
