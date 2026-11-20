import argparse
import hashlib
import json
from pathlib import Path
import queue
import re
import shutil
import subprocess
import threading
import time
import uuid
import zipfile
from xml.etree import ElementTree

def toml_string(value):
    return json.dumps(str(value), ensure_ascii=False)

class AppServer:
    def __init__(self, executable, config, cwd, runtime):
        runtime.mkdir(parents=True, exist_ok=True)
        overrides = ["-c", config, "-c", "sqlite_home=" + toml_string(runtime),
                     "-c", "log_dir=" + toml_string(runtime)]
        self.process = subprocess.Popen([str(executable), *overrides, "app-server"], cwd=cwd,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, encoding="utf-8",
            errors="replace", text=True, creationflags=subprocess.CREATE_NO_WINDOW)
        self.sequence = 0
        self.messages = queue.Queue()
        self.errors = []
        threading.Thread(target=self.receive, daemon=True).start()
        threading.Thread(target=self.drain_errors, daemon=True).start()

    def receive(self):
        for line in self.process.stdout:
            try:
                self.messages.put(json.loads(line))
            except ValueError:
                continue
        self.messages.put(None)

    def drain_errors(self):
        for line in self.process.stderr:
            self.errors.append(line.rstrip())

    def send(self, message):
        self.process.stdin.write(json.dumps(message, ensure_ascii=True) + "\n")
        self.process.stdin.flush()

    def call(self, method, params=None, timeout=95):
        self.sequence += 1
        i = self.sequence
        data = dict(jsonrpc="2.0", id=i, method=method)
        if params is not None:
            data["params"] = params
        self.send(data)
        deadline = time.monotonic() + timeout
        while True:
            response = self.messages.get(timeout=max(.01, deadline-time.monotonic()))
            if response is None:
                raise RuntimeError(f"Codex App Server exited at {method}: {self.errors[-5:]}")
            if response.get("id") != i:
                continue
            if "error" in response:
                raise RuntimeError(f"{method}: {response['error']}")
            return response["result"]

    def close(self):
        self.process.stdin.close()
        try:
            self.process.wait(timeout=8)
        except subprocess.TimeoutExpired:
            self.process.terminate()
            self.process.wait(timeout=6)

def accept_spelling(tool, base, input_root):
    original = "Merhaba dünyya. Kitapp güzel. ABCDEF! 101. kitapp ... "
    document_id = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text=original)
    first = tool("uyap_spell_check", documentId=document_id, issueLimit=1)
    assert not first["complete"] and first["nextStart"] == 14
    issue, = first["issues"]
    assert (issue["start"], issue["end"], issue["word"]) == (8, 14, "dünyya")
    assert "dünya" in issue["suggestions"]
    remaining = tool("uyap_spell_check", documentId=document_id, start=first["nextStart"])
    assert remaining["complete"]
    assert [item["word"] for item in remaining["issues"]] == ["Kitapp", "kitapp"]
    correction = dict(documentId=document_id, start=8, end=14, expectedWord="dünyya", suggestion="dünya")
    tool("uyap_apply_spelling", expected_error="SPELL_SUGGESTION_INVALID",
         **dict(correction, suggestion="örnek"))
    tool("uyap_apply_spelling", **correction)
    tool("uyap_apply_spelling", expected_error="SPELL_WORD_CHANGED", **correction)
    path = base / f"kurgu-yazim-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document_id, outputPath=str(path))
    tool("uyap_close", documentId=document_id)
    copied = shutil.copyfile(path, input_root / path.name)
    reopened_id = tool("uyap_open", path=str(copied))["documentId"]
    assert tool("uyap_read_text", documentId=reopened_id)["text"].strip() == original.replace("dünyya", "dünya").strip()
    result = tool("uyap_spell_check", documentId=reopened_id)
    assert [item["word"] for item in result["issues"]] == ["Kitapp", "kitapp"]
    tool("uyap_close", documentId=reopened_id)
    print(json.dumps({"spellingPassed": True}, ensure_ascii=True), flush=True)


def accept_comments(tool, base, input_root):
    body = "Birinci bölüm. İkinci bölüm."
    document_id = tool("uyap_new")["documentId"]
    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text=body)
    tool("uyap_add_comment", expected_error="COMMENT_TEXT_NOT_SERIALIZABLE",
         documentId=document_id, start=0, end=6, text="Örnek ]]>")
    assert tool("uyap_list_comments", documentId=document_id)["total"] == 0
    first = tool("uyap_add_comment", documentId=document_id, start=0, end=6,
                 text="İlk kurgu açıklama\nİkinci satır")
    second = tool("uyap_add_comment", documentId=document_id, start=15, end=21,
                  text="Ayrı kurgu açıklama", type=2)
    assert (first["start"], first["end"], first["type"]) == (0, 6, 1)
    assert (second["start"], second["end"], second["type"]) == (15, 21, 2)
    assert tool("uyap_list_comments", documentId=document_id, offset=1, limit=1)["comments"] == [second]
    fixture = base / f"kurgu-yorum-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document_id, outputPath=str(fixture))
    tool("uyap_close", documentId=document_id)

    copied = shutil.copyfile(fixture, input_root / fixture.name)
    digest = hashlib.sha256(Path(copied).read_bytes()).digest()
    document_id = tool("uyap_open", path=str(copied), editable=True)["documentId"]
    assert tool("uyap_list_comments", documentId=document_id)["comments"] == [first, second]
    target = dict(documentId=document_id, index=first["index"], expectedStart=0, expectedEnd=6,
                  expectedText=first["text"], text="Güncellenen açıklama\nŞüphe ve çözüm")
    tool("uyap_edit_comment", expected_error="COMMENT_STALE", **dict(target, expectedEnd=5))
    changed = tool("uyap_edit_comment", **target)
    assert changed == dict(first, text=target["text"])
    tool("uyap_edit_comment", expected_error="COMMENT_STALE", **target)
    edited = base / f"kurgu-yorum-duzenlenmis-{uuid.uuid4().hex[:8]}.udf"
    tool("uyap_save_copy", documentId=document_id, outputPath=str(edited))
    tool("uyap_close", documentId=document_id)
    assert hashlib.sha256(Path(copied).read_bytes()).digest() == digest
    with zipfile.ZipFile(edited) as archive:
        assert set(archive.namelist()) == {"content.xml", "comments.xml"}
        markup = ElementTree.fromstring(archive.read("comments.xml"))
        assert changed["text"] in markup.findtext("content") and second["text"] in markup.findtext("content")
        assert [(item.get("strtOffs"), item.get("endOffs"), item.get("type"))
                for item in markup.findall(".//comment")] == [("0", "6", "1"), ("15", "21", "2")]
        unsigned_guard = input_root / "kurgu-ek-uyeli.udf"
        with zipfile.ZipFile(unsigned_guard, "w") as extra:
            for member in archive.namelist():
                extra.writestr(member, archive.read(member))
            extra.writestr("sign.sgn", "Kurgu ek üye")
    tool("uyap_open", expected_error="UDF_EDIT_UNVERIFIED", path=str(unsigned_guard), editable=True)
    read_only = shutil.copyfile(edited, input_root / edited.name)
    document_id = tool("uyap_open", path=str(read_only))["documentId"]
    assert tool("uyap_list_comments", documentId=document_id)["comments"] == [changed, second]
    assert tool("uyap_read_text", documentId=document_id)["text"].strip() == body
    tool("uyap_edit_comment", expected_error="READ_ONLY_SESSION",
         **dict(target, documentId=document_id, expectedText=changed["text"]))
    tool("uyap_close", documentId=document_id)
    print(json.dumps({"commentsPassed": True}, ensure_ascii=True), flush=True)


def accept_template_forms(tool, base):
    document_id = tool("uyap_open_template")["documentId"]

    def open_form(action):
        return tool("uyap_open_form", documentId=document_id, action=action)

    def control(form, role, operation, label=None, value=None):
        item = next(item for item in form["controls"] if item["role"] == role
                    and (label is None or item["label"] == label))
        args = dict(documentId=document_id, revision=form["revision"], controlId=item["id"], operation=operation)
        if value is not None:
            args["value"] = value
        return tool("uyap_form_input", **args)

    def add(action, name, toggles=()):
        form = control(open_form(action), "text", "write", value=name)
        for label in toggles:
            form = control(form, "toggle", "toggle", label, "true")
        assert control(form, "button", "press", "Ekle")["state"] == "closed"

    def save(label):
        path = base / f"kurgu-sablon-{label}-{uuid.uuid4().hex[:8]}.udf"
        tool("uyap_save_copy", documentId=document_id, outputPath=str(path))
        with zipfile.ZipFile(path) as archive:
            return path, ElementTree.fromstring(archive.read("content.xml"))

    def find(query):
        result = tool("uyap_find", documentId=document_id, query=query)
        assert result["found"], result

    tool("uyap_replace_text", documentId=document_id, start=0, end=0, text="KURGU ŞABLON\n")
    tool("uyap_run_action", documentId=document_id, action="caret-end")
    add("add-field", "KURGU_ETIKET", ("Etiket",))
    end = tool("uyap_read_text", documentId=document_id)["totalCharacters"]
    tool("uyap_replace_text", documentId=document_id, start=end, end=end, text="\n")
    add("add-field", "KURGU_LISTE", ("Liste", "Hayır"))
    find("KURGU_LISTE")
    add("add-field-group", "KURGU_ALAN_GRUBU")
    find("KURGU_ETIKET")
    add("add-paragraph-field-group", "KURGU_PARAGRAF_GRUBU",
        ("Tekrarlayan Alan Grubu", "Etiket Tekrarı"))
    tool("uyap_run_action", documentId=document_id, action="template-elementMarker-switch")
    blank_field = open_form("add-field")
    assert tool("uyap_form_input", documentId=document_id, revision=blank_field["revision"],
                controlId=-1, operation="cancel")["state"] == "closed"
    palette = open_form("template-color")
    palette = control(palette, "choice", "choose", value="Parametre")
    palette = control(palette, "color", "color", value="#123456")
    palette = control(palette, "button", "press", "Kaydet")
    assert control(palette, "button", "press", "Tamam")["state"] == "closed"
    palette = control(open_form("template-color"), "choice", "choose", value="Parametre")
    assert any(item["role"] == "color" and item["value"] == "#123456" for item in palette["controls"])
    assert tool("uyap_form_input", documentId=document_id, revision=palette["revision"],
                controlId=-1, operation="cancel")["state"] == "closed"
    path, markup = save("gruplu")
    label = markup.find(".//field[@fieldName='KURGU_ETIKET']")
    assert label.get("fieldType") == "2"
    parameter = markup.find(".//field[@fieldName='KURGU_LISTE']")
    assert parameter.get("fieldType") == "1" and parameter.get("isList") == "true"
    assert parameter.get("isErasable") == "false" and parameter.get("fieldGroupName") == "KURGU_ALAN_GRUBU"
    group = markup.find(".//paragraph[@GroupName='KURGU_PARAGRAF_GRUBU']")
    assert group.get("RepeatingLabel") == "true"
    tool("uyap_close", documentId=document_id)
    document_id = tool("uyap_open_template", path=str(path))["documentId"]
    assert "KURGU_LISTE" in tool("uyap_read_text", documentId=document_id)["text"]
    pdf_path = path.with_suffix(".pdf")
    pdf = tool("uyap_export_pdf", documentId=document_id, outputPath=str(pdf_path))
    from pypdf import PdfReader
    assert pdf["pageCount"] == len(PdfReader(pdf_path, strict=True).pages) == 1

    find("KURGU_LISTE")
    tool("uyap_run_action", documentId=document_id, action="remove-field-group")
    find("KURGU_ETIKET")
    tool("uyap_run_action", documentId=document_id, action="remove-paragraph-field-group")
    _, removed = save("gruplar-kaldirildi")
    assert removed.find(".//field[@fieldName='KURGU_LISTE']").get("fieldGroupName") != "KURGU_ALAN_GRUBU"
    assert removed.find(".//paragraph[@GroupName='KURGU_PARAGRAF_GRUBU']") is None
    tool("uyap_run_action", documentId=document_id, action="caret-end")
    end = tool("uyap_read_text", documentId=document_id)["totalCharacters"]
    tool("uyap_replace_text", documentId=document_id, start=end, end=end, text="\n")
    tool("uyap_reuse_template_field", expected_error="TEMPLATE_FIELD_NOT_FOUND",
         documentId=document_id, fieldName="KURGU_OLMAYAN")
    tool("uyap_reuse_template_field", documentId=document_id, fieldName="KURGU_ETIKET")
    _, reused = save("alan-tekrar")
    duplicates = reused.findall(".//field[@fieldName='KURGU_ETIKET']")
    assert len(duplicates) == 2 and all(field.get("fieldType") == "2" for field in duplicates)
    assert open_form("template-convert")["state"] == "completed"
    _, converted = save("cevrildi")
    assert converted.find(".//field[@fieldName='KURGU_LISTE']") is not None
    assert open_form("template-unconvert")["state"] == "completed"
    _, plain = save("normal")
    assert plain.find(".//field") is None and "KURGU" in plain.findtext("content")
    tool("uyap_close", documentId=document_id)

    document_id = tool("uyap_open_template")["documentId"]
    assert open_form("field-erasable")["state"] == "completed"
    warning = open_form("field-erasable")
    assert warning["title"] == "Uyarı"
    assert control(warning, "button", "press", "Tamam")["state"] == "closed"
    ribbon = tool("uyap_ribbon", documentId=document_id, tab="Şablon")
    assert any(item["label"] == "Alanlar Silinemesin" for item in ribbon["commands"])
    table = open_form("table-insert")
    table = control(table, "button", "press", "Tamam")
    if table["state"] == "open":
        table = control(table, "button", "press", "Ekle")
    assert table["state"] == "closed"
    tool("uyap_run_action", documentId=document_id, action="caret-begin")
    tool("uyap_run_action", documentId=document_id, action="table-erasable")
    _, locked = save("tablo-kilitli")
    assert locked.find(".//table").get("isEditable") == "false"
    tool("uyap_close", documentId=document_id)
    print(json.dumps({"templateFormsPassed": True, "templateUdf": str(path)}, ensure_ascii=True), flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--codex", required=True)
    parser.add_argument("--mcp", required=True)
    parser.add_argument("--editor-dir", required=True)
    parser.add_argument("--java-bin", required=True)
    parser.add_argument("--input", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--state-dir", required=True)
    parser.add_argument("--codex-runtime")
    parser.add_argument("--extended", action="store_true")
    parser.add_argument("--media-batch", action="store_true")
    parser.add_argument("--advanced", action="store_true")
    parser.add_argument("--formats", action="store_true")
    parser.add_argument("--editor-forms", action="store_true")
    parser.add_argument("--menu-inventory", action="store_true")
    parser.add_argument("--native-renderers", action="store_true")
    parser.add_argument("--local-actions", action="store_true")
    parser.add_argument("--print-preview", action="store_true")
    parser.add_argument("--format-actions", action="store_true")
    parser.add_argument("--backup-recovery", action="store_true")
    parser.add_argument("--individual-diff", action="store_true")
    parser.add_argument("--automatic-text", action="store_true")
    parser.add_argument("--format-palettes", action="store_true")
    parser.add_argument("--list-numbering", action="store_true")
    parser.add_argument("--continue-numbering", action="store_true")
    parser.add_argument("--custom-list", action="store_true")
    parser.add_argument("--mixed-list", action="store_true")
    parser.add_argument("--list-indents", action="store_true")
    parser.add_argument("--pending-actions", action="store_true")
    parser.add_argument("--pending-action", action="append", default=[])
    parser.add_argument("--open-actions", action="store_true")
    parser.add_argument("--footer-regression", action="store_true")
    parser.add_argument("--dirty-pdf-regression", action="store_true")
    parser.add_argument("--remaining-actions", action="store_true")
    parser.add_argument("--residual-actions", action="store_true")
    parser.add_argument("--approval-actions", action="store_true")
    parser.add_argument("--signed-imz-fixture", type=Path)
    parser.add_argument("--archive-regression", action="store_true")
    parser.add_argument("--approval-action", action="append", default=[])
    parser.add_argument("--approval-group", action="append", default=[],
                        choices=("core", "mail", "print", "security"))
    parser.add_argument("--residual-action", action="append", default=[])
    parser.add_argument("--residual-group", action="append", default=[],
                        choices=("core", "lifecycle", "workflow", "security"))
    parser.add_argument("--remaining-action", action="append", default=[])
    parser.add_argument("--open-action", action="append", default=[])
    parser.add_argument("--open-action-group", action="append", default=[],
                        choices=("appearance", "interface", "special"))
    options = parser.parse_args()
    original = Path(options.input).resolve(strict=True)
    base = Path(options.output_dir).resolve()
    state = Path(options.state_dir).resolve()
    base.mkdir(parents=True, exist_ok=True)
    state.mkdir(parents=True, exist_ok=True)
    input_root = state / ("fixtures-" + uuid.uuid4().hex[:8])
    input_root.mkdir()
    source = Path(shutil.copyfile(original, input_root / original.name))
    for fixture in ("sentetik-gorsel.png", "sentetik-yerel.xml", "sentetik-yerel.rtf"):
        if (original.parent / fixture).is_file():
            shutil.copyfile(original.parent / fixture, input_root / fixture)
    target = base / ("Codex doğrulama " + uuid.uuid4().hex[:8] + ".pdf")
    server_args = ["--editor-dir", options.editor_dir, "--java-bin", options.java_bin,
                   "--input-root", str(source.parent), "--output-root", str(base), "--state-dir", str(state)]
    if options.backup_recovery:
        backup_root = input_root / "native-backups"
        backup_root.mkdir()
        server_args.extend(["--backup-root", str(backup_root)])
    config = "mcp_servers={uyap_editor={command=" + toml_string(options.mcp) + ",args=[" + ",".join(
        map(toml_string, server_args)) + "],startup_timeout_sec=40,tool_timeout_sec=90,required=true}}"
    runtime = (Path(options.codex_runtime).resolve() if options.codex_runtime else
               state / ("codex-runtime-" + uuid.uuid4().hex[:8]))
    app = AppServer(options.codex, config, Path.cwd(), runtime)
    try:
        init = app.call("initialize", {"clientInfo":{"name":"uyap-local-acceptance","version":"1.0.0"},
                                       "capabilities":{"experimentalApi":True}})
        app.send({"jsonrpc":"2.0","method":"initialized"})
        thread = app.call("thread/start", {"ephemeral":True, "cwd":str(Path.cwd()), "approvalPolicy":"never"})
        threadId = thread["thread"]["id"]
        discovery = app.call("mcpServerStatus/list", {"threadId":threadId,"serverName":"uyap_editor","detail":"toolsAndAuthOnly"})
        statuses = discovery["data"]
        assert len(statuses)==1 and statuses[0]["name"]=="uyap_editor", discovery
        tools = sorted(statuses[0]["tools"])
        print(json.dumps(dict(codexServer=init.get("userAgent"), tools=tools),ensure_ascii=True),flush=True)
        assert {"uyap_open","uyap_go_to_page","uyap_export_pdf","uyap_get_page","uyap_close"}.issubset(tools)

        def tool(name, expected_error=None, **args):
            raw = app.call("mcpServer/tool/call", {"threadId":threadId,"server":"uyap_editor","tool":name,"arguments":args})
            if expected_error:
                assert raw.get("isError") and expected_error in json.dumps(raw), raw
                print(json.dumps({"tool":name,"expectedError":expected_error}),flush=True)
                return None
            assert not raw.get("isError"), f"{name}: {raw}"
            result = raw.get("structuredContent")
            if result is None:
                result = json.loads(raw["content"][0]["text"])
            response = dict(tool=name, result=result)
            if name in ("uyap_run_action", "uyap_open_form"):
                response["nativeAction"] = args["action"]
            print(json.dumps(response, ensure_ascii=True), flush=True)
            return result

        status = tool("uyap_status")
        assert status["editorVersion"]=="5.4.20",status
        expected_digest = hashlib.sha256(source.read_bytes()).hexdigest().upper()
        opened = tool("uyap_open", path=str(source))
        doc = opened["documentId"]
        assert opened["inputSha256"]==expected_digest and opened["position"]["page"]==1
        assert opened["position"]["pageCount"]==3
        first = tool("uyap_get_page",documentId=doc)
        assert first["page"] == 1 and first["pageCount"] == 3
        live = tool("uyap_list_actions",documentId=doc,offset=0,limit=3)
        assert live["total"] >= 181
        ribbon = tool("uyap_ribbon",documentId=doc,tab="Dosya")
        assert "Dosya" in ribbon["tabs"] and any(command["label"]=="PDF Olarak Kaydet" and command["enabled"]
                                                   for command in ribbon["commands"])
        tool("uyap_go_to_page",expected_error="PAGE_OUT_OF_RANGE",documentId=doc,page=4)
        third = tool("uyap_go_to_page",documentId=doc,page=3)
        assert third["page"] == third["pageCount"] == 3
        made = tool("uyap_export_pdf",documentId=doc,outputPath=str(target))
        assert made["pageCount"] == 3 and made["sourceUnchanged"] is True and target.is_file(), made
        data=target.read_bytes()
        assert data.startswith(b"%PDF-") and data.rstrip().endswith(b"%%EOF")
        assert hashlib.sha256(data).hexdigest().upper()==made["sha256"]
        from pypdf import PdfReader
        pages = [page.extract_text() for page in PdfReader(target, strict=True).pages]
        rows = [[int(n) for n in re.findall(r"SATIR\s+(\d{3})", text)] for text in pages]
        assert [len(row) for row in rows] == [47, 47, 21]
        assert sum(rows, []) == list(range(1, 116))
        assert [text.count("Çözüm örneği şüphe ıİ.") for text in pages] == [47, 47, 21]
        tool("uyap_export_pdf",expected_error="OUTPUT_EXISTS",documentId=doc,outputPath=str(target))
        assert tool("uyap_get_page",documentId=doc)["page"]==3
        assert tool("uyap_close",documentId=doc)["closed"]
        assert hashlib.sha256(source.read_bytes()).hexdigest().upper()==expected_digest
        print(json.dumps(dict(corePassed=True,pdf=str(target),desktopBridge=third["observedThrough"],
                              pdfSha256=made["sha256"],textRows=115),ensure_ascii=True),flush=True)

        if options.signed_imz_fixture:
            from signed_imz_acceptance import accept_signed_imz
            accept_signed_imz(tool, input_root, base, source, options.signed_imz_fixture)

        if options.archive_regression:
            from archive_acceptance import accept_archive
            accept_archive(tool, input_root, base, source)

        if options.editor_forms:
            accept_spelling(tool, base, input_root)
            accept_comments(tool, base, input_root)
            accept_template_forms(tool, base)

        if options.menu_inventory:
            from local_menu_acceptance import accept_menu_inventory
            accept_menu_inventory(tool, base, source)

        if options.print_preview:
            from local_menu_acceptance import accept_print_preview
            accept_print_preview(tool, source)

        if options.format_actions:
            from local_menu_acceptance import accept_format_actions
            accept_format_actions(tool, base)

        if options.backup_recovery:
            from backup_acceptance import accept_backup_recovery
            accept_backup_recovery(tool, base, source, state, backup_root)

        if options.individual_diff:
            from individual_diff_acceptance import accept_individual_diff
            accept_individual_diff(tool, base, input_root)

        if options.automatic_text:
            from automatic_text_acceptance import accept_automatic_text
            accept_automatic_text(tool, base, input_root)

        if options.format_palettes:
            from format_palettes_acceptance import accept_format_palettes
            accept_format_palettes(tool, base, input_root)

        if options.list_numbering:
            from list_numbering_acceptance import accept_list_numbering
            accept_list_numbering(tool, base)

        if options.continue_numbering:
            from list_numbering_acceptance import accept_continue_numbering
            accept_continue_numbering(tool, base)

        if options.custom_list:
            from custom_list_acceptance import accept_custom_list
            accept_custom_list(tool, base)

        if options.mixed_list:
            from custom_list_acceptance import accept_mixed_list
            accept_mixed_list(tool, base)

        if options.list_indents:
            from custom_list_acceptance import accept_list_indents
            accept_list_indents(tool, base)

        if options.pending_actions or options.pending_action:
            from pending_actions_acceptance import accept_pending_actions
            accept_pending_actions(tool, base, input_root, options.pending_action)

        if options.open_actions or options.open_action or options.open_action_group:
            from open_actions_acceptance import accept_open_actions
            groups = options.open_action_group or (("appearance", "interface", "special") if options.open_actions else ())
            accept_open_actions(tool, base, input_root, options.open_action, groups)

        if options.dirty_pdf_regression:
            from dirty_pdf_acceptance import accept_dirty_pdf
            accept_dirty_pdf(tool, base, source)

        if options.remaining_actions or options.remaining_action:
            from remaining_actions_acceptance import accept_remaining_actions
            accept_remaining_actions(tool, base, input_root, options.remaining_action)

        if options.residual_actions or options.residual_action or options.residual_group:
            from residual37_acceptance import accept_residual_actions
            accept_residual_actions(tool, base, input_root, options.residual_group, options.residual_action)

        if options.approval_actions or options.approval_action or options.approval_group:
            from approval16_acceptance import accept_approval_actions
            accept_approval_actions(tool, base, input_root, options.approval_group, options.approval_action)

        if options.footer_regression:
            from footer_acceptance import accept_footer
            accept_footer(tool, base, source)

        if options.native_renderers:
            from local_menu_acceptance import accept_native_renderers
            accept_native_renderers(tool, base)

        if options.local_actions:
            from local_menu_acceptance import accept_local_clipboard
            from local_text_acceptance import accept_comparison, accept_local_text
            from local_forms_acceptance import accept_local_forms
            from local_misc_acceptance import accept_local_misc

            accept_local_text(tool, base, input_root)
            accept_local_clipboard(tool, base)
            forms_report = accept_local_forms(tool, base, input_root)
            print(json.dumps(forms_report, ensure_ascii=True), flush=True)
            accept_local_misc(tool, base, input_root)
            accept_comparison(tool, base, input_root)

        if options.media_batch:
            stamp = uuid.uuid4().hex[:8]
            pdf_path = base / f"toplu-{stamp}.pdf"
            odt_path = base / f"toplu-{stamp}.odt"
            second = input_root / "sentetik-ikinci.udf"
            with zipfile.ZipFile(source) as original_archive, zipfile.ZipFile(second, "w", zipfile.ZIP_DEFLATED) as generated:
                for entry in original_archive.namelist():
                    data = original_archive.read(entry)
                    generated.writestr(entry, data.replace(b"SATIR 001", b"SATIR 901", 1) if entry == "content.xml" else data)
            batch = tool("uyap_batch_export", files=[
                {"path":str(source), "outputPath":str(pdf_path)},
                {"path":str(source), "outputPath":str(pdf_path)},
                {"path":str(second), "outputPath":str(odt_path)}])
            assert batch["completed"] == 2
            assert [item["succeeded"] for item in batch["files"]] == [True, False, True]
            assert batch["files"][1]["error"] == "OUTPUT_EXISTS"
            assert len(PdfReader(pdf_path, strict=True).pages) == 3
            with zipfile.ZipFile(odt_path) as archive:
                assert archive.read("mimetype") == b"application/vnd.oasis.opendocument.text"
                assert b"SATIR 901" in archive.read("content.xml")
            assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == expected_digest
            print(json.dumps({"batchPassed":True}),flush=True)
            image = source.parent / "sentetik-gorsel.png"
            editable = tool("uyap_new")["documentId"]
            inserted = tool("uyap_insert_image", documentId=editable, path=str(image))
            assert inserted["state"] == "image_inserted"
            image_pdf = base / f"gorsel-{stamp}.pdf"
            tool("uyap_export_pdf", documentId=editable, outputPath=str(image_pdf))
            embedded = [item.image.convert("RGB") for page in PdfReader(image_pdf, strict=True).pages for item in page.images]
            palettes = [bitmap.getcolors(bitmap.width * bitmap.height) for bitmap in embedded]
            assert any(any(r > g + 35 and r > b + 20 for _, (r, g, b) in colors) and
                       any(g > r + 45 and b > r + 45 for _, (r, g, b) in colors) for colors in palettes)
            saved_image = base / f"gorsel-{stamp}.udf"
            tool("uyap_save_copy", documentId=editable, outputPath=str(saved_image))
            with zipfile.ZipFile(saved_image) as archive:
                assert re.search(br"<image(?:\s|>)", archive.read("content.xml"), re.IGNORECASE)
            background = tool("uyap_insert_image", documentId=editable, path=str(image), background=True)
            assert background["state"] == "background_dialog_applied"
            background_udf = base / f"arka-plan-{stamp}.udf"
            tool("uyap_save_copy", documentId=editable, outputPath=str(background_udf))
            with zipfile.ZipFile(background_udf) as archive:
                assert b"bgImage" in archive.read("content.xml")
            tool("uyap_close", documentId=editable)
            print(json.dumps({"imagePassed":True,"backgroundPassed":True,"imagePdf":str(image_pdf)},ensure_ascii=True),flush=True)

        if options.advanced:
            macro_doc = tool("uyap_new")["documentId"]
            tool("uyap_replace_text", documentId=macro_doc, start=0, end=0, text="abc def")
            ranges = dict(documentId=macro_doc, recordStart=0, recordEnd=3, replayStart=4, replayEnd=7)
            tool("uyap_run_macro", expected_error="MACRO_ACTION_UNAVAILABLE", actions=["save-pdf"], **ranges)
            ran = tool("uyap_run_macro", actions=["changeCase-upper"], **ranges)
            assert ran["recordedSteps"] == 1
            assert tool("uyap_read_text", documentId=macro_doc)["text"] == "ABC DEF"
            tool("uyap_replace_text", documentId=macro_doc, start=7, end=7, text="\nikinci satır")
            tool("uyap_find", documentId=macro_doc, query="ABC DEF")
            tool("uyap_run_action", documentId=macro_doc, action="font-bulleted")
            tool("uyap_find", documentId=macro_doc, query="ikinci satır")
            tool("uyap_run_action", documentId=macro_doc, action="font-numbered")
            tool("uyap_run_action", documentId=macro_doc, action="caret-end")
            tool("uyap_run_action", documentId=macro_doc, action="insert-pageBreak")
            assert tool("uyap_get_page", documentId=macro_doc)["pageCount"] == 2
            macro_path = base / f"makro-{uuid.uuid4().hex[:8]}.udf"
            tool("uyap_save_copy", documentId=macro_doc, outputPath=str(macro_path))
            with zipfile.ZipFile(macro_path) as archive:
                xml = archive.read("content.xml")
                assert b'Bulleted="true"' in xml and b'Numbered="true"' in xml and b"<page-break" in xml
            tool("uyap_close", documentId=macro_doc)
            print(json.dumps({"macroPassed":True,"listsAndPageBreakPassed":True}),flush=True)

            for extension, marker, page_count in [
                ("xml", "UYAP MCP SENTETİK SATIR 001", 3), ("rtf", "ARANACAK-TEK-IFADE", 1)
            ]:
                imported = tool("uyap_import_document", path=str(input_root / f"sentetik-yerel.{extension}"))
                imported_id = imported["documentId"]
                assert imported["position"]["pageCount"] == page_count
                assert marker in tool("uyap_read_text", documentId=imported_id, limit=128)["text"]
                tool("uyap_list_formats", documentId=imported_id)
                tool("uyap_close", documentId=imported_id)
            print(json.dumps({"xmlAndRtfPassed":True}),flush=True)

            usf_view = tool("uyap_open", path=str(source))["documentId"]
            usf_output = base / f"kurgu-{uuid.uuid4().hex[:8]}.usf"
            tool("uyap_export_document", documentId=usf_view, outputPath=str(usf_output))
            tool("uyap_close", documentId=usf_view)
            usf_input = Path(shutil.copyfile(usf_output, input_root / usf_output.name))
            round_trip = tool("uyap_import_document", path=str(usf_input))
            assert round_trip["position"]["pageCount"] == 3
            usf_id = round_trip["documentId"]
            assert "UYAP MCP SENTETİK SATIR 001" in tool("uyap_read_text", documentId=usf_id, limit=128)["text"]
            tool("uyap_close", documentId=usf_id)
            print(json.dumps({"usfPassed":True}),flush=True)

            template = tool("uyap_open_template")
            template_id = template["documentId"]
            assert template["mode"] == "template"
            assert "Şablon" in tool("uyap_ribbon",documentId=template_id,tab="Şablon")["tabs"]
            tool("uyap_replace_text", documentId=template_id, start=0, end=0, text="KURGU ŞABLON\n")
            tool("uyap_run_action", documentId=template_id, action="caret-end")
            form = tool("uyap_open_form", documentId=template_id, action="add-field")
            assert form["state"] == "open"
            field = next(control for control in form["controls"] if control["role"] == "text")
            field_name = "KURGU_DOSYA_NUMARASI"
            form = tool("uyap_form_input", documentId=template_id, revision=form["revision"],
                        controlId=field["id"], operation="write", value=field_name)
            add = next(control for control in form["controls"] if control["label"] == "Ekle")
            assert tool("uyap_form_input",documentId=template_id,revision=form["revision"],
                        controlId=add["id"],operation="press")["state"] == "closed"
            template_path = base / f"kurgu-sablon-{uuid.uuid4().hex[:8]}.udf"
            tool("uyap_save_copy", documentId=template_id, outputPath=str(template_path))
            with zipfile.ZipFile(template_path) as archive:
                import xml.etree.ElementTree as et
                assert et.fromstring(archive.read("content.xml")).find(f".//field[@fieldName='{field_name}']") is not None
            tool("uyap_close", documentId=template_id)
            reopened = tool("uyap_open_template", path=str(template_path))
            assert reopened["mode"] == "template"
            assert field_name in tool("uyap_read_text", documentId=reopened["documentId"])["text"]
            tool("uyap_close", documentId=reopened["documentId"])
            print(json.dumps({"templatePassed":True, "templateUdf":str(template_path)},ensure_ascii=True),flush=True)

        if options.formats:
            from PIL import Image
            from xml.etree import ElementTree
            file_doc = tool("uyap_new")["documentId"]
            marker = "Çözüm örneği şüphe ıİ."
            tool("uyap_replace_text", documentId=file_doc, start=0, end=0, text=marker)
            stamp = uuid.uuid4().hex[:8]
            formats = {"txt":None, "xml":None, "png":"PNG", "jpg":"JPEG", "bmp":"BMP", "tiff":"TIFF", "tif":"TIFF"}
            for ext, raster in formats.items():
                output = base / f"bicim-{stamp}.{ext}"
                tool("uyap_export_document", documentId=file_doc, outputPath=str(output))
                if raster:
                    with Image.open(output) as bitmap:
                        assert bitmap.format == raster and min(bitmap.size) > 1
                        bitmap.load()
                elif ext == "txt":
                    assert marker in output.read_text(encoding="utf-8-sig")
                else:
                    assert ElementTree.parse(output).getroot().tag == "template"
            tool("uyap_save_copy", documentId=file_doc, outputPath=str(base / f"bicim-kaynak-{stamp}.udf"))
            tool("uyap_close", documentId=file_doc)
            print(json.dumps({"formatsPassed":True, "formats":list(formats)}),flush=True)

        if options.extended:
            extra = tool("uyap_new")
            edit_id = extra["documentId"]
            assert extra["position"]["pageCount"] == 1 and extra["editable"] is True
            sample = "MCP DÜZENLEME KABULÜ"
            tool("uyap_replace_text", documentId=edit_id, start=0, end=0, text=sample)
            match = tool("uyap_find", documentId=edit_id, query=sample)
            assert match["found"]
            tool("uyap_run_action", documentId=edit_id, action="font-bold")
            tool("uyap_run_action", documentId=edit_id, action="caret-end")
            form = tool("uyap_open_form", documentId=edit_id, action="table-insert")
            assert form["state"] == "open" and form["title"] == "Tablo Özellikleri"
            latest = tool("uyap_inspect_form", documentId=edit_id)
            tool("uyap_form_input", expected_error="STALE_DIALOG_STATE", documentId=edit_id,
                 revision=form["revision"], controlId=-1, operation="cancel")
            for control in [item for item in latest["controls"] if item["role"] == "text"][:2]:
                latest = tool("uyap_form_input", documentId=edit_id, revision=latest["revision"],
                              controlId=control["id"], operation="write", value="2")
            ok = next(item for item in latest["controls"] if item["role"] == "button" and item["label"] == "Tamam")
            form = tool("uyap_form_input", documentId=edit_id, revision=latest["revision"], controlId=ok["id"],
                        operation="press")
            if form["state"] == "open":
                assert form["title"].startswith("Tablo Özellikleri <Sabit>")
                insert = next(item for item in form["controls"] if item["role"] == "button" and item["label"] == "Ekle")
                form = tool("uyap_form_input", documentId=edit_id, revision=form["revision"], controlId=insert["id"],
                            operation="press")
            assert form["state"] == "closed"
            text = tool("uyap_read_text", documentId=edit_id, limit=128)
            assert sample in text["text"]
            stamp = uuid.uuid4().hex[:8]
            udf = base / (f"duzenleme-{stamp}.udf")
            saved = tool("uyap_save_copy", documentId=edit_id, outputPath=str(udf))
            assert Path(saved["path"]) == udf
            with zipfile.ZipFile(udf) as archive:
                content = archive.read("content.xml")
                assert sample in content.decode("utf-8-sig")
                assert re.search(br"<table(?:\s|>)", content, re.IGNORECASE), "Expected an actual native table"
            tool("uyap_close",documentId=edit_id)
            viewer = tool("uyap_open",path=str(source))["documentId"]
            odt = base / (f"donusturme-{stamp}.odt")
            tool("uyap_export_document", documentId=viewer, outputPath=str(odt))
            with zipfile.ZipFile(odt) as archive:
                assert archive.read("mimetype") == b"application/vnd.oasis.opendocument.text"
            handoff = tool("uyap_prepare_workflow", documentId=viewer, workflow="portal_save")
            assert handoff["status"] == "unavailable_in_current_mode"
            tool("uyap_close",documentId=viewer)
            print(json.dumps({"extendedPassed":True}),flush=True)
    finally:
        app.close()

if __name__ == "__main__":
    main()
