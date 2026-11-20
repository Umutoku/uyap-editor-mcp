import java.awt.Container;
import java.awt.Window;
import java.awt.event.ActionEvent;
import java.io.ByteArrayOutputStream;
import java.io.OutputStream;
import java.io.PrintStream;
import java.nio.charset.Charset;
import java.util.Arrays;
import java.util.List;
import javax.swing.Action;
import javax.swing.JEditorPane;
import javax.swing.JFrame;
import javax.swing.JMenuItem;
import javax.swing.JTextPane;
import javax.swing.SwingUtilities;
import javax.swing.text.AbstractDocument;
import javax.swing.text.Element;
import javax.swing.text.JTextComponent;
import tr.com.havelsan.uyap.a.a.a.d;
import tr.com.havelsan.uyap.system.editor.common.text.DocumentEx;
import tr.com.havelsan.uyap.system.editor.common.text.gN;
import tr.com.havelsan.uyap.system.editor.common.text.gT;
import tr.com.havelsan.uyap.system.swing.wp.model.ad;

final class SpecialActions {
    private static final int MAX_DIAGNOSTIC_BYTES = 32 * 1024;
    private static final int MAX_METRIC_SELECTION = 1024;
    private static final int MAX_MODEL_LENGTH = 100_000;

    private SpecialActions() {}

    static void deleteEntryFieldGroup(JTextComponent editor) throws EditorLauncher.UiFailure {
        if (!(editor instanceof JEditorPane) || !(editor.getDocument() instanceof DocumentEx)
            || !(((JEditorPane) editor).getEditorKit() instanceof gN) || !editor.isEditable()) {
            throw new EditorLauncher.UiFailure("TEMPLATE_SESSION_REQUIRED");
        }
        d context = (d) SwingUtilities.getAncestorOfClass(d.class, editor);
        if (context == null) {
            throw new EditorLauncher.UiFailure("TEMPLATE_CONTEXT_MISSING");
        }
        DocumentEx document = (DocumentEx) editor.getDocument();
        int first = editor.getSelectionStart();
        String group = (String) document.getCharacterElement(first).getAttributes().getAttribute(ad.g);
        Element root = document.getDefaultRootElement();
        if (group == null || countParagraphs(root, group) == 0) {
            throw new EditorLauncher.UiFailure("TEMPLATE_ENTRY_GROUP_NOT_SELECTED");
        }
        editor.setCaretPosition(first);
        if (editor.getCaretPosition() != first) {
            throw new EditorLauncher.UiFailure("TEMPLATE_FIELD_POSITION_UNVERIFIED");
        }
        // gT requires a menu-item source. Bind the event to this managed template explicitly.
        gT action = new gT((gN) ((JEditorPane) editor).getEditorKit()) {
            @Override
            protected d b(ActionEvent event) {
                return context;
            }
        };
        action.actionPerformed(new ActionEvent(new JMenuItem(action), ActionEvent.ACTION_PERFORMED,
            "delete-entryfield-group"));
        if (countParagraphs(root, group) != 0) {
            throw new EditorLauncher.UiFailure("TEMPLATE_ENTRY_GROUP_DELETE_UNVERIFIED");
        }
    }

    private static int countParagraphs(Element root, String group) {
        int count = 0;
        for (int i = 0; i < root.getElementCount(); i++) {
            if (group.equals(root.getElement(i).getAttributes().getAttribute(ad.g))) {
                count++;
            }
        }
        return count;
    }

    static String dumpModel(JTextComponent editor) throws Exception {
        if (!(editor.getDocument() instanceof AbstractDocument)) {
            throw new EditorLauncher.UiFailure("DIAGNOSTIC_DOCUMENT_UNAVAILABLE");
        }
        if (editor.getDocument().getLength() > MAX_MODEL_LENGTH) {
            throw new EditorLauncher.UiFailure("DIAGNOSTIC_DOCUMENT_TOO_LARGE");
        }
        // Swing's dump-model calls this method with System.err. Capture it independently.
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        OutputStream bounded = new OutputStream() {
            @Override
            public void write(int value) {
                if (bytes.size() < MAX_DIAGNOSTIC_BYTES) {
                    bytes.write(value);
                }
            }

            @Override
            public void write(byte[] value, int offset, int length) {
                bytes.write(value, offset, Math.min(length, MAX_DIAGNOSTIC_BYTES - bytes.size()));
            }
        };
        try (PrintStream output = new PrintStream(bounded, false, "UTF-8")) {
            ((AbstractDocument) editor.getDocument()).dump(output);
        }
        // Swing's Java 8 element dumper selects JavaEsc and falls back to the JVM charset.
        Charset encoding = Charset.isSupported("JavaEsc") ? Charset.forName("JavaEsc") : Charset.defaultCharset();
        String diagnostic = new String(bytes.toByteArray(), encoding);
        return bytes.size() == MAX_DIAGNOSTIC_BYTES ? diagnostic + "\n[truncated]" : diagnostic;
    }

    static String layoutMetrics(JTextComponent editor, Action action) throws EditorLauncher.UiFailure {
        int length = editor.getSelectionEnd() - editor.getSelectionStart();
        if (length < 1 || length > MAX_METRIC_SELECTION) {
            throw new EditorLauncher.UiFailure("DIAGNOSTIC_SELECTION_REQUIRED");
        }
        List<Window> previous = Arrays.asList(Window.getWindows());
        action.actionPerformed(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, "debug-lvuMetrics"));
        for (Window window : Window.getWindows()) {
            if (window instanceof JFrame && !previous.contains(window) && window.isShowing()
                && "debug-lvuMetrics".equals(((JFrame) window).getTitle())) {
                try {
                    JTextPane pane = findMetricText(((JFrame) window).getContentPane());
                    if (pane == null || pane.getDocument().getLength() == 0) {
                        throw new EditorLauncher.UiFailure("DIAGNOSTIC_OUTPUT_UNVERIFIED");
                    }
                    String text = pane.getText();
                    return text.length() > MAX_DIAGNOSTIC_BYTES
                        ? text.substring(0, MAX_DIAGNOSTIC_BYTES) + "\n[truncated]" : text;
                } finally {
                    window.dispose();
                }
            }
        }
        throw new EditorLauncher.UiFailure("DIAGNOSTIC_OUTPUT_UNVERIFIED");
    }

    private static JTextPane findMetricText(Container parent) {
        for (java.awt.Component component : parent.getComponents()) {
            if (component instanceof JTextPane) {
                return (JTextPane) component;
            }
            if (component instanceof Container) {
                JTextPane inside = findMetricText((Container) component);
                if (inside != null) {
                    return inside;
                }
            }
        }
        return null;
    }
}
