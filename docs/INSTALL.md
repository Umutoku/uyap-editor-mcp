# Kurulum / Installation

Sunucu için etkileşimli **Windows x64 masaüstü oturumu**, **UYAP Doküman Editörü 5.4.20** ve Java Access Bridge içeren **Java 8 x64** gerekir. 0.19 sürüm adayı .NET çalışma zamanını yanında taşır. GitHub Releases’teki eski 0.18.0 önizlemesi ayrıca .NET 10 x64 çalışma zamanı ister. 7z araçları için Program Files altında 7-Zip kurulu olmalıdır. UYAP JAR dosyalarının SHA-256 değerleri başlangıçta doğrulanır. [UYAP Editör](https://www.uyap.gov.tr/UYAP-Editor) · [7-Zip](https://www.7-zip.org/)

| Bileşen | Doğrulanan uyumluluk |
| --- | --- |
| Editörü çalıştıran Java | Java 8 x64, güncelleme 503 ile UYAP 5.4.20 ve kurgu belge kabul testi geçti. Java güncelleme numarası kodda sabitlenmez. |
| Java 21 x64 | JDK 21.0.8 ile Java Access Bridge dosyası bulundu. UYAP açılırken editörün XStream bağımlılığı `InaccessibleObjectException` verdi. Java 21.0.8, bu editör paketi için desteklenen çalışma ortamı değildir. |
| Java başlatıcısını derleyen JDK | JDK 21.0.8 ile `--release 8` derlemesi doğrulandı. |
| Son kullanıcı .NET çalışma zamanı | 0.19 Windows x64 sürüm adayı paket içindeki .NET 10.0.15 çalışma zamanını yükledi. Ayrı .NET kurulumu gerekmez. |
| .NET kaynak derlemesi | Proje `net10.0-windows` hedefler. Derleme .NET 11 SDK 11.0.100 ile de geçti. |

## Windows PowerShell

GitHub Releases'ten Windows arşivini indir ve yerel bir klasöre aç. Sunucuyu kaynak koddan derlemek istersen UYAP JAR dizinini, `--release 8` destekleyen JDK'yı ve .NET 10 SDK'yı kullan. İlk örnekte `$root` arşivin açıldığı klasördür. Kaynaktan çalışıyorsan depo kökünü kullan.

```powershell
$root = 'C:/Tools/uyap-editor-mcp'
$editor = 'C:/UYAP/Editor'
$javaBin = 'C:/Java/jre8/bin'

# Yalnız kaynak koddan derleme için
& "$root/Build.ps1" -EditorDirectory $editor -JavaCompiler 'C:/Java/jdk17/bin/javac.exe'

$exe = "$root/app/UyapEditorMcp.exe"
$inputDir = "$root/fixtures"
$outputDir = "$root/exports"
$stateDir = "$root/.state"
New-Item -ItemType Directory -Force -Path $outputDir,$stateDir | Out-Null
$serverArgs = @(
  '--editor-dir', $editor, '--java-bin', $javaBin,
  '--input-root', $inputDir, '--output-root', $outputDir,
  '--state-dir', $stateDir
)
```

`$editor`, içinde `editor_lib.jar` bulunan UYAP program klasörüdür. `$javaBin` içinde `java.exe` ve `WindowsAccessBridge-64.dll` bulunmalıdır. Arşivden kurulumda derleme satırını atla. İlk test için gönderilen `fixtures` klasörünü kullan.

### Codex

```powershell
codex mcp add uyap_editor -- $exe @serverArgs
codex mcp list
```

Yeni oturumda `uyap_open` aracının göründüğünü doğrula. [Codex MCP belgeleri](https://developers.openai.com/codex/mcp/)

### Claude Code

```powershell
claude mcp add --scope local --transport stdio uyap_editor -- $exe @serverArgs
claude mcp list
```

Komutu yerel Windows ortamında çalıştır. Claude Code oturumunda `/mcp` bağlantı durumunu gösterir. [Claude Code MCP belgeleri](https://code.claude.com/docs/en/mcp)

### Claude Desktop

Windows MCPB paketini kullanıyorsan uzantıyı kur ve yapılandırma ekranında UYAP JAR, Java, girdi, çıktı ve durum klasörlerini seç. Elle yapılandırmak için geliştirici ayarlarından `claude_desktop_config.json` dosyasını aç ve aşağıdaki üretilen kaydı mevcut `mcpServers` nesnesine ekle. Claude Desktop'ı yeniden başlat. [Claude Desktop MCP kurulumu](https://modelcontextprotocol.io/docs/tutorials/use-local-mcp-server) · [MCPB yapılandırma şeması](https://github.com/modelcontextprotocol/mcpb/blob/main/MANIFEST.md)

```powershell
@{ mcpServers = @{ uyap_editor = @{ command = $exe; args = $serverArgs } } } |
  ConvertTo-Json -Depth 8
```

### Gemini CLI

Aşağıdaki PowerShell komutunun çıktısını proje klasöründeki `.gemini/settings.json` dosyasına ekle. Zaten `mcpServers` varsa o nesneye yalnızca yeni `uyap_editor` kaydını birleştir. Güvenilen proje klasöründe `gemini mcp list` ile bağlantıyı kontrol et. [Gemini CLI MCP belgeleri](https://geminicli.com/docs/tools/mcp-server/)

```powershell
@{ mcpServers = @{ uyap_editor = @{ command = $exe; args = $serverArgs; trust = $false } } } |
  ConvertTo-Json -Depth 8
gemini mcp list
```

### Diğer istemciler

Yerel stdio alt süreci çalıştırabilen MCP istemcilerinde aynı yürütülebilir dosya ve argümanlar kullanılır. İstemci ve sunucu, UYAP'ın çalıştığı aynı etkileşimli Windows oturumunda bulunmalıdır. Uzak HTTP/SSE ve tarayıcıdan bağlanan bulut istemcileri için bu dağıtımda bir ağ uç noktası bulunmaz.

## Kaynaktan derleme

Visual Studio'da `UyapEditorMcp.sln` dosyasını aç. Derleme için .NET 10 SDK ve Java 8 hedefini destekleyen bir JDK gerekir. PowerShell örneğindeki dizinleri kurulumuna göre değiştir.

```powershell
./Build.ps1 -EditorDirectory 'C:/UYAP/Editor' -JavaCompiler 'C:/Java/jdk17/bin/javac.exe'
./Package.ps1
```

`Build.ps1`, `app/` altında Windows x64 ve çalışma zamanı dahil bir çıktı oluşturur. `Package.ps1`, `dist/` altında ZIP, MCPB, SHA-256 ve MCP Registry metadata dosyalarını üretir. [Microsoft .NET dağıtım biçimleri](https://learn.microsoft.com/en-us/dotnet/core/deploying/)

İstemciye göre ayrı komut gösterme biçimi, [Microsoft Playwright MCP](https://github.com/microsoft/playwright-mcp) ve [Chrome DevTools MCP](https://github.com/ChromeDevTools/chrome-devtools-mcp) kurulum rehberleriyle karşılaştırıldı.
