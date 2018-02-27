﻿using System;
using System.Windows.Forms;
using MigAz.Providers;
using MigAz.Core.Interface;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using MigAz.Azure;
using MigAz.Azure.Generator;
using MigAz.Azure.UserControls;
using MigAz.Azure.Generator.AsmToArm;
using MigAz.Azure.Interface;
using MigAz.Azure.Forms;
using MigAz.UserControls;
using MigAz.Core;
using MigAz.AzureStack.UserControls;
using MigAz.Azure.MigrationTarget;

namespace MigAz.Forms
{
    public partial class MigAzForm : Form
    {
        #region Variables

        private Guid _AppSessionGuid = Guid.NewGuid();
        private FileLogProvider _logProvider;
        private IStatusProvider _statusProvider;
        private AppSettingsProvider _appSettingsProvider;
        private AzureTelemetryProvider _telemetryProvider = new AzureTelemetryProvider();
        private TreeNode _EventSourceNode;

        #endregion

        #region Constructors
        public MigAzForm()
        {
            InitializeComponent();
            _logProvider = new FileLogProvider();
            _logProvider.OnMessage += _logProvider_OnMessage;
            _statusProvider = new UIStatusProvider(this.toolStripStatusLabel1);
            _appSettingsProvider = new AppSettingsProvider();

            txtDestinationFolder.Text = AppDomain.CurrentDomain.BaseDirectory;
            propertyPanel1.Clear();
            splitContainer2.SplitterDistance = this.Height / 2;
            splitContainer3.SplitterDistance = splitContainer3.Width / 2;
            splitContainer4.SplitterDistance = 45;

            lblLastOutputRefresh.Text = String.Empty;

            // Future thought, do away with the "Set"s and consolidate to a Bind?
            this.targetTreeView1.LogProvider = this.LogProvider;
            this.targetTreeView1.StatusProvider = this.StatusProvider;
            this.targetTreeView1.ImageList = this.imageList1;
            this.targetTreeView1.TargetSettings = _appSettingsProvider.GetTargetSettings();

            this.propertyPanel1.TargetTreeView = targetTreeView1;
            this.propertyPanel1.PropertyChanged += PropertyPanel1_PropertyChanged;


            AlertIfNewVersionAvailable();
        }

        #region New Version Check

        private async Task AlertIfNewVersionAvailable()
        {
            string currentVersion = "2.3.4.0";
            VersionCheck versionCheck = new VersionCheck(this.LogProvider);
            string newVersionNumber = await versionCheck.GetAvailableVersion("https://migaz.azurewebsites.net/api/v2", currentVersion);
            if (versionCheck.IsVersionNewer(currentVersion, newVersionNumber))
            {
                NewVersionAvailableDialog newVersionDialog = new NewVersionAvailableDialog();
                newVersionDialog.Bind(currentVersion, newVersionNumber);
                newVersionDialog.ShowDialog();
            }
        }

        #endregion

        #region Azure Migration Source Context Events

        private void MigrationSourceControl_ClearContext()
        {
            this.propertyPanel1.Clear();
            this.targetTreeView1.Clear();

            dgvMigAzMessages.DataSource = null;
            btnRefreshOutput.Enabled = false;
            btnExport.Enabled = false;

            foreach (TabPage t in tabOutputResults.TabPages)
            {
                tabOutputResults.TabPages.Remove(t);
            }
        }

        private async Task MigrationSourceControl_AfterNodeChecked(MigrationTarget sender)
        {
            TreeNode resultUpdateARMTree = await targetTreeView1.AddMigrationTarget(sender);
        }

        private async Task MigrationSourceControl_AfterNodeUnchecked(MigrationTarget sender)
        {
            await targetTreeView1.RemoveMigrationTarget(sender);
        }

        private async Task MigrationSourceControl_AfterNodeChanged(MigrationTarget sender)
        {
            await targetTreeView1.RefreshExportArtifacts();
        }

        private async Task MigrationSourceControl_BeforeAzureTenantChange(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                migrationTargetAzure.ExistingContext = sender;
            }

        }

        private async Task MigrationSourceControl_AfterAzureTenantChange(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                migrationTargetAzure.ExistingContext = sender;
            }

        }

        private async Task MigrationSourceControl_AfterUserSignOut()
        {
        }

        private async Task MigrationSourceControl_BeforeUserSignOut()
        {
        }

        private async Task MigrationSourceControl_AfterAzureSubscriptionChange(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                migrationTargetAzure.ExistingContext = sender;
            }
        }

        private async Task MigrationSourceControl_BeforeAzureSubscriptionChange(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                migrationTargetAzure.ExistingContext = sender;
            }
        }

        private async Task MigrationSourceControl_UserAuthenticated(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl != null)
            {
                if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
                {
                    MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                    migrationTargetAzure.ExistingContext = sender;
                }
            }

            this.targetTreeView1.Enabled = true;
        }

        private async Task MigrationSourceControl_AzureEnvironmentChanged(AzureContext sender)
        {
            IMigrationTargetUserControl migrationTargetUserControl = this.MigrationTargetControl;

            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext migrationTargetAzure = (MigrationAzureTargetContext)migrationTargetUserControl;
                migrationTargetAzure.ExistingContext = sender;
            }
        }

        #endregion

        #region Form Objects

        private IMigrationSourceUserControl MigrationSourceControl
        {
            get
            {
                foreach (Control control in splitContainer3.Panel1.Controls)
                {
                    if (control.GetType().GetInterfaces().Contains(typeof(IMigrationSourceUserControl)))
                    {
                        IMigrationSourceUserControl migrationSourceControl = (IMigrationSourceUserControl)control;
                        return migrationSourceControl;
                    }
                }

                return null;
            }
        }

        private IMigrationTargetUserControl MigrationTargetControl
        {
            get
            {
                foreach (Control control in splitContainer4.Panel1.Controls)
                {
                    if (control.GetType().GetInterfaces().Contains(typeof(IMigrationTargetUserControl)))
                    {
                        IMigrationTargetUserControl migrationSourceControl = (IMigrationTargetUserControl)control;
                        return migrationSourceControl;
                    }
                }

                return null;
            }
        }

        #endregion

        private async Task PropertyPanel1_PropertyChanged(Core.MigrationTarget migrationTarget)
        {
            TreeNode targetNode = this.targetTreeView1.SeekMigrationTargetTreeNode(migrationTarget);

            if (targetNode != null)
            {
                targetNode.Tag = migrationTarget;
                targetNode.Text = migrationTarget.ToString();

                if (migrationTarget.GetType() == typeof(Azure.MigrationTarget.ResourceGroup))
                    targetNode.Name = migrationTarget.ToString();
            }

            await this.targetTreeView1.RefreshExportArtifacts();
        }

        private void _logProvider_OnMessage(string message)
        {
            txtLog.AppendText(message);
            txtLog.SelectionStart = txtLog.TextLength;
        }

        private void AzureRetriever_OnRestResult(AzureRestResponse response)
        {
            txtRest.AppendText(response.RequestGuid.ToString() + " " + response.Url + Environment.NewLine);
            txtRest.AppendText(response.Response + Environment.NewLine + Environment.NewLine);
            txtRest.SelectionStart = txtRest.TextLength;
        }

        #endregion

        #region Properties

        public ILogProvider LogProvider
        {
            get { return _logProvider; }
        }

        public IStatusProvider StatusProvider
        {
            get { return _statusProvider; }
        }

        internal AppSettingsProvider AppSettingsProvider
        {
            get { return _appSettingsProvider; }
        }

        #endregion

        #region Form Events

        private void MigAzForm_Load(object sender, EventArgs e)
        {
            _logProvider.WriteLog("MigAzForm_Load", "Program start");
            this.Text = "MigAz";
        }

        #endregion

        private void toolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://aka.ms/MigAz");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabControl1_Resize(object sender, EventArgs e)
        {
            dgvMigAzMessages.Width = tabMigAzMonitoring.Width - 10;
            dgvMigAzMessages.Height = tabMigAzMonitoring.Height - 30;
            txtLog.Width = tabMigAzMonitoring.Width - 10;
            txtLog.Height = tabMigAzMonitoring.Height - 30;
            txtRest.Width = tabMigAzMonitoring.Width - 10;
            txtRest.Height = tabMigAzMonitoring.Height - 30;
        }

        private void panel1_Resize(object sender, EventArgs e)
        {
            btnExport.Width = panel1.Width - 15;
            btnChoosePath.Left = panel1.Width - btnChoosePath.Width - 10;
            txtDestinationFolder.Width = panel1.Width - btnChoosePath.Width - 30;
        }

        private void reportAnIssueOnGithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Azure/migAz/issues/new");
        }

        private void visitMigAzOnGithubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://aka.ms/migaz");
        }

        private async void btnExport_Click_1Async(object sender, EventArgs e)
        {
            // We are refreshing both the MemoryStreams and the Output Tabs via this call, prior to writing to files
            if (await RefreshOutput())
            {
                IMigrationTargetUserControl migrationTargetControl = this.MigrationTargetControl;
                if (migrationTargetControl != null)
                {
                    if (migrationTargetControl.GetType() == typeof(MigrationAzureTargetContext))
                    {
                        MigrationAzureTargetContext azureTargetContext = (MigrationAzureTargetContext)migrationTargetControl;

                        if (azureTargetContext.TemplateGenerator != null)
                        {
                            azureTargetContext.TemplateGenerator.OutputDirectory = txtDestinationFolder.Text;

                            azureTargetContext.TemplateGenerator.Write();

                            StatusProvider.UpdateStatus("Ready");

                            var exportResults = new ExportResultsDialog(azureTargetContext.TemplateGenerator);
                            exportResults.ShowDialog(this);
                        }
                    }
                }
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            TargetTreeView targetTreeView = this.targetTreeView1;

            if (targetTreeView != null && e.RowIndex > -1)
            {
                object alert = dgvMigAzMessages.Rows[e.RowIndex].Cells["SourceObject"].Value;
                targetTreeView.SeekAlertSource(alert);
            }
        }

        private void tabOutputResults_Resize(object sender, EventArgs e)
        {
            foreach (TabPage tabPage in tabOutputResults.TabPages)
            {
                foreach (Control control in tabPage.Controls)
                {
                    control.Width = tabOutputResults.Width - 15;
                    control.Height = tabOutputResults.Height - 30;
                }
            }
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsDialog optionsDialog = new OptionsDialog();
            optionsDialog.ShowDialog();

            IMigrationSourceUserControl sourceUserControl = this.MigrationSourceControl;
            if (sourceUserControl != null)
            {
                if (sourceUserControl.GetType() == typeof(MigrationAzureSourceContext))
                {
                    MigrationAzureSourceContext migrationAzureSourceContext = (MigrationAzureSourceContext)sourceUserControl;
                    migrationAzureSourceContext.AzureContext.LoginPromptBehavior = app.Default.LoginPromptBehavior;
                }
            }
        }

        private void btnChoosePath_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
                txtDestinationFolder.Text = folderBrowserDialog1.SelectedPath;
        }

        private async void btnRefreshOutput_Click(object sender, EventArgs e)
        {
            await RefreshOutput();
        }

        private async Task<bool> RefreshOutput()
        {
            if (AssertHasTargetErrors())
            {
                return false;
            }

            IMigrationSourceUserControl migrationSourceControl = this.MigrationSourceControl;
            if (migrationSourceControl == null)
                throw new ArgumentException("Unable to Refresh Output:  NULL MigrationSourceControl Context")
;
            IMigrationTargetUserControl migrationTargetControl = this.MigrationTargetControl;
            if (migrationTargetControl == null)
                throw new ArgumentException("Unable to Refresh Output:  NULL MigrationTargetControl Context")
;
            if (migrationTargetControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext azureTargetContext = (MigrationAzureTargetContext)migrationTargetControl;

                if (azureTargetContext.TemplateGenerator != null)
                {
                    // todo now russell, this needs to be improved to handle error of wrong type
                    azureTargetContext.TemplateGenerator.SourceSubscription = ((MigrationAzureSourceContext)migrationSourceControl).AzureContext.AzureSubscription;
                    azureTargetContext.TemplateGenerator.TargetSubscription = azureTargetContext.AzureContext.AzureSubscription;
                    azureTargetContext.TemplateGenerator.AccessSASTokenLifetimeSeconds = app.Default.AccessSASTokenLifetimeSeconds;
                    azureTargetContext.TemplateGenerator.ExportArtifacts = this.targetTreeView1.ExportArtifacts;

                    await azureTargetContext.TemplateGenerator.GenerateStreams();

                    foreach (TabPage tabPage in tabOutputResults.TabPages)
                    {
                        if (!azureTargetContext.TemplateGenerator.TemplateStreams.ContainsKey(tabPage.Name))
                            tabOutputResults.TabPages.Remove(tabPage);
                    }

                    foreach (var templateStream in azureTargetContext.TemplateGenerator.TemplateStreams)
                    {
                        TabPage tabPage = null;
                        if (!tabOutputResults.TabPages.ContainsKey(templateStream.Key))
                        {
                            tabPage = new TabPage(templateStream.Key);
                            tabPage.Name = templateStream.Key;
                            tabOutputResults.TabPages.Add(tabPage);

                            if (templateStream.Key.EndsWith(".html"))
                            {
                                WebBrowser webBrowser = new WebBrowser();
                                webBrowser.Width = tabOutputResults.Width - 15;
                                webBrowser.Height = tabOutputResults.Height - 30;
                                webBrowser.AllowNavigation = false;
                                webBrowser.ScrollBarsEnabled = true;
                                tabPage.Controls.Add(webBrowser);
                            }
                            else if (templateStream.Key.EndsWith(".json") || templateStream.Key.EndsWith(".ps1"))
                            {
                                TextBox textBox = new TextBox();
                                textBox.Width = tabOutputResults.Width - 15;
                                textBox.Height = tabOutputResults.Height - 30;
                                textBox.ReadOnly = true;
                                textBox.Multiline = true;
                                textBox.WordWrap = false;
                                textBox.ScrollBars = ScrollBars.Both;
                                tabPage.Controls.Add(textBox);
                            }
                        }
                        else
                        {
                            tabPage = tabOutputResults.TabPages[templateStream.Key];
                        }

                        if (tabPage.Controls[0].GetType() == typeof(TextBox))
                        {
                            TextBox textBox = (TextBox)tabPage.Controls[0];
                            templateStream.Value.Position = 0;
                            textBox.Text = new StreamReader(templateStream.Value).ReadToEnd();
                        }
                        else if (tabPage.Controls[0].GetType() == typeof(WebBrowser))
                        {
                            WebBrowser webBrowser = (WebBrowser)tabPage.Controls[0];
                            templateStream.Value.Position = 0;

                            if (webBrowser.Document == null)
                            {
                                webBrowser.DocumentText = new StreamReader(templateStream.Value).ReadToEnd();
                            }
                            else
                            {
                                webBrowser.Document.OpenNew(true);
                                webBrowser.Document.Write(new StreamReader(templateStream.Value).ReadToEnd());
                            }
                        }
                    }

                    if (tabOutputResults.TabPages.Count != azureTargetContext.TemplateGenerator.TemplateStreams.Count)
                        throw new ArgumentException("Count mismatch between tabOutputResults TabPages and Migrator TemplateStreams.  Counts should match after addition/removal above.  tabOutputResults. TabPages Count: " + tabOutputResults.TabPages.Count + "  Migration TemplateStream Count: " + azureTargetContext.TemplateGenerator.TemplateStreams.Count);

                    // Ensure Tabs are in same order as output streams
                    int streamIndex = 0;
                    foreach (string templateStreamKey in azureTargetContext.TemplateGenerator.TemplateStreams.Keys)
                    {
                        int rotationCounter = 0;

                        // This while loop is to bubble the tab to the end, as to rotate the tab sequence to ensure they match the order returned from the stream outputs
                        // The addition/removal of Streams may result in order of existing tabPages being "out of order" to the streams generated, so we may need to consider reordering
                        while (tabOutputResults.TabPages[streamIndex].Name != templateStreamKey)
                        {
                            TabPage currentTabpage = tabOutputResults.TabPages[streamIndex];
                            tabOutputResults.TabPages.Remove(currentTabpage);
                            tabOutputResults.TabPages.Add(currentTabpage);

                            rotationCounter++;

                            if (rotationCounter > azureTargetContext.TemplateGenerator.TemplateStreams.Count)
                                throw new ArgumentException("Rotated through all tabs, unabled to locate tab '" + templateStreamKey + "' while ensuring tab order/sequencing.");
                        }

                        streamIndex++;
                    }


                    lblLastOutputRefresh.Text = "Last Refresh Completed: " + DateTime.Now.ToString();
                    btnRefreshOutput.Enabled = false;

                    // post Telemetry Record to ASMtoARMToolAPI
                    if (AppSettingsProvider.AllowTelemetry)
                    {
                        StatusProvider.UpdateStatus("BUSY: saving telemetry information");
                        _telemetryProvider.PostTelemetryRecord(_AppSessionGuid, (AzureGenerator)azureTargetContext.TemplateGenerator);
                    }
                }
            }

            StatusProvider.UpdateStatus("Ready");
            return true;
        }


        #region Split Container Resize Events

        private void splitContainer1_Panel2_Resize(object sender, EventArgs e)
        {
            propertyPanel1.Width = splitContainer1.Panel2.Width - 10;
            propertyPanel1.Height = splitContainer1.Panel2.Height - 100;
            panel1.Top = splitContainer1.Panel2.Height - panel1.Height - 15;
            panel1.Width = splitContainer1.Panel2.Width;
        }

        private void splitContainer2_Panel1_Resize(object sender, EventArgs e)
        {
            if (splitContainer2.Panel1.Controls.Count == 1)
            {
                if (splitContainer2.Panel1.Height < 300)
                    splitContainer2.Panel1.Controls[0].Height = 300;
                else
                    splitContainer2.Panel1.Controls[0].Height = splitContainer2.Panel1.Height - 20;
            }
        }

        private void splitContainer2_Panel2_Resize(object sender, EventArgs e)
        {
            this.tabMigAzMonitoring.Width = splitContainer2.Panel2.Width - 5;
            this.tabMigAzMonitoring.Height = splitContainer2.Panel2.Height - 5;
            this.tabOutputResults.Width = splitContainer2.Panel2.Width - 5;
            this.tabOutputResults.Height = splitContainer2.Panel2.Height - 55;
        }

        private void splitContainer3_Panel1_Resize(object sender, EventArgs e)
        {
            foreach (Control control in splitContainer3.Panel1.Controls)
            {
                if (splitContainer3.Panel1.Height < 300)
                    control.Height = 300;
                else
                    control.Height = splitContainer3.Panel1.Height - 10;

                control.Width = splitContainer3.Panel1.Width - 10;
            }
        }

        private void splitContainer3_Panel2_Resize(object sender, EventArgs e)
        {
            if (splitContainer3.Panel2.Controls.Count == 1)
            {
                if (splitContainer3.Panel2.Height < 300)
                    splitContainer3.Panel2.Controls[0].Height = 300;
                else
                    splitContainer3.Panel2.Controls[0].Height = splitContainer3.Panel2.Height - 20;

                splitContainer3.Panel2.Controls[0].Width = splitContainer3.Panel2.Width - 20;
            }
        }

        private void splitContainer4_Panel2_Resize(object sender, EventArgs e)
        {
            foreach (Control control in splitContainer4.Panel2.Controls)
            {
                control.Width = splitContainer4.Panel2.Width;
                control.Height = splitContainer4.Panel2.Height;
            }
        }

        private void splitContainer4_Panel1_Resize(object sender, EventArgs e)
        {
            foreach (Control control in splitContainer4.Panel1.Controls)
            {
                control.Width = splitContainer4.Panel1.Width;
                control.Height = splitContainer4.Panel1.Height;
            }
        }

        #endregion

        #region Source and Target Context Selection Events + Methods

        private bool MigrationSourceSelectionControlVisible
        {
            get
            {
                foreach (Control control in splitContainer3.Panel1.Controls)
                {
                    if (control.GetType() == typeof(UserControls.MigAzMigrationSourceSelection))
                    {
                        return control.Visible;
                    }
                }

                return false;
            }
            set
            {
                foreach (Control control in splitContainer3.Panel1.Controls)
                {
                    if (control.GetType() == typeof(UserControls.MigAzMigrationSourceSelection))
                    {
                        control.Visible = value;
                        control.Enabled = value;
                    }
                }
            }
        }

        private void migAzMigrationSourceSelection1_AfterMigrationSourceSelected(UserControl migrationSourceUserControl)
        {
            if (migrationSourceUserControl.GetType() == typeof(MigrationAzureSourceContext))
            {
                MigrationAzureSourceContext azureControl = (MigrationAzureSourceContext)migrationSourceUserControl;

                //// This will move to be based on the source context (upon instantiation)
                azureControl.Bind(this._statusProvider, this._logProvider, this._appSettingsProvider.GetTargetSettings(), this.imageList1, app.Default.LoginPromptBehavior);

                switch (app.Default.AzureEnvironment)
                {
                    case "AzureCloud":
                        azureControl.AzureContext.AzureEnvironment = AzureEnvironment.AzureCloud;
                        break;
                    case "AzureGermanCloud":
                        azureControl.AzureContext.AzureEnvironment = AzureEnvironment.AzureGermanCloud;
                        break;
                    case "AzureChinaCloud":
                        azureControl.AzureContext.AzureEnvironment = AzureEnvironment.AzureChinaCloud;
                        break;
                    case "AzureUSGovernment":
                        azureControl.AzureContext.AzureEnvironment = AzureEnvironment.AzureUSGovernment;
                        break;
                }

                azureControl.AzureEnvironmentChanged += MigrationSourceControl_AzureEnvironmentChanged;
                azureControl.UserAuthenticated += MigrationSourceControl_UserAuthenticated;
                azureControl.BeforeAzureSubscriptionChange += MigrationSourceControl_BeforeAzureSubscriptionChange;
                azureControl.AfterAzureSubscriptionChange += MigrationSourceControl_AfterAzureSubscriptionChange;
                azureControl.BeforeUserSignOut += MigrationSourceControl_BeforeUserSignOut;
                azureControl.AfterUserSignOut += MigrationSourceControl_AfterUserSignOut;
                azureControl.AfterAzureTenantChange += MigrationSourceControl_AfterAzureTenantChange;
                azureControl.BeforeAzureTenantChange += MigrationSourceControl_BeforeAzureTenantChange;
                azureControl.AfterNodeChecked += MigrationSourceControl_AfterNodeChecked;
                azureControl.AfterNodeUnchecked += MigrationSourceControl_AfterNodeUnchecked;
                azureControl.AfterNodeChanged += MigrationSourceControl_AfterNodeChanged;
                azureControl.ClearContext += MigrationSourceControl_ClearContext;
                azureControl.AfterContextChanged += AzureControl_AfterContextChanged;
                azureControl.AzureContext.AzureRetriever.OnRestResult += AzureRetriever_OnRestResult;
            }
            else if (migrationSourceUserControl.GetType() == typeof(MigrationAzureStackSourceContext))
            {
                MigrationAzureStackSourceContext azureStackSourceContext = (MigrationAzureStackSourceContext)migrationSourceUserControl;
                azureStackSourceContext.Bind(_logProvider, _statusProvider, this._appSettingsProvider.GetTargetSettings(), this.imageList1, app.Default.LoginPromptBehavior);

                switch (app.Default.AzureEnvironment)
                {
                    case "AzureCloud":
                        azureStackSourceContext.AzureStackContext.AzureEnvironment = AzureEnvironment.AzureCloud;
                        break;
                    case "AzureGermanCloud":
                        azureStackSourceContext.AzureStackContext.AzureEnvironment = AzureEnvironment.AzureGermanCloud;
                        break;
                    case "AzureChinaCloud":
                        azureStackSourceContext.AzureStackContext.AzureEnvironment = AzureEnvironment.AzureChinaCloud;
                        break;
                    case "AzureUSGovernment":
                        azureStackSourceContext.AzureStackContext.AzureEnvironment = AzureEnvironment.AzureUSGovernment;
                        break;
                }

                azureStackSourceContext.AzureEnvironmentChanged += MigrationSourceControl_AzureEnvironmentChanged;
                azureStackSourceContext.UserAuthenticated += MigrationSourceControl_UserAuthenticated;
                azureStackSourceContext.BeforeAzureSubscriptionChange += MigrationSourceControl_BeforeAzureSubscriptionChange;
                azureStackSourceContext.AfterAzureSubscriptionChange += MigrationSourceControl_AfterAzureSubscriptionChange;
                azureStackSourceContext.BeforeUserSignOut += MigrationSourceControl_BeforeUserSignOut;
                azureStackSourceContext.AfterUserSignOut += MigrationSourceControl_AfterUserSignOut;
                azureStackSourceContext.AfterAzureTenantChange += MigrationSourceControl_AfterAzureTenantChange;
                azureStackSourceContext.BeforeAzureTenantChange += MigrationSourceControl_BeforeAzureTenantChange;
                azureStackSourceContext.AfterNodeChecked += MigrationSourceControl_AfterNodeChecked;
                azureStackSourceContext.AfterNodeUnchecked += MigrationSourceControl_AfterNodeUnchecked;
                azureStackSourceContext.AfterNodeChanged += MigrationSourceControl_AfterNodeChanged;
                azureStackSourceContext.ClearContext += MigrationSourceControl_ClearContext;
                azureStackSourceContext.AfterContextChanged += AzureControl_AfterContextChanged;
                azureStackSourceContext.AzureStackContext.AzureRetriever.OnRestResult += AzureRetriever_OnRestResult;
            }

            MigrationSourceSelectionControlVisible = false;
            splitContainer3.Panel1.Controls.Add(migrationSourceUserControl);
            migrationSourceUserControl.Top = 5;
            migrationSourceUserControl.Left = 5;
            splitContainer3_Panel1_Resize(this, null);

            migAzMigrationTargetSelection1.MigrationSource = migrationSourceUserControl;
        }


        private void AzureControl_AfterContextChanged(UserControl sender)
        {
            this.MigrationTargetSelectionControl.MigrationSource = sender;
        }

        private MigAzMigrationTargetSelection MigrationTargetSelectionControl
        {
            get
            {
                foreach (Control control in splitContainer4.Panel1.Controls)
                {
                    if (control.GetType() == typeof(MigAzMigrationTargetSelection))
                    {
                        return (MigAzMigrationTargetSelection)control;
                    }
                }

                return null;
            }
        }

        private bool MigrationTargetSelectionControlVisible
        {
            get
            {
                MigAzMigrationTargetSelection targetSelectionControl = MigrationTargetSelectionControl;

                if (targetSelectionControl != null)
                    return targetSelectionControl.Visible;

                return false;
            }
            set
            {
                MigAzMigrationTargetSelection targetSelectionControl = MigrationTargetSelectionControl;
                if (targetSelectionControl != null)
                {
                    targetSelectionControl.Visible = value;
                    targetSelectionControl.Enabled = value;
                }
            }
        }

        private async Task migAzMigrationTargetSelection1_AfterMigrationTargetSelected(UserControl migrationTargetUserControl)
        {
            if (migrationTargetUserControl.GetType() == typeof(MigrationAzureTargetContext))
            {
                MigrationAzureTargetContext azureTargetContext = (MigrationAzureTargetContext)migrationTargetUserControl;
                await azureTargetContext.Bind(this.LogProvider, this.StatusProvider);
                azureTargetContext.AfterContextChanged += AzureTargetContext_AfterContextChanged;
                azureTargetContext.AfterAzureSubscriptionChange += AzureTargetContext_AfterAzureSubscriptionChange;
                azureTargetContext.AzureContext.AzureRetriever.OnRestResult += AzureRetriever_OnRestResult;

                IMigrationSourceUserControl migrationSourceControl = this.MigrationSourceControl;
                if (migrationSourceControl != null && migrationSourceControl.GetType() == typeof(MigrationAzureSourceContext))
                {
                    MigrationAzureSourceContext azureSourceContext = (MigrationAzureSourceContext)migrationSourceControl;
                    azureTargetContext.ExistingContext = azureSourceContext.AzureContext;
                    await azureTargetContext.AzureContext.CopyContext(azureSourceContext.AzureContext);
                }

                targetTreeView1.TargetBlobStorageNamespace = azureTargetContext.ExistingContext.AzureServiceUrls.GetBlobEndpointUrl();
                targetTreeView1.TargetSubscription = azureTargetContext.ExistingContext.AzureSubscription;
                await RebindPropertyPanel();
            }

            MigrationTargetSelectionControlVisible = false;
            splitContainer4.Panel1.Controls.Add(migrationTargetUserControl);
            splitContainer4_Panel1_Resize(this, null);

            await this.targetTreeView1.RefreshExportArtifacts();
        }

        private async Task AzureTargetContext_AfterAzureSubscriptionChange(AzureContext sender)
        {
            targetTreeView1.TargetBlobStorageNamespace = sender.AzureServiceUrls.GetBlobEndpointUrl();
            targetTreeView1.TargetSubscription = sender.AzureSubscription;
            await this.targetTreeView1.RefreshExportArtifacts();
        }

        private async Task AzureTargetContext_AfterContextChanged(AzureLoginContextViewer sender)
        {
            targetTreeView1.TargetBlobStorageNamespace = sender.SelectedAzureContext.AzureServiceUrls.GetBlobEndpointUrl();
            targetTreeView1.TargetSubscription = sender.SelectedAzureContext.AzureSubscription;
            await this.targetTreeView1.RefreshExportArtifacts();
        }

        #endregion

        public bool AssertHasTargetErrors()
        {
            if (this.targetTreeView1.HasErrors)
            {
                tabMigAzMonitoring.SelectTab("tabMessages");
                MessageBox.Show("There are still one or more error(s) with the template generation.  Please resolve all errors before exporting.");
            }

            return this.targetTreeView1.HasErrors;
        }

        #region Target Tree View Events

        private async Task targetTreeView1_AfterNewTargetResourceAdded(TargetTreeView sender, TreeNode selectedNode)
        {
            // Refresh Alerts from new item
            await targetTreeView1_AfterExportArtifactRefresh(sender);
        }

        private async Task targetTreeView1_AfterExportArtifactRefresh(TargetTreeView sender)
        {
            dgvMigAzMessages.DataSource = sender.Alerts.Select(x => new { AlertType = x.AlertType, Message = x.Message, SourceObject = x.SourceObject }).ToList();
            dgvMigAzMessages.Columns["Message"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvMigAzMessages.Columns["SourceObject"].Visible = false;
            btnRefreshOutput.Enabled = true;
            btnExport.Enabled = true;

            await RebindPropertyPanel();
        }

        private async Task RebindPropertyPanel()
        {
            if (this.targetTreeView1 != null && this.targetTreeView1.SelectedNode != null)
                await BindPropertyPanel(this.targetTreeView1, this.targetTreeView1.SelectedNode);
        }

        private async Task BindPropertyPanel(TargetTreeView targetTreeView, TreeNode selectedNode)
        {
            await propertyPanel1.Bind((MigrationTarget)selectedNode.Tag);
        }

        private async Task targetTreeView1_AfterTargetSelected(TargetTreeView targetTreeView, TreeNode selectedNode)
        {
            try
            {
                if (this.LogProvider != null)
                    LogProvider.WriteLog("Control_AfterTargetSelected", "Start");

                _EventSourceNode = selectedNode;
                await BindPropertyPanel(targetTreeView, selectedNode);
                _EventSourceNode = null;

                if (this.LogProvider != null)
                    LogProvider.WriteLog("Control_AfterTargetSelected", "End");

                if (this.StatusProvider != null)
                    StatusProvider.UpdateStatus("Ready");
            }
            catch (Exception exc)
            {
                UnhandledExceptionDialog unhandledExceptionDialog = new UnhandledExceptionDialog(this.LogProvider, exc);
                unhandledExceptionDialog.ShowDialog();
            }
        }

        private async Task targetTreeView1_AfterSourceNodeRemoved(TargetTreeView sender, TreeNode removedNode)
        {
            if (this.LogProvider != null)
                LogProvider.WriteLog("targetTreeView1_AfterSourceNodeRemoved", "Start");

            if (removedNode.Tag != null)
            {
                MigrationTarget migrationTarget = (MigrationTarget)removedNode.Tag;
                await this.MigrationSourceControl.UncheckMigrationTarget(migrationTarget);
            }

            if (this.LogProvider != null)
                LogProvider.WriteLog("targetTreeView1_AfterSourceNodeRemoved", "End");
        }

        #endregion

    }
}
