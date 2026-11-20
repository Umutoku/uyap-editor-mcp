# Güvenlik kontrolleri / Security checks

20 Kasım 2026 · Windows x64 · 0.18.0

| Kontrol | Sonuç |
| --- | --- |
| Microsoft Defender özel taraması | Yayın klasörü, derlenmiş dosyalar ve yayın arşivleri tarandı. Tehdit bulunmadı. Çıkış kodu 0, imza sürümü 1.463.368.0. |
| NuGet güvenlik duyuruları | Doğrudan ve dolaylı .NET bağımlılıkları denetlendi. Yapılandırılmış kaynaklarda güvenlik açığı duyurusu bulunmadı. |
| Kaynak dosya kontrolü | Yayın kopyası yerel kullanıcı yolu ve yaygın anahtar biçimleri açısından tarandı. Kurgu örnekler dışında belge, çalışma profili, tanılama günlüğü, özel anahtar ve UYAP JAR dosyaları dağıtıma alınmadı. |
| Yerel çalışma | Kurgu UDF, doğrudan başlatılan stdio sürecinde bir AI hesabı kullanılmadan açıldı ve PDF üretildi. |

Kontroller belirtilen derleme ve tarama tarihine aittir. Bir güvenlik sertifikası veya kapsamlı güvenlik incelemesi yerine geçmez. Kullandığın MCP istemcisinin ve UYAP Editör'ün veri akışı için [gizlilik sayfasına](../PRIVACY.md) bak.

[Microsoft Defender komut satırı](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus) · [NuGet güvenlik denetimi](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
