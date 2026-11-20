import java.awt.AlphaComposite;
import java.awt.Color;
import java.awt.Font;
import java.awt.FontMetrics;
import java.awt.GradientPaint;
import java.awt.Graphics;
import java.awt.Graphics2D;
import java.awt.GraphicsConfiguration;
import java.awt.Image;
import java.awt.MultipleGradientPaint;
import java.awt.LinearGradientPaint;
import java.awt.RadialGradientPaint;
import java.awt.Shape;
import java.awt.font.TextLayout;
import java.awt.geom.AffineTransform;
import java.awt.geom.PathIterator;
import java.awt.geom.Point2D;
import java.awt.image.BufferedImage;
import java.awt.image.ImageObserver;
import java.awt.image.RenderedImage;
import java.awt.image.renderable.RenderableImage;
import java.awt.print.PageFormat;
import java.awt.print.Pageable;
import java.awt.print.Printable;
import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.BufferedOutputStream;
import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.lang.reflect.Method;
import java.text.AttributedCharacterIterator;
import java.util.Base64;
import java.util.LinkedHashMap;
import java.util.Map;
import javax.imageio.ImageIO;
import javax.xml.stream.XMLOutputFactory;
import javax.xml.stream.XMLStreamException;
import javax.xml.stream.XMLStreamWriter;
import org.apache.xmlgraphics.java2d.AbstractGraphics2D;
import org.apache.xmlgraphics.java2d.GraphicContext;
import tr.com.havelsan.uyap.system.editor.common.gui.WPDocumentPanel;

/** Serializes the editor's print drawing commands as SVG paths and original images. */
final class SvgExport {
    private static final String SVG = "http://www.w3.org/2000/svg";
    private static final String XLINK = "http://www.w3.org/1999/xlink";
    private static final long MAX_IMAGE_PIXELS = 30_000_000;

    private SvgExport() { }

    static int write(byte[] udf, Path output) throws Exception {
        WPDocumentPanel panel = new WPDocumentPanel();
        panel.l(true);
        try (ByteArrayInputStream input = new ByteArrayInputStream(udf)) {
            panel.a(input);
        }
        nativeMethod(panel, "a", void.class, int.class).invoke(panel, 0);
        nativeMethod(panel, "k", void.class).invoke(panel);
        Pageable pages = (Pageable) nativeMethod(panel, "a", Pageable.class).invoke(panel);
        nativeMethod(pages, "b", void.class, boolean.class).invoke(pages, true);
        nativeMethod(pages, "c", void.class, boolean.class).invoke(pages, false);
        int count = pages.getNumberOfPages();
        if (count < 1) throw new EditorLauncher.UiFailure("SVG_PAGE_LAYOUT_UNAVAILABLE");

        PageFormat[] formats = new PageFormat[count];
        double width = 0;
        double height = 0;
        for (int index = 0; index < count; index++) {
            PageFormat format = pages.getPageFormat(index);
            formats[index] = format;
            if (format.getWidth() <= 0 || format.getHeight() <= 0) {
                throw new EditorLauncher.UiFailure("SVG_PAGE_LAYOUT_UNAVAILABLE");
            }
            width = Math.max(width, format.getWidth());
            height += format.getHeight();
        }

        try (OutputStream stream = new BufferedOutputStream(
                Files.newOutputStream(output, StandardOpenOption.CREATE_NEW), 65536)) {
            XMLStreamWriter xml = XMLOutputFactory.newFactory().createXMLStreamWriter(stream, "UTF-8");
            try {
                xml.writeStartDocument("UTF-8", "1.0");
                xml.writeStartElement("svg");
                xml.writeDefaultNamespace(SVG);
                xml.writeNamespace("xlink", XLINK);
                xml.writeAttribute("width", number(width) + "pt");
                xml.writeAttribute("height", number(height) + "pt");
                xml.writeAttribute("viewBox", viewBox(width, height));
                double y = 0;
                for (int index = 0; index < count; index++) {
                    PageFormat page = formats[index];
                    xml.writeStartElement("svg");
                    xml.writeAttribute("id", "page-" + (index + 1));
                    xml.writeAttribute("y", number(y));
                    xml.writeAttribute("width", number(page.getWidth()));
                    xml.writeAttribute("height", number(page.getHeight()));
                    xml.writeAttribute("viewBox", viewBox(page.getWidth(), page.getHeight()));
                    xml.writeAttribute("overflow", "hidden");
                    SvgGraphics graphics = new SvgGraphics(new PageWriter(xml, index + 1));
                    graphics.setClip(0, 0, (int) Math.ceil(page.getWidth()), (int) Math.ceil(page.getHeight()));
                    if (pages.getPrintable(index).print(graphics, page, index) != Printable.PAGE_EXISTS) {
                        throw new EditorLauncher.UiFailure("SVG_PAGE_LAYOUT_UNAVAILABLE");
                    }
                    xml.writeEndElement();
                    y += page.getHeight();
                }
                xml.writeEndElement();
                xml.writeEndDocument();
            } finally {
                xml.close();
            }
        } catch (SvgFailure failure) {
            throw new EditorLauncher.UiFailure(failure.getMessage());
        }
        return count;
    }

    private static Method nativeMethod(Object owner, String name, Class<?> result, Class<?>... parameters)
        throws NoSuchMethodException {
        for (Method method : owner.getClass().getMethods()) {
            if (method.getName().equals(name) && method.getReturnType() == result
                && java.util.Arrays.equals(method.getParameterTypes(), parameters)) {
                return method;
            }
        }
        throw new NoSuchMethodException(owner.getClass().getName() + "." + name);
    }

    private static String viewBox(double width, double height) {
        return "0 0 " + number(width) + " " + number(height);
    }

    private static String number(double value) {
        if (!Double.isFinite(value)) throw new SvgFailure("SVG_COORDINATE_UNSUPPORTED");
        return Double.toString(value == 0 ? 0 : value);
    }

    private static String matrix(AffineTransform transform) {
        double[] values = new double[6];
        transform.getMatrix(values);
        StringBuilder out = new StringBuilder("matrix(");
        for (int i = 0; i < values.length; i++) {
            if (i > 0) out.append(' ');
            out.append(number(values[i]));
        }
        return out.append(')').toString();
    }

    private static String path(PathIterator segments) {
        double[] values = new double[6];
        StringBuilder out = new StringBuilder();
        while (!segments.isDone()) {
            int type = segments.currentSegment(values);
            out.append("MLQCZ".charAt(type));
            int coordinates = type == PathIterator.SEG_CUBICTO ? 6 : type == PathIterator.SEG_QUADTO ? 4
                : type == PathIterator.SEG_CLOSE ? 0 : 2;
            for (int i = 0; i < coordinates; i++) {
                out.append(' ').append(number(Math.rint(values[i] * 1000) / 1000));
            }
            out.append(' ');
            segments.next();
        }
        return out.toString();
    }

    private static String color(Color value) {
        String hex = Integer.toHexString(value.getRGB() | 0x1000000);
        return "#" + hex.substring(hex.length() - 6);
    }

    private static final class SvgFailure extends RuntimeException {
        SvgFailure(String code) { super(code); }
        SvgFailure(String code, Throwable cause) { super(code, cause); }
    }

    private static final class PageWriter {
        final XMLStreamWriter xml;
        final String prefix;
        final Map<String, String> clips = new LinkedHashMap<>();
        int definitions;

        PageWriter(XMLStreamWriter xml, int page) {
            this.xml = xml;
            prefix = "p" + page + "-";
        }

        String definition(String kind) {
            return prefix + kind + ++definitions;
        }
    }

    private static final class SvgGraphics extends AbstractGraphics2D {
        private final PageWriter page;

        SvgGraphics(PageWriter page) {
            super(true);
            this.page = page;
            this.gc = new GraphicContext();
        }

        private SvgGraphics(SvgGraphics original) {
            super(original);
            page = original.page;
        }

        @Override public Graphics create() { return new SvgGraphics(this); }
        @Override public void dispose() { }
        @Override public void setXORMode(Color color) { throw new SvgFailure("SVG_COMPOSITE_UNSUPPORTED"); }
        @Override public void copyArea(int x, int y, int w, int h, int dx, int dy) {
            throw new SvgFailure("SVG_COPY_AREA_UNSUPPORTED");
        }

        @Override public GraphicsConfiguration getDeviceConfiguration() {
            Graphics2D probe = new BufferedImage(1, 1, BufferedImage.TYPE_INT_ARGB).createGraphics();
            try {
                return probe.getDeviceConfiguration();
            } finally {
                probe.dispose();
            }
        }

        @Override public FontMetrics getFontMetrics(Font font) {
            Graphics2D probe = new BufferedImage(1, 1, BufferedImage.TYPE_INT_ARGB).createGraphics();
            try {
                probe.setRenderingHints(getRenderingHints());
                probe.setTransform(getTransform());
                return probe.getFontMetrics(font);
            } finally {
                probe.dispose();
            }
        }

        @Override public void draw(Shape shape) {
            fill(getStroke().createStrokedShape(shape));
        }

        @Override public void fill(Shape shape) {
            PathIterator segments = shape.getPathIterator(getTransform());
            if (segments.isDone()) return;
            boolean evenOdd = segments.getWindingRule() == PathIterator.WIND_EVEN_ODD;
            String outline = path(segments);
            try {
                String clip = clipReference();
                String paint = paintReference();
                XMLStreamWriter xml = page.xml;
                xml.writeEmptyElement("path");
                xml.writeAttribute("d", outline);
                xml.writeAttribute("fill", paint);
                if (evenOdd) xml.writeAttribute("fill-rule", "evenodd");
                if (clip != null) xml.writeAttribute("clip-path", clip);
                float alpha = opacity();
                if (getPaint() instanceof Color) alpha *= ((Color) getPaint()).getAlpha() / 255f;
                if (alpha < 1) xml.writeAttribute("fill-opacity", number(alpha));
            } catch (XMLStreamException failure) {
                throw new SvgFailure("SVG_OUTPUT_FAILED", failure);
            }
        }

        @Override public void drawString(String value, float x, float y) {
            if (!value.isEmpty()) new TextLayout(value, getFont(), getFontRenderContext()).draw(this, x, y);
        }

        @Override public void drawString(AttributedCharacterIterator value, float x, float y) {
            if (value.getBeginIndex() < value.getEndIndex()) {
                new TextLayout(value, getFontRenderContext()).draw(this, x, y);
            }
        }

        private float opacity() {
            if (!(getComposite() instanceof AlphaComposite)) throw new SvgFailure("SVG_COMPOSITE_UNSUPPORTED");
            AlphaComposite composite = (AlphaComposite) getComposite();
            if (composite.getRule() != AlphaComposite.SRC_OVER) {
                throw new SvgFailure("SVG_COMPOSITE_UNSUPPORTED");
            }
            return composite.getAlpha();
        }

        private String clipReference() throws XMLStreamException {
            Shape clip = getClip();
            if (clip == null) return null;
            PathIterator segments = clip.getPathIterator(getTransform());
            boolean evenOdd = segments.getWindingRule() == PathIterator.WIND_EVEN_ODD;
            String outline = path(segments);
            String key = (evenOdd ? "E" : "N") + outline;
            String id = page.clips.get(key);
            if (id == null) {
                id = page.definition("clip");
                page.clips.put(key, id);
                XMLStreamWriter xml = page.xml;
                xml.writeStartElement("defs");
                xml.writeStartElement("clipPath");
                xml.writeAttribute("id", id);
                xml.writeEmptyElement("path");
                xml.writeAttribute("d", outline);
                if (evenOdd) xml.writeAttribute("clip-rule", "evenodd");
                xml.writeEndElement();
                xml.writeEndElement();
            }
            return "url(#" + id + ")";
        }

        private String paintReference() throws XMLStreamException {
            if (getPaint() instanceof Color) return color((Color) getPaint());
            XMLStreamWriter xml = page.xml;
            String id = page.definition("gradient");
            xml.writeStartElement("defs");
            if (getPaint() instanceof GradientPaint) {
                GradientPaint paint = (GradientPaint) getPaint();
                openLinear(paint.getPoint1(), paint.getPoint2());
                gradientAttributes(id, getTransform(), paint.isCyclic() ? "reflect" : "pad", false);
                stop(0, paint.getColor1());
                stop(1, paint.getColor2());
            } else if (getPaint() instanceof MultipleGradientPaint) {
                MultipleGradientPaint paint = (MultipleGradientPaint) getPaint();
                if (paint instanceof LinearGradientPaint) {
                    LinearGradientPaint linear = (LinearGradientPaint) paint;
                    openLinear(linear.getStartPoint(), linear.getEndPoint());
                } else if (paint instanceof RadialGradientPaint) {
                    RadialGradientPaint radial = (RadialGradientPaint) paint;
                    xml.writeStartElement("radialGradient");
                    point("cx", "cy", radial.getCenterPoint());
                    point("fx", "fy", radial.getFocusPoint());
                    xml.writeAttribute("r", number(radial.getRadius()));
                } else {
                    throw new SvgFailure("SVG_PAINT_UNSUPPORTED");
                }
                AffineTransform transform = getTransform();
                transform.concatenate(paint.getTransform());
                MultipleGradientPaint.CycleMethod cycle = paint.getCycleMethod();
                String spread = cycle == MultipleGradientPaint.CycleMethod.REPEAT ? "repeat"
                    : cycle == MultipleGradientPaint.CycleMethod.REFLECT ? "reflect" : "pad";
                gradientAttributes(id, transform, spread,
                    paint.getColorSpace() == MultipleGradientPaint.ColorSpaceType.LINEAR_RGB);
                float[] fractions = paint.getFractions();
                Color[] colors = paint.getColors();
                for (int i = 0; i < fractions.length; i++) stop(fractions[i], colors[i]);
            } else {
                throw new SvgFailure("SVG_PAINT_UNSUPPORTED");
            }
            xml.writeEndElement();
            xml.writeEndElement();
            return "url(#" + id + ")";
        }

        private void openLinear(Point2D start, Point2D end) throws XMLStreamException {
            page.xml.writeStartElement("linearGradient");
            point("x1", "y1", start);
            point("x2", "y2", end);
        }

        private void point(String x, String y, Point2D point) throws XMLStreamException {
            page.xml.writeAttribute(x, number(point.getX()));
            page.xml.writeAttribute(y, number(point.getY()));
        }

        private void gradientAttributes(String id, AffineTransform transform, String spread, boolean linearRgb)
            throws XMLStreamException {
            XMLStreamWriter xml = page.xml;
            xml.writeAttribute("id", id);
            xml.writeAttribute("gradientUnits", "userSpaceOnUse");
            xml.writeAttribute("gradientTransform", matrix(transform));
            xml.writeAttribute("spreadMethod", spread);
            if (linearRgb) xml.writeAttribute("color-interpolation", "linearRGB");
        }

        private void stop(float offset, Color value) throws XMLStreamException {
            page.xml.writeEmptyElement("stop");
            page.xml.writeAttribute("offset", number(offset));
            page.xml.writeAttribute("stop-color", color(value));
            if (value.getAlpha() != 255) page.xml.writeAttribute("stop-opacity", number(value.getAlpha() / 255f));
        }

        @Override public boolean drawImage(Image image, int x, int y, ImageObserver observer) {
            return drawImage(image, x, y, image.getWidth(observer), image.getHeight(observer), observer);
        }

        @Override public boolean drawImage(Image image, int x, int y, int width, int height, ImageObserver observer) {
            int imageWidth = image.getWidth(observer);
            int imageHeight = image.getHeight(observer);
            if (imageWidth < 1 || imageHeight < 1) return false;
            AffineTransform transform = AffineTransform.getTranslateInstance(x, y);
            transform.scale((double) width / imageWidth, (double) height / imageHeight);
            return drawImage(image, transform, observer);
        }

        @Override public boolean drawImage(Image image, AffineTransform transform, ImageObserver observer) {
            int width = image.getWidth(observer);
            int height = image.getHeight(observer);
            if (width < 1 || height < 1) return false;
            validateImageSize(width, height);
            if (image instanceof BufferedImage) {
                writeImage((BufferedImage) image, transform);
            } else {
                BufferedImage copy = new BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB);
                Graphics2D graphics = copy.createGraphics();
                try {
                    if (!graphics.drawImage(image, 0, 0, observer)) return false;
                } finally {
                    graphics.dispose();
                }
                writeImage(copy, transform);
            }
            return true;
        }

        @Override public void drawRenderedImage(RenderedImage image, AffineTransform transform) {
            int width = image.getWidth();
            int height = image.getHeight();
            validateImageSize(width, height);
            BufferedImage copy = new BufferedImage(width, height, BufferedImage.TYPE_INT_ARGB);
            Graphics2D graphics = copy.createGraphics();
            try {
                graphics.drawRenderedImage(image, AffineTransform.getTranslateInstance(-image.getMinX(), -image.getMinY()));
            } finally {
                graphics.dispose();
            }
            AffineTransform placement = new AffineTransform(transform);
            placement.translate(image.getMinX(), image.getMinY());
            writeImage(copy, placement);
        }

        @Override public void drawRenderableImage(RenderableImage image, AffineTransform transform) {
            drawRenderedImage(image.createDefaultRendering(), transform);
        }

        private static void validateImageSize(int width, int height) {
            if (width < 1 || height < 1 || (long) width * height > MAX_IMAGE_PIXELS) {
                throw new SvgFailure("SVG_IMAGE_SIZE_UNSUPPORTED");
            }
        }

        private void writeImage(BufferedImage image, AffineTransform placement) {
            try {
                String clip = clipReference();
                AffineTransform transform = getTransform();
                transform.concatenate(placement);
                ByteArrayOutputStream png = new ByteArrayOutputStream();
                if (!ImageIO.write(image, "png", png)) throw new SvgFailure("SVG_IMAGE_ENCODING_FAILED");
                XMLStreamWriter xml = page.xml;
                if (clip != null) {
                    xml.writeStartElement("g");
                    xml.writeAttribute("clip-path", clip);
                }
                xml.writeEmptyElement("image");
                xml.writeAttribute("width", Integer.toString(image.getWidth()));
                xml.writeAttribute("height", Integer.toString(image.getHeight()));
                xml.writeAttribute("transform", matrix(transform));
                float alpha = opacity();
                if (alpha < 1) xml.writeAttribute("opacity", number(alpha));
                xml.writeAttribute("xlink", XLINK, "href",
                    "data:image/png;base64," + Base64.getEncoder().encodeToString(png.toByteArray()));
                if (clip != null) xml.writeEndElement();
            } catch (java.io.IOException | XMLStreamException failure) {
                throw new SvgFailure("SVG_OUTPUT_FAILED", failure);
            }
        }
    }
}
