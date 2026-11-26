# MCP kataloglarına yayın

| Kanal | Dağıtım | Durum |
| --- | --- | --- |
| GitHub Releases | Windows ZIP ve MCPB | [0.19.0 Windows sürümü](https://github.com/Umutoku/uyap-editor-mcp/releases/tag/v0.19.0). 0.18.0 çevrimiçi tarama bulgusu kendi sürüm notunda. |
| Resmî MCP Registry | GitHub Releases’teki Windows MCPB bağlantısı ve SHA-256 | [0.19.0 kaydı yayında](https://registry.modelcontextprotocol.io/v0.1/servers/io.github.Umutoku%2Fuyap-editor-mcp/versions/0.19.0). Paket özeti doğrulandı. |
| mcp.tc | Açık GitHub deposu ve Windows MCPB | [Katalog kaydı yayında](https://mcp.tc/i/uyap-editor). Yerel stdio olarak listeleniyor. |
| MCP.Directory | Açık GitHub deposu | [Ücretsiz başvuru](https://mcp.directory/submit) alındı. [Katalog aramasında](https://mcp.directory/servers?q=UYAP) kayıt henüz görünmüyor. |
| MCP Market | Açık GitHub deposu | [Ücretsiz sıraya](https://mcpmarket.com/submit) alındı. Katalog, başvuru sırasında yaklaşık 4–6 hafta bekleme süresi gösterdi. |
| MCPServers.org | Açık GitHub deposu ve Registry kimliği | [Katalog kaydı yayında](https://mcpservers.org/ja/servers/umutoku/uyap-editor-mcp). |
| AllMCPs | Açık GitHub deposu | [Katalog kaydı yayında](https://allmcps.com/mcp/uyap-editor-mcp). |
| TensorBlock MCP Index | Açık GitHub deposu | [Katalog PR’ı birleşti](https://github.com/TensorBlock/awesome-mcp-servers/pull/6545). |
| Awesome MCP Servers | Açık GitHub deposu | [Listeye ekleme isteğine](https://github.com/punkpeye/awesome-mcp-servers/pull/20224) Glama rozeti ve Windows kabul kanıtı eklendi. İnceleme bekleniyor. |
| Socketlist | GitHub `mcp-server` konu etiketi | [Keşif etiketi](https://github.com/topics/mcp-server) depoya eklendi. Katalogda görünürlük henüz doğrulanmadı. |
| MCP.so | Açık GitHub deposu | [Önceki ücretsiz başvuru](https://github.com/chatmcp/mcpso/issues/1#issuecomment-7041150758) bekliyor. [Güncel form](https://mcp.so/submit) ücretli gönderim gösteriyor. |
| GitHub MCP Registry | GitHub’ın ayrı, seçkili kataloğu | [Katalog aramasında](https://github.com/mcp?search=UYAP) bulunmadı. [GitHub’ın yayın rehberi](https://github.blog/ai-and-ml/generative-ai/how-to-find-install-and-manage-mcp-servers-with-the-github-mcp-registry/) eklenme isteğini e-posta ile alıyor. |
| Claude Desktop | GitHub’dan Windows `.mcpb` dosyasını indirip elle ekleme | MCPB manifesti doğrulandı. Katalog başvuru formu Google oturumu istiyor. [Kurulum ve başvuru](https://support.claude.com/en/articles/10949351-getting-started-with-local-mcp-servers-on-claude-desktop) |
| Antigravity | [Windows eklenti kaynağı](../integrations/antigravity) ve yerel stdio ayarları | CLI eklenti doğrulaması ve Windows üzerinde 65 araçlık stdio testi geçti. Resmî mağaza formu doğrulanabilir şirket adı ve web alanı istiyor. Bağımsız proje için form gönderilmedi. [Mağaza başvurusu](https://antigravity.google/blog/plugin-marketplace-in-antigravity/) |
| ChatGPT | Web için uzak MCP, masaüstü kataloğu için masaüstü eklentisi | Proje yerel stdio paketi olarak dağıtılır. OpenAI’nin yerel MCP katalog başvurusu onaylı iş ortaklarına açık. [OpenAI masaüstü başvurusu](https://developers.openai.com/plugins/deploy/desktop-only) |
| Glama | Açık GitHub deposu | [Katalog kaydı yayında](https://glama.ai/mcp/servers/Umutoku/uyap-editor-mcp). Windows’a özgü çalışma zamanı Glama’da puanlanmadı. |
| PulseMCP | Topluluk kataloğu | Yeni başvurular geçici olarak durduruldu. [PulseMCP durumu](https://www.pulsemcp.com/submit) |
| Codex, Claude Code, Gemini CLI ve OpenCode | Yerel stdio komutu | Kurulum komutları README’de. |

## Hazırlık

1. Windows x64 sürümünü derle ve kurgu belge kabulünü çalıştır. ZIP’in içindeki çalıştırılabilir dosyayı da ayrı bir dizinde dene.
2. Taranan ZIP ve MCPB’yi aynı sürüm etiketiyle GitHub Releases’e yükle. `dist/SHA256SUMS.txt` dosyasını da ekle.
3. `dist/server.json` içindeki `identifier` bağlantısının yayımlanan MCPB’ye gittiğini ve `fileSha256` değerinin GitHub Releases’teki dosyayla eşleştiğini doğrula.
4. GitHub Actions’ta **Publish MCP Registry entry** görevini yayımlanmış kararlı sürüm numarasıyla çalıştır. Görev, GitHub sürümündeki MCPB özetini metadata ile karşılaştırır ve GitHub OIDC üzerinden yayınlar. Bir sürüm zaten Registry’deyse yeniden gönderme.

```text
gh workflow run publish-mcp-registry.yml --repo Umutoku/uyap-editor-mcp -f version=0.19.0
```

Başarılı bir yayın sonrasında Registry API’sinden aynı sürümü ve dosya özetini doğrula. Bu modelde istemciler MCP’yi kullanıcının Windows oturumunda başlatır. Registry, dağıtılan dosyalar için GitHub Release bağlantısını kullanır. [MCP Registry yayın rehberi](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/github-actions.mdx) · [MCPB paket kuralları](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/package-types.mdx)

Katalog kaydı ve uygulama içi listeleme ayrı işlemlerdir. Bir katalog kendi başvuru veya inceleme sürecini gerektiriyorsa yayımlanan sürüm bağlantısını, kaynak deposunu, Windows x64 gereksinimini ve güvenlik raporlarını başvuruya ekle.

Katalog başvuruları [MIT lisanslı açık kaynak MCP deposuna](https://github.com/Umutoku/uyap-editor-mcp) yönlendirir. UYAP Doküman Editörü kullanıcının Windows bilgisayarına ayrıca kurulur.
