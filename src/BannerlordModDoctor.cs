using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ModHealthCheckTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal sealed class MainForm : Form
    {
        private const string AppName = "Bannerlord Mod Doctor";
        private const string AppVersion = "1.0.0";
        private readonly TextBox _modulesPath;
        private readonly TextBox _workshopPath;
        private readonly CheckBox _includeWorkshop;
        private readonly CheckBox _enabledOnly;
        private readonly RichTextBox _reportBox;
        private readonly List<Control> _localizedControls = new List<Control>();
        private readonly List<ToolStripMenuItem> _languageMenuItems = new List<ToolStripMenuItem>();
        private ComboBox _languageBox;
        private Panel _modCheckView;
        private Panel _dllToolsView;
        private Panel _loadOrderView;
        private Label _navModCheck;
        private Label _navDllTools;
        private Label _navLoadOrder;
        private Label _missingCount;
        private Label _blockedCount;
        private Label _duplicateDllCount;
        private Label _xmlConflictCount;
        private string _language = "English";
        private string _lastReportPath;
        private string _lastReportKind = "Report";
        private string _lastLoadOrderPreview;

        public MainForm()
        {
            Text = AppName + " " + AppVersion;
            Width = 1280;
            Height = 840;
            MinimumSize = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(16, 20, 27);
            Font = new Font("Segoe UI", 9);

            MainMenuStrip = BuildMenu();
            Controls.Add(MainMenuStrip);

            Panel sidebar = NewPanel(0, 24, 220, 777, Color.FromArgb(20, 25, 34));
            sidebar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            Controls.Add(sidebar);

            Label sideTitle = new Label();
            sideTitle.Text = "+  MOD DOCTOR";
            sideTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            sideTitle.ForeColor = Color.White;
            sideTitle.AutoSize = true;
            sideTitle.Left = 22;
            sideTitle.Top = 22;
            sidebar.Controls.Add(sideTitle);

            Label sideSubtitle = new Label();
            sideSubtitle.Text = "Clean. Organize. Fix. Play.";
            sideSubtitle.Font = new Font("Segoe UI", 8);
            sideSubtitle.ForeColor = Color.FromArgb(184, 205, 255);
            sideSubtitle.AutoSize = true;
            sideSubtitle.Left = 24;
            sideSubtitle.Top = 57;
            sidebar.Controls.Add(sideSubtitle);

            _navModCheck = NewNavLabel("⌂  Mod Check", 28, 120, true);
            _navModCheck.Click += delegate { ShowView("ModCheck"); };
            sidebar.Controls.Add(_navModCheck);

            _navDllTools = NewNavLabel("▣  DLL Tools", 28, 178, false);
            _navDllTools.Click += delegate { ShowView("DllTools"); };
            sidebar.Controls.Add(_navDllTools);

            _navLoadOrder = NewNavLabel("☷  Load Order", 28, 236, false);
            _navLoadOrder.Click += delegate { ShowView("LoadOrder"); RefreshLoadOrderPreview(false); };
            sidebar.Controls.Add(_navLoadOrder);

            Panel ready = NewPanel(20, 690, 180, 70, Color.FromArgb(34, 42, 54));
            ready.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            sidebar.Controls.Add(ready);
            Label readyText = new Label();
            readyText.Text = "Ready\r\nGame folders detected";
            readyText.Left = 18;
            readyText.Top = 14;
            readyText.Width = 150;
            readyText.Height = 44;
            readyText.ForeColor = Color.FromArgb(151, 246, 190);
            readyText.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            ready.Controls.Add(readyText);

            Panel header = NewPanel(240, 42, 1018, 76, Color.FromArgb(25, 31, 41));
            header.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(header);
            Label title = new Label();
            title.Text = "⚕  " + AppName;
            title.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Left = 18;
            title.Top = 12;
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = L("Subtitle");
            subtitle.Tag = "Subtitle";
            _localizedControls.Add(subtitle);
            subtitle.Font = new Font("Segoe UI", 9);
            subtitle.ForeColor = Color.FromArgb(184, 205, 255);
            subtitle.AutoSize = true;
            subtitle.Left = 20;
            subtitle.Top = 47;
            header.Controls.Add(subtitle);

            Label version = new Label();
            version.Text = "v" + AppVersion + " Portable";
            version.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            version.ForeColor = Color.FromArgb(246, 222, 151);
            version.AutoSize = true;
            version.Left = 720;
            version.Top = 16;
            header.Controls.Add(version);

            _languageBox = new ComboBox();
            _languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _languageBox.Left = 750;
            _languageBox.Top = 42;
            _languageBox.Width = 240;
            _languageBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _languageBox.BackColor = Color.FromArgb(45, 52, 66);
            _languageBox.ForeColor = Color.FromArgb(235, 238, 245);
            _languageBox.Items.AddRange(new object[] { "English", "Spanish", "German", "French", "Portuguese", "Russian" });
            _languageBox.SelectedItem = _language;
            _languageBox.SelectedIndexChanged += delegate { SetLanguage(_languageBox.SelectedItem.ToString()); };
            header.Controls.Add(_languageBox);

            Panel folderPanel = NewPanel(240, 132, 1018, 98, Color.FromArgb(25, 31, 41));
            folderPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(folderPanel);
            folderPanel.Controls.Add(NewSectionLabel(L("GameFolders"), 18, 12));
            Label moduleLabel = NewLabel(L("ModulesFolder"), 18, 45);
            moduleLabel.Tag = "ModulesFolder";
            _localizedControls.Add(moduleLabel);
            folderPanel.Controls.Add(moduleLabel);
            _modulesPath = NewTextBox(DefaultModulesFolder(), 130, 41, 670);
            folderPanel.Controls.Add(_modulesPath);
            Button browseModules = NewButton(L("Browse"), 812, 39, 90, 30);
            browseModules.Tag = "Browse";
            _localizedControls.Add(browseModules);
            browseModules.Click += delegate { BrowseInto(_modulesPath); };
            folderPanel.Controls.Add(browseModules);
            Button openFolder = NewButton(L("Reports"), 914, 39, 82, 30);
            openFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openFolder.Tag = "Reports";
            _localizedControls.Add(openFolder);
            openFolder.Click += delegate { OpenReportsFolder(); };
            folderPanel.Controls.Add(openFolder);

            _includeWorkshop = new CheckBox();
            _includeWorkshop.Text = L("IncludeWorkshop");
            _includeWorkshop.Tag = "IncludeWorkshop";
            _localizedControls.Add(_includeWorkshop);
            _includeWorkshop.Checked = true;
            _includeWorkshop.Left = 750;
            _includeWorkshop.Top = 13;
            _includeWorkshop.Width = 240;
            _includeWorkshop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _includeWorkshop.ForeColor = Color.FromArgb(226, 231, 241);
            _includeWorkshop.BackColor = folderPanel.BackColor;
            folderPanel.Controls.Add(_includeWorkshop);

            _enabledOnly = new CheckBox();
            _enabledOnly.Text = "Only enabled mods";
            _enabledOnly.Checked = true;
            _enabledOnly.Left = 750;
            _enabledOnly.Top = 38;
            _enabledOnly.Width = 240;
            _enabledOnly.ForeColor = Color.FromArgb(226, 231, 241);
            _enabledOnly.BackColor = folderPanel.BackColor;
            folderPanel.Controls.Add(_enabledOnly);

            Label workshopLabel = NewLabel(L("WorkshopFolder"), 18, 72);
            workshopLabel.Tag = "WorkshopFolder";
            _localizedControls.Add(workshopLabel);
            folderPanel.Controls.Add(workshopLabel);
            _workshopPath = NewTextBox(DefaultWorkshopFolder(), 130, 68, 670);
            _workshopPath.Height = 22;
            folderPanel.Controls.Add(_workshopPath);
            Button browseWorkshop = NewButton(L("Browse"), 812, 66, 90, 26);
            browseWorkshop.Tag = "Browse";
            _localizedControls.Add(browseWorkshop);
            browseWorkshop.Click += delegate { BrowseInto(_workshopPath); };
            folderPanel.Controls.Add(browseWorkshop);

            _modCheckView = NewPanel(240, 246, 1018, 62, Color.FromArgb(16, 20, 27));
            _modCheckView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_modCheckView);
            _dllToolsView = NewPanel(240, 246, 1018, 62, Color.FromArgb(16, 20, 27));
            _dllToolsView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_dllToolsView);
            _loadOrderView = NewPanel(240, 246, 1018, 62, Color.FromArgb(16, 20, 27));
            _loadOrderView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_loadOrderView);

            Button scan = NewPrimaryButton(L("CheckMods"), 0, 0, 220, 62);
            scan.Tag = "CheckMods";
            _localizedControls.Add(scan);
            scan.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            scan.Click += delegate { RunScan(false, false); };
            _modCheckView.Controls.Add(scan);

            Button quick = NewButton(L("QuickCheck"), 238, 0, 180, 62);
            quick.Tag = "QuickCheck";
            _localizedControls.Add(quick);
            quick.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            quick.Click += delegate { RunScan(true, false); };
            _modCheckView.Controls.Add(quick);

            Button support = NewButton("□  Support Report", 438, 0, 180, 62);
            support.Click += delegate { RunSupportReport(); };
            _modCheckView.Controls.Add(support);

            Label modCheckHint = NewPanelText("Run a full scan first. The summary cards below update after each scan.", 638, 18, 350);
            _modCheckView.Controls.Add(modCheckHint);

            Button unblock = NewButton(L("FixBlocked"), 0, 0, 220, 62);
            unblock.Tag = "FixBlocked";
            _localizedControls.Add(unblock);
            unblock.Click += delegate { RunScan(false, true); };
            _dllToolsView.Controls.Add(unblock);

            Button helpers = NewButton(L("CleanDlls"), 238, 0, 220, 62);
            helpers.Tag = "CleanDlls";
            _localizedControls.Add(helpers);
            helpers.Click += delegate { RemoveSafeHelperDlls(); };
            _dllToolsView.Controls.Add(helpers);

            Button restore = NewButton(L("UndoCleanup"), 476, 0, 220, 62);
            restore.Tag = "UndoCleanup";
            _localizedControls.Add(restore);
            restore.Click += delegate { RestoreHelperDlls(); };
            _dllToolsView.Controls.Add(restore);

            Label dllHint = NewPanelText("DLL tools are reversible where possible. Cleanup moves duplicate DLLs to backup first.", 716, 14, 290);
            _dllToolsView.Controls.Add(dllHint);

            Button sorted = NewPrimaryButton(L("SortLoadOrder"), 0, 0, 220, 62);
            sorted.Tag = "SortLoadOrder";
            _localizedControls.Add(sorted);
            sorted.Click += delegate { RunSortedLoadOrder(); };
            _loadOrderView.Controls.Add(sorted);

            Button undoSort = NewButton("↶  Undo Load Order", 238, 0, 220, 62);
            undoSort.Click += delegate { UndoLoadOrderSort(); };
            _loadOrderView.Controls.Add(undoSort);

            Label loadOrderHint = NewPanelText("Shows detected current order and suggested dependency-based changes in Results.", 476, 18, 500);
            _loadOrderView.Controls.Add(loadOrderHint);

            Controls.Add(NewStatusCard("Missing dependencies", "0", Color.FromArgb(255, 176, 184), 240, 326, out _missingCount));
            Controls.Add(NewStatusCard("Blocked DLLs", "0", Color.FromArgb(246, 222, 151), 500, 326, out _blockedCount));
            Controls.Add(NewStatusCard("Duplicate DLLs", "0", Color.FromArgb(139, 239, 217), 760, 326, out _duplicateDllCount));
            Controls.Add(NewStatusCard("XML conflicts", "0", Color.FromArgb(214, 196, 255), 1020, 326, out _xmlConflictCount));

            Label resultsTitle = NewSectionLabel(L("Results"), 240, 450);
            resultsTitle.Tag = "Results";
            _localizedControls.Add(resultsTitle);
            Controls.Add(resultsTitle);

            Button copy = NewButton(L("CopyResults"), 1012, 444, 120, 30);
            copy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            copy.Tag = "CopyResults";
            _localizedControls.Add(copy);
            copy.Click += delegate { CopyResults(); };
            Controls.Add(copy);

            Button save = NewButton(L("Save"), 1142, 444, 116, 30);
            save.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            save.Tag = "Save";
            _localizedControls.Add(save);
            save.Click += delegate { SaveReportAs(); };
            Controls.Add(save);

            _reportBox = new RichTextBox();
            _reportBox.Left = 240;
            _reportBox.Top = 486;
            _reportBox.Width = 1018;
            _reportBox.Height = 240;
            _reportBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _reportBox.ReadOnly = true;
            _reportBox.ScrollBars = RichTextBoxScrollBars.Both;
            _reportBox.Font = new Font(FontFamily.GenericMonospace, 9);
            _reportBox.WordWrap = false;
            _reportBox.BackColor = Color.FromArgb(21, 26, 35);
            _reportBox.ForeColor = Color.FromArgb(226, 231, 241);
            _reportBox.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(_reportBox);

            Panel footer = NewPanel(240, 742, 1018, 48, Color.FromArgb(25, 31, 41));
            footer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(footer);
            Label donateNote = new Label();
            donateNote.Text = L("DonateNote");
            donateNote.Tag = "DonateNote";
            _localizedControls.Add(donateNote);
            donateNote.Left = 16;
            donateNote.Top = 15;
            donateNote.Width = 760;
            donateNote.Height = 22;
            donateNote.ForeColor = Color.FromArgb(246, 222, 151);
            donateNote.BackColor = footer.BackColor;
            footer.Controls.Add(donateNote);

            Button donate = NewPrimaryButton(L("Donate"), 820, 9, 180, 30);
            donate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            donate.Tag = "Donate";
            _localizedControls.Add(donate);
            donate.Click += delegate { OpenDonationLink(); };
            footer.Controls.Add(donate);
            ShowView("ModCheck");
        }
        private static Panel NewPanel(int left, int top, int width, int height, Color color)
        {
            Panel panel = new Panel();
            panel.Left = left;
            panel.Top = top;
            panel.Width = width;
            panel.Height = height;
            panel.BackColor = color;
            return panel;
        }

        private static Label NewNavLabel(string text, int left, int top, bool active)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = 160;
            label.Height = 34;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 10, active ? FontStyle.Bold : FontStyle.Regular);
            label.ForeColor = active ? Color.FromArgb(214, 196, 255) : Color.FromArgb(206, 214, 229);
            label.BackColor = active ? Color.FromArgb(52, 44, 79) : Color.FromArgb(20, 25, 34);
            label.Cursor = Cursors.Hand;
            return label;
        }

        private void ShowView(string view)
        {
            if (_modCheckView != null)
            {
                _modCheckView.Visible = string.Equals(view, "ModCheck", StringComparison.OrdinalIgnoreCase);
            }
            if (_dllToolsView != null)
            {
                _dllToolsView.Visible = string.Equals(view, "DllTools", StringComparison.OrdinalIgnoreCase);
            }
            if (_loadOrderView != null)
            {
                _loadOrderView.Visible = string.Equals(view, "LoadOrder", StringComparison.OrdinalIgnoreCase);
            }

            SetNavActive(_navModCheck, string.Equals(view, "ModCheck", StringComparison.OrdinalIgnoreCase));
            SetNavActive(_navDllTools, string.Equals(view, "DllTools", StringComparison.OrdinalIgnoreCase));
            SetNavActive(_navLoadOrder, string.Equals(view, "LoadOrder", StringComparison.OrdinalIgnoreCase));
        }

        private static void SetNavActive(Label label, bool active)
        {
            if (label == null)
            {
                return;
            }

            label.Font = new Font("Segoe UI", 10, active ? FontStyle.Bold : FontStyle.Regular);
            label.ForeColor = active ? Color.FromArgb(214, 196, 255) : Color.FromArgb(206, 214, 229);
            label.BackColor = active ? Color.FromArgb(52, 44, 79) : Color.FromArgb(20, 25, 34);
        }

        private static Label NewPanelText(string text, int left, int top, int width)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = width;
            label.Height = 42;
            label.ForeColor = Color.FromArgb(184, 205, 255);
            label.Font = new Font("Segoe UI", 9);
            return label;
        }

        private static Panel NewStatusCard(string title, string value, Color accent, int left, int top, out Label valueLabel)
        {
            Panel card = NewPanel(left, top, 238, 100, Color.FromArgb(25, 31, 41));
            valueLabel = new Label();
            valueLabel.Text = value;
            valueLabel.Left = 18;
            valueLabel.Top = 14;
            valueLabel.Width = 60;
            valueLabel.Height = 42;
            valueLabel.Font = new Font("Segoe UI", 22, FontStyle.Bold);
            valueLabel.ForeColor = accent;
            card.Controls.Add(valueLabel);

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Left = 82;
            titleLabel.Top = 22;
            titleLabel.Width = 140;
            titleLabel.Height = 42;
            titleLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(226, 231, 241);
            card.Controls.Add(titleLabel);

            Label note = new Label();
            note.Text = "Shown after scan";
            note.Left = 82;
            note.Top = 62;
            note.Width = 140;
            note.Height = 20;
            note.Font = new Font("Segoe UI", 8);
            note.ForeColor = Color.FromArgb(151, 163, 184);
            card.Controls.Add(note);
            return card;
        }

        private static Label NewSectionLabel(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.AutoSize = true;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(169, 223, 216);
            return label;
        }

        private static Label NewLabel(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.Width = 110;
            label.Height = 22;
            label.ForeColor = Color.FromArgb(215, 220, 232);
            return label;
        }

        private static TextBox NewTextBox(string text, int left, int top, int width)
        {
            TextBox box = new TextBox();
            box.Text = text;
            box.Left = left;
            box.Top = top;
            box.Width = width;
            box.Height = 24;
            box.BackColor = Color.FromArgb(55, 59, 68);
            box.ForeColor = Color.FromArgb(235, 238, 245);
            box.BorderStyle = BorderStyle.FixedSingle;
            return box;
        }

        private static Button NewPrimaryButton(string text, int left, int top, int width, int height)
        {
            Button button = NewButton(text, left, top, width, height);
            button.BackColor = Color.FromArgb(116, 94, 151);
            button.FlatAppearance.BorderColor = Color.FromArgb(214, 196, 255);
            return button;
        }

        private static Button NewButton(string text, int left, int top, int width, int height)
        {
            Button button = new Button();
            button.Text = text;
            button.Left = left;
            button.Top = top;
            button.Width = width;
            button.Height = height;
            button.BackColor = Color.FromArgb(69, 75, 87);
            button.ForeColor = Color.FromArgb(235, 238, 245);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(133, 145, 166);
            return button;
        }

        private MenuStrip BuildMenu()
        {
            MenuStrip menu = new MenuStrip();
            menu.BackColor = Color.FromArgb(33, 37, 45);
            menu.ForeColor = Color.FromArgb(235, 238, 245);

            ToolStripMenuItem help = new ToolStripMenuItem("Help");
            ToolStripMenuItem language = new ToolStripMenuItem("Language");
            foreach (string name in new[] { "English", "Spanish", "German", "French", "Portuguese", "Russian" })
            {
                ToolStripMenuItem item = new ToolStripMenuItem(name);
                item.CheckOnClick = false;
                item.Click += delegate { SetLanguage(name); };
                _languageMenuItems.Add(item);
                language.DropDownItems.Add(item);
            }

            ToolStripMenuItem howTo = new ToolStripMenuItem("How To Use");
            howTo.Click += delegate { ShowHelpWindow("How To Use", HowToUseText()); };
            ToolStripMenuItem languages = new ToolStripMenuItem("Language Help");
            languages.Click += delegate { ShowHelpWindow("Language Help", LanguageHelpText()); };

            help.DropDownItems.Add(language);
            help.DropDownItems.Add(new ToolStripSeparator());
            help.DropDownItems.Add(howTo);
            help.DropDownItems.Add(languages);
            menu.Items.Add(help);
            UpdateLanguageMenuChecks();
            return menu;
        }

        private void SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language) || string.Equals(_language, language, StringComparison.OrdinalIgnoreCase))
            {
                UpdateLanguageMenuChecks();
                return;
            }

            _language = language;
            if (_languageBox != null && !string.Equals(_languageBox.SelectedItem as string, _language, StringComparison.OrdinalIgnoreCase))
            {
                _languageBox.SelectedItem = _language;
            }
            ApplyLanguage();
            UpdateLanguageMenuChecks();
        }

        private void UpdateLanguageMenuChecks()
        {
            foreach (ToolStripMenuItem item in _languageMenuItems)
            {
                item.Checked = string.Equals(item.Text, _language, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ShowHelpWindow(string title, string body)
        {
            Form help = new Form();
            help.Text = AppName + " - " + title;
            help.Width = 620;
            help.Height = 500;
            help.StartPosition = FormStartPosition.CenterParent;
            help.BackColor = Color.FromArgb(26, 29, 35);
            help.Font = new Font("Segoe UI", 9);

            RichTextBox text = new RichTextBox();
            text.Left = 12;
            text.Top = 12;
            text.Width = 580;
            text.Height = 390;
            text.ReadOnly = true;
            text.BorderStyle = BorderStyle.FixedSingle;
            text.BackColor = Color.FromArgb(31, 34, 40);
            text.ForeColor = Color.FromArgb(226, 231, 241);
            text.Font = new Font("Segoe UI", 10);
            text.Text = body;
            help.Controls.Add(text);

            Button close = NewPrimaryButton("Close", 492, 415, 100, 30);
            close.Click += delegate { help.Close(); };
            help.Controls.Add(close);
            help.ShowDialog(this);
        }

        private static string HowToUseText()
        {
            return
                "Best first run:" + Environment.NewLine + Environment.NewLine +
                "1. Close Bannerlord." + Environment.NewLine +
                "2. Run Bannerlord Mod Doctor." + Environment.NewLine +
                "3. Confirm your Modules folder." + Environment.NewLine +
                "4. Keep Include Steam Workshop checked if you use Workshop mods." + Environment.NewLine +
                "5. Click Check My Mods." + Environment.NewLine +
                "6. Read Mod Doctor Summary first." + Environment.NewLine + Environment.NewLine +
                "Recommended fix order:" + Environment.NewLine + Environment.NewLine +
                "1. Missing dependencies" + Environment.NewLine +
                "2. Blocked DLLs" + Environment.NewLine +
                "3. Duplicate module IDs" + Environment.NewLine +
                "4. Duplicate framework DLLs" + Environment.NewLine +
                "5. Load order" + Environment.NewLine +
                "6. XML conflicts" + Environment.NewLine + Environment.NewLine +
                "Use Copy Results or Save when asking for help on Nexus or Discord.";
        }

        private static string LanguageHelpText()
        {
            return
                "Built-in app languages:" + Environment.NewLine + Environment.NewLine +
                "- English" + Environment.NewLine +
                "- Spanish" + Environment.NewLine +
                "- German" + Environment.NewLine +
                "- French" + Environment.NewLine +
                "- Portuguese" + Environment.NewLine +
                "- Russian" + Environment.NewLine + Environment.NewLine +
                "Change language from Help > Language, or use the selector in the top-right of the app." + Environment.NewLine + Environment.NewLine +
                "Detailed scan reports remain mostly English in v1 so they are easier to share with mod authors, Nexus posts, and Discord support.";
        }

        private static string DllToolsText()
        {
            return
                "DLL Tools" + Environment.NewLine + Environment.NewLine +
                "Fix Blocked DLLs:" + Environment.NewLine +
                "Removes Windows downloaded-file blocking from mod DLLs." + Environment.NewLine + Environment.NewLine +
                "Clean Duplicate DLLs:" + Environment.NewLine +
                "Moves safe duplicate framework DLL copies into a backup folder. This is reversible." + Environment.NewLine + Environment.NewLine +
                "Undo DLL Cleanup:" + Environment.NewLine +
                "Restores DLLs moved by this tool if the original location is still empty." + Environment.NewLine + Environment.NewLine +
                "Recommended order:" + Environment.NewLine +
                "1. Run Check My Mods." + Environment.NewLine +
                "2. Fix blocked DLLs if found." + Environment.NewLine +
                "3. Clean duplicate DLLs only when the summary says they are safe." + Environment.NewLine +
                "4. Launch the game and test.";
        }

        private static string SettingsText()
        {
            return
                "Settings" + Environment.NewLine + Environment.NewLine +
                "Game folders:" + Environment.NewLine +
                "Confirm the Modules folder points to your Bannerlord Modules directory." + Environment.NewLine + Environment.NewLine +
                "Steam Workshop:" + Environment.NewLine +
                "Leave Include Steam Workshop enabled if you use Workshop mods." + Environment.NewLine + Environment.NewLine +
                "Language:" + Environment.NewLine +
                "Change language from Help > Language or the selector in the header." + Environment.NewLine + Environment.NewLine +
                "Reports:" + Environment.NewLine +
                "Use Copy Results for Discord/Nexus help, or Save for a text file.";
        }

        private void ApplyLanguage()
        {
            foreach (Control control in _localizedControls)
            {
                string key = control.Tag as string;
                if (!string.IsNullOrWhiteSpace(key))
                {
                    control.Text = L(key);
                }
            }
        }

        private string L(string key)
        {
            Dictionary<string, string> pack = LanguagePack(_language);
            string value;
            if (pack.TryGetValue(key, out value))
            {
                return value;
            }

            Dictionary<string, string> english = LanguagePack("English");
            return english.TryGetValue(key, out value) ? value : key;
        }

        private static Dictionary<string, string> LanguagePack(string language)
        {
            Dictionary<string, string> english = new Dictionary<string, string>
            {
                { "Subtitle", "Find common Bannerlord mod setup problems before they waste your night." },
                { "GameFolders", "Game folders" },
                { "ModulesFolder", "Modules folder" },
                { "WorkshopFolder", "Workshop folder" },
                { "Browse", "Browse" },
                { "IncludeWorkshop", "Include Steam Workshop" },
                { "Actions", "Actions" },
                { "CheckMods", "⚕  Check My Mods" },
                { "QuickCheck", "⌕  Quick Check" },
                { "FixBlocked", "▣  Fix Blocked DLLs" },
                { "SortLoadOrder", "☷  Sort Load Order" },
                { "CleanDlls", "✦  Clean Duplicate DLLs" },
                { "UndoCleanup", "↶  Undo DLL Cleanup" },
                { "CopyResults", "□  Copy Results" },
                { "Save", "▣  Save" },
                { "Reports", "□  Reports" },
                { "Results", "Results" },
                { "DonateNote", "If Bannerlord Mod Doctor saves you time or stress, donations help support continued updates." },
                { "Donate", "♥  Donate via PayPal" }
            };

            Dictionary<string, string> pack = new Dictionary<string, string>(english);
            if (string.Equals(language, "Spanish", StringComparison.OrdinalIgnoreCase))
            {
                pack["Subtitle"] = "Encuentra problemas comunes de mods de Bannerlord antes de perder la noche.";
                pack["GameFolders"] = "Carpetas del juego";
                pack["ModulesFolder"] = "Carpeta Modules";
                pack["WorkshopFolder"] = "Carpeta Workshop";
                pack["Browse"] = "Buscar";
                pack["IncludeWorkshop"] = "Incluir Steam Workshop";
                pack["Actions"] = "Acciones";
                pack["CheckMods"] = "⚕  Revisar mods";
                pack["QuickCheck"] = "⌕  Revision rapida";
                pack["FixBlocked"] = "▣  Arreglar DLL bloqueadas";
                pack["SortLoadOrder"] = "☷  Ordenar carga";
                pack["CleanDlls"] = "✦  Limpiar DLL duplicadas";
                pack["UndoCleanup"] = "↶  Deshacer limpieza";
                pack["CopyResults"] = "□  Copiar resultados";
                pack["Save"] = "▣  Guardar";
                pack["Reports"] = "□  Reportes";
                pack["Results"] = "Resultados";
                pack["DonateNote"] = "Si Bannerlord Mod Doctor te ahorra tiempo o estres, las donaciones ayudan a seguir mejorandolo.";
                pack["Donate"] = "♥  Donar con PayPal";
            }
            else if (string.Equals(language, "German", StringComparison.OrdinalIgnoreCase))
            {
                pack["Subtitle"] = "Findet haeufige Bannerlord-Mod-Probleme, bevor sie den Abend ruinieren.";
                pack["GameFolders"] = "Spielordner";
                pack["ModulesFolder"] = "Modules-Ordner";
                pack["WorkshopFolder"] = "Workshop-Ordner";
                pack["Browse"] = "Auswaehlen";
                pack["IncludeWorkshop"] = "Steam Workshop einbeziehen";
                pack["Actions"] = "Aktionen";
                pack["CheckMods"] = "⚕  Mods pruefen";
                pack["QuickCheck"] = "⌕  Schnellpruefung";
                pack["FixBlocked"] = "▣  Blockierte DLLs reparieren";
                pack["SortLoadOrder"] = "☷  Ladereihenfolge";
                pack["CleanDlls"] = "✦  Doppelte DLLs reinigen";
                pack["UndoCleanup"] = "↶  Reinigung rueckgaengig";
                pack["CopyResults"] = "□  Ergebnis kopieren";
                pack["Save"] = "▣  Speichern";
                pack["Reports"] = "□  Berichte";
                pack["Results"] = "Ergebnisse";
                pack["DonateNote"] = "Wenn Bannerlord Mod Doctor dir Zeit oder Stress spart, helfen Spenden bei weiteren Updates.";
                pack["Donate"] = "♥  Mit PayPal spenden";
            }
            else if (string.Equals(language, "French", StringComparison.OrdinalIgnoreCase))
            {
                pack["Subtitle"] = "Trouve les problemes courants des mods Bannerlord avant de perdre ta soiree.";
                pack["GameFolders"] = "Dossiers du jeu";
                pack["ModulesFolder"] = "Dossier Modules";
                pack["WorkshopFolder"] = "Dossier Workshop";
                pack["Browse"] = "Parcourir";
                pack["IncludeWorkshop"] = "Inclure Steam Workshop";
                pack["Actions"] = "Actions";
                pack["CheckMods"] = "⚕  Verifier mes mods";
                pack["QuickCheck"] = "⌕  Verification rapide";
                pack["FixBlocked"] = "▣  Reparer les DLL bloquees";
                pack["SortLoadOrder"] = "☷  Trier l'ordre";
                pack["CleanDlls"] = "✦  Nettoyer DLL doubles";
                pack["UndoCleanup"] = "↶  Annuler nettoyage";
                pack["CopyResults"] = "□  Copier resultats";
                pack["Save"] = "▣  Sauver";
                pack["Reports"] = "□  Rapports";
                pack["Results"] = "Resultats";
                pack["DonateNote"] = "Si Bannerlord Mod Doctor te fait gagner du temps ou evite du stress, les dons aident les mises a jour.";
                pack["Donate"] = "♥  Don PayPal";
            }
            else if (string.Equals(language, "Portuguese", StringComparison.OrdinalIgnoreCase))
            {
                pack["Subtitle"] = "Encontra problemas comuns de mods do Bannerlord antes que estraguem sua noite.";
                pack["GameFolders"] = "Pastas do jogo";
                pack["ModulesFolder"] = "Pasta Modules";
                pack["WorkshopFolder"] = "Pasta Workshop";
                pack["Browse"] = "Procurar";
                pack["IncludeWorkshop"] = "Incluir Steam Workshop";
                pack["Actions"] = "Acoes";
                pack["CheckMods"] = "⚕  Verificar mods";
                pack["QuickCheck"] = "⌕  Verificacao rapida";
                pack["FixBlocked"] = "▣  Corrigir DLLs bloqueadas";
                pack["SortLoadOrder"] = "☷  Ordenar carga";
                pack["CleanDlls"] = "✦  Limpar DLLs duplicadas";
                pack["UndoCleanup"] = "↶  Desfazer limpeza";
                pack["CopyResults"] = "□  Copiar resultados";
                pack["Save"] = "▣  Salvar";
                pack["Reports"] = "□  Relatorios";
                pack["Results"] = "Resultados";
                pack["DonateNote"] = "Se Bannerlord Mod Doctor economiza tempo ou estresse, doacoes ajudam a manter atualizacoes.";
                pack["Donate"] = "♥  Doar via PayPal";
            }
            else if (string.Equals(language, "Russian", StringComparison.OrdinalIgnoreCase))
            {
                pack["Subtitle"] = "Nakhodit chastye problemy modov Bannerlord, poka oni ne isportili vecher.";
                pack["GameFolders"] = "Papki igry";
                pack["ModulesFolder"] = "Papka Modules";
                pack["WorkshopFolder"] = "Papka Workshop";
                pack["Browse"] = "Obzor";
                pack["IncludeWorkshop"] = "Vkluchit Steam Workshop";
                pack["Actions"] = "Deystviya";
                pack["CheckMods"] = "⚕  Proverit mody";
                pack["QuickCheck"] = "⌕  Bystraya proverka";
                pack["FixBlocked"] = "▣  Ispravit blok DLL";
                pack["SortLoadOrder"] = "☷  Poryadok zagruzki";
                pack["CleanDlls"] = "✦  Ochistit dubl DLL";
                pack["UndoCleanup"] = "↶  Otkatit ochistku";
                pack["CopyResults"] = "□  Kopirovat rezultat";
                pack["Save"] = "▣  Sokhranit";
                pack["Reports"] = "□  Otchety";
                pack["Results"] = "Rezultaty";
                pack["DonateNote"] = "Esli Bannerlord Mod Doctor seconomil vremya ili nervy, donaty pomogayut obnovleniyam.";
                pack["Donate"] = "♥  Donat PayPal";
            }

            return pack;
        }

        private void BrowseInto(TextBox target)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.SelectedPath = Directory.Exists(target.Text) ? target.Text : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    target.Text = dialog.SelectedPath;
                }
            }
        }

        private void RunScan(bool quick, bool unblock)
        {
            try
            {
                HealthScanner scanner = new HealthScanner();
                ScanOptions options = new ScanOptions();
                options.ModulesFolder = _modulesPath.Text;
                options.WorkshopFolder = _workshopPath.Text;
                options.IncludeWorkshop = _includeWorkshop.Checked;
                options.EnabledOnly = _enabledOnly.Checked;
                options.Quick = quick;
                options.UnblockDlls = unblock;
                ScanResult result = scanner.Scan(options);
                SetReport(result.Report);
                _lastLoadOrderPreview = result.LoadOrderPreview;
                _lastReportPath = result.ReportPath;
                _lastReportKind = unblock ? "Unblock DLL Report" : (quick ? "Quick Scan Report" : "Full Scan Report");
                MessageBox.Show(this, "Scan complete.\n\nReport saved to:\n" + result.ReportPath, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunSortedLoadOrder()
        {
            try
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "This will back up LauncherData.xml, then write the suggested load order directly to the Bannerlord launcher data.\n\nMake sure Bannerlord and the launcher are closed before continuing.\n\nContinue?",
                    AppName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes)
                {
                    return;
                }

                HealthScanner scanner = new HealthScanner();
                ScanOptions options = new ScanOptions();
                options.ModulesFolder = _modulesPath.Text;
                options.WorkshopFolder = _workshopPath.Text;
                options.IncludeWorkshop = _includeWorkshop.Checked;
                options.EnabledOnly = _enabledOnly.Checked;
                ScanResult result = scanner.ApplySortedLoadOrder(options);
                _lastLoadOrderPreview = result.LoadOrderPreview;
                SetReport(result.Report);
                _lastReportPath = result.ReportPath;
                _lastReportKind = "Sorted Load Order";
                MessageBox.Show(this, "Load order sorted and saved.\n\nA backup was created before writing changes.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Sorted load order failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshLoadOrderPreview(bool force)
        {
            try
            {
                if (!force && !string.IsNullOrWhiteSpace(_lastLoadOrderPreview))
                {
                    SetReport(_lastLoadOrderPreview);
                    _lastReportKind = "Load Order Preview";
                    return;
                }

                HealthScanner scanner = new HealthScanner();
                ScanOptions options = new ScanOptions();
                options.ModulesFolder = _modulesPath.Text;
                options.WorkshopFolder = _workshopPath.Text;
                options.IncludeWorkshop = _includeWorkshop.Checked;
                options.EnabledOnly = _enabledOnly.Checked;
                string preview = scanner.BuildLoadOrderPreview(options);
                _lastLoadOrderPreview = preview;
                SetReport(preview);
                _lastReportKind = "Load Order Preview";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Load order preview failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunSupportReport()
        {
            try
            {
                HealthScanner scanner = new HealthScanner();
                ScanOptions options = new ScanOptions();
                options.ModulesFolder = _modulesPath.Text;
                options.WorkshopFolder = _workshopPath.Text;
                options.IncludeWorkshop = _includeWorkshop.Checked;
                options.EnabledOnly = _enabledOnly.Checked;
                ScanResult result = scanner.BuildSupportReport(options);
                SetReport(result.Report);
                _lastReportPath = result.ReportPath;
                _lastReportKind = "Support Report";
                Clipboard.SetText(result.Report);
                MessageBox.Show(this, "Support report created, saved, and copied to clipboard.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Support report failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UndoLoadOrderSort()
        {
            DialogResult answer = MessageBox.Show(
                this,
                "This will restore the newest LauncherData.xml backup created by Bannerlord Mod Doctor.\n\nContinue?",
                AppName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                HealthScanner scanner = new HealthScanner();
                ScanResult result = scanner.RestoreLatestLoadOrderBackup();
                SetReport(result.Report);
                _lastReportPath = result.ReportPath;
                _lastReportKind = "Undo Load Order";
                MessageBox.Show(this, "Load order restore complete.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Undo load order failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveSafeHelperDlls()
        {
            DialogResult answer = MessageBox.Show(
                this,
                "This will move duplicate framework DLL copies into a restorable backup folder only when the official framework module is also installed.\n\nIt will not remove the official Harmony, ButterLib, UIExtenderEx, or MCM modules.\n\nContinue?",
                "Clean Duplicate DLLs",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                HealthScanner scanner = new HealthScanner();
                ScanOptions options = new ScanOptions();
                options.ModulesFolder = _modulesPath.Text;
                options.WorkshopFolder = _workshopPath.Text;
                options.IncludeWorkshop = _includeWorkshop.Checked;
                options.RemoveSafeHelperDlls = true;
                ScanResult result = scanner.Scan(options);
                SetReport(result.Report);
                _lastReportPath = result.ReportPath;
                _lastReportKind = "Safe Helper DLL Cleanup Report";
                MessageBox.Show(this, "Safe helper cleanup complete.\n\nReport saved to:\n" + result.ReportPath, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Helper cleanup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RestoreHelperDlls()
        {
            DialogResult answer = MessageBox.Show(
                this,
                "This will restore helper DLLs previously moved by this tool, as long as the original location is still empty.\n\nContinue?",
                "Restore Helper DLLs",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                HealthScanner scanner = new HealthScanner();
                ScanResult result = scanner.RestoreHelperDllBackups();
                SetReport(result.Report);
                _lastReportPath = result.ReportPath;
                _lastReportKind = "Helper DLL Restore Report";
                MessageBox.Show(this, "Helper restore complete.\n\nReport saved to:\n" + result.ReportPath, AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.ToString(), "Helper restore failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetReport(string text)
        {
            _reportBox.Clear();
            _reportBox.Text = text;
            ApplyReportColors();
            UpdateDashboardCounts(text);
        }

        private void UpdateDashboardCounts(string text)
        {
            SetCount(_missingCount, ExtractSummaryValue(text, "Missing dependencies"));
            SetCount(_blockedCount, ExtractSummaryValue(text, "Blocked DLLs"));
            SetCount(_duplicateDllCount, ExtractSummaryValue(text, "Duplicate framework DLLs safe to clean"));
            SetCount(_xmlConflictCount, ExtractSummaryValue(text, "XML record conflicts"));
        }

        private static void SetCount(Label label, string value)
        {
            if (label != null)
            {
                label.Text = string.IsNullOrWhiteSpace(value) ? "0" : value;
            }
        }

        private static string ExtractSummaryValue(string text, string label)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "0";
            }

            foreach (string line in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                string trimmed = line.Trim();
                if (!trimmed.StartsWith(label + " :", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string value = trimmed.Substring((label + " :").Length).Trim();
                return value.StartsWith("not checked", StringComparison.OrdinalIgnoreCase) ? "-" : value;
            }
            return "0";
        }

        private void ApplyReportColors()
        {
            int originalStart = _reportBox.SelectionStart;
            string[] lines = _reportBox.Lines;
            int index = 0;
            foreach (string line in lines)
            {
                Color color = Color.FromArgb(226, 231, 241);
                FontStyle style = FontStyle.Regular;

                if (line.StartsWith("== "))
                {
                    color = Color.FromArgb(214, 196, 255);
                    style = FontStyle.Bold;
                }
                else if (line.StartsWith("Missing") || line.StartsWith("Duplicate") || line.StartsWith("Blocked") || line.StartsWith("XML") || line.StartsWith("Load") || line.StartsWith("Suggested") || line.StartsWith("Notes"))
                {
                    color = Color.FromArgb(169, 223, 216);
                    style = FontStyle.Bold;
                }
                else if (line.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("Blocked:", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    color = Color.FromArgb(255, 176, 184);
                }
                else if (line.IndexOf("Suggestion", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("Likely", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    color = Color.FromArgb(246, 222, 151);
                }
                else if (line.StartsWith("  "))
                {
                    color = Color.FromArgb(184, 205, 255);
                }

                _reportBox.Select(index, line.Length);
                _reportBox.SelectionColor = color;
                _reportBox.SelectionFont = new Font(_reportBox.Font, style);
                index += line.Length + 1;
            }

            _reportBox.Select(Math.Min(originalStart, _reportBox.TextLength), 0);
        }

        private void SaveReportAs()
        {
            if (string.IsNullOrEmpty(_reportBox.Text))
            {
                MessageBox.Show(this, "No report to save yet.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                Directory.CreateDirectory(ReportFolder());
                dialog.InitialDirectory = ReportFolder();
                string label = string.IsNullOrWhiteSpace(_lastReportKind) ? "Report" : _lastReportKind.Replace(" ", "");
                dialog.FileName = "BannerlordModDoctor_" + label + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    File.WriteAllText(dialog.FileName, _reportBox.Text, Encoding.UTF8);
                }
            }
        }

        private void OpenReportsFolder()
        {
            string folder = ReportFolder();
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", folder);
        }

        private void CopyResults()
        {
            if (string.IsNullOrEmpty(_reportBox.Text))
            {
                MessageBox.Show(this, "No results to copy yet.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(_reportBox.Text);
            MessageBox.Show(this, "Results copied to clipboard.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OpenDonationLink()
        {
            try
            {
                Process.Start("https://paypal.me/KodySp");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not open PayPal link.\n\nhttps://paypal.me/KodySp\n\n" + ex.Message, "Donation Link", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string DefaultModulesFolder()
        {
            DirectoryInfo dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 4 && dir != null; i++)
            {
                if (string.Equals(dir.Name, "Modules", StringComparison.OrdinalIgnoreCase))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }

            return @"C:\Program Files (x86)\Steam\SteamApps\common\Mount & Blade II Bannerlord\Modules";
        }

        private static string DefaultWorkshopFolder()
        {
            return @"C:\Program Files (x86)\Steam\steamapps\workshop\content\261550";
        }

        private static string ReportFolder()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        }
    }

    internal sealed class ScanOptions
    {
        public string ModulesFolder;
        public string WorkshopFolder;
        public bool IncludeWorkshop;
        public bool EnabledOnly;
        public bool Quick;
        public bool UnblockDlls;
        public bool RemoveSafeHelperDlls;
    }

    internal sealed class ScanResult
    {
        public string Report;
        public string ReportPath;
        public string LoadOrderPreview;
    }

    internal sealed class ModuleInfo
    {
        public string Id;
        public string Name;
        public string Root;
        public string SubModuleXml;
        public List<string> Dependencies = new List<string>();
        public List<string> XmlNames = new List<string>();
        public List<string> DllNames = new List<string>();
        public List<XmlRecord> XmlRecords = new List<XmlRecord>();
        public int DetectedOrder;
    }

    internal sealed class XmlRecord
    {
        public string Key;
        public string ElementName;
        public string Id;
        public string File;
        public string RelativeFile;
    }

    internal sealed class DiagnosticSummary
    {
        public int DuplicateModuleIds;
        public int MissingDependencies;
        public int DependencyEdges;
        public int SafeDuplicateDlls;
        public int UnsafeDuplicateDlls;
        public int RepeatedXmlFiles;
        public int XmlRecordConflicts;
        public int DuplicateAssemblyNames;
        public int BlockedDlls;
    }

    internal sealed class HealthScanner
    {
        private static readonly string[] HelperDllNames =
        {
            "0Harmony.dll",
            "MCMv5.dll",
            "Bannerlord.ButterLib.dll",
            "Bannerlord.UIExtenderEx.dll"
        };

        private static readonly HashSet<string> IgnoredVanguardCheatsDependencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AIInfluence",
            "Soldier_Career_Simulator",
            "ModularSmithing2",
            "RBM",
            "gfrontsCulturalMusicMod",
            "AIComboAttacks",
            "Ben's Armory",
            "NamedSoldiers",
            "Diplomacy"
        };

        private static readonly HashSet<string> OfficialModuleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Native",
            "SandBoxCore",
            "Sandbox",
            "StoryMode",
            "CustomBattle",
            "BirthAndDeath",
            "Multiplayer"
        };

        private static readonly Dictionary<string, string> HelperOfficialModules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "0Harmony.dll", "Bannerlord.Harmony" },
            { "MCMv5.dll", "Bannerlord.MBOptionScreen" },
            { "Bannerlord.ButterLib.dll", "Bannerlord.ButterLib" },
            { "Bannerlord.UIExtenderEx.dll", "Bannerlord.UIExtenderEx" }
        };

        public ScanResult Scan(ScanOptions options)
        {
            List<string> report = new List<string>();
            List<ModuleInfo> modules = LoadModules(options);

            report.Add("== Bannerlord Mod Doctor Report ==");
            report.Add("");
            report.Add("Generated : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.Add("");
            report.Add("Modules folder : " + options.ModulesFolder);
            report.Add("Workshop folder : " + (options.IncludeWorkshop ? options.WorkshopFolder : "(not scanned)"));
            report.Add("Unblock DLLs : " + options.UnblockDlls);
            report.Add("Remove safe helper DLL copies : " + options.RemoveSafeHelperDlls);
            report.Add("Quick scan : " + options.Quick);
            report.Add("");

            report.Add("Modules found : " + modules.Count);
            report.Add("");

            DiagnosticSummary summary = Analyze(modules, options);
            AddDoctorSummary(report, summary, options);
            AddDuplicateModuleIdReport(report, modules);
            AddMissingDependencyReport(report, modules);
            AddLoadOrderSuggestions(report, modules);
            AddSortedLoadOrderReport(report, modules);
            AddDuplicateHelperReport(report, modules, options.RemoveSafeHelperDlls);
            AddLikelyXmlConflictReport(report, modules);
            AddXmlRecordConflictReport(report, modules);
            AddAssemblyNameConflictReport(report, modules);

            if (!options.Quick)
            {
                AddBlockedDllReport(report, modules, options.UnblockDlls);
                AddRecentCrashLogSummary(report);
            }

            report.Add("");
            report.Add("== Notes ==");
            report.Add("");
            report.Add("This tool can detect common static issues. It cannot prove Harmony patch conflicts unless a mod documents or exposes its runtime patches.");
            report.Add("Likely XML conflicts mean two or more mods ship similarly named XML data files; that can be normal, but it is worth checking if the game crashes during XML loading.");
            report.Add("");

            string text = string.Join(Environment.NewLine, report.ToArray());
            string path = WriteReport(text, ReportKind(options));
            ScanResult result = new ScanResult();
            result.Report = text;
            result.ReportPath = path;
            result.LoadOrderPreview = BuildLoadOrderPreview(modules);
            return result;
        }

        public string BuildLoadOrderPreview(ScanOptions options)
        {
            List<ModuleInfo> modules = LoadModules(options);
            return BuildLoadOrderPreview(modules);
        }

        public ScanResult ApplySortedLoadOrder(ScanOptions options)
        {
            List<ModuleInfo> modules = LoadModules(options);

            string launcherData = LauncherDataPath();
            List<string> report = new List<string>();
            report.Add("== Apply Sorted Load Order ==");
            report.Add("");
            report.Add("Launcher data : " + launcherData);
            report.Add("");

            if (!File.Exists(launcherData))
            {
                report.Add("LauncherData.xml was not found.");
                string missingText = string.Join(Environment.NewLine, report.ToArray());
                string missingPath = WriteReport(missingText, "ApplyLoadOrder");
                return new ScanResult { Report = missingText, ReportPath = missingPath, LoadOrderPreview = BuildLoadOrderPreview(modules) };
            }

            string backupPath = launcherData + ".backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(launcherData, backupPath, false);
            report.Add("Backup created : " + backupPath);
            report.Add("");

            XDocument doc = XDocument.Load(launcherData, LoadOptions.PreserveWhitespace);
            XElement modDatas = doc.Descendants("SingleplayerData")
                .Elements("ModDatas")
                .FirstOrDefault();
            if (modDatas == null)
            {
                report.Add("Singleplayer ModDatas section was not found.");
                string badText = string.Join(Environment.NewLine, report.ToArray());
                string badPath = WriteReport(badText, "ApplyLoadOrder");
                return new ScanResult { Report = badText, ReportPath = badPath, LoadOrderPreview = BuildLoadOrderPreview(modules) };
            }

            List<XElement> entries = modDatas.Elements("UserModData").ToList();
            Dictionary<string, XElement> byId = new Dictionary<string, XElement>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement entry in entries)
            {
                string id = (string)entry.Element("Id");
                if (!string.IsNullOrWhiteSpace(id) && !byId.ContainsKey(id))
                {
                    byId[id] = entry;
                }
            }

            List<ModuleInfo> sortedModules = SortModules(modules);
            List<XElement> reordered = new List<XElement>();
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in sortedModules)
            {
                XElement entry;
                if (byId.TryGetValue(module.Id, out entry))
                {
                    reordered.Add(new XElement(entry));
                    used.Add(module.Id);
                }
            }

            foreach (XElement entry in entries)
            {
                string id = (string)entry.Element("Id");
                if (string.IsNullOrWhiteSpace(id) || !used.Contains(id))
                {
                    reordered.Add(new XElement(entry));
                }
            }

            modDatas.ReplaceNodes(reordered);
            doc.Save(launcherData);

            report.Add("Load order written successfully.");
            report.Add("Entries reordered : " + reordered.Count);
            report.Add("");
            string preview = BuildLoadOrderPreview(modules);
            report.Add(preview);

            string text = string.Join(Environment.NewLine, report.ToArray());
            string path = WriteReport(text, "ApplyLoadOrder");
            return new ScanResult { Report = text, ReportPath = path, LoadOrderPreview = preview };
        }

        private static string BuildLoadOrderPreview(List<ModuleInfo> modules)
        {
            List<string> report = new List<string>();
            report.Add("== Load Order Preview ==");
            report.Add("");
            report.Add("Detected/current order is based on the modules found by this tool.");
            report.Add("Suggested order is based on dependencies and known Bannerlord framework order.");
            report.Add("");

            if (modules.Count == 0)
            {
                report.Add("No modules found.");
                return string.Join(Environment.NewLine, report.ToArray());
            }

            List<ModuleInfo> current = modules.OrderBy(m => m.DetectedOrder).ThenBy(m => m.Id).ToList();
            List<ModuleInfo> suggested = SortModules(modules);
            Dictionary<string, int> currentPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> suggestedPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < current.Count; i++)
            {
                if (!currentPositions.ContainsKey(current[i].Id))
                {
                    currentPositions[current[i].Id] = i + 1;
                }
            }
            for (int i = 0; i < suggested.Count; i++)
            {
                if (!suggestedPositions.ContainsKey(suggested[i].Id))
                {
                    suggestedPositions[suggested[i].Id] = i + 1;
                }
            }

            report.Add("Current detected order                         Suggested order");
            report.Add("----------------------                         ---------------");
            int max = Math.Max(current.Count, suggested.Count);
            for (int i = 0; i < max; i++)
            {
                string currentText = i < current.Count
                    ? (i + 1).ToString("000") + " : " + current[i].Id
                    : "";
                string suggestedText = i < suggested.Count
                    ? (i + 1).ToString("000") + " : " + suggested[i].Id
                    : "";
                string marker = "";
                if (i < suggested.Count)
                {
                    int currentPosition;
                    if (currentPositions.TryGetValue(suggested[i].Id, out currentPosition) && currentPosition != i + 1)
                    {
                        marker = "  <= moved from " + currentPosition.ToString("000");
                    }
                }
                report.Add("  " + currentText.PadRight(45) + "  " + suggestedText + marker);
            }

            report.Add("");
            report.Add("Change summary :");
            int changes = 0;
            foreach (ModuleInfo module in suggested)
            {
                int currentPosition;
                int suggestedPosition;
                if (currentPositions.TryGetValue(module.Id, out currentPosition)
                    && suggestedPositions.TryGetValue(module.Id, out suggestedPosition)
                    && currentPosition != suggestedPosition)
                {
                    changes++;
                    report.Add("  " + module.Id + " : " + currentPosition.ToString("000") + " -> " + suggestedPosition.ToString("000"));
                }
            }
            if (changes == 0)
            {
                report.Add("  No dependency-based movement suggested.");
            }

            report.Add("");
            return string.Join(Environment.NewLine, report.ToArray());
        }

        public ScanResult RestoreHelperDllBackups()
        {
            List<string> report = new List<string>();
            report.Add("== Restore Helper DLL Backups ==");
            report.Add("");
            report.Add("Generated : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.Add("");

            string backupRoot = HelperBackupRoot();
            if (!Directory.Exists(backupRoot))
            {
                report.Add("No helper DLL backup folder found.");
                string emptyText = string.Join(Environment.NewLine, report.ToArray());
                string emptyPath = WriteReport(emptyText, "HelperRestore");
                return new ScanResult { Report = emptyText, ReportPath = emptyPath };
            }

            int restored = 0;
            int skipped = 0;
            int failed = 0;
            foreach (string manifest in SafeGetFiles(backupRoot, "manifest.tsv").OrderByDescending(p => p))
            {
                foreach (string line in File.ReadAllLines(manifest))
                {
                    string[] parts = line.Split(new[] { '\t' }, 2);
                    if (parts.Length != 2)
                    {
                        continue;
                    }

                    string backupPath = parts[0];
                    string originalPath = parts[1];
                    if (!File.Exists(backupPath))
                    {
                        skipped++;
                        report.Add("Skipped : backup missing for " + originalPath);
                        continue;
                    }
                    if (File.Exists(originalPath))
                    {
                        skipped++;
                        report.Add("Skipped : original already exists at " + originalPath);
                        continue;
                    }

                    try
                    {
                        string originalFolder = Path.GetDirectoryName(originalPath);
                        if (!string.IsNullOrWhiteSpace(originalFolder))
                        {
                            Directory.CreateDirectory(originalFolder);
                        }
                        File.Move(backupPath, originalPath);
                        restored++;
                        report.Add("Restored : " + originalPath);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        report.Add("Failed : " + originalPath + " :: " + ex.Message);
                    }
                }
            }

            report.Add("");
            report.Add("Restored : " + restored);
            report.Add("Skipped : " + skipped);
            report.Add("Failed : " + failed);
            report.Add("");

            string text = string.Join(Environment.NewLine, report.ToArray());
            string path = WriteReport(text, "HelperRestore");
            return new ScanResult { Report = text, ReportPath = path };
        }

        public ScanResult BuildSupportReport(ScanOptions options)
        {
            ScanResult scan = Scan(options);
            List<string> report = new List<string>();
            report.Add("== Bannerlord Mod Doctor Support Report ==");
            report.Add("");
            report.Add("Created : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.Add("Enabled mods only : " + options.EnabledOnly);
            report.Add("");
            report.Add(scan.Report);
            report.Add("");
            AddRecentCrashLogSummary(report);
            string text = string.Join(Environment.NewLine, report.ToArray());
            string path = WriteReport(text, "SupportReport");
            return new ScanResult { Report = text, ReportPath = path, LoadOrderPreview = scan.LoadOrderPreview };
        }

        public ScanResult RestoreLatestLoadOrderBackup()
        {
            List<string> report = new List<string>();
            report.Add("== Undo Load Order Change ==");
            report.Add("");
            string launcherData = LauncherDataPath();
            string folder = Path.GetDirectoryName(launcherData);
            string newestBackup = Directory.Exists(folder)
                ? Directory.GetFiles(folder, "LauncherData.xml.backup_*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(File.GetLastWriteTime)
                    .FirstOrDefault()
                : null;

            if (string.IsNullOrWhiteSpace(newestBackup) || !File.Exists(newestBackup))
            {
                report.Add("No Bannerlord Mod Doctor load-order backup was found.");
            }
            else
            {
                string beforeRestore = launcherData + ".before_restore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                if (File.Exists(launcherData))
                {
                    File.Copy(launcherData, beforeRestore, false);
                    report.Add("Current LauncherData backup : " + beforeRestore);
                }
                File.Copy(newestBackup, launcherData, true);
                report.Add("Restored backup : " + newestBackup);
                report.Add("Launcher data : " + launcherData);
            }

            report.Add("");
            string text = string.Join(Environment.NewLine, report.ToArray());
            string path = WriteReport(text, "UndoLoadOrder");
            return new ScanResult { Report = text, ReportPath = path };
        }

        private static List<ModuleInfo> LoadModules(ScanOptions options)
        {
            List<ModuleInfo> modules = new List<ModuleInfo>();
            modules.AddRange(FindModules(options.ModulesFolder));
            if (options.IncludeWorkshop)
            {
                modules.AddRange(FindModules(options.WorkshopFolder));
            }

            if (options.EnabledOnly)
            {
                HashSet<string> enabled = ReadEnabledLauncherMods();
                if (enabled.Count > 0)
                {
                    modules = modules.Where(m => enabled.Contains(m.Id)).ToList();
                }
            }

            AssignDetectedOrder(modules);
            return modules;
        }

        private static HashSet<string> ReadEnabledLauncherMods()
        {
            HashSet<string> enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string path = LauncherDataPath();
            if (!File.Exists(path))
            {
                return enabled;
            }

            try
            {
                XDocument doc = XDocument.Load(path);
                XElement modDatas = doc.Descendants("SingleplayerData")
                    .Elements("ModDatas")
                    .FirstOrDefault();
                if (modDatas == null)
                {
                    return enabled;
                }

                foreach (XElement entry in modDatas.Elements("UserModData"))
                {
                    string id = (string)entry.Element("Id");
                    string selected = (string)entry.Element("IsSelected");
                    if (!string.IsNullOrWhiteSpace(id) && string.Equals(selected, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        enabled.Add(id);
                    }
                }
            }
            catch
            {
            }
            return enabled;
        }

        private static DiagnosticSummary Analyze(List<ModuleInfo> modules, ScanOptions options)
        {
            DiagnosticSummary summary = new DiagnosticSummary();
            HashSet<string> available = new HashSet<string>(modules.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);

            summary.DuplicateModuleIds = modules
                .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Count() > 1);

            summary.MissingDependencies = modules.Sum(module => module.Dependencies
                .Where(d => !ShouldIgnoreDependency(module.Id, d))
                .Count(d => !available.Contains(d)));

            summary.DependencyEdges = modules.Sum(module => module.Dependencies
                .Count(d => !ShouldIgnoreDependency(module.Id, d) && available.Contains(d)));

            foreach (ModuleInfo module in modules)
            {
                foreach (string helper in HelperDllNames)
                {
                    foreach (string path in SafeGetFiles(module.Root, helper))
                    {
                        if (IsSafeHelperCopy(module, helper, available))
                        {
                            summary.SafeDuplicateDlls++;
                        }
                        else
                        {
                            summary.UnsafeDuplicateDlls++;
                        }
                    }
                }
            }

            summary.RepeatedXmlFiles = CountRepeatedXmlFiles(modules);
            summary.XmlRecordConflicts = CountXmlRecordConflicts(modules);
            summary.DuplicateAssemblyNames = modules
                .SelectMany(m => m.DllNames.Select(d => new { Module = m.Id, Dll = d }))
                .GroupBy(x => x.Dll, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Select(x => x.Module).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);

            if (!options.Quick)
            {
                summary.BlockedDlls = CountBlockedDlls(modules);
            }

            return summary;
        }

        private static void AddDoctorSummary(List<string> report, DiagnosticSummary summary, ScanOptions options)
        {
            report.Add("== Mod Doctor Summary ==");
            report.Add("");
            int high = summary.DuplicateModuleIds + summary.MissingDependencies + summary.BlockedDlls;
            int medium = summary.SafeDuplicateDlls + summary.XmlRecordConflicts + summary.DuplicateAssemblyNames;
            string status = high > 0 ? "Needs attention" : (medium > 0 ? "Worth reviewing" : "Looks clean");

            report.Add("Overall : " + status);
            report.Add("Confidence : High for missing dependencies, blocked DLLs, duplicate IDs, and duplicate DLL names.");
            report.Add("");
            report.Add("Found :");
            report.Add("  Missing dependencies : " + summary.MissingDependencies);
            report.Add("  Blocked DLLs : " + (options.Quick ? "not checked in Quick Check" : summary.BlockedDlls.ToString()));
            report.Add("  Duplicate module IDs : " + summary.DuplicateModuleIds);
            report.Add("  Duplicate framework DLLs safe to clean : " + summary.SafeDuplicateDlls);
            report.Add("  XML record conflicts : " + summary.XmlRecordConflicts);
            report.Add("  Duplicate DLL file names : " + summary.DuplicateAssemblyNames);
            report.Add("");
            report.Add("Next steps :");
            if (summary.MissingDependencies > 0)
            {
                report.Add("  Install or disable mods with missing dependencies.");
            }
            if (!options.Quick && summary.BlockedDlls > 0)
            {
                report.Add("  Use Fix Blocked DLLs.");
            }
            if (summary.SafeDuplicateDlls > 0)
            {
                report.Add("  Use Clean Duplicate DLLs, then test launch. Use Undo DLL Cleanup if needed.");
            }
            if (summary.XmlRecordConflicts > 0)
            {
                report.Add("  Review XML Record Conflicts and place the mod you want to win later.");
            }
            if (high == 0 && medium == 0)
            {
                report.Add("  No obvious setup problems found.");
            }
            report.Add("");
        }

        private static void AddDuplicateModuleIdReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Duplicate Module IDs ==");
            report.Add("");
            var groups = modules.GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).ToList();
            if (groups.Count == 0)
            {
                report.Add("No duplicate module IDs found.");
            }
            else
            {
                foreach (var group in groups)
                {
                report.Add("Duplicate ID : " + group.Key);
                    foreach (ModuleInfo module in group)
                    {
                        report.Add("  " + module.Root);
                    }
                }
            }
            report.Add("");
        }

        private static void AddMissingDependencyReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Missing Dependencies ==");
            report.Add("");
            HashSet<string> available = new HashSet<string>(modules.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
            int missing = 0;
            foreach (ModuleInfo module in modules.OrderBy(m => m.Id))
            {
                foreach (string dep in module.Dependencies.Where(d => !ShouldIgnoreDependency(module.Id, d)))
                {
                    if (!available.Contains(dep))
                    {
                        missing++;
                        report.Add(module.Id + " requires missing module : " + dep);
                    }
                }
            }
            if (missing == 0)
            {
                report.Add("No missing dependencies found.");
            }
            report.Add("");
        }

        private static void AddLoadOrderSuggestions(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Load Order Suggestions ==");
            report.Add("");
            report.Add("Suggested broad order :");
            report.Add("  1. Bannerlord.Harmony");
            report.Add("  2. Bannerlord.ButterLib");
            report.Add("  3. Bannerlord.UIExtenderEx");
            report.Add("  4. Bannerlord.MBOptionScreen");
            report.Add("  5. Native, SandBoxCore, Sandbox, StoryMode");
            report.Add("  6. Total conversions / maps");
            report.Add("  7. Large gameplay overhauls");
            report.Add("  8. Smaller tweaks and patches");
            report.Add("");
            report.Add("Required order by mod :");
            report.Add("");
            foreach (ModuleInfo module in modules.OrderBy(m => m.Id))
            {
                List<string> deps = module.Dependencies
                    .Where(d => !ShouldIgnoreDependency(module.Id, d))
                    .OrderBy(d => d)
                    .ToList();
                if (deps.Count == 0)
                {
                    continue;
                }

                report.Add(module.Id + " :");
                foreach (string dep in deps)
                {
                    report.Add("  - " + dep + " should load before " + module.Id);
                }
                report.Add("");
            }
            report.Add("");
        }

        private static void AddSortedLoadOrderReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Suggested Load Order ==");
            report.Add("");
            report.Add("Generated from mod dependencies and known Bannerlord framework order.");
            report.Add("");

            List<ModuleInfo> sorted = SortModules(modules);
            int index = 1;
            foreach (ModuleInfo module in sorted)
            {
                report.Add(index.ToString("000") + " : " + module.Id + "  (" + module.Name + ")");
                index++;
            }
            report.Add("");
        }

        private static void AddDuplicateHelperReport(List<string> report, List<ModuleInfo> modules, bool removeSafeCopies)
        {
            report.Add("== Duplicate Helper DLLs ==");
            report.Add("");
            report.Add("Duplicate framework DLLs can cause confusing startup problems.");
            report.Add("");
            int count = 0;
            int safeCount = 0;
            int removed = 0;
            HashSet<string> available = new HashSet<string>(modules.Select(m => m.Id), StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in modules.OrderBy(m => m.Id))
            {
                foreach (string helper in HelperDllNames)
                {
                    foreach (string path in SafeGetFiles(module.Root, helper))
                    {
                        count++;
                        bool safe = IsSafeHelperCopy(module, helper, available);
                        if (safe)
                        {
                            safeCount++;
                        }

                        report.Add(module.Id + " contains helper DLL : " + path);
                        report.Add("  Status : " + (safe ? "Can clean" : "Leave alone"));
                        report.Add("  Reason : " + HelperReason(module, helper, available, safe));

                        if (safe && removeSafeCopies)
                        {
                            try
                            {
                                string backupPath = BackupHelperDll(path);
                                File.Move(path, backupPath);
                                removed++;
                                report.Add("  Action : Moved to restorable backup");
                                report.Add("  Backup : " + backupPath);
                            }
                            catch (Exception ex)
                            {
                                report.Add("  Action : Failed to move - " + ex.Message);
                            }
                        }
                    }
                }
            }
            if (count == 0)
            {
                report.Add("No duplicate helper DLLs found.");
            }
            else
            {
                report.Add("");
                report.Add("Helper DLL copies found : " + count);
                report.Add("Safe bundled copies : " + safeCount);
                report.Add("Moved to restorable backup this run : " + removed);
                report.Add("");
                report.Add("Suggestion : use Clean Duplicate DLLs only for items marked Can clean.");
            }
            report.Add("");
        }

        private static void AddLikelyXmlConflictReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Repeated XML File Names ==");
            report.Add("");
            Dictionary<string, List<string>> owners = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in modules.Where(m => !IsOfficialModule(m)))
            {
                foreach (string name in module.XmlNames)
                {
                    if (!owners.ContainsKey(name))
                    {
                        owners[name] = new List<string>();
                    }
                    owners[name].Add(module.Id);
                }
            }

            var conflicts = owners.Where(kv => kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .OrderBy(kv => kv.Key)
                .ToList();

            if (conflicts.Count == 0)
            {
                report.Add("No obvious repeated XML names found.");
            }
            else
            {
                foreach (var item in conflicts.Take(100))
                {
                    report.Add(item.Key + " appears in: " + string.Join(", ", item.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
                }
                if (conflicts.Count > 100)
                {
                    report.Add("Additional XML repeats omitted: " + (conflicts.Count - 100));
                }
            }
            report.Add("");
        }

        private static void AddXmlRecordConflictReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== XML Record Conflicts ==");
            report.Add("");
            report.Add("Only mod-vs-mod records are shown. Core game, DLC, and language XML are ignored.");
            report.Add("");

            List<ModuleInfo> modModules = modules.Where(m => !IsOfficialModule(m)).ToList();
            var records = modModules.SelectMany(m => m.XmlRecords.Select(r => new { Module = m, Record = r })).ToList();
            var conflicts = records
                .GroupBy(x => x.Record.Key, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Select(x => x.Module.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .ToList();

            if (conflicts.Count == 0)
            {
                report.Add("No same-record XML conflicts found.");
                report.Add("");
                return;
            }

            foreach (var group in conflicts.Take(150))
            {
                var entries = group
                    .OrderBy(x => x.Module.DetectedOrder)
                    .ToList();
                var winner = entries.Last();
                report.Add(group.Key);
                report.Add("  Likely overwritten by : " + winner.Module.Id + " (" + winner.Record.RelativeFile + ")");
                report.Add("  Definitions :");
                foreach (var entry in entries)
                {
                    report.Add("    [" + entry.Module.DetectedOrder + "] " + entry.Module.Id + " :: " + entry.Record.RelativeFile);
                }
            }

            if (conflicts.Count > 150)
            {
                report.Add("Additional same-record conflicts omitted: " + (conflicts.Count - 150));
            }

            report.Add("");
            report.Add("Load-order suggestion: if you want a specific mod's XML record to win, place that mod later than the other mod defining the same record.");
            report.Add("");
        }

        private static void AddAssemblyNameConflictReport(List<string> report, List<ModuleInfo> modules)
        {
            report.Add("== Duplicate Assembly File Names ==");
            report.Add("");
            Dictionary<string, List<string>> owners = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in modules)
            {
                foreach (string name in module.DllNames)
                {
                    if (!owners.ContainsKey(name))
                    {
                        owners[name] = new List<string>();
                    }
                    owners[name].Add(module.Id);
                }
            }

            var duplicates = owners.Where(kv => kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
                .OrderBy(kv => kv.Key)
                .ToList();

            if (duplicates.Count == 0)
            {
                report.Add("No duplicate DLL file names found.");
            }
            else
            {
                foreach (var item in duplicates)
                {
                    report.Add(item.Key + " appears in: " + string.Join(", ", item.Value.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
                }
            }
            report.Add("");
        }

        private static void AddBlockedDllReport(List<string> report, List<ModuleInfo> modules, bool unblock)
        {
            report.Add("== Blocked DLLs ==");
            report.Add("");
            int scanned = 0;
            int blocked = 0;
            int unblocked = 0;
            foreach (ModuleInfo module in modules.OrderBy(m => m.Id))
            {
                foreach (string dll in SafeGetFiles(module.Root, "*.dll"))
                {
                    scanned++;
                    string zone = dll + ":Zone.Identifier";
                    if (!File.Exists(zone))
                    {
                        continue;
                    }

                    blocked++;
                    if (unblock)
                    {
                        try
                        {
                            File.Delete(zone);
                            unblocked++;
                            report.Add("Unblocked : " + dll);
                        }
                        catch (Exception ex)
                        {
                            report.Add("Failed to unblock : " + dll + " :: " + ex.Message);
                        }
                    }
                    else
                    {
                    report.Add("Blocked : " + dll);
                    }
                }
            }

            if (blocked == 0)
            {
                report.Add("No blocked DLLs found.");
            }
            report.Add("");
            report.Add("DLLs scanned : " + scanned);
            report.Add("Blocked DLLs found : " + blocked);
            report.Add("DLLs unblocked : " + unblocked);
            report.Add("");
        }

        private static void AddRecentCrashLogSummary(List<string> report)
        {
            report.Add("== Recent Crash Log Clues ==");
            report.Add("");
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord");

            if (!Directory.Exists(root))
            {
                report.Add("Bannerlord Documents folder was not found.");
                report.Add("");
                return;
            }

            List<string> candidates = new List<string>();
            foreach (string pattern in new[] { "*.log", "*.txt", "*.xml" })
            {
                candidates.AddRange(SafeGetFiles(root, pattern));
            }

            candidates = candidates
                .Where(p => LooksLikeCrashLog(p))
                .OrderByDescending(File.GetLastWriteTime)
                .Take(5)
                .ToList();

            if (candidates.Count == 0)
            {
                report.Add("No recent crash-style logs found.");
                report.Add("");
                return;
            }

            foreach (string file in candidates)
            {
                report.Add(Path.GetFileName(file) + " : " + File.GetLastWriteTime(file).ToString("yyyy-MM-dd HH:mm"));
                foreach (string clue in ExtractCrashClues(file).Take(8))
                {
                    report.Add("  " + clue);
                }
                report.Add("");
            }
        }

        private static bool LooksLikeCrashLog(string path)
        {
            string name = Path.GetFileName(path);
            string full = path;
            return name.IndexOf("crash", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("rgl_log", StringComparison.OrdinalIgnoreCase) >= 0
                || full.IndexOf("crash", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<string> ExtractCrashClues(string file)
        {
            List<string> clues = new List<string>();
            try
            {
                foreach (string line in File.ReadLines(file).Take(2000))
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }
                    if (trimmed.IndexOf("exception", StringComparison.OrdinalIgnoreCase) >= 0
                        || trimmed.IndexOf("could not load", StringComparison.OrdinalIgnoreCase) >= 0
                        || trimmed.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0
                        || trimmed.IndexOf("harmony", StringComparison.OrdinalIgnoreCase) >= 0
                        || trimmed.IndexOf("xml", StringComparison.OrdinalIgnoreCase) >= 0
                        || trimmed.IndexOf("module", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        clues.Add(trimmed.Length > 180 ? trimmed.Substring(0, 180) + "..." : trimmed);
                    }
                    if (clues.Count >= 12)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                clues.Add("Could not read log: " + ex.Message);
            }
            return clues;
        }

        private static List<ModuleInfo> FindModules(string root)
        {
            List<ModuleInfo> modules = new List<ModuleInfo>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return modules;
            }

            int order = modules.Count;
            foreach (string xmlPath in SafeGetFiles(root, "SubModule.xml"))
            {
                ModuleInfo module = ParseModule(xmlPath);
                if (module != null)
                {
                    module.DetectedOrder = order++;
                    modules.Add(module);
                }
            }
            return modules;
        }

        private static void AssignDetectedOrder(List<ModuleInfo> modules)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].DetectedOrder = i + 1;
            }
        }

        private static bool ShouldIgnoreDependency(string moduleId, string dependencyId)
        {
            return string.Equals(moduleId, "VanguardCheats", StringComparison.OrdinalIgnoreCase)
                && IgnoredVanguardCheatsDependencies.Contains(dependencyId);
        }

        private static bool IsOfficialModule(ModuleInfo module)
        {
            return module != null && OfficialModuleIds.Contains(module.Id);
        }

        private static int CountRepeatedXmlFiles(List<ModuleInfo> modules)
        {
            Dictionary<string, List<string>> owners = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in modules.Where(m => !IsOfficialModule(m)))
            {
                foreach (string name in module.XmlNames)
                {
                    if (!owners.ContainsKey(name))
                    {
                        owners[name] = new List<string>();
                    }
                    owners[name].Add(module.Id);
                }
            }

            return owners.Count(kv => kv.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);
        }

        private static int CountXmlRecordConflicts(List<ModuleInfo> modules)
        {
            List<ModuleInfo> modModules = modules.Where(m => !IsOfficialModule(m)).ToList();
            return modModules
                .SelectMany(m => m.XmlRecords.Select(r => new { Module = m.Id, Record = r }))
                .GroupBy(x => x.Record.Key, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Select(x => x.Module).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1);
        }

        private static int CountBlockedDlls(List<ModuleInfo> modules)
        {
            int blocked = 0;
            foreach (ModuleInfo module in modules)
            {
                foreach (string dll in SafeGetFiles(module.Root, "*.dll"))
                {
                    if (File.Exists(dll + ":Zone.Identifier"))
                    {
                        blocked++;
                    }
                }
            }
            return blocked;
        }

        private static bool IsLanguageXmlFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            string marker = Path.DirectorySeparatorChar + "Languages" + Path.DirectorySeparatorChar;
            if (normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            string fileName = Path.GetFileName(path);
            return fileName.StartsWith("language", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("strings", StringComparison.OrdinalIgnoreCase)
                || fileName.StartsWith("std_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSafeHelperCopy(ModuleInfo module, string helperDll, HashSet<string> availableModules)
        {
            string officialModule;
            if (!HelperOfficialModules.TryGetValue(helperDll, out officialModule))
            {
                return false;
            }

            return !string.Equals(module.Id, officialModule, StringComparison.OrdinalIgnoreCase)
                && availableModules.Contains(officialModule);
        }

        private static string HelperReason(ModuleInfo module, string helperDll, HashSet<string> availableModules, bool safe)
        {
            string officialModule;
            if (!HelperOfficialModules.TryGetValue(helperDll, out officialModule))
            {
                return "This helper is not recognized by the safety rules.";
            }
            if (string.Equals(module.Id, officialModule, StringComparison.OrdinalIgnoreCase))
            {
                return "This is the official framework module copy.";
            }
            if (!availableModules.Contains(officialModule))
            {
                return "The official module " + officialModule + " was not found, so removing this copy could break the mod.";
            }
            if (safe)
            {
                return "The official module " + officialModule + " is installed, so this bundled copy is redundant.";
            }
            return "Not enough information to remove this safely.";
        }

        private static string BackupHelperDll(string originalPath)
        {
            string runFolder = Path.Combine(HelperBackupRoot(), DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(runFolder);

            string backupFileName = SanitizeFileName(originalPath) + ".dll";
            string backupPath = Path.Combine(runFolder, backupFileName);
            int index = 1;
            while (File.Exists(backupPath))
            {
                backupPath = Path.Combine(runFolder, SanitizeFileName(originalPath) + "_" + index + ".dll");
                index++;
            }

            string manifest = Path.Combine(runFolder, "manifest.tsv");
            File.AppendAllText(manifest, backupPath + "\t" + originalPath + Environment.NewLine, Encoding.UTF8);
            return backupPath;
        }

        private static string HelperBackupRoot()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HelperDllBackups");
        }

        private static string SanitizeFileName(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (Path.GetInvalidFileNameChars().Contains(c) || c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar || c == ':')
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }

        private static List<ModuleInfo> SortModules(List<ModuleInfo> modules)
        {
            Dictionary<string, ModuleInfo> byId = new Dictionary<string, ModuleInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (ModuleInfo module in modules)
            {
                if (!byId.ContainsKey(module.Id))
                {
                    byId[module.Id] = module;
                }
            }

            Dictionary<string, int> priority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            priority["Bannerlord.Harmony"] = 0;
            priority["Bannerlord.ButterLib"] = 1;
            priority["Bannerlord.UIExtenderEx"] = 2;
            priority["Bannerlord.MBOptionScreen"] = 3;
            priority["Native"] = 10;
            priority["SandBoxCore"] = 11;
            priority["Sandbox"] = 12;
            priority["StoryMode"] = 13;
            priority["CustomBattle"] = 14;

            List<ModuleInfo> result = new List<ModuleInfo>();
            HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ModuleInfo module in modules
                .OrderBy(m => priority.ContainsKey(m.Id) ? priority[m.Id] : 100)
                .ThenBy(m => m.DetectedOrder)
                .ThenBy(m => m.Id))
            {
                Visit(module, byId, visited, visiting, result);
            }

            return result;
        }

        private static void Visit(
            ModuleInfo module,
            Dictionary<string, ModuleInfo> byId,
            HashSet<string> visited,
            HashSet<string> visiting,
            List<ModuleInfo> result)
        {
            if (visited.Contains(module.Id))
            {
                return;
            }
            if (visiting.Contains(module.Id))
            {
                return;
            }

            visiting.Add(module.Id);
            foreach (string dependency in module.Dependencies
                .Where(d => !ShouldIgnoreDependency(module.Id, d))
                .OrderBy(d => d))
            {
                ModuleInfo dependencyModule;
                if (byId.TryGetValue(dependency, out dependencyModule))
                {
                    Visit(dependencyModule, byId, visited, visiting, result);
                }
            }
            visiting.Remove(module.Id);
            visited.Add(module.Id);
            result.Add(module);
        }

        private static ModuleInfo ParseModule(string xmlPath)
        {
            try
            {
                XDocument doc = XDocument.Load(xmlPath);
                XElement root = doc.Root;
                if (root == null)
                {
                    return null;
                }

                string folder = Path.GetDirectoryName(xmlPath);
                ModuleInfo module = new ModuleInfo();
                module.SubModuleXml = xmlPath;
                module.Root = folder;
                module.Id = GetValue(root.Element("Id"));
                module.Name = GetValue(root.Element("Name"));
                if (string.IsNullOrWhiteSpace(module.Id))
                {
                    module.Id = new DirectoryInfo(folder).Name;
                }
                if (string.IsNullOrWhiteSpace(module.Name))
                {
                    module.Name = module.Id;
                }

                foreach (XElement dep in root.Descendants("DependedModule"))
                {
                    string id = GetValue(dep);
                    if (!string.IsNullOrWhiteSpace(id) && !module.Dependencies.Contains(id, StringComparer.OrdinalIgnoreCase))
                    {
                        module.Dependencies.Add(id);
                    }
                }

                foreach (string file in SafeGetFiles(folder, "*.xml"))
                {
                    string name = Path.GetFileName(file);
                    if (!string.Equals(name, "SubModule.xml", StringComparison.OrdinalIgnoreCase) && !IsLanguageXmlFile(file))
                    {
                        module.XmlNames.Add(name);
                        module.XmlRecords.AddRange(ParseXmlRecords(folder, file));
                    }
                }

                foreach (string file in SafeGetFiles(folder, "*.dll"))
                {
                    module.DllNames.Add(Path.GetFileName(file));
                }

                return module;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<XmlRecord> ParseXmlRecords(string moduleRoot, string xmlPath)
        {
            List<XmlRecord> records = new List<XmlRecord>();
            if (IsLanguageXmlFile(xmlPath))
            {
                return records;
            }
            try
            {
                XDocument doc = XDocument.Load(xmlPath, LoadOptions.None);
                foreach (XElement element in doc.Descendants())
                {
                    string id = GetRecordId(element);
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    string relative = MakeRelative(moduleRoot, xmlPath);
                    string fileName = Path.GetFileName(xmlPath);
                    XmlRecord record = new XmlRecord();
                    record.ElementName = element.Name.LocalName;
                    record.Id = id;
                    record.File = xmlPath;
                    record.RelativeFile = relative;
                    record.Key = fileName + "::" + record.ElementName + "::" + id;
                    records.Add(record);
                }
            }
            catch
            {
            }
            return records;
        }

        private static string GetRecordId(XElement element)
        {
            foreach (string name in new[] { "id", "Id", "name", "Name", "type", "Type" })
            {
                XAttribute attr = element.Attribute(name);
                if (attr != null && !string.IsNullOrWhiteSpace(attr.Value))
                {
                    return attr.Value.Trim();
                }
            }
            return null;
        }

        private static string MakeRelative(string root, string file)
        {
            try
            {
                Uri rootUri = new Uri(AppendSlash(root));
                Uri fileUri = new Uri(file);
                return Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
            catch
            {
                return file;
            }
        }

        private static string AppendSlash(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString()) || path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                return path;
            }
            return path + Path.DirectorySeparatorChar;
        }

        private static string GetValue(XElement element)
        {
            if (element == null)
            {
                return null;
            }
            XAttribute attr = element.Attribute("value") ?? element.Attribute("Value") ?? element.Attribute("Id") ?? element.Attribute("id");
            return attr == null ? null : attr.Value;
        }

        private static IEnumerable<string> SafeGetFiles(string root, string pattern)
        {
            try
            {
                if (!Directory.Exists(root))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.GetFiles(root, pattern, SearchOption.AllDirectories);
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static string ReportKind(ScanOptions options)
        {
            if (options.RemoveSafeHelperDlls)
            {
                return "SafeHelperCleanup";
            }
            if (options.UnblockDlls)
            {
                return "UnblockDlls";
            }
            if (options.Quick)
            {
                return "QuickScan";
            }
            return "FullScan";
        }

        private static string LauncherDataPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Mount and Blade II Bannerlord",
                "Configs",
                "LauncherData.xml");
        }

        private static string WriteReport(string report, string kind)
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(folder);
            string safeKind = string.IsNullOrWhiteSpace(kind) ? "Report" : kind;
            string path = Path.Combine(folder, "BannerlordModDoctor_" + safeKind + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");
            File.WriteAllText(path, report, Encoding.UTF8);
            return path;
        }
    }
}

