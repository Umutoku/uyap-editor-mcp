import java.awt.Frame;
import java.lang.reflect.Method;
import java.net.URI;
import javax.swing.JTextField;
import javax.swing.SwingUtilities;
import javax.swing.text.Element;
import javax.swing.text.JTextComponent;
import tr.com.havelsan.uyap.a.a.a.d;
import tr.com.havelsan.uyap.system.editor.common.gui.jF;
import tr.com.havelsan.uyap.system.editor.common.gui.lo;
import tr.com.havelsan.uyap.system.editor.common.gui.mK;
import tr.com.havelsan.uyap.system.editor.common.text.DocumentEx;
import tr.com.havelsan.uyap.system.editor.common.text.fi;
import tr.com.havelsan.uyap.system.swing.wp.model.V;

final class FormActions {
    private FormActions() { }

    static boolean needsContext(String id) {
        return "insert-url-link".equals(id) || "table-properties-new".equals(id);
    }

    static void requireContext(String id, JTextComponent component)
        throws ReflectiveOperationException, EditorLauncher.UiFailure {
        require(component instanceof fi && component.isEditable() && component.getDocument() instanceof DocumentEx,
            "FORM_CONTEXT_UNAVAILABLE");
        fi pane = (fi) component;
        DocumentEx document = (DocumentEx) pane.getDocument();
        int bodyMode = Class.forName("tr.com.havelsan.uyap.system.swing.wp.a.f").getField("j").getInt(null);
        require(editMode(document) == bodyMode, "FORM_BODY_MODE_REQUIRED");
        if ("insert-url-link".equals(id)) {
            require(pane.getSelectionEnd() > pane.getSelectionStart(), "HYPERLINK_SELECTION_REQUIRED");
        } else {
            require(SwingUtilities.getAncestorOfClass(d.class, pane) instanceof lo && selectedTable(pane) != null,
                "TABLE_CELL_SELECTION_REQUIRED");
        }
    }

    static void open(String id, JTextComponent pane, Frame owner) throws Exception {
        requireContext(id, pane);
        fi editor = (fi) pane;
        if ("insert-url-link".equals(id)) {
            insertHyperlink(editor, owner);
        } else {
            // Both native table properties callbacks construct this dialog in table mode 0.
            mK properties = new mK(owner, editor, selectedTable(editor), 0);
            properties.setVisible(true);
        }
    }

    private static Element selectedTable(fi editor) {
        DocumentEx document = (DocumentEx) editor.getDocument();
        return DocumentEx.a(document.getCharacterElement(editor.getCaretPosition()), "table");
    }

    private static void insertHyperlink(fi editor, Frame owner) throws Exception {
        DocumentEx document = (DocumentEx) editor.getDocument();
        int start = editor.getSelectionStart();
        int end = editor.getSelectionEnd();
        String selected = document.getText(start, end - start);
        // This is the same local dialog and document write as the stock hyperlink callback.
        jF dialog = new jF(owner, "Köprü Ekle", true);
        dialog.setSize(230, 120);
        int decision = (Integer) nativeMethod(jF.class, "a", int.class).invoke(dialog);
        if (decision != 0) {
            return;
        }
        JTextField field = (JTextField) nativeMethod(jF.class, "a",
            Class.forName("tr.com.havelsan.uyap.system.editor.common.gui.a.R")).invoke(dialog);
        String url = field.getText().trim();
        require(validLink(url), "HYPERLINK_URL_INVALID");
        require(editor.getDocument() == document && end <= document.getLength()
            && selected.equals(document.getText(start, end - start)), "HYPERLINK_SELECTION_CHANGED");
        editor.select(start, end);
        require(editor.getSelectionStart() == start && editor.getSelectionEnd() == end,
            "HYPERLINK_SELECTION_CHANGED");
        editor.c(url);
        for (int offset = start; offset < end;) {
            Element run = document.getCharacterElement(offset);
            require(url.equals(run.getAttributes().getAttribute(V.u)) && run.getEndOffset() > offset,
                "HYPERLINK_UNVERIFIED");
            offset = run.getEndOffset();
        }
    }

    static void editRegion(fi pane, int regionMode, int start)
        throws ReflectiveOperationException, EditorLauncher.UiFailure {
        // The combined callback selects the first character of the chosen page region.
        pane.c(regionMode);
        pane.setCaretPosition(start);
        require(editMode((DocumentEx) pane.getDocument()) == regionMode, "PAGE_REGION_MODE_UNVERIFIED");
        require(pane.getCaretPosition() == start, "PAGE_REGION_CARET_UNVERIFIED");
    }

    private static int editMode(DocumentEx document) throws ReflectiveOperationException {
        return (Integer) nativeMethod(DocumentEx.class, "f", int.class).invoke(document);
    }

    static boolean validLink(String value) throws ReflectiveOperationException {
        return nativeMethod(tr.com.havelsan.uyap.system.editor.utils.p.class, "a", URI.class, String.class)
            .invoke(null, value) != null;
    }

    private static Method nativeMethod(Class<?> owner, String name, Class<?> result, Class<?>... parameters)
        throws NoSuchMethodException {
        for (Method method : owner.getMethods()) {
            if (method.getName().equals(name) && method.getReturnType() == result
                && java.util.Arrays.equals(method.getParameterTypes(), parameters)) {
                return method;
            }
        }
        throw new NoSuchMethodException(owner.getName() + "." + name);
    }

    private static void require(boolean condition, String failure) throws EditorLauncher.UiFailure {
        if (!condition) {
            throw new EditorLauncher.UiFailure(failure);
        }
    }
}
