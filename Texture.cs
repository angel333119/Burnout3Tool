using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DXTTools;

namespace Burnout3EI
{
    public partial class Texture : Form
    {
        private List<List<TxdTexture>> texturesPerFile = new List<List<TxdTexture>>();
        private List<string> openFilePaths = new List<string>();
        private static readonly int[] _Interlace = { 0x00, 0x10, 0x02, 0x12, 0x11, 0x01, 0x13, 0x03 };
        private static readonly int[] _Matrix = { 0, 1, -1, 0 };
        private static readonly int[] _Tile = { 4, -4 };

        private Image originalImage;
        private double zoomFactor = 1.0;
        private int zoomSteps = 0;

        private int currentPaletteIndex = 0;
        private TxdTexture currentTexture;

        private ContextMenuStrip menuDeContexto;

        public Texture()
        {
            InitializeComponent();

            ConfigurarMenuDeContexto();
            pictureBoxDisplay.ContextMenuStrip = menuDeContexto;
            pictureBoxDisplay.BackColor = Color.Transparent;

            comboBoxBinFiles.SelectedIndexChanged += ComboBoxBinFiles_SelectedIndexChanged;
            comboBoxImages.SelectedIndexChanged += ComboBoxImages_SelectedIndexChanged;

            //Botão de Zoom
            btnZoomIn.Click += btnZoomIn_Click;
            btnZoomOut.Click += btnZoomOut_Click;
        }

        private Bitmap CriarFundoQuadriculado(int largura, int altura, int tamanhoQuadrado)
        {
            Bitmap fundo = new Bitmap(largura, altura);
            using (Graphics g = Graphics.FromImage(fundo))
            {
                Color cor1 = Color.LightGray;
                Color cor2 = Color.White;
                for (int y = 0; y < altura; y += tamanhoQuadrado)
                    for (int x = 0; x < largura; x += tamanhoQuadrado)
                        using (var brush = new SolidBrush(((x / tamanhoQuadrado + y / tamanhoQuadrado) % 2 == 0) ? cor1 : cor2))
                            g.FillRectangle(brush, x, y, tamanhoQuadrado, tamanhoQuadrado);
            }
            return fundo;
        }

        private void buttonAbrirArquivos_Click(object sender, EventArgs e)
        {
            comboBoxBinFiles.Items.Clear();
            comboBoxImages.Items.Clear();
            texturesPerFile.Clear();
            pictureBoxDisplay.Image = null;
            openFilePaths.Clear();
            pictureBoxDisplay.BackgroundImage = null;

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Burnout 3 TXD|*.TXD|Todos|*.*";
                ofd.Multiselect = true;
                if (ofd.ShowDialog() != DialogResult.OK) return;
                
                var quad = CriarFundoQuadriculado(pictureBoxDisplay.Width, pictureBoxDisplay.Height, 10);
                pictureBoxDisplay.BackgroundImage = quad;
                pictureBoxDisplay.BackgroundImageLayout = ImageLayout.Tile;

                foreach (var path in ofd.FileNames)
                {
                    comboBoxBinFiles.Items.Add(Path.GetFileName(path));
                    openFilePaths.Add(path);
                    texturesPerFile.Add(CarregaTexturasDoArquivo(path));
                }

                // ** aqui selecionamos automaticamente o primeiro TXD **
                if (comboBoxBinFiles.Items.Count > 0)
                    comboBoxBinFiles.SelectedIndex = 0;
            }
        }
        private List<TxdTexture> CarregaTexturasDoArquivo(string path)
        {
            var lista = new List<TxdTexture>();
            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var br = new BinaryReader(fs))
            {
                br.BaseStream.Seek(0x08, SeekOrigin.Begin);
                int count = br.ReadInt32();
                int table = br.ReadInt32();

                var offsets = new long[count];
                var ids = new long[count];
                br.BaseStream.Seek(table, SeekOrigin.Begin);
                for (int i = 0; i < count; i++)
                {
                    ids[i] = br.ReadInt64();
                    offsets[i] = br.ReadInt64();
                }

                for (int i = 0; i < count; i++)
                {
                    br.BaseStream.Seek(offsets[i], SeekOrigin.Begin);
                    int console = br.ReadInt32(); // Determina o console

                    if (console == 0x00040001) // Xbox
                    {
                        int relImg = br.ReadInt32();

                        br.BaseStream.Seek(offsets[i] + 0x34, SeekOrigin.Begin);
                        int formato = br.ReadInt32();
                        int w = br.ReadInt32();
                        int h = br.ReadInt32();
                        int bpp = br.ReadInt32();

                        br.BaseStream.Seek(0x04, SeekOrigin.Current);
                        string nome = Encoding.Default.GetString(br.ReadBytes(8)).TrimEnd('\0');

                        if (formato == 0xB) // Paletada (Xbox)
                        {
                            // 1. Lê os pixels indexados (swizzled)
                            br.BaseStream.Seek(offsets[i] + relImg, SeekOrigin.Begin);
                            // Para 4bpp, cada byte contém 2 pixels. Para 8bpp, cada byte é um pixel.
                            int rawSize = (bpp == 4) ? (w * h) / 2 : (w * h);
                            byte[] rawPix = br.ReadBytes(rawSize);

                            // 2. Desswizzle (reorganização dos índices para a ordem linear)
                            byte[] indices = (bpp == 4) ? Unswizzle4bppXBOX(rawPix, w, h) : Unswizzle8bppXBOX(rawPix, w, h);

                            // 3. Leitura da estrutura de paleta
                            br.BaseStream.Seek(offsets[i] + 0x14, SeekOrigin.Begin);
                            uint palPointerOffset = br.ReadUInt32();
                            long palStructAbs = offsets[i] + palPointerOffset;

                            br.BaseStream.Seek(palStructAbs, SeekOrigin.Begin);
                            byte[] palHeader = br.ReadBytes(4);

                            if (!(palHeader[0] == 0x01 && palHeader[1] == 0x00 && palHeader[2] == 0x03 &&
                                  (palHeader[3] == 0x00 || palHeader[3] == 0xC0)))
                            {
                                // Se o cabeçalho não for o esperado, podemos pular esta textura para evitar erros.
                                // O ideal seria logar um aviso.
                                Console.WriteLine($"Cabeçalho de paleta inválido no offset {palStructAbs:X} para a textura {nome}. Pulando.");
                                continue;
                            }

                            uint palDataRelOffset = br.ReadUInt32();
                            long palDataAbs = offsets[i] + palDataRelOffset;

                            // 4. Leitura dos dados reais da paleta
                            br.BaseStream.Seek(palDataAbs, SeekOrigin.Begin);
                            int colorCount = (bpp == 4) ? 16 : 256;
                            byte[] palData = br.ReadBytes(colorCount * 4);

                            if (palData.Length < colorCount * 4)
                            {
                                Console.WriteLine($"Paleta incompleta para a textura {nome}: esperava {colorCount * 4} bytes, leu {palData.Length}.");
                                continue; // Pula texturas com dados de paleta corrompidos/incompletos
                            }

                            // CORREÇÃO 2: Remover a chamada ao SwapPalette.
                            // A função SwapPalette é para o formato do PS2, o Xbox não precisa disso.
                            // byte[] palette = SwapPalette(palData); // <-- Linha REMOVIDA
                            byte[] palette = palData; // <-- Usar os dados da paleta diretamente

                            Bitmap bmp = ApplyPaletteToBitmap(indices, palette, w, h, bpp);

                            lista.Add(new TxdTexture
                            {
                                Name = $"{ids[i]} - {(bpp == 4 ? "4bpp" : "8bpp")} - {nome}",
                                Width = w,
                                Height = h,
                                BitsPerPixel = 32, // O bitmap final é 32bpp
                                DirectBitmap = bmp,
                                Palettes = new List<byte[]> { palette }, // Armazena a paleta caso precise dela depois
                                TextureOffset = offsets[i] + relImg,
                                PaletteOffset = palDataAbs
                            });
                        }

                        else if (formato == 0xC || formato == 0xE || formato == 0xF) // DXT1/3/5
                        {
                            br.BaseStream.Seek(offsets[i] + relImg, SeekOrigin.Begin);
                            int blockSize = (formato == 0xC) ? 8 : 16;
                            int dataSize = ((w + 3) / 4) * ((h + 3) / 4) * blockSize;
                            byte[] data = br.ReadBytes(dataSize);

                            var dxtFmt = (formato == 0xC) ? DXTFormat.DXT1 :
                                         (formato == 0xE) ? DXTFormat.DXT3 : DXTFormat.DXT5;
                            var pixels = DXTDecoder.DecodeDXT(data, w, h, dxtFmt);

                            string formato_dxt = "";

                            if (formato == 0xC)
                            {
                                formato_dxt = "1";
                            }
                            if (formato == 0xE)
                            {
                                formato_dxt = "3";
                            }
                            if (formato == 0xF)
                            {
                                formato_dxt = "5";
                            }

                            long texOffset = offsets[i] + relImg;

                            var bmp = CreateBitmapFromPixels(pixels, w, h);
                            lista.Add(new TxdTexture
                            {
                                Name = $"{ids[i]} - DXT{formato_dxt} - {nome}",
                                Width = w,
                                Height = h,
                                BitsPerPixel = 32,
                                DirectBitmap = bmp,
                                Palettes = new List<byte[]>(),
                                TextureOffset = texOffset,
                                PaletteOffset = 0,
                                OriginalDxtFormat = dxtFmt
                            });
                        }
                        else if (formato == 0x3A) // RGBA
                        {
                            br.BaseStream.Seek(offsets[i] + relImg, SeekOrigin.Begin);
                            byte[] rgba = br.ReadBytes(w * h * 4);

                            Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                            var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, bmp.PixelFormat);
                            System.Runtime.InteropServices.Marshal.Copy(rgba, 0, bd.Scan0, rgba.Length);
                            bmp.UnlockBits(bd);

                            long texOffset = offsets[i] + relImg;
                            
                            lista.Add(new TxdTexture
                            {
                                Name = $"{ids[i]} - RGBA_{nome}",
                                Width = w,
                                Height = h,
                                BitsPerPixel = 32,
                                DirectBitmap = bmp,
                                Palettes = new List<byte[]>(),
                                TextureOffset = texOffset,
                                PaletteOffset = 0
                            });
                        }
                    }
                    else if (console == 0) //PS2
                    {
                        int relImg = br.ReadInt32();
                        int relPal = br.ReadInt32();
                        int w = br.ReadInt32();
                        int h = br.ReadInt32();
                        int bpp = br.ReadInt32();

                        br.BaseStream.Seek(offsets[i] + 0xA0, SeekOrigin.Begin);
                        int palCount = br.ReadByte();
                        br.BaseStream.Seek(offsets[i] + 0xA8, SeekOrigin.Begin);
                        string nome = Encoding.Default
                            .GetString(br.ReadBytes(8))
                            .TrimEnd('\0');

                        if (w <= 0 || h <= 0 || (bpp != 4 && bpp != 8) || palCount < 1)
                            continue;

                        int rawSize = (bpp == 4) ? w * h / 2 : w * h;
                        int colorsPerPalette = (bpp == 4) ? 16 : 256;
                        int palSize = colorsPerPalette * 4;
                        long palOffset = offsets[i] + relPal;
                        long endimag = offsets[i] + relImg;

                        // 1) lê bloco inteiro de paletas (espera palCount * palSize bytes)
                        br.BaseStream.Seek(palOffset, SeekOrigin.Begin);
                        byte[] rawAll = br.ReadBytes(palCount * palSize);

                        // 2) tenta desentrelaçar; em caso de erro, faz fallback sequencial
                        byte[][] deinterleaved;
                        try
                        {
                            deinterleaved = DeinterleavePalettes(rawAll, palCount, colorsPerPalette);
                        }
                        catch (ArgumentException)
                        {
                            // Fallback: assume paletas não intercaladas – só fatia rawAll em palCount blocos
                            deinterleaved = new byte[palCount][];
                            for (int j = 0; j < palCount; j++)
                            {
                                int start = j * palSize;
                                int available = Math.Max(0, Math.Min(palSize, rawAll.Length - start));
                                var slice = new byte[palSize];
                                // copia o que tiver, deixa o resto com zeros
                                if (available > 0)
                                    Array.Copy(rawAll, start, slice, 0, available);
                                deinterleaved[j] = slice;
                            }
                        }

                        // 3) swap BGRA→RGBA
                        var palettes = deinterleaved
                            .Select(p => SwapPalette(p))
                            .ToList();

                        // 4) lê pixels
                        br.BaseStream.Seek(offsets[i] + relImg, SeekOrigin.Begin);
                        byte[] pix = br.ReadBytes(rawSize);

                        lista.Add(new TxdTexture
                        {
                            Name = $"{ids[i]} - {nome}",
                            Width = w,
                            Height = h,
                            BitsPerPixel = bpp,
                            PixelIndices = pix,
                            Palettes = palettes,
                            TextureOffset = endimag,
                            PaletteOffset = palOffset
                        });
                    }
                }
            }

            return lista;
        }
        private static Bitmap ApplyPaletteToBitmap(byte[] indices, byte[] palette, int width, int height, int bpp)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bd = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmp.PixelFormat);

            int stride = Math.Abs(bd.Stride);
            byte[] raw = new byte[stride * height];

            int paletteColors = palette.Length / 4;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = y * width + x;

                    // --- 1) Calcula o índice de paleta ---
                    int idx;
                    if (pos >= indices.Length)
                    {
                        idx = 0; // Se o índice não existe, usa cor 0
                    }
                    else if (bpp == 4)
                    {
                        int packed = indices[pos >> 1];
                        bool hi = ((x & 1) == 0);
                        idx = hi ? (packed >> 4) : (packed & 0x0F);
                    }
                    else
                    {
                        idx = indices[pos];
                    }

                    // --- 2) Garante que o índice seja válido ---
                    if (idx < 0 || idx >= paletteColors)
                        idx = 0;

                    int pi = idx * 4;
                    int basePos = y * stride + x * 4;

                    // --- 3) Protege contra paleta incompleta ---
                    if (pi + 3 >= palette.Length)
                    {
                        // Cor padrão de erro visual (rosa choque)
                        raw[basePos + 0] = 255; // B
                        raw[basePos + 1] = 0;   // G
                        raw[basePos + 2] = 255; // R
                        raw[basePos + 3] = 255; // A
                    }
                    else
                    {
                        raw[basePos + 0] = palette[pi + 0]; // B
                        raw[basePos + 1] = palette[pi + 1]; // G
                        raw[basePos + 2] = palette[pi + 2]; // R
                        raw[basePos + 3] = palette[pi + 3]; // A
                    }
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(raw, 0, bd.Scan0, raw.Length);
            bmp.UnlockBits(bd);
            return bmp;
        }

        private static Bitmap CreateBitmapFromPixels(Color[] pixels, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var bd = bmp.LockBits(new Rectangle(0, 0, width, height),
                                   ImageLockMode.WriteOnly, bmp.PixelFormat);
            int stride = Math.Abs(bd.Stride);
            byte[] raw = new byte[stride * height];

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[y * width + x];
                    int pos = y * stride + x * 4;
                    raw[pos + 0] = c.B;
                    raw[pos + 1] = c.G;
                    raw[pos + 2] = c.R;
                    raw[pos + 3] = c.A;
                }

            System.Runtime.InteropServices.Marshal.Copy(raw, 0, bd.Scan0, raw.Length);
            bmp.UnlockBits(bd);
            return bmp;
        }
        private void ComboBoxBinFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBoxImages.Items.Clear();
            pictureBoxDisplay.Image = null;
            comboBoxBinFiles.Enabled = true;
            btnZoomIn.Enabled = true;
            btnZoomOut.Enabled = true;
            int idx = comboBoxBinFiles.SelectedIndex;
            if (idx < 0 || idx >= texturesPerFile.Count) return;

            foreach (var t in texturesPerFile[idx])
                comboBoxImages.Items.Add(t.Name);

            if (comboBoxImages.Items.Count > 0)
            {
                comboBoxImages.Enabled = true;
                comboBoxImages.SelectedIndex = 0;
            }
        }

        private void ComboBoxImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int fi = comboBoxBinFiles.SelectedIndex;
            int ti = comboBoxImages.SelectedIndex;
            if (fi < 0 || ti < 0) return;

            currentTexture = texturesPerFile[fi][ti];
            currentPaletteIndex = 0;
            if (currentTexture.DirectBitmap != null)
                originalImage = currentTexture.DirectBitmap;
            else
                originalImage = CreateBitmapFromData(currentTexture, currentPaletteIndex);
            zoomSteps = 0;
            AtualizarZoomFactor();
            ApplyZoom();

            AtualizarBotoesPaleta();

            Enderecotextura.Text = $"Texture address: {currentTexture.TextureOffset}";
            paleta.Text = $"Palette address: {currentTexture.PaletteOffset}";
            Resolucao.Text = $"Resolution: {currentTexture.Width}x{currentTexture.Height}";
        }

        private Bitmap CreateBitmapFromData(TxdTexture tex, int paletteIndex)
        {
            var bmp = new Bitmap(tex.Width, tex.Height, PixelFormat.Format32bppArgb);
            byte[] indices = (tex.BitsPerPixel == 4)
                ? UnswizzleNew4bpp(tex.PixelIndices, tex.Width, tex.Height)
                : Unswizzle8bppPS2(tex.PixelIndices, tex.Width, tex.Height);

            var rect = new Rectangle(0, 0, tex.Width, tex.Height);
            var data = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            IntPtr ptr = data.Scan0;
            int stride = data.Stride;
            int total = Math.Abs(stride) * tex.Height;
            byte[] rgba = new byte[total];

            var palette = tex.Palettes[paletteIndex];

            int p = 0;
            for (int y = 0; y < tex.Height; y++)
            {
                for (int x = 0; x < tex.Width; x++)
                {
                    int idx = indices[p++];
                    int off = idx * 4;
                    if (off + 3 >= palette.Length) continue;

                    byte r = palette[off];
                    byte g = palette[off + 1];
                    byte b = palette[off + 2];
                    byte a = (byte)Math.Min(palette[off + 3] * 2, 255);

                    int pos = y * stride + x * 4;
                    rgba[pos] = b;
                    rgba[pos + 1] = g;
                    rgba[pos + 2] = r;
                    rgba[pos + 3] = a;
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgba, 0, ptr, total);
            bmp.UnlockBits(data);
            return bmp;
        }
        // Encontra o índice da cor na paleta que é mais parecida com o pixel alvo
        private int FindClosestPaletteIndex(Color pixel, byte[] palette)
        {
            int closestIndex = 0;
            int minDistance = int.MaxValue;
            int colorCount = palette.Length / 4;

            for (int i = 0; i < colorCount; i++)
            {
                int palR = palette[i * 4 + 0];
                int palG = palette[i * 4 + 1];
                int palB = palette[i * 4 + 2];
                // O alfa da paleta do PS2 é de 7-bit, então normalizamos para 8-bit
                int palA = Math.Min(palette[i * 4 + 3] * 2, 255);

                // Calcula a "distância" entre as cores
                int distance = (pixel.R - palR) * (pixel.R - palR) +
                               (pixel.G - palG) * (pixel.G - palG) +
                               (pixel.B - palB) * (pixel.B - palB) +
                               (pixel.A - palA) * (pixel.A - palA);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = i;
                }

                if (distance == 0) return i; // Encontrou a cor exata
            }
            return closestIndex;
        }

        // Converte um Bitmap inteiro para um array de índices de paleta (lineares)
        private byte[] QuantizeImageToIndices(Bitmap image, byte[] palette)
        {
            byte[] indices = new byte[image.Width * image.Height];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    indices[y * image.Width + x] = (byte)FindClosestPaletteIndex(pixel, palette);
                }
            }
            return indices;
        }
        /// <summary>
        /// Desentrelaça rawPalAll (palCount paletas de paletteEntries entradas) em byte[palCount][]
        /// </summary>
        private static byte[][] DeinterleavePalettes(byte[] rawPalAll, int paletteCount, int paletteEntries)
        {
            const int BlockEntries = 16;
            const int BlockBytes = BlockEntries * 4;
            int groups = paletteEntries / BlockEntries;

            // prepara até 8 slots
            var slots = new List<byte>[8];
            for (int i = 0; i < 8; i++)
                slots[i] = new List<byte>(groups * BlockBytes);

            int ptr = 0;
            // primeira passada: slots 0/1 sempre; 2/3 se aplicável
            for (int g = 0; g < groups; g++)
            {
                // slot 0
                var chunk0 = new byte[BlockBytes];
                Array.Copy(rawPalAll, ptr, chunk0, 0, BlockBytes);
                slots[0].AddRange(chunk0);
                ptr += BlockBytes;

                // slot 1
                var chunk1 = new byte[BlockBytes];
                Array.Copy(rawPalAll, ptr, chunk1, 0, BlockBytes);
                slots[1].AddRange(chunk1);
                ptr += BlockBytes;

                if (paletteCount == 3 || paletteCount >= 5)
                {
                    var chunk2 = new byte[BlockBytes];
                    Array.Copy(rawPalAll, ptr, chunk2, 0, BlockBytes);
                    slots[2].AddRange(chunk2);
                    ptr += BlockBytes;

                    if (paletteCount >= 7)
                    {
                        var chunk3 = new byte[BlockBytes];
                        Array.Copy(rawPalAll, ptr, chunk3, 0, BlockBytes);
                        slots[3].AddRange(chunk3);
                        ptr += BlockBytes;
                    }
                }
            }

            // segunda passada: slots 4/5 se >=4; slot 6 se >=6; slot 7 se >=8
            if (paletteCount >= 4)
            {
                for (int g = 0; g < groups; g++)
                {
                    var chunk4 = new byte[BlockBytes];
                    Array.Copy(rawPalAll, ptr, chunk4, 0, BlockBytes);
                    slots[4].AddRange(chunk4);
                    ptr += BlockBytes;

                    var chunk5 = new byte[BlockBytes];
                    Array.Copy(rawPalAll, ptr, chunk5, 0, BlockBytes);
                    slots[5].AddRange(chunk5);
                    ptr += BlockBytes;

                    if (paletteCount >= 6)
                    {
                        var chunk6 = new byte[BlockBytes];
                        Array.Copy(rawPalAll, ptr, chunk6, 0, BlockBytes);
                        slots[6].AddRange(chunk6);
                        ptr += BlockBytes;
                    }

                    if (paletteCount >= 8)
                    {
                        var chunk7 = new byte[BlockBytes];
                        Array.Copy(rawPalAll, ptr, chunk7, 0, BlockBytes);
                        slots[7].AddRange(chunk7);
                        ptr += BlockBytes;
                    }
                }
            }

            // determina ordem de saída
            var outIdx = GetOutputIndices(paletteCount);
            var result = new byte[paletteCount][];
            for (int i = 0; i < paletteCount; i++)
                result[i] = slots[outIdx[i]].ToArray();

            return result;
        }
        private static List<int> GetOutputIndices(int n)
        {
            var idxs = new List<int> { 0, 1 };
            if (n == 3 || n >= 5)
            {
                idxs.Add(2);
                if (n >= 7) idxs.Add(3);
            }
            if (n >= 4)
            {
                idxs.Add(4);
                idxs.Add(5);
                if (n >= 6) idxs.Add(6);
                if (n >= 8) idxs.Add(7);
            }
            if (idxs.Count > n)
                idxs = idxs.GetRange(0, n);
            return idxs;
        }
        public static byte[] SwapPalette(byte[] d)
        {
            if (d.Length != 1024) return d;
            var p = new byte[1024];
            Buffer.BlockCopy(d, 0, p, 0, 1024);
            for (int pos = 32; pos < 992; pos += 64)
                if ((pos - 32) % 128 == 0)
                    for (int j = 0; j < 32; j++)
                    {
                        byte t = p[pos + j];
                        p[pos + j] = p[pos + 32 + j];
                        p[pos + 32 + j] = t;
                    }
            return p;
        }
        public static byte[] Unswizzle4bppXBOX(byte[] input, int width, int height)
        {
            int numPixels = width * height;
            byte[] output = new byte[numPixels / 2];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 2)
                {
                    int morton = EncodeMorton2(y, x / 2);
                    if (morton >= input.Length)
                        continue;

                    byte packed = input[morton];
                    int dst = (y * width + x) / 2;
                    output[dst] = packed;
                }
            }

            return output;
        }
        public static byte[] Unswizzle8bppXBOX(byte[] input, int width, int height)
        {
            byte[] output = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int morton = EncodeMorton2(x, y);
                    int destIndex = y * width + x;

                    if (morton < input.Length)
                        output[destIndex] = input[morton];
                }
            }

            return output;
        }

        // Morton decoder (Z-order curve)
        private static int EncodeMorton2(int x, int y)
        {
            return Part1By1(x) | (Part1By1(y) << 1);
        }

        private static int Part1By1(int n)
        {
            n &= 0x0000ffff;
            n = (n | (n << 8)) & 0x00FF00FF;
            n = (n | (n << 4)) & 0x0F0F0F0F;
            n = (n | (n << 2)) & 0x33333333;
            n = (n | (n << 1)) & 0x55555555;
            return n;
        }
        public static byte[] Unswizzle8bppPS2(byte[] buf, int w, int h)
        {
            var outb = new byte[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int block = (y & ~0x0F) * w + (x & ~0x0F) * 2;
                    int swap = (((y + 2) >> 2) & 1) * 4;
                    int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
                    int col = posY * w * 2 + ((x + swap) & 7) * 4;
                    int bn = ((y >> 1) & 1) + ((x >> 2) & 2);
                    int src = block + col + bn;
                    if (src < buf.Length) outb[y * w + x] = buf[src];
                }
            return outb;
        }

        /// <summary>
        /// Desswizzle 4bppNew no formato W=32, H=8 (blocos 32×8),
        /// retornando um array de width*height índices (um byte por pixel).
        /// </summary>
        public static byte[] UnswizzleNew4bpp(byte[] buf, int width, int height)
        {
            int total = width * height;
            byte[] pixels = new byte[total];
            int ptr = 0;
            int stride = width >> 1;

            // 1) extrai nibbles para pixels lineares
            for (int y = 0; y < height; y++)
            {
                int lineOff = y * stride;
                for (int x = 0; x < stride; x++)
                {
                    byte b = buf[lineOff + x];
                    pixels[ptr++] = (byte)(b & 0x0F);
                    pixels[ptr++] = (byte)(b >> 4);
                }
            }

            // 2) reordena blocos 32×8
            byte[] linear = new byte[total];
            int MW = (width % 32 == 0) ? width : ((width / 32) * 32 + 32);

            for (int y = 0; y < height; y++)
            {
                bool oddY = (y & 1) != 0;
                int m4 = y & 3;
                int tileY = (y >> 2) & 1;

                for (int x = 0; x < width; x++)
                {
                    bool oddX = ((x >> 2) & 1) != 0;
                    int idx4 = (x >> 2) & 3;
                    int im = idx4 + (oddY ? 4 : 0);

                    int I = _Interlace[im]
                          + ((x * 4) & 0x0F)
                          + ((x >> 4) * 32)
                          + (oddY ? (y - 1) * MW : y * MW);

                    int XX = x + _Tile[oddX ? 1 : 0] * tileY;
                    int YY = y + _Matrix[m4];
                    int J = YY * width + XX;

                    if (I >= 0 && I < total && J >= 0 && J < total)
                        linear[J] = pixels[I];
                }
            }

            return linear;
        }
        public static byte[] Swizzle8bppPS2(byte[] linear, int width, int height)
        {
            var swizzled = new byte[linear.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int block = (y & ~0x0F) * width + (x & ~0x0F) * 2;
                    int swap = (((y + 2) >> 2) & 1) * 4;
                    int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
                    int col = posY * width * 2 + ((x + swap) & 7) * 4;
                    int bn = ((y >> 1) & 1) + ((x >> 2) & 2);

                    int destIndex = block + col + bn;
                    int sourceIndex = y * width + x;

                    if (destIndex < swizzled.Length)
                    {
                        swizzled[destIndex] = linear[sourceIndex];
                    }
                }
            }
            return swizzled;
        }
        public static byte[] Swizzle4bppPS2(byte[] linearIndices, int width, int height)
        {
            // A lógica de swizzle para 4bpp no PS2 é complexa e pode variar.
            // Esta implementação é uma aproximação baseada no formato de 8bpp.
            // Primeiro, desempacotamos os índices para 1 byte por pixel para facilitar o swizzle.
            byte[] unpacked = new byte[width * height];
            for (int i = 0; i < linearIndices.Length; i++)
            {
                unpacked[i * 2] = (byte)(linearIndices[i] & 0x0F);
                unpacked[i * 2 + 1] = (byte)(linearIndices[i] >> 4);
            }

            // Realiza o swizzle como se fosse 8bpp
            byte[] swizzledUnpacked = Swizzle8bppPS2(unpacked, width, height);

            // Reempacota os dados swizzled para 4bpp
            byte[] repacked = new byte[width * height / 2];
            for (int i = 0; i < repacked.Length; i++)
            {
                byte p1 = swizzledUnpacked[i * 2];
                byte p2 = swizzledUnpacked[i * 2 + 1];
                repacked[i] = (byte)(p1 | (p2 << 4));
            }
            return repacked;
        }
        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomSteps++;
            AtualizarZoomFactor();
            ApplyZoom();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            // Não permite zoom menor que 50% para evitar imagens muito pequenas
            if (zoomFactor > 0.5)
            {
                zoomSteps--;
                AtualizarZoomFactor();
                ApplyZoom();
            }
        }

        private void AtualizarZoomFactor()
        {
            // Usando uma escala exponencial para um zoom mais suave
            zoomFactor = Math.Pow(1.2, zoomSteps);
            zoomLevel.Text = $"Zoom {Math.Round(zoomFactor * 100)}%";
        }

        private void ApplyZoom()
        {
            if (originalImage == null) return;

            int newW = (int)(originalImage.Width * zoomFactor);
            int newH = (int)(originalImage.Height * zoomFactor);

            // Previne a criação de bitmaps com tamanho zero ou negativo
            if (newW < 1 || newH < 1) return;

            var zoomed = new Bitmap(newW, newH);
            using (Graphics g = Graphics.FromImage(zoomed))
            {
                // Usar 'NearestNeighbor' preserva os pixels originais em grandes ampliações
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(originalImage, 0, 0, newW, newH);
            }

            pictureBoxDisplay.Image = zoomed;
            // Centraliza a imagem se ela for menor que o PictureBox, senão, aplica zoom para caber
            pictureBoxDisplay.SizeMode = (newW <= pictureBoxDisplay.Width && newH <= pictureBoxDisplay.Height)
                ? PictureBoxSizeMode.CenterImage
                : PictureBoxSizeMode.Zoom;
        }

        private void Paletteplus_Click(object sender, EventArgs e)
        {
            if (currentTexture == null) return;
            if (currentPaletteIndex < currentTexture.Palettes.Count - 1)
            {
                currentPaletteIndex++;
                if (currentTexture.DirectBitmap != null)
                    originalImage = currentTexture.DirectBitmap;
                else
                    originalImage = CreateBitmapFromData(currentTexture, currentPaletteIndex);
                ApplyZoom();
                AtualizarBotoesPaleta(); // <-- Atualiza botões após mudar paleta
            }
        }

        private void Paletteminus_Click(object sender, EventArgs e)
        {
            if (currentTexture == null) return;
            if (currentPaletteIndex > 0)
            {
                currentPaletteIndex--;
                if (currentTexture.DirectBitmap != null)
                    originalImage = currentTexture.DirectBitmap;
                else
                    originalImage = CreateBitmapFromData(currentTexture, currentPaletteIndex);
                ApplyZoom();
                AtualizarBotoesPaleta(); // <-- Atualiza botões após mudar paleta
            }
        }
        private void AtualizarBotoesPaleta()
        {
            if (currentTexture == null)
            {
                Paletteplus.Enabled = false;
                Paletteminus.Enabled = false;
                return;
            }

            bool temMaisDeUmaPaleta = currentTexture.Palettes != null && currentTexture.Palettes.Count > 1;

            Paletteplus.Enabled = temMaisDeUmaPaleta && currentPaletteIndex < currentTexture.Palettes.Count - 1;
            Paletteminus.Enabled = temMaisDeUmaPaleta && currentPaletteIndex > 0;

            // ATUALIZA O TEXTO DO LABEL SEM REMOVER O TEXTO FIXO
            palettenumber.Text = $"Paleta: {currentPaletteIndex + 1}/{currentTexture.Palettes.Count}";
        }
        private void ConfigurarMenuDeContexto()
        {
            menuDeContexto = new ContextMenuStrip();

            var salvarItem = new ToolStripMenuItem("Salvar como PNG");
            salvarItem.Click += (s, e) => SalvarImagemComoPng();

            var importarItem = new ToolStripMenuItem("Importar PNG");
            importarItem.Click += (s, e) => ImportarImagemPng();

            menuDeContexto.Items.Add(salvarItem);
            menuDeContexto.Items.Add(importarItem);

            pictureBoxDisplay.ContextMenuStrip = menuDeContexto;
        }
        private void ImportarImagemPng()
        {
            if (currentTexture == null)
            {
                MessageBox.Show("Nenhuma textura selecionada.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Abre a janela para selecionar o arquivo PNG
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "PNG Image|*.png";
                ofd.Title = "Importar textura de um PNG";
                if (ofd.ShowDialog() != DialogResult.OK) return;

                // Carrega o PNG e verifica as dimensões
                Bitmap newBitmap;
                try { newBitmap = new Bitmap(ofd.FileName); }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao carregar o arquivo de imagem: " + ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (newBitmap.Width != currentTexture.Width || newBitmap.Height != currentTexture.Height)
                {
                    MessageBox.Show($"A imagem selecionada ({newBitmap.Width}x{newBitmap.Height}) tem dimensões diferentes da original ({currentTexture.Width}x{currentTexture.Height}).\n\nPor favor, use uma imagem com as mesmas dimensões.", "Dimensões Incompatíveis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    newBitmap.Dispose();
                    return;
                }

                int selectedIndex = comboBoxBinFiles.SelectedIndex;
                if (selectedIndex < 0) return;
                string filePath = openFilePaths[selectedIndex];

                try
                {
                    // ---- LÓGICA DE IMPORTAÇÃO ----
                    if (currentTexture.OriginalDxtFormat != null) // É DXT (XBOX)
                    {
                        // Converte Bitmap para Color[]
                        Color[] pixels = new Color[newBitmap.Width * newBitmap.Height];
                        for (int y = 0; y < newBitmap.Height; y++)
                            for (int x = 0; x < newBitmap.Width; x++)
                                pixels[y * newBitmap.Width + x] = newBitmap.GetPixel(x, y);

                        // Codifica para DXT
                        DXTFormat dxtFormat = currentTexture.OriginalDxtFormat.Value;
                        byte[] newDxtData = DXTEncoder.EncodeDXT(pixels, currentTexture.Width, currentTexture.Height, dxtFormat);

                        // Escreve os dados DXT no arquivo
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                        {
                            fs.Seek(currentTexture.TextureOffset, SeekOrigin.Begin);
                            fs.Write(newDxtData, 0, newDxtData.Length);
                        }
                    }
                    else if (currentTexture.Palettes != null && currentTexture.Palettes.Count > 0) // É paletada (PS2)
                    {
                        // Pega a paleta atual que está sendo visualizada
                        byte[] currentPalette = currentTexture.Palettes[currentPaletteIndex];

                        // 1. "Quantiza" a imagem para obter os índices lineares
                        byte[] linearIndices = QuantizeImageToIndices(newBitmap, currentPalette);

                        // 2. Faz o "Swizzle" dos índices para o formato do PS2
                        byte[] swizzledData;
                        if (currentTexture.BitsPerPixel == 8)
                        {
                            swizzledData = Swizzle8bppPS2(linearIndices, currentTexture.Width, currentTexture.Height);
                        }
                        else // 4bpp
                        {
                            // Para 4bpp, precisamos empacotar 2 índices em cada byte antes do swizzle
                            byte[] packedIndices = new byte[linearIndices.Length / 2];
                            for (int i = 0; i < packedIndices.Length; i++)
                            {
                                byte p1 = linearIndices[i * 2 + 0];
                                byte p2 = linearIndices[i * 2 + 1];
                                packedIndices[i] = (byte)(p1 | (p2 << 4));
                            }
                            swizzledData = Swizzle4bppPS2(packedIndices, currentTexture.Width, currentTexture.Height);
                        }

                        // 3. Escreve os novos dados de pixel no arquivo (não mexemos na paleta)
                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                        {
                            fs.Seek(currentTexture.TextureOffset, SeekOrigin.Begin);
                            fs.Write(swizzledData, 0, swizzledData.Length);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Este formato de textura não suporta importação no momento.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // ---- ATUALIZAÇÃO DA UI ----
                    currentTexture.DirectBitmap = (Bitmap)newBitmap.Clone();
                    originalImage = currentTexture.DirectBitmap;
                    ApplyZoom();

                    MessageBox.Show($"Textura '{currentTexture.Name}' importada e salva com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ocorreu um erro ao salvar os dados no arquivo: " + ex.Message, "Erro de Escrita", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    newBitmap.Dispose();
                }
            }
        }
        private void SalvarImagemComoPng()
        {
            if (originalImage == null)
            {
                MessageBox.Show("Nenhuma imagem carregada para salvar.");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png";
                sfd.Title = "Salvar textura em PNG";
                sfd.FileName = currentTexture?.Name ?? "imagem";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ImageFormat formato = ImageFormat.Png;
                        originalImage.Save(sfd.FileName, formato);
                        MessageBox.Show("Imagem salva com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao salvar imagem: " + ex.Message);
                    }
                }
            }
        }
    }
    public class TxdTexture
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public byte[] PixelIndices { get; set; }
        public List<byte[]> Palettes { get; set; }
        public Bitmap DirectBitmap { get; set; }
        public long TextureOffset { get; set; }
        public long PaletteOffset { get; set; }
        public DXTFormat? OriginalDxtFormat { get; set; }
    }    
}
