# Test durumu / Test status

UYAP Doküman Editörü 5.4.20 · Windows x64

**244/275** yerel UYAP eylem kaydında kurgu kabulü tamamlandı. Bu sayı, aynı işlev bir MCP yolu üzerinden doğrulandığında onu da kapsar. **65** MCP aracı Codex App Server tarafından listelendi.

| Durum | Doğrulanan adım |
| --- | --- |
| Codex Desktop / App Server | Gerçek MCP çağrılarıyla kurgu UDF açma, üçüncü sayfa, PDF, imzalı UDF’den IMZ ve 7z parola/doğru baytlarla geri yükleme geçti. |
| Doğrudan stdio | AI hesabı kullanmadan MCP başlatıldı. 65 araç listelendi, üçüncü sayfa açıldı ve üç sayfalık PDF üretildi. Kaynak UDF'nin baytları aynı kaldı. Geçersiz yol denemesinde sabit hata kodu ve kaynak dosyasından biçimlenen mesaj doğrulandı. |
| Gemini CLI 0.60.0 | Ayrı proje yapılandırmasında yerel stdio sunucusu `Connected` olarak keşfedildi. Belge üzerinden uçtan uca Gemini testi bekliyor. |
| Claude Code 2.1.329 | Ayrı proje yapılandırmasında yerel stdio sunucusu `Connected` olarak keşfedildi. Belge üzerinden uçtan uca Claude Code testi bekliyor. |
| Claude Desktop | MCPB manifesti resmî CLI ile doğrulandı. Uygulama içinden kurulum ve belge testi bekliyor. |
| E-posta eki · 4 | UDF, DOC, ODT ve PDF için dosya ve yerel `.eml` içeriği doğrulandı. Bir istemciden teslimat bekliyor. |
| Yazdırma · 2 | Tekli ve toplu PDF önizlemeleri doğrulandı. Windows yazıcıya gönderim bekliyor. |
| Yerel ayarlar ve yardımcı işlemler · 4 | İmza kütüphanesi, proxy, yardım ve güncelleme için menü/hazırlık doğrulandı. Son işlem ve sonucu bekliyor. |
| İmza · 3 | Tekli, toplu ve mobil imza hazırlığı doğrulandı. Desteklenen e-imza veya mobil onay ile son adım bekliyor. |
| Sertifika ve portal · 2 | SoftHSM ve Kamu SM test zinciri ayrı olarak doğrulandı. UYAP'ın kendi Sertifikalar menüsünde tanıdığı okuyucu ve sertifika ile kabul bekliyor. Portal kaydı için yetkili kayıt ve oturum gerekiyor. |
| Editör içi · 15 | İç olay, etiket veya işleyici. Kullanıcı eylemlerinden ayrı kaydedildi. |
| Yerel şifreleme · 1 | UYAP 5.4.20 yöntemi devre dışı. MCP'nin AES korumalı 7z oluşturma ve açma akışı geçti. |

IMZ doğrulaması kurgu belgelerle yerel dışa aktarma ve imza baytlarının eşleşmesini kapsar. Nitelikli sertifika güveni, kartla imza ve portal kaydı ayrı kabul konularıdır. [Eylem tablosu](uyap-5.4.20-actions.md)

Temiz derlemede 16 onay hazırlığı yeniden geçti. Ayrı PDF denemesinde kaydedilmemiş ekleme ve geri alma, üretilen PDF'de doğrulandı. UYAP, o denemede değişiklik ve geri alma adımlarında şerit düğmesi yerine dahili PDF eylemine geçti. Dolayısıyla yalnızca şerit yolunu şart koşan tekrar testi açık kaldı.

macOS ve Linux yerel sunucu platformu test kapsamında bulunmuyor.

## .NET kabul testini tekrarlama

İlk kez çalıştırmadan önce [kurulumu](INSTALL.md) tamamla. PowerShell örneği .NET 10 SDK'yı ve standart Program Files yoluna kurulu 7-Zip'i kullanır. DOC eki için editörün yerel dönüştürücüsü veya Microsoft Word gerekir.

```powershell
dotnet run --project ./tests/Acceptance/UyapEditorMcp.Acceptance.csproj -c Release -- `
  ./app/UyapEditorMcp.exe $editor $javaBin ./fixtures/sentetik-3-sayfa.udf
```

Bu test geçici bir çalışma dizini açar. UDF, PDF, doğru ve yanlış 7z parolası, araç açıklamaları ve 16 onay hazırlığını doğrular. Önceki Codex koşularının envanteri, .NET kabul projesinin kapsamından daha geniştir.
