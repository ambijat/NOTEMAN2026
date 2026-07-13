using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Tesseract;

namespace NoteMaker6
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI Controls
        TextBox tbFolder;
        TextBox tbFile;
        TextBox tbRef;
        TextBox tbPage;
        TextBox tbWorking;
        TextBox tbContent;
        Button btnFolder;
        Button btnNew;
        Button btnOpen;
        Button btnReset;
        Button btnExport;
        Button btnPaste;
        Button btnClipOCR;
        Button btnOCRRead;

        string workDir = string.Empty;

        public MainForm()
        {
            Text = "NoteMaker6";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            InitializeComponents();
        }

        void InitializeComponents()
        {
            // Left panel for buttons and inputs
            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 150,
                RowCount = 13,
                ColumnCount = 1,
            };

            btnFolder = new Button { Text = "FOLDER" };
            btnFolder.Click += BtnFolder_Click;
            left.Controls.Add(btnFolder);

            tbFolder = new TextBox { Text = "Folder Name Here" };
            left.Controls.Add(tbFolder);

            btnNew = new Button { Text = "NEW_NOTE" };
            btnNew.Click += BtnNew_Click;
            left.Controls.Add(btnNew);

            tbFile = new TextBox();
            left.Controls.Add(tbFile);

            btnOpen = new Button { Text = "OPEN_NOTE" };
            btnOpen.Click += BtnOpen_Click;
            left.Controls.Add(btnOpen);

            btnReset = new Button { Text = "RESET" };
            btnReset.Click += (s, e) => tbContent.Clear();
            left.Controls.Add(btnReset);

            btnExport = new Button { Text = "EXPORT" };
            btnExport.Click += BtnExport_Click;
            left.Controls.Add(btnExport);

            btnPaste = new Button { Text = "PASTE" };
            btnPaste.Click += BtnPaste_Click;
            left.Controls.Add(btnPaste);

            tbRef = new TextBox { Text = "Reference..." };
            left.Controls.Add(tbRef);

            var pgPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
            var btnPrev = new Button { Text = "<<" };
            btnPrev.Click += (s, e) => ChangePage(-1);
            var btnNext = new Button { Text = ">>" };
            btnNext.Click += (s, e) => ChangePage(1);
            pgPanel.Controls.Add(btnPrev);
            pgPanel.Controls.Add(btnNext);
            left.Controls.Add(pgPanel);

            tbPage = new TextBox { Text = "Pgs." };
            left.Controls.Add(tbPage);

            btnClipOCR = new Button { Text = "CLP_OCR" };
            btnClipOCR.Click += BtnClipOCR_Click;
            left.Controls.Add(btnClipOCR);

            btnOCRRead = new Button { Text = "OCR_READ" };
            btnOCRRead.Click += BtnOCRRead_Click;
            left.Controls.Add(btnOCRRead);

            // Right panel for text content
            tbContent = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
            };

            tbWorking = new TextBox { Dock = DockStyle.Bottom };

            var main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            main.Controls.Add(left, 0, 0);
            main.Controls.Add(tbContent, 1, 0);
            main.Controls.Add(tbWorking, 1, 1);
            main.SetRowSpan(left, 2);
            Controls.Add(main);
            Width = 700;
            Height = 400;
        }

        void BtnFolder_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbFolder.Text) || tbFolder.Text == "Folder Name Here")
            {
                MessageBox.Show("Set Folder Name First and Click FOLDER.");
                return;
            }
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    workDir = Path.Combine(fbd.SelectedPath, tbFolder.Text);
                    if (!Directory.Exists(workDir))
                        Directory.CreateDirectory(workDir);
                    tbWorking.Text = "Working Location : " + workDir;
                }
            }
        }

        void BtnNew_Click(object sender, EventArgs e)
        {
            var ts = DateTime.Now.ToString("yyMMddHHmm");
            tbFile.Text = "note" + ts + ".txt";
        }

        void BtnOpen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(workDir) || string.IsNullOrEmpty(tbFile.Text))
            {
                MessageBox.Show("Set folder and note file first.");
                return;
            }
            var path = Path.Combine(workDir, tbFile.Text);
            if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            else
                MessageBox.Show("Note file not found.");
        }

        void BtnExport_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(workDir) || string.IsNullOrEmpty(tbFile.Text))
            {
                MessageBox.Show("Set folder and note file first.");
                return;
            }
            var path = Path.Combine(workDir, tbFile.Text);
            File.AppendAllText(path, tbContent.Text + Environment.NewLine);
            tbContent.Clear();
            tbContent.Text = "Content Exported to\n" + path + "\nPress RESET.";
        }

        void BtnPaste_Click(object sender, EventArgs e)
        {
            var refText = tbRef.Text + "{" + tbPage.Text + "}\n";
            if (Clipboard.ContainsText())
            {
                var clipText = Clipboard.GetText().Replace("-\n", string.Empty).Replace("\n", " ");
                tbContent.AppendText(refText + clipText + Environment.NewLine + Environment.NewLine);
                Clipboard.Clear();
            }
            else
            {
                MessageBox.Show("Clipboard is Empty.");
            }
        }

        void BtnClipOCR_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(workDir) || string.IsNullOrEmpty(tbFile.Text))
            {
                MessageBox.Show("Set folder and note file first.");
                return;
            }
            if (Clipboard.ContainsImage())
            {
                using (var img = Clipboard.GetImage())
                using (var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default))
                {
                    using (var pix = PixConverter.ToPix(new Bitmap(img)))
                    using (var page = engine.Process(pix))
                    {
                        var text = page.GetText().Replace("-\n", string.Empty).Replace("\n", " ");
                        var refText = tbRef.Text + "{" + tbPage.Text + "}\n";
                        tbContent.AppendText(refText + text + Environment.NewLine + Environment.NewLine);
                        Clipboard.Clear();
                    }
                }
            }
            else
            {
                MessageBox.Show("No image on Clipboard found.");
            }
        }

        void BtnOCRRead_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(workDir) || string.IsNullOrEmpty(tbFile.Text))
            {
                MessageBox.Show("Set folder and note file first.");
                return;
            }

            var images = Directory.GetFiles(workDir, "*.png");
            int count = 0;
            using (var engine = new TesseractEngine("./tessdata", "eng", EngineMode.Default))
            {
                foreach (var file in images)
                {
                    using (var pix = Pix.LoadFromFile(file))
                    using (var page = engine.Process(pix))
                    {
                        var text = page.GetText().Replace("-\n", string.Empty).Replace("\n", " ");
                        var refText = tbRef.Text + "{" + tbPage.Text + "}\n";
                        tbContent.AppendText(refText + text + Environment.NewLine + Environment.NewLine);
                        count++;
                    }
                }
            }

            if (count == 0)
                MessageBox.Show("No Image Files Found.");
            else
                MessageBox.Show(count + " Files Read.");
        }

        void ChangePage(int delta)
        {
            if (int.TryParse(tbPage.Text, out int pg))
            {
                pg += delta;
                tbPage.Text = pg.ToString();
            }
            else
            {
                MessageBox.Show("Invalid Input.");
                tbPage.Text = "Pgs.";
            }
        }
    }
}
