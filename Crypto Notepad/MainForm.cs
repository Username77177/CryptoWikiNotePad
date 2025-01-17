﻿using Crypto_Notepad.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Crypto_Notepad
{
    public partial class MainForm : Form
    {
        Properties.Settings settings = Settings.Default;
        readonly string[] args = Environment.GetCommandLineArgs();
        bool preventExit = false;
        string filePath = "";
        string argsPath = "";
        int findPos = 0;

        public MainForm()
        {
            InitializeComponent();
            richTextBox.DragDrop += new DragEventHandler(RichTextBox_DragDrop);
            richTextBox.AllowDrop = true;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x112;
            const int SC_MINIMIZE = 0xF020;

            if (m.Msg == WM_SYSCOMMAND && m.WParam.ToInt32() == SC_MINIMIZE && settings.autoLock && PublicVar.encryptionKey.Get() != null)
            {
                SaveMainMenu_Click(this, new EventArgs());
                AutoLock(true);
                return;
            }

            base.WndProc(ref m);
        }


        #region Methods
        private void DecryptAES()
        {
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this
            };
            enterKeyForm.ShowDialog();

            if (!PublicVar.okPressed)
            {
                PublicVar.openFileName = Path.GetFileName(filePath);
                return;
            }
            if (searchPanel.Visible)
            {
                FindMainMenu_Click(this, new EventArgs());
            }
            try
            {
                string opnfile = File.ReadAllText(openFileDialog.FileName);
                string NameWithotPath = Path.GetFileName(openFileDialog.FileName);
                string de;

                de = AES.Decrypt(opnfile, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
                richTextBox.Text = de;
                Text = PublicVar.appName + " – " + NameWithotPath;
                filePath = openFileDialog.FileName;
                PublicVar.openFileName = Path.GetFileName(openFileDialog.FileName);
                PublicVar.encryptionKey.Set(TypedPassword.Value);
                TypedPassword.Value = null;
            }
            catch (CryptographicException)
            {
                using (new CenterWinDialog(this))
                {
                    TypedPassword.Value = null;
                    DialogResult dialogResult = MessageBox.Show("Invalid key!", PublicVar.appName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Retry)
                    {
                        DecryptAES();
                    }
                    if (dialogResult == DialogResult.Cancel)
                    {
                        PublicVar.openFileName = Path.GetFileName(filePath);
                        return;
                    }

                }
            }
        }

        private void OpenAsotiations()
        {
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this,
                StartPosition = FormStartPosition.CenterScreen
            };
            string fileExtension = Path.GetExtension(args[1]);
            PublicVar.openFileName = Path.GetFileName(args[1]);
            if (fileExtension == ".cnp")
            {
                try
                {
                    string NameWithotPath = Path.GetFileName(args[1]);
                    string opnfile = File.ReadAllText(args[1]);

                    enterKeyForm.ShowDialog();
                    if (!PublicVar.okPressed)
                    {
                        openFileDialog.FileName = "";
                        return;
                    }
                    PublicVar.okPressed = false;

                    string de = AES.Decrypt(opnfile, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
                    richTextBox.Text = de;
                    Text = PublicVar.appName + " – " + NameWithotPath;
                    filePath = args[1];
                    PublicVar.encryptionKey.Set(TypedPassword.Value);
                    TypedPassword.Value = null;
                }
                catch (CryptographicException)
                {
                    TypedPassword.Value = null;
                    DialogResult dialogResult = MessageBox.Show("Invalid key!", PublicVar.appName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Retry)
                    {
                        OpenAsotiations();
                    }
                }

            }
            else
            {
                string opnfile = File.ReadAllText(args[1]);
                string NameWithotPath = Path.GetFileName(args[1]);
                richTextBox.Text = opnfile;
                Text = PublicVar.appName + " – " + NameWithotPath;
            }
        }

        private void SendTo()
        {
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this,
                StartPosition = FormStartPosition.CenterScreen
            };
            string fileExtension = Path.GetExtension(argsPath);

            if (fileExtension == ".cnp")
            {
                try
                {
                    string NameWithotPath = Path.GetFileName(argsPath);
                    string opnfile = File.ReadAllText(argsPath);
                    PublicVar.openFileName = Path.GetFileName(argsPath);

                    enterKeyForm.ShowDialog();
                    if (!PublicVar.okPressed)
                    {
                        openFileDialog.FileName = "";
                        return;
                    }
                    PublicVar.okPressed = false;
                    string de = AES.Decrypt(opnfile, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
                    richTextBox.Text = de;
                    Text = PublicVar.appName + " – " + NameWithotPath;
                    filePath = argsPath;
                    PublicVar.encryptionKey.Set(TypedPassword.Value);
                    TypedPassword.Value = null;
                }
                catch (CryptographicException)
                {
                    TypedPassword.Value = null;
                    DialogResult dialogResult = MessageBox.Show("Invalid key!", PublicVar.appName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Retry)
                    {
                        SendTo();
                    }
                }

            }
            else
            {
                string opnfile = File.ReadAllText(argsPath);
                string NameWithotPath = Path.GetFileName(argsPath);
                richTextBox.Text = opnfile;
                Text = PublicVar.appName + " – " + NameWithotPath;
            }
        }

        private void ContextMenuEncryptReplace()
        {
            if (args[1].Contains(".cnp"))
            {
                MessageBox.Show("Looks like this file is already encrypted", PublicVar.appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult res = MessageBox.Show("This action will delete the source file and replace it with encrypted version", PublicVar.appName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

            if (res == DialogResult.Cancel)
            {
                Environment.Exit(0);
            }

            if (!args[1].Contains(".cnp"))
            {
                string opnfile = File.ReadAllText(args[1]);
                richTextBox.Text = opnfile;
                PublicVar.openFileName = Path.GetFileName(args[1]);
                string newFile = Path.GetDirectoryName(args[1]) + @"\" + Path.GetFileNameWithoutExtension(args[1]) + ".cnp";
                EnterKeyForm enterKeyForm = new EnterKeyForm
                {
                    Owner = this
                };
                enterKeyForm.ShowDialog();
                if (!PublicVar.okPressed)
                {
                    Application.Exit();
                }
                PublicVar.okPressed = false;
                File.Delete(args[1]);
                string noenc = richTextBox.Text;
                string en;
                en = AES.Encrypt(richTextBox.Text, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
                richTextBox.Text = en;
                StreamWriter sw = new StreamWriter(newFile);
                int i = richTextBox.Lines.Count();
                int j = 0;
                i = i - 1;
                while (j <= i)
                {
                    sw.WriteLine(richTextBox.Lines.GetValue(j).ToString());
                    j = j + 1;
                }
                sw.Close();
                PublicVar.encryptionKey.Set(TypedPassword.Value);
                TypedPassword.Value = null;
                filePath = newFile;
                PublicVar.openFileName = Path.GetFileName(newFile);
                Text = PublicVar.appName + " – " + PublicVar.openFileName;
                richTextBox.Text = noenc;
            }
            richTextBox.Modified = false;
            if (PublicVar.okPressed)
            {
                PublicVar.okPressed = false;
            }
        }

        private void ContextMenuEncrypt()
        {
            if (!args[1].Contains(".cnp"))
            {
                string opnfile = File.ReadAllText(args[1]);
                string NameWithotPath = Path.GetFileName(args[1]);
                richTextBox.Text = opnfile;
                Text = PublicVar.appName + NameWithotPath;
                PublicVar.openFileName = Path.GetFileName(args[1]);
                filePath = openFileDialog.FileName;
            }

            richTextBox.Modified = false;

            if (PublicVar.okPressed)
            {
                PublicVar.okPressed = false;
            }
        }

        private void DeleteUpdateFiles()
        {
            string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
            string UpdaterExe = exePath + "Updater.exe";
            string UpdateZip = exePath + "Crypto-Notepad-Update.zip";
            string ZipDll = exePath + "Ionic.Zip.dll";

            if (File.Exists(UpdaterExe))
            {
                File.Delete(UpdaterExe);
            }

            if (File.Exists(UpdateZip))
            {
                File.Delete(UpdateZip);
            }

            if (File.Exists(ZipDll))
            {
                File.Delete(ZipDll);
            }
        }

        private void SaveConfirm(bool exit)
        {
            if (!richTextBox.Modified)
            {
                if (exit)
                {
                    Environment.Exit(0);
                }
            }
            else
            {
                if (PublicVar.openFileName == null)
                {
                    PublicVar.openFileName = "Unnamed.cnp";
                }

                if (PublicVar.openFileName == String.Empty)
                {
                    PublicVar.openFileName = "Unnamed.cnp";
                }

                if (richTextBox.Text != "")
                {
                    string messageBoxText;
                    if (!PublicVar.keyChanged)
                    {
                        messageBoxText = "Save file: " + "\"" + PublicVar.openFileName + "\"" + " ? ";
                    }
                    else
                    {
                        messageBoxText = "Save file: " + "\"" + PublicVar.openFileName + "\"" + " with a new key? ";
                    }

                    using (new CenterWinDialog(this))
                    {
                        DialogResult res = MessageBox.Show(messageBoxText, PublicVar.appName, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (res == DialogResult.Yes)
                        {
                            SaveMainMenu_Click(this, new EventArgs());
                            if (exit)
                            {
                                Environment.Exit(0);
                            }
                        }

                        if (res == DialogResult.No)
                        {
                            if (exit)
                            {
                                Environment.Exit(0);
                            }
                        }

                        if (res == DialogResult.Cancel)
                        {
                            preventExit = true;
                        }
                    }
                }
            }
        }

        private void CheckForUpdates(bool autoCheck)
        {
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead("https://raw.githubusercontent.com/Crypto-Notepad/Crypto-Notepad/master/version.txt");
                StreamReader reader = new StreamReader(stream);
                string content = reader.ReadToEnd();
                string version = Application.ProductVersion;
                string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
                int appVersion = Convert.ToInt32(version.Replace(".", "")), serverVersion = Convert.ToInt32(content.Replace(".", ""));

                if (serverVersion > appVersion)
                {
                    if (statusPanel.Visible)
                    {
                        StatusPanelMessage("update-needed");
                    }
                    else
                    {
                        using (new CenterWinDialog(this))
                        {
                            DialogResult res = MessageBox.Show("New version is available. Install it now?", PublicVar.appName, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (res == DialogResult.Yes)
                            {
                                File.WriteAllBytes(exePath + "Ionic.Zip.dll", Resources.Ionic_Zip);
                                File.WriteAllBytes(exePath + "Updater.exe", Resources.Updater);
                                var pr = new Process();
                                pr.StartInfo.FileName = exePath + "Updater.exe";
                                pr.StartInfo.Arguments = "/u";
                                pr.Start();
                                Application.Exit();
                            }
                        }
                    }
                }

                if (serverVersion <= appVersion && autoCheck)
                {
                    using (new CenterWinDialog(this))
                    {
                        if (statusPanel.Visible)
                        {
                            StatusPanelMessage("update-missing");
                        }
                        else
                        {
                            MessageBox.Show("Crypto Notepad is up to date.", PublicVar.appName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch
            {
                if (autoCheck)
                {
                    mainMenu.Invoke((Action)delegate
                    {
                        using (new CenterWinDialog(this))
                        {
                            if (statusPanel.Visible)
                            {
                                StatusPanelMessage("update-failed");
                            }
                            else
                            {
                                MessageBox.Show("Checking for updates failed:\nConnection lost or the server is busy.", PublicVar.appName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    });
                }
            }
        }

        private async void StatusPanelMessage(string type)
        {
            string ready = "Ready";

            if (statusLabel.Text == "New version is available")
            {
                ready = "New version is available";
            }

            switch (type)
            {
                case "save":
                    if (statusLabel.Text != "File Saved")
                    {
                        statusLabel.Text = "File Saved";
                        await Task.Delay(3000);
                        statusLabel.Text = ready;
                    }
                    break;
                case "update-missing":
                    statusLabel.Text = "Crypto Notepad is up to date";
                    await Task.Delay(3000);
                    statusLabel.Text = ready;
                    break;
                case "update-failed":
                    statusLabel.Text = "Checking for updates failed";
                    await Task.Delay(3000);
                    statusLabel.Text = ready;
                    break;
                case "update-needed":
                    statusLabel.Text = "New version is available";
                    break;
            }
        }

        private void StatusPanelTextInfo()
        {
            int currentColumn = 1 + richTextBox.SelectionStart - richTextBox.GetFirstCharIndexOfCurrentLine();
            int currentLine = 0;
            using (RichTextBox rtb = new RichTextBox() { WordWrap = false, Text = richTextBox.Text })
            {
                currentLine = 1 + rtb.GetLineFromCharIndex(richTextBox.SelectionStart);
            }
            int linesCount = richTextBox.Lines.Count();
            if (linesCount == 0)
            {
                linesCount = 1;
            }

            lengthStatusLabel.Text = "Length: " + richTextBox.TextLength;
            linesStatusLabel.Text = "Lines: " + linesCount;
            lnStatusLabel.Text = "Ln: " + currentLine;
            colStatusLabel.Text = "Col: " + currentColumn;
        }

        private void AutoLock(bool minimize)
        {
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this
            };
            PublicVar.encryptionKey.Set(null);
            int caretPos = richTextBox.SelectionStart;
            enterKeyForm.MinimizeBox = true;
            Hide();

            if (minimize)
            {
                enterKeyForm.WindowState = FormWindowState.Minimized;
            }
            enterKeyForm.ShowDialog();

            if (!PublicVar.okPressed)
            {
                PublicVar.encryptionKey.Set(null);
                richTextBox.Clear();
                Text = PublicVar.appName;
                PublicVar.openFileName = null;
                filePath = "";
                Show();
                return;
            }
            PublicVar.okPressed = false;
            try
            {
                richTextBox.Clear();
                string opnfile = File.ReadAllText(filePath);
                string de = AES.Decrypt(opnfile, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
                richTextBox.Text = de;
                Text = PublicVar.appName + " – " + PublicVar.openFileName;
                richTextBox.SelectionStart = caretPos;
                PublicVar.encryptionKey.Set(TypedPassword.Value);
                TypedPassword.Value = null;
                Show();
            }
            catch (Exception ex)
            {
                if (ex is CryptographicException)
                {
                    TypedPassword.Value = null;
                    DialogResult dialogResult = MessageBox.Show("Invalid key!", PublicVar.appName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
                    if (dialogResult == DialogResult.Retry)
                    {
                        AutoLock(false);
                    }
                    if (dialogResult == DialogResult.Cancel)
                    {
                        PublicVar.encryptionKey.Set(null);
                        richTextBox.Clear();
                        Text = PublicVar.appName;
                        filePath = "";
                        PublicVar.openFileName = null;
                        Show();
                        return;
                    }
                }
            }
        }

        private void LoadSettings()
        {
            if (settings.editorRightToLeft)
            {
                richTextBox.RightToLeft = RightToLeft.Yes;
                RTBLineNumbers.Dock = DockStyle.Right;
                rightToLeftContextMenu.Checked = true;
            }
            else
            {
                richTextBox.RightToLeft = RightToLeft.No;
                RTBLineNumbers.Dock = DockStyle.Left;
                rightToLeftContextMenu.Checked = false;
            }

            if (settings.insKey == "Disable")
            {
                insMainMenu.ShortcutKeys = Keys.Insert;
            }
            else
            {
                insMainMenu.ShortcutKeys = Keys.None;
            }

            if (settings.autoCheckUpdate)
            {
                CheckForUpdates(false);
            }

            if (settings.windowLocation.ToString() != "{X=0,Y=0}")
            {
                Location = settings.windowLocation;
            }
            Size = settings.windowSize;
            WindowState = settings.windowState;

            if (settings.toolbarBorder)
            {
                toolbarPanel.BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                toolbarPanel.BorderStyle = BorderStyle.None;
            }

            wordWrapMainMenu.Checked = settings.editorWrap;

            toolbarPanel.BackColor = settings.toolbarBackColor;
            toolbarPanel.Visible = settings.toolbarVisible;

            mainMenu.Visible = settings.mainMenuVisible;
            rightToLeftContextMenu.Checked = settings.editorRightToLeft;

            RTBLineNumbers.BackColor = settings.lnBackColor;
            RTBLineNumbers.Font = settings.editorFont;
            RTBLineNumbers.ForeColor = settings.lnForeColor;

            statusPanel.ForeColor = settings.statusPanelFontColor;
            statusPanel.BackColor = settings.statusPanelBackColor;
            statusPanel.Visible = settings.statusPanelVisible;

            richTextBox.WordWrap = settings.editorWrap;
            richTextBox.ForeColor = settings.editroForeColor;
            richTextBox.BackColor = settings.editorBackColor;
            richTextBox.Font = settings.editorFont;
            BackColor = settings.editorBackColor;

            searchPanel.BackColor = settings.searchPanelBackColor;
            searchPanel.ForeColor = settings.searchPanelForeColor;
            searchTextBox.BackColor = settings.searchPanelBackColor;
            searchTextBox.ForeColor = settings.searchPanelForeColor;
            caseSensitiveCheckBox.ForeColor = settings.searchPanelForeColor;
            wholeWordCheckBox.ForeColor = settings.searchPanelForeColor;
            findNextButton.ForeColor = settings.searchPanelForeColor;

            RTBLineNumbers.Visible = bool.Parse(settings.lnVisible);
            RTBLineNumbers.Show_BorderLines = bool.Parse(settings.blShow);
            RTBLineNumbers.Show_GridLines = bool.Parse(settings.glShow);
            RTBLineNumbers.Show_MarginLines = bool.Parse(settings.mlVisible);
            RTBLineNumbers.GridLines_Color = settings.glColor;
            RTBLineNumbers.MarginLines_Color = settings.mlColor;
            RTBLineNumbers.BorderLines_Color = settings.blColor;
            RTBLineNumbers.BorderLines_Style = (DashStyle)Enum.Parse(typeof(DashStyle), settings.blStyle);
            RTBLineNumbers.GridLines_Style = (DashStyle)Enum.Parse(typeof(DashStyle), settings.glStyle);
            RTBLineNumbers.MarginLines_Style = (DashStyle)Enum.Parse(typeof(DashStyle), settings.mlStyle);
            RTBLineNumbers.MarginLines_Side = (LineNumbers.LineNumbers.LineNumberDockSide)Enum.Parse(typeof(LineNumbers.LineNumbers.LineNumberDockSide), settings.mlSide);
        }

        public void MenuIcons(bool menuIcons)
        {
            if (menuIcons)
            {
                newMainMenu.Image = Resources.document_plus;
                openMainMenu.Image = Resources.folder_open_document;
                saveMainMenu.Image = Resources.disk_return_black;
                saveAsMainMenu.Image = Resources.disks_black;
                fileLocationMainMenu.Image = Resources.folder_horizontal;
                deleteFileMainMenu.Image = Resources.document_minus;
                exitMainMenu.Image = Resources.cross_button;
                undoMainMenu.Image = Resources.arrow_left;
                redoMainMenu.Image = Resources.arrow_right;
                cutMainMenu.Image = Resources.scissors;
                copyMainMenu.Image = Resources.document_copy;
                pasteMainMenu.Image = Resources.clipboard;
                deleteMainMenu.Image = Resources.minus;
                findMainMenu.Image = Resources.magnifier;
                selectAllMainMenu.Image = Resources.selection_input;
                wordWrapMainMenu.Image = Resources.wrap_option;
                clearMainMenu.Image = Resources.document;
                changeKeyMainMenu.Image = Resources.key;
                lockMainMenu.Image = Resources.lock_warning;
                settingsMainMenu.Image = Resources.gear;
                docsMainMenu.Image = Resources.document_text;
                updatesMainMenu.Image = Resources.upload_cloud;
                aboutMainMenu.Image = Resources.information;
            }
            else
            {
                foreach (ToolStripItem item in mainMenu.Items)
                {
                    if (item is ToolStripDropDownItem)
                        foreach (ToolStripItem dropDownItem in ((ToolStripDropDownItem)item).DropDownItems)
                        {
                            dropDownItem.Image = null;
                        }
                }
            }
        }

        public void Toolbaricons(bool oldIcons)
        {
            if (oldIcons)
            {
                newToolbarButton.Image = Resources.old_page_white_add;
                openToolbarButton.Image = Resources.old_folder_vertical_document;
                saveToolbarButton.Image = Resources.old_diskette;
                fileLocationToolbarButton.Image = Resources.old_folder_stand;
                deleteFileToolbarButton.Image = Resources.old_page_white_delete;
                cutToolbarButton.Image = Resources.old_cut_red;
                copyToolbarButton.Image = Resources.old_page_white_copy;
                pasteToolbarButton.Image = Resources.old_paste_plain;
                changeKeyToolbarButton.Image = Resources.old_page_white_key;
                lockToolbarButton.Image = Resources.old_lock;
                settingsToolbarButton.Image = Resources.old_setting_tools;
            }
            else
            {
                newToolbarButton.Image = Resources.document_plus;
                openToolbarButton.Image = Resources.folder_open_document;
                saveToolbarButton.Image = Resources.disk_return_black;
                fileLocationToolbarButton.Image = Resources.folder_horizontal;
                deleteFileToolbarButton.Image = Resources.document_minus;
                cutToolbarButton.Image = Resources.scissors;
                copyToolbarButton.Image = Resources.document_copy;
                pasteToolbarButton.Image = Resources.clipboard;
                changeKeyToolbarButton.Image = Resources.key;
                lockToolbarButton.Image = Resources.lock_warning;
                settingsToolbarButton.Image = Resources.gear;
            }

        }
        #endregion


        #region Event Handlers
        private void MainWindow_Activated(object sender, EventArgs e)
        {
            richTextBox.Focus();

            if (PublicVar.keyChanged)
            {
                richTextBox.Modified = true;
            }

            if (PublicVar.encryptionKey.Get() == null)
            {
                fileLocationToolbarButton.Enabled = false;
                deleteFileToolbarButton.Enabled = false;
                changeKeyToolbarButton.Enabled = false;
                lockToolbarButton.Enabled = false;
            }
            else
            {
                fileLocationToolbarButton.Enabled = true;
                deleteFileToolbarButton.Enabled = true;
                changeKeyToolbarButton.Enabled = true;
                lockToolbarButton.Enabled = true;
            }
            RTBLineNumbers.Refresh();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (WindowState == FormWindowState.Normal)
            {
                settings.windowSize = Size;
                settings.windowLocation = Location;
                settings.windowState = WindowState;
            }

            if (WindowState == FormWindowState.Maximized)
            {
                settings.windowState = WindowState;
            }
            settings.Save();

            SaveConfirm(true);

            if (preventExit)
            {
                e.Cancel = true;
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Visible = true;
            richTextBox.SetInnerMargins(Convert.ToInt32(settings.editorPaddingLeft), 0, 0, 0);
            richTextBox.Modified = false;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            Visible = false;

            LoadSettings();
            DeleteUpdateFiles();
            MenuIcons(settings.menuIcons);
            Toolbaricons(settings.oldToolbarIcons);

            if (args.Length == 2) /*drag & drop to executable*/
            {
                OpenAsotiations();
            }

            if (args.Contains("/s")) /*send to*/
            {
                foreach (var arg in args)
                {
                    argsPath = arg;
                }
                SendTo();
            }

            if (args.Contains("/o"))  /*decrypt & open cnp*/
            {
                OpenAsotiations();
            }

            if (args.Contains("/e")) /*encrypt*/
            {
                ContextMenuEncrypt();
            }

            if (args.Contains("/er")) /*encrypt and replace*/
            {
                ContextMenuEncryptReplace();
            }

#if DEBUG
            debugMainMenu.Visible = true;
#endif
        }

        private void RichTextBox_SelectionChanged(object sender, EventArgs e)
        {
            if (richTextBox.SelectionLength != 0)
            {
                cutToolbarButton.Enabled = true;
                copyToolbarButton.Enabled = true;
            }
            else
            {
                cutToolbarButton.Enabled = false;
                copyToolbarButton.Enabled = false;
            }
            StatusPanelTextInfo();
        }

        private void RichTextBox_Click(object sender, EventArgs e)
        {
            StatusPanelTextInfo();
        }

        private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                searchTextBox.Text = "";
                searchPanel.Visible = false;
                richTextBox.Focus();
                richTextBox.DeselectAll();
                e.Handled = e.SuppressKeyPress = true;
                findPos = 0;
            }

            if (e.KeyCode == Keys.Enter & searchPanel.Visible & searchTextBox.Text != "")
            {
                FindNextButton_Click(this, new EventArgs());
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void RichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (settings.openLinks == "LMB Click")
            {
                Process.Start(e.LinkText);
            }

            if (settings.openLinks == "Shift+LMB")
            {
                if ((ModifierKeys & Keys.Shift) != 0)
                {
                    Process.Start(e.LinkText);
                }
            }

            if (settings.openLinks == "Control+LMB")
            {
                if ((ModifierKeys & Keys.Control) != 0)
                {
                    Process.Start(e.LinkText);
                }
            }
        }

        private void RichTextBox_DragDrop(object sender, DragEventArgs e)
        {
            SaveConfirm(false);
            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            foreach (string file in FileList) openFileDialog.FileName = file;
            object fname = e.Data.GetData("FileDrop");
            PublicVar.openFileName = Path.GetFileName(openFileDialog.FileName);
            if (fname != null)
            {
                var list = fname as string[];
                if (list != null && !string.IsNullOrWhiteSpace(list[0]))
                {
                    if (preventExit)
                    {
                        preventExit = false;
                        return;
                    }

                    if (!openFileDialog.FileName.Contains(".cnp"))
                    {
                        string opnfile = File.ReadAllText(openFileDialog.FileName);
                        string NameWithotPath = Path.GetFileName(openFileDialog.FileName);
                        richTextBox.Text = opnfile;
                        Text = PublicVar.appName + " – " + NameWithotPath;
                        filePath = openFileDialog.FileName;
                        return;
                    }
                    DecryptAES();
                    if (PublicVar.okPressed)
                    {
                        PublicVar.okPressed = false;
                    }
                }
            }
            if (PublicVar.encryptionKey.Get() == null)
            {
                fileLocationToolbarButton.Enabled = false;
                deleteFileToolbarButton.Enabled = false;
                changeKeyToolbarButton.Enabled = false;
                lockToolbarButton.Enabled = false;
            }
            else
            {
                fileLocationToolbarButton.Enabled = true;
                deleteFileToolbarButton.Enabled = true;
                changeKeyToolbarButton.Enabled = true;
                lockToolbarButton.Enabled = true;
            }
        }

        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {
            StatusPanelTextInfo();
        }

        private void RichTextBox_CursorPositionChanged(object sender, EventArgs e)
        {
            StatusPanelTextInfo();
        }

        private void StatusLabel_TextChanged(object sender, EventArgs e)
        {
            if (statusLabel.Text == "New version is available")
            {
                statusLabel.IsLink = true;
            }
            else
            {
                statusLabel.IsLink = false;
            }
        }

        private void StatusLabel_Click(object sender, EventArgs e)
        {
            if (statusLabel.Text == "New version is available")
            {
                string exePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\";
                using (new CenterWinDialog(this))
                {
                    DialogResult res = MessageBox.Show("New version is available. Install it now?", PublicVar.appName, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (res == DialogResult.Yes)
                    {
                        File.WriteAllBytes(exePath + "Ionic.Zip.dll", Resources.Ionic_Zip);
                        File.WriteAllBytes(exePath + "Updater.exe", Resources.Updater);
                        var pr = new Process();
                        pr.StartInfo.FileName = exePath + "Updater.exe";
                        pr.StartInfo.Arguments = "/u";
                        pr.Start();
                        Application.Exit();
                    }
                }
            }
        }
        #endregion


        #region Main Menu
        /*File*/
        private void NewMainMenu_Click(object sender, EventArgs e)
        {
            SaveConfirm(false);
            PublicVar.openFileName = "Unnamed.cnp";
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this
            };
            enterKeyForm.ShowDialog();

            if (!PublicVar.okPressed)
            {
                if (filePath != "")
                {
                    PublicVar.openFileName = Path.GetFileName(filePath);
                }
                TypedPassword.Value = null;
                return;
            }
            else
            {
                saveFileDialog.FileName = "Unnamed.cnp";
                PublicVar.encryptionKey.Set(TypedPassword.Value);

                PublicVar.okPressed = false;
                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                {
                    TypedPassword.Value = null;
                    return;
                }

                richTextBox.Clear();
                StreamWriter sw = new StreamWriter(saveFileDialog.FileName);
                string NameWithotPath = Path.GetFileName(saveFileDialog.FileName);
                Text = PublicVar.appName + " – " + NameWithotPath;
                filePath = saveFileDialog.FileName;
                PublicVar.openFileName = Path.GetFileName(saveFileDialog.FileName);
                sw.Close();
            }
            TypedPassword.Value = null;
        }

        private void OpenMainMenu_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = "";
            SaveConfirm(false);

            if (preventExit)
            {
                preventExit = false;
                return;
            }

            if (openFileDialog.ShowDialog() != DialogResult.OK) return;
            {
                PublicVar.openFileName = Path.GetFileName(openFileDialog.FileName);
                if (!openFileDialog.FileName.Contains(".cnp"))
                {
                    string opnfile = File.ReadAllText(openFileDialog.FileName);
                    string NameWithotPath = Path.GetFileName(openFileDialog.FileName);
                    richTextBox.Text = opnfile;
                    Text = PublicVar.appName + " – " + NameWithotPath;
                    return;
                }

                DecryptAES();

                richTextBox.Modified = false;

                if (PublicVar.okPressed)
                {
                    PublicVar.okPressed = false;
                }

                if (PublicVar.encryptionKey.Get() == null)
                {
                    fileLocationToolbarButton.Enabled = false;
                    deleteFileToolbarButton.Enabled = false;
                    changeKeyToolbarButton.Enabled = false;
                    lockToolbarButton.Enabled = false;
                }
                else
                {
                    fileLocationToolbarButton.Enabled = true;
                    deleteFileToolbarButton.Enabled = true;
                    changeKeyToolbarButton.Enabled = true;
                    lockToolbarButton.Enabled = true;
                }
            }
        }

        private void SaveMainMenu_Click(object sender, EventArgs e)
        {
            if (PublicVar.encryptionKey.Get() == null)
            {
                SaveAsMainMenu_Click(this, new EventArgs());
                if (!PublicVar.okPressed)
                {
                    return;
                }
                PublicVar.okPressed = false;
            }
            string enc = AES.Encrypt(richTextBox.Text, PublicVar.encryptionKey.Get(), null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(enc);
                writer.Close();
            }
            richTextBox.Modified = false;
            PublicVar.keyChanged = false;
            StatusPanelMessage("save");
        }

        private void SaveAsMainMenu_Click(object sender, EventArgs e)
        {
            if (filePath != "")
            {
                PublicVar.openFileName = Path.GetFileName(filePath);
                saveFileDialog.FileName = Path.GetFileName(filePath);
            }
            else
            {
                PublicVar.openFileName = "Unnamed.cnp";
                saveFileDialog.FileName = "Unnamed.cnp";
            }
            EnterKeyForm enterKeyForm = new EnterKeyForm
            {
                Owner = this
            };

            if (string.IsNullOrEmpty(PublicVar.encryptionKey.Get()))
            {
                enterKeyForm.ShowDialog();
                if (!PublicVar.okPressed)
                {
                    return;
                }
                PublicVar.okPressed = false;
            }

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (TypedPassword.Value == null)
            {
                TypedPassword.Value = PublicVar.encryptionKey.Get();
            }

            filePath = saveFileDialog.FileName;
            string enc = AES.Encrypt(richTextBox.Text, TypedPassword.Value, null, settings.HashAlgorithm, Convert.ToInt32(settings.PasswordIterations), Convert.ToInt32(settings.KeySize));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.Write(enc);
                writer.Close();
            }

            richTextBox.Modified = false;
            Text = PublicVar.appName + " – " + Path.GetFileName(filePath);
            PublicVar.encryptionKey.Set(TypedPassword.Value);
            TypedPassword.Value = null;
            PublicVar.openFileName = Path.GetFileName(saveFileDialog.FileName);
            StatusPanelMessage("save");
        }

        private void FileLocationMainMenu_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", @"/select, " + filePath);
        }

        private void DeleteFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filePath != "")
            {
                using (new CenterWinDialog(this))
                {
                    if (MessageBox.Show("Delete file: " + "\"" + filePath + "\"" + " ?", PublicVar.appName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        File.Delete(filePath);
                        richTextBox.Clear();
                        PublicVar.encryptionKey.Set(null);
                        fileLocationToolbarButton.Enabled = false;
                        deleteFileToolbarButton.Enabled = false;
                        changeKeyToolbarButton.Enabled = false;
                        lockToolbarButton.Enabled = false;
                        filePath = "";
                        PublicVar.openFileName = null;
                        Text = PublicVar.appName;
                        return;
                    }
                }
            }
        }

        private void ExitMainMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void fileMainMenu_DropDownOpened(object sender, EventArgs e)
        {
            if (filePath == "")
            {
                fileLocationMainMenu.Enabled = false;
                deleteFileMainMenu.Enabled = false;
            }
            else
            {
                fileLocationMainMenu.Enabled = true;
                deleteFileMainMenu.Enabled = true;
            }
        }
        /*File*/

        /*Edit*/
        private void EditMainMenu_DropDownOpened(object sender, EventArgs e)
        {
            if (richTextBox.SelectionLength != 0)
            {
                cutMainMenu.Enabled = true;
                copyMainMenu.Enabled = true;
                deleteMainMenu.Enabled = true;
            }
            else
            {
                cutMainMenu.Enabled = false;
                copyMainMenu.Enabled = false;
                deleteMainMenu.Enabled = false;
            }
        }

        private void UndoMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.Undo();
            richTextBox.DeselectAll();
        }

        public void RedoMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.Redo();
        }

        private void CutMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.Cut();
        }

        private void CopyMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.Copy();
        }

        private void PasteMainMenu_Click(object sender, EventArgs e)
        {
            if (richTextBox.Focused)
            {
                richTextBox.Paste(DataFormats.GetFormat(DataFormats.Text));
            }
            if (searchTextBox.Focused)
            {
                searchTextBox.Paste();
            }
        }

        private void DeleteMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.SelectedText = "";
        }

        private void FindMainMenu_Click(object sender, EventArgs e)
        {
            if (searchPanel.Visible)
            {
                searchTextBox.Text = "";
                searchPanel.Visible = false;
                richTextBox.Focus();
                richTextBox.DeselectAll();
            }
            else
            {
                searchPanel.Visible = true;
                searchTextBox.Focus();
            }
        }

        private void SelectAllMainMenu_Click(object sender, EventArgs e)
        {
            if (richTextBox.Focused)
            {
                richTextBox.SelectAll();
            }
            if (searchTextBox.Focused)
            {
                searchTextBox.SelectAll();
            }
        }

        private void WordWrapMainMenu_Click(object sender, EventArgs e)
        {
            if (wordWrapMainMenu.Checked)
            {
                richTextBox.WordWrap = true;
            }
            else
            {
                richTextBox.WordWrap = false;
            }
            settings.menuWrap = wordWrapMainMenu.Checked;
            settings.editorWrap = richTextBox.WordWrap;
            settings.Save();
        }

        private void ClearMainMenu_Click(object sender, EventArgs e)
        {
            richTextBox.SelectAll();
            richTextBox.SelectedText = " ";
        }
        /*Edit*/

        /*Tools*/
        private void ToolsMainMenu_DropDownOpened(object sender, EventArgs e)
        {
            if (PublicVar.encryptionKey.Get() == null)
            {
                changeKeyMainMenu.Enabled = false;
                lockMainMenu.Enabled = false;
            }
            else
            {
                changeKeyMainMenu.Enabled = true;
                lockMainMenu.Enabled = true;
            }
        }

        private void ChangeKeyMainMenu_Click(object sender, EventArgs e)
        {
            ChangeKeyForm changeKeyForm = new ChangeKeyForm();
            changeKeyForm.ShowDialog(this);
        }

        private void LockMainMenu_Click(object sender, EventArgs e)
        {
            SaveMainMenu_Click(this, new EventArgs());
            AutoLock(false);
        }

        private void SettingsMainMenu_Click(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm
            {
                Owner = this
            };
            settingsForm.ShowDialog();
        }
        /*Tools*/

        /*Help*/
        private void DocsMainMenu_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Crypto-Notepad/Crypto-Notepad/wiki/Documentation");
        }

        private void UpdatesMainMenu_Click(object sender, EventArgs e)
        {
            CheckForUpdates(true);
        }

        private void AboutMainMenu_Click(object sender, EventArgs e)
        {
            AboutFrom aboutFrom = new AboutFrom();
            aboutFrom.ShowDialog(this);
        }
        /*Help*/
        #endregion


        #region Editor Menu
        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (richTextBox.SelectionLength != 0)
            {
                cutContextMenu.Enabled = true;
                copyContextMenu.Enabled = true;
                deleteContextMenu.Enabled = true;
            }
            else
            {
                cutContextMenu.Enabled = false;
                copyContextMenu.Enabled = false;
                deleteContextMenu.Enabled = false;
            }
        }

        private void UndoContextMenu_Click(object sender, EventArgs e)
        {
            UndoMainMenu_Click(this, new EventArgs());
        }

        private void RedoContextMenu_Click(object sender, EventArgs e)
        {
            RedoMainMenu_Click(this, new EventArgs());
        }

        private void CutContextMenu_Click(object sender, EventArgs e)
        {
            CutMainMenu_Click(this, new EventArgs());
        }

        private void CopyContextMenu_Click(object sender, EventArgs e)
        {
            CopyMainMenu_Click(this, new EventArgs());
        }

        private void PasteContextMenu_Click(object sender, EventArgs e)
        {
            PasteMainMenu_Click(this, new EventArgs());
        }

        private void DeleteContextMenu_Click(object sender, EventArgs e)
        {
            DeleteMainMenu_Click(this, new EventArgs());
        }

        private void SelectAllContextMenu_Click(object sender, EventArgs e)
        {
            SelectAllMainMenu_Click(this, new EventArgs());
        }

        private void RightToLeftContextMenu_Click(object sender, EventArgs e)
        {
            if (rightToLeftContextMenu.Checked)
            {
                if (!richTextBox.WordWrap)
                {
                    string rtbTxt = richTextBox.Text;
                    richTextBox.Clear();
                    richTextBox.RightToLeft = RightToLeft.Yes;
                    Application.DoEvents();
                    richTextBox.Text = rtbTxt;
                }
                else
                {
                    string rtbTxt = richTextBox.Text;
                    richTextBox.Clear();
                    richTextBox.RightToLeft = RightToLeft.Yes;
                    Application.DoEvents();
                    richTextBox.Text = rtbTxt;
                }
                settings.editorRightToLeft = true;
                RTBLineNumbers.Dock = DockStyle.Right;
                richTextBox.Modified = false;
            }
            else
            {
                richTextBox.RightToLeft = RightToLeft.No;
                settings.editorRightToLeft = false;
                RTBLineNumbers.Dock = DockStyle.Left;
                richTextBox.Modified = false;
            }
            settings.Save();
        }

        private void ClearContextMenu_Click(object sender, EventArgs e)
        {
            ClearMainMenu_Click(this, new EventArgs());
        }
        #endregion


        #region Toolbar
        private void NewToolbarButton_Click(object sender, EventArgs e)
        {
            NewMainMenu_Click(this, new EventArgs());
        }

        private void OpenToolbarButton_Click(object sender, EventArgs e)
        {
            OpenMainMenu_Click(this, new EventArgs());
        }

        private void SaveToolbarButton_Click(object sender, EventArgs e)
        {
            SaveMainMenu_Click(this, new EventArgs());
        }

        private void FileLocationToolbarButton_Click(object sender, EventArgs e)
        {
            FileLocationMainMenu_Click(this, new EventArgs());
        }

        private void DeleteFileToolbarButton_Click(object sender, EventArgs e)
        {
            DeleteFileToolStripMenuItem_Click(this, new EventArgs());
        }

        private void CutToolbarButton_Click(object sender, EventArgs e)
        {
            CutMainMenu_Click(this, new EventArgs());
        }

        private void CopyToolbarButton_Click(object sender, EventArgs e)
        {
            CopyMainMenu_Click(this, new EventArgs());
        }

        private void PasteToolbarButton_Click(object sender, EventArgs e)
        {
            PasteMainMenu_Click(this, new EventArgs());
        }

        private void ChangeKeyToolbarButton_Click(object sender, EventArgs e)
        {
            ChangeKeyMainMenu_Click(this, new EventArgs());
        }

        private void SettingsToolbarButton_Click(object sender, EventArgs e)
        {
            SettingsMainMenu_Click(this, new EventArgs());
        }

        private void LockToolbarButton_Click(object sender, EventArgs e)
        {
            LockMainMenu_Click(this, new EventArgs());
        }

        private void CloseToolbarButton_Click(object sender, EventArgs e)
        {
            toolbarPanel.Visible = false;
            settings.toolbarVisible = false;
        }

        private void CloseToolbarButton_MouseEnter(object sender, EventArgs e)
        {
            closeToolbarButton.Image = Resources.close_b;
        }

        private void CloseToolbarButton_MouseLeave(object sender, EventArgs e)
        {
            closeToolbarButton.Image = Resources.close_g;
        }
        #endregion


        #region Search Panel
        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            findPos = 0;
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                searchTextBox.Text = "";
                searchPanel.Visible = false;
                richTextBox.Focus();
                richTextBox.DeselectAll();
                e.Handled = e.SuppressKeyPress = true;
                findPos = 0;
            }

            if (e.KeyCode == Keys.Enter & searchPanel.Visible & searchTextBox.Text != "")
            {
                FindNextButton_Click(this, new EventArgs());
                e.Handled = e.SuppressKeyPress = true;
            }
        }

        private void CloseSearchPanel_Click(object sender, EventArgs e)
        {
            FindMainMenu_Click(this, new EventArgs());
        }

        private void CloseSearchPanel_MouseHover(object sender, EventArgs e)
        {
            closeSearchPanel.Image = Resources.close_b;
        }

        private void CloseSearchPanel_MouseLeave(object sender, EventArgs e)
        {
            closeSearchPanel.Image = Resources.close_g;
        }

        private void CaseSensitiveCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            findPos = 0;
            richTextBox.DeselectAll();
        }

        private void WholeWordCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            findPos = 0;
            richTextBox.DeselectAll();
        }

        private void FindText(string text, RichTextBoxFinds findOptions)
        {
            if (text.Length > 0)
            {
                try
                {
                    findPos = richTextBox.Find(searchTextBox.Text, findPos, findOptions);
                    if (findPos == -1)
                    {
                        findPos = 0;
                        searchTextBox.Focus();
                        return;
                    }
                    richTextBox.Focus();
                    richTextBox.Select(findPos, searchTextBox.Text.Length);
                    findPos += searchTextBox.Text.Length + 1;
                }
                catch
                {
                    findPos = 0;
                }
            }
        }

        private void FindNextButton_Click(object sender, EventArgs e)
        {
            if ((!wholeWordCheckBox.Checked) & (!caseSensitiveCheckBox.Checked))
            {
                FindText(searchTextBox.Text, RichTextBoxFinds.None);
                return;
            }

            if (wholeWordCheckBox.Checked & caseSensitiveCheckBox.Checked)
            {
                FindText(searchTextBox.Text, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                return;
            }

            if (caseSensitiveCheckBox.Checked)
            {
                FindText(searchTextBox.Text, RichTextBoxFinds.MatchCase);
                return;
            }

            if (wholeWordCheckBox.Checked)
            {
                FindText(searchTextBox.Text, RichTextBoxFinds.WholeWord);
                return;
            }
        }
        #endregion


        #region Debug Menu
        private void VariablesMainMenu_Click(object sender, EventArgs e)
        {
#if DEBUG
            string formattedTime = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss");
            Debug.WriteLine("\nTime: " + formattedTime);
            Debug.WriteLine("PublicVar.openFileName: " + PublicVar.openFileName);
            Debug.WriteLine("filePath: " + filePath);
            Debug.WriteLine("encryptionKey: " + PublicVar.encryptionKey.Get());
            Debug.WriteLine("TypedPassword: " + TypedPassword.Value);
            Debug.WriteLine("preventExit: " + preventExit);
            Debug.WriteLine("keyChanged: " + PublicVar.keyChanged);
            Debug.WriteLine("okPressed: " + PublicVar.okPressed);
            Debug.WriteLine("RichTextBox.Modified: " + richTextBox.Modified);
            Debug.WriteLine("EditorMenuStrip: " + contextMenu.Enabled);
#endif
        }

        #endregion


    }
}