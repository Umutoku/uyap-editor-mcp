# Kod kalitesi / Code quality

Windows PowerShell'de çözüm kökünde çalıştır.

```powershell
dotnet format whitespace ./UyapEditorMcp.sln --verify-no-changes
dotnet build ./UyapEditorMcp.sln -c Release -p:CodeQualityAnalysis=true
```

`.editorconfig` C# biçimini tanımlar. İkinci komut resmî Sonar C# analizörünü derleme sırasında yükler ve tekrar eden sabit metinler, bilişsel karmaşıklık ve diğer bakım uyarılarını raporlar. Analizör bu komut için derleme bağımlılığıdır. MCP çalışma paketine dahil edilmez. [dotnet format](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-format) · [Sonar C# analizörü](https://github.com/SonarSource/sonar-dotnet)

20 Kasım 2026'da ilk yerel analiz 55 uyarı, düzenlemelerden sonraki analiz 47 uyarı bildirdi. Bilişsel karmaşıklık (14 uyarı) ve boş gövdeler (11 uyarı) açık. Roslyn MCP derleyici tanılamasında hata veya uyarı bulunmadı.

Gözden geçirmede DTO, sonuç kaydı, servis ve repository'nin ayrı sorumluluk taşıdığını doğrula. Ortak yordamı aynı davranış iki yerde gerektiğinde çıkar. Yeni bir katman, interface veya seçenek için mevcut çağrılardan örnek göster. Analiz uyarılarını, kullanıcının ihtiyaç duyduğu davranış ve .NET kabul sonuçlarıyla birlikte değerlendir.

SonarQube MCP, kurulmuş bir SonarQube Server veya Cloud hesabından bulgu okuyabilir. Buradaki inceleme komutu yerel Roslyn analizörüyle çalışır. GitHub CodeQL güvenlik ve bağlam raporları için ayrıdır. [SonarQube MCP](https://github.com/SonarSource/sonarqube-mcp-server) · [CodeQL](https://github.com/Umutoku/uyap-editor-mcp/security/code-scanning)

Katman bağımlılığı için [ArchUnitNET](https://github.com/TNG/ArchUnitNET) tür bağımlılıklarını test edebilir. Yeni bir kural, çözümdeki fiilî katman sınırını tarif etmelidir. DRY, YAGNI, KISS ve SOLID için kod incelemesi de gerekir.
