import json
import re
import uuid

from pypdf import PdfReader


def accept_footer(tool, base, source):
    suffix = uuid.uuid4().hex[:8]
    revised = base / f"kurgu-alt-yazi-{suffix}.udf"
    exported = base / f"kurgu-alt-yazi-{suffix}.pdf"
    initial = "KURGU ILK ALT YAZI 731"
    final = "KURGU ALT YAZI 927"
    opened = tool("uyap_open", path=str(source), editable=True)
    document = opened["documentId"]
    assert opened["position"]["pageCount"] == 3

    def call(name, **kwargs):
        return tool("uyap_" + name, documentId=document, **kwargs)

    def footer():
        return next(region for region in call("page_regions")["regions"] if region["region"] == "footer")

    assert not footer()["present"]
    assert call("set_page_region", region="footer", text=initial)["text"] == initial
    assert call("page_region_range", region="footer", lastPage=2)["lastPage"] == 2
    assert call("set_page_region", region="footer", text="")["text"] == ""
    assert call("set_page_region", region="footer", text=final)["text"] == final
    assert call("page_region_range", region="footer")["lastPage"] == 2
    assert call("page_region_range", region="footer", lastPage=-1).get("lastPage") is None
    assert footer()["text"] == final
    call("save_copy", outputPath=str(revised))
    assert call("close")["closed"]
    document = tool("uyap_open", path=str(revised))["documentId"]
    try:
        assert footer()["text"] == final
        assert call("export_pdf", outputPath=str(exported))["pageCount"] == 3
        pages = [page.extract_text() for page in PdfReader(exported, strict=True).pages]
        assert all(page.count(final) == 1 and initial not in page for page in pages), pages
        assert [int(row) for page in pages for row in re.findall(r"SATIR\s+(\d{3})", page)] == list(range(1, 116))
        assert all(page.count("Çözüm örneği şüphe ıİ.") == count for page, count in zip(pages, (47, 47, 21)))
    finally:
        assert call("close")["closed"]
    print(json.dumps({"footerRegression": "passed", "pages": 3, "bodyRows": 115, "savedUdf": str(revised),
                      "nativePdf": str(exported)}, ensure_ascii=True), flush=True)
