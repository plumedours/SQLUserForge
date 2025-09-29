using Microsoft.Data.SqlClient;
using SQLUserForge.Models;
using SQLUserForge.Services;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLUserForge.Forms
{
    public class MainForm : Form
    {
        // Menu
        private MenuStrip _menu = default!;
        private ToolStripMenuItem _mFile = default!, _mAbout = default!, _mQuit = default!;
        private ToolStripMenuItem _mLanguage = default!, _mLangEN = default!, _mLangFR = default!;

        // Connexion / Auth
        private ComboBox _cbInstance = default!;
        private RadioButton _rbWindows = default!;
        private RadioButton _rbSql = default!;
        private TextBox _tbAdminLogin = default!;
        private TextBox _tbAdminPwd = default!;

        // Labels
        private Label _lblInstance = default!;
        private Label _lblDb = default!;
        private Label _lblLogin = default!;
        private Label _lblPwd2 = default!;

        // GroupBoxes
        private GroupBox _grpInstance = default!;
        private GroupBox _grpAuth = default!;
        private GroupBox _grpDbRoles = default!;
        private GroupBox _grpSrvRoles = default!;
        private GroupBox _grpLogin = default!;

        // Cible
        private ComboBox _cbDatabase = default!;
        private CheckedListBox _clbDbRoles = default!;
        private CheckedListBox _clbSrvRoles = default!;

        // Login à créer
        private TextBox _tbNewLogin = default!;
        private TextBox _tbNewPwd = default!;

        private Button _btnCreate = default!;
        private Button _btnQuitBtn = default!; // renommer pour ne pas masquer _mQuit

        public MainForm()
        {
            Text = TranslationProvider.T("app_title");
            MinimumSize = new Size(860, 580);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            AutoScaleMode = AutoScaleMode.Dpi;
            this.Icon = Properties.Resources.AppIcon;

            TranslationProvider.LanguageChanged += (_, __) => ApplyTranslations();

            BuildUi();
            LoadRoleLists();
            ApplyTranslations();
            // chargement async des instances après apparition
            this.Shown += async (_, __) => await LoadInstancesAsync();
        }

        private void BuildUi()
        {
            int margin = 16;

            // ===== Menu =====
            _menu = new MenuStrip { Dock = DockStyle.Top };
            _mFile = new ToolStripMenuItem();
            _mAbout = new ToolStripMenuItem();
            _mQuit = new ToolStripMenuItem();
            _mLanguage = new ToolStripMenuItem();
            _mLangEN = new ToolStripMenuItem();
            _mLangFR = new ToolStripMenuItem();

            _mAbout.Click += (_, __) => ShowAbout();
            _mQuit.Click += (_, __) => Close();
            _mLangEN.Click += (_, __) => { TranslationProvider.SetLanguage("en"); UpdateLangChecks(); };
            _mLangFR.Click += (_, __) => { TranslationProvider.SetLanguage("fr"); UpdateLangChecks(); };

            _menu.Items.AddRange(new ToolStripItem[] { _mFile, _mLanguage });
            this.MainMenuStrip = _menu;
            Controls.Add(_menu);

            // ===== Pied (boutons) =====
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(0, 8, 16, 8),
                BackColor = Color.White
            };

            var flowRight = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true
            };

            _btnCreate = new Button { Width = 128, Height = 32, Margin = new Padding(0, 0, 8, 0) };
            _btnCreate.Click += OnCreateClick;

            _btnQuitBtn = new Button { Width = 128, Height = 32, Margin = new Padding(0) };
            _btnQuitBtn.Click += (_, __) => Close();

            flowRight.Controls.Add(_btnCreate);
            flowRight.Controls.Add(_btnQuitBtn);
            bottomPanel.Controls.Add(flowRight);
            Controls.Add(bottomPanel);

            // ===== Layout central =====
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(margin, margin + 8, margin, margin),
                ColumnCount = 2,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            // ===== GroupBox Instance (gauche) =====
            _grpInstance = new GroupBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 8, 0),
                Padding = new Padding(8)
            };

            var instanceLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            instanceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            instanceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            // labels stockés
            _lblInstance = new Label { AutoSize = true, Dock = DockStyle.Top };
            _cbInstance = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Top, Margin = new Padding(0, 0, 8, 0) };

            _lblDb = new Label { AutoSize = true, Dock = DockStyle.Top };
            _cbDatabase = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, Margin = new Padding(8, 0, 0, 0) };

            var panelInstance = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
            panelInstance.Controls.Add(_cbInstance);
            panelInstance.Controls.Add(_lblInstance);

            var panelDb = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
            panelDb.Controls.Add(_cbDatabase);
            panelDb.Controls.Add(_lblDb);

            instanceLayout.Controls.Add(panelInstance, 0, 0);
            instanceLayout.Controls.Add(panelDb, 1, 0);

            _grpInstance.Controls.Add(instanceLayout);

            // ===== GroupBox Auth (droite) =====
            _grpAuth = new GroupBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(8, 4, 0, 0),
                Padding = new Padding(8)
            };

            var authLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            authLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            authLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            _rbWindows = new RadioButton { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 0, 8, 0) };
            _rbWindows.Checked = true;
            _rbWindows.CheckedChanged += (_, __) => ToggleSqlFields();

            _rbSql = new RadioButton { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(8, 0, 0, 0) };
            _rbSql.CheckedChanged += (_, __) => ToggleSqlFields();

            authLayout.Controls.Add(_rbWindows, 0, 0);
            authLayout.Controls.Add(_rbSql, 1, 0);

            _tbAdminLogin = new TextBox { Dock = DockStyle.Top, Enabled = false, Margin = new Padding(0, 4, 8, 0) };
            _tbAdminPwd = new TextBox { Dock = DockStyle.Top, Enabled = false, UseSystemPasswordChar = true, Margin = new Padding(8, 4, 0, 0) };

            authLayout.Controls.Add(_tbAdminLogin, 0, 1);
            authLayout.Controls.Add(_tbAdminPwd, 1, 1);

            _grpAuth.Controls.Add(authLayout);

            root.Controls.Add(_grpInstance, 0, 0);
            root.Controls.Add(_grpAuth, 1, 0);

            // ===== Rôles (identiques à avant) =====
            _grpDbRoles = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 18, 8, 8),
                Margin = new Padding(0, 8, 8, 2)
            };
            _clbDbRoles = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            var dbRolesHost = new Panel { Dock = DockStyle.Top, Height = 220 };
            dbRolesHost.Controls.Add(_clbDbRoles);
            _grpDbRoles.Controls.Add(dbRolesHost);

            _grpSrvRoles = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 18, 8, 8),
                Margin = new Padding(8, 8, 0, 2)
            };
            _clbSrvRoles = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false };
            var srvRolesHost = new Panel { Dock = DockStyle.Top, Height = 220 };
            srvRolesHost.Controls.Add(_clbSrvRoles);
            _grpSrvRoles.Controls.Add(srvRolesHost);

            root.Controls.Add(_grpDbRoles, 0, 1);
            root.Controls.Add(_grpSrvRoles, 1, 1);

            // ===== Bloc login =====
            _grpLogin = new GroupBox
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8, 12, 8, 8),
                Margin = new Padding(0, 10, 0, 0)
            };
            var loginRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            loginRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            loginRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            loginRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            loginRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var lblLogin = new Label { AutoSize = true, Margin = new Padding(0, 0, 8, 8) };
            var lblPwd2 = new Label { AutoSize = true, Margin = new Padding(16, 0, 8, 8) };
            _lblLogin = new Label { AutoSize = true, Margin = new Padding(0, 0, 8, 8) };
            _tbNewLogin = new TextBox { Dock = DockStyle.Top, Margin = new Padding(0, 0, 8, 8) };
            _lblPwd2 = new Label { AutoSize = true, Margin = new Padding(16, 0, 8, 8) };
            _tbNewPwd = new TextBox { UseSystemPasswordChar = true, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8) };

            loginRow.Controls.Add(lblLogin, 0, 0);
            loginRow.Controls.Add(_tbNewLogin, 1, 0);
            loginRow.Controls.Add(lblPwd2, 2, 0);
            loginRow.Controls.Add(_tbNewPwd, 3, 0);
            _grpLogin.Controls.Add(loginRow);

            root.Controls.Add(_grpLogin, 0, 2);
            root.SetColumnSpan(_grpLogin, 2);

            // Auto-refresh DB quand instance choisie
            _cbInstance.SelectedIndexChanged += (_, __) =>
            {
                if (_cbInstance.SelectedIndex > 0) RefreshDatabases();
                else { _cbDatabase.Items.Clear(); _cbDatabase.Text = ""; }
            };

            ApplyTranslations();
        }

        private void ApplyTranslations()
        {
            Text = TranslationProvider.T("app_title");

            // Menu
            _mFile.Text = TranslationProvider.T("menu_file");
            _mAbout.Text = TranslationProvider.T("menu_about");
            _mQuit.Text = TranslationProvider.T("menu_quit");
            _mLanguage.Text = TranslationProvider.T("menu_language");
            _mLangEN.Text = TranslationProvider.T("menu_lang_en");
            _mLangFR.Text = TranslationProvider.T("menu_lang_fr");

            _mFile.DropDownItems.Clear();
            _mFile.DropDownItems.AddRange(new ToolStripItem[]
            {
        _mAbout,
        new ToolStripSeparator(),
        _mQuit
            });
            _mLanguage.DropDownItems.Clear();
            _mLanguage.DropDownItems.AddRange(new ToolStripItem[] { _mLangEN, _mLangFR });
            UpdateLangChecks();

            // GroupBox Instance
            _grpInstance.Text = TranslationProvider.T("grp_instance_title");
            _lblInstance.Text = TranslationProvider.T("label_instance");
            _lblDb.Text = TranslationProvider.T("label_default_db");

            // GroupBox Auth
            _grpAuth.Text = TranslationProvider.T("grp_auth_title");
            _rbWindows.Text = TranslationProvider.T("rb_auth_windows");
            _rbSql.Text = TranslationProvider.T("rb_auth_sql");
            _tbAdminLogin.PlaceholderText = TranslationProvider.T("ph_admin_login");
            _tbAdminPwd.PlaceholderText = TranslationProvider.T("ph_admin_password");

            // Rôles
            _grpDbRoles.Text = TranslationProvider.T("grp_db_roles");
            _grpSrvRoles.Text = TranslationProvider.T("grp_srv_roles");

            // Bloc login
            _grpLogin.Text = TranslationProvider.T("grp_login_title");
            _lblLogin.Text = TranslationProvider.T("label_login_name");
            _lblPwd2.Text = TranslationProvider.T("label_password");
            _tbNewLogin.PlaceholderText = "ex: reporting";
            _tbNewPwd.PlaceholderText = TranslationProvider.T("label_password");

            // Boutons
            _btnCreate.Text = TranslationProvider.T("btn_validate");
            _btnQuitBtn.Text = TranslationProvider.T("btn_quit");
        }

        private void UpdateLangChecks()
        {
            _mLangEN.Checked = string.Equals(TranslationProvider.CurrentLang, "en", StringComparison.OrdinalIgnoreCase);
            _mLangFR.Checked = string.Equals(TranslationProvider.CurrentLang, "fr", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowAbout()
        {
            using var form = new Form
            {
                Text = TranslationProvider.T("about_title"),
                Size = new Size(400, 220),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F)
            };

            var lblText = new Label
            {
                Text = TranslationProvider.T("about_text"),
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 100,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var link = new LinkLabel
            {
                Text = TranslationProvider.T("about_link"),
                Dock = DockStyle.Bottom,
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic)
            };
            link.Links[0].LinkData = "https://github.com/plumedours";
            link.LinkClicked += (s, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Link.LinkData.ToString(),
                    UseShellExecute = true
                });
            };

            form.Controls.Add(link);
            form.Controls.Add(lblText);

            form.ShowDialog(this);
        }

        private void ToggleSqlFields()
        {
            bool sql = _rbSql.Checked;
            _tbAdminLogin.Enabled = sql;
            _tbAdminPwd.Enabled = sql;
        }

        private async Task LoadInstancesAsync()
        {
            SetUiEnabled(false);
            Application.UseWaitCursor = true;
            Cursor = Cursors.WaitCursor;

            var dlg = new LoadingDialog(TranslationProvider.T("loading_discovering"));
            BeginInvoke(new MethodInvoker(() => dlg.Show(this)));

            try
            {
                var instances = await Task.Run(() => SQLUserForge.Services.SqlHelper.EnumerateLocalInstances().ToList());

                _cbInstance.Items.Clear();
                _cbInstance.Items.Add("-- " + TranslationProvider.T("label_instance") + " --"); // placeholder
                foreach (var s in instances) _cbInstance.Items.Add(s);
                _cbInstance.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Discovery", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!dlg.IsDisposed) dlg.BeginInvoke(new MethodInvoker(() => dlg.Close()));
                Application.UseWaitCursor = false;
                Cursor = Cursors.Default;
                SetUiEnabled(true);
            }
        }

        private void SetUiEnabled(bool enabled)
        {
            _cbInstance.Enabled = enabled;
            _cbDatabase.Enabled = enabled;
            _rbWindows.Enabled = enabled;
            _rbSql.Enabled = enabled;
            _tbAdminLogin.Enabled = enabled && _rbSql.Checked;
            _tbAdminPwd.Enabled = enabled && _rbSql.Checked;
        }

        private void LoadRoleLists()
        {
            string[] dbRoles =
            {
                "db_owner","db_datareader","db_datawriter","db_ddladmin",
                "db_securityadmin","db_backupoperator","db_accessadmin"
            };
            string[] serverRoles =
            {
                "sysadmin","serveradmin","securityadmin","processadmin",
                "setupadmin","bulkadmin","diskadmin"
            };

            _clbDbRoles.Items.Clear();
            _clbDbRoles.Items.AddRange(dbRoles);
            _clbSrvRoles.Items.Clear();
            _clbSrvRoles.Items.AddRange(serverRoles);
        }

        private UserRequest? BuildRequestOrShowError()
        {
            string instance = _cbInstance.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(instance))
            {
                MessageBox.Show(TranslationProvider.T("errors_instance_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            bool useIntegrated = _rbWindows.Checked;
            string? adminLogin = _tbAdminLogin.Text?.Trim();
            string? adminPwd = _tbAdminPwd.Text;

            if (!useIntegrated)
            {
                if (string.IsNullOrWhiteSpace(adminLogin) || string.IsNullOrWhiteSpace(adminPwd))
                {
                    MessageBox.Show(TranslationProvider.T("errors_auth_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }

            string db = _cbDatabase.SelectedItem?.ToString() ?? _cbDatabase.Text;
            if (string.IsNullOrWhiteSpace(db))
            {
                MessageBox.Show(TranslationProvider.T("errors_db_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string newLogin = _tbNewLogin.Text?.Trim() ?? "";
            string newPwd = _tbNewPwd.Text ?? "";
            if (string.IsNullOrWhiteSpace(newLogin) || string.IsNullOrWhiteSpace(newPwd))
            {
                MessageBox.Show(TranslationProvider.T("errors_login_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var dbRoles = _clbDbRoles.CheckedItems.Cast<string>().ToArray();
            var srvRoles = _clbSrvRoles.CheckedItems.Cast<string>().ToArray();

            return new UserRequest
            {
                ServerInstance = instance,
                UseIntegratedSecurity = useIntegrated,
                AdminLogin = useIntegrated ? null : adminLogin,
                AdminPassword = useIntegrated ? null : adminPwd,
                TargetDatabase = db,
                NewLoginName = newLogin,
                NewLoginPassword = newPwd,
                SelectedDbRoles = dbRoles,
                SelectedServerRoles = srvRoles
            };
        }

        private void RefreshDatabases()
        {
            var req = BuildRequestOrShowErrorForDbList();
            if (req is null) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                var dbs = Services.SqlHelper.GetDatabases(req);
                _cbDatabase.Items.Clear();
                foreach (var d in dbs) _cbDatabase.Items.Add(d);
                if (_cbDatabase.Items.Count > 0) _cbDatabase.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private UserRequest? BuildRequestOrShowErrorForDbList()
        {
            string instance = _cbInstance.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(instance))
            {
                MessageBox.Show(TranslationProvider.T("errors_instance_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            bool useIntegrated = _rbWindows.Checked;
            string? adminLogin = _tbAdminLogin.Text?.Trim();
            string? adminPwd = _tbAdminPwd.Text;

            if (!useIntegrated)
            {
                if (string.IsNullOrWhiteSpace(adminLogin) || string.IsNullOrWhiteSpace(adminPwd))
                {
                    MessageBox.Show(TranslationProvider.T("errors_auth_required"), TranslationProvider.T("btn_validate"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
            }

            return new UserRequest
            {
                ServerInstance = instance,
                UseIntegratedSecurity = useIntegrated,
                AdminLogin = useIntegrated ? null : adminLogin,
                AdminPassword = useIntegrated ? null : adminPwd
            };
        }

        private void OnCreateClick(object? sender, EventArgs e)
        {
            var req = BuildRequestOrShowError();
            if (req is null) return;

            using var dlg = new ConfirmDialog(req);
            var dr = dlg.ShowDialog(this);
            if (dr != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;

                Services.SqlHelper.CreateLogin(req);
                Services.SqlHelper.CreateDbUserAndRoles(req);

                MessageBox.Show(TranslationProvider.T("success_body"),
                                TranslationProvider.T("success_title"),
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (SqlException sqlex)
            {
                MessageBox.Show(sqlex.Message, "SQL error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }
    }
}