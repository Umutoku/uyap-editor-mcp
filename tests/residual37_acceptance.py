import json

from open_actions_acceptance import OpenActions


GROUPS = ("core", "lifecycle", "workflow", "security")


def accept_residual_actions(tool, base, input_root, groups, requested):
    suite = OpenActions(tool, base, input_root)
    cases = {}
    for group in groups or GROUPS:
        discovered = __import__(f"residual37_{group}").residual_cases(suite)
        assert not cases.keys() & discovered.keys(), group
        cases.update(discovered)
    selected = requested or list(cases)
    assert len(set(selected)) == len(selected) and set(selected) <= cases.keys(), selected
    for action in selected:
        try:
            evidence = cases[action](action)
            coverage = evidence.pop("coverage", "passed") if isinstance(evidence, dict) else "passed"
            assert coverage in ("passed", "prepared"), evidence
            outcome = {"status": coverage, "evidence": evidence}
        except (AssertionError, KeyError, LookupError, RuntimeError, ValueError) as error:
            outcome = {"status": "blocked", "detail": str(error) or type(error).__name__}
        print(json.dumps({"residualAction": action, **outcome}, ensure_ascii=True), flush=True)
    print(json.dumps({"residualActionsAttempted": len(selected)}), flush=True)
