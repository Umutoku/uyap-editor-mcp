# UYAP Editor MCP

UYAP Doküman Editörü 5.4.20 için Windows x64 üzerinde çalışan yerel bir MCP sunucusu. Kurgu UDF açma, sayfa gezintisi, PDF ve IMZ dışa aktarma, belge düzenleme, şablonlar, toplu dönüştürme ve şifreli 7z arşivleri için 65 araç sunar.

Local Windows x64 MCP server for UYAP Document Editor 5.4.20. It uses the interactive editor for document editing and export. The server uses MCP over stdio and has no project-operated telemetry, hosted backend or document upload. [Privacy and client data flow](PRIVACY.md)

- [Windows PowerShell kurulumu](docs/INSTALL.md#windows-powershell)
- [Codex](docs/INSTALL.md#codex) · [Claude Code](docs/INSTALL.md#claude-code) · [Claude Desktop](docs/INSTALL.md#claude-desktop) · [Gemini CLI](docs/INSTALL.md#gemini-cli)
- [macOS üzerinden Windows oturumu](docs/INSTALL.md#macos)
- [Doğrulanan ve bekleyen testler](docs/TESTING.md)
- [Güvenlik kontrolleri](docs/SECURITY-CHECKS.md)
- [Kod yapısı ve Visual Studio](docs/ARCHITECTURE.md)
- [Kod kalitesi ve analiz komutları](docs/CODE-QUALITY.md)
- [275 eylemin kapsam tablosu](docs/uyap-5.4.20-actions.md)
- [GitHub Releases](https://github.com/Umutoku/uyap-editor-mcp/releases)

Bağımsız bir projedir. UYAP ve Java dosyalarını kendi kaynaklarından edinmen gerekir. [Resmî UYAP Editör sayfası](https://www.uyap.gov.tr/UYAP-Editor)

GitHub Releases'teki `0.18.0` paketinin çevrimiçi tarama bulgusu sürüm notunda yer alır. Güncel kaynak, ayrı .NET kabul projesini ve yeniden düzenlenen kod yapısını içerir. [Güvenlik kontrolleri](docs/SECURITY-CHECKS.md)

İlk deneme için istemciden depodaki `fixtures/sentetik-3-sayfa.udf` dosyasını `uyap_open` ile açmasını, üçüncü sayfaya gitmesini ve `uyap_export_pdf` ile yeni bir PDF oluşturmasını iste. Kendi belgelerini bağlamadan önce kurgu dosyayla kontrol et.
