using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Namicon
{
    public class NamiconGenerator
    {
        private const int DefaultRenderSizeFactor = 4;
        private const float DefaultFontToRenderSizeFactor = 2.5f;

        private static readonly Color DefaultTextColor = Color.Black;
        private static readonly StringFormat GenericTypo = StringFormat.GenericTypographic;
        private static readonly Regex SplitRegex = new Regex(@"\s+|['`´]", RegexOptions.ExplicitCapture);

        public NamiconGenerator(int outputSize = 100)
        {
            SetDefaultSize(outputSize);

            // more defaults
            FontFamily = "Consolas";
            Lightness = 0.7f;
            Saturation = 1.0f;
            Round = true;
            FallbackText = "";
        }

        public bool Round { get; set; }
        public int OutputSize { get; set; }
        public int RenderSize { get; set; }
        public float FontSize { get; set; }
        public string FontFamily { get; set; }
        public float Lightness { get; set; }
        public float Saturation { get; set; }
        public string FallbackText { get; set; }
        
        public virtual uint Hasher(string text)
        {
            return (uint) text.GetHashCode();
        }

        public void SetDefaultSize(int outputSize)
        {
            SetDefaultSize(outputSize, DefaultRenderSizeFactor*outputSize);
        }

        public void SetDefaultSize(int outputSize, int renderSize)
        {
            SetDefaultSize(outputSize, renderSize, renderSize/DefaultFontToRenderSizeFactor);
        }

        public void SetDefaultSize(int outputSize, int renderSize, float fontSize)
        {
            OutputSize = outputSize;
            RenderSize = renderSize;
            FontSize = fontSize;
        }

        public Color GetColorFromText(string text)
        {
            const float max = uint.MaxValue;

            uint hash = Hasher(text);
            float hue = hash/max;

            return HslToRgba(hue, Saturation, Lightness);
        }

        public Bitmap CreateImage(string name)
        {
            return CreateImageRaw(GetInitials(name), DefaultTextColor, GetColorFromText(name));
        }

        public Bitmap CreateImageRaw(string text)
        {
            return CreateImageRaw(text, DefaultTextColor, GetColorFromText(text));
        }

        public Bitmap CreateImageRaw(string text, Color textColor, Color backgroundColor)
        {
            if (text == null)
            {
                text = FallbackText;
            }

            var image = new Bitmap(RenderSize, RenderSize);

            using (Graphics g = CreateGraphics(image))
            {
                if (Round)
                {
                    using (var b = new SolidBrush(backgroundColor))
                    {
                        g.FillEllipse(b, 0, 0, image.Width, image.Height);
                    }
                }
                else
                {
                    g.Clear(backgroundColor);
                }

                using (var b = new SolidBrush(textColor))
                using (var f = new Font(FontFamily, FontSize))
                {
                    SizeF size = g.MeasureString(text, f, 0, GenericTypo);

                    float x = (image.Width - size.Width)/2;
                    float y = (image.Height - size.Height)/2;

                    g.DrawString(text, f, b, x, y, GenericTypo);
                }
            }

            return ResizeImage(image, OutputSize, OutputSize);
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = CreateGraphics(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static string GetInitials(string name)
        {
            if (name == null) throw new ArgumentNullException("name");

            name = name.Trim();

            if (name == string.Empty)
            {
                return null;
            }

            string[] parts = SplitRegex
                .Split(name)
                .Select(Clean)
                .Where(x => x.Length > 0)
                .ToArray();

            string initials;

            if (parts.Length == 0)
            {
                return null;
            }

            if (parts.Length == 1)
            {
                initials = parts[0].Substring(0, Math.Min(parts[0].Length, 2));
            }
            else
            {
                initials = "" + parts.First()[0] + parts.Last()[0];
            }

            return initials.ToUpperInvariant();
        }

        private static string Clean(string str)
        {
            var sb = new StringBuilder();

            foreach (char c in str)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);

                if (uc == UnicodeCategory.LowercaseLetter ||
                    uc == UnicodeCategory.UppercaseLetter ||
                    uc == UnicodeCategory.DecimalDigitNumber)
                    sb.Append(c);
            }

            return sb.ToString().Trim();
        }

        private static Graphics CreateGraphics(Image image)
        {
            Graphics graphics = Graphics.FromImage(image);
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            return graphics;
        }

        private static Color HslToRgba(float h, float s, float l, float a = 1.0f)
        {
            float r, g, b;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (s == 0.0f)
            {
                r = g = b = l;
            }
            else
            {
                float q = l < 0.5f ? l*(1.0f + s) : l + s - l*s;
                float p = 2.0f*l - q;
                r = HueToRgb(p, q, h + 1.0f/3.0f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1.0f/3.0f);
            }

            return Color.FromArgb((int) (a*255), (int) (r*255), (int) (g*255), (int) (b*255));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0.0f) t += 1.0f;
            if (t > 1.0f) t -= 1.0f;
            if (t < 1.0f/6.0f) return p + (q - p)*6.0f*t;
            if (t < 1.0f/2.0f) return q;
            if (t < 2.0f/3.0f) return p + (q - p)*(2.0f/3.0f - t)*6.0f;

            return p;
        }
    }
}