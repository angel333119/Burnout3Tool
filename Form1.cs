using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Burnout3EI
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Burnout 3|*.BIN|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Abrir arquivo de Burnout 3 ou Revenge...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (BinaryReader br = new BinaryReader(File.OpenRead(file)))
                    {
                        int magic = br.ReadInt32(); // Read magic

                        if (magic == 0)
                        {

                        }
                        else if (magic == 0x69F60308) // Magic do arquivo Burnout 3
                        {
                            br.BaseStream.Seek(0x08, SeekOrigin.Begin);

                            int totaldetextos = br.ReadInt32(); //Lê o total de textos

                            br.BaseStream.Seek(0x10, SeekOrigin.Begin);

                            int[] ponteiros = new int[totaldetextos];

                            for (int i = 0; i < totaldetextos; i++)
                            {
                                ponteiros[i] = br.ReadInt32();
                            }

                            string todosOsTextos = "";

                            for (int i = 0; i < totaldetextos; i++)
                            {
                                br.BaseStream.Seek(ponteiros[i], SeekOrigin.Begin);

                                bool acabouotexto = false;

                                int tamanhotexto = 0;

                                while (acabouotexto == false)
                                {
                                    int comparador = br.ReadInt16();
                                    tamanhotexto++;
                                    tamanhotexto++;

                                    if (comparador == 0)
                                    {
                                        acabouotexto = true;
                                        br.BaseStream.Seek(-tamanhotexto, SeekOrigin.Current);
                                    }
                                }

                                byte[] bytes = new byte[tamanhotexto];

                                for (int j = 0; j < tamanhotexto; j++)
                                {
                                    bytes[j] = br.ReadByte();
                                }

                                string utf16 = Encoding.Unicode.GetString(bytes);

                                todosOsTextos += utf16.Replace("\n", "<quebra_de_linha>").Replace("\0", String.Empty) + "\r\n";
                            }

                            //aqui já terminou de ler todos os textos, escreve o TXT com o texto dumpado
                            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt", todosOsTextos);

                            todosOsTextos = "";
                        }
                        else if (magic == 0x586A0308) // Magic do arquivo Burnout Revenge
                        {
                            MessageBox.Show("O arquivo não é um arquivo de Burnout 3!", "AVISO!");
                            return;
                        }
                        else
                        {
                            MessageBox.Show("Este programa não funciona com esse tipo de arquivo!", "AVISO!");
                            return;
                        }
                    }
                }
                //Avisa que a extração dos textos terminou
                MessageBox.Show("Texto Extraido", "AVISO");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Burnout 3|*.BIN|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Abrir arquivo de Burnout 3 ou Revenge...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    var dump = openFileDialog1.FileName;

                    using (FileStream stream = File.Open(dump, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        string arquivotxt = Path.Combine(Path.GetDirectoryName(dump), Path.GetFileNameWithoutExtension(dump)) + ".txt";

                        FileInfo fi = new FileInfo(arquivotxt); // Verifica se o txt existe

                        if (fi.Exists)
                        {
                            var linhasdetextos = File.ReadLines(arquivotxt);

                            br.BaseStream.Seek(0x10, SeekOrigin.Begin);

                            stream.SetLength(0x14); //Apaga todo o arquivo

                            int novoponteiro = br.ReadInt32(); //Lê o valor do primeiro ponteiro

                            int numerolinha = 0;

                            foreach (var linha in linhasdetextos)
                            {
                                bw.BaseStream.Seek(0x10 + 4 * numerolinha, SeekOrigin.Begin);

                                bw.Write(novoponteiro);

                                string texto = linha.Replace("<quebra_de_linha>", "\n");

                                byte[] bytes = Encoding.Unicode.GetBytes(texto);

                                bw.BaseStream.Seek(novoponteiro, SeekOrigin.Begin);

                                bw.Write(bytes);

                                bw.Write((short)0);

                                novoponteiro += bytes.Length + 2;

                                numerolinha++;
                            }
                        }
                    }
                }
                //Avisa que terminou
                MessageBox.Show("Texto Inserido", "AVISO");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Hide(); //Esconde o formulário principal
            Texture tx = new Texture(); //Define form2 como o formulário de visualização e extração gráfica do PS2
            tx.FormClosed += (s, args) => this.Show(); //Fecha o programa se fechar o formulário
            tx.Show(); //Mostra o formulário
        }
    }
}