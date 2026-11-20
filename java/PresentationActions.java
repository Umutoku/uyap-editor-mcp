import java.awt.Dimension;
import java.awt.Font;
import java.awt.GraphicsEnvironment;
import java.awt.Insets;
import java.awt.Point;
import java.awt.Rectangle;
import java.awt.print.PageFormat;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;
import javax.swing.JComboBox;
import javax.swing.JTextPane;
import javax.swing.JViewport;
import javax.swing.SwingUtilities;
import javax.swing.text.AttributeSet;
import javax.swing.text.JTextComponent;
import javax.swing.text.MutableAttributeSet;
import javax.swing.text.SimpleAttributeSet;
import javax.swing.text.StyleConstants;
import javax.swing.text.StyledDocument;
import javax.swing.text.View;
import com.lowagie.text.pdf.BaseFont;
import com.lowagie.text.pdf.FontMapper;
import tr.com.havelsan.uyap.system.swing.wp.l;

final class PresentationActions {
    private static final String LIRA = "\u20BA";
    private static final String LEGACY_FAMILY = tr.com.havelsan.uyap.system.editor.common.text.J.o;
    private static final float MINIMUM_SCALE = 0.1f;

    private PresentationActions() {
    }

    static boolean hasLegacyLiraFont() {
        return Arrays.asList(fontFamilies()).contains(LEGACY_FAMILY);
    }

    static void insertUnicodeLira(JTextComponent editor) throws Exception {
        require(editor instanceof JTextPane && editor.isEditable(), "EDITOR_READ_ONLY");
        JTextPane pane = (JTextPane) editor;
        StyledDocument document = pane.getStyledDocument();
        int start = pane.getSelectionStart();
        int expectedLength = document.getLength() - (pane.getSelectionEnd() - start) + LIRA.length();
        AttributeSet typingAttributes = pane.getInputAttributes().copyAttributes();
        String currentFamily = StyleConstants.getFontFamily(pane.getCharacterAttributes());
        String family = liraFamily(currentFamily);
        SimpleAttributeSet symbolAttributes = new SimpleAttributeSet();
        StyleConstants.setFontFamily(symbolAttributes, family);
        try {
            pane.replaceSelection(LIRA);
            require(document.getLength() == expectedLength && LIRA.equals(document.getText(start, LIRA.length())),
                "TL_SYMBOL_INSERT_UNVERIFIED");
            document.setCharacterAttributes(start, LIRA.length(), symbolAttributes, false);
            require(family.equals(StyleConstants.getFontFamily(document.getCharacterElement(start).getAttributes())),
                "TL_SYMBOL_FONT_UNVERIFIED");
        } finally {
            MutableAttributeSet input = pane.getInputAttributes();
            input.removeAttributes(input);
            input.addAttributes(typingAttributes);
        }
    }

    private static String[] fontFamilies() {
        return GraphicsEnvironment.getLocalGraphicsEnvironment().getAvailableFontFamilyNames();
    }

    private static String liraFamily(String current) throws EditorLauncher.UiFailure {
        String[] available = fontFamilies();
        List<String> installed = Arrays.asList(available);
        for (String name : new String[] { current, "Arial", "Segoe UI", "DejaVu Sans", "Calibri" }) {
            if (installed.contains(name) && new Font(name, Font.PLAIN, 12).canDisplay(0x20BA)) {
                return name;
            }
        }
        throw new EditorLauncher.UiFailure("TL_SYMBOL_FONT_UNAVAILABLE");
    }

    static final class PdfLiraFonts implements AutoCloseable {
        private final List<FontEncoding> overrides = new ArrayList<>();

        static PdfLiraFonts prepare(JTextComponent editor) throws Exception {
            PdfLiraFonts scope = new PdfLiraFonts();
            if (!(editor.getDocument() instanceof StyledDocument)) {
                return scope;
            }
            StyledDocument document = (StyledDocument) editor.getDocument();
            Set<Font> fonts = new LinkedHashSet<>();
            for (int offset = 0; offset < document.getLength(); offset += 8192) {
                String segment = document.getText(offset, Math.min(8192, document.getLength() - offset));
                for (int index = segment.indexOf(LIRA); index >= 0; index = segment.indexOf(LIRA, index + 1)) {
                    fonts.add(document.getFont(document.getCharacterElement(offset + index).getAttributes()));
                }
            }
            if (fonts.isEmpty()) {
                return scope;
            }

            // The pinned native PDF converter's font registry defaults to Windows-1254.
            Class<?> converter = Class.forName("tr.com.havelsan.uyap.system.editor.b.b");
            Method nativeRegistry = converter.getDeclaredMethod("a");
            nativeRegistry.setAccessible(true);
            Object registry = nativeRegistry.invoke(null);
            require(registry instanceof FontMapper, "PDF_LIRA_FONT_UNAVAILABLE");
            Method findEntry = Arrays.stream(registry.getClass().getMethods())
                .filter(method -> "a".equals(method.getName())
                    && Arrays.equals(method.getParameterTypes(), new Class<?>[] { String.class })
                    && "tr.com.havelsan.uyap.system.editor.b.d".equals(method.getReturnType().getName()))
                .findFirst().orElseThrow(() -> new NoSuchMethodException("Native PDF font entry"));
            try {
                for (Font font : fonts) {
                    require(font != null, "PDF_LIRA_FONT_UNAVAILABLE");
                    Object entry = findEntry.invoke(registry, font.getFontName());
                    require(entry != null, "PDF_LIRA_FONT_UNAVAILABLE");
                    FontEncoding override = new FontEncoding(entry);
                    if (!BaseFont.IDENTITY_H.equals(override.encoding.get(entry)) || !override.embedded.getBoolean(entry)) {
                        scope.overrides.add(override);
                        override.encoding.set(entry, BaseFont.IDENTITY_H);
                        override.embedded.setBoolean(entry, true);
                    }
                    require(((FontMapper) registry).awtToPdf(font).charExists(0x20BA), "PDF_LIRA_FONT_UNAVAILABLE");
                }
                return scope;
            } catch (Exception failure) {
                scope.close();
                throw failure;
            }
        }

        boolean changed() {
            return !overrides.isEmpty();
        }

        @Override
        public void close() throws ReflectiveOperationException {
            for (int index = overrides.size() - 1; index >= 0; index--) {
                FontEncoding original = overrides.get(index);
                original.encoding.set(original.entry, original.previousEncoding);
                original.embedded.setBoolean(original.entry, original.previousEmbedded);
            }
        }

        private static final class FontEncoding {
            final Object entry;
            final Field encoding;
            final Field embedded;
            final Object previousEncoding;
            final boolean previousEmbedded;

            FontEncoding(Object entry) throws ReflectiveOperationException {
                this.entry = entry;
                encoding = field(entry, "b", String.class);
                embedded = field(entry, "a", boolean.class);
                previousEncoding = encoding.get(entry);
                previousEmbedded = embedded.getBoolean(entry);
            }

            private static Field field(Object entry, String name, Class<?> type) throws NoSuchFieldException {
                for (Field field : entry.getClass().getFields()) {
                    if (field.getName().equals(name) && field.getType() == type && !Modifier.isStatic(field.getModifiers())) {
                        return field;
                    }
                }
                throw new NoSuchFieldException(name);
            }
        }
    }

    static String fitPage(JTextComponent editor, JComboBox<?> zoomControl, float fullSizeScale, int page)
        throws Exception {
        require(editor instanceof l && editor.getUI() != null, "PAGE_FIT_VIEW_UNAVAILABLE");
        JViewport viewport = (JViewport) SwingUtilities.getAncestorOfClass(JViewport.class, editor);
        require(viewport != null && viewport.getView() == editor, "PAGE_FIT_VIEWPORT_UNAVAILABLE");
        View root = editor.getUI().getRootView(editor);
        require(root.getViewCount() == 1, "PAGE_FIT_VIEW_UNAVAILABLE");

        // Read the same page formatter used by the native paginated view.
        View view = root.getView(0);
        Object formatter = getter(view, "b", "tr.com.havelsan.uyap.system.swing.wp.b.ar");
        PageFormat format = (PageFormat) getter(formatter, "b", "tr.com.havelsan.uyap.system.swing.wp.model.P");
        int stride = (Integer) getter(formatter, "o", "int");
        double width = format.getWidth();
        double height = format.getHeight();
        require(width > 0 && height > 0 && stride >= (int) height, "PAGE_FIT_LAYOUT_UNAVAILABLE");
        Insets insets = editor.getInsets();
        Dimension extent = viewport.getExtentSize();
        double usableWidth = extent.width - insets.left - insets.right - 4;
        double usableHeight = extent.height - insets.top - insets.bottom - 4;
        // Reserve the native paper separation as well as the component border.
        double maximum = Math.min(usableWidth / width, usableHeight / stride);
        int percent = Math.min(400, (int) Math.floor(maximum * 100 / fullSizeScale));
        float requested = fullSizeScale * percent / 100;
        require(requested >= MINIMUM_SCALE, "PAGE_FIT_VIEWPORT_TOO_SMALL");

        l renderer = (l) editor;
        String zoomLabel = "%" + percent;
        zoomControl.getModel().setSelectedItem(zoomLabel);
        if (Math.abs(renderer.a() - requested) >= 0.001f) {
            renderer.a(requested);
        }
        double applied = renderer.a();
        require(Math.abs(applied - requested) < 0.001 && zoomLabel.equals(zoomControl.getSelectedItem()),
            "PAGE_FIT_ZOOM_UNVERIFIED");
        viewport.doLayout();
        int offsetY = (int) Math.floor((page - 1) * (long) stride * applied);
        viewport.setViewPosition(new Point(0,
            Math.min(Math.max(0, editor.getHeight() - viewport.getExtentSize().height), offsetY)));
        Rectangle fitted = viewport.getViewRect();
        int pageWidth = (int) Math.ceil(width * applied);
        int pageHeight = (int) Math.ceil(height * applied);
        // The native page painter places the paper inside equal gaps within each page stride.
        double paperStart = (page - 1) * (long) stride + (stride - (int) height) / 2.0;
        int pageTop = (int) Math.floor(paperStart * applied);
        int pageBottom = (int) Math.ceil((paperStart + height) * applied);
        require(pageWidth <= fitted.width && pageHeight <= fitted.height
            && fitted.y <= pageTop && pageBottom <= fitted.y + fitted.height, "PAGE_FIT_GEOMETRY_UNVERIFIED");
        return String.format(Locale.ROOT,
            "{\"page\":%d,\"zoomPercent\":%d,\"pageWidth\":%d,\"pageHeight\":%d,"
                + "\"viewportWidth\":%d,\"viewportHeight\":%d,\"pageTop\":%d,\"pageBottom\":%d,\"viewportTop\":%d}",
            page, percent, pageWidth, pageHeight, fitted.width, fitted.height, pageTop, pageBottom, fitted.y);
    }

    private static Object getter(Object target, String name, String result) throws Exception {
        if (target != null) {
            for (Method method : target.getClass().getMethods()) {
                if (name.equals(method.getName()) && method.getParameterCount() == 0
                    && result.equals(method.getReturnType().getName())) {
                    return method.invoke(target);
                }
            }
        }
        throw new EditorLauncher.UiFailure("PAGE_FIT_LAYOUT_UNAVAILABLE");
    }

    private static void require(boolean verified, String failure) throws EditorLauncher.UiFailure {
        if (!verified) {
            throw new EditorLauncher.UiFailure(failure);
        }
    }
}
