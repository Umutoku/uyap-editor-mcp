import hashlib
import json
import uuid


def accept_archive(tool, input_root, output_root, source):
    password_file = input_root / "kurgu-arsiv-parola.txt"
    wrong_file = input_root / "kurgu-yanlis-parola.txt"
    password_file.write_text("Kurgu-Şİfre " + uuid.uuid4().hex + "\n", encoding="utf-8")
    wrong_file.write_text(uuid.uuid4().hex, encoding="utf-8")
    stamp = uuid.uuid4().hex[:8]
    archive = output_root / f"kurgu-korumali-{stamp}.7z"
    restored = output_root / f"kurgu-kurtarilan-{stamp}.udf"
    original = source.read_bytes()
    try:
        packed = tool("uyap_encrypt_7z", udfPath=str(source), outputPath=str(archive), passwordFile=str(password_file))
        assert packed["format"] == "7z" and archive.read_bytes().startswith(b"7z\xbc\xaf\x27\x1c")
        assert packed["sha256"] == hashlib.sha256(archive.read_bytes()).hexdigest().upper()
        tool("uyap_extract_7z", expected_error="SEVEN_ZIP_FAILED", archivePath=str(archive),
             outputPath=str(restored), passwordFile=str(wrong_file))
        assert not restored.exists()
        extracted = tool("uyap_extract_7z", archivePath=str(archive), outputPath=str(restored), passwordFile=str(password_file))
        assert extracted["format"] == "udf" and restored.read_bytes() == original == source.read_bytes()
        document = tool("uyap_open", path=str(restored))["documentId"]
        assert tool("uyap_go_to_page", documentId=document, page=3)["page"] == 3
        tool("uyap_close", documentId=document)
        tool("uyap_extract_7z", expected_error="OUTPUT_EXISTS", archivePath=str(archive),
             outputPath=str(restored), passwordFile=str(password_file))
        print(json.dumps({"archiveRegression": "passed", "archive": str(archive), "udfSha256": extracted["sha256"],
                          "wrongPasswordRejected": True, "sameUdfBytes": True, "reopenedPage": 3}), flush=True)
    finally:
        password_file.unlink(missing_ok=True)
        wrong_file.unlink(missing_ok=True)
