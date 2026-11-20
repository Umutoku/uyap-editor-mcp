# Gizlilik ve veri akışı / Privacy and data flow

Bu MCP'nin yayıncısı belge, kullanım istatistiği veya telemetri toplamaz. Sunucu bilgisayarında ayrı bir Windows süreci olarak çalışır ve istemciyle stdin/stdout üzerinden haberleşir. Projenin işlettiği bir hesap, ağ uç noktası veya belge deposu yoktur.

Sunucu, ayarlanan girdi kökündeki belgeleri ve isteğe bağlı yedek kökünü okur. Çıktı köküne yeni dosyalar, durum köküne ayrı UYAP profilleri ve çalışma kopyaları yazar. Bu kopyalar belge verisi içerebilir. Bu klasörleri bulut eşitlemesi yapılmayan, erişimini kontrol ettiğin bir konumda tut. 7z parolası yerel UTF-8 dosyasından okunur ve 7-Zip'e standart girdiyle verilir. Parola dosyasını ayrı tut ve sürüm kontrolüne alma.

MCP sonuçlarını seçtiğin yapay zekâ istemcisi görür. Örneğin `uyap_read_text` belge metnini çağıran istemciye döndürür. İstemci bu içeriği kendi model sağlayıcısına iletebilir. İstemcinin oturum geçmişi, günlükleri ve veri politikası ayrıca geçerlidir. Gizli belgelerle çalışmadan önce istemcinin ayarlarını incele.

Gerçek dosyalarda kişisel veri veya meslek sırrı bulunabilir. Bu belgeleri bir modele iletmeden önce paylaşım yetkini, sağlayıcının veri işleme koşullarını ve varsa yurt dışına aktarım şartlarını değerlendir. Kurulum ve deneme için depodaki kurgu belgeleri kullanabilirsin. [KVKK yurt dışına aktarım bilgileri](https://www.kvkk.gov.tr/Icerik/2053/Yurtdisina-Aktarim)

UYAP Editör, Java, Windows ve diğer yerel uygulamalar kendi veri politikalarından sorumludur. E-posta taslakları yerel olarak hazırlanır. İmzalama, gönderim, portal kaydı ve benzeri son adımlar ilgili uygulamada veya cihazda onay bekler.

The publisher collects no documents, usage data or telemetry. The local stdio server reads configured folders and writes exports and temporary editor profiles on your machine. Tool results go to the MCP client. That client may forward content to its configured model provider or retain it in its own logs. Review the client's data policy before processing confidential documents.
