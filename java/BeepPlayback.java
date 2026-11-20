import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import javax.sound.sampled.AudioFormat;
import javax.sound.sampled.AudioSystem;
import javax.sound.sampled.Clip;
import javax.sound.sampled.LineEvent;
import javax.sound.sampled.LineUnavailableException;

final class BeepPlayback {
    private static final int SAMPLE_RATE = 44100;
    private static final int FRAME_COUNT = SAMPLE_RATE * 120 / 1000;
    private static final int FADE_FRAMES = SAMPLE_RATE / 100;

    private BeepPlayback() {}

    static String play() throws EditorLauncher.UiFailure, InterruptedException {
        AudioFormat format = new AudioFormat(SAMPLE_RATE, 16, 1, true, false);
        byte[] tone = tone();
        try (Clip clip = AudioSystem.getClip()) {
            CountDownLatch stopped = new CountDownLatch(1);
            clip.addLineListener(event -> {
                if (event.getType() == LineEvent.Type.STOP) {
                    stopped.countDown();
                }
            });
            clip.open(format, tone, 0, tone.length);
            clip.start();
            if (!stopped.await(2, TimeUnit.SECONDS)) {
                throw new EditorLauncher.UiFailure("BEEP_PLAYBACK_TIMEOUT");
            }
            long completed = clip.getLongFramePosition();
            if (completed < FRAME_COUNT) {
                throw new EditorLauncher.UiFailure("BEEP_PLAYBACK_INCOMPLETE");
            }
            return "{\"backend\":\"Java Sound Clip\",\"sampleRate\":" + SAMPLE_RATE
                + ",\"framesSubmitted\":" + FRAME_COUNT + ",\"framesPlayed\":" + completed + "}";
        } catch (LineUnavailableException | IllegalArgumentException | IllegalStateException | SecurityException failure) {
            throw new EditorLauncher.UiFailure("BEEP_PLAYBACK_UNAVAILABLE");
        }
    }

    private static byte[] tone() {
        byte[] pcm = new byte[FRAME_COUNT * 2];
        for (int frame = 0; frame < FRAME_COUNT; frame++) {
            double envelope = Math.min(1.0, Math.min(frame, FRAME_COUNT - 1 - frame) / (double) FADE_FRAMES);
            short sample = (short) (2600 * envelope * Math.sin(2 * Math.PI * 880 * frame / SAMPLE_RATE));
            pcm[frame * 2] = (byte) sample;
            pcm[frame * 2 + 1] = (byte) (sample >> 8);
        }
        return pcm;
    }
}
