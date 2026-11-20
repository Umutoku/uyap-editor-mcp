def verify_comparison_navigation(suite, action):
    baseline_text = "Kurgu başlangıç.\nBirinci alan KARA bitiş.\nSabit ara.\nİkinci alan MAVİ bitiş.\nKurgu son.\n"
    current_text = baseline_text.replace("KARA", "PEMBE").replace("MAVİ", "TURUNCU")
    with suite.opened(baseline_text):
        baseline = suite.save("fark-gezinme-temel")
    with suite.opened(current_text):
        reference = {"baselinePath": str(baseline)}
        comparison = suite.call("compare_udf", **reference)
        groups = {}
        for change in comparison["changes"]:
            groups[change["changeId"]] = (change["groupStart"], change["groupEnd"])
        assert len(groups) == 2, comparison
        first, last = list(groups)
        direction = action.removesuffix("-changes")
        origin, destination = (first, last) if direction == "next" else (last, first)
        result = suite.call("compare_udf", **reference, navigationDirection=direction,
                            navigationFromChangeId=origin, limit=1)
        navigation = result["navigation"]
        assert navigation["found"] and navigation["action"] == action, navigation
        assert navigation["observedThrough"] == "native comparison action", navigation
        assert navigation["changeId"] == destination, navigation
        assert (navigation["start"], navigation["end"]) == groups[destination], navigation
        bound = suite.call("compare_udf", **reference, navigationDirection=direction,
                           navigationFromChangeId=destination)["navigation"]
        assert bound == {"action": action, "found": False}, bound
        assert suite.read_text() == current_text.rstrip("\n"), "Karşılaştırma çalışma belgesini değiştirdi."
    return f"Yerel {action} eylemi karşılaştırma penceresinde {groups[destination]} aralığını seçti."
