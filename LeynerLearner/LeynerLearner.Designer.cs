namespace LeynerLearner
{
    partial class LeynerLearner
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            toolbar = new FlowLayoutPanel();
            lblShabbat = new Label();
            dtpShabbat = new DateTimePicker();
            btnUpcoming = new Button();
            lblOccasion = new Label();
            cboOccasion = new ComboBox();
            btnPrev = new Button();
            btnNext = new Button();
            chkIsrael = new CheckBox();
            lblParasha = new Label();
            cboParasha = new ComboBox();
            lblYear = new Label();
            cboYear = new ComboBox();
            lblHaftarah = new Label();
            cboHaftarah = new ComboBox();
            chkUpper = new CheckBox();
            lblShevaNa = new Label();
            cboShevaNa = new ComboBox();
            lblFont = new Label();
            cboFont = new ComboBox();
            lblSize = new Label();
            numSize = new NumericUpDown();
            lblEnglish = new Label();
            cboEnglish = new ComboBox();
            btnPdf = new Button();
            btnPrint = new Button();
            btnHelp = new Button();
            btnData = new Button();
            lblTranslit = new Label();
            cboTranslit = new ComboBox();
            toolTip = new ToolTip(components);
            lblInfo = new Label();
            rtbReading = new RichTextBox();
            toolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numSize).BeginInit();
            SuspendLayout();
            // 
            // toolbar
            // 
            toolbar.AutoSize = true;
            toolbar.Controls.Add(lblShabbat);
            toolbar.Controls.Add(dtpShabbat);
            toolbar.Controls.Add(btnUpcoming);
            toolbar.Controls.Add(lblOccasion);
            toolbar.Controls.Add(cboOccasion);
            toolbar.Controls.Add(btnPrev);
            toolbar.Controls.Add(btnNext);
            toolbar.Controls.Add(chkIsrael);
            toolbar.Controls.Add(lblParasha);
            toolbar.Controls.Add(cboParasha);
            toolbar.Controls.Add(lblYear);
            toolbar.Controls.Add(cboYear);
            toolbar.Controls.Add(lblHaftarah);
            toolbar.Controls.Add(cboHaftarah);
            toolbar.Controls.Add(chkUpper);
            toolbar.Controls.Add(lblShevaNa);
            toolbar.Controls.Add(cboShevaNa);
            toolbar.Controls.Add(lblFont);
            toolbar.Controls.Add(cboFont);
            toolbar.Controls.Add(lblSize);
            toolbar.Controls.Add(numSize);
            toolbar.Controls.Add(lblEnglish);
            toolbar.Controls.Add(cboEnglish);
            toolbar.Controls.Add(lblTranslit);
            toolbar.Controls.Add(cboTranslit);
            toolbar.Controls.Add(btnPdf);
            toolbar.Controls.Add(btnPrint);
            toolbar.Controls.Add(btnHelp);
            toolbar.Controls.Add(btnData);
            toolbar.Dock = DockStyle.Top;
            toolbar.Location = new Point(0, 0);
            toolbar.Name = "toolbar";
            toolbar.Padding = new Padding(6);
            toolbar.Size = new Size(1084, 103);
            toolbar.TabIndex = 0;
            // 
            // lblShabbat
            // 
            lblShabbat.Anchor = AnchorStyles.Left;
            lblShabbat.AutoSize = true;
            lblShabbat.Location = new Point(9, 14);
            lblShabbat.Name = "lblShabbat";
            lblShabbat.Size = new Size(34, 15);
            lblShabbat.TabIndex = 0;
            lblShabbat.Text = "Date:";
            // 
            // dtpShabbat
            // 
            dtpShabbat.CustomFormat = "ddd MMM d, yyyy";
            dtpShabbat.Format = DateTimePickerFormat.Custom;
            dtpShabbat.Location = new Point(49, 9);
            dtpShabbat.MaxDate = new DateTime(2199, 12, 31, 0, 0, 0, 0);
            dtpShabbat.MinDate = new DateTime(1983, 10, 1, 0, 0, 0, 0);
            dtpShabbat.Name = "dtpShabbat";
            dtpShabbat.Size = new Size(160, 23);
            dtpShabbat.TabIndex = 1;
            dtpShabbat.ValueChanged += dtpShabbat_ValueChanged;
            // 
            // btnUpcoming
            // 
            btnUpcoming.AutoSize = true;
            btnUpcoming.Location = new Point(215, 9);
            btnUpcoming.Name = "btnUpcoming";
            btnUpcoming.Size = new Size(75, 25);
            btnUpcoming.TabIndex = 2;
            btnUpcoming.Text = "Today";
            btnUpcoming.Click += btnUpcoming_Click;
            // 
            // lblOccasion
            // 
            lblOccasion.Anchor = AnchorStyles.Left;
            lblOccasion.AutoSize = true;
            lblOccasion.Location = new Point(305, 14);
            lblOccasion.Margin = new Padding(12, 0, 3, 0);
            lblOccasion.Name = "lblOccasion";
            lblOccasion.Size = new Size(53, 15);
            lblOccasion.TabIndex = 3;
            lblOccasion.Text = "Reading:";
            // 
            // cboOccasion
            // 
            cboOccasion.DropDownStyle = ComboBoxStyle.DropDownList;
            cboOccasion.Location = new Point(364, 9);
            cboOccasion.MaxDropDownItems = 30;
            cboOccasion.Name = "cboOccasion";
            cboOccasion.Size = new Size(380, 23);
            cboOccasion.TabIndex = 3;
            toolTip.SetToolTip(cboOccasion, "Shabbat portions, holidays, fast days and Rosh Chodesh with a Torah reading");
            cboOccasion.SelectedIndexChanged += cboOccasion_SelectedIndexChanged;
            // 
            // btnPrev
            // 
            btnPrev.AutoSize = true;
            btnPrev.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPrev.Location = new Point(750, 9);
            btnPrev.Name = "btnPrev";
            btnPrev.Size = new Size(27, 25);
            btnPrev.TabIndex = 4;
            btnPrev.Text = "◀";
            toolTip.SetToolTip(btnPrev, "Previous reading");
            btnPrev.Click += btnPrev_Click;
            // 
            // btnNext
            // 
            btnNext.AutoSize = true;
            btnNext.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNext.Location = new Point(783, 9);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(27, 25);
            btnNext.TabIndex = 4;
            btnNext.Text = "▶";
            toolTip.SetToolTip(btnNext, "Next reading");
            btnNext.Click += btnNext_Click;
            // 
            // chkIsrael
            // 
            chkIsrael.Anchor = AnchorStyles.Left;
            chkIsrael.AutoSize = true;
            toolbar.SetFlowBreak(chkIsrael, true);
            chkIsrael.Location = new Point(816, 12);
            chkIsrael.Name = "chkIsrael";
            chkIsrael.Size = new Size(53, 19);
            chkIsrael.TabIndex = 5;
            chkIsrael.Text = "Israel";
            chkIsrael.CheckedChanged += chkIsrael_CheckedChanged;
            // 
            // lblParasha
            // 
            lblParasha.Anchor = AnchorStyles.Left;
            lblParasha.AutoSize = true;
            lblParasha.Location = new Point(18, 44);
            lblParasha.Margin = new Padding(12, 0, 3, 0);
            lblParasha.Name = "lblParasha";
            lblParasha.Size = new Size(58, 15);
            lblParasha.TabIndex = 3;
            lblParasha.Text = "Parashah:";
            // 
            // cboParasha
            // 
            cboParasha.DropDownStyle = ComboBoxStyle.DropDownList;
            cboParasha.Location = new Point(82, 40);
            cboParasha.MaxDropDownItems = 25;
            cboParasha.Name = "cboParasha";
            cboParasha.Size = new Size(190, 23);
            cboParasha.TabIndex = 3;
            cboParasha.SelectedIndexChanged += selection_Changed;
            // 
            // lblYear
            // 
            lblYear.Anchor = AnchorStyles.Left;
            lblYear.AutoSize = true;
            lblYear.Location = new Point(278, 44);
            lblYear.Name = "lblYear";
            lblYear.Size = new Size(80, 15);
            lblYear.TabIndex = 4;
            lblYear.Text = "Triennial year:";
            // 
            // cboYear
            // 
            cboYear.DropDownStyle = ComboBoxStyle.DropDownList;
            cboYear.Items.AddRange(new object[] { "1", "2", "3" });
            cboYear.Location = new Point(364, 40);
            cboYear.Name = "cboYear";
            cboYear.Size = new Size(45, 23);
            cboYear.TabIndex = 4;
            cboYear.SelectedIndexChanged += selection_Changed;
            // 
            // lblHaftarah
            // 
            lblHaftarah.Anchor = AnchorStyles.Left;
            lblHaftarah.AutoSize = true;
            lblHaftarah.Location = new Point(424, 44);
            lblHaftarah.Margin = new Padding(12, 0, 3, 0);
            lblHaftarah.Name = "lblHaftarah";
            lblHaftarah.Size = new Size(56, 15);
            lblHaftarah.TabIndex = 6;
            lblHaftarah.Text = "Haftarah:";
            // 
            // cboHaftarah
            // 
            cboHaftarah.DropDownStyle = ComboBoxStyle.DropDownList;
            cboHaftarah.Location = new Point(486, 40);
            cboHaftarah.Name = "cboHaftarah";
            cboHaftarah.Size = new Size(110, 23);
            cboHaftarah.TabIndex = 6;
            toolTip.SetToolTip(cboHaftarah, "Which haftarah to show. Triennial: the alternative triennial-cycle haftarot (standard haftarah where there is none).");
            cboHaftarah.SelectedIndexChanged += display_Changed;
            // 
            // chkUpper
            // 
            chkUpper.Anchor = AnchorStyles.Left;
            chkUpper.AutoSize = true;
            chkUpper.Location = new Point(602, 42);
            chkUpper.Name = "chkUpper";
            chkUpper.Size = new Size(89, 19);
            chkUpper.TabIndex = 9;
            chkUpper.Text = "Upper trope";
            toolTip.SetToolTip(chkUpper, "Use ta'am elyon (the upper cantillation) for the Ten Commandments and the midrashic reading of Genesis 35:22. Unchecked shows ta'am tachton.");
            chkUpper.CheckedChanged += display_Changed;
            // 
            // lblShevaNa
            // 
            lblShevaNa.Anchor = AnchorStyles.Left;
            lblShevaNa.AutoSize = true;
            lblShevaNa.Location = new Point(697, 44);
            lblShevaNa.Name = "lblShevaNa";
            lblShevaNa.Size = new Size(90, 15);
            lblShevaNa.TabIndex = 10;
            lblShevaNa.Text = "Sounded sheva:";
            // 
            // cboShevaNa
            // 
            cboShevaNa.DropDownStyle = ComboBoxStyle.DropDownList;
            toolbar.SetFlowBreak(cboShevaNa, true);
            cboShevaNa.Items.AddRange(new object[] { "Off", "Color", "Large sheva", "Large sheva and color", "Sheva with rafe", "Sheva with rafe and color" });
            cboShevaNa.Location = new Point(793, 40);
            cboShevaNa.Name = "cboShevaNa";
            cboShevaNa.Size = new Size(150, 23);
            cboShevaNa.TabIndex = 9;
            toolTip.SetToolTip(cboShevaNa, "How to show a sounded sheva (sheva na), from Phonetic MAM. Color: the letter in red (any font). Large sheva: a bigger sheva, drawn by the bundled Hebrew Sheva Na font (selected automatically). Large sheva and color: both. Sheva with rafe: a rafe (bar) above the letter (any font with a rafe). Sheva with rafe and color: both.");
            cboShevaNa.SelectedIndexChanged += cboShevaNa_SelectedIndexChanged;
            // 
            // lblFont
            // 
            lblFont.Anchor = AnchorStyles.Left;
            lblFont.AutoSize = true;
            lblFont.Location = new Point(9, 74);
            lblFont.Name = "lblFont";
            lblFont.Size = new Size(34, 15);
            lblFont.TabIndex = 6;
            lblFont.Text = "Font:";
            // 
            // cboFont
            // 
            cboFont.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFont.Location = new Point(49, 69);
            cboFont.MaxDropDownItems = 25;
            cboFont.Name = "cboFont";
            cboFont.Size = new Size(170, 23);
            cboFont.TabIndex = 6;
            cboFont.SelectedIndexChanged += display_Changed;
            // 
            // lblSize
            // 
            lblSize.Anchor = AnchorStyles.Left;
            lblSize.AutoSize = true;
            lblSize.Location = new Point(225, 74);
            lblSize.Name = "lblSize";
            lblSize.Size = new Size(30, 15);
            lblSize.TabIndex = 7;
            lblSize.Text = "Size:";
            // 
            // numSize
            // 
            numSize.Location = new Point(261, 69);
            numSize.Maximum = new decimal(new int[] { 72, 0, 0, 0 });
            numSize.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
            numSize.Name = "numSize";
            numSize.Size = new Size(50, 23);
            numSize.TabIndex = 7;
            numSize.Value = new decimal(new int[] { 24, 0, 0, 0 });
            numSize.ValueChanged += display_Changed;
            // 
            // lblEnglish
            // 
            lblEnglish.Anchor = AnchorStyles.Left;
            lblEnglish.AutoSize = true;
            lblEnglish.Location = new Point(615, 74);
            lblEnglish.Name = "lblEnglish";
            lblEnglish.Size = new Size(48, 15);
            lblEnglish.TabIndex = 8;
            lblEnglish.Text = "English:";
            // 
            // cboEnglish
            // 
            cboEnglish.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEnglish.Items.AddRange(new object[] { "Off", "JPS 1917", "JPS 1917 Modern" });
            cboEnglish.Location = new Point(669, 69);
            cboEnglish.Name = "cboEnglish";
            cboEnglish.Size = new Size(125, 23);
            cboEnglish.TabIndex = 8;
            toolTip.SetToolTip(cboEnglish, "English under each aliyah: the JPS 1917 translation, or JPS 1917 Modern (the same translation in modern English).");
            cboEnglish.SelectedIndexChanged += display_Changed;
            //
            // lblTranslit
            //
            lblTranslit.Anchor = AnchorStyles.Left;
            lblTranslit.AutoSize = true;
            lblTranslit.Location = new Point(685, 74);
            lblTranslit.Name = "lblTranslit";
            lblTranslit.Size = new Size(91, 15);
            lblTranslit.TabIndex = 8;
            lblTranslit.Text = "Transliteration:";
            //
            // cboTranslit
            //
            cboTranslit.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTranslit.Items.AddRange(new object[] { "Off", "Sephardic", "Ashkenazic" });
            cboTranslit.Location = new Point(782, 69);
            cboTranslit.Name = "cboTranslit";
            cboTranslit.Size = new Size(95, 23);
            cboTranslit.TabIndex = 8;
            toolTip.SetToolTip(cboTranslit, "Show each aliyah transliterated, from Phonetic MAM. A raised e is a sounded sheva; the stressed syllable is underlined.");
            cboTranslit.SelectedIndexChanged += display_Changed;
            // 
            // btnPdf
            // 
            btnPdf.AutoSize = true;
            btnPdf.Location = new Point(694, 69);
            btnPdf.Margin = new Padding(12, 3, 3, 3);
            btnPdf.Name = "btnPdf";
            btnPdf.Size = new Size(75, 25);
            btnPdf.TabIndex = 9;
            btnPdf.Text = "Save PDF…";
            btnPdf.Click += btnPdf_Click;
            // 
            // btnPrint
            // 
            btnPrint.AutoSize = true;
            btnPrint.Location = new Point(775, 69);
            btnPrint.Name = "btnPrint";
            btnPrint.Size = new Size(75, 25);
            btnPrint.TabIndex = 11;
            btnPrint.Text = "Print…";
            btnPrint.Click += btnPrint_Click;
            //
            // btnHelp
            //
            btnHelp.AutoSize = true;
            btnHelp.Location = new Point(868, 69);
            btnHelp.Margin = new Padding(12, 3, 3, 3);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new Size(75, 25);
            btnHelp.TabIndex = 12;
            btnHelp.Text = "Help";
            toolTip.SetToolTip(btnHelp, "Where the data comes from, and sheva na vs. sheva nach (F1)");
            btnHelp.Click += btnHelp_Click;
            // 
            // btnData
            // 
            btnData.AutoSize = true;
            btnData.Location = new Point(949, 69);
            btnData.Name = "btnData";
            btnData.Size = new Size(90, 25);
            btnData.TabIndex = 13;
            btnData.Text = "Update data…";
            toolTip.SetToolTip(btnData, "Download updated Hebrew text, transliteration, schedules and English, or go back to the built-in data");
            btnData.Click += btnData_Click;
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Dock = DockStyle.Top;
            lblInfo.ForeColor = SystemColors.GrayText;
            lblInfo.Location = new Point(0, 103);
            lblInfo.Name = "lblInfo";
            lblInfo.Padding = new Padding(10, 0, 10, 6);
            lblInfo.Size = new Size(20, 21);
            lblInfo.TabIndex = 10;
            // 
            // rtbReading
            // 
            rtbReading.BackColor = SystemColors.Window;
            rtbReading.BorderStyle = BorderStyle.None;
            rtbReading.Dock = DockStyle.Fill;
            rtbReading.Location = new Point(0, 124);
            rtbReading.Name = "rtbReading";
            rtbReading.ReadOnly = true;
            rtbReading.Size = new Size(1084, 637);
            rtbReading.TabIndex = 11;
            rtbReading.Text = "";
            // 
            // LeynerLearner
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 761);
            Controls.Add(rtbReading);
            Controls.Add(lblInfo);
            Controls.Add(toolbar);
            Name = "LeynerLearner";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Leyner Learner";
            Load += LeynerLearner_Load;
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numSize).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FlowLayoutPanel toolbar;
        private Label lblShabbat;
        private DateTimePicker dtpShabbat;
        private Button btnUpcoming;
        private Label lblOccasion;
        private ComboBox cboOccasion;
        private Button btnPrev;
        private Button btnNext;
        private Label lblHaftarah;
        private ComboBox cboHaftarah;
        private Label lblParasha;
        private ComboBox cboParasha;
        private Label lblYear;
        private ComboBox cboYear;
        private CheckBox chkIsrael;
        private Label lblFont;
        private ComboBox cboFont;
        private Label lblSize;
        private NumericUpDown numSize;
        private Label lblEnglish;
        private ComboBox cboEnglish;
        private CheckBox chkUpper;
        private Label lblShevaNa;
        private ComboBox cboShevaNa;
        private ToolTip toolTip;
        private Button btnPdf;
        private Button btnPrint;
        private Button btnHelp;
        private Button btnData;
        private Label lblTranslit;
        private ComboBox cboTranslit;
        private Label lblInfo;
        private RichTextBox rtbReading;
    }
}
