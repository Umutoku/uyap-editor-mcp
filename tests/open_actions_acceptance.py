from contextlib import contextmanager
import json
import uuid

from pending_actions_acceptance import PendingActions


NAVIGATION = {
    "caret-backward": (4, (3, 3)),
    "caret-forward": (4, (5, 5)),
    "caret-begin-paragraph": (4, (0, 0)),
    "caret-end-paragraph": (4, (11, 11)),
    "caret-down": (4, (15, 15)),
    "caret-up": (15, (4, 4)),
    "selection-backward": (4, (3, 4)),
    "selection-forward": (4, (4, 5)),
    "selection-begin-paragraph": (4, (0, 4)),
    "selection-end-paragraph": (4, (4, 11)),
    "selection-down": (4, (4, 15)),
    "selection-up": (15, (4, 15)),
    "select-paragraph": (4, (0, 11)),
    "unselect": ((2, 6), (6, 6)),
}

TEXT_EDITS = {
    "delete-next": (5, "abcde ghij\nklmno pqrst\n"),
    "delete-previous": (5, "abcdf ghij\nklmno pqrst\n"),
    "delete-next-word": (7, "abcdef \nklmno pqrst\n"),
    "delete-previous-word": (6, " ghij\nklmno pqrst\n"),
    "insert-break": (5, "abcde\nf ghij\nklmno pqrst\n"),
    "insert-tab": (5, "abcde\tf ghij\nklmno pqrst\n"),
    "insert-tab-ex": (5, "abcde\tf ghij\nklmno pqrst\n"),
    "tab-shift": (5, "abcde\tf ghij\nklmno pqrst\n"),
    "default-typed": ((2, 6), "abŞ ghij\nklmno pqrst\n"),
    "insert-content": ((2, 6), "abKURGU ghij\nklmno pqrst\n"),
}
EDIT_INPUT = "abcdef ghij\nklmno pqrst\n"
NAV_INPUT = "abcdefghij\nabcdefghij\nabcdefghij\n"
HORIZONTAL = ("selection-page-left", "selection-page-right")
INTERNAL = {
    "add-selection": "Native cI action resolves the target editor and returns without changing selection.",
    "show-selection": "Native cW action body returns immediately.",
}
CASES = (*NAVIGATION, *HORIZONTAL, *TEXT_EDITS)


class OpenActions(PendingActions):
    @contextmanager
    def reopened(self, path):
        previous = self.document
        try:
            self.document = self.tool("uyap_open", path=str(path))["documentId"]
            try:
                yield
            finally:
                self.call("close")
        finally:
            self.document = previous

    def run(self, action):
        if action in NAVIGATION:
            return self.navigation(action)
        if action in HORIZONTAL:
            return self.horizontal(action)
        return self.change_text(action)

    def navigation(self, action):
        position, expected = NAVIGATION[action]
        if not hasattr(self, "navigation_seed"):
            with self.opened(NAV_INPUT):
                self.navigation_seed = self.save("gezinme-baslangic")
        with self.reopened(self.navigation_seed):
            self.select(*(position if isinstance(position, tuple) else (position,)))
            actual = self.act(action)
            view = self.call("view_state")
            observed = (actual["selectionStart"], actual["selectionEnd"])
            assert observed == expected, (action, observed, expected, view)
            assert observed == (view["selectionStart"], view["selectionEnd"]), view
            assert self.read_text() == NAV_INPUT.rstrip("\n")
        return {"range": observed, "caret": view["caret"], "readOnly": True}

    def horizontal(self, action):
        source = "abcdefghijklmnopqrstuvwxyz" * 6 + "\n"
        with self.opened(source):
            self.call("set_view", zoomPercent=300)
            right = action.endswith("right")
            start = 0 if right else 38
            self.select(start)
            before = self.call("view_state")
            after = self.act(action)
            view = self.call("view_state")
            observed = (after["selectionStart"], after["selectionEnd"])
            assert observed == (view["selectionStart"], view["selectionEnd"]), view
            assert (observed[0] == start < observed[1]) if right else (observed[0] < start == observed[1]), (
                action, before, after, view)
            assert self.read_text() == source.rstrip("\n")
        return {"origin": start, "range": observed, "zoom": view["zoomPercent"]}

    def change_text(self, action):
        position, expected = TEXT_EDITS[action]
        with self.opened(EDIT_INPUT):
            self.select(*(position if isinstance(position, tuple) else (position,)))
            if action == "default-typed":
                self.call("run_action", action=action, text="Ş")
            elif action == "insert-content":
                self.call("run_action", action=action, text="KURGU")
            else:
                self.act(action)
            observed = self.read_text()
            assert observed == expected.rstrip("\n"), (action, observed, expected)
            path = self.save(action)
            with self.reopened(path):
                assert self.read_text() == observed, (action, observed, self.read_text())
        return {"body": observed, "savedAndReopened": True}


def accept_open_actions(tool, base, input_root, requested, groups):
    suite = OpenActions(tool, base, input_root)
    actions = {action: suite.run for action in CASES}
    internal = dict(INTERNAL)
    for group in groups:
        module = __import__(f"open73_{group}")
        group_actions, group_internal = module.open_action_cases(suite)
        assert not set(actions).intersection(group_actions), group
        assert not set(internal).intersection(group_internal), group
        actions.update(group_actions)
        internal.update(group_internal)
    assert not set(actions).intersection(internal)
    chosen = requested or sorted(actions.keys() | internal.keys(), key=str.casefold)
    assert len(set(chosen)) == len(chosen) and set(chosen) <= actions.keys() | internal.keys(), chosen
    for action in chosen:
        if action in internal:
            outcome = {"status": "reviewed", "detail": internal[action]}
        else:
            try:
                evidence = actions[action](action)
                outcome = {"status": "passed", "evidence": evidence}
            except (AssertionError, KeyError, LookupError, RuntimeError, ValueError) as error:
                outcome = {"status": "blocked", "detail": str(error) or type(error).__name__}
        print(json.dumps({"openAction": action, **outcome}, ensure_ascii=True), flush=True)
    print(json.dumps({"openActionsAttempted": len(chosen)}), flush=True)
