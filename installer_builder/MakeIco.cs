using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public class MakeIco
{
    public static void Main(string[] args)
    {
        string pngPath = args.Length > 0 ? args[0] : "c:\\laragon\\www\\sae-feeder\\sae-logo.png";
        string icoPath = args.Length > 1 ? args[1] : "c:\\laragon\\www\\sae-feeder\\installer_builder\\sae-feeder.ico";

        using (Bitmap srcBmp = new Bitmap(pngPath))
        {
            int[] sizes = new int[] { 16, 32, 48, 64, 128, 256 };
            byte[][] pngBuffers = new byte[sizes.Length][];

            for (int i = 0; i < sizes.Length; i++)
            {
                int sz = sizes[i];
                using (Bitmap resized = new Bitmap(sz, sz))
                {
                    using (Graphics g = Graphics.FromImage(resized))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.Clear(Color.Transparent);

                        // Keep aspect ratio
                        float ratio = Math.Min((float)sz / srcBmp.Width, (float)sz / srcBmp.Height);
                        int w = (int)(srcBmp.Width * ratio);
                        int h = (int)(srcBmp.Height * ratio);
                        int x = (sz - w) / 2;
                        int y = (sz - h) / 2;

                        g.DrawImage(srcBmp, x, y, w, h);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        resized.Save(ms, ImageFormat.Png);
                        pngBuffers[i] = ms.ToArray();
                    }
                }
            }

            // Write multi-res ICO file
            using (FileStream fs = new FileStream(icoPath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // ICONDIR
                bw.Write((short)0); // Reserved
                bw.Write((short)1); // Type: 1 = ICO
                bw.Write((short)sizes.Length); // Count

                int offset = 6 + (16 * sizes.Length);

                for (int i = 0; i < sizes.Length; i++)
                {
                    int sz = sizes[i];
                    byte bSz = sz >= 256 ? (byte)0 : (byte)sz;

                    bw.Write(bSz); // Width
                    bw.Write(bSz); // Height
                    bw.Write((byte)0); // Color count
                    bw.Write((byte)0); // Reserved
                    bw.Write((short)1); // Color planes
                    bw.Write((short)32); // Bits per pixel
                    bw.Write((int)pngBuffers[i].Length); // Size in bytes
                    bw.Write((int)offset); // Offset

                    offset += pngBuffers[i].Length;
                }

                for (int i = 0; i < sizes.Length; i++)
                {
                    bw.Write(pngBuffers[i]);
                }
            }
        }

        Console.WriteLine("Successfully created valid multi-resolution ICO at " + icoPath);
    }
}
