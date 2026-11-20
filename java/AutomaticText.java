import java.io.InputStream;
import java.lang.reflect.Method;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import javax.swing.KeyStroke;
import javax.swing.text.DefaultCaret;
import javax.swing.text.Document;
import javax.swing.text.JTextComponent;
import tr.com.havelsan.uyap.system.editor.common.an;
import tr.com.havelsan.uyap.system.editor.common.c;
import tr.com.havelsan.uyap.system.editor.common.h;
import tr.com.havelsan.uyap.system.editor.common.gui.WPDocumentPanel;
import tr.com.havelsan.uyap.system.editor.common.gui.lN;
import tr.com.havelsan.uyap.system.editor.common.gui.lR;
import tr.com.havelsan.uyap.system.editor.common.gui.lo;
import tr.com.havelsan.uyap.system.editor.common.gui.np;
import tr.com.havelsan.uyap.system.editor.common.text.EditorDataFlavor;
import tr.com.havelsan.uyap.system.editor.common.text.fi;
import tr.com.havelsan.uyap.system.editor.common.text.gC;
import tr.com.havelsan.uyap.system.editor.common.text.gF;
import tr.com.havelsan.uyap.system.editor.common.text.gz;

final class AutomaticText {
    private static final Locale TURKISH = new Locale("tr", "TR");
    private static final int LIMIT = 100;

    private AutomaticText() {}

    static String[] list() throws Exception {
        Path store = checkedStore();
        List<String> rows = new ArrayList<>();
        addEntries(rows, nativeEntries(false), "plain");
        addEntries(rows, nativeEntries(true), "rich");
        addShortcuts(rows, store);
        return rows.toArray(new String[0]);
    }

    static String[] set(JTextComponent editor, String inputKey, String kind, String operation, String value)
        throws Exception {
        require(editor instanceof fi && editor.isEditable(), "EDITOR_READ_ONLY");
        Path store = checkedStore();
        switch (kind) {
            case "plain":
                setPlain(key(inputKey), operation, value);
                break;
            case "rich":
                setRich((fi) editor, store, key(inputKey), operation, value);
                break;
            case "shortcut":
                setShortcut((fi) editor, store, stroke(inputKey), operation, value);
                break;
            default:
                throw new EditorLauncher.UiFailure("AUTO_TEXT_KIND_INVALID");
        }
        return list();
    }

    static String[] insert(JTextComponent editor, String inputKey, String kind) throws Exception {
        require(editor instanceof fi && editor.isEditable(), "EDITOR_READ_ONLY");
        Path store = checkedStore();
        int start = editor.getSelectionStart();
        int before = editor.getDocument().getLength();
        int removed = editor.getSelectionEnd() - start;
        String appliedKey;
        String plain = null;
        if ("shortcut".equals(kind)) {
            KeyStroke stroke = stroke(inputKey);
            String id = an.a(stroke);
            gF entry = checkedShortcut(shortcutBindings(), store, id);
            require(entry != null, "AUTO_TEXT_NOT_FOUND");
            shortcutRegistry((fi) editor).a(shortcutPath(store, stroke).toString());
            appliedKey = stroke.toString();
        } else {
            require("plain".equals(kind) || "rich".equals(kind), "AUTO_TEXT_KIND_INVALID");
            appliedKey = key(inputKey);
            boolean rich = "rich".equals(kind);
            HashMap<?, ?> matches = h.a(appliedKey, rich);
            Object value = matches.get(appliedKey);
            require(value instanceof String, "AUTO_TEXT_NOT_FOUND");
            if (rich) {
                Path path = store.resolve("otomatikmetin").resolve(appliedKey + ".udf");
                fi source = loadFragment(path);
                source.select(0, source.getDocument().getLength());
                EditorDataFlavor payload =
                    (EditorDataFlavor) accessor(fi.class, "a", EditorDataFlavor.class).invoke(source);
                require(payload != null, "AUTO_TEXT_FRAGMENT_INVALID");
                ((fi) editor).a(payload);
            } else {
                plain = (String) value;
                editor.replaceSelection(plain);
            }
        }
        Document changed = editor.getDocument();
        int inserted = changed.getLength() - before + removed;
        require(inserted > 0 && (plain == null || plain.equals(changed.getText(start, inserted))),
            "AUTO_TEXT_INSERT_UNVERIFIED");
        return new String[] { appliedKey, kind, Integer.toString(start), Integer.toString(start + inserted) };
    }

    private static void setPlain(String key, String operation, String text) throws Exception {
        c existing = nativeEntries(false);
        ArrayList<String> keys = new ArrayList<>();
        ArrayList<String> values = new ArrayList<>();
        if (existing != null && existing.a() != null && existing.b() != null) {
            for (int i = 0; i < existing.a().size(); i++) {
                keys.add((String) existing.a().get(i));
                values.add((String) existing.b().get(i));
            }
        }
        int index = keys.indexOf(key);
        if ("delete".equals(operation)) {
            require(index >= 0, "AUTO_TEXT_NOT_FOUND");
            keys.remove(index);
            values.remove(index);
        } else {
            require("set".equals(operation), "AUTO_TEXT_OPERATION_INVALID");
            String normalized = text.replace("\r\n", "\n").replace('\r', '\n');
            require(normalized.length() >= 3 && normalized.length() <= 8192 &&
                normalized.codePoints().allMatch(AutomaticText::xmlCharacter), "AUTO_TEXT_VALUE_INVALID");
            require(!matchesKey(nativeEntries(true), key), "AUTO_TEXT_KEY_CONFLICT");
            if (index < 0) {
                require(keys.size() < LIMIT, "AUTO_TEXT_LIMIT");
                keys.add(key);
                values.add(normalized);
            } else {
                values.set(index, normalized);
            }
        }
        c updated = new c();
        updated.a(keys);
        updated.b(values);
        new h().a(updated);
        c saved = nativeEntries(false);
        require(saved != null && keys.equals(saved.a()) && values.equals(saved.b()), "AUTO_TEXT_SAVE_UNVERIFIED");
    }

    private static void setRich(fi editor, Path store, String key, String operation, String value) throws Exception {
        lN registry = (lN) accessor(lo.class, "a", lN.class).invoke(owner(editor));
        require(registry != null, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
        Path directory = store.resolve("otomatikmetin");
        Files.createDirectories(directory);
        require(directory.toRealPath().equals(directory), "AUTO_TEXT_PROFILE_INVALID");
        Path fragment = directory.resolve(key + ".udf");
        checkedExistingFile(fragment);
        c existing = nativeEntries(true);
        if ("delete".equals(operation)) {
            require(matchesKey(existing, key), "AUTO_TEXT_NOT_FOUND");
            accessor(lN.class, "a", Object.class, String.class).invoke(registry, key);
            Files.deleteIfExists(fragment);
        } else {
            require("set".equals(operation), "AUTO_TEXT_OPERATION_INVALID");
            require(!matchesKey(nativeEntries(false), key), "AUTO_TEXT_KEY_CONFLICT");
            require(matchesKey(existing, key) || existing == null || existing.a() == null
                || existing.a().size() < LIMIT, "AUTO_TEXT_LIMIT");
            String preview = storeFragment(store, fragment, value);
            registry.a(key, preview, fragment.toString());
        }
        profiler().c();
        accessor(h.class, "b", void.class).invoke(null);
        require(matchesKey(nativeEntries(true), key) == !"delete".equals(operation), "AUTO_TEXT_SAVE_UNVERIFIED");
    }

    private static void setShortcut(fi editor, Path store, KeyStroke stroke, String operation, String value) throws Exception {
        gz bindings = shortcutBindings();
        String id = an.a(stroke);
        gF existing = checkedShortcut(bindings, store, id);
        Path path = shortcutPath(store, stroke);
        Files.createDirectories(path.getParent());
        require(path.getParent().toRealPath().equals(path.getParent()), "AUTO_TEXT_PROFILE_INVALID");
        checkedExistingFile(path);
        if ("delete".equals(operation)) {
            require(existing != null, "AUTO_TEXT_NOT_FOUND");
            bindings.remove(id);
            Files.deleteIfExists(path);
        } else {
            require("set".equals(operation), "AUTO_TEXT_OPERATION_INVALID");
            require(existing != null || bindings.get(id) == null, "AUTO_TEXT_SHORTCUT_IN_USE");
            require(editor.getInputMap().get(stroke) == null, "AUTO_TEXT_SHORTCUT_IN_USE");
            String preview = storeFragment(store, path, value);
            shortcutRegistry(editor).a(stroke, path.toString(), preview);
        }
        profiler().c();
        require((checkedShortcut(bindings, store, id) != null) == !"delete".equals(operation), "AUTO_TEXT_SAVE_UNVERIFIED");
    }

    private static String storeFragment(Path store, Path fragment, String value) throws Exception {
        Path source = EditorFileScope.profileInput(value, "AUTO_TEXT_FRAGMENT_INVALID").toPath();
        require(source.getParent().equals(store.getParent())
            && source.getFileName().toString().matches("\\.auto-text-[0-9a-f]+\\.udf"), "AUTO_TEXT_FRAGMENT_INVALID");
        Document doc = loadFragment(source).getDocument();
        String preview = doc.getText(0, Math.min(doc.getLength(), 30)).trim();
        require(!preview.isEmpty(), "AUTO_TEXT_FRAGMENT_INVALID");
        Files.move(source, fragment, StandardCopyOption.ATOMIC_MOVE, StandardCopyOption.REPLACE_EXISTING);
        return preview;
    }

    private static fi loadFragment(Path path) throws Exception {
        require(Files.isRegularFile(path) && path.toRealPath().equals(path), "AUTO_TEXT_FRAGMENT_UNAVAILABLE");
        WPDocumentPanel fragment = new WPDocumentPanel();
        fi source = (fi) accessor(WPDocumentPanel.class, "a", fi.class).invoke(fragment);
        source.setCaret(new DefaultCaret());
        try (InputStream stream = Files.newInputStream(path)) {
            fragment.a(stream);
        }
        require(source.getDocument().getLength() > 0, "AUTO_TEXT_FRAGMENT_INVALID");
        return source;
    }

    private static void addShortcuts(List<String> rows, Path store) throws Exception {
        gz bindings = shortcutBindings();
        for (Object key : bindings.keySet()) {
            if (key instanceof String && ((String) key).matches("[0-9]+\\|[0-9]+")) {
                String id = (String) key;
                gF entry = checkedShortcut(bindings, store, id);
                if (entry != null) {
                    rows.add(profileStroke(id).toString());
                    rows.add("shortcut");
                    rows.add((String) accessor(gC.class, "c", String.class).invoke(entry));
                }
            }
        }
    }

    private static gF checkedShortcut(gz bindings, Path store, String id) throws Exception {
        Object entry = bindings.get(id);
        if (!(entry instanceof gF)) {
            return null;
        }
        Path path = shortcutPath(store, profileStroke(id));
        Object registered = accessor(gC.class, "b", String.class).invoke(entry);
        return path.toString().equals(registered) && Files.isRegularFile(path) && path.toRealPath().equals(path)
            ? (gF) entry : null;
    }

    private static Path shortcutPath(Path store, KeyStroke stroke) {
        return store.resolve("kisayollar").resolve("mcp-" + stroke.getKeyCode() + "-" + stroke.getModifiers() + ".udf");
    }

    private static gz shortcutBindings() throws Exception {
        gz bindings = (gz) accessor(an.class, "a", gz.class, String.class).invoke(profiler(), "keyBindings");
        require(bindings != null, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
        return bindings;
    }

    private static lR shortcutRegistry(fi editor) throws Exception {
        return (lR) accessor(lo.class, "a", lR.class).invoke(owner(editor));
    }

    private static Object owner(fi editor) throws Exception {
        Object context = accessor(fi.class, "a", tr.com.havelsan.uyap.a.a.a.d.class).invoke(editor);
        require(context instanceof lo, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
        return context;
    }

    private static an profiler() throws Exception {
        return (an) accessor(an.class, "a", an.class).invoke(null);
    }

    private static KeyStroke profileStroke(String key) throws Exception {
        return (KeyStroke) accessor(an.class, "a", KeyStroke.class, String.class).invoke(null, key);
    }

    private static KeyStroke stroke(String input) throws EditorLauncher.UiFailure {
        require(input.length() <= 80, "AUTO_TEXT_KEY_INVALID");
        KeyStroke stroke = KeyStroke.getKeyStroke(input);
        require(stroke != null && !stroke.isOnKeyRelease() && stroke.getKeyCode() > 0 && stroke.getModifiers() != 0
            && np.a(stroke), "AUTO_TEXT_KEY_INVALID");
        return stroke;
    }

    private static void addEntries(List<String> rows, c entries, String kind) throws EditorLauncher.UiFailure {
        if (entries == null || entries.a() == null) {
            return;
        }
        require(entries.b() != null && entries.a().size() == entries.b().size()
            && entries.a().size() <= LIMIT, "AUTO_TEXT_STORE_INVALID");
        for (int i = 0; i < entries.a().size(); i++) {
            rows.add((String) entries.a().get(i));
            rows.add(kind);
            rows.add((String) entries.b().get(i));
        }
    }

    private static c nativeEntries(boolean rich) throws Exception {
        return (c) accessor(h.class, rich ? "b" : "a", c.class).invoke(null);
    }

    private static boolean matchesKey(c entries, String key) {
        return entries != null && entries.a() != null && entries.a().contains(key);
    }

    private static Path checkedStore() throws Exception {
        Path store = EditorFileScope.profile().resolve(".uki");
        if (!Files.exists(store, LinkOption.NOFOLLOW_LINKS)) {
            Files.createDirectory(store);
        }
        require(Files.isDirectory(store, LinkOption.NOFOLLOW_LINKS)
            && store.toRealPath().equals(store), "AUTO_TEXT_PROFILE_INVALID");
        for (String name : new String[] { "autoText.xml", "tercihler.xml" }) {
            checkedExistingFile(store.resolve(name));
        }
        return store;
    }

    private static void checkedExistingFile(Path file) throws Exception {
        require(!Files.exists(file, LinkOption.NOFOLLOW_LINKS)
            || (Files.isRegularFile(file) && file.toRealPath().equals(file)), "AUTO_TEXT_PROFILE_INVALID");
    }

    private static String key(String input) throws EditorLauncher.UiFailure {
        String normalized = input.toLowerCase(TURKISH);
        require(normalized.matches("[\\p{L}\\p{N}_-]{3,40}") &&
            !normalized.matches("(?i:con|prn|aux|nul|com[1-9]|lpt[1-9])"), "AUTO_TEXT_KEY_INVALID");
        return normalized;
    }

    private static boolean xmlCharacter(int cp) {
        return cp == '\t' || cp == '\n' || cp >= 0x20 && cp <= 0xd7ff ||
            cp >= 0xe000 && cp <= 0xfffd || cp >= 0x10000;
    }

    private static Method accessor(Class<?> owner, String name, Class<?> result, Class<?>... parameters)
        throws NoSuchMethodException {
        return Arrays.stream(owner.getMethods())
            .filter(method -> method.getName().equals(name) && Arrays.equals(method.getParameterTypes(), parameters)
                && method.getReturnType() == result)
            .findFirst().orElseThrow(NoSuchMethodException::new);
    }

    private static void require(boolean value, String code) throws EditorLauncher.UiFailure {
        if (!value) {
            throw new EditorLauncher.UiFailure(code);
        }
    }
}
