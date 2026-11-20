import java.awt.event.ActionEvent;
import java.awt.event.KeyListener;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.util.Properties;
import javax.swing.Action;
import javax.swing.JCheckBoxMenuItem;
import javax.swing.text.AttributeSet;
import javax.swing.text.Element;
import javax.swing.text.JTextComponent;
import tr.com.havelsan.uyap.system.editor.common.text.DocumentEx;
import tr.com.havelsan.uyap.system.editor.common.text.fY;
import tr.com.havelsan.uyap.system.editor.common.text.hN;
import tr.com.havelsan.uyap.system.pki.b.l;
import tr.com.havelsan.uyap.system.swing.wp.model.Y;

final class AppearanceActions {
    private AppearanceActions() {}

    static boolean isCapitalization(String id) {
        return "first Letter Upper".equals(id) || "To-Uppercase".equals(id);
    }

    static void toggleCapitalization(JTextComponent editor, Action action, String id)
        throws ReflectiveOperationException, EditorLauncher.UiFailure {
        boolean firstLetter = "first Letter Upper".equals(id);
        String label = firstLetter ? "Baş Harfler Büyük" : "Otomatik Büyük Harf";
        Class<?> ribbon = Class.forName("tr.gov.uyap.system.a.b.a.a.z");
        JCheckBoxMenuItem check = null;
        for (Field field : ribbon.getFields()) {
            if (Modifier.isStatic(field.getModifiers()) && field.getType() == JCheckBoxMenuItem.class) {
                JCheckBoxMenuItem candidate = (JCheckBoxMenuItem) field.get(null);
                if (candidate != null && label.equals(candidate.getText())) {
                    check = candidate;
                    break;
                }
            }
        }
        if (check == null || !check.isEnabled()) {
            throw new EditorLauncher.UiFailure("CAPITALIZATION_CONTROL_UNAVAILABLE");
        }

        // Mirror the checkbox event, using the managed editor as its action context.
        boolean enabled = !check.isSelected();
        check.setSelected(enabled);
        action.actionPerformed(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, check.getActionCommand()));
        l preferences = preferences();
        Properties values = firstLetter ? preferences.n() : preferences.m();
        String key = firstLetter ? "FirstLetterUpperCase" : "ToUpperCase";
        if (!Boolean.toString(enabled).equals(values.getProperty(key))) {
            check.setSelected(!enabled);
            throw new EditorLauncher.UiFailure("CAPITALIZATION_SETTING_UNVERIFIED");
        }
    }

    private static l preferences() throws ReflectiveOperationException {
        for (Method method : l.class.getMethods()) {
            if ("a".equals(method.getName()) && method.getParameterCount() == 0
                && method.getReturnType() == l.class && Modifier.isStatic(method.getModifiers())) {
                return (l) method.invoke(null);
            }
        }
        throw new NoSuchMethodException("UYAP preferences singleton");
    }

    static String[] capitalizationState(JTextComponent editor) throws ReflectiveOperationException {
        l preferences = preferences();
        boolean firstActive = false;
        boolean uppercaseActive = false;
        for (KeyListener listener : editor.getKeyListeners()) {
            firstActive |= listener instanceof fY;
            uppercaseActive |= listener instanceof hN;
        }
        return new String[] {
            Boolean.toString(Boolean.parseBoolean(preferences.n().getProperty("FirstLetterUpperCase"))),
            Boolean.toString(firstActive),
            Boolean.toString(Boolean.parseBoolean(preferences.m().getProperty("ToUpperCase"))),
            Boolean.toString(uppercaseActive),
        };
    }

    static String[] pageRange(JTextComponent editor, Action startAction, Action stopAction,
        String region, String first, String last) throws EditorLauncher.UiFailure {
        if (!"header".equals(region) && !"footer".equals(region)) {
            throw new EditorLauncher.UiFailure("PAGE_REGION_INVALID");
        }
        if (!(editor.getDocument() instanceof DocumentEx)) {
            throw new EditorLauncher.UiFailure("PAGE_REGION_UNAVAILABLE");
        }
        if ((startAction != null || stopAction != null) && !editor.isEditable()) {
            throw new EditorLauncher.UiFailure("EDITOR_READ_ONLY");
        }
        AttributeSet before = regionAttributes(editor, region);
        int previousFirst = firstPage(before);
        int previousLast = Y.k(before);
        int firstPage = first.isEmpty() ? previousFirst : Integer.parseInt(first);
        int lastPage = last.isEmpty() ? previousLast : Integer.parseInt(last);
        if (firstPage < 1 || lastPage != -1 && lastPage < firstPage) {
            throw new EditorLauncher.UiFailure("PAGE_REGION_RANGE_INVALID");
        }
        String prefix = "header".equals(region) ? "h" : "f";
        changeRange(editor, startAction, prefix, firstPage);
        changeRange(editor, stopAction, prefix, lastPage);

        AttributeSet after = regionAttributes(editor, region);
        if (firstPage(after) != firstPage || Y.k(after) != lastPage) {
            throw new EditorLauncher.UiFailure("PAGE_REGION_RANGE_UNVERIFIED");
        }
        return new String[] { region, Integer.toString(firstPage), lastPage == -1 ? "" : Integer.toString(lastPage) };
    }

    private static AttributeSet regionAttributes(JTextComponent editor, String region) throws EditorLauncher.UiFailure {
        Element root = editor.getDocument().getDefaultRootElement();
        for (int i = 0; i < root.getElementCount(); i++) {
            Element element = root.getElement(i);
            if (region.equals(element.getName())) {
                return element.getAttributes();
            }
        }
        throw new EditorLauncher.UiFailure("PAGE_REGION_MISSING");
    }

    private static int firstPage(AttributeSet attributes) {
        return Math.max(1, Y.j(attributes));
    }

    private static void changeRange(JTextComponent editor, Action action, String prefix, int page) {
        if (action != null) {
            action.actionPerformed(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, prefix + page));
        }
    }
}
