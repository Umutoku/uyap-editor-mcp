# Güvenlik kontrolleri / Security checks

20 Kasım 2026 · Windows x64 · 0.18.0

| Kontrol | Sonuç |
| --- | --- |
| Microsoft Defender özel taraması | Yayın klasörü, derlenmiş dosyalar ve yayın arşivleri tarandı. Tehdit bulunmadı. Çıkış kodu 0, imza sürümü 1.463.368.0. |
| NuGet güvenlik duyuruları | Doğrudan ve dolaylı .NET bağımlılıkları denetlendi. Yapılandırılmış kaynaklarda güvenlik açığı duyurusu bulunmadı. |
| OSV · 0.19 sürüm adayı | .NET 10.0.15 Windows çalışma zamanı dahil 34 NuGet paketi ve sürümü sorgulandı. Eşleşen duyuru dönmedi. [OSV sorgu API’si](https://google.github.io/osv.dev/post-v1-querybatch/) |
| Jotti · 0.18.0 ZIP | 14 motordan biri `PeFileDrop` bildirdi. 13 motor bulgu bildirmedi. [ZIP raporu](https://virusscan.jotti.org/en-US/filescanjob/3lbtyga51z) |
| VirusTotal · 0.18.0 ZIP | Aynı SHA-256 değerli arşiv için motorlar zararlı yazılım işaretlemedi. Hizmet çok dosyalı ve 3 MB üstü bu arşivde iç dosya taramasının sınırlı olduğunu bildirdi. [VirusTotal raporu](https://www.virustotal.com/gui/file/5e8468089baba1c666bc078d916feb9b20be948b8c96b4bb37b9ba2847a0abe2) |
| Jotti · 0.18.0 çalıştırılabilir dosya ve .NET DLL | Dosyalar ayrı tarandı. ZXCore bu dosya türlerinde sonuç vermedi. [Dosya raporları](https://virusscan.jotti.org/en-US/filescanjob/s9htmldv2e,vq1h7kkioi) |
| GitHub CodeQL ve Dependabot | Açık depoda etkin. İlk CodeQL koşusunun bulguları incelemede. [CodeQL koşusu](https://github.com/Umutoku/uyap-editor-mcp/actions/runs/44708516352) |
| 7-Zip çalıştırılabilir dosya yolu · 0.19 sürüm adayı | Yol, Windows’un makine düzeyindeki 64 bit kurulum kaydından okunur ve yerel, yönlendirilmemiş dosya olarak doğrulanır. Eski yol ve komut satırı uyarıları için yeni CodeQL sonucu bekleniyor. |
| Kaynak dosya kontrolü | Yayın kopyası yerel kullanıcı yolu ve yaygın anahtar biçimleri açısından tarandı. Release derlemesinde hata ayıklama simgeleri kapalı. Kurgu örnekler dışında belge, çalışma profili, tanılama günlüğü, özel anahtar ve UYAP JAR dosyaları dağıtıma alınmadı. |
| Yerel çalışma | Kurgu UDF, doğrudan başlatılan stdio sürecinde bir AI hesabı kullanılmadan açıldı ve PDF üretildi. |

Kontroller belirtilen derleme ve tarama tarihine aittir. Bir güvenlik sertifikası veya kapsamlı güvenlik incelemesi yerine geçmez. Kullandığın MCP istemcisinin ve UYAP Editör'ün veri akışı için [gizlilik sayfasına](../PRIVACY.md) bak.

[Microsoft Defender komut satırı](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus) · [NuGet güvenlik denetimi](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages)
