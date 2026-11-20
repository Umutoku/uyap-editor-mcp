import java.awt.Dialog;
import java.awt.Frame;
import javax.swing.JTree;
import javax.swing.text.Element;
import javax.swing.tree.TreeModel;
import tr.com.havelsan.uyap.system.editor.common.gui.dn;

final class InterfaceActions {
    private static final int TREE_NODES = 100;
    private static final int TREE_DEPTH = 12;

    private InterfaceActions() { }

    static boolean isDiagnosticForm(String action) {
        return "release-notes-action-str".equals(action) || "show-elementTreePanel".equals(action);
    }

    static boolean matchesForm(String action, Dialog candidate, Frame frame) {
        if ("release-notes-action-str".equals(action)) {
            return "Son Sürümlerde Yapılan Güncellemeler:".equals(candidate.getTitle());
        }
        return "show-elementTreePanel".equals(action) && candidate instanceof dn
            && candidate.getOwner() == frame && "E.T.P.".equals(candidate.getTitle());
    }

    static boolean activateWindow(Frame frame) {
        if (!frame.isShowing()) {
            return false;
        }
        int state = frame.getExtendedState();
        if ((state & Frame.ICONIFIED) != 0) {
            frame.setExtendedState(state & ~Frame.ICONIFIED);
        }
        // Each managed editor has its own JVM. Bring its existing frame forward without
        // re-running the native frame's show routine, which also refreshes its document layout.
        frame.toFront();
        frame.requestFocus();
        return true;
    }

    static String treeText(JTree tree) {
        TreeModel model = tree.getModel();
        StringBuilder value = new StringBuilder();
        appendTree(value, model, model.getRoot(), 0, TREE_NODES);
        return value.substring(0, Math.min(8192, value.length()));
    }

    private static int appendTree(StringBuilder value, TreeModel model, Object node, int depth, int remaining) {
        if (node == null || remaining == 0) {
            return remaining;
        }
        for (int index = 0; index < depth; index++) {
            value.append("  ");
        }
        if (node instanceof Element) {
            Element element = (Element) node;
            value.append(element.getName()).append(' ').append(element.getStartOffset())
                .append('-').append(element.getEndOffset());
        } else {
            value.append(node);
        }
        value.append('\n');
        remaining--;
        if (depth == TREE_DEPTH) {
            return remaining;
        }
        for (int index = 0; remaining > 0 && index < model.getChildCount(node); index++) {
            remaining = appendTree(value, model, model.getChild(node, index), depth + 1, remaining);
        }
        return remaining;
    }
}
