"""Exercise the packaged server without an AI account or an HTTP transport."""

import argparse
import json
from pathlib import Path
from queue import Queue
import subprocess
import tempfile
import threading


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mcp", type=Path, required=True)
    parser.add_argument("--editor-dir", type=Path, required=True)
    parser.add_argument("--java-bin", type=Path, required=True)
    parser.add_argument("--input", type=Path, required=True)
    args = parser.parse_args()
    source = args.input.resolve(strict=True)
    with tempfile.TemporaryDirectory(prefix="uyap-mcp-smoke-") as scratch:
        output = Path(scratch, "output")
        state = Path(scratch, "state")
        output.mkdir()
        state.mkdir()
        command = [
            str(args.mcp.resolve(strict=True)), "--editor-dir", str(args.editor_dir.resolve(strict=True)),
            "--java-bin", str(args.java_bin.resolve(strict=True)), "--input-root", str(source.parent),
            "--output-root", str(output), "--state-dir", str(state),
        ]
        with subprocess.Popen(command, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                              text=True, encoding="utf-8", bufsize=1) as server:
            replies = Queue()

            def receive():
                for line in server.stdout:
                    replies.put(line)
                replies.put(None)

            threading.Thread(target=receive, daemon=True).start()
            next_id = 0

            def send(method, params, notification=False):
                nonlocal next_id
                request = {"jsonrpc": "2.0", "method": method, "params": params}
                if not notification:
                    next_id += 1
                    request["id"] = next_id
                server.stdin.write(json.dumps(request) + "\n")
                server.stdin.flush()
                if notification:
                    return None
                line = replies.get(timeout=150)
                assert line is not None, f"Server closed stdout during {method}"
                response = json.loads(line)
                assert response.get("id") == next_id and "error" not in response, response
                return response["result"]

            def tool(name, **kwargs):
                result = send("tools/call", {"name": name, "arguments": kwargs})
                assert not result.get("isError"), result
                return result.get("structuredContent") or json.loads(result["content"][0]["text"])

            try:
                init = send("initialize", {"protocolVersion": "2025-06-18", "capabilities": {},
                                           "clientInfo": {"name": "uyap-local-smoke", "version": "1"}})
                send("notifications/initialized", {}, notification=True)
                tools = {entry["name"] for entry in send("tools/list", {})["tools"]}
                assert {"uyap_open", "uyap_go_to_page", "uyap_export_pdf", "uyap_close"} <= tools
                original = source.read_bytes()
                missing = send("tools/call", {
                    "name": "uyap_open", "arguments": {"path": str(Path(scratch, "outside.udf"))},
                })
                assert missing.get("isError")
                message = missing["content"][0]["text"]
                assert "PATH_OUTSIDE_SCOPE: Use an absolute .udf path under the configured folder." in message
                document_id = tool("uyap_open", path=str(source))["documentId"]
                assert tool("uyap_go_to_page", documentId=document_id, page=3)["page"] == 3
                pdf = output / "kurgu.pdf"
                result = tool("uyap_export_pdf", documentId=document_id, outputPath=str(pdf))
                assert result["pageCount"] == 3 and pdf.read_bytes().startswith(b"%PDF-")
                tool("uyap_close", documentId=document_id)
                assert source.read_bytes() == original
                print(json.dumps({"transport": "stdio", "protocol": init["protocolVersion"], "tools": len(tools),
                                  "page": 3, "pdfPages": result["pageCount"], "sourceUnchanged": True,
                                  "formattedResourceError": True}))
            finally:
                server.stdin.close()
                try:
                    server.wait(timeout=15)
                except subprocess.TimeoutExpired:
                    server.kill()
                    server.wait()


if __name__ == "__main__":
    main()
