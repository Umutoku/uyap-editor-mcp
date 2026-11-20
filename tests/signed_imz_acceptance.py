import hashlib
import json
import shutil
import uuid
import zipfile


def accept_signed_imz(tool, input_root, output_root, unsigned, fixture):
    signed = input_root / "kurgu-imzali.udf"
    shutil.copyfile(fixture, signed)
    original = signed.read_bytes()
    target = output_root / f"kurgu-resmi-kullanim-disi-{uuid.uuid4().hex[:8]}.imz"
    tool("uyap_open", expected_error="UDF_EDIT_UNVERIFIED", path=str(signed), editable=True)
    opened = tool("uyap_open", path=str(signed))
    document = opened["documentId"]
    try:
        assert not opened["editable"] and opened["position"]["pageCount"] == 3, opened
        assert opened["signatureStatus"] == "native_invalid_signature", opened
        assert tool("uyap_go_to_page", documentId=document, page=3)["page"] == 3
        result = tool("uyap_export_signed_imz", documentId=document, outputPath=str(target))
        output = target.read_bytes()
        assert result["editorCommand"] == "ribbon:Dosya/Dışa Aktar" and result["sourceUnchanged"], result
        assert result["bytes"] == len(output) and result["sha256"] == hashlib.sha256(output).hexdigest().upper()
        with zipfile.ZipFile(signed) as archive:
            assert output and archive.read("sign.sgn").endswith(output)
        assert signed.read_bytes() == original
        tool("uyap_export_signed_imz", expected_error="OUTPUT_EXISTS", documentId=document, outputPath=str(target))
        assert target.read_bytes() == output
    finally:
        tool("uyap_close", documentId=document)

    unsigned_document = tool("uyap_open", path=str(unsigned))["documentId"]
    absent = target.with_name(f"unsigned-{target.name}")
    try:
        tool("uyap_export_signed_imz", expected_error="SIGNED_UDF_REQUIRED", documentId=unsigned_document,
             outputPath=str(absent))
        assert not absent.exists()
    finally:
        tool("uyap_close", documentId=unsigned_document)
    print(json.dumps({"signedImzRegression": "passed", "signatureStatus": opened["signatureStatus"],
                      "output": str(target), "bytes": result["bytes"], "sha256": result["sha256"]}), flush=True)
