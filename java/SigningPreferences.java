import java.awt.event.ActionEvent;
import java.lang.reflect.Method;
import java.lang.reflect.Modifier;
import java.util.Map;
import javax.swing.Action;
import org.pushingpixels.flamingo.api.common.JCommandToggleButton;
import tr.com.havelsan.uyap.system.editor.common.Utils;
import tr.com.havelsan.uyap.system.editor.common.an;
import tr.com.havelsan.uyap.system.editor.common.text.V;
import tr.com.havelsan.uyap.system.pki.b.l;

final class SigningPreferences {
    private static final String APDU_KEY = "APDU_ACTIVE";

    private SigningPreferences() { }

    static boolean toggleApdu(JCommandToggleButton button, Action action) throws Exception {
        if (!(action instanceof V) || !button.getActionModel().isEnabled()) {
            throw new EditorLauncher.UiFailure("APDU_CONTROL_UNAVAILABLE");
        }
        l preferences = nativePreferences();
        String before = preferences.g().getProperty(APDU_KEY);
        boolean wasSelected = button.getActionModel().isSelected();
        if (!Boolean.toString(wasSelected).equals(before)) {
            throw new EditorLauncher.UiFailure("APDU_STATE_UNVERIFIED");
        }
        boolean enabled = !wasSelected;
        button.getActionModel().setSelected(enabled);
        // The stock action expects a Flamingo toggle as its event source. Its
        // only effect is writing the setting inside this JVM's managed profile.
        ((V) action).a(new ActionEvent(button, ActionEvent.ACTION_PERFORMED, "apdu-action"));
        String chosen = Boolean.toString(enabled);
        Map<?, ?> persisted = an.a(Utils.k(), "initValues");
        if (persisted == null || !chosen.equals(persisted.get(APDU_KEY))) {
            button.getActionModel().setSelected(wasSelected);
            throw new EditorLauncher.UiFailure("APDU_STATE_UNVERIFIED");
        }
        // 5.4.20 persists APDU_ACTIVE but its preference write-through omits
        // the APDU cache. Synchronize this managed instance after native readback.
        preferences.g().setProperty(APDU_KEY, chosen);
        return enabled;
    }

    private static l nativePreferences() throws ReflectiveOperationException {
        for (Method method : l.class.getMethods()) {
            if ("a".equals(method.getName()) && method.getParameterCount() == 0
                && method.getReturnType() == l.class && Modifier.isStatic(method.getModifiers())) {
                return (l) method.invoke(null);
            }
        }
        throw new NoSuchMethodException("UYAP signing preferences");
    }
}
