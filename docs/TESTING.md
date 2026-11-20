# Test durumu / Test status

UYAP Doküman Editörü 5.4.20 · Windows x64

Testler etkileşimli Windows masaüstünde çalıştırıldı. Windows sanal makinesi, gerçek akıllı kart ve nitelikli imza bu test ortamında yoktu.

**244/275** yerel UYAP eylem kaydında kurgu kabulü tamamlandı. Bu sayı, aynı işlev bir MCP yolu üzerinden doğrulandığında onu da kapsar. **65** MCP aracı Codex App Server tarafından listelendi.

| Durum | Doğrulanan adım |
| --- | --- |
| Codex Desktop / App Server | Gerçek MCP çağrılarıyla kurgu UDF açma, üçüncü sayfa, PDF, imzalı UDF’den IMZ ve 7z parola/doğru baytlarla geri yükleme geçti. |
| Doğrudan stdio | AI hesabı kullanmadan MCP başlatıldı. 65 araç listelendi, üçüncü sayfa açıldı ve üç sayfalık PDF üretildi. Kaynak UDF'nin baytları aynı kaldı. Geçersiz yol denemesinde sabit hata kodu ve kaynak dosyasından biçimlenen mesaj doğrulandı. |
| 0.19 çalışma zamanı | `DOTNET_ROOT` ve `DOTNET_ROOT_X64` boş bir dizine ayarlandı. MCP’nin stdio başlangıcı ve paketten yüklenen .NET 10.0.15 `coreclr.dll` doğrulandı. |
| 0.19 kurulum ve kaldırma | ZIP açıldı, çalışma zamanı dahil kuruldu ve yeniden kurulumda aynı paket kullanıldı. Kurulu kopyayla UYAP kabulü geçti. Kaldırma denemesi sırasında belgeler ve başka bir klasör korundu. |
| Yayın ZIP’i ve resmî UYAP dağıtımları | GitHub’dan indirilen ZIP’in SHA-256 değeri doğrulandı ve ayrı dizinde kuruldu. UYAP’ın güncel Windows MSI ve EXE paketlerinde MCP’nin kullandığı yedi JAR dosyası eşleşti. [Resmî UYAP indirmeleri](https://uyap.gov.tr/uyap-editor/) |
| MCP Inspector 2.16.0 | Yayın paketindeki 65 araç listelendi ve durum çağrısı geçti. Inspector, 25 aracın girdi veya çıktı şemasında 83 JSON Schema taşınabilirlik uyarısı bildirdi. [MCP Inspector](https://github.com/modelcontextprotocol/inspector) |
| Gemini CLI 0.60.0 | Ayrı proje yapılandırmasında yerel stdio sunucusu `Connected` olarak keşfedildi. Belge üzerinden uçtan uca Gemini testi bekliyor. |
| Claude Code 2.1.329 | Ayrı proje yapılandırmasında yerel stdio sunucusu `Connected` olarak keşfedildi. Belge üzerinden uçtan uca Claude Code testi bekliyor. |
| Claude Desktop | MCPB manifesti resmî CLI ile doğrulandı. Uygulama içinden kurulum ve belge testi bekliyor. |
| E-posta eki · 4 | UDF, DOC, ODT ve PDF için yerel `.eml` eki çözüldü ve dışa aktarılan dosyayla bayt bayt karşılaştırıldı. Bir istemciden teslimat bekliyor. |
| Yazdırma · 2 | Tekli ve toplu önizlemelerde dosya özeti, PDF başlığı ve üç sayfalık belge sayısı doğrulandı. Windows yazıcıya gönderim bekliyor. |
| Yerel ayarlar ve yardımcı işlemler · 4 | İmza kütüphanesi, proxy, yardım ve güncelleme için menü/hazırlık doğrulandı. Son işlem ve sonucu bekliyor. |
| İmza · 3 | Tekli, toplu ve mobil imza hazırlığı doğrulandı. Desteklenen e-imza veya mobil onay ile son adım bekliyor. |
| Sertifika ve portal · 2 | SoftHSM ve Kamu SM test zinciri ayrı olarak doğrulandı. UYAP'ın kendi Sertifikalar menüsünde tanıdığı okuyucu ve sertifika ile kabul bekliyor. Portal kaydı için yetkili kayıt ve oturum gerekiyor. |
| Editör içi · 15 | İç olay, etiket veya işleyici. Kullanıcı eylemlerinden ayrı kaydedildi. |
| Yerel şifreleme · 1 | UYAP 5.4.20 yöntemi devre dışı. MCP'nin AES korumalı 7z oluşturma ve açma akışı geçti. |

IMZ kabulü, bu test için üretilen ve Windows tarafından güvenilir sayılmayan CMS imzalı UDF’yi açar. Editörün Dışa Aktar komutundan çıkan IMZ baytları, UDF içindeki imza verisiyle karşılaştırılır. Kaynak UDF’nin değişmediği ayrıca kontrol edilir. Nitelikli sertifika güveni, kartla imza ve portal kaydı ayrı kabul konularıdır. [Eylem tablosu](uyap-5.4.20-actions.md)

`set-tooltip-enable` adlı UYAP 5.4.20 komutu yerel bir yardım ayarını değiştirmek yerine tarayıcıda eski yardım adresini açar. MCP onay hazırlığında güncel HTTPS yardım adresini gösterir. Yerel yardım ayarı için kabul sayısına eklenmedi.

Yayın ZIP’inden kurulan MCP, resmî EXE’nin UYAP dosyalarıyla genişletilmiş kabulü geçti. Kaydedilmemiş metin PDF’de göründü. Geri alma sonrasında metin PDF’den çıktı. İlk çıktı PDF şerit komutundan, ikinci çıktı editörün dahili PDF eyleminden geldi. Oturumu yanlışlıkla kapatma engellendi ve kullanıcıya ait kaynak UDF korunarak ayrı bir UDF kopyası kaydedildi. Aynı koşuda 16 onay hazırlığı da geçti.

0.19 önizleme kurucusu, .NET çalışma zamanı dosyası eksik olan aynı sürümü yeniden kullanabiliyordu. 0.19.0 kurucusu dağıtımdaki uygulama dosyalarının tamamını yeniden kullanım öncesi doğrular. Eksik çalışma zamanı reddedildi. Kaldırıp kurma denemesi çalışma zamanını geri yükledi ve örnek belgeyi korudu.

Java dosya kapsamı kabulü, yönetilen RTF, görsel, karşılaştırma kopyası, zengin otomatik metin ve yerel yedek akışlarını çalıştırır. Ayrıca alt klasöre SVG ve UDF çıktısı alır.

## .NET kabul testini tekrarlama

İlk kez çalıştırmadan önce [kurulumu](INSTALL.md) tamamla. PowerShell örneği .NET 10 SDK'yı ve standart Program Files yoluna kurulu 7-Zip'i kullanır. DOC eki için editörün yerel dönüştürücüsü veya Microsoft Word gerekir.

```powershell
dotnet run --project ./tests/Acceptance/UyapEditorMcp.Acceptance.csproj -c Release -- `
  ./app/UyapEditorMcp.exe $editor $javaBin ./fixtures/sentetik-3-sayfa.udf
```

Bu test geçici bir çalışma dizini açar. UDF, PDF, doğru ve yanlış 7z parolası, mevcut çıktı dosyasının korunması, araç açıklamaları ve 16 onay hazırlığını doğrular. Kurgu IMZ testini eklemek için beşinci argüman olarak aşağıdaki komutla üretilen UDF’yi ver.

```powershell
pwsh -NoProfile -File ./tests/New-Synthetic-UdfCms.ps1 -Source ./fixtures/sentetik-3-sayfa.udf -Output ./synthetic-signed.udf
dotnet run --project ./tests/Acceptance/UyapEditorMcp.Acceptance.csproj -c Release -- `
  ./app/UyapEditorMcp.exe $editor $javaBin ./fixtures/sentetik-3-sayfa.udf ./synthetic-signed.udf
```
