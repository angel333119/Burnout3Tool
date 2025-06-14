using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DXTTools
{
    /// <summary>
    /// Enumeração para os diferentes formatos de compressão DXT.
    /// </summary>
    public enum DXTFormat
    {
        DXT1,
        DXT3,
        DXT5
    }

    /// <summary>
    /// Classe estática para DECODIFICAR texturas nos formatos DXT.
    /// </summary>
    public static class DXTDecoder
    {
        /// <summary>
        /// Decodifica um array de bytes de dados DXT em um array de cores.
        /// </summary>
        public static Color[] DecodeDXT(byte[] dxtData, int width, int height, DXTFormat format)
        {
            Color[] pixels = new Color[width * height];

            using (var ms = new MemoryStream(dxtData))
            using (var reader = new BinaryReader(ms))
            {
                for (int y = 0; y < height; y += 4)
                {
                    for (int x = 0; x < width; x += 4)
                    {
                        Color[] block;
                        switch (format)
                        {
                            case DXTFormat.DXT1:
                                block = DecodeDXT1Block(reader);
                                break;
                            case DXTFormat.DXT3:
                                block = DecodeDXT3Block(reader);
                                break;
                            case DXTFormat.DXT5:
                                block = DecodeDXT5Block(reader);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(format), format, null);
                        }

                        for (int by = 0; by < 4; by++)
                        {
                            for (int bx = 0; bx < 4; bx++)
                            {
                                int dstX = x + bx;
                                int dstY = y + by;
                                if (dstX < width && dstY < height)
                                {
                                    pixels[dstY * width + dstX] = block[by * 4 + bx];
                                }
                            }
                        }
                    }
                }
            }
            return pixels;
        }

        private static Color[] DecodeDXT1Block(BinaryReader reader)
        {
            ushort color0_16 = reader.ReadUInt16();
            ushort color1_16 = reader.ReadUInt16();
            uint bits = reader.ReadUInt32();

            Color[] colors = new Color[4];
            colors[0] = RGB565ToColor(color0_16);
            colors[1] = RGB565ToColor(color1_16);

            if (color0_16 > color1_16)
            {
                colors[2] = Color.FromArgb(255, (2 * colors[0].R + colors[1].R) / 3, (2 * colors[0].G + colors[1].G) / 3, (2 * colors[0].B + colors[1].B) / 3);
                colors[3] = Color.FromArgb(255, (colors[0].R + 2 * colors[1].R) / 3, (colors[0].G + 2 * colors[1].G) / 3, (colors[0].B + 2 * colors[1].B) / 3);
            }
            else
            {
                colors[2] = Color.FromArgb(255, (colors[0].R + colors[1].R) / 2, (colors[0].G + colors[1].G) / 2, (colors[0].B + colors[1].B) / 2);
                colors[3] = Color.FromArgb(0, 0, 0, 0); // Transparente
            }

            Color[] result = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                int code = (int)((bits >> (2 * i)) & 0x03);
                result[i] = colors[code];
            }
            return result;
        }

        private static Color[] DecodeDXT3Block(BinaryReader reader)
        {
            ulong alphaBits = reader.ReadUInt64();
            Color[] colorBlock = DecodeDXT1Block(reader);

            Color[] result = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                byte alpha = (byte)(((alphaBits >> (i * 4)) & 0xF) * 17); // Expande 4-bit para 8-bit
                Color c = colorBlock[i];
                result[i] = Color.FromArgb(alpha, c.R, c.G, c.B);
            }
            return result;
        }

        private static Color[] DecodeDXT5Block(BinaryReader reader)
        {
            byte alpha0 = reader.ReadByte();
            byte alpha1 = reader.ReadByte();
            
            ulong alphaBits = 0;
            for (int i = 0; i < 6; i++)
                alphaBits |= ((ulong)reader.ReadByte()) << (8 * i);

            Color[] colorBlock = DecodeDXT1Block(reader);

            byte[] alphas = new byte[8];
            alphas[0] = alpha0;
            alphas[1] = alpha1;

            if (alpha0 > alpha1)
            {
                for (int i = 2; i < 8; i++)
                    alphas[i] = (byte)(((8 - i) * alpha0 + (i - 1) * alpha1) / 7);
            }
            else
            {
                for (int i = 2; i < 6; i++)
                    alphas[i] = (byte)(((6 - i) * alpha0 + (i - 1) * alpha1) / 5);
                alphas[6] = 0;
                alphas[7] = 255;
            }

            Color[] result = new Color[16];
            for (int i = 0; i < 16; i++)
            {
                int alphaCode = (int)((alphaBits >> (3 * i)) & 0x07);
                byte alpha = alphas[alphaCode];
                Color c = colorBlock[i];
                result[i] = Color.FromArgb(alpha, c.R, c.G, c.B);
            }
            return result;
        }

        private static Color RGB565ToColor(ushort colorValue)
        {
            int r = (colorValue >> 11) & 0x1F;
            int g = (colorValue >> 5) & 0x3F;
            int b = colorValue & 0x1F;
            
            r = (r << 3) | (r >> 2);
            g = (g << 2) | (g >> 4);
            b = (b << 3) | (b >> 2);
            
            return Color.FromArgb(255, r, g, b);
        }
    }

    /// <summary>
    /// Classe estática para CODIFICAR texturas para os formatos DXT.
    /// (VERSÃO CORRIGIDA E MELHORADA)
    /// </summary>
    public static class DXTEncoder
    {
        /// <summary>
        /// Codifica um array de cores para um array de bytes no formato DXT.
        /// </summary>
        public static byte[] EncodeDXT(Color[] pixels, int width, int height, DXTFormat format)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            Color[] block = new Color[16];
                            for (int by = 0; by < 4; by++)
                            {
                                for (int bx = 0; bx < 4; bx++)
                                {
                                    block[by * 4 + bx] = pixels[(y + by) * width + (x + bx)];
                                }
                            }

                            switch (format)
                            {
                                case DXTFormat.DXT1:
                                    EncodeDXT1Block(writer, block, true);
                                    break;
                                case DXTFormat.DXT3:
                                    EncodeDXT3Block(writer, block);
                                    break;
                                case DXTFormat.DXT5:
                                    EncodeDXT5Block(writer, block);
                                    break;
                            }
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        private static ushort ColorToRGB565(Color color)
        {
            return (ushort)(((color.R >> 3) << 11) | ((color.G >> 2) << 5) | (color.B >> 3));
        }

        private static void GetMinMaxColors(Color[] block, out Color min, out Color max)
        {
            min = Color.FromArgb(255, 255, 255);
            max = Color.FromArgb(0, 0, 0);

            foreach (Color c in block)
            {
                if (c.R < min.R) min = Color.FromArgb(c.R, min.G, min.B);
                if (c.G < min.G) min = Color.FromArgb(min.R, c.G, min.B);
                if (c.B < min.B) min = Color.FromArgb(min.R, min.G, c.B);

                if (c.R > max.R) max = Color.FromArgb(c.R, max.G, max.B);
                if (c.G > max.G) max = Color.FromArgb(max.R, c.G, max.B);
                if (c.B > max.B) max = Color.FromArgb(max.R, max.G, c.B);
            }
        }

        private static void EncodeDXT1Block(BinaryWriter writer, Color[] block, bool isDxt1)
        {
            GetMinMaxColors(block, out Color minColor, out Color maxColor);

            ushort c0_16 = ColorToRGB565(maxColor);
            ushort c1_16 = ColorToRGB565(minColor);

            if (c0_16 < c1_16)
            {
                (c0_16, c1_16) = (c1_16, c0_16);
                (minColor, maxColor) = (maxColor, minColor);
            }

            writer.Write(c0_16);
            writer.Write(c1_16);

            Color[] palette = new Color[4];
            palette[0] = maxColor;
            palette[1] = minColor;

            if (c0_16 > c1_16 || !isDxt1) // DXT3 e DXT5 sempre usam 4 cores
            {
                palette[2] = Color.FromArgb(255, (2 * palette[0].R + palette[1].R) / 3, (2 * palette[0].G + palette[1].G) / 3, (2 * palette[0].B + palette[1].B) / 3);
                palette[3] = Color.FromArgb(255, (palette[0].R + 2 * palette[1].R) / 3, (palette[0].G + 2 * palette[1].G) / 3, (palette[0].B + 2 * palette[1].B) / 3);
            }
            else // DXT1 com canal alfa
            {
                palette[2] = Color.FromArgb(255, (palette[0].R + palette[1].R) / 2, (palette[0].G + palette[1].G) / 2, (palette[0].B + palette[1].B) / 2);
                palette[3] = Color.FromArgb(0, 0, 0, 0); // Transparente
            }

            uint bits = 0;
            for (int i = 0; i < 16; i++)
            {
                int bestIndex = 0;
                int minDistance = int.MaxValue;
                for (int j = 0; j < 4; j++)
                {
                    int dist = (block[i].R - palette[j].R) * (block[i].R - palette[j].R) +
                               (block[i].G - palette[j].G) * (block[i].G - palette[j].G) +
                               (block[i].B - palette[j].B) * (block[i].B - palette[j].B);

                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestIndex = j;
                    }
                }
                bits |= (uint)(bestIndex << (i * 2));
            }
            writer.Write(bits);
        }

        private static void EncodeDXT3Block(BinaryWriter writer, Color[] block)
        {
            ulong alphaBits = 0;
            for (int i = 0; i < 16; i++)
            {
                byte alpha = (byte)(block[i].A >> 4);
                alphaBits |= (ulong)alpha << (i * 4);
            }
            writer.Write(alphaBits);
            EncodeDXT1Block(writer, block, false); // O `false` indica que não é DXT1 puro (não usa alfa)
        }

        private static void EncodeDXT5Block(BinaryWriter writer, Color[] block)
        {
            // 1. Encontrar min e max alfa no bloco
            byte alpha0 = 0;   // max
            byte alpha1 = 255; // min
            foreach (var pixel in block)
            {
                if (pixel.A > alpha0) alpha0 = pixel.A;
                if (pixel.A < alpha1) alpha1 = pixel.A;
            }

            writer.Write(alpha0);
            writer.Write(alpha1);

            // 2. Gerar a paleta de 8 valores de alfa
            byte[] alphaPalette = new byte[8];
            alphaPalette[0] = alpha0;
            alphaPalette[1] = alpha1;
            if (alpha0 > alpha1)
            {
                for (int i = 2; i < 8; i++) alphaPalette[i] = (byte)(((8 - i) * alpha0 + (i - 1) * alpha1) / 7);
            }
            else
            {
                for (int i = 2; i < 6; i++) alphaPalette[i] = (byte)(((6 - i) * alpha0 + (i - 1) * alpha1) / 5);
                alphaPalette[6] = 0;
                alphaPalette[7] = 255;
            }

            // 3. Encontrar o melhor índice para cada pixel e empacotar em 48 bits (6 bytes)
            ulong alphaBits = 0;
            for (int i = 0; i < 16; i++)
            {
                int bestIndex = 0;
                int minDistance = int.MaxValue;
                for (int j = 0; j < 8; j++)
                {
                    int dist = Math.Abs(block[i].A - alphaPalette[j]);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestIndex = j;
                    }
                }
                alphaBits |= (ulong)bestIndex << (i * 3);
            }

            // 4. ESCREVER OS 6 BYTES CORRETAMENTE
            writer.Write((byte)(alphaBits & 0xFF));
            writer.Write((byte)((alphaBits >> 8) & 0xFF));
            writer.Write((byte)((alphaBits >> 16) & 0xFF));
            writer.Write((byte)((alphaBits >> 24) & 0xFF));
            writer.Write((byte)((alphaBits >> 32) & 0xFF));
            writer.Write((byte)((alphaBits >> 40) & 0xFF));

            // 5. Codificar o bloco de cor
            EncodeDXT1Block(writer, block, false);
        }
    }
}