# Kod yapısı / Architecture

Visual Studio'da depo kökündeki `UyapEditorMcp.sln` dosyasını aç. Çözüm .NET MCP sunucusunu ve .NET kabul projesini içerir. Java köprüsünü ve .NET uygulamasını birlikte üretmek için [PowerShell derleme adımlarını](INSTALL.md#windows-powershell) kullan.

| Dizin | Sorumluluk |
| --- | --- |
| `Mcp/` | Uygulama başlangıcı, stdio bağlantısı ve MCP araç sözleşmeleri. |
| `Contracts/Dtos/` | MCP istemcisinden gelen istek modelleri. |
| `Models/Records/` | Araçların döndürdüğü sonuç modelleri. Her record ayrı dosyada durur. |
| `Services/` | Belge oturumu, onay akışları ve ayrı yerel 7z arşiv servisi. |
| `Repositories/` | Yerel dosya kapsamı, girdi denetimi, çıktı doğrulama ve dosya yazma. |
| `Interop/` | Java alt süreci, editör cevabı extension'ları, Windows Java Access Bridge ve DOC dönüştürme bağlayıcısı. |
| `Constants/` | UYAP eylem kimlikleri, dosya biçimleri, uzantılar ve sınırlar. |
| `Resources/` | Tipli hata tanımları, sabit hata kodları ve `EditorMessages.resx` içindeki mesajlar ile araç açıklamaları. |
| `java/` | UYAP'ın kendi menüleri ve belge bileşenleriyle çalışan Java köprüsü. |
| `tests/Acceptance/` | Resmî MCP C# istemcisiyle kurgu belge, kaynak mesaj, 7z ve onay testleri. |

Akış `MCP istemcisi → stdio → MCP aracı → yerel belge işlemi → Java köprüsü → UYAP Editör` şeklindedir. Oturum kilidi, eşzamanlı çağrıların aynı editör durumunu tutarlı kullanmasını sağlar. Çıktı doğrulaması tamamlandıktan sonra dosya seçilen hedefe yazılır.

Yeni bir kullanıcı hatası için mevcut hata tanımını kullan veya `EditorErrorCodes`, `EditorErrors` ve `EditorMessages.resx` dosyalarına tek bir tanım ekle. Eylem veya durum protokol değeri için `EditorActionIds.cs` içindeki sabitleri kullan.
