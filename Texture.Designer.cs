namespace Burnout3EI
{
    partial class Texture
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Texture));
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonAbrirArquivos = new System.Windows.Forms.Button();
            this.comboBoxImages = new System.Windows.Forms.ComboBox();
            this.comboBoxBinFiles = new System.Windows.Forms.ComboBox();
            this.pictureBoxDisplay = new System.Windows.Forms.PictureBox();
            this.palettenumber = new System.Windows.Forms.Label();
            this.Paletteminus = new System.Windows.Forms.Button();
            this.Paletteplus = new System.Windows.Forms.Button();
            this.Enderecotextura = new System.Windows.Forms.Label();
            this.paleta = new System.Windows.Forms.Label();
            this.Resolucao = new System.Windows.Forms.Label();
            this.zoomLevel = new System.Windows.Forms.Label();
            this.btnZoomOut = new System.Windows.Forms.Button();
            this.btnZoomIn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 116);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Select a image";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 86);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Select a BIN file";
            // 
            // buttonAbrirArquivos
            // 
            this.buttonAbrirArquivos.Location = new System.Drawing.Point(101, 49);
            this.buttonAbrirArquivos.Name = "buttonAbrirArquivos";
            this.buttonAbrirArquivos.Size = new System.Drawing.Size(149, 23);
            this.buttonAbrirArquivos.TabIndex = 9;
            this.buttonAbrirArquivos.Text = "Open File";
            this.buttonAbrirArquivos.UseVisualStyleBackColor = true;
            this.buttonAbrirArquivos.Click += new System.EventHandler(this.buttonAbrirArquivos_Click);
            // 
            // comboBoxImages
            // 
            this.comboBoxImages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxImages.Enabled = false;
            this.comboBoxImages.FormattingEnabled = true;
            this.comboBoxImages.Location = new System.Drawing.Point(101, 113);
            this.comboBoxImages.Name = "comboBoxImages";
            this.comboBoxImages.Size = new System.Drawing.Size(149, 21);
            this.comboBoxImages.TabIndex = 8;
            // 
            // comboBoxBinFiles
            // 
            this.comboBoxBinFiles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBinFiles.Enabled = false;
            this.comboBoxBinFiles.FormattingEnabled = true;
            this.comboBoxBinFiles.Location = new System.Drawing.Point(101, 78);
            this.comboBoxBinFiles.Name = "comboBoxBinFiles";
            this.comboBoxBinFiles.Size = new System.Drawing.Size(149, 21);
            this.comboBoxBinFiles.TabIndex = 7;
            // 
            // pictureBoxDisplay
            // 
            this.pictureBoxDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxDisplay.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxDisplay.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxDisplay.Location = new System.Drawing.Point(256, 25);
            this.pictureBoxDisplay.Name = "pictureBoxDisplay";
            this.pictureBoxDisplay.Size = new System.Drawing.Size(592, 545);
            this.pictureBoxDisplay.TabIndex = 6;
            this.pictureBoxDisplay.TabStop = false;
            // 
            // palettenumber
            // 
            this.palettenumber.AutoSize = true;
            this.palettenumber.Location = new System.Drawing.Point(122, 251);
            this.palettenumber.Name = "palettenumber";
            this.palettenumber.Size = new System.Drawing.Size(40, 13);
            this.palettenumber.TabIndex = 17;
            this.palettenumber.Text = "Palette";
            // 
            // Paletteminus
            // 
            this.Paletteminus.Enabled = false;
            this.Paletteminus.Location = new System.Drawing.Point(41, 261);
            this.Paletteminus.Name = "Paletteminus";
            this.Paletteminus.Size = new System.Drawing.Size(75, 23);
            this.Paletteminus.TabIndex = 19;
            this.Paletteminus.Text = "Palette -";
            this.Paletteminus.UseVisualStyleBackColor = true;
            this.Paletteminus.Click += new System.EventHandler(this.Paletteminus_Click);
            // 
            // Paletteplus
            // 
            this.Paletteplus.Enabled = false;
            this.Paletteplus.Location = new System.Drawing.Point(41, 232);
            this.Paletteplus.Name = "Paletteplus";
            this.Paletteplus.Size = new System.Drawing.Size(75, 23);
            this.Paletteplus.TabIndex = 18;
            this.Paletteplus.Text = "Palette +";
            this.Paletteplus.UseVisualStyleBackColor = true;
            this.Paletteplus.Click += new System.EventHandler(this.Paletteplus_Click);
            // 
            // Enderecotextura
            // 
            this.Enderecotextura.AutoSize = true;
            this.Enderecotextura.Location = new System.Drawing.Point(60, 151);
            this.Enderecotextura.Name = "Enderecotextura";
            this.Enderecotextura.Size = new System.Drawing.Size(80, 13);
            this.Enderecotextura.TabIndex = 20;
            this.Enderecotextura.Text = "Texture adress:";
            // 
            // paleta
            // 
            this.paleta.AutoSize = true;
            this.paleta.Location = new System.Drawing.Point(60, 178);
            this.paleta.Name = "paleta";
            this.paleta.Size = new System.Drawing.Size(77, 13);
            this.paleta.TabIndex = 21;
            this.paleta.Text = "Palette adress:";
            // 
            // Resolucao
            // 
            this.Resolucao.AutoSize = true;
            this.Resolucao.Location = new System.Drawing.Point(60, 200);
            this.Resolucao.Name = "Resolucao";
            this.Resolucao.Size = new System.Drawing.Size(60, 13);
            this.Resolucao.TabIndex = 22;
            this.Resolucao.Text = "Resolution:";
            // 
            // zoomLevel
            // 
            this.zoomLevel.AutoSize = true;
            this.zoomLevel.Location = new System.Drawing.Point(132, 356);
            this.zoomLevel.Name = "zoomLevel";
            this.zoomLevel.Size = new System.Drawing.Size(34, 13);
            this.zoomLevel.TabIndex = 25;
            this.zoomLevel.Text = "Zoom";
            // 
            // btnZoomOut
            // 
            this.btnZoomOut.Location = new System.Drawing.Point(41, 365);
            this.btnZoomOut.Name = "btnZoomOut";
            this.btnZoomOut.Size = new System.Drawing.Size(75, 23);
            this.btnZoomOut.TabIndex = 24;
            this.btnZoomOut.Text = "Zoom -";
            this.btnZoomOut.UseVisualStyleBackColor = true;
            // 
            // btnZoomIn
            // 
            this.btnZoomIn.Location = new System.Drawing.Point(41, 336);
            this.btnZoomIn.Name = "btnZoomIn";
            this.btnZoomIn.Size = new System.Drawing.Size(75, 23);
            this.btnZoomIn.TabIndex = 23;
            this.btnZoomIn.Text = "Zoom +";
            this.btnZoomIn.UseVisualStyleBackColor = true;
            // 
            // Texture
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::Burnout3EI.Properties.Resources.back;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(860, 582);
            this.Controls.Add(this.zoomLevel);
            this.Controls.Add(this.btnZoomOut);
            this.Controls.Add(this.btnZoomIn);
            this.Controls.Add(this.Resolucao);
            this.Controls.Add(this.paleta);
            this.Controls.Add(this.Enderecotextura);
            this.Controls.Add(this.Paletteminus);
            this.Controls.Add(this.Paletteplus);
            this.Controls.Add(this.palettenumber);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonAbrirArquivos);
            this.Controls.Add(this.comboBoxImages);
            this.Controls.Add(this.comboBoxBinFiles);
            this.Controls.Add(this.pictureBoxDisplay);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Texture";
            this.Text = "Visualizador, Extrator e Importador de texturas";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDisplay)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonAbrirArquivos;
        private System.Windows.Forms.ComboBox comboBoxImages;
        private System.Windows.Forms.ComboBox comboBoxBinFiles;
        private System.Windows.Forms.PictureBox pictureBoxDisplay;
        private System.Windows.Forms.Label palettenumber;
        private System.Windows.Forms.Button Paletteminus;
        private System.Windows.Forms.Button Paletteplus;
        private System.Windows.Forms.Label Enderecotextura;
        private System.Windows.Forms.Label paleta;
        private System.Windows.Forms.Label Resolucao;
        private System.Windows.Forms.Label zoomLevel;
        private System.Windows.Forms.Button btnZoomOut;
        private System.Windows.Forms.Button btnZoomIn;
    }
}