import java.awt.Component;
import java.awt.Container;
import java.awt.Dialog;
import java.awt.GridBagConstraints;
import java.awt.GridBagLayout;
import java.awt.event.ActionEvent;
import java.awt.event.MouseEvent;
import java.awt.event.MouseListener;
import java.awt.event.MouseMotionListener;
import javax.swing.Action;
import javax.swing.JPanel;
import javax.swing.text.Element;
import javax.swing.text.JTextComponent;
import tr.com.havelsan.uyap.system.editor.common.text.DocumentEx;
import tr.com.havelsan.uyap.system.editor.common.text.aB;
import tr.com.havelsan.uyap.system.editor.common.text.aN;

final class TableActions {
    private static final String GRID_MOTION = "tr.com.havelsan.uyap.system.editor.common.text.ci";
    private static final String GRID_PRESS = "tr.com.havelsan.uyap.system.editor.common.text.cj";

    static void combine(JTextComponent editor) throws EditorLauncher.UiFailure {
        DocumentEx document = tableDocument(editor);
        int start = editor.getSelectionStart();
        int end = editor.getSelectionEnd();
        require(start < end, "TABLE_CELL_SELECTION_REQUIRED");
        Element first = document.getCharacterElement(start);
        Element last = document.getCharacterElement(end - 1);
        Element table = ancestor(first, "table");
        require(table != null && table == ancestor(last, "table")
            && ancestor(first, "cell") != ancestor(last, "cell"), "TABLE_CELL_SELECTION_REQUIRED");
        Element nativeEnd = document.getCharacterElement(end);
        require(ancestor(last, "cell") == ancestor(nativeEnd, "cell"), "TABLE_CELL_SELECTION_REQUIRED");
        Element firstRow = ancestor(first, "row");
        Element lastRow = ancestor(last, "row");
        if (firstRow != lastRow) {
            // UYAP merges whole rows for a multirow selection.
            require(firstRow != null && lastRow != null
                && ancestor(first, "cell") == firstRow.getElement(0)
                && ancestor(last, "cell") == lastRow.getElement(lastRow.getElementCount() - 1),
                "TABLE_WHOLE_ROWS_REQUIRED");
        }
        int before = cellCount(table);
        // The ribbon's enabled flag depends on physical table selection. Invoke the same
        // native callback after checking the cell range supplied by the MCP client.
        for (Action action : editor.getActions()) {
            if (action instanceof aB && "combine-table-cell".equals(action.getValue(Action.NAME))) {
                ((aB) action).a(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, "combine-table-cell"));
                Element merged = ancestor(document.getCharacterElement(start), "table");
                require(merged != null && cellCount(merged) < before, "TABLE_MERGE_UNVERIFIED");
                return;
            }
        }
        throw new EditorLauncher.UiFailure("EDITOR_ACTION_UNAVAILABLE");
    }

    static void divide(JTextComponent editor) throws EditorLauncher.UiFailure {
        DocumentEx document = tableDocument(editor);
        Element leaf = document.getCharacterElement(editor.getCaretPosition());
        require(ancestor(leaf, "cell") != null && ancestor(leaf, "table") != null, "TABLE_CELL_SELECTION_REQUIRED");
        for (Action action : editor.getActions()) {
            if (action instanceof aN && "divide-table-cell".equals(action.getValue(Action.NAME))) {
                ((aN) action).a(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, "divide-table-cell"));
                return;
            }
        }
        throw new EditorLauncher.UiFailure("EDITOR_ACTION_UNAVAILABLE");
    }

    static boolean isGridChoice(Component component) {
        if (!(component instanceof JPanel) || component.getParent() == null
            || !(component.getParent().getLayout() instanceof GridBagLayout)) {
            return false;
        }
        for (MouseMotionListener listener : component.getMouseMotionListeners()) {
            if (GRID_MOTION.equals(listener.getClass().getName())) {
                return true;
            }
        }
        return false;
    }

    static boolean isQuickTableDialog(Dialog dialog) {
        return !dialog.isModal() && hasGridChoice(dialog);
    }

    static String gridChoiceLabel(Component component) {
        GridBagLayout layout = (GridBagLayout) component.getParent().getLayout();
        GridBagConstraints position = layout.getConstraints(component);
        return (position.gridy + 1) + "x" + (position.gridx + 1);
    }

    static void pressGridChoice(Component component) throws EditorLauncher.UiFailure {
        MouseMotionListener move = null;
        MouseListener press = null;
        for (MouseMotionListener listener : component.getMouseMotionListeners()) {
            if (GRID_MOTION.equals(listener.getClass().getName())) {
                move = listener;
            }
        }
        for (MouseListener listener : component.getMouseListeners()) {
            if (GRID_PRESS.equals(listener.getClass().getName())) {
                press = listener;
            }
        }
        require(move != null && press != null, "TABLE_GRID_UNAVAILABLE");
        int x = component.getWidth() / 2;
        int y = component.getHeight() / 2;
        long now = System.currentTimeMillis();
        move.mouseMoved(new MouseEvent(component, MouseEvent.MOUSE_MOVED, now, 0, x, y, 0, false));
        press.mouseReleased(new MouseEvent(component, MouseEvent.MOUSE_RELEASED, now, 0, x, y, 1, false,
            MouseEvent.BUTTON1));
    }

    private static boolean hasGridChoice(Container parent) {
        for (Component child : parent.getComponents()) {
            if (isGridChoice(child) || child instanceof Container && hasGridChoice((Container) child)) {
                return true;
            }
        }
        return false;
    }

    private static DocumentEx tableDocument(JTextComponent editor) throws EditorLauncher.UiFailure {
        require(editor.isEditable() && editor.getDocument() instanceof DocumentEx, "EDITOR_READ_ONLY");
        return (DocumentEx) editor.getDocument();
    }

    private static Element ancestor(Element element, String type) {
        for (Element current = element; current != null; current = current.getParentElement()) {
            if (type.equals(current.getName())) {
                return current;
            }
        }
        return null;
    }

    private static int cellCount(Element table) {
        int count = 0;
        for (int index = 0; index < table.getElementCount(); index++) {
            Element row = table.getElement(index);
            if ("row".equals(row.getName())) {
                count += row.getElementCount();
            }
        }
        return count;
    }

    private static void require(boolean condition, String code) throws EditorLauncher.UiFailure {
        if (!condition) {
            throw new EditorLauncher.UiFailure(code);
        }
    }
}
