# UYAP 5.4.20 yerel menü ve eylem kapsamı

19 Kasım 2026 · Standart editör, Şablon Editörü ve kurgu tablolu belge

Menü yolları Codex App Server üzerinden canlı editörde okundu. Eylem tablosu canlı Swing kayıtlarını ve JAR’daki açıklamalı bildirimleri birleştirir. Kullanılabilirlik, envanter alındığı sıradaki belge durumuna ait.

MCP ve kurgu kabulü sütunundaki işaretler ayrı komut düzeyindeki veya aynı işlevi sağlayan MCP yolu düzeyindeki kanıta dayanır. Bir menü kaydındaki birden çok eylem kimliği aynı etiketi kullanan adayları gösterir.

275 eylem kaydında 244 tam kurgu kabulü, 13 kullanıcı onayı bekleyen akış, 2 ön koşul bekleyen akış, 15 editör içi kayıt ve 1 bu sürümde kapalı eylem var. [Test durumu](TESTING.md)

| Görünüm | Menü girdisi | Canlı Swing eylemi |
| --- | ---: | ---: |
| Standart editör | 164 | 251 |
| Şablon Editörü | 170 | 261 |
| Kurgu tablolu belge | 178 | — |

## Menü yolları

| Yol | Etiket | Doküman | Şablon | Tablo | Eşleşen eylem |
| --- | --- | --- | --- | --- | --- |
| Araçlar → Araçlar | Hafıza Boşalt | Etkin | Etkin | Etkin | `application-freemem-level1` |
| Araçlar → Araçlar | Log Başlat/Bitir | Etkin | Etkin | Etkin | `logger-activator` |
| Araçlar → Araçlar | Proxy | Etkin | Etkin | Etkin | `set proxy` |
| Araçlar → Açıklamalar | Açıklama Ekle | Etkin | Etkin | Etkin | `comment-add-type1` |
| Araçlar → Açıklamalar | Açıklamaları Göster | Etkin | Etkin | Etkin | `comment-show` |
| Araçlar → Açıklamalar | Sil | Pasif | Pasif | Pasif | `comment-delete` |
| Araçlar → Açıklamalar | Sonraki | Pasif | Pasif | Pasif | `comment-next`, `next-changes` |
| Araçlar → Açıklamalar | Türünü Değiştir | Pasif | Pasif | Pasif | `changeCommentType-switch` |
| Araçlar → Açıklamalar | Önceki | Pasif | Pasif | Pasif | `comment-previous`, `previous-changes` |
| Araçlar → Açıklamalar → Açıklama Ekle | Açıklama Ekle | Etkin | Etkin | Etkin | `comment-add-type1` |
| Araçlar → Açıklamalar → Açıklama Ekle | Açıklama Ekle - Çıktıda Görünmez | Etkin | Etkin | Etkin | `comment-add-type2` |
| Araçlar → Açıklamalar → Sil | Belgedeki Tüm Açıklamaları Sil | Etkin | Etkin | Etkin | `comment-delete-all` |
| Araçlar → Açıklamalar → Sil | Sil | Etkin | Etkin | Etkin | `comment-delete` |
| Araçlar → Güncelle | Editorü Güncelle | Etkin | Etkin | Etkin | `update` |
| Araçlar → Kısayol | Kısayol Düzenle | Etkin | Etkin | Etkin | `short-cut` |
| Araçlar → Kısayol | Kısayolları Görüntüle | Etkin | Etkin | Etkin | `all-shortcuts` |
| Araçlar → Kısayol | Otomatik Metin | Etkin | Etkin | Etkin | `auto-Text` |
| Araçlar → Kısayol | Yazı Kısayolları | Etkin | Etkin | Etkin | `insert-shortcutText` |
| Araçlar → İmza | APDU Etkin | Etkin | Etkin | Etkin | `apdu-action` |
| Araçlar → İmza | Dokümanı Mobil İmzala | Etkin | Etkin | Etkin | `Mobile-Sign` |
| Araçlar → İmza | Sertifikalar | Etkin | Etkin | Etkin | `certificate-owned-validate` |
| Araçlar → İmza | İmza Kütüphanesi | Etkin | Etkin | Etkin | `Set Sign Libary` |
| Araçlar → İmza | İmzala | Etkin | Etkin | Etkin | `sign` |
| Araçlar → İmza | İmzalar | Etkin | Etkin | Etkin | `show-signatures` |
| Biçim → Harf Denetimi | Tümünü Denetle | Etkin | Etkin | Etkin | `full-spellCheck` |
| Biçim → Renkler | Renkleri Düzenle | Etkin | Etkin | Etkin | `edit-colors` |
| Biçim → Renkler | Sayfa Arkaplan Rengi | Etkin | Etkin | Etkin | `page - background` |
| Biçim → Renkler | Sayfa Rengi | Etkin | Etkin | Etkin | `page-color` |
| Biçim → Renkler → Sayfa Arkaplan Rengi | Otomatik | Etkin | Etkin | Etkin | — |
| Biçim → Renkler → Sayfa Arkaplan Rengi | Tüm Renkler | Etkin | Etkin | Etkin | — |
| Biçim → Renkler → Sayfa Rengi | Otomatik | Etkin | Etkin | Etkin | — |
| Biçim → Renkler → Sayfa Rengi | Tüm Renkler | Etkin | Etkin | Etkin | — |
| Biçim → Yazı Özellikleri | Paragraf Özellikleri | Etkin | Etkin | Etkin | `paragraph-action` |
| Biçim → Yazı Özellikleri | Stil Özellikleri | Etkin | Etkin | Etkin | `ini-designer` |
| Biçim → Yazı Özellikleri | Yazı Özellikleri | Etkin | Etkin | Etkin | `font-action` |
| Dosya | Açık Pencereleri Listele | Etkin | Etkin | Etkin | `list-window` |
| Dosya | Çıkış | Etkin | Etkin | Etkin | `document-close` |
| Dosya → Aç | Aç | Etkin | Etkin | Etkin | `open-action` |
| Dosya → Aç | Yedekleri Aç | Etkin | Etkin | Etkin | `open-backups` |
| Dosya → Aç | Yeni | Etkin | Etkin | Etkin | `new-action` |
| Dosya → Dosya | Farklı Kaydet | Etkin | Etkin | Etkin | `save-as` |
| Dosya → Dosya | Kaydet | Etkin | Pasif | Etkin | `ctrl-save`, `save` |
| Dosya → Dosya | ODT Olarak Kaydet | Etkin | Etkin | Etkin | `save-odt` |
| Dosya → Dosya | PDF Olarak Kaydet | Etkin | Etkin | Etkin | `save-pdf` |
| Dosya → Dosya | Sisteme Kaydet | Pasif | Pasif | Etkin | `dss-save` |
| Dosya → Dosya | Tümünü Farklı Kaydet | Etkin | Etkin | Etkin | `fileConversion` |
| Dosya → Yazdır - Gönder | Dışa Aktar | Etkin | Etkin | Etkin | `document-export-signedddoc` |
| Dosya → Yazdır - Gönder | Gönder | Etkin | Etkin | Etkin | `attachto-mail` |
| Dosya → Yazdır - Gönder | Toplu Yazdır | Etkin | Etkin | Etkin | `print-batch` |
| Dosya → Yazdır - Gönder | Toplu İmzala | Etkin | Etkin | Etkin | `sign-batch` |
| Dosya → Yazdır - Gönder | Yazdır | Etkin | Etkin | Etkin | `document-print` |
| Dosya → Yazdır - Gönder | Yazdırma Önizlemesi | Etkin | Etkin | Etkin | `print-preview` |
| Dosya → Yazdır - Gönder → Gönder | Gönder | Etkin | Etkin | Etkin | `attachto-mail` |
| Dosya → Yazdır - Gönder → Gönder | ODT Olarak Gönder | Etkin | Etkin | Etkin | `attachto-mail-odt` |
| Dosya → Yazdır - Gönder → Gönder | PDF Olarak Gönder | Etkin | Etkin | Etkin | `attachto-mail-pdf` |
| Düzenle → Kayıt Yönetimi | Geri Al | Etkin | Etkin | Etkin | `undo` |
| Düzenle → Kayıt Yönetimi | Tekrarla | Etkin | Etkin | Etkin | `redo` |
| Düzenle → Kayıt Yönetimi | Tümünü Seç | Etkin | Etkin | Etkin | `select-all` |
| Düzenle → Makro | Makro Durdur | Pasif | Pasif | Pasif | `macro-stop` |
| Düzenle → Makro | Makroyu Çalıştır | Etkin | Etkin | Etkin | `macro-runCurrent` |
| Düzenle → Makro | Yeni Makro Başlat | Etkin | Etkin | Etkin | `macro-start-new` |
| Düzenle → Sayfa Yapısı | Alt Yazı Düzenle | Pasif | Pasif | Pasif | `edit-footer` |
| Düzenle → Sayfa Yapısı | Alt/Üst Yazı Düzenle | Pasif | Pasif | Pasif | `edit-hf` |
| Düzenle → Sayfa Yapısı | Sayfa Düzenle | Etkin | Etkin | Etkin | `page-setup` |
| Düzenle → Sayfa Yapısı | Sayfa Numarası Düzenle | Etkin | Etkin | Etkin | `edit-pageNumber` |
| Düzenle → Sayfa Yapısı | Üst Yazı Düzenle | Pasif | Pasif | Pasif | `edit-header` |
| Ekle → Alt/Üst Yazı | Alt Yazı Ekle | Etkin | Etkin | Etkin | `insert-footer` |
| Ekle → Alt/Üst Yazı | Paragraf Ekle | Etkin | Etkin | Etkin | `insert-paragraph-aftertable` |
| Ekle → Alt/Üst Yazı | Üst Yazı Ekle | Etkin | Etkin | Etkin | `insert-header` |
| Ekle → Grafik | Arka Plan Resmi  Ekle | Etkin | — | Etkin | `insert-background-image` |
| Ekle → Grafik | Arka Plan Resmi Ekle | — | Etkin | — | `insert-background-image` |
| Ekle → Grafik | Barkod Ekle | Etkin | Etkin | Etkin | `insert-barcode` |
| Ekle → Grafik | Köprü Ekle | Pasif | Pasif | Pasif | `insert-url-link` |
| Ekle → Grafik | Resim Ekle/Düzenle | Etkin | Etkin | Etkin | `insert-image` |
| Ekle → Grafik | Sembol Ekle | Etkin | Etkin | Etkin | `insert symbol` |
| Ekle → Sayfa Numarası | Sayfa Kesmesi | Etkin | Etkin | Etkin | `insert-pageBreak` |
| Ekle → Sayfa Numarası | Sayfa Numarası | Etkin | Etkin | Etkin | `insert-pageNumber` |
| Ekle → Sayfa Numarası | Sayfaya Git | Etkin | Etkin | Etkin | `go-pageNumber` |
| Ekle → Tablo | Tablo Ekle | Etkin | Etkin | Etkin | `prepareTable`, `table-insert` |
| Giriş → Arama | Bul | Etkin | Etkin | Etkin | `find` |
| Giriş → Arama → Bul | Bul | Etkin | Etkin | Etkin | `find` |
| Giriş → Arama → Bul | Değiştir | Etkin | Etkin | Etkin | `replace` |
| Giriş → Font | Alt Simge | Etkin | Etkin | Etkin | `sub-script` |
| Giriş → Font | Altı Çizili | Etkin | Etkin | Etkin | `font-underline` |
| Giriş → Font | Biçimlendirmeyi Temizle | Etkin | Etkin | Etkin | `clear-format` |
| Giriş → Font | Font Büyüt | Etkin | Etkin | Etkin | `increase-font-size` |
| Giriş → Font | Font Küçült | Etkin | Etkin | Etkin | `decrease-font-size` |
| Giriş → Font | Kalın | Etkin | Etkin | Etkin | `font-bold` |
| Giriş → Font | Yazı Arkaplan Rengi | Etkin | Etkin | Etkin | `back-ground` |
| Giriş → Font | Yazı Rengi | Etkin | Etkin | Etkin | `fore-ground` |
| Giriş → Font | null | Pasif | Pasif | Pasif | — |
| Giriş → Font | Üst Simge | Etkin | Etkin | Etkin | `super-script` |
| Giriş → Font | Üstü Çizili | Etkin | Etkin | Etkin | `Strike-through` |
| Giriş → Font | İtalik | Etkin | Etkin | Etkin | `font-italic` |
| Giriş → Font → Yazı Arkaplan Rengi | Renk Yok | Etkin | Etkin | Etkin | — |
| Giriş → Font → Yazı Arkaplan Rengi | Tüm Renkler | Etkin | Etkin | Etkin | — |
| Giriş → Font → Yazı Rengi | Otomatik | Etkin | Etkin | Etkin | — |
| Giriş → Font → Yazı Rengi | Tüm Renkler | Etkin | Etkin | Etkin | — |
| Giriş → Font → null | BÜYÜK HARF | Pasif | Pasif | Pasif | `changeCase-upper` |
| Giriş → Font → null | Döndür | Etkin | Etkin | Etkin | `rotate-text` |
| Giriş → Font → null | Harf Modu Değiştir | Pasif | Pasif | Pasif | `changeCase-switch` |
| Giriş → Font → null | Her Sözcüğü Büyük Harfe Çevir | Pasif | Pasif | Pasif | `changeCase-firstUpper` |
| Giriş → Font → null | bÜYÜK kÜÇÜK dÖNÜŞTÜR | Etkin | Etkin | Etkin | `switch-letters` |
| Giriş → Font → null | küçük harf | Pasif | Pasif | Pasif | `changeCase-lower` |
| Giriş → Pano | Biçim Kopyala | Etkin | Etkin | Etkin | `format-brush` |
| Giriş → Pano | Kes | Pasif | Pasif | Pasif | `cut-to-clipboard` |
| Giriş → Pano | Kopyala | Pasif | Pasif | Pasif | `copy-to-clipboard` |
| Giriş → Pano | Yapıştır | Etkin | Etkin | Etkin | `paste-from-clipboard` |
| Giriş → Paragraf | Boşlukları Göster | Etkin | Etkin | Etkin | `view-showWhiteSpace` |
| Giriş → Paragraf | Farklı çok düzeyli liste stilleri seçmek için tıklayın | Etkin | Etkin | Etkin | `list-multilevellist` |
| Giriş → Paragraf | Girinti Artır | Etkin | Etkin | Etkin | `increase-leftIndent` |
| Giriş → Paragraf | Girinti Azalt | Etkin | Etkin | Etkin | `decrease-leftIndent` |
| Giriş → Paragraf | Madde işaretli listeyi başlatın. Farklı madde işareti stilleri seçmek için oku tıklatın | Etkin | Etkin | Etkin | `font-bulleted` |
| Giriş → Paragraf | Numaralandırılmış listeyi başlatın. Farklı numaralandırma biçimleri seçmek için oku tıklatın | Etkin | Etkin | Etkin | `font-numbered` |
| Giriş → Paragraf | Ortala | Etkin | Etkin | Etkin | `center-justify` |
| Giriş → Paragraf | Sağa Yasla | Etkin | Etkin | Etkin | `right-justify` |
| Giriş → Paragraf | Sola Yasla | Etkin | Etkin | Etkin | `left-justify` |
| Giriş → Paragraf | null | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf | İki Yana Yasla | Etkin | Etkin | Etkin | `both-justify` |
| Giriş → Paragraf → null | 1.0 | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf → null | 1.15 | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf → null | 2.0 | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf → null | 2.5 | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf → null | 3.0 | Etkin | Etkin | Etkin | — |
| Giriş → Paragraf → null | Paragraf Özellikleri | Etkin | Etkin | Etkin | `paragraph-action` |
| Görünüm → Değişiklikler | Değişiklikleri İzle | Etkin | Etkin | Etkin | `watch-changes` |
| Görünüm → Değişiklikler | Kabul Et | Pasif | Pasif | Pasif | `accept-changes` |
| Görünüm → Değişiklikler | Reddet | Pasif | Pasif | Pasif | `reject-changes` |
| Görünüm → Değişiklikler | Sonraki | Pasif | Pasif | Pasif | `comment-next`, `next-changes` |
| Görünüm → Değişiklikler | Önceki | Pasif | Pasif | Pasif | `comment-previous`, `previous-changes` |
| Görünüm → Değişiklikler → Kabul Et | Tümünü Kabul Et | Etkin | Etkin | Etkin | `accept-all-changes` |
| Görünüm → Değişiklikler → Reddet | Tümünü Reddet | Etkin | Etkin | Etkin | `reject-all-changes` |
| Görünüm → Göster | Kelime Özellikleri | Etkin | Etkin | Etkin | `selection-word-info` |
| Görünüm → Göster | Seçim Bilgisi | Etkin | Etkin | Etkin | `info-selection` |
| Görünüm → Yardım | Sürüm Notlarını Aç | Etkin | Etkin | Etkin | `release-notes-action-str` |
| Görünüm → Yardım | Yardım Özelliklerini Aç | Etkin | Etkin | Etkin | `set-tooltip-enable` |
| Hızlı erişim | Aç | Etkin | Etkin | Etkin | `open-action` |
| Hızlı erişim | Geri Al | Etkin | Etkin | Etkin | `undo` |
| Hızlı erişim | Kaydet | Etkin | Pasif | Etkin | `ctrl-save`, `save` |
| Hızlı erişim | Sisteme Kaydet | Pasif | Pasif | Etkin | `dss-save` |
| Hızlı erişim | Tekrarla | Etkin | Etkin | Etkin | `redo` |
| Hızlı erişim | İmzala | Etkin | Etkin | Etkin | `sign` |
| Tablo → Tablo → Hücre | Hücreleri Birleştir | — | — | Etkin | `combine-table-cell` |
| Tablo → Tablo → Hücre | Hücreleri Böl | — | — | Etkin | `divide-table-cell` |
| Tablo → Tablo → Hücre | Sağa Hücre Ekle | — | — | Etkin | `insert-tablecell-after` |
| Tablo → Tablo → Hücre | Sola Hücre Ekle | — | — | Etkin | `insert-tablecell-before` |
| Tablo → Tablo → Satırlar ve Sütunlar | Altına Ekle | — | — | Etkin | `insert-tablerow-after` |
| Tablo → Tablo → Satırlar ve Sütunlar | Sağa Ekle | — | — | Etkin | `insert-column-right` |
| Tablo → Tablo → Satırlar ve Sütunlar | Sola Ekle | — | — | Etkin | `insert-column-left` |
| Tablo → Tablo → Satırlar ve Sütunlar | Üstüne Ekle | — | — | Etkin | `insert-tablerow-before` |
| Tablo → Tablo → Tablo | Tablo Kenarlıkları | — | — | Etkin | `edit-table-border` |
| Tablo → Tablo → Tablo | Tablo Sil | — | — | Etkin | `remove-table` |
| Tablo → Tablo → Tablo | Tablo Özellikleri | — | — | Etkin | `table-properties`, `table-properties-new` |
| Tablo → Tablo → Tablo → Tablo Sil | Hücre Sil | — | — | Etkin | `remove-tablecell` |
| Tablo → Tablo → Tablo → Tablo Sil | Satır Sil | — | — | Etkin | `remove-tablerow` |
| Tablo → Tablo → Tablo → Tablo Sil | Sütun Sil | — | — | Etkin | `remove-tablecolumn` |
| Tablo → Tablo → Tablo → Tablo Sil | Tablo Sil | — | — | Etkin | `remove-table` |
| Uygulama menüsü | Aç | Etkin | Etkin | Etkin | `open-action` |
| Uygulama menüsü | Açık Pencereleri Listele | Etkin | Etkin | Etkin | `list-window` |
| Uygulama menüsü | Dışa Aktar | Etkin | Etkin | Etkin | `document-export-signedddoc` |
| Uygulama menüsü | Farklı Kaydet | Etkin | Etkin | Etkin | `save-as` |
| Uygulama menüsü | Gönder | Etkin | Etkin | Etkin | `attachto-mail` |
| Uygulama menüsü | Kaydet | Etkin | Pasif | Etkin | `ctrl-save`, `save` |
| Uygulama menüsü | Sisteme Kaydet | Pasif | Pasif | Etkin | `dss-save` |
| Uygulama menüsü | Son Belgeler | Etkin | Etkin | Etkin | `last-opened-docs` |
| Uygulama menüsü | Yazdır | Etkin | Etkin | Etkin | `document-print` |
| Uygulama menüsü | Yedekleri Aç | Etkin | Etkin | Etkin | `open-backups` |
| Uygulama menüsü | Yeni | Etkin | Etkin | Etkin | `new-action` |
| Uygulama menüsü | Çıkış | Etkin | Etkin | Etkin | `document-close` |
| Uygulama menüsü → Farklı Kaydet → Belgenin bir kopyasını kaydet | Farklı Kaydet | Etkin | Etkin | Etkin | `save-as` |
| Uygulama menüsü → Farklı Kaydet → Belgenin bir kopyasını kaydet | ODT Olarak Kaydet | Etkin | Etkin | Etkin | `save-odt` |
| Uygulama menüsü → Farklı Kaydet → Belgenin bir kopyasını kaydet | PDF Olarak Kaydet | Etkin | Etkin | Etkin | `save-pdf` |
| Uygulama menüsü → Farklı Kaydet → Belgenin bir kopyasını kaydet | Tümünü Farklı Kaydet | Etkin | Etkin | Etkin | `fileConversion` |
| Uygulama menüsü → Gönder → Belgenin bir kopyasını e-postaya iliştir | DOC Olarak Gönder | Etkin | Etkin | Etkin | `attachto-mail-doc` |
| Uygulama menüsü → Gönder → Belgenin bir kopyasını e-postaya iliştir | Gönder | Etkin | Etkin | Etkin | `attachto-mail` |
| Uygulama menüsü → Gönder → Belgenin bir kopyasını e-postaya iliştir | ODT Olarak Gönder | Etkin | Etkin | Etkin | `attachto-mail-odt` |
| Uygulama menüsü → Gönder → Belgenin bir kopyasını e-postaya iliştir | PDF Olarak Gönder | Etkin | Etkin | Etkin | `attachto-mail-pdf` |
| Uygulama menüsü → Son Belgeler → Son Açılan Belgeler | sentetik-3-sayfa.udf | Etkin | — | — | — |
| Uygulama menüsü → Yazdır → Belgeyi önizle ve yazdır | Yazdır | Etkin | Etkin | Etkin | `document-print` |
| Uygulama menüsü → Yazdır → Belgeyi önizle ve yazdır | Yazdırma Önizlemesi | Etkin | Etkin | Etkin | `print-preview` |
| Şablon → Alan | Alan Ekle/Düzenle | — | Etkin | — | `add-field`, `add-field-from-list` |
| Şablon → Alan | Alan Grubu Ekle | — | Etkin | — | `add-field-group` |
| Şablon → Alan | Alanlar Silinebilsin | — | Etkin | — | `field-erasable` |
| Şablon → Alan | Paragraf Alan Grubu Ekle | — | Etkin | — | `add-paragraph-field-group` |
| Şablon → Alan | Tablo Düzenlenemesin | — | Pasif | — | `table-erasable` |
| Şablon → Dönüştür | Şablona Dönüştür | — | Etkin | — | `template-convert` |
| Şablon → Dönüştür | Şablondan Geri Dönüştür | — | Etkin | — | `template-unconvert` |

## Eylem kimlikleri

| Kimlik | Etiket | Doküman | Şablon | MCP ve kurgu kabulü |
| --- | --- | --- | --- | --- |
| `accept-all-changes` | Tümünü Kabul Et | Etkin | Etkin | uyap_compare_udf · Kurgu kabulü |
| `accept-changes` | Kabul Et | Pasif | Pasif | uyap_compare_udf · Kurgu kabulü |
| `add-annotation` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `add-field` | Alan Ekle/Düzenle | — | Etkin | uyap_open_form · Kurgu kabulü |
| `add-field-from-list` | Alan Ekle/Düzenle | — | Etkin | uyap_reuse_template_field · Kurgu kabulü |
| `add-field-group` | Alan Grubu Ekle | — | Etkin | uyap_open_form · Kurgu kabulü |
| `add-paragraph-field-group` | Paragraf Alan Grubu Ekle | — | Etkin | uyap_open_form · Kurgu kabulü |
| `add-selection` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `all-shortcuts` | Kısayolları Görüntüle | Etkin | Etkin | uyap_list_shortcuts · Kurgu kabulü |
| `apdu-action` | APDU Etkin | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `application-freemem-level1` | Hafıza Boşalt | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `attachto-mail` | Gönder | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `attachto-mail-doc` | DOC Olarak Gönder | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `attachto-mail-odt` | ODT Olarak Gönder | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `attachto-mail-pdf` | PDF Olarak Gönder | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `attributes-changed-character` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `attributes-changed-paragraph` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `auto-Text` | Otomatik Metin | Etkin | Etkin | uyap_list_automatic_text / uyap_set_automatic_text · Kurgu kabulü |
| `back-ground` | Yazı Arkaplan Rengi | Etkin | Etkin | uyap_set_palette_color · Kurgu kabulü |
| `beep` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `both-justify` | İki Yana Yasla | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-backward` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-begin` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-begin-line` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-begin-paragraph` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-begin-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-down` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-end` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-end-line` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-end-paragraph` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-end-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-forward` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-next-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-previous-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `caret-up` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `center-justify` | Ortala | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `certificate-owned-validate` | Sertifikalar | Etkin | Etkin | uyap_prepare_approval · Ön koşul bekliyor |
| `changeCase-firstUpper` | Her Sözcüğü Büyük Harfe Çevir | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `changeCase-lower` | küçük harf | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `changeCase-switch` | Harf Modu Değiştir | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `changeCase-upper` | BÜYÜK HARF | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `changeCommentType-switch` | Türünü Değiştir | Etkin | Etkin | uyap_change_comment · Kurgu kabulü |
| `changeListNo-action` | Numaralandırma değeri ayarla | Etkin | Etkin | uyap_set_numbering_start / uyap_open_form · Kurgu kabulü |
| `clear-format` | Biçimlendirmeyi Temizle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `combine-table-cell` | Hücreleri Birleştir | Pasif | Pasif | uyap_run_action · Kurgu kabulü |
| `comment-add-type1` | Açıklama Ekle | Etkin | Etkin | uyap_add_comment · Kurgu kabulü |
| `comment-add-type2` | Açıklama Ekle - Çıktıda Görünmez | Etkin | Etkin | uyap_add_comment · Kurgu kabulü |
| `comment-delete` | Sil | Etkin | Etkin | uyap_change_comment · Kurgu kabulü |
| `comment-delete-all` | Belgedeki Tüm Açıklamaları Sil | Etkin | Etkin | uyap_change_comment · Kurgu kabulü |
| `comment-next` | Sonraki | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `comment-previous` | Önceki | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `comment-show` | Açıklamaları Göster | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `continueNumbering-action` | Numaralandırmaya Devam Et | Etkin | Etkin | uyap_continue_numbering · Kurgu kabulü |
| `control_image_size` | Yeniden boyutlandır | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `copy-to-clipboard` | Kopyala | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `ctrl-save` | Kaydet | Etkin | Etkin | uyap_save_copy · Kurgu kabulü |
| `cut-to-clipboard` | Kes | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `debug-lvuMetrics` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `decrease-firstline-indent` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `decrease-font-size` | Font Küçült | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `decrease-hanging-indent` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `decrease-left-indent-step` | Sol Girinti Kaldır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `decrease-leftIndent` | Girinti Azalt | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `default-typed` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `delete-entryfield-group` | — | — | Etkin | uyap_run_action · Kurgu kabulü |
| `delete-next` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `delete-next-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `delete-previous` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `delete-previous-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `dirty-state-change` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `divide-table-cell` | Hücreleri Böl | Pasif | Pasif | uyap_open_form · Kurgu kabulü |
| `document-close` | Çıkış | Etkin | Etkin | uyap_close · Kurgu kabulü |
| `document-export-signedddoc` | Dışa Aktar | Etkin | Etkin | uyap_export_signed_imz · Kurgu kabulü |
| `document-print` | Yazdır | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `dss-save` | Sisteme Kaydet | Pasif | Pasif | uyap_prepare_approval · Ön koşul bekliyor |
| `dump-model` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `edit-colors` | Renkleri Düzenle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `edit-footer` | Alt Yazı Düzenle | Pasif | Pasif | uyap_set_page_region · Kurgu kabulü |
| `edit-header` | Üst Yazı Düzenle | Pasif | Pasif | uyap_set_page_region · Kurgu kabulü |
| `edit-hf` | Alt/Üst Yazı Düzenle | Pasif | Pasif | uyap_run_action · Kurgu kabulü |
| `edit-pageNumber` | Sayfa Numarası Düzenle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `edit-table-border` | Tablo Kenarlıkları | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `element-changed-character` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `element-changed-paragraph` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `encrypt` | `uyap_encrypt_7z`, `uyap_extract_7z` (7z arşivi) | Etkin | Etkin | Yerel şifreleme işleyicisi devre dışı · Harici 7z arşiv akışı Codex üzerinden doğrulandı |
| `exit` | — | Etkin | Etkin | uyap_close · Kurgu kabulü |
| `export-fop` | export-fop | — | — | uyap_export_document · Kurgu kabulü |
| `export-html` | export-html | — | — | uyap_export_document · Kurgu kabulü |
| `export-html-with-breaks` | export-html-with-breaks | — | — | uyap_export_document · Kurgu kabulü |
| `export-pdf` | export-pdf | — | — | uyap_export_pdf · Kurgu kabulü |
| `export-ps` | export-ps | — | — | uyap_export_document · Kurgu kabulü |
| `export-rtf` | export-rtf | — | — | uyap_export_document · Kurgu kabulü |
| `export-svg` | export-svg | — | — | uyap_export_document · Kurgu kabulü |
| `export-xsl` | export-xsl | — | — | uyap_export_document · Kurgu kabulü |
| `field-erasable` | Alanlar Silinebilsin | — | Etkin | uyap_open_form · Kurgu kabulü |
| `file-toolbar` | Dosya Araç çubuğu | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `fileConversion` | Tümünü Farklı Kaydet | Etkin | Etkin | uyap_batch_export · Kurgu kabulü |
| `find` | Bul | Etkin | Etkin | uyap_open_form / uyap_find · Kurgu kabulü |
| `first Letter Upper` | Baş Harfler Büyük | Etkin | Etkin | uyap_open_form + uyap_capitalization_state · Kurgu kabulü |
| `font-action` | Yazı Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `font-bold` | Kalın | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-bulleted` | Madde işaretli listeyi başlatın. Farklı madde işareti stilleri seçmek için oku tıklatın | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-family` | — | Etkin | Etkin | uyap_set_font · Kurgu kabulü |
| `font-family-Monospaced` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-family-SansSerif` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-family-Serif` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-italic` | İtalik | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-numbered` | Numaralandırılmış listeyi başlatın. Farklı numaralandırma biçimleri seçmek için oku tıklatın | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size` | — | Etkin | Etkin | uyap_set_font · Kurgu kabulü |
| `font-size-10` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-12` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-14` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-16` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-18` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-24` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-36` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-48` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-size-8` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `font-underline` | Altı Çizili | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `fore-ground` | Yazı Rengi | Etkin | Etkin | uyap_set_palette_color · Kurgu kabulü |
| `format-brush` | Biçim Kopyala | Etkin | Etkin | uyap_apply_format_brush · Kurgu kabulü |
| `format-brush-clear` | — | Etkin | Etkin | uyap_apply_format_brush · Kurgu kabulü |
| `full-spellCheck` | Tümünü Denetle | Etkin | Etkin | uyap_spell_check / uyap_apply_spelling · Kurgu kabulü |
| `fullscreen` | Ekrana Sığdır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `go-pageNumber` | Sayfaya Git | Etkin | Etkin | uyap_go_to_page · Kurgu kabulü |
| `hf-startPage` | — | Etkin | Etkin | uyap_page_region_range · Kurgu kabulü |
| `hf-stopPage` | — | Etkin | Etkin | uyap_page_region_range · Kurgu kabulü |
| `increase-firstline-indent` | İlk Satır Girinti Artır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `increase-font-size` | Font Büyüt | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `increase-hanging-indent` | Asılı Girinti Artır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `increase-leftIndent` | Girinti Artır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `info-selection` | Seçim Bilgisi | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `ini-designer` | Stil Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `insert symbol` | Sembol Ekle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `insert Tl symbol` | TL Simgesi | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-background-image` | Arka Plan Resmi Ekle | Etkin | Etkin | uyap_insert_image · Kurgu kabulü |
| `insert-barcode` | Barkod Ekle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `insert-break` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-column-left` | Sola Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-column-right` | Sağa Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-content` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-footer` | Alt Yazı Ekle | Etkin | Etkin | uyap_set_page_region · Kurgu kabulü |
| `insert-header` | Üst Yazı Ekle | Etkin | Etkin | uyap_set_page_region · Kurgu kabulü |
| `insert-image` | Resim Ekle/Düzenle | Etkin | Etkin | uyap_insert_image · Kurgu kabulü |
| `insert-pageBreak` | Sayfa Kesmesi | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-pageNumber` | Sayfa Numarası | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `insert-paragraph-aftertable` | Paragraf Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-shortcutText` | Yazı Kısayolları | Etkin | Etkin | uyap_insert_automatic_text · Kurgu kabulü |
| `insert-tab` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-tab-ex` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-tablecell-after` | Sağa Hücre Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-tablecell-before` | Sola Hücre Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-tablerow-after` | Altına Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-tablerow-before` | Üstüne Ekle | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `insert-url-link` | Köprü Ekle | Pasif | Pasif | uyap_open_form · Kurgu kabulü |
| `last-opened-docs` | Son Belgeler | Etkin | Etkin | uyap_menu_inventory + uyap_open · Kurgu kabulü |
| `layoutChanged-horizontal` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `left-justify` | Sola Yasla | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `letter-mode-select-menu` | — | Etkin | Etkin | uyap_menu_inventory + uyap_run_action · Kurgu kabulü |
| `line-gap` | — | Etkin | Etkin | uyap_set_line_spacing · Kurgu kabulü |
| `list-multilevellist` | Farklı çok düzeyli liste stilleri seçmek için tıklayın | Etkin | Etkin | uyap_set_list_palette / uyap_set_list_level / uyap_set_custom_list / uyap_set_list_indents · Kurgu kabulü |
| `list-window` | Açık Pencereleri Listele | Etkin | Etkin | uyap_list_windows · Kurgu kabulü |
| `logger-activator` | Log Başlat/Bitir | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `macro-runCurrent` | Makroyu Çalıştır | Etkin | Etkin | uyap_run_macro · Kurgu kabulü |
| `macro-start-new` | Yeni Makro Başlat | Etkin | Etkin | uyap_run_macro · Kurgu kabulü |
| `macro-stop` | Makro Durdur | Pasif | Pasif | uyap_run_macro · Kurgu kabulü |
| `Mobile-Sign` | Dokümanı Mobil İmzala | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `new-action` | Yeni | Etkin | Etkin | uyap_new / uyap_open_template · Kurgu kabulü |
| `new-doc-action` | — | Etkin | Etkin | uyap_new · Kurgu kabulü |
| `new-temp-action` | — | Etkin | Etkin | uyap_open_template · Kurgu kabulü |
| `next-changes` | Sonraki | Pasif | Pasif | uyap_compare_udf · Kurgu kabulü |
| `open-action` | Aç | Etkin | Etkin | uyap_open / uyap_import_document · Kurgu kabulü |
| `open-backups` | Yedekleri Aç | Etkin | Etkin | uyap_list_backups / uyap_restore_backup · Kurgu kabulü |
| `page - background` | Sayfa Arkaplan Rengi | Etkin | Etkin | uyap_set_palette_color · Kurgu kabulü |
| `page-color` | Sayfa Rengi | Etkin | Etkin | uyap_set_palette_color · Kurgu kabulü |
| `page-down` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `page-setup` | Sayfa Düzenle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `page-up` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `paragraph-action` | Paragraf Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `paste-from-clipboard` | Yapıştır | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `plain-auto-text` | Düz Metin | Etkin | Etkin | uyap_set_automatic_text · Kurgu kabulü |
| `prepareTable` | Tablo Ekle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `previous-changes` | Önceki | Pasif | Pasif | uyap_compare_udf · Kurgu kabulü |
| `print-batch` | Toplu Yazdır | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `print-preview` | Yazdırma Önizlemesi | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `redo` | Tekrarla | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `reject-all-changes` | Tümünü Reddet | Etkin | Etkin | uyap_compare_udf · Kurgu kabulü |
| `reject-changes` | Reddet | Pasif | Pasif | uyap_compare_udf · Kurgu kabulü |
| `release-notes-action-str` | Sürüm Notlarını Aç | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `remove-field-group` | Alan Grubu Kaldır | — | — | uyap_run_action · Kurgu kabulü |
| `remove-paragraph-field-group` | Paragraf Alan Grubu Kaldır | — | — | uyap_run_action · Kurgu kabulü |
| `remove-table` | Tablo Sil | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `remove-tablecell` | Hücre Sil | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `remove-tablecolumn` | Sütun Sil | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `remove-tablerow` | Satır Sil | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `replace` | Değiştir | Etkin | Etkin | uyap_open_form / uyap_replace_text · Kurgu kabulü |
| `resetfullscreen` | Normal Boyut | Etkin | Etkin | uyap_set_view · Kurgu kabulü |
| `right-justify` | Sağa Yasla | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `rotate-text` | Döndür | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `save` | Kaydet | Pasif | Pasif | uyap_save_copy · Kurgu kabulü |
| `save-as` | Farklı Kaydet | Etkin | Etkin | uyap_save_copy / uyap_export_document · Kurgu kabulü |
| `save-content` | — | Etkin | Etkin | uyap_save_copy · Kurgu kabulü |
| `save-doc` | DOC Olarak Kaydet | Etkin | Etkin | uyap_export_document · Kurgu kabulü |
| `save-odt` | ODT Olarak Kaydet | Etkin | Etkin | uyap_export_document · Kurgu kabulü |
| `save-pdf` | PDF Olarak Kaydet | Etkin | Etkin | uyap_export_pdf · Kurgu kabulü |
| `select-all` | Tümünü Seç | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `select-line` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `select-paragraph` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `select-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-backward` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-begin` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-begin-line` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-begin-paragraph` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-begin-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-down` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-end` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-end-line` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-end-paragraph` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-end-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-forward` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-next-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-page-down` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-page-left` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-page-right` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-page-up` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-previous-word` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-up` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `selection-word-info` | Kelime Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `sessionTimeOut` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `set proxy` | Proxy | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `Set Sign Libary` | İmza Kütüphanesi | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `set-document` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `set-read-only` | — | Etkin | Etkin | uyap_edit_mode · Kurgu kabulü |
| `set-tooltip-enable` | Yardım Özelliklerini Aç | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `set-writable` | — | Etkin | Etkin | uyap_edit_mode · Kurgu kabulü |
| `short-cut` | Kısayol Düzenle | Etkin | Etkin | uyap_set_automatic_text · Kurgu kabulü |
| `show-all-annotation` | Tüm Açıklamaları Gör | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `show-annotation` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `show-elementTreePanel` | — | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `show-paragraph-properties` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `show-selection` | — | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `show-signatures` | İmzalar | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `sign` | İmzala | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `sign-batch` | Toplu İmzala | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `spell-check` | Kelime Denetimi | Etkin | Etkin | uyap_spell_check / uyap_apply_spelling · Kurgu kabulü |
| `startNumbering-action` | Yeniden Başlat:1 | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `Strike-through` | Üstü Çizili | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `style-toolbar` | Stil Araç çubğu | Etkin | Etkin | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `sub-script` | Alt Simge | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `super-script` | Üst Simge | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `switch-letters` | bÜYÜK kÜÇÜK dÖNÜŞTÜR | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `tab-shift` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `table-col-properties` | Sütun Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `table-erasable` | Tablo Düzenlenemesin | — | Etkin | uyap_run_action · Kurgu kabulü |
| `table-height` | Tablo Yüksekliği | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `table-insert` | Tablo Ekle | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `table-properties` | Tablo Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `table-properties-new` | Tablo Özellikleri | Pasif | Pasif | uyap_open_form · Kurgu kabulü |
| `table-row-heights` | Satır Yükseklikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `table-row-properties` | Satır Özellikleri | Etkin | Etkin | uyap_open_form · Kurgu kabulü |
| `Tamam` | Tamam | — | — | uyap_form_input · Kurgu kabulü |
| `template-color` | Renk Seçimi | — | — | uyap_open_form · Kurgu kabulü |
| `template-convert` | Şablona Dönüştür | — | Etkin | uyap_open_form · Kurgu kabulü |
| `template-elementMarker-switch` | eleman işaretleyicileri | — | Etkin | uyap_run_action · Kurgu kabulü |
| `template-unconvert` | Şablondan Geri Dönüştür | — | Etkin | uyap_open_form · Kurgu kabulü |
| `test` | Editor tested | — | — | Editör içi veya geliştirici eylemi · Paket kanıtı |
| `To-Uppercase` | Otomatik Büyük Harf | Etkin | Etkin | uyap_open_form + uyap_capitalization_state · Kurgu kabulü |
| `undo` | Geri Al | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `unselect` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `update` | Editorü Güncelle | Etkin | Etkin | uyap_prepare_approval · Onay incelemesi doğrulandı |
| `Vazgeç` | Vazgeç | — | — | uyap_form_input · Kurgu kabulü |
| `view-showWhiteSpace` | Boşlukları Göster | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `watch-changes` | Değişiklikleri İzle | Etkin | Etkin | uyap_compare_udf · Kurgu kabulü |
| `windows-menu` | — | Etkin | Etkin | uyap_run_action · Kurgu kabulü |
| `zoom` | — | Etkin | Etkin | uyap_set_view · Kurgu kabulü |

## Kapsam sınırları

Uygulama menüsü, şerit sekmeleri, hızlı erişim ve oluşturulabilen düğme alt menüleri tarandı. Açık bir belge ve denenen kurgu tablo durumunda erişilebilen bağlamsal gruplar dahil. İmza, fiziksel yazdırma, e-posta, güncelleme ve UYAP sistemine kayıt kullanıcı işlemidir.

DOC ve SVG’nin bu bilgisayarda doğrulanan uyumluluk yolları sırasıyla kurulu Microsoft Word’ü ve UYAP’ın yerel sayfa çizimini kullanır. Sesli uyarıda oynatma hattının teslim aldığı kareler ölçüldü.
