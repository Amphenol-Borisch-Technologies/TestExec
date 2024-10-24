﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Outlook = Microsoft.Office.Interop.Outlook;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using ABT.Test.Exec.AppConfig;
using ABT.Test.Exec.Logging;
using ABT.Test.Exec.InstrumentDrivers.Interfaces;

// NOTE:  Recommend using Microsoft's Visual Studio Code to develop/debug TestPlan based closed source/proprietary projects:
//        - Visual Studio Code is a co$t free, open-source Integrated Development Environment entirely suitable for textual C# development, like TestPlan.
//          - That is, it's excellent for non-GUI (WinForms/WPF/WinUI) C# development.
//          - VS Code is free for both private & commercial use:
//            - https://code.visualstudio.com/docs/supporting/FAQ
//            - https://code.visualstudio.com/license
// NOTE:  Recommend using Microsoft's Visual Studio Community Edition to develop/debug open sourced TestExec:
//        - https://github.com/Amphenol-Borisch-Technologies/TestExec/blob/master/LICENSE.txt
//        - "An unlimited number of users within an organization can use Visual Studio Community for the following scenarios:
//           in a classroom learning environment, for academic research, or for contributing to open source projects."
//        - TestPlan based projects are very likely closed source/proprietary, which are disqualified from using VS Studio Community Edition.
//          - https://visualstudio.microsoft.com/vs/community/
//          - https://visualstudio.microsoft.com/license-terms/vs2022-ga-community/
// NOTE:  - VS Studio Community Edition is more preferable for GUI C# development than VS Code.
//          - If not developing GUI code (WinForms/WPF/WinUI), then VS Code is entirely sufficient & potentially preferable.
// TODO:  Eventually; refactor TestExec to Microsoft's C# Coding Conventions, https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions.
// NOTE:  For public methods, will deviate by using PascalCasing for parameters.  Will use recommended camelCasing for internal & private method parameters.
//        - Prefer named arguments for public methods be Capitalized/PascalCased, not uncapitalized/camelCased.
//        - Invoking public methods with named arguments is a superb, self-documenting coding technique, improved by PascalCasing.
// TODO:  Soon; add documentation per https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments.
// TODO:  Eventually; update to .Net 8.0 & C# 12.0 instead of .Net FrameWork 4.8 & C# 7.3.
//        - Currently cannot because all provider VISA.Net implementations are only compatible with the IVI Foundation's VISA.Net shared component versions 7.2 or lower.
//          - https://github.com/vnau/IviVisaNetSample.
//          - The IVI Foundation's VISA.NET shared components version 7.2 are .Net Framework versions that target .NET 2.0.
//          - The IVI Foundation's VISA.NET shared components version 7.3 are available in both .Net Framework versions that target .Net 4.5, and .NET versions that target .Net 6.0 or higher.
//          - https://www.ivifoundation.org/downloads/VISA/vpp436_2024-02-08.pdf
//        - National Instruments is considering .Net support for their NI-VISA; https://forums.ni.com/t5/Instrument-Control-GPIB-Serial/Will-NI-release-a-NET-Standard-version-of-the-NI-VISA-NET/td-p/4115465
//        - Keysight hasn't yet for their IO Libraries VISA implementation; https://community.keysight.com/forums/s/question/0D55a00009FHEIzCAP/are-the-keysight-io-libraries-compatible-with-net-or-net-core.
// TODO:  Eventually; consider updating to WinUI or WPF instead of WinForms if beneficial.
// NOTE:  With deep appreciation for https://learn.microsoft.com/en-us/docs/ & https://stackoverflow.com/!
// NOTE:  ABT's Zero Trust, Cloudflare Warp enterprise security solution inhibits GitHub's security, causing below error when sychronizing with
//        TestExec's GitHub repository at https://github.com/Amphenol-Borisch-Technologies/TestExec:
//             Opening repositories:
//             P:\Test\Engineers\repos\TestPlan
//             Opening repositories:
//             P:\Test\Engineers\repos\TestPlan
//             C:\Users\phils\source\repos\TestExec
//             Git failed with a fatal error.
//             Git failed with a fatal error.
//             unable to access 'https://github.com/Amphenol-Borisch-Technologies/TestLibrary/': schannel: CertGetCertificateChain trust error CERT_TRUST_IS_PARTIAL_CHAIN
//        - Temporarily disabling Zero Trust by "pausing" it resolves above error.
//        - https://stackoverflow.com/questions/27087483/how-to-resolve-git-pull-fatal-unable-to-access-https-github-com-empty
//        - FYI, synchronizing with TestPlan's repository doesn't error out, as it doesn't utilize a Git server.

namespace ABT.Test.Exec {
    /// <remarks>
    ///  <b>References:</b>
    /// <item>
    ///  <description><a href="https://github.com/Amphenol-Borisch-Technologies/TestExec">TestExec</a></description>
    ///  <description><a href="https://github.com/Amphenol-Borisch-Technologies/TestPlan">TestPlan</a></description>
    ///  </item>
    ///  </remarks>
    /// <summary>
    /// NOTE:  Test Developer is responsible for ensuring Measurements can be both safely &amp; correctly called in sequence defined in App.config:
    /// <para>
    ///        - That is, if Measurements execute sequentially as (M1, M2, M3, M4, M5), Test Developer is responsible for ensuring all equipment is
    ///          configured safely &amp; correctly between each Measurement step.
    ///          - If:
    ///            - M1 is unpowered Shorts &amp; Opens measurements.
    ///            - M2 is powered voltage measurements.
    ///            - M3 begins with unpowered operator cable connections/disconnections for In-System Programming.
    ///          - Then Test Developer must ensure necessary equipment state transitions are implemented so test operator isn't
    ///            plugging/unplugging a powered UUT in T03.
    /// </para>
    /// </summary>
    /// 
    /// <summary>
    /// NOTE:  Two types of TestPlan cancellations possible, each having two sub-types resulting in 4 altogether:
    /// <para>
    /// A) Spontaneous Operator Initiated Cancellations:
    ///      1)  Operator Proactive:
    ///          - Microsoft's recommended CancellationTokenSource technique, permitting Operator to proactively
    ///            cancel currently executing Measurement.
    ///          - Requires TestPlan implementation by the Test Developer, but is initiated by Operator, so categorized as such.
    ///          - Implementation necessary if the *currently* executing Measurement must be cancellable during execution by the Operator.
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/threading/canceling-threads-cooperatively
    ///      2)  Operator Reactive:
    ///          - TestExec's already implemented, always available &amp; default reactive "Cancel before next Test" technique,
    ///            which simply invokes CTS_Cancel.Cancel().
    ///          - CTS_Cancel.IsCancellationRequested is checked at the end of TestExec.MeasurementsRun()'s foreach loop.
    ///            - If true, TestExec.MeasurementsRun()'s foreach loop is broken, causing reactive cancellation.
    ///            prior to the next Measurement's execution.
    ///          - Note: This doesn't proactively cancel the *currently* executing Measurement, which runs to completion.
    /// B) PrePlanned Developer Programmed Cancellations:
    ///      3)  TestPlan/Test Developer initiated cancellations:
    ///          - Any TestPlan's Measurement can initiate a cancellation programmatically by simply throwing an OperationCanceledException:
    ///          - Permits immediate cancellation if specific condition(s) occur in a Measurement; perhaps to prevent UUT or equipment damage,
    ///            or simply because futher execution is pointless.
    ///          - Simply throw an OperationCanceledException if the specific condition(s) occcur.
    ///      4)  App.config's CancelNotPassed:
    ///          - App.config's TestMeasurement element has a Boolean "CancelNotPassed" field:
    ///          - If the current TestPlan.MeasurementRun() has CancelNotPassed=true and it's resulting EvaluateResultMeasurement() doesn't return TestEvents.PASS,
    ///            TestExec.MeasurementsRun() will break/exit, stopping further testing.
    ///		    - Do not pass Go, do not collect $200, go directly to TestExec.MeasurementsPostRun().
    ///
    /// NOTE:  The Operator Proactive &amp; TestPlan/Test Developer initiated cancellations both occur while the currently executing TestPlan.MeasurementRun() conpletes, via 
    ///        thrown OperationCanceledException.
    /// NOTE:  The Operator Reactive &amp; App.config's CancelNotPassed cancellations both occur after the currently executing TestPlan.MeasurementRun() completes, via checks
    ///        inside the TestExec.MeasurementsRun() loop.
    /// </para>
    /// </summary>
    public abstract partial class TestExec : Form {
        public static readonly String ConfigurationTestExec = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\Exec.config.xml";
        public const String MutexTestPlanName = "MutexTestPlan";
        public static Mutex MutexTestPlan = null;
        public const String NONE = "NONE";
        public readonly AppConfigLogger ConfigLogger = AppConfigLogger.Get();
        public readonly Dictionary<String, Object> Instruments = null;
        public static AppConfigUUT ConfigUUT = AppConfigUUT.Get();
        public AppConfigTest ConfigTest { get; private set; } = null; // Requires form; instantiated by ButtonSelectTests_Click method.
        private CancellationTokenSource CTS_Cancel;
        public static CancellationToken CT_Cancel;
        private CancellationTokenSource CTS_EmergencyStop;
        public static CancellationToken CT_EmergencyStop;
        public String MeasurementIDPresent { get; private set; } = String.Empty;
        public Measurement MeasurementPresent { get; private set; } = null;
        private readonly String _serialNumberRegEx = null;
        private readonly SerialNumberDialog _serialNumberDialog = null;
        private readonly RegistryKey _serialNumberRegistryKey = null;
        private const String _serialNumberMostRecent = "MostRecent";
        private const String NOT_APPLICABLE = "NotApplicable";
        private readonly System.Timers.Timer _statusTime = new System.Timers.Timer(10000);

        protected TestExec(Icon icon) {
            InitializeComponent();
            Icon = icon;
            // NOTE:  https://stackoverflow.com/questions/40933304/how-to-create-an-icon-for-visual-studio-with-just-mspaint-and-visual-studio
            if (String.Equals(ConfigUUT.SerialNumberRegExCustom, NOT_APPLICABLE)) _serialNumberRegEx = XElement.Load(ConfigurationTestExec).Element("SerialNumberRegExDefault").Value;
            else _serialNumberRegEx = ConfigUUT.SerialNumberRegExCustom;

            if (RegexInvalid(_serialNumberRegEx)) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Invalid Serial Number Regular Expression '{_serialNumberRegEx}':");
                sb.AppendLine($"   Check {ConfigurationTestExec}/SerialNumberRegExDefault or App.config/UUT_SerialNumberRegExCustom for valid Regular Expression syntax.");
                sb.AppendLine($"   Thank you & have a nice day {UserPrincipal.Current.DisplayName}!");
                throw new ArgumentException(sb.ToString());
            }

            _serialNumberRegistryKey = Registry.CurrentUser.CreateSubKey($"SOFTWARE\\{ConfigUUT.Customer}\\{ConfigUUT.Number}\\SerialNumber");
            ConfigUUT.SerialNumber = _serialNumberRegistryKey.GetValue(_serialNumberMostRecent, String.Empty).ToString();
            // NOTE:  Using Application Settings is generally advisable over the Windows Registry, but doing so permits all app users to write the App.config file.
            // - This means all users can also modify App.config's TestOperations, TestGroups & TestMeasurements, which is a no-no.
            _statusTime.Elapsed += StatusTimeUpdate;
            _statusTime.AutoReset = true;
            CTS_Cancel = new CancellationTokenSource();
            CT_Cancel = CTS_Cancel.Token;
            CTS_EmergencyStop = new CancellationTokenSource();
            CT_EmergencyStop = CTS_EmergencyStop.Token;

            if (!ConfigUUT.Simulate) {
                Instruments = AppConfig.Instruments.Get();
                if (ConfigLogger.SerialNumberDialogEnabled) _serialNumberDialog = new SerialNumberDialog(_serialNumberRegEx);
            }
        }

        #region Form Miscellaneous
        public static void ErrorMessage(String Error) {
            _ = MessageBox.Show(ActiveForm, $"Unexpected error:{Environment.NewLine}{Error}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static void ErrorMessage(Exception Ex) {
            if (!String.Equals(ConfigUUT.EMailTestEngineer, NOT_APPLICABLE)) {
                ErrorMessage($"'{Ex.Message}'{Environment.NewLine}{Environment.NewLine}Will attempt to E-Mail details To {ConfigUUT.EMailTestEngineer}.{Environment.NewLine}{Environment.NewLine}Please select your Microsoft 365 Outlook profile if dialog appears.");
                SendAdministratorMailMessage("Exception caught!", Ex);
            }
        }

        private void Form_Closing(Object sender, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) PreApplicationExit(); }

        private void Form_Shown(Object sender, EventArgs e) { ButtonSelect_Click(sender, e); }

        private void FormModeReset() {
            TextTest.Text = String.Empty;
            TextTest.BackColor = Color.White;
            TextTest.Refresh();
            rtfResults.Text = String.Empty;
            StatusTimeUpdate(null, null);
            StatusStatisticsUpdate(null, null);
            StatusCustomWrite(String.Empty, SystemColors.ControlLight);
            StatusModeUpdate(MODES.Resetting);
        }

        private void FormModeRun() {
            ButtonCancelReset(enabled: true);
            ButtonEmergencyStopReset(enabled: true);
            ButtonSelect.Enabled = false;
            ButtonRunReset(enabled: false);
            TSMI_File_Change.Enabled = false;
            TSMI_File_Exit.Enabled = false;
            TSMI_System_Diagnostics.Enabled = false;
            TSMI_System_BarcodeScannerDiscovery.Enabled = false;
            TSMI_UUT_Statistics.Enabled = false;
            StatusModeUpdate(MODES.Running);
        }

        private void FormModeSelect() {
            ButtonCancelReset(enabled: false);
            ButtonEmergencyStopReset(enabled: false);
            TSMI_File_Change.Enabled = true;
            TSMI_File_Exit.Enabled = true;
            ButtonSelect.Enabled = true;
            ButtonRunReset(enabled: ConfigTest != null);
            TSMI_System_Diagnostics.Enabled = true;
            TSMI_System_BarcodeScannerDiscovery.Enabled = true;
            TSMI_UUT_Statistics.Enabled = true;
            StatusModeUpdate(MODES.Selecting);
        }

        private String GetFolder(String FolderID) { return XElement.Load(ConfigurationTestExec).Element("Folders").Element(FolderID).Value; }

        private static Outlook.MailItem GetMailItem() {
            Outlook.Application outlook;
            try {
                if (Process.GetProcessesByName("OUTLOOK").Length > 0) {
                    outlook = Marshal.GetActiveObject("Outlook.Application") as Outlook.Application;
                } else {
                    outlook = new Outlook.Application();
                    Outlook.NameSpace nameSpace = outlook.GetNamespace("MAPI");
                    nameSpace.Logon("", "", true, true);
                    nameSpace = null;
                }
                return outlook.CreateItem(Outlook.OlItemType.olMailItem);
            } catch {
                _ = MessageBox.Show(ActiveForm, "Could not open Microsoft 365 Outlook.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.LogError("Could not open Microsoft 365 Outlook.");
                throw new NotImplementedException("Outlook not good...");
            }
        }

        public virtual void SystemReset() {
            if (ConfigUUT.Simulate) return;
            IPowerSuppliesOutputsOff();
            IRelaysOpenAll();
            IInstrumentsResetClear();
        }

        public virtual void IInstrumentsResetClear() {
            if (ConfigUUT.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in Instruments) if (kvp.Value is IInstruments ii) ii.ResetClear();
        }

        public virtual void IRelaysOpenAll() {
            if (ConfigUUT.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in Instruments) if (kvp.Value is IRelays ir) ir.OpenAll();
        }

        public virtual void IPowerSuppliesOutputsOff() {
            if (ConfigUUT.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in Instruments) if (kvp.Value is IPowerSupply ips) ips.OutputsOff();
        }

        private void InvalidPathError(String InvalidPath) { _ = MessageBox.Show(ActiveForm, $"Path {InvalidPath} invalid.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error); }

        public static String NotImplementedMessageEnum(Type enumType) { return $"Unimplemented Enum item; switch/case must support all items in enum '{String.Join(",", Enum.GetNames(enumType))}'."; }

        private void OpenApp(String CompanyID, String AppID, String Arguments = "") {
            String app = XElement.Load(ConfigurationTestExec).Element("Apps").Element(CompanyID).Element(AppID).Value;

            if (File.Exists(app)) {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = $"\"{app}\"",
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = "",
                    Arguments = $"\"{Arguments}\""
                    // Paths with embedded spaces require enclosing double-quotes (").
                    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                };
                Process.Start(psi);
            } else InvalidPathError(app);
        }

        private void OpenFolder(String FolderPath) {
            if (Directory.Exists(FolderPath)) {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "explorer.exe",
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = $"\"{FolderPath}\""
                    // Paths with embedded spaces require enclosing double-quotes (").
                    // Even then, simpler 'System.Diagnostics.Process.Start("explorer.exe", path);' invocation fails - thus using ProcessStartInfo class.
                    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                };
                Process.Start(psi);
            } else InvalidPathError(FolderPath);
        }

        private void PreApplicationExit() {
            SystemReset();
            if (ConfigLogger.SerialNumberDialogEnabled) _serialNumberDialog.Close();
            MutexTestPlan.ReleaseMutex();
            MutexTestPlan.Dispose();
        }

        public static Boolean RegexInvalid(String RegularExpression) {
            if (String.IsNullOrWhiteSpace(RegularExpression)) return true;
            try {
                Regex.Match("", RegularExpression);
            } catch (ArgumentException) {
                return true;
            }
            return false;
        }

        public static void SendAdministratorMailMessage(String Subject, Exception Ex) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"MachineName           : {Environment.MachineName}");
            sb.AppendLine($"UserPrincipal         : {UserPrincipal.Current.DisplayName}");
            sb.AppendLine($"Exception.ToString()  : {Ex}");
            SendAdministratorMailMessage(Subject, Body: sb.ToString());
        }

        public static void SendAdministratorMailMessage(String Subject, String Body) {
            try {
                Outlook.MailItem mailItem = GetMailItem();
                mailItem.Subject = Subject;
                mailItem.To = String.Equals(NOT_APPLICABLE, ConfigUUT.EMailTestEngineer) ? String.Empty : ConfigUUT.EMailTestEngineer;
                mailItem.Importance = Outlook.OlImportance.olImportanceHigh;
                mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
                mailItem.Body = Body;
                mailItem.Send();
            } catch {
                Logger.LogError(Subject);
            }
        }

        private void SendMailMessageWithAttachment(String subject) {
            try {
                Outlook.MailItem mailItem = GetMailItem();
                mailItem.Subject = subject;
                mailItem.To = String.Equals(NOT_APPLICABLE, ConfigUUT.EMailTestEngineer) ? String.Empty : ConfigUUT.EMailTestEngineer;
                mailItem.Importance = Outlook.OlImportance.olImportanceHigh;
                mailItem.Body =
                    $"Please detail desired Bug Report or Improvement Request:{Environment.NewLine}" +
                    $" - Please attach relevant files, and/or embed relevant screen-captures.{Environment.NewLine}" +
                    $" - Be specific!  Be verbose!  Unleash your inner author!  It's your time to shine!{Environment.NewLine}";
                String rtfTempFile = $"{Path.GetTempPath()}\\{ConfigUUT.Number}.rtf";
                rtfResults.SaveFile(rtfTempFile);
                _ = mailItem.Attachments.Add(rtfTempFile, Outlook.OlAttachmentType.olByValue, 1, $"{ConfigUUT.Number}.rtf");
                mailItem.Display();
            } catch {
                Logger.LogError(subject);
            }
        }
        #endregion Form Miscellaneous

        #region Form Command Buttons
        private void ButtonCancel_Clicked(Object sender, EventArgs e) {
            Debug.Assert(!CTS_Cancel.IsCancellationRequested);
            ButtonCancelReset(enabled: false);
            StatusModeUpdate(MODES.Cancelling);
            CTS_Cancel.Cancel();
        }

        private void ButtonCancelReset(Boolean enabled) {
            if (enabled) {
                ButtonCancel.UseVisualStyleBackColor = false;
                ButtonCancel.BackColor = Color.Yellow;
            } else {
                ButtonCancel.BackColor = SystemColors.Control;
                ButtonCancel.UseVisualStyleBackColor = true;
            }
            if (CTS_Cancel.IsCancellationRequested) {
                CTS_Cancel.Dispose();
                CTS_Cancel = new CancellationTokenSource();
                CT_Cancel = CTS_Cancel.Token;
            }
            ButtonCancel.Enabled = enabled;
        }

        private void ButtonEmergencyStop_Clicked(Object sender, EventArgs e) {
            Debug.Assert(!CTS_EmergencyStop.IsCancellationRequested);
            ButtonEmergencyStop.Enabled = false;
            ButtonCancelReset(enabled: false);
            StatusModeUpdate(MODES.Emergency_Stopping);
            CTS_EmergencyStop.Cancel();
        }

        private void ButtonEmergencyStopReset(Boolean enabled) {
            if (CT_EmergencyStop.IsCancellationRequested) {
                CTS_EmergencyStop = new CancellationTokenSource();
                CT_EmergencyStop = CTS_EmergencyStop.Token;
            }
            ButtonEmergencyStop.Enabled = enabled;
        }

        private void ButtonSelect_Click(Object sender, EventArgs e) {
            ConfigTest = AppConfigTest.Get();
            _statusTime.Start();  // NOTE:  Cannot update Status Bar until ConfigTest is instantiated.
            Text = $"{ConfigUUT.Number}, {ConfigUUT.Description}, {ConfigTest.TestElementID}";
            FormModeReset();
            FormModeSelect();
        }

        private async void ButtonRun_Clicked(Object sender, EventArgs e) {
            String serialNumber;
            if (ConfigLogger.SerialNumberDialogEnabled) {
                _serialNumberDialog.Set(ConfigUUT.SerialNumber);
                serialNumber = _serialNumberDialog.ShowDialog(this).Equals(DialogResult.OK) ? _serialNumberDialog.Get() : String.Empty;
                _serialNumberDialog.Hide();
            } else {
                serialNumber = Interaction.InputBox(Prompt: "Please enter ABT Serial Number", Title: "Enter ABT Serial Number", DefaultResponse: ConfigUUT.SerialNumber).Trim().ToUpper();
                serialNumber = Regex.IsMatch(serialNumber, _serialNumberRegEx) ? serialNumber : String.Empty;
            }
            if (String.Equals(serialNumber, String.Empty)) return;
            _serialNumberRegistryKey.SetValue(_serialNumberMostRecent, serialNumber);
            ConfigUUT.SerialNumber = serialNumber;

            FormModeReset();
            FormModeRun();
            MeasurementsPreRun();
            await MeasurementsRun();
            MeasurementsPostRun();
            FormModeSelect();
        }

        private void ButtonRunReset(Boolean enabled) {
            if (enabled) {
                ButtonRun.UseVisualStyleBackColor = false;
                ButtonRun.BackColor = Color.Green;
            } else {
                ButtonRun.BackColor = SystemColors.Control;
                ButtonRun.UseVisualStyleBackColor = true;
            }
            ButtonRun.Enabled = enabled;
        }
        #endregion Form Command Buttons

        #region Form Tool Strip Menu Items
        private void TSMI_File_Change_Click(Object sender, EventArgs e) {
            if (GetTestPlanFolder() != String.Empty) { }
            // TODO: "Restart" TestExec with new TestPlanFolder.
        }
        private String GetTestPlanFolder() {
            CommonOpenFileDialog cofd = new CommonOpenFileDialog {
                InitialDirectory = XElement.Load(ConfigurationTestExec).Element("Folders").Element("TestPlans").Value,
                IsFolderPicker = true,
                Title= "Select a TestPlan Folder"
            };
            return (cofd.ShowDialog() == CommonFileDialogResult.Ok) ? cofd.FileName : String.Empty;
        }
        private void TSMI_File_Exit_Click(Object sender, EventArgs e) {
            PreApplicationExit();
            System.Windows.Forms.Application.Exit();
        }
        private void TSMI_File_Save_Click(Object sender, EventArgs e) {
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save Test Results",
                Filter = "Rich Text Format|*.rtf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = $"{ConfigUUT.Number}_{ConfigTest.TestElementID}_{ConfigUUT.SerialNumber}",
                DefaultExt = "rtf",
                CreatePrompt = false,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK) rtfResults.SaveFile(saveFileDialog.FileName);
        }

        private void TSMI_Apps_KeysightBenchVue_Click(Object sender, EventArgs e) { OpenApp("Keysight", "BenchVue"); }
        private void TSMI_Apps_KeysightCommandExpert_Click(Object sender, EventArgs e) { OpenApp("Keysight", "CommandExpert"); }
        private void TSMI_Apps_KeysightConnectionExpert_Click(Object sender, EventArgs e) { OpenApp("Keysight", "ConnectionExpert"); }

        private void TSMI_Apps_MicrosoftSQL_ServerManagementStudio_Click(Object sender, EventArgs e) { OpenApp("Microsoft", "SQLServerManagementStudio"); }
        private void TSMI_Apps_MicrosoftVisualStudio_Click(Object sender, EventArgs e) { OpenApp("Microsoft", "VisualStudio"); }
        private void TSMI_Apps_MicrosoftVisualStudioCode_Click(Object sender, EventArgs e) { OpenApp("Microsoft", "VisualStudioCode"); }
        private void TSMI_Apps_MicrosoftXML_Notepad_Click(Object sender, EventArgs e) { OpenApp("Microsoft", "XMLNotepad"); }

        private void TSMI_Feedback_ComplimentsPraiseεPlaudits_Click(Object sender, EventArgs e) { _ = MessageBox.Show($"You are a kind person, {UserPrincipal.Current.DisplayName}.", $"Thank you!", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void TSMI_Feedback_ComplimentsMoney_Click(Object sender, EventArgs e) { _ = MessageBox.Show($"Prefer ₿itcoin donations!", $"₿₿₿", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void TSMI_Feedback_CritiqueBugReport_Click(Object sender, EventArgs e) { SendMailMessageWithAttachment($"Bug Report from {UserPrincipal.Current.DisplayName} for {ConfigUUT.Number}, {ConfigUUT.Description}."); }
        private void TSMI_Feedback_CritiqueImprovementRequest_Click(Object sender, EventArgs e) { SendMailMessageWithAttachment($"Improvement Request from {UserPrincipal.Current.DisplayName} for {ConfigUUT.Number}, {ConfigUUT.Description}."); }

        private async void TSMI_System_BarcodeScannerDiscovery_Click(Object sender, EventArgs e) {
            DialogResult dr = MessageBox.Show($"About to clear/erase result box.{Environment.NewLine}{Environment.NewLine}" +
                $"Please Cancel & File/Save results if needed, then re-run Discovery.", "Alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (dr == DialogResult.Cancel) return;
            rtfResults.Clear();
            DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(BarcodeScanner.GetDeviceSelector(PosConnectionTypes.Local));
            StringBuilder sb = new StringBuilder($"Discovering Microsoft supported, corded Barcode Scanner(s):{Environment.NewLine}");
            sb.AppendLine($"  - See https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/pos-device-support.");
            sb.AppendLine($"  - Note that only corded Barcode Scanners are discovered; cordless BlueTooth & Wireless scanners are ignored.");
            sb.AppendLine($"  - Modify ConfigurationTestExec to use a discovered Barcode Scanner.");
            sb.AppendLine($"  - Scanners must be programmed into USB-HID mode to function properly:");
            sb.AppendLine(@"    - See: file:///P:/Test/Engineers/Equipment%20Manuals/TestExec/Honeywell%20Voyager%201200g/Honeywell%20Voyager%201200G%20User's%20Guide%20ReadMe.pdf");
            sb.AppendLine($"    - Or:  https://prod-edam.honeywell.com/content/dam/honeywell-edam/sps/ppr/en-us/public/products/barcode-scanners/general-purpose-handheld/1200g/documents/sps-ppr-vg1200-ug.pdf{Environment.NewLine}");
            foreach (DeviceInformation di in dic) {
                sb.AppendLine($"Name: '{di.Name}'.");
                sb.AppendLine($"Kind: '{di.Kind}'.");
                sb.AppendLine($"ID  : '{di.Id}'.{Environment.NewLine}");
            }
            rtfResults.Text = sb.ToString();
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save Discovered Corded Barcode Scanner(s)",
                Filter = "Rich Text Format|*.rtf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = $"Discovered Corded Barcode Scanner(s)",
                DefaultExt = "rtf",
                CreatePrompt = false,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK) rtfResults.SaveFile(saveFileDialog.FileName, RichTextBoxStreamType.RichText);
        }
        private void TSMI_System_DiagnosticsInstruments_Click(Object sender, EventArgs e) {
            UseWaitCursor = true;
            Boolean failed = false;
            IInstruments instrument;

            foreach (KeyValuePair<String, Object> kvp in Instruments) {
                instrument = (IInstruments)kvp.Value;
                if (instrument.Diagnostics() is DIAGNOSTICS_RESULTS.FAIL) instrument.ResetClear(); // Try, try again...
                    if (instrument.Diagnostics() is DIAGNOSTICS_RESULTS.FAIL) {
                    failed = true;
                    _ = MessageBox.Show(ActiveForm, $"Instrument {kvp.Key} failed:{Environment.NewLine}" + 
                        $"Type:      {instrument.InstrumentType}{Environment.NewLine}" +
                        $"Detail:    {instrument.Detail}{Environment.NewLine}" +
                        $"Address:   {instrument.Address}"
                        , "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if (!failed) _ = MessageBox.Show(ActiveForm, "Instrument Self-Tests all passed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UseWaitCursor = false;
        }
        private void TSMI_System_ManualsBarcodeScanner_Click(Object sender, EventArgs e) { OpenFolder(GetFolder("BarcodeScanner")); }
        private void TSMI_System_ManualsInstruments_Click(Object sender, EventArgs e) { OpenFolder(GetFolder("Instruments")); }
        private void TSMI_System_TestExecConfigXML_Click(Object sender, EventArgs e) {

        }
        private void TSMI_System_About_Click(Object sender, EventArgs e) {
            Form about = new MessageBoxMonoSpaced (
                Title: "About TestExec",
                Text: $"{Assembly.GetExecutingAssembly().GetName().Name}, {Assembly.GetExecutingAssembly().GetName().Version}, {Logger.BuildDate(Assembly.GetExecutingAssembly().GetName().Version)}.{Environment.NewLine}{Environment.NewLine}© 2022, Amphenol Borisch Technologies.",
                Link: "https://github.com/Amphenol-Borisch-Technologies/TestExec"
            );
            _ = about.ShowDialog();
        }

        private void TSMI_UUT_AppConfig_Click(Object sender, EventArgs e) {
            StringBuilder sb = new StringBuilder();
            String EA = Assembly.GetEntryAssembly().GetName().Name;
            sb.AppendLine($"Please backport any permanently desired '{EA}.exe.config' changes to source repository's 'App.config'.{Environment.NewLine}");
            sb.AppendLine($"Also please undo any temporary undesired '{EA}.exe.config' changes.");
            DialogResult dr = MessageBox.Show(sb.ToString(), $"Warning.", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (dr == DialogResult.OK) OpenApp("Microsoft", "XMLNotepad", $"{EA}.exe.config");
        }
        private void TSMI_UUT_eDocs_Click(Object sender, EventArgs e) { OpenFolder(ConfigUUT.DocumentationFolder); }
        private void TSMI_UUT_ManualsInstruments_Click(Object sender, EventArgs e) { OpenFolder(ConfigUUT.ManualsFolder); }
        private void TSMI_UUT_StatisticsDisplay_Click(Object sender, EventArgs e) {
            Form statistics = new MessageBoxMonoSpaced(
                Title: $"{ConfigUUT.Number}, {ConfigTest.TestElementID}, {ConfigTest.StatusTime()}",
                Text: ConfigTest.StatisticsDisplay(),
                Link: String.Empty
            );
            _ = statistics.ShowDialog();


        }
        private void TSMI_UUT_StatisticsReset_Click(Object sender, EventArgs e) {
            ConfigTest.Statistics = new Statistics();
            StatusTimeUpdate(null, null);
            StatusStatisticsUpdate(null, null);
        }
        private void TSMI_UUT_TestData_P_DriveTDR_Folder_Click(Object sender, EventArgs e) { OpenFolder(ConfigLogger.FilePath); }
        private void TSMI_UUT_TestDataSQL_ReportingAndQuerying_Click(Object sender, EventArgs e) { }
        private void TSMI_UUT_About_Click(Object sender, EventArgs e) {
            Form about = new MessageBoxMonoSpaced (
                Title: "About TestPlan",
                Text: $"{Assembly.GetEntryAssembly().GetName().Name}, {Assembly.GetEntryAssembly().GetName().Version}, {Logger.BuildDate(Assembly.GetEntryAssembly().GetName().Version)}.{Environment.NewLine}{Environment.NewLine}© 2022, Amphenol Borisch Technologies.",
                Link: "https://github.com/Amphenol-Borisch-Technologies/TestPlan"
            );
            _ = about.ShowDialog();
        }
        #endregion Form Tool Strip Menu Items

        #region Measurements
        private void MeasurementsPreRun() {
            Logger.Start(this, ref rtfResults);
            foreach (KeyValuePair<String, Measurement> kvp in ConfigTest.Measurements) {
                if (String.Equals(kvp.Value.ClassName, nameof(MeasurementNumeric))) kvp.Value.Value = Double.NaN.ToString();
                else kvp.Value.Value = String.Empty;
                kvp.Value.TestEvent = TestEvents.UNSET;
                kvp.Value.Message.Clear();
            }
            ConfigUUT.TestEvent = TestEvents.UNSET;
            SystemReset();
        }

        private async Task MeasurementsRun() {
            foreach (String groupID in ConfigTest.GroupIDsSequence) {
                foreach (String measurementID in ConfigTest.GroupIDsToMeasurementIDs[groupID]) {
                    MeasurementIDPresent = measurementID;
                    MeasurementPresent = ConfigTest.Measurements[MeasurementIDPresent];
                    try {
                        StatusStatisticsUpdate(null, null);
                        ConfigTest.Measurements[measurementID].Value = await Task.Run(() => MeasurementRun(measurementID));
                        ConfigTest.Measurements[measurementID].TestEvent = MeasurementEvaluate(ConfigTest.Measurements[measurementID]);
                        if (CT_EmergencyStop.IsCancellationRequested || CT_Cancel.IsCancellationRequested) {
                            SystemReset();
                            return;
                        }
                    } catch (Exception e) {
                        SystemReset();
                        if (e.ToString().Contains(typeof(OperationCanceledException).Name)) {
                            ConfigTest.Measurements[measurementID].TestEvent = TestEvents.CANCEL;  // NOTE:  May be altered to TestEvents.EMERGENCY_STOP in finally block.
                            while (!(e is OperationCanceledException) && (e.InnerException != null)) e = e.InnerException; // No fluff, just stuff.
                            ConfigTest.Measurements[measurementID].Message.Append($"{Environment.NewLine}{typeof(OperationCanceledException).Name}:{Environment.NewLine}{e.Message}");
                        }
                        if (!CT_EmergencyStop.IsCancellationRequested && !CT_Cancel.IsCancellationRequested) {
                            ConfigTest.Measurements[measurementID].TestEvent = TestEvents.ERROR;
                            ConfigTest.Measurements[measurementID].Message.Append($"{Environment.NewLine}{e}");
                            ErrorMessage(e);
                        }
                        return;
                    } finally {
                        // NOTE:  Normally executes, regardless if catchable Exception occurs or returned out of try/catch blocks.
                        // Exceptional exceptions are exempted; https://stackoverflow.com/questions/345091/will-code-in-a-finally-statement-fire-if-i-return-a-value-in-a-try-block.
                        if      (CT_EmergencyStop.IsCancellationRequested) ConfigTest.Measurements[measurementID].TestEvent = TestEvents.EMERGENCY_STOP;
                        else if (CT_Cancel.IsCancellationRequested) ConfigTest.Measurements[measurementID].TestEvent = TestEvents.CANCEL;
                        // NOTE:  Both CT_Cancel.IsCancellationRequested & CT_EmergencyStop.IsCancellationRequested could be true; prioritize CT_EmergencyStop.
                        Logger.LogTest(ConfigTest.IsOperation, ConfigTest.Measurements[measurementID], ref rtfResults);
                    }
                    if (MeasurementCancelNotPassed(measurementID)) return;
                }
                if (MeasurementsCancelNotPassed(groupID)) return;
            }
        }

        protected abstract Task<String> MeasurementRun(String measurementID);

        private void MeasurementsPostRun() {
            SystemReset();
            ConfigUUT.TestEvent = MeasurementsEvaluate(ConfigTest.Measurements);
            TextTest.Text = ConfigUUT.TestEvent;
            TextTest.BackColor = TestEvents.GetColor(ConfigUUT.TestEvent);
            ConfigTest.Statistics.Update(ConfigUUT.TestEvent);
            StatusStatisticsUpdate(null, null);
            Logger.Stop(this, ref rtfResults);
        }

        private Boolean MeasurementCancelNotPassed(String measurementID) { return !String.Equals(ConfigTest.Measurements[measurementID].TestEvent, TestEvents.PASS) && ConfigTest.Measurements[measurementID].CancelNotPassed; }

        private Boolean MeasurementsCancelNotPassed(String groupID) { return !String.Equals(MeasurementsEvaluate(MeasurementsGet(groupID)), TestEvents.PASS) && ConfigTest.Groups[groupID].CancelNotPassed; }

        private Dictionary<String, Measurement> MeasurementsGet(String groupID) {
            Dictionary<String, Measurement> measurements = new Dictionary<String, Measurement>();
            foreach (String measurementID in ConfigTest.GroupIDsToMeasurementIDs[groupID]) measurements.Add(measurementID, ConfigTest.Measurements[measurementID]);
            return measurements;
        }

        private String MeasurementEvaluate(Measurement measurement) {
            switch (measurement.ClassObject) {
                case MeasurementCustom _:
                    return measurement.TestEvent; // Test Developer must set TestEvent in TestPlan, else it remains MeasurementsPreRun()'s initial TestEvents.UNSET.
                case MeasurementNumeric _:
                    if (!Double.TryParse(measurement.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out Double dMeasurement)) throw new InvalidOperationException($"TestMeasurement ID '{measurement.ID}' Measurement '{measurement.Value}' ≠ System.Double.");
                    MeasurementNumeric mn = (MeasurementNumeric)measurement.ClassObject;
                    if ((mn.Low <= dMeasurement) && (dMeasurement <= mn.High)) return TestEvents.PASS;
                    return TestEvents.FAIL;
                case MeasurementProcess _:
                    MeasurementProcess mp = (MeasurementProcess)measurement.ClassObject;
                    if (String.Equals(mp.ProcessExpected, measurement.Value, StringComparison.Ordinal)) return TestEvents.PASS;
                    return TestEvents.FAIL;
                case MeasurementTextual _:
                    MeasurementTextual mt = (MeasurementTextual)measurement.ClassObject;
                    if (String.Equals(mt.Text, measurement.Value, StringComparison.Ordinal)) return TestEvents.PASS;
                    return TestEvents.FAIL;
                default:
                    throw new NotImplementedException($"TestMeasurement ID '{measurement.ID}' with ClassName '{measurement.ClassObject.GetType().Name}' not implemented.");
            }
        }

        private String MeasurementsEvaluate(Dictionary<String, Measurement> measurements) {
            if (MeasurementEventsCount(measurements, TestEvents.PASS) == measurements.Count) return TestEvents.PASS;
            // 1st priority evaluation (or could also be last, but we're irrationally optimistic.)
            // All measurement TestEvents are PASS, so overall TestEvent is PASS.
            if (MeasurementEventsCount(measurements, TestEvents.EMERGENCY_STOP) != 0) return TestEvents.EMERGENCY_STOP;
            // 2nd priority evaluation:
            // - If any measurement TestEvent is EMERGENCY_STOP, overall TestEvent is EMERGENCY_STOP.
            if (MeasurementEventsCount(measurements, TestEvents.ERROR) != 0) return TestEvents.ERROR;
            // 3rd priority evaluation:
            // - If any measurement TestEvent is ERROR, and none were EMERGENCY_STOP, overall TestEvent is ERROR.
            if (MeasurementEventsCount(measurements, TestEvents.CANCEL) != 0) return TestEvents.CANCEL;
            // rth priority evaluation:
            // - If any measurement TestEvent is CANCEL, and none were EMERGENCY_STOP or ERROR, overall TestEvent is CANCEL.
            if (MeasurementEventsCount(measurements, TestEvents.UNSET) != 0) return TestEvents.CANCEL;
            // 5th priority evaluation:
            // - If any measurement TestEvent is UNSET, and none were EMERGENCY_STOP, ERROR or CANCEL, then Measurement(s) didn't complete.
            // - Likely occurred because a Measurement failed that had its App.config TestMeasurement CancelOnFail flag set to true.
            if (MeasurementEventsCount(measurements, TestEvents.FAIL) != 0) return TestEvents.FAIL;
            // 6th priority evaluation:
            // - If any measurement TestEvent is FAIL, and none were EMERGENCY_STOP, ERROR, CANCEL or UNSET, TestEvent is FAIL.

            String validEvents = String.Empty, invalidTests = String.Empty;
            foreach (FieldInfo fi in typeof(TestEvents).GetFields()) validEvents += ((String)fi.GetValue(null), String.Empty);
            foreach (KeyValuePair<String, Measurement> kvp in measurements) if (!validEvents.Contains(kvp.Value.TestEvent)) invalidTests += $"ID: '{kvp.Key}' TestEvent: '{kvp.Value.TestEvent}'.{Environment.NewLine}";
            Logger.LogError($"{Environment.NewLine}Invalid Measurement ID(s) to TestEvents(s):{Environment.NewLine}{invalidTests}");
            return TestEvents.ERROR;
            // Above handles class TestEvents changing (adding/deleting/renaming TestEvents) without accomodating MeasurementsEvaluate() changes. 
        }

        private Int32 MeasurementEventsCount(Dictionary<String, Measurement> measurements, String testEvent) { return (from measurement in measurements where String.Equals(measurement.Value.TestEvent, testEvent) select measurement).Count(); }
        #endregion Measurements

        #region Introspective methods.
        public Boolean AreMethodNamesPriorNext(String prior, String next) { return String.Equals(GetID_MeasurementPrior(), prior) && String.Equals(GetID_MeasurementNext(), next); }

        public Boolean IsGroup(String GroupID) { return String.Equals(ConfigTest.Measurements[MeasurementIDPresent].GroupID, GroupID); }

        public Boolean IsGroup(String GroupID, String Description, String MeasurementIDs, Boolean Selectable, Boolean CancelNotPassed) {
            return
                String.Equals(ConfigTest.Measurements[MeasurementIDPresent].GroupID, GroupID) &&
                String.Equals(ConfigTest.Groups[GetID_Group()].Description, Description) &&
                String.Equals(ConfigTest.Groups[GetID_Group()].TestMeasurementIDs, MeasurementIDs) &&
                ConfigTest.Groups[GetID_Group()].Selectable == Selectable &&
                ConfigTest.Groups[GetID_Group()].CancelNotPassed == CancelNotPassed;
        }

        public Boolean IsMeasurement(String Description, String IDPrior, String IDNext, String ClassName, Boolean CancelNotPassed, String Arguments) {
            return
                IsMeasurement(Description, ClassName, CancelNotPassed, Arguments) &&
                String.Equals(GetID_MeasurementPrior(), IDPrior) &&
                String.Equals(GetID_MeasurementNext(), IDNext);
        }

        public Boolean IsMeasurement(String Description, String ClassName, Boolean CancelNotPassed, String Arguments) {
            return
                String.Equals(MeasurementPresent.Description, Description) &&
                String.Equals(MeasurementPresent.ClassObject.GetType().Name, ClassName) &&
                MeasurementPresent.CancelNotPassed == CancelNotPassed &&
                String.Equals((String)MeasurementPresent.ClassObject.GetType().GetMethod("ArgumentsGet").Invoke(MeasurementPresent.ClassObject, null), Arguments);
        }

        public Boolean IsOperation(String OperationID) { return String.Equals(ConfigTest.TestElementID, OperationID); }

        public Boolean IsOperation(String OperationID, String Description, String Revision, String GroupsIDs) {
            return
            String.Equals(ConfigTest.TestElementID, OperationID) &&
            String.Equals(ConfigTest.TestElementDescription, Description) &&
            String.Equals(ConfigTest.TestElementRevision, Revision) &&
            String.Equals(String.Join(MeasurementAbstract.SA.ToString(), ConfigTest.GroupIDsSequence.ToArray()), GroupsIDs);
        }

        private String GetID_Group() { return ConfigTest.Measurements[MeasurementIDPresent].GroupID; }

        private String GetID_MeasurementNext() {
            if (GetIDs_MeasurementSequence() == ConfigTest.TestMeasurementIDsSequence.Count - 1) return NONE;
            return ConfigTest.TestMeasurementIDsSequence[GetIDs_MeasurementSequence() + 1];
        }

        private String GetID_MeasurementPrior() {
            if (GetIDs_MeasurementSequence() == 0) return NONE;
            return ConfigTest.TestMeasurementIDsSequence[GetIDs_MeasurementSequence() - 1];
        }

        private Int32 GetIDs_MeasurementSequence() { return ConfigTest.TestMeasurementIDsSequence.FindIndex(x => x.Equals(MeasurementIDPresent)); }

        public String GetMeasurementNumericArguments(String measurementID) {
            MeasurementNumeric mn = (MeasurementNumeric)Measurement.Get(measurementID).ClassObject;
            return (String)mn.GetType().GetMethod("ArgumentsGet").Invoke(mn, null);
        }
        #endregion Introspective methods.

        #region Logging methods.
        public void LogCaller([CallerFilePath] String callerFilePath = "", [CallerMemberName] String callerMemberName = "", [CallerLineNumber] Int32 callerLineNumber = 0) {
            MessageAppendLine("Caller File", $"'{callerFilePath}'");
            MessageAppendLine("Caller Member", $"'{callerMemberName}'");
            MessageAppendLine("Caller Line #", $"'{callerLineNumber}'");
        }

        public void MessageAppend(String Message) { MeasurementPresent.Message.Append(Message); }

        public void MessageAppendLine(String Message) { MeasurementPresent.Message.AppendLine(Message); }

        public void MessageAppendLine(String Label, String Message) { MeasurementPresent.Message.AppendLine($"{Label}".PadLeft(Logger.SPACES_21.Length) + $" : {Message}"); }

        public void MessagesAppendLines(List<(String, String)> Messages) { foreach ((String Label, String Message) in Messages) MessageAppendLine(Label, Message); }
        #endregion Logging methods.

        #region Status Strip methods.
        private void StatusTimeUpdate(Object source, ElapsedEventArgs e) { Invoke((Action)(() => StatusTimeLabel.Text = ConfigTest.StatusTime())); }

        private void StatusStatisticsUpdate(Object source, ElapsedEventArgs e) { Invoke((Action)(() => StatusStatisticsLabel.Text = ConfigTest.StatisticsStatus())); }

        private enum MODES { Resetting, Selecting, Running, Cancelling, Emergency_Stopping };

        private void StatusModeUpdate(MODES mode) {
            Dictionary<MODES, Color> ModeColors = new Dictionary<MODES, Color>() {
                { MODES.Resetting, SystemColors.ControlLight }, // Invisible ink!
                { MODES.Selecting, Color.Black },
                { MODES.Running, Color.Green },
                { MODES.Cancelling, Color.Yellow },
                { MODES.Emergency_Stopping, Color.Firebrick }
            };

            Invoke((Action)(() => StatusModeLabel.Text = Enum.GetName(typeof(MODES), mode)));
            Invoke((Action)(() => StatusModeLabel.ForeColor = ModeColors[mode]));
        }

        public void StatusCustomWrite(String Message, Color ForeColor) {
            Invoke((Action)(() => StatusCustomLabel.Text = Message));
            Invoke((Action)(() => StatusCustomLabel.ForeColor = ForeColor));
        }
        #endregion Status Strip methods.
    }
}

