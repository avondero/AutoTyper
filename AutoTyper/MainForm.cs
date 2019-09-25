namespace AutoTyper
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using System.Xml.Linq;

    using global::AutoTyper.Properties;

    #endregion

    /// <summary>
    ///     Form principale.
    /// </summary>
    public partial class MainForm : Form
    {
        #region Champs et constantes statiques

        private const string MsgboxTitle = "AutoTyper";

        private static readonly List<string> FunctionKeys = new List<string> { "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12" };

        #endregion

        #region Champs

        private readonly string scenario;

        private string[] autoTypedText;

        private AutoTyper typer;

        #endregion

        #region Constructeurs et destructeurs

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        /// <param name="scenario">Scénario.</param>
        public MainForm(string scenario)
        {
            this.InitializeComponent();

            this.Text = $@"AutoTyper (V{Assembly.GetExecutingAssembly().GetName().Version})";
            this.scenario = scenario;
        }

        #endregion

        #region Méthodes protected

        /// <summary>
        ///     Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.typer?.Dispose();
                this.components?.Dispose();
                this.lblInfo?.Dispose();
                this.cboKey?.Dispose();
                this.btnLoadScenarii?.Dispose();
                this.rtbTextToType?.Dispose();
                this.niTaskBar?.Dispose();
                this.mnuNotifyIcon?.Dispose();
                this.mnuQuit?.Dispose();
                this.mnuLoadScenarii?.Dispose();
                this.mnuOpenWindow?.Dispose();
                this.txtNbLettersTyped?.Dispose();
                this.label1?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region Méthodes privées

        private void BtnLoadScenarii_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.CheckFileExists = true;
                dlg.CheckPathExists = true;
                dlg.Title = @"Choose file containing autotyper scenarii";
                dlg.Filter = @"Xml files (*.xml)|*.xml|All files (*.*)|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    this.InitializeAutoTyper(dlg.FileName);
                }
            }
        }

        private void CboKey_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.RemoveSelectionColor();
            this.rtbTextToType.Text = this.autoTypedText[this.cboKey.SelectedIndex];
        }

        private bool InitializeAutoTyper(string file)
        {
            try
            {
                this.Typer_Stopped(null, null);

                // Dispose old autotyper first
                this.typer?.Dispose();

                var doc = XDocument.Load(file);
                this.autoTypedText = new string[FunctionKeys.Count];
                if (doc.Root != null)
                {
                    foreach (var eltKey in doc.Root.Elements("Key"))
                    {
                        var key = eltKey.Attribute("value")?.Value;
                        var numKey = FunctionKeys.IndexOf(key);
                        if (numKey < 0 || numKey > 11)
                        {
                            continue;
                        }

                        // Reading CDATA information
                        var cdata = (from n in eltKey.Nodes() where n is XCData select n).FirstOrDefault();
                        if (cdata != null)
                        {
                            this.autoTypedText[numKey] = (cdata as XCData)?.Value.Replace("\n", "\r\n");
                        }
                    }
                }

                this.cboKey.SelectedIndex = 0;
                this.typer = new AutoTyper(this.autoTypedText);
                this.typer.Started += this.Typer_Started;
                this.typer.Stopped += this.Typer_Stopped;
                this.typer.KeyStroke += this.Typer_KeyStroke;
                this.typer.NbOfLettersTypedChanged += this.Typer_NbOfLettersTypedChanged;
                this.RemoveSelectionColor();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while reading config file '{file}'\r\n{ex.Message}", MsgboxTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!this.InitializeAutoTyper(this.scenario))
            {
                this.Close();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Debug.WriteLine("MainForm_Resize : FormWindowState = " + this.WindowState);
            switch (this.WindowState)
            {
                case FormWindowState.Minimized:
                    this.ShowInTaskbar = false;
                    break;
                case FormWindowState.Normal:
                    this.ShowInTaskbar = true;
                    break;
            }
        }

        private void MnuOpenWindow_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
        }

        private void MnuQuit_Click(object sender, EventArgs e)
        {
            this.typer.Dispose();
            Application.Exit();
        }

        private void NiTaskBar_DoubleClick(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Show();
        }

        private void RemoveSelectionColor()
        {
            this.rtbTextToType.SelectAll();
            this.rtbTextToType.SelectionBackColor = this.rtbTextToType.BackColor;
        }

        private void TxtNbLettersTyped_ValueChanged(object sender, EventArgs e)
        {
            this.typer.NbOfLettersTyped = (int)this.txtNbLettersTyped.Value;
        }

        private void Typer_KeyStroke(object sender, int e)
        {
            this.rtbTextToType.Select(0, e);
            this.rtbTextToType.SelectionBackColor = Color.Yellow;
        }

        private void Typer_NbOfLettersTypedChanged(object sender, int e)
        {
            this.txtNbLettersTyped.Value = e;
        }

        private void Typer_Started(object sender, int e)
        {
            this.lblInfo.Text = "AutoTyper started. Change scenario using Ctrl + Shift + function key(F1 - F12)";
            this.cboKey.SelectedIndex = e;
            this.cboKey.Enabled = false;
            this.rtbTextToType.SelectAll();
            this.rtbTextToType.SelectionBackColor = this.rtbTextToType.BackColor;
            var firstChars = this.rtbTextToType.Text.Length > 20 ? this.rtbTextToType.Text.Substring(0, 20) + "..." : this.rtbTextToType.Text;
            this.niTaskBar.Text = $"AutoTyper - Started using F{(e + 1).ToString()}\n{firstChars}";
            switch (e)
            {
                case 0:
                    this.niTaskBar.Icon = Resources.F1;
                    break;
                case 1:
                    this.niTaskBar.Icon = Resources.F2;
                    break;
                case 2:
                    this.niTaskBar.Icon = Resources.F3;
                    break;
                case 3:
                    this.niTaskBar.Icon = Resources.F4;
                    break;
                case 4:
                    this.niTaskBar.Icon = Resources.F5;
                    break;
                case 5:
                    this.niTaskBar.Icon = Resources.F6;
                    break;
                case 6:
                    this.niTaskBar.Icon = Resources.F7;
                    break;
                case 7:
                    this.niTaskBar.Icon = Resources.F8;
                    break;
                case 8:
                    this.niTaskBar.Icon = Resources.F9;
                    break;
                case 9:
                    this.niTaskBar.Icon = Resources.F10;
                    break;
                case 10:
                    this.niTaskBar.Icon = Resources.F11;
                    break;
                case 11:
                    this.niTaskBar.Icon = Resources.F12;
                    break;
            }
        }

        private void Typer_Stopped(object sender, EventArgs e)
        {
            this.lblInfo.Text = "AutoTyper stopped.Start scenario using Ctrl + Shift + function key(F1 - F12)";
            this.niTaskBar.Text = "AutoTyper - Stopped";
            this.niTaskBar.Icon = Resources.AutoTyper;
            this.cboKey.Enabled = true;
        }

        #endregion
    }
}
