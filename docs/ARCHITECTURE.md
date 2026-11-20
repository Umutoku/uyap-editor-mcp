# Kod yapısı / Architecture

Visual Studio'da depo kökündeki `UyapEditorMcp.sln` dosyasını aç. Çözüm tek .NET yürütülebilir projesini içerir. Java köprüsünü ve .NET uygulamasını birlikte üretmek için [PowerShell derleme adımlarını](INSTALL.md#windows-powershell) kullan.

| Dizin | Sorumluluk |
| --- | --- |
| `Mcp/` | Uygulama başlangıcı, stdio bağlantısı ve MCP araç sözleşmeleri. |
| `Editor/` | Yönetilen belge oturumu, belge işlemleri, kullanıcı onayı akışları ve dosya sınırları. Hata ve eylem kimlikleri, istemcilerde kullanılan aynı protokol değerlerini taşır. |
| `Interop/` | Java editör alt süreci, Windows Java Access Bridge ve DOC dönüştürme bağlayıcısı. |
| `Resources/` | Tipli hata tanımları, sabit hata kodları ve `EditorMessages.resx` içindeki kullanıcı mesajları. |
| `java/` | UYAP'ın kendi menüleri ve belge bileşenleriyle çalışan Java köprüsü. |
| `tests/` | Kurgu belgelerle doğrudan stdio ile Codex kabulü. |

Akış `MCP istemcisi → stdio → MCP aracı → yerel belge işlemi → Java köprüsü → UYAP Editör` şeklindedir. Oturum kilidi, eşzamanlı çağrıların aynı editör durumunu tutarlı kullanmasını sağlar. Çıktı doğrulaması tamamlandıktan sonra dosya seçilen hedefe yazılır.

Yeni bir kullanıcı hatası için mevcut hata tanımını kullan veya `EditorErrorCodes`, `EditorErrors` ve `EditorMessages.resx` dosyalarına tek bir tanım ekle. Eylem veya durum protokol değeri için `EditorActionIds.cs` içindeki sabitleri kullan.
