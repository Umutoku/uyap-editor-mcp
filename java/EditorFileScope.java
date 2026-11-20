import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.InvalidPathException;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.Paths;

final class EditorFileScope {
    private static final String STATE_ROOT_PROPERTY = "uyap.mcp.stateRoot";
    private static final String OUTPUT_ROOT_PROPERTY = "uyap.mcp.outputRoot";
    private static final String INVALID_STAGING_PATH = "INVALID_STAGING_PATH";
    private static Path stateRoot;
    private static Path outputRoot;
    private static Path profile;

    private EditorFileScope() { }

    static void initialize() throws IOException {
        stateRoot = checkedRoot(Paths.get(System.getProperty(STATE_ROOT_PROPERTY)));
        outputRoot = checkedRoot(Paths.get(System.getProperty(OUTPUT_ROOT_PROPERTY)));
        profile = checkedRoot(Paths.get("").toAbsolutePath());
        if (profile.equals(stateRoot) || !profile.startsWith(stateRoot)) {
            throw new IOException("Editor working directory is outside the managed state root.");
        }
    }

    static Path profile() {
        return profile;
    }

    static File profileInput(String value, String error) throws EditorLauncher.UiFailure {
        return existingFile(value, profile, error);
    }

    static File comparisonInput(String value) throws EditorLauncher.UiFailure {
        final String error = "COMPARISON_INPUT_INVALID";
        Path folder = profile.getParent();
        if (folder == null || folder.equals(stateRoot) || !folder.startsWith(stateRoot)) {
            throw new EditorLauncher.UiFailure(error);
        }
        File source = existingFile(value, folder, error);
        if (!folder.equals(source.toPath().getParent())) {
            throw new EditorLauncher.UiFailure(error);
        }
        return source;
    }

    static File newOutput(String value) throws EditorLauncher.UiFailure {
        try {
            Path target = absolute(value, INVALID_STAGING_PATH);
            Path parent = target.getParent();
            if (parent == null || (!parent.startsWith(stateRoot) && !parent.startsWith(outputRoot))) {
                throw new EditorLauncher.UiFailure(INVALID_STAGING_PATH);
            }
            Path realParent = checkedRoot(parent);
            Path scoped = realParent.resolve(target.getFileName()).normalize();
            if ((!scoped.startsWith(stateRoot) && !scoped.startsWith(outputRoot))
                || Files.exists(scoped, LinkOption.NOFOLLOW_LINKS)) {
                throw new EditorLauncher.UiFailure(INVALID_STAGING_PATH);
            }
            return scoped.toFile();
        } catch (IOException | InvalidPathException failure) {
            throw new EditorLauncher.UiFailure(INVALID_STAGING_PATH);
        }
    }

    private static File existingFile(String value, Path root, String error) throws EditorLauncher.UiFailure {
        try {
            Path file = absolute(value, error);
            if (!file.startsWith(root) || !Files.isRegularFile(file, LinkOption.NOFOLLOW_LINKS)) {
                throw new EditorLauncher.UiFailure(error);
            }
            Path realFile = file.toRealPath();
            if (!realFile.startsWith(root) || !realFile.equals(file)) {
                throw new EditorLauncher.UiFailure(error);
            }
            return realFile.toFile();
        } catch (IOException | InvalidPathException failure) {
            throw new EditorLauncher.UiFailure(error);
        }
    }

    private static Path absolute(String value, String error) throws EditorLauncher.UiFailure {
        Path path = Paths.get(value);
        if (!path.isAbsolute() || !path.equals(path.normalize())) {
            throw new EditorLauncher.UiFailure(error);
        }
        return path;
    }

    private static Path checkedRoot(Path path) throws IOException {
        Path realPath = path.toRealPath();
        if (!path.equals(realPath) || !Files.isDirectory(realPath, LinkOption.NOFOLLOW_LINKS)) {
            throw new IOException("A managed directory redirects to a different location.");
        }
        return realPath;
    }
}
