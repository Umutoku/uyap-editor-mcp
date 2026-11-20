def residual_cases(suite):
    return {"show-paragraph-properties": lambda action: paragraph_properties(suite, action)}


def paragraph_properties(suite, action):
    first = "KURGU ILK PARAGRAF"
    second = "KURGU IKINCI PARAGRAF"
    text = first + "\n" + second
    start = len(first) + 1

    def properties(offset):
        suite.select(offset)
        actual = suite.act(action)
        assert actual["selectionStart"] == actual["selectionEnd"] == offset, actual
        return dict(line.split("=", 1) for line in actual["diagnostic"].split("\n", 6))

    with suite.opened(text):
        original = properties(start + 2)
        suite.act("right-justify")
        suite.call("set_line_spacing", multiplier=2.0)
        changed = properties(start + 2)
        assert int(changed["start"]) == start and int(changed["end"]) == len(text), changed
        assert changed["alignment"] == "2" and float(changed["lineSpacingMultiplier"]) == 2, changed
        assert changed["attributes"] != original["attributes"], changed
        assert properties(2)["alignment"] == "0"
        saved = suite.save("paragraf-yerel-oznitelikler")
        with suite.reopened(saved):
            reopened = properties(start + 2)
            assert reopened["alignment"] == "2" and float(reopened["lineSpacingMultiplier"]) == 2, reopened
            assert suite.read_text() == text
    return {"readOnlyRoundTrip": True, "paragraphStart": start, "alignment": "right", "lineSpacing": 2}
