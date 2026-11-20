# MCP kataloglarına yayın

| Kanal | Dağıtım | Durum |
| --- | --- | --- |
| GitHub Releases | Windows ZIP ve MCPB | [0.19 Windows önizlemesi](https://github.com/Umutoku/uyap-editor-mcp/releases/tag/v0.19.0-preview.1). 0.18.0 çevrimiçi tarama bulgusu kendi sürüm notunda. |
| Resmî MCP Registry | GitHub Releases’teki MCPB bağlantısı ve SHA-256 | `io.github.umutoku/uyap-editor-mcp` için metadata hazır. 0.18.0 kaydı başarılı olmadı. |
| GitHub MCP Registry | GitHub’ın katalog listelemesi | Ayrı katalog. Bu projenin listelendiği doğrulanmadı. [GitHub MCP Registry](https://github.com/mcp) |
| Claude Desktop | Windows MCPB uzantısını kullanıcı ekler | MCPB manifesti doğrulandı. İstemci içi kurulum testi açık. |
| Codex, Claude Code ve Gemini CLI | Yerel stdio komutu | Kurulum komutları README’de. |

## Hazırlık

1. Windows x64 sürümünü derle ve kurgu belge kabulünü çalıştır. ZIP’in içindeki çalıştırılabilir dosyayı da ayrı bir dizinde dene.
2. Taranan ZIP ve MCPB’yi aynı sürüm etiketiyle GitHub Releases’e yükle. `dist/SHA256SUMS.txt` dosyasını da ekle.
3. `dist/server.json` içindeki `identifier` bağlantısının yayımlanan MCPB’ye gittiğini ve `fileSha256` değerinin GitHub Releases’teki dosyayla eşleştiğini doğrula.
4. Resmî yayıncı aracını aç, GitHub cihaz girişini tamamla ve aynı oturumda kaydı gönder.

```powershell
mcp-publisher validate ./dist/server.json
mcp-publisher login github
mcp-publisher publish ./dist/server.json
```

Giriş süresi dolarsa giriş ve yayın adımlarını yeniden çalıştır. Başarılı bir yayın sonrasında Registry API’sinden aynı sürümü ve dosya özetini doğrula. Bu modelde istemciler MCP’yi kullanıcının Windows oturumunda başlatır. Registry, dağıtılan dosyalar için GitHub Release bağlantısını kullanır. [MCP Registry yayın rehberi](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/quickstart.mdx) · [MCPB paket kuralları](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/package-types.mdx)

Katalog kaydı ve uygulama içi listeleme ayrı işlemlerdir. Bir katalog kendi başvuru veya inceleme sürecini gerektiriyorsa yayımlanan sürüm bağlantısını, kaynak deposunu, Windows x64 gereksinimini ve güvenlik raporlarını başvuruya ekle.
