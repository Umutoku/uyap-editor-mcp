import java.awt.Component;
import java.awt.Container;
import java.awt.Dialog;
import java.awt.EventQueue;
import java.awt.Frame;
import java.awt.Window;
import java.awt.datatransfer.Clipboard;
import java.awt.datatransfer.Transferable;
import java.awt.event.ActionEvent;
import java.awt.event.MouseEvent;
import java.awt.event.WindowEvent;
import java.io.BufferedReader;
import java.io.File;
import java.io.FileDescriptor;
import java.io.FileOutputStream;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.PrintStream;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.StandardOpenOption;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Base64;
import java.util.Collections;
import java.util.HashSet;
import java.util.LinkedHashMap;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.concurrent.Callable;
import java.util.concurrent.FutureTask;
import java.util.concurrent.atomic.AtomicLong;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import javax.swing.AbstractButton;
import javax.swing.Action;
import javax.swing.JComboBox;
import javax.swing.JEditorPane;
import javax.swing.JFileChooser;
import javax.swing.JLabel;
import javax.swing.JMenuItem;
import javax.swing.JPasswordField;
import javax.swing.JTextField;
import javax.swing.JToggleButton;
import javax.swing.MenuElement;
import javax.swing.MenuSelectionManager;
import javax.swing.TransferHandler;
import javax.swing.filechooser.FileFilter;
import javax.swing.event.DocumentEvent;
import javax.swing.event.DocumentListener;
import javax.swing.text.AbstractDocument;
import javax.swing.text.AttributeSet;
import javax.swing.text.Document;
import javax.swing.text.Element;
import javax.swing.text.SimpleAttributeSet;
import javax.swing.text.StyleConstants;
import javax.swing.text.StyledDocument;
import javax.swing.text.JTextComponent;
import net.zemberek.erisim.Zemberek;
import org.pushingpixels.flamingo.api.common.AbstractCommandButton;
import org.pushingpixels.flamingo.api.common.JCommandButton;
import org.pushingpixels.flamingo.api.common.JCommandToggleButton;
import org.pushingpixels.flamingo.api.common.popup.JCommandPopupMenu;
import org.pushingpixels.flamingo.api.common.popup.JPopupPanel;
import org.pushingpixels.flamingo.api.ribbon.AbstractRibbonBand;
import org.pushingpixels.flamingo.api.ribbon.JRibbon;
import org.pushingpixels.flamingo.api.ribbon.RibbonApplicationMenu;
import org.pushingpixels.flamingo.api.ribbon.RibbonApplicationMenuEntryFooter;
import org.pushingpixels.flamingo.api.ribbon.RibbonApplicationMenuEntryPrimary;
import org.pushingpixels.flamingo.api.ribbon.RibbonApplicationMenuEntrySecondary;
import org.pushingpixels.flamingo.api.ribbon.RibbonContextualTaskGroup;
import org.pushingpixels.flamingo.api.ribbon.RibbonTask;
import tr.com.havelsan.uyap.system.editor.common.Utils;
import tr.com.havelsan.uyap.system.editor.common.text.im;
import tr.com.havelsan.uyap.system.editor.common.gui.hP;
import tr.com.havelsan.uyap.system.editor.common.gui.jV;
import tr.com.havelsan.uyap.system.editor.common.text.DocumentEx;
import tr.com.havelsan.uyap.system.editor.common.text.fi;
import tr.com.havelsan.uyap.system.editor.common.text.l;
import tr.com.havelsan.uyap.system.editor.utils.k;
import tr.com.havelsan.uyap.system.swing.wp.model.ad;

/** Swing operations in the JVM explicitly launched and owned by the stdio MCP server. */
public final class EditorLauncher {
    private static final String ENTRY_CLASS = "tr.com.havelsan.uyap.system.editor.common.WPAppManager";
    private static final String TEMPLATE_MODE = "EDITOR_TYPE_TEMPLATE";
    private static final String VERSION = "5.4.20";
    private static final String DEFAULT_LIST_NUMBER_FORMAT = "NUMBER_TYPE_NUMBER_DOT";
    private static final Pattern LIST_LEVEL_FORMAT = Pattern.compile(
        "NUMBER_TYPE_(?:(?:NUMBER|CHAR_SMALL|CHAR_BIG|ROMAN_SMALL|ROMAN_BIG)"
        + "_(?:DOT|TRE|PARANTHESE|D_PARANTHESE)|NUMBER_DOT_3)"
        + "|BULLET_TYPE_(?:ELLIPSE|RECTANGLE|RECTANGLE_D|ARROW|DIAMOND|TRIANGLE)");
    private static final Pattern PAGE = Pattern.compile("^Sayfa\\s*:\\s*(\\d+)\\s*/\\s*(\\d+)\\s*$");
    private static final int UI_TIMEOUT = 12000;
    private static Frame frame;
    private static Document initialEditModel;
    private static boolean editSuspended;
    private static boolean templateMode;
    private static volatile boolean dirty;
    private static boolean changedSinceOpen;
    private static Dialog localDialog;
    private static String localFormAction;
    private static Dialog dialogControlsWindow;
    private static List<Component> dialogControls;
    private static long dialogRevision;
    private static final Clipboard sessionClipboard = new Clipboard("UYAP managed document");
    private static final Set<String> FORM_ACTIONS = new HashSet<>(Arrays.asList(
        "Set Sign Libary",
        "font-action", "paragraph-action", "page-setup", "prepareTable", "table-insert", "divide-table-cell",
        "table-properties", "table-properties-new", "table-row-properties", "table-col-properties",
        "table-height", "table-row-heights", "edit-table-border", "ini-designer",
        "insert-header", "insert-footer", "edit-header", "edit-footer",
        "insert-pageNumber", "edit-pageNumber", "insert symbol", "insert-barcode",
        "insert-url-link", "control_image_size", "changeListNo-action", "edit-colors",
        "comment-add-type1", "comment-add-type2", "show-all-annotation", "selection-word-info",
        "info-selection", "all-shortcuts", "show-signatures", "print-preview",
        "remove-table", "remove-tablerow", "remove-tablecolumn", "remove-tablecell",
        "accept-changes", "accept-all-changes", "reject-changes", "reject-all-changes",
        "comment-delete", "comment-delete-all", "auto-Text", "short-cut", "find", "replace",
        "template-convert", "template-unconvert", "add-field", "add-field-group", "add-paragraph-field-group",
        "field-erasable", "template-color", "release-notes-action-str", "show-elementTreePanel",
        "first Letter Upper", "To-Uppercase", "logger-activator"));
    private static final Set<String> TEMPLATE_ACTIONS = new HashSet<>(Arrays.asList(
        "template-convert", "template-unconvert", "add-field", "add-field-group", "add-paragraph-field-group",
        "field-erasable", "template-color", "template-elementMarker-switch", "table-erasable",
        "remove-field-group", "remove-paragraph-field-group", "delete-entryfield-group"));
    private static final Set<String> FORM_BUTTONS = new HashSet<>(Arrays.asList(
        "Tamam", "Uygula", "Ekle", "Kapat", "Kaydet", "İptal", "Vazgeç", "Seç", "OK",
        "Apply", "Cancel", "Close", "Insert", "Add"));

    private interface Selector<T> {
        boolean matches(T item);
    }

    private static final class ListFormatKeys {
        // Initialize the native document attributes after the editor has started.
        private static final Object[] BY_LEVEL = { ad.q, ad.r, ad.s, ad.t, ad.u, ad.v };
    }

    public static void main(String[] args) throws Exception {
        if (args.length < 1 || args.length > 2) {
            throw new IllegalArgumentException("Expected one managed UDF path");
        }

        PrintStream wire = new PrintStream(new FileOutputStream(FileDescriptor.out), true, "UTF-8");
        System.setOut(System.err);
        String launch = args.length == 2 ? args[1] : "document";
        templateMode = "template".equals(launch) || "template-new".equals(launch);
        boolean create = "new".equals(launch) || "template-new".equals(launch);
        String[] editorArgs;
        if (templateMode) {
            editorArgs = create ? new String[] { "getNewWPInstance", TEMPLATE_MODE }
                : new String[] { "getNewWPInstance", TEMPLATE_MODE, args[0] };
        } else {
            editorArgs = create ? new String[0] : new String[] { "getNewWPInstance", args[0] };
        }
        Class.forName(ENTRY_CLASS).getMethod("main", String[].class).invoke(null, (Object) editorArgs);

        BufferedReader stdin = new BufferedReader(new InputStreamReader(System.in, StandardCharsets.UTF_8));
        for (String line; (line = stdin.readLine()) != null; ) {
            String[] request = line.split("\t", -1);
            try {
                String[] fields = new String[Math.max(0, request.length - 2)];
                for (int i = 0; i < fields.length; i++) {
                    fields[i] = new String(Base64.getDecoder().decode(request[i + 2]), StandardCharsets.UTF_8);
                }
                String[] response = dispatch(request[1], fields, args[0]);
                write(wire, request[0], "OK", response);
                if ("close".equals(request[1])) {
                    return;
                }
            } catch (Exception ex) {
                Throwable cause = ex.getCause() == null ? ex : ex.getCause();
                if (Boolean.getBoolean("uyap.mcp.diagnostics") && !(cause instanceof UiFailure)) {
                    cause.printStackTrace(System.err);
                }
                String code = cause instanceof UiFailure ? cause.getMessage() : "EDITOR_UI_ERROR";
                write(wire, request[0], "ERR", new String[] { code });
            }
        }
        if (frame != null && !dirty) {
            try {
                close();
            } catch (Exception ignored) {
                // Leave the managed editor available when a user action is pending.
            }
        }
    }

    private static void write(PrintStream wire, String id, String status, String[] values) {
        StringBuilder result = new StringBuilder(id).append('\t').append(status);
        for (String value : values) {
            result.append('\t').append(Base64.getEncoder().encodeToString(value.getBytes(StandardCharsets.UTF_8)));
        }
        wire.println(result.toString());
    }

    private static String[] dispatch(String command, String[] args, String path) throws Exception {
        if ("ready".equals(command)) {
            return ready(path);
        }
        ensure(edt(() -> frame != null && frame.isDisplayable()), "EDITOR_WINDOW_CLOSED");
        switch (command) {
            case "page":
                return edt(EditorLauncher::page);
            case "view_state":
                return edt(EditorLauncher::viewState);
            case "edit_mode":
                return editMode(args[0]);
            case "capitalization_state":
                return edt(() -> AppearanceActions.capitalizationState(body()));
            case "set_view":
                return setView(args[0], args[1]);
            case "set_font":
                return setFont(args[0], args[1]);
            case "set_palette_color":
                return setPaletteColor(args[0], args[1]);
            case "set_line_spacing":
                return setLineSpacing(args[0]);
            case "format_brush":
                return applyFormatBrush(Integer.parseInt(args[0]), Integer.parseInt(args[1]), Integer.parseInt(args[2]),
                    Boolean.parseBoolean(args[3]));
            case "set_list_palette":
                return setListPalette(args[0]);
            case "set_custom_list":
                return setCustomList(args);
            case "set_list_indents":
                return setListIndents(args);
            case "get_list_item":
                return edt(() -> {
                    ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
                    JTextComponent pane = body();
                    return listItemValues(pane, listParagraph(pane));
                });
            case "set_list_level":
                return setListLevel(Integer.parseInt(args[0]));
            case "set_numbering_start":
                return setNumberingStart(Integer.parseInt(args[0]));
            case "continue_numbering":
                return continueNumbering();
            case "shortcuts":
                return edt(() -> shortcutList(Integer.parseInt(args[0]), Integer.parseInt(args[1])));
            case "go":
                return go(Integer.parseInt(args[0]));
            case "pdf":
                return new String[] { exportPdf(args[0]) };
            case "signed_imz":
                return new String[] { exportSignedImz(args[0]) };
            case "save_udf":
                return new String[] { export(args[0], "Farklı Kaydet", "udf", true) };
            case "restore_backup":
                return restoreNativeBackup(args[0]);
            case "mark_saved":
                dirty = false;
                return new String[] { "saved" };
            case "export":
                return new String[] { export(args[0], "Farklı Kaydet", args[1], true) };
            case "render_export":
                return renderExport(args[0], args[1]);
            case "tabs":
                return edt(EditorLauncher::tabs);
            case "actions":
                return edt(EditorLauncher::actionNames);
            case "catalog":
                return edt(() -> catalog(args[0]));
            case "commands":
                selectTab(args[0]);
                return edt(EditorLauncher::commands);
            case "formats":
                return formats();
            case "import_rtf":
                return importRtf(args[0]);
            case "read_text":
                return text(Integer.parseInt(args[0]), Integer.parseInt(args[1]));
            case "page_regions":
                return edt(() -> pageRegions(body().getDocument()));
            case "set_page_region":
                return setPageRegion(args[0], args[1]);
            case "page_region_range":
                return edt(() -> {
                    ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                        "USER_ACTION_REQUIRED");
                    return AppearanceActions.pageRange(body(),
                        args[1].isEmpty() ? null : editorAction("hf-startPage"),
                        args[2].isEmpty() ? null : editorAction("hf-stopPage"), args[0], args[1], args[2]);
                });
            case "select_text":
                return selectText(Integer.parseInt(args[0]), Integer.parseInt(args[1]));
            case "compare_udf":
                return compareUdf(args[0], args[1], args[2], Integer.parseInt(args[3]), args.length > 4 ? args[4] : "",
                    args.length > 5 ? Integer.parseInt(args[5]) : 0, args.length > 6 ? args[6] : "",
                    args.length > 7 ? args[7] : "");
            case "snapshot_udf":
                return edt(() -> {
                    ensure(dialog(title -> true) == null && searchWindow() == null, "USER_ACTION_REQUIRED");
                    writeNativeUdf(args[0]);
                    return new String[] { "saved" };
                });
            case "pdf_source_changed":
                return edt(() -> new String[] { Boolean.toString(changedSinceOpen) });
            case "find":
                return find(args[0], Integer.parseInt(args[1]), Boolean.parseBoolean(args[2]));
            case "spell_check":
                return spellCheck(Integer.parseInt(args[0]), Integer.parseInt(args[1]), Integer.parseInt(args[2]));
            case "spell_apply":
                return applySpelling(Integer.parseInt(args[0]), Integer.parseInt(args[1]), args[2], args[3]);
            case "replace":
                return replace(Integer.parseInt(args[0]), Integer.parseInt(args[1]), args[2]);
            case "template_reuse":
                return reuseTemplateField(args[0]);
            case "list_comments":
                return listComments(Integer.parseInt(args[0]), Integer.parseInt(args[1]));
            case "list_automatic_text":
                return edt(() -> {
                    ensure(dialog(title -> true) == null && !templateMode, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
                    return AutomaticText.list();
                });
            case "set_automatic_text":
                return edt(() -> {
                    ensure(dialog(title -> true) == null && !templateMode, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
                    return AutomaticText.set(body(), args[0], args[1], args[2], args[3]);
                });
            case "insert_automatic_text":
                return edt(() -> {
                    ensure(dialog(title -> true) == null && !templateMode, "AUTO_TEXT_CONTEXT_UNAVAILABLE");
                    return AutomaticText.insert(body(), args[0], args[1]);
                });
            case "add_comment":
                return addComment(Integer.parseInt(args[0]), Integer.parseInt(args[1]), args[2], Integer.parseInt(args[3]));
            case "edit_comment":
                return editComment(Integer.parseInt(args[0]), Integer.parseInt(args[1]), Integer.parseInt(args[2]),
                    args[3], args[4]);
            case "change_comment":
                return changeComment(Integer.parseInt(args[0]), Integer.parseInt(args[1]), Integer.parseInt(args[2]),
                    Integer.parseInt(args[3]), args[4], args[5]);
            case "image":
                return insertImage(args[0], args[1]);
            case "action":
                return simpleAction(args[0], args.length > 1 ? args[1] : "");
            case "macro":
                return macro(args);
            case "open_form":
                return openForm(args[0]);
            case "inspect_form":
                return edt(EditorLauncher::formState);
            case "form_input":
                return formInput(args[0], Integer.parseInt(args[1]), args[2], args[3]);
            case "close":
                return close();
            default:
                throw new UiFailure("UNKNOWN_COMMAND");
        }
    }

    private static String[] ready(String path) throws Exception {
        String documentPath = "(" + path + ")";
        frame = waitFor(() -> edt(() -> {
            for (Frame candidate : Frame.getFrames()) {
                String mode = templateMode ? "Şablon Editörü" : "Doküman Editörü";
                if (candidate.isShowing() && candidate.getTitle().trim().startsWith(mode + " v" + VERSION + " -")
                    && (candidate.getTitle().endsWith(documentPath)
                        || isSignedWindow(candidate, documentPath))) {
                    return candidate;
                }
            }
            return null;
        }), "EDITOR_WINDOW_TIMEOUT");
        // The startup notes can be owned by Swing's shared owner rather than the editor frame.
        Thread.sleep(300);
        boolean signed = isSignedWindow(frame, documentPath);
        boolean[] warning = {false};
        // Signature validation runs asynchronously after the signed title appears.
        long signatureNoticeUntil = System.nanoTime() + 2500000000L;
        waitFor(() -> edt(() -> {
            for (Window window : Window.getWindows()) {
                if (window instanceof Dialog && window.isShowing()
                    && ((Dialog) window).getTitle().contains("Yapılan Güncellemeler")) {
                    button((Dialog) window, "Kapat").doClick(0);
                    return null;
                }
            }
            if (signed) {
                if (readSignedOpeningNotice(warning) || dialog(title -> true) != null
                    || (!warning[0] && System.nanoTime() < signatureNoticeUntil)) {
                    return null;
                }
            }
            return nodes(frame, JTextComponent.class, EditorLauncher::isBody).size() == 1 ? Boolean.TRUE : null;
        }), "EDITOR_DOCUMENT_TIMEOUT");
        return edt(() -> {
            JTextComponent editor = body();
            initialEditModel = editor.getDocument();
            DocumentListener listener = new DocumentListener() {
                public void insertUpdate(DocumentEvent e) { noteDocumentChange(); }
                public void removeUpdate(DocumentEvent e) { noteDocumentChange(); }
                public void changedUpdate(DocumentEvent e) { noteDocumentChange(); }
            };
            editor.getDocument().addDocumentListener(listener);
            if (templateMode) {
                // Convert and unconvert replace the editor's native Swing document.
                editor.addPropertyChangeListener("document", change -> {
                    if (change.getOldValue() instanceof Document) {
                        ((Document) change.getOldValue()).removeDocumentListener(listener);
                    }
                    if (change.getNewValue() instanceof Document) {
                        ((Document) change.getNewValue()).addDocumentListener(listener);
                        noteDocumentChange();
                    }
                });
            }
            if (templateMode) {
                ensure(Arrays.asList(tabs()).contains("Şablon"), "TEMPLATE_MODE_NOT_VERIFIED");
                return new String[] { "template" };
            }
            String[] position = page();
            return signed ? new String[] { position[0], position[1],
                warning[0] ? "native_invalid_signature" : "present_unverified" } : position;
        });
    }

    private static boolean isSignedWindow(Frame candidate, String documentPath) {
        return !templateMode && candidate.getTitle().endsWith(documentPath + " (İmzalı doküman)");
    }

    // Keep the editor's verdict visible to the caller. Only acknowledge the specific
    // informational startup notice and close the signature details it opened.
    private static boolean readSignedOpeningNotice(boolean[] warning) throws UiFailure {
        Dialog invalid = dialog("Hata"::equals);
        if (invalid != null && !nodes(invalid, JTextComponent.class,
            text -> text.getText().contains("geçersiz imza tespit edildi")).isEmpty()) {
            warning[0] = true;
            AbstractButton acknowledge = button(invalid, "Tamam");
            EventQueue.invokeLater(() -> acknowledge.doClick(0));
            return true;
        }
        Dialog details = warning[0] ? dialog("İmzalar"::equals) : null;
        if (details != null) {
            button(details, "Kapat").doClick(0);
            return true;
        }
        return false;
    }

    private static String exportSignedImz(String path) throws Exception {
        edt(() -> {
            ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                "USER_ACTION_REQUIRED");
            return null;
        });
        return export(path, "Dışa Aktar", "imz", false);
    }

    private static String[] page() throws UiFailure {
        ensure(!templateMode, "PAGE_COUNTER_UNAVAILABLE_IN_TEMPLATE");
        JLabel counter = single(frame, JLabel.class, label -> PAGE.matcher(textOf(label)).matches());
        Matcher match = PAGE.matcher(textOf(counter));
        ensure(match.matches(), "PAGE_UNAVAILABLE");
        return new String[] { match.group(1), match.group(2) };
    }

    private static String[] go(int number) throws Exception {
        String[] before = edt(EditorLauncher::page);
        int total = Integer.parseInt(before[1]);
        ensure(number >= 1 && number <= total, "PAGE_OUT_OF_RANGE");
        if (number == Integer.parseInt(before[0])) {
            return before;
        }

        Action action = edt(() -> editorAction("go-pageNumber"));
        EventQueue.invokeLater(() -> fire(action, "go-pageNumber"));
        Dialog target = waitFor(() -> edt(() -> dialog("Sayfaya Git"::equals)), "PAGE_DIALOG_TIMEOUT");
        try {
            edt(() -> {
                JTextField input = single(target, JTextField.class, JTextField::isEditable);
                input.setText(Integer.toString(number));
                button(target, "Git").doClick(0);
                return null;
            });
            return waitFor(() -> edt(() -> {
                String[] current = page();
                return Integer.parseInt(current[0]) == number ? current : null;
            }), "PAGE_POSITION_UNVERIFIED");
        } finally {
            edt(() -> {
                target.dispatchEvent(new WindowEvent(target, WindowEvent.WINDOW_CLOSING));
                return null;
            });
        }
    }

    private static String exportPdf(String path) throws Exception {
        PresentationActions.PdfLiraFonts fonts = edt(() -> {
            ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                "USER_ACTION_REQUIRED");
            return PresentationActions.PdfLiraFonts.prepare(body());
        });
        try {
            String command = export(path, "PDF Olarak Kaydet", "pdf", false);
            if (fonts.changed()) {
                File output = new File(path);
                waitFor(() -> hasPdfTrailer(output) ? Boolean.TRUE : null, "PDF_LIRA_EXPORT_UNVERIFIED");
            }
            return command;
        } finally {
            edt(() -> {
                fonts.close();
                return null;
            });
        }
    }

    private static void noteDocumentChange() {
        changedSinceOpen = true;
        dirty = true;
    }

    private static boolean hasPdfTrailer(File output) {
        try (java.io.RandomAccessFile file = new java.io.RandomAccessFile(output, "r")) {
            int count = (int) Math.min(file.length(), 64);
            byte[] tail = new byte[count];
            file.seek(file.length() - count);
            file.readFully(tail);
            return new String(tail, StandardCharsets.US_ASCII).trim().endsWith("%%EOF");
        } catch (java.io.IOException pending) {
            return false;
        }
    }

    private static String export(String path, String label, String extension, boolean selectFormat) throws Exception {
        File file = new File(path);
        ensure(file.isAbsolute() && file.getName().endsWith("." + extension) && !file.exists(), "INVALID_STAGING_PATH");
        boolean doc = "doc".equals(extension);
        FutureTask<Void> opening;
        if (doc) {
            // Only save-doc passes the native event source that installs the Word filter.
            Action nativeSave = edt(() -> editorAction("save-doc"));
            opening = new FutureTask<>(() -> {
                fire(nativeSave, "save-doc");
                return null;
            });
        } else {
            selectTab("Dosya");
            AbstractCommandButton command = waitFor(() -> edt(() -> {
                List<AbstractCommandButton> buttons = nodes(frame, AbstractCommandButton.class,
                    button -> button.getText() != null && label.equals(button.getText().trim())
                        && button.getActionModel().isEnabled());
                ensure(buttons.size() <= 1, "AMBIGUOUS_RIBBON_COMMAND");
                return buttons.isEmpty() ? null : buttons.get(0);
            }), "RIBBON_COMMAND_UNAVAILABLE");
            opening = new FutureTask<>(() -> {
                command.doActionClick();
                return null;
            });
        }
        EventQueue.invokeLater(opening);
        String fallback = "imz".equals(extension) ? null : selectFormat ? "save-as" : "save-pdf";
        long fallbackAt = System.nanoTime() + 1500000000L;
        boolean[] retried = {false};
        JFileChooser chooser = waitFor(() -> edt(() -> {
            Dialog open = dialog(title -> true);
            if (open != null) {
                List<JFileChooser> matches = nodes(open, JFileChooser.class,
                    component -> component.getDialogType() == JFileChooser.SAVE_DIALOG);
                ensure(matches.size() <= 1, "AMBIGUOUS_SAVE_DIALOG");
                return matches.isEmpty() ? null : matches.get(0);
            }
            if (doc && opening.isDone()) {
                opening.get();
                throw new UiFailure("SAVE_DIALOG_TIMEOUT");
            }
            if (fallback != null && !doc && !retried[0] && opening.isDone() && System.nanoTime() >= fallbackAt) {
                opening.get();
                retried[0] = true;
                // The ribbon can return without opening a chooser during window activation.
                // Invoke the matching native action only while no dialog is open.
                Action nativeSave = editorAction(fallback);
                EventQueue.invokeLater(() -> fire(nativeSave, fallback));
            }
            return null;
        }), "SAVE_DIALOG_TIMEOUT");

        try {
            edt(() -> {
                if (selectFormat) {
                    FileFilter selected = null;
                    for (FileFilter filter : chooser.getChoosableFileFilters()) {
                        if (matchesExtension(filter, extension)) {
                            ensure(selected == null, "SAVE_FORMAT_AMBIGUOUS");
                            selected = filter;
                        }
                    }
                    ensure(selected != null, "SAVE_FORMAT_UNAVAILABLE");
                    chooser.setFileFilter(selected);
                }
                ensure(chooser.getFileFilter() != null && matchesExtension(chooser.getFileFilter(), extension),
                    "SAVE_FORMAT_MISMATCH");
                if (doc) {
                    Class<?> detector = Class.forName("tr.com.havelsan.uyap.system.editor.b.a.g");
                    File officeHome = (File) commentLookup(detector, "b", File.class).invoke(null);
                    ensure(officeHome != null && org.artofsolving.jodconverter.office.OfficeUtils
                        .getOfficeExecutable(officeHome).isFile(), "DOC_OFFICE_RUNTIME_UNAVAILABLE");
                }
                chooser.setSelectedFile(file);
                ensure(file.equals(chooser.getSelectedFile()), "SAVE_TARGET_MISMATCH");
                chooser.approveSelection();
                return null;
            });
            waitFor(() -> edt(() -> !chooser.isShowing() ? Boolean.TRUE : null), "SAVE_DIALOG_NOT_CLOSED");
            if ("txt".equals(extension)) {
                confirmPlainTextExport(file);
            }
            if (doc) {
                return "action:save-doc";
            }
            return retried[0] ? "action:" + fallback : "ribbon:Dosya/" + label;
        } catch (Exception error) {
            edt(() -> {
                if (chooser.isShowing()) {
                    chooser.cancelSelection();
                }
                return null;
            });
            throw error;
        }
    }

    private static boolean matchesExtension(FileFilter filter, String extension) {
        String text = filter.getDescription().toLowerCase(java.util.Locale.ROOT);
        return Pattern.compile("(?:\\.|\\b)" + Pattern.quote(extension) + "(?![a-z0-9])").matcher(text).find();
    }

    private static String[] restoreNativeBackup(String path) throws Exception {
        final String name = "MCP-Recovery.udf";
        File staged = new File(Utils.f(), "~" + name);
        File target = new File(path);
        ensure(!templateMode && staged.isFile() && target.isAbsolute() && target.getName().endsWith(".udf")
            && !target.exists(), "INVALID_STAGING_PATH");
        edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            File[] candidates = staged.getParentFile().listFiles((location, item) -> item.startsWith("~")
                && item.toLowerCase(java.util.Locale.ROOT).endsWith(".udf"));
            ensure(candidates != null && candidates.length == 1, "BACKUP_STAGE_AMBIGUOUS");
            // This editor owns one isolated recovery profile and exactly one staged native autosave.
            fire(editorAction("open-backups"), "open-backups");
            return null;
        });
        tr.com.havelsan.uyap.system.editor.common.gui.oK row = waitFor(() -> edt(() -> {
            List<tr.com.havelsan.uyap.system.editor.common.gui.oK> rows = nodes(frame,
                tr.com.havelsan.uyap.system.editor.common.gui.oK.class,
                button -> button.getText().contains(">" + name + "<"));
            ensure(rows.size() <= 1, "BACKUP_STAGE_AMBIGUOUS");
            return rows.isEmpty() ? null : rows.get(0);
        }), "BACKUP_PANEL_UNVERIFIED");
        FutureTask<Void> save = new FutureTask<>(() -> {
            row.dispatchEvent(new MouseEvent(row, MouseEvent.MOUSE_RELEASED, System.currentTimeMillis(),
                0, 2, 2, 1, false, MouseEvent.BUTTON1));
            tr.com.havelsan.uyap.system.editor.common.gui.oL popup = null;
            for (MenuElement item : MenuSelectionManager.defaultManager().getSelectedPath()) {
                if (item instanceof tr.com.havelsan.uyap.system.editor.common.gui.oL) {
                    popup = (tr.com.havelsan.uyap.system.editor.common.gui.oL) item;
                }
            }
            ensure(popup != null, "BACKUP_MENU_UNVERIFIED");
            JMenuItem saveItem = single(popup, JMenuItem.class, item -> "Kaydet".equals(item.getText()));
            MenuSelectionManager.defaultManager().clearSelectedPath();
            saveItem.doClick(0);
            return null;
        });
        EventQueue.invokeLater(save);
        JFileChooser chooser = waitFor(() -> edt(() -> {
            if (save.isDone()) {
                save.get();
            }
            Dialog open = dialog(title -> true);
            if (open == null) {
                return null;
            }
            return single(open, JFileChooser.class, item -> item.getDialogType() == JFileChooser.SAVE_DIALOG);
        }), "BACKUP_SAVE_DIALOG_UNVERIFIED");
        try {
            edt(() -> {
                // The native recovery handler appends .udf itself.
                File choice = new File(target.getParentFile(),
                    target.getName().substring(0, target.getName().length() - 4));
                chooser.setSelectedFile(choice);
                ensure(choice.equals(chooser.getSelectedFile()), "BACKUP_SAVE_PATH_MISMATCH");
                chooser.approveSelection();
                return null;
            });
            waitFor(() -> save.isDone() && target.isFile() && target.length() > 0 ? Boolean.TRUE : null,
                "BACKUP_OUTPUT_UNVERIFIED");
            save.get();
        } finally {
            edt(() -> {
                if (chooser.isShowing()) {
                    chooser.cancelSelection();
                }
                return null;
            });
        }
        return new String[] { "recovery:Yedekleri Aç/Kaydet" };
    }

    private static String[] renderExport(String path, String format) throws Exception {
        String mime;
        String extension = format;
        switch (format) {
            case "html-with-breaks":
                extension = "html";
                mime = "text/html+breaks";
                break;
            case "html": mime = "text/html"; break;
            case "rtf": mime = "application/rtf"; break;
            case "ps": mime = "application/postscript"; break;
            case "xsl": mime = "text/xsl"; break;
            case "svg": mime = "image/svg+xml"; break;
            default: throw new UiFailure("SAVE_FORMAT_UNAVAILABLE");
        }
        File target = new File(path);
        ensure(target.isAbsolute() && target.getName().endsWith("." + extension) && !target.exists(),
            "INVALID_STAGING_PATH");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            if ("svg".equals(format)) {
                java.io.ByteArrayOutputStream snapshot = new java.io.ByteArrayOutputStream();
                writeNativeUdf(snapshot);
                SvgExport.write(snapshot.toByteArray(), target.toPath());
                ensure(target.length() > 0, "OUTPUT_UNVERIFIED");
                return new String[] { "compat:uyap-printable/svg" };
            }
            // The native export actions all delegate to this document-context renderer.
            Object owner = commentLookup(tr.com.havelsan.uyap.system.editor.common.text.fi.class, "a",
                tr.com.havelsan.uyap.a.a.a.d.class).invoke(body());
            ensure(owner instanceof tr.com.havelsan.uyap.a.a.a.d, "EXPORT_CONTEXT_UNAVAILABLE");
            String limitName = "jdk.xml.xpathExprOpLimit";
            String previousLimit = System.getProperty(limitName);
            boolean html = mime.startsWith("text/html");
            try (OutputStream stream = Files.newOutputStream(target.toPath(), StandardOpenOption.CREATE_NEW,
                StandardOpenOption.WRITE)) {
                // The bundled HTML stylesheet exceeds the Java 8 update's default of 100
                // operators per expression. Increase only this bound during its native render.
                if (html) {
                    System.setProperty(limitName, "256");
                }
                ((tr.com.havelsan.uyap.a.a.a.d) owner).a(mime, stream);
            } finally {
                if (html && previousLimit == null) {
                    System.clearProperty(limitName);
                } else if (html) {
                    System.setProperty(limitName, previousLimit);
                }
            }
            ensure(target.length() > 0, "OUTPUT_UNVERIFIED");
            return new String[] { "renderer:export-" + format };
        });
    }

    private static void confirmPlainTextExport(File file) throws Exception {
        waitFor(() -> edt(() -> {
            Dialog confirmation = dialog(title -> true);
            if (confirmation == null) {
                return file.isFile() ? Boolean.TRUE : null;
            }
            ensure("Editor".equals(confirmation.getTitle()) &&
                nodes(confirmation, JLabel.class,
                    label -> textOf(label).startsWith("Dokümanda varolan stil bilgisi dosyaya")).size() == 1,
                "USER_ACTION_REQUIRED");
            button(confirmation, "Evet").doClick(0);
            return Boolean.TRUE;
        }), "TEXT_EXPORT_CONFIRMATION_TIMEOUT");
    }

    private static String[] formats() throws Exception {
        List<String> result = new ArrayList<>();
        for (boolean input : new boolean[] { true, false }) {
            JFileChooser chooser = documentChooser(input);
            try {
                result.addAll(edt(() -> {
                    List<String> labels = new ArrayList<>();
                    for (FileFilter filter : chooser.getChoosableFileFilters()) {
                        if (filter != chooser.getAcceptAllFileFilter() && filter.getDescription() != null) {
                            labels.add(input ? "open" : "save");
                            labels.add(filter.getDescription().trim());
                        }
                    }
                    return labels;
                }));
            } finally {
                edt(() -> {
                    if (chooser.isShowing()) {
                        chooser.cancelSelection();
                    }
                    return null;
                });
            }
            waitFor(() -> edt(() -> chooser.isShowing() ? null : Boolean.TRUE), "FORMAT_DIALOG_NOT_CLOSED");
        }
        return result.toArray(new String[0]);
    }

    private static JFileChooser documentChooser(boolean input) throws Exception {
        String actionId = input ? "open-action" : "save-as";
        Action action = edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            return editorAction(actionId);
        });
        EventQueue.invokeLater(() -> fire(action, actionId));
        return waitFor(() -> edt(() -> {
            Dialog open = dialog(title -> true);
            if (open == null) {
                return null;
            }
            int type = input ? JFileChooser.OPEN_DIALOG : JFileChooser.SAVE_DIALOG;
            List<JFileChooser> matches = nodes(open, JFileChooser.class, item -> item.getDialogType() == type);
            ensure(matches.size() <= 1, "FORMAT_DIALOG_AMBIGUOUS");
            return matches.isEmpty() ? null : matches.get(0);
        }), "FORMAT_DIALOG_TIMEOUT");
    }

    private static String[] importRtf(String path) throws Exception {
        File file = new File(path);
        ensure(file.isAbsolute() && file.isFile(), "IMPORT_SOURCE_INVALID");
        JFileChooser chooser = documentChooser(true);
        try {
            edt(() -> {
                FileFilter selected = null;
                for (FileFilter filter : chooser.getChoosableFileFilters()) {
                    if (matchesExtension(filter, "rtf")) {
                        ensure(selected == null, "IMPORT_FORMAT_AMBIGUOUS");
                        selected = filter;
                    }
                }
                ensure(selected != null, "IMPORT_FORMAT_UNAVAILABLE");
                chooser.setFileFilter(selected);
                chooser.setSelectedFile(file);
                ensure(file.equals(chooser.getSelectedFile()), "IMPORT_TARGET_MISMATCH");
                chooser.approveSelection();
                return null;
            });
            waitFor(() -> edt(() -> chooser.isShowing() ? null : Boolean.TRUE), "FORMAT_DIALOG_NOT_CLOSED");
        } finally {
            edt(() -> {
                if (chooser.isShowing()) {
                    chooser.cancelSelection();
                }
                return null;
            });
        }
        return ready(path);
    }

    private static String[] tabs() throws UiFailure {
        JRibbon ribbon = ribbon();
        String[] result = new String[ribbon.getTaskCount()];
        for (int i = 0; i < result.length; i++) {
            result[i] = ribbon.getTask(i).getTitle();
        }
        return result;
    }

    private static void selectTab(String name) throws Exception {
        edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JRibbon ribbon = ribbon();
            for (int i = 0; i < ribbon.getTaskCount(); i++) {
                RibbonTask task = ribbon.getTask(i);
                if (name.equals(task.getTitle())) {
                    ribbon.setSelectedTask(task);
                    return null;
                }
            }
            throw new UiFailure("RIBBON_TAB_UNKNOWN");
        });
    }

    private static JRibbon ribbon() throws UiFailure {
        return single(frame, JRibbon.class, component -> true);
    }

    private static String[] commands() throws UiFailure {
        List<AbstractCommandButton> buttons = nodes(ribbon(), AbstractCommandButton.class, b -> b.getText() != null);
        List<String> result = new ArrayList<>();
        for (AbstractCommandButton button : buttons) {
            result.add(button.getText());
            result.add(Boolean.toString(button.getActionModel().isEnabled()));
        }
        return result.toArray(new String[0]);
    }

    private static String[] catalog(String scope) throws UiFailure {
        ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
        Set<List<String>> rows = new LinkedHashSet<>();
        if ("actions".equals(scope)) {
            for (Action action : body().getActions()) {
                Object name = action.getValue(Action.NAME);
                if (name instanceof String && !((String) name).isEmpty()) {
                    Object description = action.getValue(Action.SHORT_DESCRIPTION);
                    rows.add(Arrays.asList("", description == null ? "" : description.toString(),
                        action.isEnabled() ? "enabled" : "disabled", (String) name, action.getClass().getName()));
                }
            }
        } else {
            ensure("menus".equals(scope), "INVENTORY_SCOPE_INVALID");
            JRibbon ribbon = ribbon();
            RibbonTask selected = ribbon.getSelectedTask();
            try {
                for (int i = 0; i < ribbon.getTaskCount(); i++) {
                    RibbonTask task = ribbon.getTask(i);
                    addRibbonTask(rows, ribbon, task, task.getTitle());
                }
                for (int i = 0; i < ribbon.getContextualTaskGroupCount(); i++) {
                    RibbonContextualTaskGroup group = ribbon.getContextualTaskGroup(i);
                    if (ribbon.isVisible(group)) {
                        for (int j = 0; j < group.getTaskCount(); j++) {
                            RibbonTask task = group.getTask(j);
                            addRibbonTask(rows, ribbon, task, group.getTitle() + "\u001f" + task.getTitle());
                        }
                    }
                }
                for (Component component : ribbon.getTaskbarComponents()) {
                    addMenuButtons(rows, "Hızlı erişim", component);
                }
                RibbonApplicationMenu appMenu = ribbon.getApplicationMenu();
                if (appMenu != null) {
                    for (List<RibbonApplicationMenuEntryPrimary> group : appMenu.getPrimaryEntries()) {
                        for (RibbonApplicationMenuEntryPrimary entry : group) {
                            addAppMenuEntry(rows, "Uygulama menüsü", entry.getText(), entry.isEnabled(), entry.getEntryKind());
                            for (int i = 0; i < entry.getSecondaryGroupCount(); i++) {
                                String path = "Uygulama menüsü\u001f" + entry.getText().trim() + "\u001f"
                                    + entry.getSecondaryGroupTitleAt(i);
                                for (RibbonApplicationMenuEntrySecondary child : entry.getSecondaryGroupEntries(i)) {
                                    addAppMenuEntry(rows, path, child.getText(), child.isEnabled(), child.getEntryKind());
                                }
                            }
                        }
                    }
                    for (RibbonApplicationMenuEntryFooter entry : appMenu.getFooterEntries()) {
                        addAppMenuEntry(rows, "Uygulama menüsü", entry.getText(), entry.isEnabled(), entry.getEntryKind());
                    }
                }
            } finally {
                if (selected != null) {
                    ribbon.setSelectedTask(selected);
                }
            }
        }
        List<String> result = new ArrayList<>();
        for (List<String> row : rows) {
            result.addAll(row);
        }
        return result.toArray(new String[0]);
    }

    private static void addRibbonTask(Set<List<String>> rows, JRibbon ribbon, RibbonTask task, String path) {
        ribbon.setSelectedTask(task);
        for (AbstractRibbonBand<?> band : task.getBands()) {
            addMenuButtons(rows, path + "\u001f" + band.getTitle(), band.getControlPanel());
        }
    }

    private static void addAppMenuEntry(Set<List<String>> rows, String path, String label, boolean enabled,
        JCommandButton.CommandButtonKind kind) {
        rows.add(Arrays.asList(path, label.trim(), enabled ? "enabled" : "disabled", "", kind.name()));
    }

    private static void addMenuButtons(Set<List<String>> rows, String path, Component component) {
        if (component instanceof AbstractCommandButton) {
            AbstractCommandButton button = (AbstractCommandButton) component;
            String label = button.getText();
            if (label == null || label.trim().isEmpty() || "null".equals(label)) {
                label = button.getToolTipText();
            }
            if ((label == null || label.trim().isEmpty()) && button instanceof JCommandButton
                && ((JCommandButton) button).getPopupCallback() != null) {
                label = "Etiketsiz menü";
            }
            if (label != null && !label.trim().isEmpty()) {
                String kind = button instanceof JCommandButton
                    ? ((JCommandButton) button).getCommandButtonKind().name() : "ACTION_ONLY";
                rows.add(Arrays.asList(path, label.trim(), button.getActionModel().isEnabled()
                    ? "enabled" : "disabled", "", kind));
                if (button instanceof JCommandButton) {
                    JCommandButton popup = (JCommandButton) button;
                    if (popup.getPopupCallback() != null) {
                        addPopupMenu(rows, path + "\u001f" + label.trim(), popup);
                    }
                }
            }
        } else if (component instanceof Container) {
            for (Component child : ((Container) component).getComponents()) {
                addMenuButtons(rows, path, child);
            }
        }
    }

    private static void addPopupMenu(Set<List<String>> rows, String path, JCommandButton button) {
        // Build the editor's list without opening its popup or dispatching any item action.
        try {
            JPopupPanel panel = button.getPopupCallback().getPopupPanel(button);
            if (panel instanceof JCommandPopupMenu) {
                for (Component item : ((JCommandPopupMenu) panel).getMenuComponents()) {
                    if (item instanceof AbstractCommandButton) {
                        AbstractCommandButton child = (AbstractCommandButton) item;
                        String label = child.getText();
                        if (label != null && !label.trim().isEmpty()) {
                            rows.add(Arrays.asList(path, label.trim(), child.getActionModel().isEnabled()
                                ? "enabled" : "disabled", "", "POPUP_ITEM"));
                        }
                    }
                }
            }
        } catch (RuntimeException unavailable) {
            rows.add(Arrays.asList(path, "", "unavailable", "", "POPUP"));
        }
    }

    private static String[] text(int start, int limit) throws Exception {
        return edt(() -> {
            Document doc = body().getDocument();
            ensure(start >= 0 && start <= doc.getLength() && limit > 0 && limit <= 8192, "TEXT_RANGE_INVALID");
            int count = Math.min(limit, doc.getLength() - start);
            return new String[] { Integer.toString(doc.getLength()), doc.getText(start, count) };
        });
    }

    private static final class ViewControls {
        // UYAP 5.4.20 hj initializes the renderer as zoomRatio / 100 * 1.4.
        private static final float FULL_SIZE_RENDER_SCALE = 1.4f;
        private static final Class<?> PANE = tr.com.havelsan.uyap.system.editor.common.text.fi.class;
        private static final Class<?> ZOOM_SLIDER = tr.com.havelsan.uyap.system.editor.common.gui.gq.class;
        private static final Method ZOOM_SCALE = method("a", float.class);
        private static final Method WHITESPACE = method("p", boolean.class);
        private static final java.lang.reflect.Field ZOOM_OPTIONS = zoomOptionsField();

        private static Method method(String name, Class<?> result) {
            // The pinned bytecode overloads no-argument Java methods by return type.
            return Arrays.stream(PANE.getMethods())
                .filter(method -> method.getName().equals(name) && method.getParameterCount() == 0
                    && method.getReturnType() == result)
                .findFirst().orElseThrow(() -> new IllegalStateException("Native editor view accessor unavailable"));
        }

        private static float renderScale(JTextComponent pane) throws Exception {
            return (Float) ZOOM_SCALE.invoke(pane);
        }

        private static boolean whitespace(JTextComponent pane) throws Exception {
            return (Boolean) WHITESPACE.invoke(pane);
        }

        private static java.lang.reflect.Field zoomOptionsField() {
            // The pinned slider has one JComboBox field alongside same-named fields of other types.
            java.lang.reflect.Field field = Arrays.stream(ZOOM_SLIDER.getDeclaredFields())
                .filter(candidate -> candidate.getType() == JComboBox.class)
                .findFirst().orElseThrow(() -> new IllegalStateException("Native zoom options unavailable"));
            field.setAccessible(true);
            return field;
        }

        private static JComboBox<?> zoomOptions() throws Exception {
            javax.swing.JSlider slider = single(frame, javax.swing.JSlider.class, ZOOM_SLIDER::isInstance);
            return (JComboBox<?>) ZOOM_OPTIONS.get(slider);
        }
    }

    private static String[] viewState() throws Exception {
        JTextComponent pane = body();
        JComboBox<?> combo = ViewControls.zoomOptions();
        float renderScale = ViewControls.renderScale(pane);
        List<String> result = new ArrayList<>(Arrays.asList(
            Integer.toString(Math.round(renderScale * 100 / ViewControls.FULL_SIZE_RENDER_SCALE)),
            Boolean.toString(ViewControls.whitespace(pane)),
            Integer.toString(pane.getCaretPosition()), Integer.toString(pane.getSelectionStart()),
            Integer.toString(pane.getSelectionEnd()), Integer.toString(pane.getDocument().getLength()),
            Float.toString(renderScale), Boolean.toString(!templateMode && commentPanelEnabled(commentOwner(pane)))));
        for (int index = 0; index < combo.getItemCount(); index++) {
            String item = String.valueOf(combo.getItemAt(index));
            if (item.matches("%[0-9]+")) {
                result.add(item.substring(1));
            }
        }
        return result.toArray(new String[0]);
    }

    private static int zoomPercent(JComboBox<?> combo) throws UiFailure {
        String item = String.valueOf(combo.getSelectedItem());
        ensure(item.matches("%[0-9]+"), "ZOOM_STATE_UNVERIFIED");
        return Integer.parseInt(item.substring(1));
    }

    private static String[] setView(String requestedZoom, String requestedWhitespace) throws Exception {
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            if (!requestedZoom.isEmpty()) {
                setZoom(pane, Integer.parseInt(requestedZoom));
            }
            if (!requestedWhitespace.isEmpty()) {
                boolean desired = Boolean.parseBoolean(requestedWhitespace);
                if (ViewControls.whitespace(pane) != desired) {
                    fire(editorAction("view-showWhiteSpace"), "view-showWhiteSpace");
                }
                ensure(ViewControls.whitespace(pane) == desired, "WHITESPACE_STATE_UNVERIFIED");
            }
            return viewState();
        });
    }

    private static void setZoom(JTextComponent pane, int percent) throws Exception {
        JComboBox<?> combo = ViewControls.zoomOptions();
        String item = "%" + percent;
        for (int index = 0; index < combo.getItemCount(); index++) {
            if (item.equals(combo.getItemAt(index))) {
                combo.setSelectedIndex(index);
                float expected = ViewControls.FULL_SIZE_RENDER_SCALE * percent / 100;
                if (Math.abs(ViewControls.renderScale(pane) - expected) >= 0.02f) {
                    // The detached startup combo can update its selection before notification reaches the pane.
                    ((tr.com.havelsan.uyap.system.editor.common.text.fi) pane).a(expected);
                }
                ensure(zoomPercent(combo) == percent
                    && Math.abs(ViewControls.renderScale(pane) - expected) < 0.02f, "ZOOM_STATE_UNVERIFIED");
                return;
            }
        }
        throw new UiFailure("ZOOM_OPTION_UNKNOWN");
    }

    private static String[] setFont(String family, String size) throws Exception {
        selectTab("Giriş");
        return edt(() -> {
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            ensure(pane.getSelectionEnd() > pane.getSelectionStart(), "FONT_SELECTION_REQUIRED");
            ensure(pane.getDocument() instanceof javax.swing.text.StyledDocument, "FONT_SELECTION_UNAVAILABLE");
            JComboBox<?> familyControl = family.isEmpty() ? null : fontControl(
                tr.com.havelsan.uyap.system.editor.common.text.bb.class);
            JComboBox<?> sizeControl = size.isEmpty() ? null : fontControl(
                tr.com.havelsan.uyap.system.editor.common.text.bc.class);
            int familyIndex = -1;
            if (familyControl != null) {
                for (int index = 0; index < familyControl.getItemCount(); index++) {
                    if (family.equals(familyControl.getItemAt(index))) {
                        familyIndex = index;
                        break;
                    }
                }
                ensure(familyIndex >= 0, "FONT_FAMILY_UNKNOWN");
            }
            int start = pane.getSelectionStart();
            int end = pane.getSelectionEnd();
            if (familyControl != null) {
                familyControl.setSelectedIndex(familyIndex);
            }
            if (sizeControl != null) {
                sizeControl.setSelectedItem(size);
            }
            javax.swing.text.StyledDocument doc = (javax.swing.text.StyledDocument) pane.getDocument();
            for (int offset = start; offset < end;) {
                Element run = doc.getCharacterElement(offset);
                javax.swing.text.AttributeSet attributes = run.getAttributes();
                ensure((family.isEmpty() || family.equals(javax.swing.text.StyleConstants.getFontFamily(attributes)))
                    && (size.isEmpty() || Integer.parseInt(size) == javax.swing.text.StyleConstants.getFontSize(attributes)),
                    "FONT_CHANGE_UNVERIFIED");
                offset = run.getEndOffset();
            }
            return new String[] { Integer.toString(start), Integer.toString(end), family, size };
        });
    }

    private static JComboBox<?> fontControl(Class<?> listenerType) throws UiFailure {
        return single(ribbon(), JComboBox.class, combo ->
            Arrays.stream(combo.getActionListeners()).anyMatch(listenerType::isInstance));
    }

    private static String[] setPaletteColor(String target, String value) throws Exception {
        ensure(value.matches("#[0-9a-fA-F]{6}"), "PALETTE_COLOR_INVALID");
        String label;
        boolean text;
        switch (target) {
            case "foreground": label = "Yazı Rengi"; text = true; break;
            case "highlight": label = "Yazı Arkaplan Rengi"; text = true; break;
            case "paper": label = "Sayfa Rengi"; text = false; break;
            case "canvas": label = "Sayfa Arkaplan Rengi"; text = false; break;
            default: throw new UiFailure("PALETTE_TARGET_UNKNOWN");
        }
        selectTab(text ? "Giriş" : "Biçim");
        return edt(() -> {
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            int start = pane.getSelectionStart();
            int end = pane.getSelectionEnd();
            ensure(!text || start < end, "PALETTE_SELECTION_REQUIRED");
            JCommandButton button = single(ribbon(), JCommandButton.class,
                item -> item.getText() != null && label.equals(item.getText().trim()) && item.getPopupCallback() != null);
            JPopupPanel popup = button.getPopupCallback().getPopupPanel(button);
            ensure(popup instanceof org.pushingpixels.flamingo.api.common.popup.JColorSelectorPopupMenu,
                "PALETTE_CONTROL_UNAVAILABLE");
            java.awt.Color color = java.awt.Color.decode(value);
            ((org.pushingpixels.flamingo.api.common.popup.JColorSelectorPopupMenu) popup)
                .getColorSelectorCallback().onColorSelected(color);
            if (text) {
                StyledDocument doc = (StyledDocument) pane.getDocument();
                for (int offset = start; offset < end;) {
                    Element run = doc.getCharacterElement(offset);
                    java.awt.Color applied = "foreground".equals(target) ? StyleConstants.getForeground(run.getAttributes())
                        : StyleConstants.getBackground(run.getAttributes());
                    ensure(color.equals(applied), "PALETTE_COLOR_UNVERIFIED");
                    offset = run.getEndOffset();
                }
            } else {
                java.awt.Color applied = "paper".equals(target)
                    ? tr.com.havelsan.uyap.system.swing.wp.p.G : pane.getBackground();
                ensure(color.equals(applied), "PALETTE_COLOR_UNVERIFIED");
            }
            return new String[] { value.toUpperCase(java.util.Locale.ROOT), Integer.toString(text ? start : -1),
                Integer.toString(text ? end : -1) };
        });
    }

    private static String[] setLineSpacing(String value) throws Exception {
        ensure(Arrays.asList("1.0", "1.15", "2.0", "2.5", "3.0").contains(value), "LINE_SPACING_UNAVAILABLE");
        selectTab("Giriş");
        int[] range = edt(() -> {
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            JCommandButton button = single(ribbon(), JCommandButton.class, item -> item.getPopupCallback() != null
                && "tr.gov.uyap.system.a.b.a.a.I".equals(item.getPopupCallback().getClass().getName()));
            JPopupPanel popup = button.getPopupCallback().getPopupPanel(button);
            ensure(popup instanceof JCommandPopupMenu, "LINE_SPACING_UNAVAILABLE");
            for (Component item : ((JCommandPopupMenu) popup).getMenuComponents()) {
                if (item instanceof AbstractCommandButton
                    && value.equals(((AbstractCommandButton) item).getText())) {
                    ((AbstractCommandButton) item).doActionClick();
                    return new int[] { pane.getSelectionStart(), pane.getSelectionEnd() };
                }
            }
            throw new UiFailure("LINE_SPACING_UNAVAILABLE");
        });
        float expected = Float.parseFloat(value) - 1f;
        return waitFor(() -> edt(() -> {
            StyledDocument doc = (StyledDocument) body().getDocument();
            for (int offset = range[0]; offset <= Math.max(range[0], range[1] - 1);) {
                Element paragraph = doc.getParagraphElement(offset);
                if (Math.abs(StyleConstants.getLineSpacing(paragraph.getAttributes()) - expected) > 0.001f) {
                    return null;
                }
                offset = paragraph.getEndOffset();
            }
            return new String[] { Integer.toString(range[0]), Integer.toString(range[1]) };
        }), "LINE_SPACING_UNVERIFIED");
    }

    private static String[] applyFormatBrush(int source, int start, int end, boolean includeParagraph) throws Exception {
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent body = body();
            ensure(body.isEditable(), "EDITOR_READ_ONLY");
            ensure(body instanceof tr.com.havelsan.uyap.system.editor.common.text.fi
                && body.getDocument() instanceof StyledDocument, "FORMAT_BRUSH_UNAVAILABLE");
            tr.com.havelsan.uyap.system.editor.common.text.fi pane =
                (tr.com.havelsan.uyap.system.editor.common.text.fi) body;
            StyledDocument doc = (StyledDocument) pane.getDocument();
            ensure(source >= 0 && source < doc.getLength() && start >= 0 && start < end && end <= doc.getLength(),
                "TEXT_RANGE_INVALID");
            ensure(isBrushText(doc.getCharacterElement(source)), "FORMAT_BRUSH_TEXT_REQUIRED");
            for (int offset = start; offset < end;) {
                Element run = doc.getCharacterElement(offset);
                ensure(isBrushText(run), "FORMAT_BRUSH_TEXT_REQUIRED");
                offset = run.getEndOffset();
            }
            java.lang.reflect.Field active = Arrays.stream(pane.getClass().getFields())
                .filter(field -> "e".equals(field.getName()) && field.getType() == boolean.class)
                .findFirst().orElse(null);
            ensure(active != null, "FORMAT_BRUSH_UNAVAILABLE");
            ensure(!active.getBoolean(pane), "FORMAT_BRUSH_ALREADY_ACTIVE");
            javax.swing.text.AttributeSet character = doc.getCharacterElement(source).getAttributes().copyAttributes();
            javax.swing.text.AttributeSet paragraph = doc.getParagraphElement(source).getAttributes().copyAttributes();
            Action capture = editorAction("format-brush");
            java.awt.Cursor cursor = pane.getCursor();
            try {
                selectRange(pane, source, includeParagraph ? source : source + 1);
                if (!includeParagraph) {
                    // The native brush captures the caret character; keep it on the source side.
                    pane.getCaret().setDot(source + 1);
                    pane.getCaret().moveDot(source);
                }
                fire(capture, "format-brush");
                ensure(active.getBoolean(pane), "FORMAT_BRUSH_UNAVAILABLE");
                selectRange(pane, start, end);
                java.awt.event.MouseEvent release = new java.awt.event.MouseEvent(pane,
                    java.awt.event.MouseEvent.MOUSE_RELEASED, System.currentTimeMillis(), 0, 0, 0, 1, false,
                    java.awt.event.MouseEvent.BUTTON1);
                java.awt.event.MouseListener listener = formatBrushListener(pane);
                ensure(listener != null, "FORMAT_BRUSH_UNAVAILABLE");
                listener.mouseReleased(release);
                verifyBrushAttributes(doc, character, paragraph, start, end, includeParagraph);
            } finally {
                java.awt.event.MouseListener listener = formatBrushListener(pane);
                if (listener != null) {
                    pane.removeMouseListener(listener);
                }
                active.setBoolean(pane, false);
                pane.setCursor(cursor);
            }
            return new String[] { Integer.toString(start), Integer.toString(end) };
        });
    }

    private static boolean isBrushText(Element run) {
        // UYAP represents ordinary spaces as separate native text runs.
        return "content".equals(run.getName()) || "space".equals(run.getName());
    }

    private static java.awt.event.MouseListener formatBrushListener(JTextComponent pane) {
        for (java.awt.event.MouseListener listener : pane.getMouseListeners()) {
            if ("tr.com.havelsan.uyap.system.editor.common.text.bf".equals(listener.getClass().getName())) {
                return listener;
            }
        }
        return null;
    }

    private static void verifyBrushAttributes(StyledDocument doc, javax.swing.text.AttributeSet character,
        javax.swing.text.AttributeSet paragraph, int start, int end, boolean includeParagraph) throws UiFailure {
        Object[] characterKeys = { StyleConstants.FontFamily, StyleConstants.FontSize, StyleConstants.Bold,
            StyleConstants.Italic, StyleConstants.Underline, StyleConstants.StrikeThrough, StyleConstants.Foreground,
            StyleConstants.Background, StyleConstants.Subscript, StyleConstants.Superscript };
        Object[] paragraphKeys = { StyleConstants.Alignment, StyleConstants.LineSpacing, StyleConstants.LeftIndent,
            StyleConstants.RightIndent, StyleConstants.FirstLineIndent, StyleConstants.SpaceAbove, StyleConstants.SpaceBelow };
        for (int offset = start; offset < end;) {
            Element run = doc.getCharacterElement(offset);
            for (Object key : characterKeys) {
                ensure(!character.isDefined(key)
                    || java.util.Objects.equals(character.getAttribute(key), run.getAttributes().getAttribute(key)),
                    "FORMAT_BRUSH_UNVERIFIED");
            }
            if (includeParagraph) {
                javax.swing.text.AttributeSet applied = doc.getParagraphElement(offset).getAttributes();
                for (Object key : paragraphKeys) {
                    ensure(!paragraph.isDefined(key)
                        || java.util.Objects.equals(paragraph.getAttribute(key), applied.getAttribute(key)),
                        "FORMAT_BRUSH_UNVERIFIED");
                }
            }
            offset = run.getEndOffset();
        }
    }

    private static String[] setListPalette(String preset) throws Exception {
        String listenerName;
        String nativeStyle;
        switch (preset) {
            case "none": listenerName = "kb"; nativeStyle = ""; break;
            case "number_parenthesis": listenerName = "kc"; nativeStyle = "NUMBER_TYPE_NUMBER_PARANTHESE_2"; break;
            case "number_dot": listenerName = "kd"; nativeStyle = "NUMBER_TYPE_NUMBER_DOT_2"; break;
            case "uppercase": listenerName = "ke"; nativeStyle = "NUMBER_TYPE_CHAR_BIG_2"; break;
            case "number_outline": listenerName = "kf"; nativeStyle = "NUMBER_TYPE_NUMBER_DOT_3"; break;
            case "diamond": listenerName = "jX"; nativeStyle = "BULLET_TYPE_DIAMOND_2"; break;
            default: throw new UiFailure("LIST_PALETTE_OPTION_INVALID");
        }
        selectTab("Giriş");
        return edt(() -> {
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            jV popup = listPalettePopup();
            String listenerClass = "tr.com.havelsan.uyap.system.editor.common.gui." + listenerName;
            Component tile = listPaletteTile(popup, listenerClass);
            ensure(tile != null, "LIST_PALETTE_UNAVAILABLE");
            int start = pane.getSelectionStart();
            int end = pane.getSelectionEnd();
            java.awt.event.MouseEvent click = new java.awt.event.MouseEvent(tile,
                java.awt.event.MouseEvent.MOUSE_CLICKED, System.currentTimeMillis(), 0, 0, 0, 1, false,
                java.awt.event.MouseEvent.BUTTON1);
            Arrays.stream(tile.getMouseListeners()).filter(listener -> listenerClass.equals(listener.getClass().getName()))
                .findFirst().get().mouseClicked(click);
            if (nativeStyle.isEmpty()) {
                clearRemainingListAttributes(pane, start, end);
            }
            verifyListPalette((StyledDocument) pane.getDocument(), start, end, nativeStyle);
            return new String[] { nativeStyle, Integer.toString(start), Integer.toString(end) };
        });
    }

    private static jV listPalettePopup() throws UiFailure {
        JCommandButton button = single(ribbon(), JCommandButton.class, item -> item.getPopupCallback() != null
            && "tr.gov.uyap.system.a.b.a.a.H".equals(item.getPopupCallback().getClass().getName()));
        JPopupPanel popup = button.getPopupCallback().getPopupPanel(button);
        ensure(popup instanceof jV, "LIST_PALETTE_UNAVAILABLE");
        return (jV) popup;
    }

    private static String[] setCustomList(String[] formats) throws Exception {
        Object[] keys = ListFormatKeys.BY_LEVEL;
        ensure(formats.length == keys.length && Arrays.stream(formats).anyMatch(format -> !format.isEmpty())
            && Arrays.stream(formats).allMatch(format -> format.isEmpty() || LIST_LEVEL_FORMAT.matcher(format).matches()),
            "LIST_NUMBER_FORMAT_INVALID");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            ensure(pane.isEditable() && pane instanceof fi, "EDITOR_READ_ONLY");
            fi editor = (fi) pane;
            DocumentEx document = (DocumentEx) editor.getDocument();
            List<?> selection = listSelection(document, editor);
            AttributeSet first = ((Element) selection.get(0)).getAttributes();
            boolean create = !isListItem(first);
            Object existingId = first.getAttribute(ad.x);
            for (Object item : selection) {
                AttributeSet attributes = ((Element) item).getAttributes();
                ensure(create ? !isListItem(attributes)
                    : Boolean.TRUE.equals(attributes.getAttribute(ad.m))
                        && java.util.Objects.equals(existingId, attributes.getAttribute(ad.x)),
                    "LIST_SELECTION_INVALID");
            }
            List<?> items = selection;
            if (!create) {
                items = existingListItems(editor, selection, existingId);
            }
            long id = create ? nextListId(document) : (Long) existingId;
            Map<Element, AttributeSet> edits = new LinkedHashMap<>();
            for (Object item : items) {
                Element paragraph = (Element) item;
                SimpleAttributeSet attributes = new SimpleAttributeSet();
                if (create) {
                    attributes.addAttribute(ad.m, true);
                    attributes.addAttribute(ad.o, DEFAULT_LIST_NUMBER_FORMAT);
                    attributes.addAttribute(ad.p, 1);
                    attributes.addAttribute(ad.x, id);
                    // Match the native multilevel list action's initial indent.
                    StyleConstants.setLeftIndent(attributes, 25f);
                }
                for (int index = 0; index < formats.length; index++) {
                    if (!formats[index].isEmpty()) {
                        attributes.addAttribute(keys[index], formats[index]);
                    }
                }
                edits.put(paragraph, attributes);
            }
            // Use the native grouped paragraph edit so list changes participate in undo and layout.
            document.a((l) editor.getCaret(), Collections.singletonList(edits));
            editor.repaint();
            for (Element paragraph : edits.keySet()) {
                AttributeSet attributes = paragraph.getAttributes();
                ensure(Boolean.TRUE.equals(attributes.getAttribute(ad.m)) && Long.valueOf(id).equals(attributes.getAttribute(ad.x)),
                    "CUSTOM_LIST_UNVERIFIED");
                for (int index = 0; index < formats.length; index++) {
                    ensure(formats[index].isEmpty() || formats[index].equals(attributes.getAttribute(keys[index])),
                        "CUSTOM_LIST_UNVERIFIED");
                }
            }
            return new String[] { Integer.toString(edits.size()), Boolean.toString(create) };
        });
    }

    private static List<?> listSelection(DocumentEx document, fi editor) throws UiFailure {
        int start = editor.getSelectionStart();
        int end = editor.getSelectionEnd();
        List<?> selection = start == end ? Collections.singletonList(document.getParagraphElement(start))
            : document.d(start, end - start);
        ensure(!selection.isEmpty(), "LIST_SELECTION_INVALID");
        return selection;
    }

    private static List<?> existingListItems(fi editor, List<?> selection, Object id) throws Exception {
        ensure(id instanceof Long && ((Long) id) != 0L, "LIST_SELECTION_INVALID");
        k lists = (k) commentLookup(fi.class, "a", k.class).invoke(editor);
        List<?> items = lists.b((Long) id);
        ensure(items != null && items.containsAll(selection), "LIST_SELECTION_INVALID");
        return items;
    }

    private static String[] setListIndents(String[] values) throws Exception {
        Float[] indents = new Float[values.length];
        for (int index = 0; index < indents.length; index++) {
            if (!values[index].isEmpty()) {
                indents[index] = Float.valueOf(values[index]);
            }
        }
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            ensure(pane.isEditable() && pane instanceof fi, "EDITOR_READ_ONLY");
            fi editor = (fi) pane;
            DocumentEx document = (DocumentEx) editor.getDocument();
            List<?> selection = listSelection(document, editor);
            Object id = ((Element) selection.get(0)).getAttributes().getAttribute(ad.x);
            List<?> items = existingListItems(editor, selection, id);
            Map<Element, AttributeSet> edits = new LinkedHashMap<>();
            Set<Integer> editedLevels = new HashSet<>();
            for (Object item : items) {
                Element paragraph = (Element) item;
                AttributeSet current = paragraph.getAttributes();
                int level = Math.max(1, listNumber(current, ad.p, 1));
                if (level > indents.length || indents[level - 1] == null) {
                    continue;
                }
                SimpleAttributeSet change = new SimpleAttributeSet();
                StyleConstants.setLeftIndent(change, indents[level - 1]);
                edits.put(paragraph, change);
                editedLevels.add(level);
            }
            for (int index = 0; index < indents.length; index++) {
                ensure(indents[index] == null || editedLevels.contains(index + 1), "LIST_LEVEL_NOT_PRESENT");
            }
            document.a((l) editor.getCaret(), Collections.singletonList(edits));
            editor.repaint();
            for (Map.Entry<Element, AttributeSet> edit : edits.entrySet()) {
                ensure(Float.compare(StyleConstants.getLeftIndent(edit.getKey().getAttributes()),
                    StyleConstants.getLeftIndent(edit.getValue())) == 0, "LIST_INDENT_UNVERIFIED");
            }
            return new String[] { Integer.toString(edits.size()) };
        });
    }

    private static long nextListId(DocumentEx document) throws Exception {
        // The obfuscated document declares several fields named "a".
        Field counter = Arrays.stream(DocumentEx.class.getDeclaredFields())
            .filter(field -> field.getType() == AtomicLong.class)
            .findFirst().orElseThrow(() -> new UiFailure("LIST_SELECTION_INVALID"));
        return ((AtomicLong) counter.get(document)).incrementAndGet();
    }

    private static Element listParagraph(JTextComponent pane) throws UiFailure {
        StyledDocument document = (StyledDocument) pane.getDocument();
        int start = pane.getSelectionStart();
        Element paragraph = document.getParagraphElement(start);
        ensure(document.getParagraphElement(Math.max(start, pane.getSelectionEnd() - 1)) == paragraph,
            "LIST_SINGLE_ITEM_REQUIRED");
        return paragraph;
    }

    private static int listNumber(AttributeSet attributes, Object key, int fallback) {
        Object value = attributes.getAttribute(key);
        return value instanceof Number ? ((Number) value).intValue() : fallback;
    }

    private static boolean isListItem(AttributeSet attributes) {
        return Boolean.TRUE.equals(attributes.getAttribute(ad.m))
            || Boolean.TRUE.equals(attributes.getAttribute(ad.l));
    }

    private static boolean isNumberedListItem(AttributeSet attributes) {
        if (!isListItem(attributes)) {
            return false;
        }
        int level = Math.max(1, listNumber(attributes, ad.p, 1));
        Object[] keys = ListFormatKeys.BY_LEVEL;
        Object override = level <= keys.length ? attributes.getAttribute(keys[level - 1]) : null;
        if (override instanceof String) {
            return ((String) override).startsWith("NUMBER_TYPE_");
        }
        return Boolean.TRUE.equals(attributes.getAttribute(ad.m));
    }

    private static String[] listItemValues(JTextComponent pane, Element paragraph) {
        AttributeSet attributes = paragraph.getAttributes();
        boolean listed = isListItem(attributes);
        int marker = listNumber(attributes, ad.w, -1);
        String[] result = new String[] { Integer.toString(paragraph.getStartOffset()),
            Integer.toString(Math.min(pane.getDocument().getLength(), paragraph.getEndOffset())),
            listed ? Integer.toString(listNumber(attributes, ad.p, 1)) : "",
            Boolean.toString(isNumberedListItem(attributes)),
            !listed || marker < 0 ? "" : Integer.toString(marker + 1) };
        if (!listed) {
            return result;
        }
        Object[] keys = ListFormatKeys.BY_LEVEL;
        String[] withFormats = Arrays.copyOf(result, result.length + keys.length + 1);
        for (int index = 0; index < keys.length; index++) {
            Object format = attributes.getAttribute(keys[index]);
            withFormats[result.length + index] = format instanceof String ? (String) format : "";
        }
        withFormats[withFormats.length - 1] = Float.toString(StyleConstants.getLeftIndent(attributes));
        return withFormats;
    }

    private static String[] setListLevel(int level) throws Exception {
        ensure(level >= 1 && level <= 6, "LIST_LEVEL_INVALID");
        selectTab("Giriş");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            Element paragraph = listParagraph(pane);
            AttributeSet attributes = paragraph.getAttributes();
            ensure(isListItem(attributes), "LIST_ITEM_REQUIRED");
            if (listNumber(attributes, ad.p, 1) != level) {
                jV popup = listPalettePopup();
                // Use the submenu's selection callback, which applies the native list level action.
                Method createLevels = Arrays.stream(jV.class.getDeclaredMethods())
                    .filter(method -> method.getParameterCount() == 0 && method.getReturnType() == hP.class)
                    .findFirst().orElseThrow(() -> new UiFailure("LIST_PALETTE_UNAVAILABLE"));
                createLevels.setAccessible(true);
                hP menu = (hP) createLevels.invoke(popup);
                javax.swing.event.PopupMenuListener apply = Arrays.stream(menu.getPopupMenuListeners())
                    .filter(listener -> "tr.com.havelsan.uyap.system.editor.common.gui.ka".equals(listener.getClass().getName()))
                    .findFirst().orElseThrow(() -> new UiFailure("LIST_PALETTE_UNAVAILABLE"));
                Method chooseLevel = hP.class.getDeclaredMethod("a", hP.class, int.class);
                chooseLevel.setAccessible(true);
                chooseLevel.invoke(null, menu, level);
                apply.popupMenuWillBecomeInvisible(new javax.swing.event.PopupMenuEvent(menu));
                paragraph = listParagraph(pane);
                ensure(listNumber(paragraph.getAttributes(), ad.p, 1) == level, "LIST_LEVEL_UNVERIFIED");
            }
            return listItemValues(pane, paragraph);
        });
    }

    private static String[] setNumberingStart(int startAt) throws Exception {
        ensure(startAt > 0, "NUMBERING_START_INVALID");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            Element paragraph = listParagraph(pane);
            ensure(pane instanceof fi && Boolean.TRUE.equals(paragraph.getAttributes().getAttribute(ad.m))
                && isNumberedListItem(paragraph.getAttributes()),
                "NUMBERED_LIST_ITEM_REQUIRED");
            int start = pane.getSelectionStart();
            int end = pane.getSelectionEnd();
            selectRange(pane, start, start);
            try {
                // Match the numbering dialog's restart behavior for the current item's list run.
                ((fi) pane).b(startAt);
                paragraph = listParagraph(pane);
                if (startAt == 1 && listNumber(paragraph.getAttributes(), ad.w, -1) != 0) {
                    // The native restart at 1 does not overwrite an existing explicit start.
                    SimpleAttributeSet reset = new SimpleAttributeSet();
                    reset.addAttribute(ad.w, 0);
                    ((StyledDocument) pane.getDocument()).setParagraphAttributes(start, 0, reset, false);
                    paragraph = listParagraph(pane);
                }
                ensure(listNumber(paragraph.getAttributes(), ad.w, -1) == startAt - 1,
                    "NUMBERING_START_UNVERIFIED");
                return listItemValues(pane, paragraph);
            } finally {
                selectRange(pane, start, end);
            }
        });
    }

    private static String[] continueNumbering() throws Exception {
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent pane = body();
            ensure(pane.isEditable(), "EDITOR_READ_ONLY");
            Element paragraph = listParagraph(pane);
            ensure(pane instanceof fi && Boolean.TRUE.equals(paragraph.getAttributes().getAttribute(ad.m))
                && isNumberedListItem(paragraph.getAttributes()),
                "NUMBERED_LIST_ITEM_REQUIRED");
            DocumentEx document = (DocumentEx) pane.getDocument();
            Element previous = document.c(paragraph);
            // Use the same paragraph traversal and predecessor as the native continue action.
            while (previous != null && !Boolean.TRUE.equals(previous.getAttributes().getAttribute(ad.m))) {
                previous = document.c(previous);
            }
            ensure(previous != null, "PREVIOUS_NUMBERED_LIST_REQUIRED");
            Object previousId = previous.getAttributes().getAttribute(ad.x);
            ensure(previousId instanceof Number && ((Number) previousId).longValue() != 0,
                "PREVIOUS_NUMBERED_LIST_REQUIRED");

            int start = pane.getSelectionStart();
            int end = pane.getSelectionEnd();
            selectRange(pane, start, start);
            try {
                // The numbering dialog delegates to this method for "Önceki listeden devam et".
                ((fi) pane).m();
                paragraph = listParagraph(pane);
                AttributeSet attributes = paragraph.getAttributes();
                ensure(previousId.equals(attributes.getAttribute(ad.x))
                    && listNumber(attributes, ad.w, -1) < 0, "NUMBERING_CONTINUATION_UNVERIFIED");
                return listItemValues(pane, paragraph);
            } finally {
                selectRange(pane, start, end);
            }
        });
    }

    private static void clearRemainingListAttributes(JTextComponent pane, int start, int end) {
        tr.com.havelsan.uyap.system.editor.common.text.DocumentEx doc =
            (tr.com.havelsan.uyap.system.editor.common.text.DocumentEx) pane.getDocument();
        Object[] listKeys = { tr.com.havelsan.uyap.system.swing.wp.model.ad.l,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.m, tr.com.havelsan.uyap.system.swing.wp.model.ad.n,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.o, tr.com.havelsan.uyap.system.swing.wp.model.ad.p,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.q, tr.com.havelsan.uyap.system.swing.wp.model.ad.r,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.s, tr.com.havelsan.uyap.system.swing.wp.model.ad.t,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.u, tr.com.havelsan.uyap.system.swing.wp.model.ad.v,
            tr.com.havelsan.uyap.system.swing.wp.model.ad.x };
        for (int offset = start; offset <= Math.max(start, end - 1);) {
            Element paragraph = doc.getParagraphElement(offset);
            javax.swing.text.AttributeSet existing = paragraph.getAttributes();
            boolean listed = Boolean.TRUE.equals(existing.getAttribute(listKeys[0]))
                || Boolean.TRUE.equals(existing.getAttribute(listKeys[1]));
            if (listed) {
                Object level = existing.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.ad.p);
                int depth = level instanceof Number ? ((Number) level).intValue() : 0;
                SimpleAttributeSet attributes = new SimpleAttributeSet(existing);
                for (Object key : listKeys) {
                    attributes.removeAttribute(key);
                }
                // Match the native list removal's indentation adjustment.
                float indent = StyleConstants.getLeftIndent(existing) - 25f * depth;
                if (indent > 0) {
                    StyleConstants.setLeftIndent(attributes, indent);
                } else {
                    attributes.removeAttribute(StyleConstants.LeftIndent);
                }
                doc.setParagraphAttributes(offset, Math.min(doc.getLength(), paragraph.getEndOffset()) - offset,
                    attributes, true);
            }
            offset = paragraph.getEndOffset();
        }
    }

    private static Component listPaletteTile(Container container, String listenerClass) {
        for (Component item : container.getComponents()) {
            if (item instanceof JLabel && Arrays.stream(item.getMouseListeners())
                .anyMatch(listener -> listenerClass.equals(listener.getClass().getName()))) {
                return item;
            }
            if (item instanceof Container) {
                Component match = listPaletteTile((Container) item, listenerClass);
                if (match != null) {
                    return match;
                }
            }
        }
        return null;
    }

    private static void verifyListPalette(StyledDocument doc, int start, int end, String style) throws UiFailure {
        for (int offset = start; offset <= Math.max(start, end - 1);) {
            Element paragraph = doc.getParagraphElement(offset);
            javax.swing.text.AttributeSet attributes = paragraph.getAttributes();
            boolean bulleted = Boolean.TRUE.equals(attributes.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.ad.l));
            boolean numbered = Boolean.TRUE.equals(attributes.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.ad.m));
            boolean verified;
            if (style.isEmpty()) {
                verified = !bulleted && !numbered;
            } else if (style.startsWith("BULLET")) {
                verified = bulleted && style.equals(attributes.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.ad.n));
            } else {
                verified = numbered && style.equals(attributes.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.ad.o));
            }
            ensure(verified, "LIST_PALETTE_UNVERIFIED");
            offset = paragraph.getEndOffset();
        }
    }

    private static String[] shortcutList(int offset, int limit) throws UiFailure {
        List<String[]> shortcuts = new ArrayList<>();
        for (Action action : body().getActions()) {
            Object key = action.getValue(Action.ACCELERATOR_KEY);
            Object id = action.getValue(Action.NAME);
            Object label = action.getValue(Action.SHORT_DESCRIPTION);
            if (key instanceof javax.swing.KeyStroke && id instanceof String) {
                shortcuts.add(new String[] { id.toString(), label == null ? "" : label.toString(), key.toString() });
            }
        }
        List<String> result = new ArrayList<>();
        result.add(Integer.toString(shortcuts.size()));
        for (int index = offset; index < shortcuts.size() && index - offset < limit; index++) {
            result.addAll(Arrays.asList(shortcuts.get(index)));
        }
        return result.toArray(new String[0]);
    }

    private static String[] find(String query, int from, boolean caseSensitive) throws Exception {
        ensure(!query.isEmpty() && query.length() <= 1000, "SEARCH_QUERY_INVALID");
        return edt(() -> {
            JTextComponent body = body();
            int length = body.getDocument().getLength();
            ensure(from >= 0 && from <= length, "TEXT_RANGE_INVALID");
            String content = body.getDocument().getText(0, length);
            int flags = caseSensitive ? 0 : Pattern.CASE_INSENSITIVE | Pattern.UNICODE_CASE;
            Matcher match = Pattern.compile(Pattern.quote(query), flags).matcher(content);
            if (!match.find(from)) {
                return new String[] { "-1", "-1" };
            }
            selectRange(body, match.start(), match.end());
            return new String[] { Integer.toString(match.start()), Integer.toString(match.end()) };
        });
    }

    private static final class NativeSpelling {
        private static final Method ENGINE = method(im.class, Zemberek.class);
        private static final Class<?> ITERATOR = iteratorClass();
        private static final Constructor<?> WORDS = iteratorConstructor();
        private static final Method HAS_WORD = method(ITERATOR, boolean.class);
        private static final Method NEXT_WORD = method(ITERATOR, String.class);
        private static final Method WORD_START = method(ITERATOR, int.class);
        private static final Method WORD_END = method(ITERATOR, "b", int.class);
        private static final Method EXCLUDED = method(Utils.class, boolean.class, String.class);
        private static final Method DELIMITER = method(Utils.class, boolean.class, char.class);

        // The 5.4.20 binary overloads several methods by return type alone.
        private static Method method(Class<?> owner, Class<?> result, Class<?>... arguments) {
            return method(owner, "a", result, arguments);
        }

        private static Method method(Class<?> owner, String name, Class<?> result, Class<?>... arguments) {
            for (Method candidate : owner.getMethods()) {
                if (candidate.getName().equals(name) && candidate.getReturnType() == result
                    && Arrays.equals(arguments, candidate.getParameterTypes())) {
                    return candidate;
                }
            }
            throw new IllegalStateException("Missing 5.4.20 spelling method: " + owner.getName());
        }

        private static Class<?> iteratorClass() {
            // A sibling class named common.b shadows this package in Java source.
            try {
                return Class.forName("tr.com.havelsan.uyap.system.editor.common.b.b");
            } catch (ClassNotFoundException error) {
                throw new IllegalStateException("Missing 5.4.20 spelling iterator", error);
            }
        }

        private static Constructor<?> iteratorConstructor() {
            try {
                return ITERATOR.getConstructor(Document.class, int.class);
            } catch (NoSuchMethodException error) {
                throw new IllegalStateException("Missing 5.4.20 spelling iterator constructor", error);
            }
        }

        private static Zemberek engine() throws Exception {
            Zemberek engine = (Zemberek) ENGINE.invoke(null);
            ensure(engine != null, "SPELL_DICTIONARY_UNAVAILABLE");
            return engine;
        }

        private static boolean delimiter(char ch) throws Exception {
            return (Boolean) DELIMITER.invoke(null, ch);
        }

        private static boolean misspelled(Zemberek engine, String word) throws Exception {
            return word.length() > 1 && !word.equals(word.toUpperCase())
                && !(Boolean) EXCLUDED.invoke(null, word) && !engine.kelimeDenetle(word);
        }
    }

    private static String[] spellCheck(int start, int characterLimit, int issueLimit) throws Exception {
        return edt(() -> {
            Document document = body().getDocument();
            int length = document.getLength();
            ensure(start >= 0 && start <= length && characterLimit > 0 && characterLimit <= 8192
                && issueLimit > 0 && issueLimit <= 50, "SPELL_RANGE_INVALID");
            Zemberek engine = NativeSpelling.engine();
            int scanEnd = start + Math.min(characterLimit, length - start);
            int next = start;
            if (next > 0 && next < length && !NativeSpelling.delimiter(document.getText(next - 1, 1).charAt(0))) {
                while (next < scanEnd && !NativeSpelling.delimiter(document.getText(next, 1).charAt(0))) {
                    next++;
                }
            }
            int count = 0;
            List<String> result = new ArrayList<>();
            Object words = NativeSpelling.WORDS.newInstance(document, next);
            while (next < scanEnd && count < issueLimit && (Boolean) NativeSpelling.HAS_WORD.invoke(words)) {
                String word = (String) NativeSpelling.NEXT_WORD.invoke(words);
                int wordStart = (Integer) NativeSpelling.WORD_START.invoke(words);
                int wordEnd = (Integer) NativeSpelling.WORD_END.invoke(words);
                if (word.isEmpty() || wordStart >= scanEnd) {
                    next = scanEnd;
                    break;
                }
                ensure(wordStart >= next && wordEnd == wordStart + word.length() && wordEnd <= length,
                    "SPELL_WORD_RANGE_UNVERIFIED");
                next = wordEnd;
                if (NativeSpelling.misspelled(engine, word)) {
                    result.add(Integer.toString(wordStart));
                    result.add(Integer.toString(wordEnd));
                    result.add(word);
                    result.add(String.join("\u001f", engine.oner(word)));
                    count++;
                }
            }
            if (count < issueLimit) {
                next = Math.max(next, scanEnd);
            }
            result.add(0, Integer.toString(next));
            result.add(0, Integer.toString(length));
            return result.toArray(new String[0]);
        });
    }

    private static String[] applySpelling(int start, int end, String expected, String suggestion) throws Exception {
        return edt(() -> {
            JTextComponent editor = body();
            Document document = editor.getDocument();
            int length = document.getLength();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            ensure(start >= 0 && start < end && end <= length, "SPELL_RANGE_INVALID");
            ensure(expected.equals(document.getText(start, end - start))
                && (start == 0 || NativeSpelling.delimiter(document.getText(start - 1, 1).charAt(0)))
                && (end == length || NativeSpelling.delimiter(document.getText(end, 1).charAt(0))),
                "SPELL_WORD_CHANGED");
            Zemberek engine = NativeSpelling.engine();
            ensure(NativeSpelling.misspelled(engine, expected)
                && Arrays.asList(engine.oner(expected)).contains(suggestion), "SPELL_SUGGESTION_INVALID");
            selectRange(editor, start, end);
            editor.replaceSelection(suggestion);
            ensure(document.getLength() == length - expected.length() + suggestion.length()
                && suggestion.equals(document.getText(start, suggestion.length())), "SPELL_CHANGE_UNVERIFIED");
            return new String[] { Integer.toString(document.getLength()) };
        });
    }

    private static String[] replace(int start, int end, String replacement) throws Exception {
        return edt(() -> {
            JTextComponent body = body();
            ensure(body.isEditable(), "EDITOR_READ_ONLY");
            ensure(start >= 0 && start <= end && end <= body.getDocument().getLength(), "TEXT_RANGE_INVALID");
            selectRange(body, start, end);
            body.replaceSelection(replacement);
            return new String[] { Integer.toString(body.getDocument().getLength()) };
        });
    }

    private static String[] listComments(int offset, int limit) throws Exception {
        return edt(() -> {
            AbstractDocument notes = commentsDocument(body());
            List<Element> comments = comments(notes);
            ensure(offset >= 0 && offset <= comments.size() && limit >= 1 && limit <= 20, "COMMENT_PAGE_INVALID");
            List<String> result = new ArrayList<>();
            result.add(Integer.toString(comments.size()));
            for (int i = offset; i < Math.min(comments.size(), offset + limit); i++) {
                result.addAll(Arrays.asList(commentFields(notes, comments.get(i), i)));
            }
            return result.toArray(new String[0]);
        });
    }

    private static String[] addComment(int start, int end, String text, int type) throws Exception {
        String replacement = commentTextInput(text);
        ensure(type == 1 || type == 2, "COMMENT_TYPE_INVALID");
        return edt(() -> {
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            ensure(start >= 0 && start < end && end <= editor.getDocument().getLength(), "TEXT_RANGE_INVALID");
            AbstractDocument notes = commentsDocument(editor);
            List<Element> comments = comments(notes);
            Set<Object> existing = new HashSet<>();
            for (Element comment : comments) {
                existing.add(commentReference(comment));
            }
            // This is the method used by both native comment actions. Calling it with explicit
            // offsets avoids targeting the current comment again when its pane has focus.
            Method create = tr.com.havelsan.uyap.system.editor.common.text.fi.class
                .getDeclaredMethod("a", int.class, int.class, int.class);
            create.setAccessible(true);
            noteDocumentChange();
            create.invoke(editor, type, start, end);
            comments = comments(notes);
            ensure(comments.size() == existing.size() + 1, "COMMENT_CREATE_UNVERIFIED");
            for (int i = 0; i < comments.size(); i++) {
                Element candidate = comments.get(i);
                tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(candidate);
                if (!existing.contains(reference)) {
                    ensure(reference.getStartOffset() == start && reference.getEndOffset() == end && reference.d() == type,
                        "COMMENT_CREATE_UNVERIFIED");
                    return writeCommentText(notes, comments, i, replacement);
                }
            }
            throw new UiFailure("COMMENT_CREATE_UNVERIFIED");
        });
    }

    private static String[] editComment(int index, int start, int end, String expectedText, String text) throws Exception {
        String replacement = commentTextInput(text);
        return edt(() -> {
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            AbstractDocument notes = commentsDocument(editor);
            List<Element> comments = comments(notes);
            ensure(index >= 0 && index < comments.size(), "COMMENT_STALE");
            Element target = comments.get(index);
            tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(target);
            ensure(reference.getStartOffset() == start && reference.getEndOffset() == end
                && commentText(notes, target).equals(expectedText), "COMMENT_STALE");
            return writeCommentText(notes, comments, index, replacement);
        });
    }

    private static String[] changeComment(int index, int start, int end, int type, String text, String operation)
        throws Exception {
        ensure("delete".equals(operation) || "toggle_type".equals(operation), "COMMENT_OPERATION_INVALID");
        return edt(() -> {
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            AbstractDocument notes = commentsDocument(editor);
            List<Element> before = comments(notes);
            ensure(index >= 0 && index < before.size(), "COMMENT_STALE");
            Element target = before.get(index);
            tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(target);
            ensure(reference.getStartOffset() == start && reference.getEndOffset() == end
                && reference.d() == type && commentText(notes, target).equals(text), "COMMENT_STALE");
            noteDocumentChange();
            if ("delete".equals(operation)) {
                // This overload is used when deleting a selected item in UYAP's comment pane.
                Method remove = tr.com.havelsan.uyap.system.editor.common.text.fi.class.getDeclaredMethod("a",
                    tr.com.havelsan.uyap.system.editor.e.c.class.getSuperclass());
                remove.setAccessible(true);
                remove.invoke(editor, reference);
                List<Element> after = comments(notes);
                ensure(after.size() == before.size() - 1
                    && after.stream().noneMatch(item -> commentReference(item) == reference), "COMMENT_DELETE_UNVERIFIED");
                editor.repaint();
                return new String[] { Integer.toString(after.size()), Integer.toString(type) };
            }
            ((tr.com.havelsan.uyap.system.editor.common.text.q) notes).a(java.util.Collections.singletonList(reference));
            List<Element> after = comments(notes);
            ensure(after.size() == before.size() && commentReference(after.get(index)) == reference
                && reference.d() == (type == 1 ? 2 : 1) && commentText(notes, after.get(index)).equals(text),
                "COMMENT_TYPE_UNVERIFIED");
            editor.repaint();
            return new String[] { Integer.toString(after.size()), Integer.toString(reference.d()) };
        });
    }

    private static String[] writeCommentText(AbstractDocument notes, List<Element> before, int index, String text)
        throws Exception {
        Element target = before.get(index);
        Object reference = commentReference(target);
        int start = target.getStartOffset();
        int length = target.getEndOffset() - start - 1;
        if (!commentText(notes, target).equals(text)) {
            noteDocumentChange();
            // The final character separates native comment elements, including the Swing end sentinel.
            notes.replace(start, length, text, null);
        }
        List<Element> after = comments(notes);
        ensure(after.size() == before.size() && commentReference(after.get(index)) == reference
            && commentText(notes, after.get(index)).equals(text), "COMMENT_EDIT_UNVERIFIED");
        return commentFields(notes, after.get(index), index);
    }

    private static AbstractDocument commentsDocument(JTextComponent editor) throws Exception {
        ensure(!templateMode && dialog(title -> true) == null, "COMMENTS_UNAVAILABLE");
        JTextComponent pane = commentPane(commentOwner(editor));
        ensure(pane != null && pane.getDocument() instanceof tr.com.havelsan.uyap.system.editor.common.text.q,
            "COMMENTS_UNAVAILABLE");
        return (AbstractDocument) pane.getDocument();
    }

    private static tr.com.havelsan.uyap.system.editor.common.gui.lo commentOwner(JTextComponent editor) throws Exception {
        return (tr.com.havelsan.uyap.system.editor.common.gui.lo)
            commentLookup(tr.com.havelsan.uyap.system.editor.common.text.fi.class, "a",
                tr.com.havelsan.uyap.a.a.a.d.class).invoke(editor);
    }

    private static JTextComponent commentPane(tr.com.havelsan.uyap.system.editor.common.gui.lo owner) throws Exception {
        return (JTextComponent) commentLookup(tr.com.havelsan.uyap.system.editor.common.gui.lo.class,
            "c", tr.com.havelsan.uyap.system.editor.common.text.fi.class).invoke(owner);
    }

    private static boolean commentPanelEnabled(tr.com.havelsan.uyap.system.editor.common.gui.lo owner) throws Exception {
        return (int) commentLookup(tr.com.havelsan.uyap.system.editor.common.gui.lo.class, "d", int.class).invoke(owner) == 1;
    }

    private static void navigateComments(JTextComponent editor, boolean next) throws Exception {
        List<Element> ordered = comments(commentsDocument(editor));
        ensure(!ordered.isEmpty(), "COMMENT_NOT_FOUND");
        ordered.sort(java.util.Comparator.comparingInt(comment -> commentReference(comment).getStartOffset()));
        int caret = editor.getCaretPosition();
        Element target = next ? ordered.get(0) : ordered.get(ordered.size() - 1);
        for (int i = 0; i < ordered.size(); i++) {
            Element candidate = ordered.get(next ? i : ordered.size() - i - 1);
            tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(candidate);
            if ((next && reference.getStartOffset() > caret) || (!next && reference.getEndOffset() < caret)) {
                target = candidate;
                break;
            }
        }
        tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(target);
        int start = reference.getStartOffset();
        selectRange(editor, start, start);
        ensure(editor.getCaretPosition() == start, "COMMENT_NAVIGATION_UNVERIFIED");
        java.awt.Rectangle location = editor.modelToView(start);
        ensure(location != null, "COMMENT_NAVIGATION_UNVERIFIED");
        editor.scrollRectToVisible(location);

        tr.com.havelsan.uyap.system.editor.common.gui.lo owner = commentOwner(editor);
        if (commentPanelEnabled(owner)) {
            owner.b(target.getStartOffset());
        } else {
            java.awt.Rectangle end = editor.modelToView(reference.getEndOffset());
            ensure(end != null, "COMMENT_NAVIGATION_UNVERIFIED");
            owner.a(reference, new java.awt.Point(end.x + 2, location.y),
                target.getStartOffset(), target.getStartOffset(), false, false, true);
        }
    }

    private static void setCommentVisibility(JTextComponent editor, boolean visible) throws Exception {
        ensure(!templateMode, "COMMENTS_UNAVAILABLE");
        tr.com.havelsan.uyap.system.editor.common.gui.lo owner = commentOwner(editor);
        if (commentPanelEnabled(owner) != visible) {
            fire(editorAction("comment-show"), "comment-show");
        }
        ensure(commentPanelEnabled(owner) == visible, "COMMENT_VISIBILITY_UNVERIFIED");
    }

    private static String[] showAllComments() throws Exception {
        boolean containsComments = edt(() -> {
            ensure(dialog(title -> true) == null && searchWindow() == null, "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            boolean populated = !comments(commentsDocument(editor)).isEmpty();
            setCommentVisibility(editor, true);
            if (populated && !commentPane(commentOwner(editor)).isShowing()) {
                commentOwner(editor).b(-1);
            }
            return populated;
        });
        if (containsComments) {
            waitFor(() -> edt(() -> commentPane(commentOwner(body())).isShowing() ? Boolean.TRUE : null),
                "COMMENT_VISIBILITY_UNVERIFIED");
        }
        return new String[] { "completed", "", "" };
    }

    private static Method commentLookup(Class<?> owner, String name, Class<?> returnType) throws UiFailure {
        // The vendor bytecode overloads zero-argument methods by return type.
        for (Method method : owner.getMethods()) {
            if (method.getName().equals(name) && method.getParameterCount() == 0 && method.getReturnType() == returnType) {
                return method;
            }
        }
        throw new UiFailure("COMMENTS_UNAVAILABLE");
    }

    private static List<Element> comments(AbstractDocument notes) {
        List<Element> result = new ArrayList<>();
        Element root = notes.getDefaultRootElement();
        for (int i = 0; i < root.getElementCount(); i++) {
            Element comment = root.getElement(i);
            if ("comment".equals(comment.getName()) && commentReference(comment) != null) {
                result.add(comment);
            }
        }
        return result;
    }

    private static tr.com.havelsan.uyap.system.editor.e.c commentReference(Element comment) {
        Object reference = comment.getAttributes().getAttribute("comRef");
        return reference instanceof tr.com.havelsan.uyap.system.editor.e.c
            ? (tr.com.havelsan.uyap.system.editor.e.c) reference : null;
    }

    private static String commentText(AbstractDocument notes, Element comment) throws Exception {
        int start = comment.getStartOffset();
        return notes.getText(start, comment.getEndOffset() - start - 1);
    }

    private static String[] commentFields(AbstractDocument notes, Element comment, int index) throws Exception {
        tr.com.havelsan.uyap.system.editor.e.c reference = commentReference(comment);
        return new String[] { Integer.toString(index), Integer.toString(reference.getStartOffset()),
            Integer.toString(reference.getEndOffset()), Integer.toString(reference.d()), commentText(notes, comment) };
    }

    private static String commentTextInput(String text) throws UiFailure {
        String normalized = text.replace("\r\n", "\n").replace('\r', '\n');
        ensure(normalized.length() <= 8192, "COMMENT_TEXT_LIMIT");
        // UYAP 5.4.20 writes comment text as one native XML CDATA section.
        ensure(!normalized.contains("]]>") && normalized.codePoints().allMatch(codePoint ->
            codePoint == '\t' || codePoint == '\n' || (codePoint >= 0x20 && codePoint <= 0xd7ff)
                || (codePoint >= 0xe000 && codePoint <= 0xfffd) || codePoint >= 0x10000),
            "COMMENT_TEXT_NOT_SERIALIZABLE");
        return normalized;
    }

    private static String[] selectText(int start, int end) throws Exception {
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            ensure(start >= 0 && start <= end && end <= editor.getDocument().getLength(), "TEXT_RANGE_INVALID");
            selectRange(editor, start, end);
            ensure(editor.getSelectionStart() == start && editor.getSelectionEnd() == end,
                "EDITOR_SELECTION_UNVERIFIED");
            return new String[] { Integer.toString(start), Integer.toString(end) };
        });
    }

    private static String[] compareUdf(String path, String decision, String output, int limit, String changeId, int offset,
        String direction, String fromChangeId) throws Exception {
        File baseline = new File(path);
        ensure(!templateMode && baseline.isAbsolute() && baseline.isFile(), "COMPARISON_INPUT_INVALID");
        Frame original = frame;
        List<Window> existing = edt(() -> Arrays.asList(Window.getWindows()));
        Action compareAction = edt(() -> {
            ensure(dialog(title -> true) == null && searchWindow() == null, "USER_ACTION_REQUIRED");
            original.toFront();
            original.requestFocus();
            body().requestFocusInWindow();
            return editorAction("watch-changes");
        });
        // The local version provider captures the focused document before opening its chooser.
        waitFor(() -> edt(() -> original.isFocused() ? Boolean.TRUE : null), "COMPARISON_WINDOW_INACTIVE");
        FutureTask<Void> event = new FutureTask<>(() -> {
            ensure(original.isFocused(), "COMPARISON_WINDOW_INACTIVE");
            fire(compareAction, "watch-changes");
            return null;
        });
        EventQueue.invokeLater(event);
        JFileChooser chooser = waitFor(() -> edt(() -> {
            JFileChooser visible = comparisonChooser(existing);
            if (visible == null && event.isDone()) {
                event.get();
            }
            return visible;
        }), "COMPARISON_CHOOSER_MISSING");
        try {
            edt(() -> {
                chooser.setSelectedFile(baseline);
                ensure(baseline.equals(chooser.getSelectedFile()) && chooser.getFileFilter().accept(baseline),
                    "COMPARISON_INPUT_INVALID");
                chooser.approveSelection();
                return null;
            });
        } finally {
            edt(() -> {
                if (chooser.isShowing()) {
                    chooser.cancelSelection();
                }
                return null;
            });
        }

        java.util.Optional<Frame> result = waitFor(() -> edt(() -> comparisonFrame(existing, event)),
            "COMPARISON_FRAME_MISSING");
        if (!result.isPresent()) {
            ensure(output.isEmpty(), "COMPARISON_NO_DIFFERENCES");
            ensure(fromChangeId.isEmpty(), "COMPARISON_CHANGE_STALE");
            return direction.isEmpty() ? new String[] { "0" }
                : comparisonNavigationResponse(new String[] { "0" }, new String[] { "false", "", "", "", "" });
        }
        Frame comparison = result.get();
        try {
            frame = comparison;
            return edt(() -> {
                ComparisonView before = comparisonView();
                String[] response = before.preview(offset, limit);
                if (!direction.isEmpty()) {
                    response = comparisonNavigationResponse(response, navigateComparison(before, direction, fromChangeId));
                }
                if (!output.isEmpty()) {
                    resolveComparison(before, decision, changeId);
                    writeNativeUdf(output);
                }
                return response;
            });
        } finally {
            frame = original;
            edt(() -> {
                Class<tr.com.havelsan.uyap.system.editor.common.WPAppManager> managerType =
                    tr.com.havelsan.uyap.system.editor.common.WPAppManager.class;
                tr.com.havelsan.uyap.system.editor.common.WPAppManager manager =
                    managerType.cast(commentLookup(managerType, "a", managerType).invoke(null));
                manager.a((tr.com.havelsan.uyap.a.a.a.d) comparison);
                original.toFront();
                original.requestFocus();
                return null;
            });
        }
    }

    private static String[] comparisonNavigationResponse(String[] preview, String[] navigation) {
        String[] result = new String[preview.length + navigation.length];
        result[0] = preview[0];
        System.arraycopy(navigation, 0, result, 1, navigation.length);
        System.arraycopy(preview, 1, result, 1 + navigation.length, preview.length - 1);
        return result;
    }

    private static String[] navigateComparison(ComparisonView view, String direction, String fromChangeId) throws Exception {
        ensure("next".equals(direction) || "previous".equals(direction), "COMPARISON_NAVIGATION_INVALID");
        boolean next = "next".equals(direction);
        ComparisonGroup from = null;
        int origin = next ? -1 : view.groups.size();
        if (!fromChangeId.isEmpty()) {
            from = view.groups.stream().filter(group -> view.id(group).equals(fromChangeId)).findFirst()
                .orElseThrow(() -> new UiFailure("COMPARISON_CHANGE_STALE"));
            origin = view.groups.indexOf(from);
        }
        int destination = origin + (next ? 1 : -1);
        if (destination < 0 || destination >= view.groups.size()) {
            return new String[] { "false", "", "", "", "" };
        }

        JTextComponent editor = body();
        int boundary = next ? 0 : editor.getDocument().getLength();
        selectRange(editor, from == null ? boundary : from.start, from == null ? boundary : from.end);
        ComparisonGroup expected = view.groups.get(destination);
        String observedThrough;
        if (expected.start == 0 || (!next && from == null && expected.end == boundary)) {
            // The native scanner uses zero and document length as sentinels, so it can
            // skip marked text on those boundaries. Select the verified model range there.
            selectRange(editor, expected.start, expected.end);
            observedThrough = "comparison document selection";
        } else {
            String command = direction + "-changes";
            ActionEvent event = new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, command);
            Action registered = Arrays.stream(editor.getActions())
                .filter(action -> command.equals(action.getValue(Action.NAME)))
                .findFirst().orElseThrow(() -> new UiFailure("EDITOR_ACTION_UNAVAILABLE"));
            // The document kit leaves these actions disabled by default. Invoke the
            // native handler only in the verified transient comparison and group range.
            if (next && registered instanceof tr.com.havelsan.uyap.system.editor.common.text.bS) {
                ((tr.com.havelsan.uyap.system.editor.common.text.bS) registered).a(event);
            } else if (!next && registered instanceof tr.com.havelsan.uyap.system.editor.common.text.ck) {
                ((tr.com.havelsan.uyap.system.editor.common.text.ck) registered).a(event);
            } else {
                throw new UiFailure("EDITOR_ACTION_UNAVAILABLE");
            }
            observedThrough = "native comparison action";
        }
        ensure(editor.getSelectionStart() == expected.start && editor.getSelectionEnd() == expected.end,
            "COMPARISON_NAVIGATION_UNVERIFIED");
        return new String[] { "true", Integer.toString(expected.start), Integer.toString(expected.end),
            view.id(expected), observedThrough };
    }

    private static void writeNativeUdf(String output) throws Exception {
        File target = new File(output);
        ensure(target.isAbsolute() && !target.exists(), "INVALID_STAGING_PATH");
        try (OutputStream stream = Files.newOutputStream(target.toPath(), StandardOpenOption.CREATE_NEW)) {
            writeNativeUdf(stream);
        }
    }

    private static void writeNativeUdf(OutputStream stream) throws Exception {
        // Serialize the live model without changing the native document's source path.
        Object owner = commentLookup(tr.com.havelsan.uyap.system.editor.common.text.fi.class, "a",
            tr.com.havelsan.uyap.a.a.a.d.class).invoke(body());
        ensure(owner instanceof tr.com.havelsan.uyap.system.editor.common.gui.lo, "DOCUMENT_SNAPSHOT_UNAVAILABLE");
        ((tr.com.havelsan.uyap.system.editor.common.gui.lo) owner).a(stream, true);
    }

    private static JFileChooser comparisonChooser(List<Window> existing) throws UiFailure {
        List<JFileChooser> matches = new ArrayList<>();
        for (Window window : Window.getWindows()) {
            // The vendor's comparison chooser is opened without the document frame as its owner.
            if (window instanceof Dialog && window.isShowing() && !existing.contains(window)) {
                matches.addAll(nodes((Dialog) window, JFileChooser.class,
                    item -> item.getDialogType() == JFileChooser.OPEN_DIALOG));
            }
        }
        ensure(matches.size() <= 1, "COMPARISON_CHOOSER_AMBIGUOUS");
        return matches.isEmpty() ? null : matches.get(0);
    }

    private static java.util.Optional<Frame> comparisonFrame(List<Window> existing, FutureTask<Void> event)
        throws Exception {
        Frame result = null;
        for (Frame candidate : Frame.getFrames()) {
            if (candidate.isShowing() && !existing.contains(candidate)
                && candidate.getTitle().trim().startsWith("Doküman Editörü v" + VERSION)
                && !nodes(candidate, Container.class, item -> item.getClass().getName()
                    .equals("tr.com.havelsan.uyap.system.swing.wp.textUtils.a.r")).isEmpty()) {
                ensure(result == null, "COMPARISON_FRAME_AMBIGUOUS");
                result = candidate;
            }
        }
        if (result != null) {
            return java.util.Optional.of(result);
        }
        Dialog message = dialog(title -> true);
        if (message != null && !existing.contains(message)) {
            List<javax.swing.JOptionPane> notices = nodes(message, javax.swing.JOptionPane.class,
                item -> "Belgeler arasında bir fark bulunamadı".equals(item.getMessage()));
            ensure(notices.size() == 1, "COMPARISON_FAILED");
            notices.get(0).setValue(javax.swing.JOptionPane.OK_OPTION);
            return java.util.Optional.empty();
        }
        if (event.isDone()) {
            event.get();
        }
        return null;
    }

    private static ComparisonView comparisonView() throws Exception {
        Document document = body().getDocument();
        ensure(document instanceof StyledDocument, "COMPARISON_MODEL_UNAVAILABLE");
        StyledDocument styled = (StyledDocument) document;
        List<ComparisonRange> ranges = new ArrayList<>();
        int offset = 0;
        while (offset < document.getLength()) {
            Element element = styled.getCharacterElement(offset);
            int end = Math.min(document.getLength(), element.getEndOffset());
            ensure(end > offset, "COMPARISON_RANGE_UNVERIFIED");
            Object change = element.getAttributes().getAttribute("change");
            if (change != null && !"EQUAL".equals(change.toString())) {
                ranges.add(new ComparisonRange(offset, end, change.toString(),
                    String.valueOf(element.getAttributes().getAttribute("user"))));
            }
            offset = end;
        }
        return new ComparisonView(document.getText(0, document.getLength()), ranges);
    }

    private static void resolveComparison(ComparisonView before, String decision, String changeId) throws Exception {
        boolean accept = "accept_one".equals(decision) || "accept_all".equals(decision);
        boolean individual = "accept_one".equals(decision) || "reject_one".equals(decision);
        ensure(accept || "reject_all".equals(decision) || "reject_one".equals(decision),
            "COMPARISON_DECISION_INVALID");
        if (individual) {
            ComparisonGroup selected = before.groups.stream()
                .filter(group -> before.id(group).equals(changeId))
                .findFirst().orElseThrow(() -> new UiFailure("COMPARISON_CHANGE_STALE"));
            String expected = before.project(selected, accept, false);
            JTextComponent editor = body();
            selectRange(editor, selected.start, selected.end);
            ensure(editor.getSelectionStart() == selected.start && editor.getSelectionEnd() == selected.end,
                "COMPARISON_SELECTION_UNVERIFIED");
            ensure(editor instanceof tr.com.havelsan.uyap.system.editor.common.text.fi
                && ((JEditorPane) editor).getEditorKit() instanceof tr.com.havelsan.uyap.system.editor.common.text.J,
                "COMPARISON_MODEL_UNAVAILABLE");
            Method resolve = tr.com.havelsan.uyap.system.editor.common.text.J.class.getDeclaredMethod("a",
                tr.com.havelsan.uyap.system.editor.common.text.fi.class, int.class, int.class, boolean.class);
            resolve.setAccessible(true);
            Object kit = ((JEditorPane) editor).getEditorKit();
            // This is the range handler shared by the installed accept and reject actions.
            // Invoke it directly to avoid the ribbon's focus-dependent auto-advance.
            resolve.invoke(kit, editor, selected.start, selected.end, accept);
            ComparisonView after = comparisonView();
            ensure(after.text.equals(before.project(selected, accept, true))
                && after.markedLength() == before.markedLength() - (selected.end - selected.start),
                "COMPARISON_CHANGE_UNVERIFIED");
            // A UDF has no persistent comparison workspace. Keep each other marked range
            // on the base side for accept_one, or on the current side for reject_one.
            for (int i = after.groups.size() - 1; i >= 0; i--) {
                ComparisonGroup group = after.groups.get(i);
                resolve.invoke(kit, editor, group.start, group.end, !accept);
            }
            ComparisonView resolved = comparisonView();
            ensure(resolved.ranges.isEmpty(), "COMPARISON_UNRESOLVED");
            ensure(resolved.text.equals(expected), "COMPARISON_CHANGE_UNVERIFIED");
            return;
        }
        String command = accept ? "accept-all-changes" : "reject-all-changes";
        if (!before.ranges.isEmpty()) {
            fire(editorAction(command), command);
        }
        ensure(comparisonView().ranges.isEmpty(), "COMPARISON_UNRESOLVED");
    }

    private static final class ComparisonRange {
        final int start;
        final int end;
        final String kind;
        final String author;
        ComparisonGroup group;

        ComparisonRange(int start, int end, String kind, String author) {
            this.start = start;
            this.end = end;
            this.kind = kind;
            this.author = author;
        }

        String key() {
            return start + "." + end + "." + kind;
        }

        boolean keep(boolean accept) throws UiFailure {
            ensure("INSERT".equals(kind) || "DELETE".equals(kind), "COMPARISON_CHANGE_UNSUPPORTED");
            return accept == "INSERT".equals(kind);
        }
    }

    private static final class ComparisonGroup {
        final int start;
        int end;
        final String author;

        ComparisonGroup(ComparisonRange first) {
            start = first.start;
            end = first.end;
            author = first.author;
        }
    }

    private static final class ComparisonView {
        final String text;
        final List<ComparisonRange> ranges;
        final List<ComparisonGroup> groups = new ArrayList<>();
        private String revision;

        ComparisonView(String text, List<ComparisonRange> ranges) {
            this.text = text;
            this.ranges = ranges;
            ComparisonGroup group = null;
            for (ComparisonRange range : ranges) {
                if (group == null || group.end != range.start || !group.author.equals(range.author)) {
                    group = new ComparisonGroup(range);
                    groups.add(group);
                } else {
                    group.end = range.end;
                }
                range.group = group;
            }
        }

        String id(ComparisonGroup group) {
            return revision + "." + group.start + "." + group.end;
        }

        String[] preview(int offset, int limit) throws Exception {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            digest.update(text.getBytes(StandardCharsets.UTF_16BE));
            for (ComparisonRange range : ranges) {
                digest.update((";" + range.key()).getBytes(StandardCharsets.UTF_8));
                digest.update((range.group.end == range.end ? (byte) 1 : (byte) 0));
            }
            revision = Base64.getUrlEncoder().withoutPadding().encodeToString(digest.digest());
            List<String> result = new ArrayList<>();
            result.add(Integer.toString(ranges.size()));
            int start = Math.min(offset, ranges.size());
            int end = start + Math.min(limit, ranges.size() - start);
            for (int i = start; i < end; i++) {
                ComparisonRange range = ranges.get(i);
                int previewEnd = Math.min(range.end, range.start + 160);
                if (previewEnd < range.end && Character.isHighSurrogate(text.charAt(previewEnd - 1))) {
                    previewEnd--;
                }
                result.add(Integer.toString(range.start));
                result.add(Integer.toString(range.end));
                result.add(range.kind);
                result.add(text.substring(range.start, previewEnd));
                result.add(id(range.group));
                result.add(Integer.toString(range.group.start));
                result.add(Integer.toString(range.group.end));
            }
            return result.toArray(new String[0]);
        }

        int markedLength() {
            return ranges.stream().mapToInt(range -> range.end - range.start).sum();
        }

        String project(ComparisonGroup selected, boolean accept, boolean leaveUnresolved) throws UiFailure {
            StringBuilder result = new StringBuilder(text.length());
            int offset = 0;
            for (ComparisonRange range : ranges) {
                result.append(text, offset, range.start);
                if (range.group == selected ? range.keep(accept) : leaveUnresolved || range.keep(!accept)) {
                    result.append(text, range.start, range.end);
                }
                offset = range.end;
            }
            return result.append(text, offset, text.length()).toString();
        }
    }

    private static void selectRange(JTextComponent editor, int start, int end) throws UiFailure {
        try {
            editor.getCaret().setSelectionVisible(true);
            editor.select(start, end);
        } catch (java.util.EmptyStackException error) {
            // The 5.4.20 caret listener can fail after applying the requested selection.
            // Only continue if the actual range is already correct.
            ensure(editor.getSelectionStart() == start && editor.getSelectionEnd() == end,
                "EDITOR_SELECTION_UNVERIFIED");
        }
    }

    private static String[] insertImage(String path, String action) throws Exception {
        ensure("insert-image".equals(action) || "insert-background-image".equals(action), "IMAGE_ACTION_INVALID");
        boolean background = "insert-background-image".equals(action);
        String editorClass = "tr.com.havelsan.uyap.system.editor.common.gui." + (background ? "gR" : "cz");
        File file = new File(path);
        ensure(file.isAbsolute() && file.isFile(), "IMAGE_SOURCE_INVALID");
        int imagesBefore = edt(() -> countImages(body().getDocument().getDefaultRootElement()));
        Action insert = edt(() -> {
            ensure(dialog(title -> true) == null && body().isEditable(), "USER_ACTION_REQUIRED");
            ensure(imageWindow(editorClass, false) == null, "IMAGE_EDITOR_ALREADY_OPEN");
            return editorAction(action);
        });
        EventQueue.invokeLater(() -> fire(insert, action));
        if (background) {
            Window panel = waitFor(() -> edt(() -> imageWindow(editorClass, true)), "BACKGROUND_DIALOG_UNAVAILABLE");
            AbstractButton browse = edt(() -> button(panel, "GÖZAT"));
            EventQueue.invokeLater(() -> browse.doClick(0));
        }
        JFileChooser chooser = waitFor(() -> edt(() -> {
            Window owner = imageWindow(editorClass, false);
            List<JFileChooser> matches = new ArrayList<>();
            for (Window window : Window.getWindows()) {
                if (window instanceof Dialog && window.isShowing()
                    && (window.getOwner() == frame || (owner != null && window.getOwner() == owner))) {
                    matches.addAll(nodes(window, JFileChooser.class,
                        item -> item.getDialogType() == JFileChooser.OPEN_DIALOG));
                }
            }
            ensure(matches.size() <= 1, "IMAGE_DIALOG_AMBIGUOUS");
            return matches.isEmpty() ? null : matches.get(0);
        }), "IMAGE_DIALOG_UNAVAILABLE");
        edt(() -> {
            chooser.setSelectedFile(file);
            ensure(file.equals(chooser.getSelectedFile()), "IMAGE_TARGET_MISMATCH");
            chooser.approveSelection();
            return null;
        });
        waitFor(() -> edt(() -> !chooser.isShowing() ? Boolean.TRUE : null), "IMAGE_DIALOG_NOT_CLOSED");
        Window panel = waitFor(() -> edt(() -> imageWindow(editorClass, true)), "IMAGE_PREVIEW_UNAVAILABLE");
        AbstractButton apply = edt(() -> button(panel, background ? "Tamam" : "Ekle"));
        EventQueue.invokeLater(() -> apply.doClick(0));
        waitFor(() -> edt(() -> !panel.isShowing() ? Boolean.TRUE : null), "IMAGE_APPLY_UNVERIFIED");
        if (!background) {
            waitFor(() -> edt(() -> countImages(body().getDocument().getDefaultRootElement()) > imagesBefore
                ? Boolean.TRUE : null), "IMAGE_NOT_INSERTED");
        }
        return new String[] { background ? "background_dialog_applied" : "image_inserted" };
    }

    private static Window imageWindow(String className, boolean visible) throws UiFailure {
        Window match = null;
        for (Window window : Window.getWindows()) {
            if (window.isDisplayable() && (!visible || window.isShowing()) && window.getClass().getName().equals(className)) {
                ensure(match == null, "IMAGE_DIALOG_AMBIGUOUS");
                match = window;
            }
        }
        return match;
    }

    private static int countImages(javax.swing.text.Element element) {
        int count = "image".equals(element.getName()) ? 1 : 0;
        for (int i = 0; i < element.getElementCount(); i++) {
            count += countImages(element.getElement(i));
        }
        return count;
    }

    private static String[] simpleAction(String id, String text) throws Exception {
        // These actions affect the managed copy or its viewport and do not open modal dialogs.
        ensure(!"watch-changes".equals(id), "USE_COMPARE_TOOL");
        ensure(!"divide-table-cell".equals(id), "USE_FORM_TOOL");
        if ("edit-hf".equals(id)) {
            String[] region = setPageRegion(null, text);
            return edt(() -> {
                String[] selection = selectionValues(body());
                return new String[] { selection[0], selection[1], region[0] };
            });
        }
        if ("startNumbering-action".equals(id)) {
            setNumberingStart(1);
            return edt(() -> selectionValues(body()));
        }
        if ("windows-menu".equals(id)) {
            return activateManagedWindow();
        }
        if ("application-freemem-level1".equals(id)) {
            return clearManagedHistory();
        }
        if ("show-paragraph-properties".equals(id)) {
            return edt(EditorLauncher::paragraphProperties);
        }
        if ("apdu-action".equals(id)) {
            selectTab("Araçlar");
            return edt(() -> {
                ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                    "USER_ACTION_REQUIRED");
                JCommandToggleButton toggle = single(ribbon(), JCommandToggleButton.class,
                    button -> "APDU Etkin".equals(button.getText()));
                boolean enabled = SigningPreferences.toggleApdu(toggle, editorAction(id));
                String[] selection = selectionValues(body());
                return new String[] { selection[0], selection[1], "{\"apduEnabled\":" + enabled + "}" };
            });
        }
        if ("beep".equals(id)) {
            String[] selection = edt(() -> {
                ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                    "USER_ACTION_REQUIRED");
                return selectionValues(body());
            });
            return new String[] { selection[0], selection[1], BeepPlayback.play() };
        }
        switch (id) {
            case "undo": case "redo": case "font-bold": case "font-italic": case "font-underline":
            case "font-bulleted": case "font-numbered":
            case "Strike-through": case "sub-script": case "super-script": case "clear-format":
            case "left-justify": case "center-justify": case "right-justify": case "both-justify":
            case "increase-font-size": case "decrease-font-size": case "insert-pageBreak":
            case "combine-table-cell": case "insert-tablerow-after":
            case "insert-tablerow-before": case "insert-column-left": case "insert-column-right":
            case "insert-tablecell-after": case "insert-tablecell-before":
            case "insert-paragraph-aftertable": case "increase-firstline-indent":
            case "increase-hanging-indent": case "increase-leftIndent": case "decrease-leftIndent":
            case "changeCase-firstUpper": case "changeCase-lower": case "changeCase-upper": case "changeCase-switch":
            case "decrease-firstline-indent": case "insert Tl symbol":
            case "comment-delete-all":
            case "copy-to-clipboard": case "cut-to-clipboard": case "paste-from-clipboard":
            case "switch-letters": case "rotate-text": case "comment-next": case "comment-previous":
            case "comment-show": case "next-changes": case "previous-changes":
            case "selection-begin": case "selection-end": case "caret-begin": case "caret-end":
            case "caret-begin-line": case "caret-end-line": case "caret-previous-word": case "caret-next-word":
            case "caret-begin-word": case "caret-end-word": case "select-line": case "select-word":
            case "selection-begin-line": case "selection-end-line": case "selection-previous-word":
            case "selection-next-word": case "selection-begin-word": case "selection-end-word":
            case "page-up": case "page-down": case "selection-page-up": case "selection-page-down":
            case "select-all": case "view-showWhiteSpace": case "fullscreen": case "resetfullscreen":
            case "template-elementMarker-switch": case "table-erasable":
            case "remove-field-group": case "remove-paragraph-field-group":
            case "caret-backward": case "caret-forward": case "caret-down": case "caret-up":
            case "caret-begin-paragraph": case "caret-end-paragraph": case "select-paragraph":
            case "selection-backward": case "selection-forward": case "selection-down": case "selection-up":
            case "selection-begin-paragraph": case "selection-end-paragraph":
            case "selection-page-left": case "selection-page-right": case "unselect":
            case "delete-next": case "delete-next-word": case "delete-previous": case "delete-previous-word":
            case "insert-break": case "insert-tab": case "insert-tab-ex": case "tab-shift":
            case "default-typed": case "insert-content":
            case "decrease-hanging-indent": case "decrease-left-indent-step":
            case "font-family-Monospaced": case "font-family-SansSerif": case "font-family-Serif":
            case "font-size-8": case "font-size-10": case "font-size-12": case "font-size-14":
            case "font-size-16": case "font-size-18": case "font-size-24": case "font-size-36": case "font-size-48":
            case "delete-entryfield-group": case "debug-lvuMetrics": case "dump-model":
                break;
            default:
                throw new UiFailure("ACTION_NOT_AVAILABLE_IN_MCP");
        }
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            if ("delete-entryfield-group".equals(id)) {
                ensure(templateMode, "TEMPLATE_SESSION_REQUIRED");
                SpecialActions.deleteEntryFieldGroup(editor);
                return selectionValues(editor);
            }
            if ("debug-lvuMetrics".equals(id) || "dump-model".equals(id)) {
                String diagnostic = "dump-model".equals(id) ? SpecialActions.dumpModel(editor)
                    : SpecialActions.layoutMetrics(editor, editorAction(id));
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()), diagnostic };
            }
            if ("combine-table-cell".equals(id)) {
                TableActions.combine(editor);
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            if ("clear-format".equals(id)) {
                clearCharacterFormatting(editor);
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            if ("copy-to-clipboard".equals(id) || "cut-to-clipboard".equals(id)
                || "paste-from-clipboard".equals(id)) {
                useSessionClipboard(editor, id);
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            if ("insert Tl symbol".equals(id) && !PresentationActions.hasLegacyLiraFont()) {
                PresentationActions.insertUnicodeLira(editor);
                return selectionValues(editor);
            }
            if ("comment-next".equals(id) || "comment-previous".equals(id)) {
                navigateComments(editor, "comment-next".equals(id));
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            if ("comment-show".equals(id)) {
                ensure(!templateMode, "COMMENTS_UNAVAILABLE");
                setCommentVisibility(editor, !commentPanelEnabled(commentOwner(editor)));
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            if ("comment-delete-all".equals(id)) {
                ensure(editor.isEditable(), "EDITOR_READ_ONLY");
                AbstractDocument notes = commentsDocument(editor);
                if (!comments(notes).isEmpty()) {
                    noteDocumentChange();
                    fire(new tr.com.havelsan.uyap.system.editor.common.text.aH(), id);
                    ensure(comments(notes).isEmpty(), "COMMENT_DELETE_UNVERIFIED");
                }
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()) };
            }
            Action action = editorAction(id);
            if ("selection-page-left".equals(id)) {
                // UYAP 5.4.20 registers both horizontal actions with the rightward flag.
                action = new tr.com.havelsan.uyap.system.editor.common.text.eI(id, true, true);
            }
            if ("table-erasable".equals(id)) {
                ensure(elementAtCaret(editor, "table") != null, "TEMPLATE_TABLE_NOT_SELECTED");
            }
            if ("remove-field-group".equals(id) && editor.getSelectionStart() != editor.getSelectionEnd()) {
                // UYAP selects the whole placeholder atomically. Its native group action reads
                // the character at the caret, so position it at the selected field's start.
                int first = editor.getSelectionStart();
                selectRange(editor, first, first);
                ensure(editor.getCaretPosition() == first, "TEMPLATE_FIELD_POSITION_UNVERIFIED");
            }
            if ("font-bulleted".equals(id) || "font-numbered".equals(id)) {
                // These native actions expect the UYAP document context as the event source.
                Class<?> contextType = Class.forName("tr.com.havelsan.uyap.a.a.a.d");
                Container context = javax.swing.SwingUtilities.getAncestorOfClass(contextType, editor);
                ensure(context != null, "LIST_CONTEXT_MISSING");
                action.actionPerformed(new ActionEvent(context, ActionEvent.ACTION_PERFORMED, id));
            } else if ("fullscreen".equals(id)) {
                int number = templateMode ? 1 : Integer.parseInt(page()[0]);
                String geometry = PresentationActions.fitPage(editor, ViewControls.zoomOptions(),
                    ViewControls.FULL_SIZE_RENDER_SCALE, number);
                return new String[] { Integer.toString(editor.getSelectionStart()),
                    Integer.toString(editor.getSelectionEnd()), geometry };
            } else if ("resetfullscreen".equals(id)) {
                setZoom(editor, 100);
            } else if (id.startsWith("font-family-")) {
                // Swing interprets the event command as the family when its source is the editor pane.
                fire(action, id.substring("font-family-".length()));
            } else if ("default-typed".equals(id) || "insert-content".equals(id)) {
                ensure(editor.isEditable(), "EDITOR_READ_ONLY");
                int oldLength = editor.getDocument().getLength();
                int start = editor.getSelectionStart();
                int removed = editor.getSelectionEnd() - start;
                action.actionPerformed(new ActionEvent(editor, ActionEvent.ACTION_PERFORMED, text));
                ensure(editor.getDocument().getLength() == oldLength - removed + text.length()
                    && text.equals(editor.getDocument().getText(start, text.length())), "TEXT_ACTION_UNVERIFIED");
            } else {
                fire(action, id);
            }
            return selectionValues(editor);
        });
    }

    private static String[] selectionValues(JTextComponent editor) {
        return new String[] { Integer.toString(editor.getSelectionStart()), Integer.toString(editor.getSelectionEnd()) };
    }

    private static String[] paragraphProperties() throws Exception {
        ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
            "USER_ACTION_REQUIRED");
        JTextComponent editor = body();
        ensure(editor.getDocument() instanceof StyledDocument, "PARAGRAPH_PROPERTIES_UNAVAILABLE");
        StyledDocument document = (StyledDocument) editor.getDocument();
        Element paragraph = document.getParagraphElement(editor.getCaretPosition());
        AttributeSet attributes = new SimpleAttributeSet(paragraph.getAttributes());
        String nativeAttributes = attributes.toString();
        String diagnostic = "start=" + paragraph.getStartOffset()
            + "\nend=" + Math.min(document.getLength(), paragraph.getEndOffset())
            + "\nalignment=" + StyleConstants.getAlignment(attributes)
            + "\nleftIndent=" + StyleConstants.getLeftIndent(attributes)
            + "\nrightIndent=" + StyleConstants.getRightIndent(attributes)
            + "\nlineSpacingMultiplier=" + (StyleConstants.getLineSpacing(attributes) + 1f)
            + "\nattributes=" + nativeAttributes.substring(0, Math.min(8192, nativeAttributes.length()));
        return new String[] { Integer.toString(editor.getSelectionStart()),
            Integer.toString(editor.getSelectionEnd()), diagnostic };
    }

    private static String[] clearManagedHistory() throws Exception {
        return edt(() -> {
            ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            tr.com.havelsan.uyap.system.editor.common.text.hO history = managedHistory(editor);
            // The native command also clears the system clipboard. Restrict that part to the
            // clipboard used by this isolated MCP session.
            history.discardAllEdits();
            sessionClipboard.setContents(null, null);
            ensure(!history.canUndo() && !history.canRedo() && sessionClipboard.getContents(null) == null,
                "MANAGED_HISTORY_CLEAR_UNVERIFIED");
            return selectionValues(editor);
        });
    }

    private static tr.com.havelsan.uyap.system.editor.common.text.hO managedHistory(JTextComponent editor)
        throws UiFailure {
        ensure(editor.getDocument() instanceof AbstractDocument, "UNDO_HISTORY_UNAVAILABLE");
        tr.com.havelsan.uyap.system.editor.common.text.hO history = null;
        for (javax.swing.event.UndoableEditListener listener :
            ((AbstractDocument) editor.getDocument()).getUndoableEditListeners()) {
            if (listener instanceof tr.com.havelsan.uyap.system.editor.common.text.hO) {
                ensure(history == null, "UNDO_HISTORY_AMBIGUOUS");
                history = (tr.com.havelsan.uyap.system.editor.common.text.hO) listener;
            }
        }
        ensure(history != null, "UNDO_HISTORY_UNAVAILABLE");
        return history;
    }

    private static String[] activateManagedWindow() throws Exception {
        edt(() -> {
            ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                "USER_ACTION_REQUIRED");
            ensure(InterfaceActions.activateWindow(frame), "WINDOW_ACTION_UNAVAILABLE");
            return null;
        });
        return waitFor(() -> edt(() -> frame.isActive() ? selectionValues(body()) : null),
            "WINDOW_ACTIVATION_UNVERIFIED");
    }

    private static void useSessionClipboard(JTextComponent editor, String id) throws UiFailure {
        TransferHandler handler = editor.getTransferHandler();
        ensure(handler != null, "CLIPBOARD_UNAVAILABLE");
        if ("paste-from-clipboard".equals(id)) {
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            Transferable contents = sessionClipboard.getContents(null);
            ensure(contents != null, "CLIPBOARD_EMPTY");
            ensure(handler.importData(editor, contents), "CLIPBOARD_PASTE_UNVERIFIED");
            return;
        }
        boolean cut = "cut-to-clipboard".equals(id);
        ensure(!cut || editor.isEditable(), "EDITOR_READ_ONLY");
        int length = editor.getDocument().getLength();
        int selected = editor.getSelectionEnd() - editor.getSelectionStart();
        ensure(selected > 0, "CLIPBOARD_SELECTION_REQUIRED");
        Transferable before = sessionClipboard.getContents(null);
        handler.exportToClipboard(editor, sessionClipboard, cut ? TransferHandler.MOVE : TransferHandler.COPY);
        ensure(sessionClipboard.getContents(null) != before, "CLIPBOARD_COPY_UNVERIFIED");
        if (cut) {
            ensure(editor.getDocument().getLength() == length - selected, "CLIPBOARD_CUT_UNVERIFIED");
        }
    }

    private static void clearCharacterFormatting(JTextComponent editor) throws UiFailure {
        ensure(editor.isEditable(), "EDITOR_READ_ONLY");
        ensure(editor.getDocument() instanceof StyledDocument, "FORMAT_DOCUMENT_UNAVAILABLE");
        int end = editor.getSelectionEnd();
        StyledDocument document = (StyledDocument) editor.getDocument();
        Object[] characterStyles = { StyleConstants.FontFamily, StyleConstants.FontSize, StyleConstants.Bold,
            StyleConstants.Italic, StyleConstants.Underline, StyleConstants.StrikeThrough,
            StyleConstants.Subscript, StyleConstants.Superscript, StyleConstants.Foreground, StyleConstants.Background,
            StyleConstants.ResolveAttribute };
        // The 5.4.20 ribbon action is empty. Clear styles in the native model and preserve
        // semantic attributes, embedded elements and the exact document text.
        for (int start = editor.getSelectionStart(); start < end;) {
            Element run = document.getCharacterElement(start);
            int next = Math.min(end, run.getEndOffset());
            if ("content".equals(run.getName())) {
                SimpleAttributeSet plain = new SimpleAttributeSet(run.getAttributes());
                for (Object style : characterStyles) {
                    plain.removeAttribute(style);
                }
                document.setCharacterAttributes(start, next - start, plain, true);
            }
            start = next;
        }
    }

    private static javax.swing.text.Element elementAtCaret(JTextComponent editor, String name) {
        if (!(editor.getDocument() instanceof javax.swing.text.StyledDocument)) {
            return null;
        }
        javax.swing.text.Element element = ((javax.swing.text.StyledDocument) editor.getDocument())
            .getCharacterElement(editor.getCaretPosition());
        while (element != null && !name.equals(element.getName())) {
            element = element.getParentElement();
        }
        return element;
    }

    private static String[] reuseTemplateField(String name) throws Exception {
        ensure(templateMode, "TEMPLATE_SESSION_REQUIRED");
        ensure(!name.isEmpty() && name.length() <= 512, "TEMPLATE_FIELD_NAME_INVALID");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            ensure(editor.getSelectionStart() == editor.getSelectionEnd() && elementAtCaret(editor, "field") == null,
                "TEMPLATE_FIELD_INSERTION_POSITION_REQUIRED");
            tr.com.havelsan.uyap.system.editor.common.gui.oo context =
                (tr.com.havelsan.uyap.system.editor.common.gui.oo) javax.swing.SwingUtilities.getAncestorOfClass(
                    tr.com.havelsan.uyap.system.editor.common.gui.oo.class, editor);
            ensure(context != null, "TEMPLATE_CONTEXT_UNAVAILABLE");

            // UYAP overloads several zero-argument methods with the same obfuscated name but
            // different return types. The ArrayList overload is the field list used by its menu.
            java.lang.reflect.Method list = templateAccessor(
                tr.com.havelsan.uyap.system.editor.common.text.fi.class, ArrayList.class);
            java.lang.reflect.Method nameOf = templateAccessor(
                tr.com.havelsan.uyap.system.editor.common.text.fT.class, String.class);
            Object fields = list.invoke(editor);
            tr.com.havelsan.uyap.system.editor.common.text.fT field = null;
            if (fields instanceof List) {
                for (Object item : (List<?>) fields) {
                    if (item instanceof tr.com.havelsan.uyap.system.editor.common.text.fT && name.equals(nameOf.invoke(item))) {
                        field = (tr.com.havelsan.uyap.system.editor.common.text.fT) item;
                        break;
                    }
                }
            }
            if (field == null) {
                field = documentTemplateField(editor.getDocument(), name);
            }
            int before = editor.getDocument().getLength();
            context.a(field);
            int length = editor.getDocument().getLength();
            ensure(length > before, "TEMPLATE_FIELD_INSERTION_UNVERIFIED");
            return new String[] { Integer.toString(length) };
        });
    }

    private static tr.com.havelsan.uyap.system.editor.common.text.fT documentTemplateField(Document document, String name)
        throws UiFailure {
        // Local documents do not necessarily have a preloaded field list. Read the same four
        // model attributes that UYAP reads when opening the edit dialog for an existing field.
        java.util.ArrayDeque<javax.swing.text.Element> pending = new java.util.ArrayDeque<>();
        pending.add(document.getDefaultRootElement());
        List<Object> definition = null;
        while (!pending.isEmpty()) {
            javax.swing.text.Element item = pending.removeLast();
            for (int i = item.getElementCount() - 1; i >= 0; i--) {
                pending.add(item.getElement(i));
            }
            if (!"field".equals(item.getName())) {
                continue;
            }
            javax.swing.text.AttributeSet a = item.getAttributes();
            if (name.equals(a.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.V.g))) {
                List<Object> found = Arrays.asList(a.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.V.h),
                    a.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.V.i),
                    a.getAttribute(tr.com.havelsan.uyap.system.swing.wp.model.V.j));
                ensure(definition == null || definition.equals(found), "TEMPLATE_FIELD_AMBIGUOUS");
                definition = found;
            }
        }
        ensure(definition != null && definition.get(0) instanceof Integer, "TEMPLATE_FIELD_NOT_FOUND");
        tr.com.havelsan.uyap.system.editor.common.text.fT field =
            new tr.com.havelsan.uyap.system.editor.common.text.fT(name, (Integer) definition.get(0));
        if (definition.get(1) instanceof Boolean) {
            field.a((Boolean) definition.get(1));
        }
        if (definition.get(2) instanceof Boolean) {
            field.b((Boolean) definition.get(2));
        }
        return field;
    }

    private static java.lang.reflect.Method templateAccessor(Class<?> owner, Class<?> result) throws UiFailure {
        return Arrays.stream(owner.getMethods())
            .filter(method -> method.getName().equals("a") && method.getParameterCount() == 0
                && method.getReturnType() == result)
            .findFirst().orElseThrow(() -> new UiFailure("TEMPLATE_FIELD_LIST_UNAVAILABLE"));
    }

    private static String[] macro(String[] args) throws Exception {
        ensure(args.length >= 5 && args.length <= 12, "MACRO_STEPS_INVALID");
        int start = Integer.parseInt(args[0]);
        int end = Integer.parseInt(args[1]);
        int replayStart = Integer.parseInt(args[2]);
        int replayEnd = Integer.parseInt(args[3]);
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent editor = body();
            ensure(editor.isEditable(), "EDITOR_READ_ONLY");
            int length = editor.getDocument().getLength();
            ensure(start >= 0 && start < end && end <= length &&
                replayStart >= 0 && replayStart < replayEnd && replayEnd <= length, "TEXT_RANGE_INVALID");
            ensure(end <= replayStart || replayEnd <= start, "MACRO_RANGES_OVERLAP");
            List<Action> steps = new ArrayList<>();
            for (int i = 4; i < args.length; i++) {
                switch (args[i]) {
                    case "font-bold": case "font-italic": case "font-underline":
                    case "left-justify": case "right-justify": case "center-justify": case "both-justify":
                    case "changeCase-upper": case "changeCase-lower":
                        steps.add(editorAction(args[i]));
                        break;
                    default:
                        throw new UiFailure("MACRO_ACTION_UNAVAILABLE");
                }
            }
            Action begin = editor.getActionMap().get("macro-start-new");
            Action stop = editor.getActionMap().get("macro-stop");
            Action replay = editor.getActionMap().get("macro-runCurrent");
            ensure(begin != null && stop != null && replay != null, "MACRO_UNAVAILABLE");
            ensure(begin.isEnabled() && !stop.isEnabled(), "MACRO_ALREADY_RECORDING");
            selectRange(editor, start, end);
            fire(begin, "uyap-mcp-" + java.util.UUID.randomUUID().toString());
            try {
                ensure(stop.isEnabled(), "MACRO_START_UNVERIFIED");
                for (int i = 0; i < steps.size(); i++) {
                    fire(steps.get(i), args[i + 4]);
                }
            } finally {
                if (stop.isEnabled()) {
                    fire(stop, "macro-stop");
                }
            }
            ensure(begin.isEnabled() && !stop.isEnabled(), "MACRO_STOP_UNVERIFIED");
            int shift = replayStart >= end ? editor.getDocument().getLength() - length : 0;
            selectRange(editor, replayStart + shift, replayEnd + shift);
            ensure(replay.isEnabled(), "MACRO_REPLAY_UNAVAILABLE");
            fire(replay, "macro-runCurrent");
            return new String[] { Integer.toString(steps.size()), Integer.toString(editor.getSelectionStart()),
                Integer.toString(editor.getSelectionEnd()) };
        });
    }

    private static String[] pageRegions(Document document) throws Exception {
        List<String> result = new ArrayList<>();
        for (String name : new String[] { "header", "footer" }) {
            result.addAll(Arrays.asList(pageRegion(document, name)));
        }
        return result.toArray(new String[0]);
    }

    private static String[] pageRegion(Document document, String name) throws Exception {
        Element region = pageRegionElement(document, name);
        if (region == null) {
            return new String[] { name, "false", "-1", "-1", "" };
        }
        int start = region.getStartOffset();
        int end = Math.min(document.getLength(), region.getEndOffset() - 1);
        return new String[] { name, "true", Integer.toString(start), Integer.toString(end),
            document.getText(start, end - start) };
    }

    private static Element pageRegionElement(Document document, String name) {
        Element root = document.getDefaultRootElement();
        for (int i = 0; i < root.getElementCount(); i++) {
            Element child = root.getElement(i);
            if (name.equals(child.getName())) {
                return child;
            }
        }
        return null;
    }

    private static String[] setPageRegion(String requestedName, String value) throws Exception {
        ensure(requestedName == null || "header".equals(requestedName) || "footer".equals(requestedName),
            "PAGE_REGION_INVALID");
        ensure(value.length() <= 8192 && value.indexOf('\n') < 0 && value.indexOf('\r') < 0,
            "PAGE_REGION_TEXT_INVALID");
        return edt(() -> {
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            JTextComponent target = body();
            ensure(target.isEditable() && target instanceof tr.com.havelsan.uyap.system.editor.common.text.fi,
                "PAGE_REGION_UNAVAILABLE");
            tr.com.havelsan.uyap.system.editor.common.text.fi editor =
                (tr.com.havelsan.uyap.system.editor.common.text.fi) target;
            Document document = editor.getDocument();
            boolean combined = requestedName == null;
            // The combined native command edits the header first, then falls back to the footer.
            String name = combined ? (pageRegionElement(document, "header") == null ? "footer" : "header")
                : requestedName;
            Element region = pageRegionElement(document, name);
            if (region == null) {
                ensure(!combined, "PAGE_REGION_MISSING");
                String id = "insert-" + name;
                fire(editorAction(id), id);
                region = pageRegionElement(document, name);
                ensure(region != null, "PAGE_REGION_CREATION_UNVERIFIED");
            }
            // This class's package is shadowed by a sibling obfuscated class in Java source.
            Class<?> modes = Class.forName("tr.com.havelsan.uyap.system.swing.wp.a.f");
            int bodyMode = modes.getField("j").getInt(null);
            int regionMode = modes.getField("header".equals(name) ? "k" : "l").getInt(null);
            javax.swing.text.Position originalStart = combined ? document.createPosition(editor.getSelectionStart()) : null;
            javax.swing.text.Position originalEnd = combined ? document.createPosition(editor.getSelectionEnd()) : null;
            try {
                if ("footer".equals(name) && !value.isEmpty()
                    && region.getEndOffset() - region.getStartOffset() == 1) {
                    // An empty footer has only its structural newline. Restore the native
                    // placeholder before inserting so the new text stays inside the region.
                    region = reseedEmptyFooter(editor, region, bodyMode);
                }
                int start = region.getStartOffset();
                int end = Math.min(document.getLength(), region.getEndOffset() - 1);
                if (combined) {
                    FormActions.editRegion(editor, regionMode, start);
                } else {
                    editor.c(regionMode);
                }
                // Keep the first footer character in place until insertion has finished. Removing
                // the whole placeholder first can leave a stale leaf at the body/footer boundary.
                int prefix = 0;
                if ("footer".equals(name) && start < end && !value.isEmpty()) {
                    String current = document.getText(start, end - start);
                    prefix = Character.charCount(current.codePointAt(0));
                }
                replacePageRegionSelection(editor, start + prefix, end, value);
                if (prefix != 0) {
                    replacePageRegionSelection(editor, start, start + prefix, "");
                }
                String[] result = pageRegion(document, name);
                ensure("true".equals(result[1]) && result[4].equals(value), "PAGE_REGION_WRITE_UNVERIFIED");
                return result;
            } finally {
                editor.c(bodyMode);
                if (combined) {
                    selectRange(editor, originalStart.getOffset(), originalEnd.getOffset());
                }
            }
        });
    }

    private static Element reseedEmptyFooter(fi editor, Element region, int bodyMode) throws Exception {
        Element paragraph = region.getElement(0);
        ensure(editor.getDocument() instanceof DocumentEx && region.getElementCount() == 1
            && paragraph != null && paragraph.getElementCount() == 1
            && "content".equals(paragraph.getElement(0).getName()), "FOOTER_EMPTY_STRUCTURE_UNAVAILABLE");
        AttributeSet regionProperties = region.getAttributes().copyAttributes();
        AttributeSet paragraphStyle = paragraph.getAttributes().copyAttributes();
        DocumentEx document = (DocumentEx) editor.getDocument();
        editor.c(bodyMode);
        tr.com.havelsan.uyap.system.editor.common.text.hj nativeEditor = editor;
        ensure(nativeEditor.j() && nativeEditor.d(paragraphStyle), "FOOTER_PLACEHOLDER_UNAVAILABLE");
        document.b(regionProperties, true);
        Element footer = pageRegionElement(document, "footer");
        ensure(footer != null && footer.getEndOffset() - footer.getStartOffset() > 1,
            "FOOTER_PLACEHOLDER_UNAVAILABLE");
        return footer;
    }

    private static void replacePageRegionSelection(JTextComponent editor, int start, int end, String value)
        throws UiFailure {
        selectRange(editor, start, end);
        ensure(editor.getSelectionStart() == start && editor.getSelectionEnd() == end,
            "PAGE_REGION_SELECTION_UNVERIFIED");
        editor.replaceSelection(value);
    }

    private static String[] openForm(String id) throws Exception {
        ensure(!"edit-hf".equals(id), "USE_PAGE_REGION_ACTION");
        ensure(FORM_ACTIONS.contains(id), "FORM_ACTION_UNAVAILABLE");
        if ("show-all-annotation".equals(id)) {
            return showAllComments();
        }
        boolean search = "find".equals(id) || "replace".equals(id);
        List<Window> existing = edt(EditorLauncher::visibleWindows);
        Action action = edt(() -> {
            ensure(dialog(title -> true) == null && searchWindow() == null
                && (localDialog == null || !localDialog.isShowing()), "USER_ACTION_REQUIRED");
            if (FormActions.needsContext(id)) {
                FormActions.requireContext(id, body());
                return null;
            }
            return "divide-table-cell".equals(id) ? null : editorAction(id);
        });
        FutureTask<Void> event = new FutureTask<>(() -> {
            if ("divide-table-cell".equals(id)) {
                TableActions.divide(body());
            } else if (FormActions.needsContext(id)) {
                FormActions.open(id, body(), frame);
            } else if (AppearanceActions.isCapitalization(id)) {
                AppearanceActions.toggleCapitalization(body(), action, id);
            } else {
                fire(action, id);
            }
            return null;
        });
        EventQueue.invokeLater(event);
        return waitFor(() -> edt(() -> {
            Dialog visible = search ? searchWindow() : dialog(title -> true);
            if (visible == null) {
                visible = supplementalForm(id, existing);
            }
            if (visible == null) {
                if (!event.isDone()) {
                    return null;
                }
                event.get();
                return new String[] { "completed", "", "" };
            }
            ensure(nodes(visible, JFileChooser.class, item -> true).isEmpty(), "FILE_DIALOG_REQUIRES_DEDICATED_TOOL");
            localDialog = visible;
            localFormAction = id;
            return formState();
        }), "FORM_OPEN_UNVERIFIED");
    }

    private static List<Window> visibleWindows() {
        List<Window> visible = new ArrayList<>();
        for (Window window : Window.getWindows()) {
            if (window.isShowing()) {
                visible.add(window);
            }
        }
        return visible;
    }

    private static Dialog supplementalForm(String action, List<Window> existing) throws UiFailure {
        Dialog found = null;
        for (Window window : visibleWindows()) {
            if (!(window instanceof Dialog) || existing.contains(window)) {
                continue;
            }
            Dialog candidate = (Dialog) window;
            boolean matches = ("selection-word-info".equals(action) && "Kelime Özellikleri".equals(candidate.getTitle()))
                || ("info-selection".equals(action) && candidate.getOwner() == frame
                    && !nodes(candidate, JLabel.class, label -> "Pozisyon :".equals(textOf(label))).isEmpty())
                || ("prepareTable".equals(action) && candidate.getOwner() == frame && TableActions.isQuickTableDialog(candidate))
                || InterfaceActions.matchesForm(action, candidate, frame);
            if (matches) {
                ensure(found == null, "AMBIGUOUS_DIALOG");
                found = candidate;
            }
        }
        return found;
    }

    private static String[] formState() throws UiFailure {
        if (localDialog == null) {
            return new String[] { "closed", "", "" };
        }
        if (!localDialog.isShowing()) {
            Dialog continuation = dialog(title -> true);
            if (continuation != null && localDialog.getTitle().startsWith("Tablo Özellikleri")
                && continuation.getTitle().startsWith("Tablo Özellikleri")
                && nodes(continuation, JFileChooser.class, item -> true).isEmpty()) {
                localDialog = continuation;
            } else {
                ensure(continuation == null, "USER_ACTION_REQUIRED");
                boolean restoreFocus = "selection-word-info".equals(localFormAction) || "ini-designer".equals(localFormAction);
                localDialog = null;
                localFormAction = null;
                dialogControls = null;
                dialogControlsWindow = null;
                if (restoreFocus) {
                    frame.toFront();
                    frame.requestFocus();
                    body().requestFocusInWindow();
                }
                return new String[] { "closed", "", "" };
            }
        }
        Dialog active = formWindow();
        boolean readMessage = "Set Sign Libary".equals(localFormAction)
            || "info-selection".equals(localFormAction) || "selection-word-info".equals(localFormAction)
            || "show-signatures".equals(localFormAction) || InterfaceActions.isDiagnosticForm(localFormAction)
            || "logger-activator".equals(localFormAction) || AppearanceActions.isCapitalization(localFormAction);
        boolean font = active instanceof tr.com.havelsan.uyap.system.editor.common.gui.jm;
        List<Component> components = nodes(active, Component.class, item ->
            (readMessage && item instanceof JLabel && !textOf((JLabel) item).isEmpty())
            || item.isEnabled() && ((item instanceof JTextComponent && !(item instanceof JPasswordField))
            || item instanceof JComboBox || item instanceof JToggleButton || item instanceof AbstractButton
            || (font && item instanceof javax.swing.JList)
            || ("prepareTable".equals(localFormAction) && TableActions.isGridChoice(item))
            || ("show-elementTreePanel".equals(localFormAction) && item instanceof javax.swing.JTree)
            || isFormColorControl(item)));
        dialogControls = new ArrayList<>();
        dialogControlsWindow = active;
        List<String> result = new ArrayList<>(Arrays.asList("open", active.getTitle(),
            Long.toString(++dialogRevision)));
        for (Component component : components) {
            String role;
            String value;
            String options = "";
            String label = component.getAccessibleContext() == null ? "" :
                component.getAccessibleContext().getAccessibleName();
            if (component instanceof javax.swing.JTree) {
                role = "message";
                label = "Element tree";
                value = InterfaceActions.treeText((javax.swing.JTree) component);
            } else if ("prepareTable".equals(localFormAction) && TableActions.isGridChoice(component)) {
                role = "button";
                label = TableActions.gridChoiceLabel(component);
                value = "";
            } else if (readMessage && component instanceof JLabel) {
                role = "message";
                value = textOf((JLabel) component);
                label = "";
            } else if (isFormColorControl(component)) {
                role = "color";
                java.awt.Color color = readFormColor(component);
                value = String.format(java.util.Locale.ROOT, "#%06X", color.getRGB() & 0xffffff);
            } else if (component instanceof JComboBox) {
                JComboBox<?> combo = (JComboBox<?>) component;
                role = "choice";
                value = String.valueOf(combo.getSelectedItem());
                List<String> items = new ArrayList<>();
                for (int index = 0; index < Math.min(combo.getItemCount(), 100); index++) {
                    items.add(String.valueOf(combo.getItemAt(index)));
                }
                options = String.join("\u001f", items);
            } else if (component instanceof javax.swing.JList) {
                javax.swing.JList<?> list = (javax.swing.JList<?>) component;
                role = "choice";
                value = String.valueOf(list.getSelectedValue());
                List<String> items = new ArrayList<>();
                for (int index = 0; index < Math.min(list.getModel().getSize(), 500); index++) {
                    items.add(String.valueOf(list.getModel().getElementAt(index)));
                }
                options = String.join("\u001f", items);
            } else if (component instanceof JToggleButton) {
                role = "toggle";
                value = Boolean.toString(((JToggleButton) component).isSelected());
            } else if (component instanceof AbstractButton) {
                label = ((AbstractButton) component).getText();
                if (!formButtonAllowed(label)) {
                    continue;
                }
                role = "button";
                value = "";
            } else {
                JTextComponent text = (JTextComponent) component;
                if (readMessage && (text instanceof JEditorPane || !text.isEditable())) {
                    role = "message";
                    try {
                        value = text.getDocument().getText(0, Math.min(8192, text.getDocument().getLength()));
                    } catch (javax.swing.text.BadLocationException failure) {
                        throw new UiFailure("FORM_MESSAGE_UNAVAILABLE");
                    }
                } else {
                    if (!text.isEditable() || (text instanceof JEditorPane
                    && !"text/plain".equals(((JEditorPane) text).getContentType()))) {
                        continue;
                    }
                    role = "text";
                    value = text.getText();
                    if (value.length() > 8192) {
                        value = value.substring(0, 8192);
                    }
                }
            }
            result.add(Integer.toString(dialogControls.size()));
            result.add(role);
            result.add(label == null ? "" : label.trim());
            result.add(value);
            result.add(options);
            dialogControls.add(component);
        }
        return result.toArray(new String[0]);
    }

    private static String[] formInput(String revision, int index, String operation, String value) throws Exception {
        edt(() -> {
            ensure(localDialog != null && localDialog.isShowing() && dialogControls != null &&
                Long.toString(dialogRevision).equals(revision) && formWindow() == dialogControlsWindow,
                "STALE_DIALOG_STATE");
            ensure(!"Set Sign Libary".equals(localFormAction) || "cancel".equals(operation),
                "SIGNING_LIBRARY_READ_ONLY");
            if ("cancel".equals(operation)) {
                dialogControlsWindow.dispatchEvent(new WindowEvent(dialogControlsWindow, WindowEvent.WINDOW_CLOSING));
                if ("show-elementTreePanel".equals(localFormAction)) {
                    dialogControlsWindow.dispose();
                }
                return null;
            }
            ensure(index >= 0 && index < dialogControls.size(), "FORM_CONTROL_INVALID");
            Component item = dialogControls.get(index);
            ensure(item.isShowing() && item.isEnabled(), "FORM_CONTROL_CHANGED");
            switch (operation) {
                case "write":
                    ensure(item instanceof JTextComponent && !(item instanceof JPasswordField)
                        && ((JTextComponent) item).isEditable() && value.length() <= 8192, "FORM_VALUE_INVALID");
                    if (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.hb) {
                        ensure(value.matches("[0-9]{0,11}"), "BARCODE_VALUE_INVALID");
                    }
                    ((JTextComponent) item).setText(value);
                    if (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.hb) {
                        // This dialog refreshes its native length validation on key release.
                        java.awt.event.KeyEvent release = new java.awt.event.KeyEvent(item,
                            java.awt.event.KeyEvent.KEY_RELEASED, System.currentTimeMillis(), 0,
                            java.awt.event.KeyEvent.VK_UNDEFINED, java.awt.event.KeyEvent.CHAR_UNDEFINED);
                        for (java.awt.event.KeyListener listener : item.getKeyListeners()) {
                            listener.keyReleased(release);
                        }
                    }
                    break;
                case "choose":
                    if (dialogControlsWindow instanceof tr.com.havelsan.uyap.system.editor.common.gui.jm
                        && item instanceof javax.swing.JList) {
                        javax.swing.JList<?> list = (javax.swing.JList<?>) item;
                        for (int i = 0; i < list.getModel().getSize(); i++) {
                            if (value.equals(String.valueOf(list.getModel().getElementAt(i)))) {
                                list.setSelectedIndex(i);
                                return null;
                            }
                        }
                        throw new UiFailure("FORM_OPTION_UNKNOWN");
                    }
                    ensure(item instanceof JComboBox, "FORM_CONTROL_INVALID");
                    JComboBox<?> combo = (JComboBox<?>) item;
                    for (int i = 0; i < combo.getItemCount(); i++) {
                        if (value.equals(String.valueOf(combo.getItemAt(i)))) {
                            combo.setSelectedIndex(i);
                            return null;
                        }
                    }
                    throw new UiFailure("FORM_OPTION_UNKNOWN");
                case "toggle":
                    ensure(item instanceof JToggleButton && ("true".equals(value) || "false".equals(value)),
                        "FORM_VALUE_INVALID");
                    JToggleButton toggle = (JToggleButton) item;
                    boolean selected = Boolean.parseBoolean(value);
                    if (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.lh
                        || localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.jm) {
                        // These dialogs update their preview/model from the native action listener.
                        if (toggle.isSelected() != selected) {
                            toggle.doClick(0);
                            ensure(toggle.isSelected() == selected, "FORM_VALUE_INVALID");
                        }
                    } else {
                        toggle.setSelected(selected);
                    }
                    break;
                case "color":
                    ensure(isFormColorControl(item) && value.matches("#[0-9a-fA-F]{6}"), "FORM_VALUE_INVALID");
                    setFormColor(item, java.awt.Color.decode(value));
                    break;
                case "press":
                    if ("prepareTable".equals(localFormAction) && TableActions.isGridChoice(item)) {
                        TableActions.pressGridChoice(item);
                        break;
                    }
                    ensure(item instanceof AbstractButton && !(item instanceof JToggleButton)
                        && formButtonAllowed(((AbstractButton) item).getText()), "FORM_BUTTON_UNAVAILABLE");
                    final AbstractButton button = (AbstractButton) item;
                    if (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.jF
                        && "Tamam".equals(button.getText())) {
                        List<JTextField> fields = nodes(localDialog, JTextField.class, JTextField::isEditable);
                        ensure(fields.size() == 1 && FormActions.validLink(fields.get(0).getText()), "HYPERLINK_URL_INVALID");
                    }
                    EventQueue.invokeLater(() -> button.doClick(0));
                    break;
                default:
                    throw new UiFailure("FORM_OPERATION_UNKNOWN");
            }
            return null;
        });
        return edt(EditorLauncher::formState);
    }

    private static boolean formButtonAllowed(String label) {
        if (label == null) {
            return false;
        }
        String text = label.trim();
        return FORM_BUTTONS.contains(text) || (isTemplateColorDialog() && "Geri Al".equals(text))
            || ("edit-pageNumber".equals(localFormAction) && "Düzenle".equals(text))
            || (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.jc &&
                ("Sonrakini Bul".equals(text) || "Değiştir".equals(text) || "Hepsini Değiştir".equals(text)));
    }

    private static Dialog searchWindow() throws UiFailure {
        Dialog result = null;
        for (Window window : Window.getWindows()) {
            if (window instanceof tr.com.havelsan.uyap.system.editor.common.gui.jc
                && window.getOwner() == frame && window.isShowing()) {
                ensure(result == null, "AMBIGUOUS_DIALOG");
                result = (Dialog) window;
            }
        }
        return result;
    }

    private static Dialog formWindow() throws UiFailure {
        Dialog modal = dialog(title -> true);
        if (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.jc) {
            ensure(searchWindow() == localDialog, "USER_ACTION_REQUIRED");
            if (modal == null) {
                return localDialog;
            }
            ensure("Bilgi".equals(modal.getTitle()) && nodes(modal, javax.swing.JOptionPane.class,
                panel -> panel.getMessageType() == javax.swing.JOptionPane.INFORMATION_MESSAGE).size() == 1,
                "USER_ACTION_REQUIRED");
            return modal;
        }
        if ("ini-designer".equals(localFormAction) || "edit-pageNumber".equals(localFormAction)) {
            Dialog nested = null;
            for (Window window : visibleWindows()) {
                if (window.getOwner() == localDialog && window instanceof Dialog && ((Dialog) window).isModal()) {
                    Dialog candidate = (Dialog) window;
                    boolean allowed = ("ini-designer".equals(localFormAction) && "Stil Bilgisi".equals(candidate.getTitle()))
                        || ("edit-pageNumber".equals(localFormAction) && candidate.getTitle().startsWith("Sayfa Numarası"));
                    ensure(allowed && nested == null, "USER_ACTION_REQUIRED");
                    nested = candidate;
                }
            }
            if (nested != null) {
                return nested;
            }
        }
        if (modal == null && ("selection-word-info".equals(localFormAction)
            || "info-selection".equals(localFormAction) || "prepareTable".equals(localFormAction)
            || InterfaceActions.isDiagnosticForm(localFormAction))) {
            return localDialog;
        }
        ensure(modal == localDialog, "USER_ACTION_REQUIRED");
        return localDialog;
    }

    private static boolean isTemplateColorDialog() {
        return templateMode && localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.O;
    }

    private static boolean isFormColorControl(Component item) {
        return (isTemplateColorDialog() && item instanceof tr.com.havelsan.uyap.system.editor.common.gui.Y)
            || (localDialog instanceof tr.com.havelsan.uyap.system.editor.common.gui.em
                && item instanceof tr.com.havelsan.uyap.system.editor.common.gui.X);
    }

    private static java.awt.Color readFormColor(Component control) throws UiFailure {
        if (control instanceof tr.com.havelsan.uyap.system.editor.common.gui.Y) {
            return ((tr.com.havelsan.uyap.system.editor.common.gui.Y) control).a();
        }
        tr.com.havelsan.uyap.system.editor.common.text.gC property = paletteProperty(control);
        try {
            return new java.awt.Color(Integer.parseInt((String) commentLookup(
                tr.com.havelsan.uyap.system.editor.common.text.gC.class, "b", String.class).invoke(property)), true);
        } catch (ReflectiveOperationException | NumberFormatException failed) {
            throw new UiFailure("PALETTE_CONTROL_UNAVAILABLE");
        }
    }

    private static void setFormColor(Component control, java.awt.Color color) throws UiFailure {
        if (control instanceof tr.com.havelsan.uyap.system.editor.common.gui.Y) {
            ((tr.com.havelsan.uyap.system.editor.common.gui.Y) control).a(color);
            return;
        }
        tr.com.havelsan.uyap.system.editor.common.text.gC property = paletteProperty(control);
        // W is the value editor created by clicking the native Renkler swatch. Its commit updates
        // the shared editor preference and the same swatch is refreshed by the application's listener.
        tr.com.havelsan.uyap.system.editor.common.gui.W editor =
            new tr.com.havelsan.uyap.system.editor.common.gui.W();
        editor.a(property);
        editor.a(color);
        editor.c();
        ((tr.com.havelsan.uyap.system.editor.common.gui.X) control).a(property);
        ensure(color.equals(readFormColor(control)), "PALETTE_COLOR_UNVERIFIED");
    }

    private static tr.com.havelsan.uyap.system.editor.common.text.gC paletteProperty(Component swatch)
        throws UiFailure {
        Class<tr.com.havelsan.uyap.system.editor.common.text.gC> propertyType =
            tr.com.havelsan.uyap.system.editor.common.text.gC.class;
        for (java.awt.event.MouseListener listener : swatch.getMouseListeners()) {
            if ("tr.com.havelsan.uyap.system.editor.common.gui.b".equals(listener.getClass().getName())) {
                for (java.lang.reflect.Field field : listener.getClass().getDeclaredFields()) {
                    if (field.getType() == propertyType) {
                        try {
                            field.setAccessible(true);
                            return propertyType.cast(field.get(listener));
                        } catch (IllegalAccessException unavailable) {
                            throw new UiFailure("PALETTE_CONTROL_UNAVAILABLE");
                        }
                    }
                }
            }
        }
        throw new UiFailure("PALETTE_CONTROL_UNAVAILABLE");
    }

    private static void fire(Action action, String id) {
        try {
            JTextComponent target = body();
            action.actionPerformed(new ActionEvent(target, ActionEvent.ACTION_PERFORMED, id));
        } catch (UiFailure ex) {
            throw new IllegalStateException(ex);
        }
    }

    private static Action editorAction(String id) throws UiFailure {
        ensure(templateMode || !TEMPLATE_ACTIONS.contains(id), "TEMPLATE_SESSION_REQUIRED");
        for (Action action : body().getActions()) {
            if (id.equals(action.getValue(Action.NAME)) && action.isEnabled()) {
                return action;
            }
        }
        if (templateMode && body() instanceof JEditorPane) {
            javax.swing.text.EditorKit kit = ((JEditorPane) body()).getEditorKit();
            if (kit instanceof tr.com.havelsan.uyap.system.editor.common.text.gN) {
                tr.com.havelsan.uyap.system.editor.common.text.gN nativeKit =
                    (tr.com.havelsan.uyap.system.editor.common.text.gN) kit;
                switch (id) {
                    case "remove-field-group":
                        return new tr.com.havelsan.uyap.system.editor.common.text.gV(nativeKit);
                    case "remove-paragraph-field-group":
                        return new tr.com.havelsan.uyap.system.editor.common.text.gW(nativeKit);
                    case "template-color":
                        return new tr.com.havelsan.uyap.system.editor.common.text.hb(nativeKit);
                    default:
                        break;
                }
            }
        }
        throw new UiFailure("EDITOR_ACTION_UNAVAILABLE");
    }

    private static String[] actionNames() throws UiFailure {
        List<String> names = new ArrayList<>();
        for (Action action : body().getActions()) {
            Object name = action.getValue(Action.NAME);
            if (name instanceof String) {
                names.add((String) name);
            }
        }
        return names.toArray(new String[0]);
    }

    private static String[] editMode(String action) throws Exception {
        return edt(() -> {
            JTextComponent pane = body();
            boolean initialModel = pane.getDocument() == initialEditModel;
            if (!action.isEmpty()) {
                boolean writable = "set-writable".equals(action);
                ensure("set-read-only".equals(action) || writable, "EDIT_MODE_INVALID");
                ensure(initialModel, "EDIT_MODE_DOCUMENT_CHANGED");
                ensure(dialog(title -> true) == null && (localDialog == null || !localDialog.isShowing()),
                    "USER_ACTION_REQUIRED");
                if (pane.isEditable() != writable) {
                    ensure(!writable || editSuspended, "EDIT_MODE_RESTORE_UNAVAILABLE");
                    fire(editorAction(action), action);
                    ensure(pane.isEditable() == writable, "EDIT_MODE_UNVERIFIED");
                    editSuspended = !writable;
                }
            }
            return new String[] { Boolean.toString(pane.isEditable()),
                Boolean.toString(initialModel && (pane.isEditable() || editSuspended)) };
        });
    }

    private static String[] close() throws Exception {
        edt(() -> {
            ensure(!dirty, "UNSAVED_CHANGES");
            ensure(dialog(title -> true) == null, "USER_ACTION_REQUIRED");
            frame.dispatchEvent(new WindowEvent(frame, WindowEvent.WINDOW_CLOSING));
            return null;
        });
        waitFor(() -> edt(() -> !frame.isDisplayable() || !frame.isShowing() ? Boolean.TRUE : null),
            "CLOSE_UNVERIFIED");
        return new String[] { "closed" };
    }

    private static JTextComponent body() throws UiFailure {
        return single(frame, JTextComponent.class, EditorLauncher::isBody);
    }

    private static boolean isBody(JTextComponent component) {
        return component instanceof JEditorPane && "text/plain".equals(((JEditorPane) component).getContentType());
    }

    private static AbstractButton button(Container parent, String label) throws UiFailure {
        return single(parent, AbstractButton.class, button -> label.equals(button.getText()) && button.isEnabled());
    }

    private static Dialog dialog(Selector<String> title) throws UiFailure {
        Dialog match = null;
        for (Window window : Window.getWindows()) {
            if (window instanceof Dialog && window.isShowing() && window.getOwner() == frame) {
                Dialog candidate = (Dialog) window;
                if (candidate.isModal() && title.matches(candidate.getTitle())) {
                    ensure(match == null, "AMBIGUOUS_DIALOG");
                    match = candidate;
                }
            }
        }
        return match;
    }

    private static String textOf(JLabel label) {
        return label.getText() == null ? "" : label.getText().trim();
    }

    private static <T extends Component> T single(Container parent, Class<T> type, Selector<T> selector)
        throws UiFailure {
        List<T> matches = nodes(parent, type, selector);
        ensure(matches.size() == 1, matches.isEmpty() ? "EDITOR_CONTROL_MISSING" : "EDITOR_CONTROL_AMBIGUOUS");
        return matches.get(0);
    }

    private static <T extends Component> List<T> nodes(Container root, Class<T> type, Selector<T> selector) {
        List<T> matches = new ArrayList<>();
        collect(root, type, selector, matches);
        return matches;
    }

    private static <T extends Component> void collect(Component item, Class<T> type, Selector<T> selector,
        List<T> matches) {
        if (!item.isShowing()) {
            return;
        }
        if (type.isInstance(item)) {
            T match = type.cast(item);
            if (selector.matches(match)) {
                matches.add(match);
            }
        }
        if (item instanceof Container && !(item instanceof JTextComponent)) {
            for (Component child : ((Container) item).getComponents()) {
                collect(child, type, selector, matches);
            }
        }
    }

    private static <T> T edt(Callable<T> operation) throws Exception {
        FutureTask<T> task = new FutureTask<>(operation);
        EventQueue.invokeLater(task);
        return task.get(UI_TIMEOUT, java.util.concurrent.TimeUnit.MILLISECONDS);
    }

    private static <T> T waitFor(Callable<T> operation, String error) throws Exception {
        long limit = System.nanoTime() + UI_TIMEOUT * 1000000L;
        do {
            T result = operation.call();
            if (result != null) {
                return result;
            }
            Thread.sleep(75);
        } while (System.nanoTime() < limit);
        throw new UiFailure(error);
    }

    private static void ensure(boolean condition, String error) throws UiFailure {
        if (!condition) {
            throw new UiFailure(error);
        }
    }

    static final class UiFailure extends Exception {
        UiFailure(String code) {
            super(code);
        }
    }
}
