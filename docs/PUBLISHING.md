# MCP kataloglarına yayın

| Kanal | Dağıtım | Durum |
| --- | --- | --- |
| GitHub Releases | Windows ZIP ve MCPB | [0.19.0 Windows sürümü](https://github.com/Umutoku/uyap-editor-mcp/releases/tag/v0.19.0). 0.18.0 çevrimiçi tarama bulgusu kendi sürüm notunda. |
| Resmî MCP Registry | GitHub Releases’teki Windows MCPB bağlantısı ve SHA-256 | [0.19.0 kaydı yayında](https://registry.modelcontextprotocol.io/v0.1/servers/io.github.Umutoku%2Fuyap-editor-mcp/versions/0.19.0). Paket özeti doğrulandı. |
| MCP.so | Açık GitHub deposu | [Başvuru gönderildi](https://github.com/chatmcp/mcpso/issues/1#issuecomment-7041150758). Katalog kaydı bekliyor. |
| GitHub MCP Registry | GitHub’ın ayrı, seçkili kataloğu | Listeleme doğrulanmadı. [GitHub MCP Registry](https://github.com/mcp) |
| Claude Desktop | GitHub’dan Windows `.mcpb` dosyasını indirip elle ekleme | MCPB manifesti doğrulandı. Katalog başvuru formu Google oturumu istiyor. [Kurulum ve başvuru](https://support.claude.com/en/articles/10949351-getting-started-with-local-mcp-servers-on-claude-desktop) |
| Antigravity IDE | `mcp_config.json` dosyasına yerel stdio ayarlarını ekleme | Kurulum komutu README’de. Resmî Plugin Marketplace, doğrulanmış eklenti paketi ve iş ortağı başvurusu istiyor. [MCP kurulumu](https://antigravity.google/docs/mcp) · [Mağaza başvurusu](https://antigravity.google/blog/plugin-marketplace-in-antigravity/) |
| ChatGPT | Web için uzak MCP, masaüstü kataloğu için masaüstü eklentisi | Proje yerel stdio paketi olarak dağıtılır. OpenAI’nin yerel MCP katalog başvurusu onaylı iş ortaklarına açık. [OpenAI masaüstü başvurusu](https://developers.openai.com/plugins/deploy/desktop-only) |
| Glama | Açık GitHub deposu | Add Server ekranı hesapla giriş istiyor. [Glama başvuru rehberi](https://glama.ai/mcp/faq) |
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
