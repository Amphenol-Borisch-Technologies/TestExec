﻿using ABT.Test.TestExec.Logging;
using ABT.Test.TestLib;
using ABT.Test.TestLib.Configuration;
using ABT.Test.TestLib.InstrumentDrivers.Interfaces;
using ABT.Test.TestLib.Miscellaneous;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Windows.Devices.Enumeration;
using Windows.Devices.PointOfService;
using static ABT.Test.TestLib.Data;
using Outlook = Microsoft.Office.Interop.Outlook;
// TODO:  Eventually; evaluate Keysight OpenTAP as potential option in addition to TestExec/TestLib/TestPlan.  https://opentap.io/.
// - Briefly evaluated previously; time for reevaluation.
// TODO:  Eventually; GitHub automated workflows; CI/CD including automated deployment to subscribed TestExec PCs (assuming its possible).
// NOTE:  Recommend using Microsoft's Visual Studio Code to develop/debug Tests based closed source/proprietary projects:
//        - Visual Studio Code is a co$t free, open-source Integrated Development Environment entirely suitable for textual C# development, like Tests.
//          - That is, it's excellent for non-GUI (WinForms/WPF/WinUI) C# development.
//          - VS Code is free for both private & commercial use:
//            - https://code.visualstudio.com/docs/supporting/FAQ
//            - https://code.visualstudio.com/license
// NOTE:  Recommend using Microsoft's Visual Studio Community Edition to develop/debug open sourced TestExec:
//        - https://github.com/Amphenol-Borisch-Technologies/TestExec/blob/master/LICENSE.txt
//        - "An unlimited number of users within an organization can use Visual Studio Community for the following scenarios:
//           in a classroom learning environment, for academic research, or for contributing to open source projects."
//        - Tests based projects are closed source/proprietary, which are disqualified from using VS Studio Community Edition.
//          - https://visualstudio.microsoft.com/vs/community/
//          - https://visualstudio.microsoft.com/license-terms/vs2022-ga-community/
// NOTE:  - VS Studio Community Edition is more preferable for GUI C# development than VS Code.
//          - If not developing GUI code (WinForms/WPF/WinUI), then VS Code is entirely sufficient & potentially preferable.
// TODO:  Eventually; refactor TestExec to Microsoft's C# Coding Conventions, https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions.
// NOTE:  For public methods, will deviate by using PascalCasing for parameters.  Will use recommended camelCasing for internal & private method parameters.
//        - Prefer named arguments for public methods be Capitalized/PascalCased, not uncapitalized/camelCased.
//        - Invoking public methods with named arguments is a superb, self-documenting coding technique, improved by PascalCasing.
// TODO:  Eventually; add documentation per https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments.
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
//             P:\Test\Engineers\repos\ABT\TestExec
//             Git failed with a fatal error.
//             unable to access 'https://github.com/Amphenol-Borisch-Technologies/TestLibrary/': schannel: CertGetCertificateChain trust error CERT_TRUST_IS_PARTIAL_CHAIN
//        - Temporarily disabling Zero Trust by "pausing" it resolves above error.
//        - https://stackoverflow.com/questions/27087483/how-to-resolve-git-pull-fatal-unable-to-access-https-github-com-empty
//        - FYI, synchronizing with Tests repository doesn't error out, as it doesn't utilize a Git server.

namespace ABT.Test.TestExec {
    /// <remarks>
    ///  <b>References:</b>
    /// <item>
    ///  <description><a href="https://github.com/Amphenol-Borisch-Technologies/TestExec">TestExec</a></description>
    ///  <description><a href="https://github.com/Amphenol-Borisch-Technologies/TestLib">TestLib</a></description>
    ///  <description><a href="https://github.com/Amphenol-Borisch-Technologies/TestPlan">TestPlan</a></description>
    ///  </item>
    ///  </remarks>
    /// <summary>
    /// NOTE:  Test Developer is responsible for ensuring Methods can be both safely &amp; correctly called in sequence defined in TestPlanDefinition.xml:
    /// <para>
    ///        - That is, if Methods execute sequentially as (M1, M2, M3, M4, M5), Test Developer is responsible for ensuring all equipment is
    ///          configured safely &amp; correctly between each Method.
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
    /// NOTE:  Two types of TestExec cancellations possible, each having two sub-types resulting in 4 altogether:
    /// <para>
    /// A) Spontaneous Operator Initiated Cancellations:
    ///      1)  Operator Proactive:
    ///          - Microsoft's recommended CancellationTokenSource technique, permitting Operator to proactively
    ///            cancel currently executing Method.
    ///          - Requires Test project implementation by the Test Developer, but is initiated by Operator, so categorized as such.
    ///          - Implementation necessary if the *currently* executing Method must be cancellable during execution by the Operator.
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/task-cancellation
    ///          - https://learn.microsoft.com/en-us/dotnet/standard/threading/canceling-threads-cooperatively
    ///      2)  Operator Reactive:
    ///          - TestExec's already implemented, always available &amp; default reactive "Cancel before next Test" technique,
    ///            which simply invokes CTS_Cancel.Cancel().
    ///          - CTS_Cancel.IsCancellationRequested is checked at the end of TestExec.MethodsRun()'s foreach loop.
    ///            - If true, TestExec.MethodsRun()'s foreach loop is broken, causing reactive cancellation.
    ///            prior to the next Method's execution.
    ///          - Note: This doesn't proactively cancel the *currently* executing Method, which runs to completion.
    /// B) PrePlanned Developer Programmed Cancellations:
    ///      3)  TestExec Developer initiated cancellations:
    ///          - Any Tests project's Method can initiate a cancellation programmatically by simply throwing an OperationCanceledException:
    ///          - Permits immediate cancellation if specific condition(s) occur in a Method; perhaps to prevent UUT or equipment damage,
    ///            or simply because futher execution is pointless.
    ///          - Simply throw an OperationCanceledException if the specific condition(s) occcur.
    ///      4)  TestPlanDefinition.xml's CancelNotPassed:
    ///          - TestPlanDefinition.xml's Method elements have Boolean "CancelNotPassed" fields:
    ///          - If the current Test.MethodRun() has CancelNotPassed=true and it's resulting EvaluateResultMethod() doesn't return EVENTS.PASS,
    ///            TestExec.MethodsRun() will break/exit, stopping further testing.
    ///		    - Do not pass Go, do not collect $200, go directly to TestExec.MethodsPostRun().
    ///
    /// NOTE:  The Operator Proactive &amp; TestExec Developer initiated cancellations both occur while the currently executing Tests.MethodRun() conpletes, via 
    ///        thrown OperationCanceledException.
    /// NOTE:  The Operator Reactive &amp; TestPlanDefinition.xml's CancelNotPassed cancellations both occur after the currently executing Tests.MethodRun() completes, via checks
    ///        inside the Exec.MethodsRun() loop.
    /// </para>
    /// </summary>
    public abstract partial class TestExec : Form {
        public static System.Timers.Timer StatusTimer = new System.Timers.Timer(10000);
        private const String _serialNumberMostRecent = "MostRecent";
        private const String _NOT_APPLICABLE = "NotApplicable";
        private readonly SerialNumberDialog _serialNumberDialog = null;

        protected TestExec(Icon icon, String baseDirectory) {
            InitializeComponent();
            Icon = icon; // NOTE:  https://stackoverflow.com/questions/40933304/how-to-create-an-icon-for-visual-studio-with-just-mspaint-and-visual-studio
            BaseDirectory = baseDirectory;
            TestPlanDefinitionXML = BaseDirectory + @"\TestPlan\TestPlanDefinition.xml";
            if (TestPlanDefinitionValidator.ValidSpecification(TestPlanDefinitionXSD, TestPlanDefinitionXML)) testPlanDefinition = Serializing.DeserializeFromFile<TestPlanDefinition>(xmlFile: $"{TestPlanDefinitionXML}");
            else throw new ArgumentException($"Invalid XML '{TestPlanDefinitionXML}'; doesn't comply with XSD '{TestPlanDefinitionXSD}'.");

            UserName = GetUserPrincipal();
            _ = Task.Run(() => LoadDeveloperAddresses());

            InstrumentDrivers = GetInstrumentDriversTestPlanDefinition();

            TSMI_UUT_TestData.Enabled = testPlanDefinition.SerialNumberEntry.IsEnabled();
            if (TSMI_UUT_TestData.Enabled) {
                if (!(testExecDefinition.TestData.Item is TextFiles) && !(testExecDefinition.TestData.Item is SQL_DB)) throw new ArgumentException($"Unknown {nameof(TestPlanDefinition)}.{nameof(TestData)}.{nameof(TestData.Item)} '{nameof(testExecDefinition.TestData.Item)}'.");
                TSMI_UUT_TestDataP_DriveTDR_Folder.Enabled = (testExecDefinition.TestData.Item is TextFiles);
                TSMI_UUT_TestDataSQL_ReportingAndQuerying.Enabled = (testExecDefinition.TestData.Item is SQL_DB);

                if (RegexInvalid(testPlanDefinition.SerialNumberEntry.RegularEx)) throw new ArgumentException($"Invalid {nameof(SerialNumberEntry.RegularEx)} '{testPlanDefinition.SerialNumberEntry.RegularEx}' in file '{TestPlanDefinitionXML}'.");
                if (testPlanDefinition.SerialNumberEntry.EntryType is SerialNumberEntryType.Barcode) _serialNumberDialog = new SerialNumberDialog(testPlanDefinition.SerialNumberEntry.RegularEx, testPlanDefinition.SerialNumberEntry.Format, testExecDefinition.BarcodeReader.ID);

            }

            StatusTimer.Elapsed += StatusTimeUpdate;
            StatusTimer.AutoReset = true;
            CTS_Cancel = new CancellationTokenSource();
            CT_Cancel = CTS_Cancel.Token;
            CTS_EmergencyStop = new CancellationTokenSource();
            CT_EmergencyStop = CTS_EmergencyStop.Token;
        }

        #region Form Miscellaneous
        public static void ErrorMessage(String Error) {
            _ = MessageBox.Show(ActiveForm, $"Unexpected error:{Environment.NewLine}{Error}", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
        }

        public static void ErrorMessage(Exception Ex) {
            if (testPlanDefinition != null && testPlanDefinition.Development.Developer != null && testPlanDefinition.Development.EMailAddresses != null) {
                if (!String.Equals(testPlanDefinition.Development.EMailAddresses, String.Empty)) {
                    ErrorMessage($"'{Ex.Message}'{Environment.NewLine}{Environment.NewLine}Will attempt to E-Mail details To {testPlanDefinition.Development.EMailAddresses}.{Environment.NewLine}{Environment.NewLine}Please select your Microsoft 365 Outlook profile if dialog appears.");
                    SendDevelopersMailMessage("Exception caught!", Ex);
                }
            }
        }

        private static String GetUserPrincipal() {
            String UserName;
            try { UserName = UserPrincipal.Current.DisplayName; } catch {
                // NOTE:  UserPrincipal.Current.DisplayName requires a connected/active Domain session for Active Directory PCs.
                UserName = InputForm.Show(Title: "Enter your full name for test data.", SystemIcons.Question);
            }
            UserName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(UserName);
            return UserName;
        }

        private void Form_Closing(Object sender, FormClosingEventArgs e) {
            SystemReset();
            _serialNumberDialog?.Close();
            MutexTest?.ReleaseMutex();
            MutexTest?.Dispose();
        }

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
            TSMI_Test.Enabled = false;
            TSMI_System_SelfTests.Enabled = false;
            TSMI_System_BarcodeScannerDiscovery.Enabled = false;
            TSMI_UUT_Statistics.Enabled = false;
            StatusModeUpdate(MODES.Running);
        }

        private void FormModeWait() {
            ButtonCancelReset(enabled: false);
            ButtonEmergencyStopReset(enabled: false);
            TSMI_Test.Enabled = true;
            ButtonSelect.Enabled = true;
            ButtonRunReset(enabled: testSequence != null);
            TSMI_System_SelfTests.Enabled = true;
            TSMI_System_BarcodeScannerDiscovery.Enabled = true;
            TSMI_UUT_Statistics.Enabled = true;
            StatusModeUpdate(MODES.Waiting);
        }

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
            if (testPlanDefinition.TestSpace.Simulate) return;
            IPowerSuppliesOutputsOff();
            IInstrumentsResetClear();
            IRelaysOpenAll();
        }

        public virtual void IInstrumentsResetClear() {
            if (testPlanDefinition.TestSpace.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in InstrumentDrivers) if (kvp.Value is IInstrument iInstrument) iInstrument.ResetClear();
        }

        public virtual void IPowerSuppliesOutputsOff() {
            if (testPlanDefinition.TestSpace.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in InstrumentDrivers) if (kvp.Value is IPowerSupply iIPowerSupply) iIPowerSupply.OutputsOff();
        }

        public virtual void IRelaysOpenAll() {
            if (testPlanDefinition.TestSpace.Simulate) return;
            foreach (KeyValuePair<String, Object> kvp in InstrumentDrivers) if (kvp.Value is IRelay iRelay) iRelay.OpenAll();
        }

        private void InvalidPathError(String InvalidPath) { _ = MessageBox.Show(this, $"Path {InvalidPath} invalid.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error); }

        public static String NotImplementedMessageEnum(Type enumType) { return $"Unimplemented Enum item; switch/case must support all items in enum '{String.Join(",", Enum.GetNames(enumType))}'."; }

        private void OpenApp(String appPath, String arguments = "") {
            if (File.Exists(appPath)) {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = $"\"{appPath}\"",
                    WindowStyle = ProcessWindowStyle.Normal,
                    WorkingDirectory = "",
                    Arguments = String.Equals(arguments, "") ? "" : $"\"{arguments}\""
                    // Paths with embedded spaces require enclosing double-quotes (").
                    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                };
                _ = Process.Start(psi);
            } else InvalidPathError(appPath);
        }

        private void OpenFolder(String folderPath) {
            if (Directory.Exists(folderPath)) {
                ProcessStartInfo psi = new ProcessStartInfo {
                    FileName = "explorer.exe",
                    WindowStyle = ProcessWindowStyle.Normal,
                    Arguments = $"\"{folderPath}\""
                    // Paths with embedded spaces require enclosing double-quotes (").
                    // Even then, simpler 'System.Diagnostics.Process.Start("explorer.exe", path);' invocation fails - thus using ProcessStartInfo class.
                    // https://stackoverflow.com/questions/334630/opening-a-folder-in-explorer-and-selecting-a-file
                };
                _ = Process.Start(psi);
            } else InvalidPathError(folderPath);
        }

        public static Boolean RegexInvalid(String RegularExpression) {
            if (String.IsNullOrWhiteSpace(RegularExpression)) return true;
            try {
                _ = Regex.Match("", RegularExpression);
            } catch (ArgumentException) {
                return true;
            }
            return false;
        }

        public static void SendDevelopersMailMessage(String Subject, Exception Ex) {
            const Int32 PR = 22;
            StringBuilder stringBuilder = new StringBuilder();
            _ = stringBuilder.AppendLine($"{nameof(Environment.MachineName)}".PadRight(PR) + $": {Environment.MachineName}");
            _ = stringBuilder.AppendLine($"{UserName}".PadRight(PR) + $": {UserName}");
            _ = stringBuilder.AppendLine($"Exception.ToString()".PadRight(PR) + $": {Ex}");
            SendDevelopersMailMessage(Subject, Body: stringBuilder.ToString());
        }

        public static void SendDevelopersMailMessage(String Subject, String Body) {
            try {
                Outlook.MailItem mailItem = GetMailItem();
                mailItem.Subject = Subject;
                mailItem.To = testPlanDefinition.Development.EMailAddresses;
                mailItem.Importance = Outlook.OlImportance.olImportanceHigh;
                mailItem.BodyFormat = Outlook.OlBodyFormat.olFormatPlain;
                mailItem.Body = Body;
                mailItem.Send();
            } catch {
                _ = MessageBox.Show(ActiveForm, $"Sorry, cannot E-Mail presently.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private void SendMailMessageWithAttachment(String subject) {
            try {
                Outlook.MailItem mailItem = GetMailItem();
                mailItem.Subject = subject;
                mailItem.To = testPlanDefinition.Development.EMailAddresses;
                mailItem.Importance = Outlook.OlImportance.olImportanceHigh;
                mailItem.Body =
                    $"Please detail desired Bug Report or Improvement Request:{Environment.NewLine}" +
                    $" - Please attach relevant files, and/or embed relevant screen-captures.{Environment.NewLine}" +
                    $" - Be specific!  Be verbose!  Unleash your inner author!  It's your time to shine!{Environment.NewLine}";
                String rtfTempFile = $"{Path.GetTempPath()}\\{testPlanDefinition.UUT.Number}.rtf";
                rtfResults.SaveFile(rtfTempFile);
                _ = mailItem.Attachments.Add(rtfTempFile, Outlook.OlAttachmentType.olByValue, 1, $"{testPlanDefinition.UUT.Number}.rtf");
                mailItem.Display();
            } catch {
                _ = MessageBox.Show(this, $"Sorry, cannot E-Mail presently.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static async Task LoadDeveloperAddresses() {
            Outlook.Application outlookApp = new Outlook.Application();
            Outlook.NameSpace outlookNamespace = outlookApp.GetNamespace("MAPI");
            Outlook.AddressList addressList = outlookNamespace.AddressLists["Offline Global Address List"];

            if (addressList != null) {
                try {
                    Object task;
                    foreach (Developer developer in testPlanDefinition.Development.Developer) {
                        task = await Task.Run(() => GetAddress(addressList, developer.Name));
                        developer.EMailAddress = (String)task;
                        if (!String.Equals(developer.EMailAddress, String.Empty)) testPlanDefinition.Development.EMailAddresses += $"{developer.EMailAddress}; ";
                    }
                } catch { }
                ;
                if (testPlanDefinition.Development.EMailAddresses.EndsWith("; ")) testPlanDefinition.Development.EMailAddresses = testPlanDefinition.Development.EMailAddresses.Substring(0, testPlanDefinition.Development.EMailAddresses.Length - 2);
            }
        }

        private static String GetAddress(Outlook.AddressList addressList, String Name) {
            Outlook.ExchangeUser exchangeUser;
            foreach (Outlook.AddressEntry entry in addressList.AddressEntries) {
                if (entry != null) {
                    exchangeUser = entry.GetExchangeUser();
                    if (exchangeUser != null && String.Equals(exchangeUser.Name, Name)) return exchangeUser.PrimarySmtpAddress;
                }
            }
            return String.Empty;
        }
        #endregion Form Miscellaneous

        #region Form Command Buttons
        private void ButtonCancel_Enter(Object sender, EventArgs e) {
            TSMI_About_TestPlan.Select(); // Prevents ButtonCanel or ButtonEmergencyStop from having focus, so if a MessageBox loses focus while testing and operator presses keyboard Enter key, won't cancel or Emergency Stop.
        }

        private void ButtonCancel_Clicked(Object sender, EventArgs e) {
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

        private void ButtonEmergencyStop_Enter(Object sender, EventArgs e) {
            TSMI_About_TestPlan.Select(); // Prevents ButtonCanel or ButtonEmergencyStop from having focus, so if a MessageBox loses focus while testing and operator presses keyboard Enter key, won't cancel or Emergency Stop.
        }

        private void ButtonEmergencyStop_Clicked(Object sender, EventArgs e) {
            ButtonEmergencyStop.Enabled = false;
            ButtonCancelReset(enabled: false);
            StatusModeUpdate(MODES.Emergency_Stopping);
            SystemReset();
            CTS_EmergencyStop.Cancel();
        }

        private void ButtonEmergencyStopReset(Boolean enabled) {
            if (CT_EmergencyStop.IsCancellationRequested) {
                CTS_EmergencyStop.Dispose();
                CTS_EmergencyStop = new CancellationTokenSource();
                CT_EmergencyStop = CTS_EmergencyStop.Token;
            }
            ButtonEmergencyStop.Enabled = enabled;
        }

        private void ButtonSelect_Click(Object sender, EventArgs e) {
            testSequence = TestSelect.Get();
            base.Text = $"{testSequence.UUT.Number}, {testSequence.UUT.Description}, {(testSequence.IsOperation ? testSequence.TestOperation.NamespaceTrunk : testSequence.TestOperation.TestGroups[0].Classname)}";
            StatusTimer.Start();
            FormModeReset();
            FormModeWait();
        }

        private async void ButtonRun_Clicked(Object sender, EventArgs e) {
            TSMI_About_TestPlan.Select(); // Prevents ButtonCanel or ButtonEmergencyStop from having focus, so if a MessageBox loses focus while testing and operator presses keyboard Enter key, won't cancel or Emergency Stop.
            if (testPlanDefinition.SerialNumberEntry.IsEnabled()) {
                String serialNumber;
                if (testPlanDefinition.SerialNumberEntry.EntryType is SerialNumberEntryType.Barcode) {
                    _serialNumberDialog.Set(testSequence.SerialNumber);
                    serialNumber = _serialNumberDialog.ShowDialog(this).Equals(DialogResult.OK) ? _serialNumberDialog.Get() : String.Empty;
                    _serialNumberDialog.Hide();
                } else {
                    serialNumber = Interaction.InputBox(
                        Prompt: $"Please enter Serial Number in below format:{Environment.NewLine}{Environment.NewLine}" +
                        $"{testPlanDefinition.SerialNumberEntry.Format}",
                        Title: "Enter Serial Number", DefaultResponse: testSequence.SerialNumber).Trim().ToUpper();
                    serialNumber = Regex.IsMatch(serialNumber, testPlanDefinition.SerialNumberEntry.RegularEx) ? serialNumber : String.Empty;
                }
                if (String.Equals(serialNumber, String.Empty)) return;
                testSequence.SerialNumber = serialNumber;
            }

            FormModeReset();
            FormModeRun();
            MethodsPreRun();
            await MethodsRun();
            MethodsPostRun();
            FormModeWait();
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
        private void TSMI_Test_Choose_Click(Object sender, EventArgs e) {
            // NOTE: Canonical method to load/unload DLLs in .Net Framework is AppDomain.
            // - But AppDomains require marshalling across process boundaries, as AppDomains are their own separate processes.
            // - Further, AppDomains aren't supported in .Net, just .Net Framework.
            // - .Net instead provides AssemblyLoadContext which would be perfect for TestExec...but isn't available in .Net Framework.
            // - Thus this compromise.
            ProcessStartInfo processStartInfo = new ProcessStartInfo(testExecDefinition.Apps.ABT.TestChooser) {
                Arguments = Convert.ToString(Process.GetCurrentProcess().Id),
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardError = false,
                RedirectStandardOutput = false
            };
            _ = Process.Start(processStartInfo);
            Application.Exit();
        }
        private void TSMI_Test_SaveResults_Click(Object sender, EventArgs e) {
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save Test Results",
                Filter = "Rich Text Format|*.rtf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                FileName = $"{testSequence.UUT.Number}_{testSequence.TestOperation.NamespaceTrunk}_{testSequence.SerialNumber}",
                DefaultExt = "rtf",
                CreatePrompt = false,
                OverwritePrompt = true
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK) rtfResults.SaveFile(saveFileDialog.FileName);
        }
        private void TSMI_Test_Exit_Click(Object sender, EventArgs e) { Application.Exit(); }

        private void TSMI_Apps_ABTGenerateTestPlan_Click(Object sender, EventArgs e) {
            if (!TestPlanDefinitionValidator.ValidSpecification(TestPlanDefinitionXSD: TestPlanDefinitionXSD, TestPlanDefinitionXML)) return;
            TestPlanGenerator.Generate(TestPlanDefinitionXML);
        }
        private void TSMI_Apps_ABTChooseTestPlan_Click(Object sender, EventArgs e) { TSMI_Test_Choose_Click(sender, e); }
        private void TSMI_Apps_ABTValidateTestPlanDefinition_Click(Object sender, EventArgs e) {
            if (TestPlanDefinitionValidator.ValidSpecification(TestPlanDefinitionXSD, TestPlanDefinitionXML)) _ = MessageBox.Show(this, "Validation passed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        private void TSMI_Apps_KeysightCommandExpert_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Keysight.CommandExpert); }
        private void TSMI_Apps_KeysightConnectionExpert_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Keysight.ConnectionExpert); }
        private void TSMI_Apps_MicrosoftSQL_ServerManagementStudio_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Microsoft.SQLServerManagementStudio); }
        private void TSMI_Apps_MicrosoftVisualStudio_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Microsoft.VisualStudio); }
        private void TSMI_Apps_MicrosoftVisualStudioCode_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Microsoft.VisualStudioCode); }
        private void TSMI_Apps_MicrosoftXML_Notepad_Click(Object sender, EventArgs e) { OpenApp(testExecDefinition.Apps.Microsoft.XMLNotepad); }

        private void TSMI_Feedback_ComplimentsPraiseεPlaudits_Click(Object sender, EventArgs e) { _ = MessageBox.Show(this, $"You are a kind person, {UserName}.", $"Thank you!", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void TSMI_Feedback_ComplimentsMoney_Click(Object sender, EventArgs e) { _ = MessageBox.Show(this, $"Prefer ₿itcoin donations!", $"₿₿₿", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void TSMI_Feedback_CritiqueBugReport_Click(Object sender, EventArgs e) { SendMailMessageWithAttachment($"Bug Report from {UserName} for {testPlanDefinition.UUT.Number}, {testPlanDefinition.UUT.Description}."); }
        private void TSMI_Feedback_CritiqueImprovementRequest_Click(Object sender, EventArgs e) { SendMailMessageWithAttachment($"Improvement Request from {UserName} for {testPlanDefinition.UUT.Number}, {testPlanDefinition.UUT.Description}."); }

        private async void TSMI_System_BarcodeScannerDiscovery_Click(Object sender, EventArgs e) {
            DeviceInformationCollection dic = await DeviceInformation.FindAllAsync(BarcodeScanner.GetDeviceSelector(PosConnectionTypes.Local));
            StringBuilder stringBuilder = new StringBuilder();
            _ = stringBuilder.AppendLine($"Discovering Microsoft supported, corded Barcode Scanner(s):{Environment.NewLine}");
            _ = stringBuilder.AppendLine($"  - See https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/pos-device-support.");
            _ = stringBuilder.AppendLine($"  - Note that only corded Barcode Scanners are discovered; cordless BlueTooth & Wireless scanners are ignored.");
            _ = stringBuilder.AppendLine($"  - Note that cameras are also discovered; cameras are digital imagers, just as many bar-code readers are.");
            _ = stringBuilder.AppendLine($"  - Modify ConfigurationTestExec to use a discovered Barcode Scanner.");
            _ = stringBuilder.AppendLine($"  - Scanners must be programmed into USB-HID mode to function properly:");
            _ = stringBuilder.AppendLine(@"    - See: file:///P:/Test/Engineers/Equipment_Manuals/Honeywell/Honeywell_Voyager_1200G_User's_Guide_ReadMe.pdf");
            _ = stringBuilder.AppendLine($"    - Or:  https://prod-edam.honeywell.com/content/dam/honeywell-edam/sps/ppr/en-us/public/products/barcode-scanners/general-purpose-handheld/1200g/documents/sps-ppr-vg1200-ug.pdf{Environment.NewLine}{Environment.NewLine}");
            foreach (DeviceInformation di in dic) {
                _ = stringBuilder.AppendLine($"Name: '{di.Name}'.");
                _ = stringBuilder.AppendLine($"Kind: '{di.Kind}'.");
                _ = stringBuilder.AppendLine($"ID  : '{di.Id}'.{Environment.NewLine}");
            }

            CustomMessageBox.Show(Title: $"Microsoft supported, corded Barcode Scanner(s)", Message: stringBuilder.ToString());
        }
        private void TSMI_System_ColorCode_Click(Object sender, EventArgs e) {
            CustomMessageBox customMessageBox = new CustomMessageBox {
                Icon = SystemIcons.Information,
                Text = "Event Color Codes"
            };
            RichTextBox richTextBox = (RichTextBox)customMessageBox.Controls["richTextBox"];
            richTextBox.Font = new Font(richTextBox.Font.FontFamily, 12);
            richTextBox.Text = String.Empty;
            foreach (EVENTS Event in Enum.GetValues(typeof(EVENTS))) richTextBox.Text += $"{nameof(EVENTS)}.{Event}{Environment.NewLine}{Environment.NewLine}";

            foreach (EVENTS Event in Enum.GetValues(typeof(EVENTS))) {
                String s = $"{nameof(EVENTS)}.{Event}";
                richTextBox.SelectionStart = richTextBox.Find(s, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                richTextBox.SelectionLength = s.Length;
                if (EventColors.ContainsKey(Event)) richTextBox.SelectionBackColor = EventColors[Event];
                else throw new InvalidOperationException($"Test Engineering needs to update '{nameof(EventColors)}', associating '{nameof(EVENTS)}.{Event}' with a color!");
            }
            customMessageBox.ShowDialog();
        }
        private void TSMI_System_SelfTestsInstruments_Click(Object sender, EventArgs e) {
            UseWaitCursor = true;
            Boolean passed = true;
            foreach (KeyValuePair<String, Object> kvp in InstrumentDrivers) passed &= ((IInstrument)kvp.Value).SelfTests() is SELF_TEST_RESULTS.PASS;
            if (passed) _ = MessageBox.Show(this, "SCPI VISA Instrument Self-Tests all passed.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UseWaitCursor = false;
        }
        private void TSMI_System_ManualsBarcodeScanner_Click(Object sender, EventArgs e) { OpenFolder(testExecDefinition.BarcodeReader.Folder); }
        private void TSMI_System_ManualsInstruments_Click(Object sender, EventArgs e) { OpenFolder(testExecDefinition.InstrumentsTestExec.Folder); }

        private void TSMI_UUT_eDocs_Click(Object sender, EventArgs e) {
            foreach (Documentation documentation in testPlanDefinition.UUT.Documentation) OpenFolder(documentation.Folder);
        }
        private void TSMI_UUT_Manuals_Click(Object sender, EventArgs e) {
            foreach (Documentation documentation in testPlanDefinition.Development.Documentation) OpenFolder(documentation.Folder);
        }
        private void TSMI_UUT_StatisticsDisplay_Click(Object sender, EventArgs e) {
            Form statistics = new Miscellaneous.MessageBoxMonoSpaced(
                Title: $"{testSequence.UUT.Number}, {testSequence.TestOperation.NamespaceTrunk}, {testPlanDefinition.TestSpace.StatusTime()}",
                Text: testPlanDefinition.TestSpace.StatisticsDisplay(),
                Link: String.Empty
            );
            _ = statistics.ShowDialog();
        }
        private void TSMI_UUT_StatisticsReset_Click(Object sender, EventArgs e) {
            testPlanDefinition.TestSpace.Statistics = new Statistics();
            StatusTimeUpdate(null, null);
            StatusStatisticsUpdate(null, null);
        }
        private void TSMI_UUT_TestData_P_DriveTDR_Folder_Click(Object sender, EventArgs e) {
            Debug.Assert(testExecDefinition.TestData.Item is TextFiles);
            OpenFolder($"{((TextFiles)testExecDefinition.TestData.Item).Folder}\\{testPlanDefinition.UUT.Number}\\{testSequence.TestOperation.NamespaceTrunk}");
        }
        private void TSMI_UUT_TestDataSQL_ReportingAndQuerying_Click(Object sender, EventArgs e) {
            Debug.Assert(testExecDefinition.TestData.Item is SQL_DB);
            OpenApp(testExecDefinition.Apps.Microsoft.SQLServerManagementStudio);
        }
        private void TSMI_About_TestExec_Click(Object sender, EventArgs e) {
            Development development = Serializing.DeserializeFromFile<Development>(TestExecDefinitionXML);
            ShowAbout(Assembly.GetExecutingAssembly(), development);
        }
        private void TSMI_About_TestPlan_Click(Object sender, EventArgs e) {
            ShowAbout(Assembly.GetEntryAssembly(), testPlanDefinition.Development, isTestPlan: true);
        }
        private void ShowAbout(Assembly assembly, Development development, Boolean isTestPlan = false) {
            StringBuilder stringBuilder = new StringBuilder();
            const Int32 PR = 16;
            stringBuilder.AppendLine($"{nameof(Assembly)}:");
            stringBuilder.AppendLine($"\t{nameof(Name)}".PadRight(PR) + $": {assembly.GetName().Name}");
            stringBuilder.AppendLine($"\t{nameof(Version)}".PadRight(PR) + $": {assembly.GetName().Version}");
            stringBuilder.AppendLine($"\tBuilt".PadRight(PR) + $": {BuildDate(assembly.GetName().Version)}");
            AssemblyCopyrightAttribute assemblyCopyrightAttribute = (AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute));
            String copyRight = assemblyCopyrightAttribute is null ? "© Amphenol Borisch Technologies" : assemblyCopyrightAttribute.Copyright;
            stringBuilder.AppendLine($"\t{copyRight}{Environment.NewLine}{Environment.NewLine}");

            stringBuilder.AppendLine($"{nameof(Repository)}(s):");
            foreach (Repository repository in development.Repository) stringBuilder.AppendLine($"\t{nameof(Repository.URL)}".PadRight(PR) + $": {repository.URL}");
            stringBuilder.AppendLine($"{Environment.NewLine}");

            stringBuilder.AppendLine($"{nameof(Development)}:");
            stringBuilder.AppendLine($"\t{nameof(Development.Released)}".PadRight(PR) + $": {development.Released:d}");
            foreach (Developer developer in development.Developer) stringBuilder.AppendLine($"\t{nameof(Development)}".PadRight(PR) + $": {developer.Name}, {developer.Language}.");
            foreach (Documentation documentation in development.Documentation) stringBuilder.AppendLine($"\t{nameof(Documentation)}".PadRight(PR) + $": {ConvertWindowsPathToUrl(documentation.Folder)}");

            CustomMessageBox.Show(Title: $"About {(isTestPlan ? "TestPlan " : String.Empty)}{assembly.GetName().Name}", Message: stringBuilder.ToString());
        }
        #endregion Form Tool Strip Menu Items

        #region Methods
        private void MethodsPreRun() {
            testSequence.PreRun();
            TestIndices.Nullify();
            Logger.Start(ref rtfResults);
            SystemReset();
        }

        private async Task MethodsRun() {
            TestIndices.TestOperation = testSequence.TestOperation;
            foreach (TestGroup testGroup in testSequence.TestOperation.TestGroups) {
                TestIndices.TestGroup = testGroup;
                foreach (Method method in testGroup.Methods) {
                    TestIndices.Method = method;
                    try {
                        method.Value = await Task.Run(() => MethodRun(method));
                        method.Event = ((IEvaluate)method).Evaluate();
                        method.LogString = method.Log.ToString(); // NOTE:  XmlSerializer doesn't support [OnSerializing] attribute, so have to explicitly invoke LogConvert().
                        if (CT_EmergencyStop.IsCancellationRequested || CT_Cancel.IsCancellationRequested) {
                            SystemReset();
                            return;
                        }
                    } catch (Exception e) {
                        SystemReset();
                        if (e.ToString().Contains(typeof(OperationCanceledException).Name)) {
                            method.Event = EVENTS.CANCEL;// NOTE:  May be altered to EVENTS.EMERGENCY_STOP in finally block.
                            while (!(e is OperationCanceledException) && (e.InnerException != null)) e = e.InnerException; // No fluff, just stuff.
                            _ = method.Log.AppendLine($"{Environment.NewLine}{typeof(OperationCanceledException).Name}:{Environment.NewLine}{e.Message}");
                        }
                        if (!CT_EmergencyStop.IsCancellationRequested && !CT_Cancel.IsCancellationRequested) {
                            method.Event = EVENTS.ERROR;
                            _ = method.Log.AppendLine($"{Environment.NewLine}{e}");
                            ErrorMessage(e);
                        }
                        return;
                    } finally {
                        // NOTE:  Normally executes, regardless if catchable Exception occurs or returned out of try/catch blocks.
                        // Exceptional exceptions are exempted; https://stackoverflow.com/questions/345091/will-code-in-a-finally-statement-fire-if-i-return-a-value-in-a-try-block.
                        if (CT_EmergencyStop.IsCancellationRequested) method.Event = EVENTS.EMERGENCY_STOP;
                        else if (CT_Cancel.IsCancellationRequested) method.Event = EVENTS.CANCEL;
                        // NOTE:  Both CT_Cancel.IsCancellationRequested & CT_EmergencyStop.IsCancellationRequested could be true; prioritize CT_EmergencyStop.
                        Logger.LogMethod(ref rtfResults, method);
                    }
                    if (method.Event != EVENTS.PASS && method.CancelNotPassed) return;
                }
                if (GroupEvaluate(testGroup) != EVENTS.PASS && testGroup.CancelNotPassed) return;
            }
        }

        protected abstract Task<String> MethodRun(Method method);

        private void MethodsPostRun() {
            SystemReset();
            testSequence.PostRun(OperationEvaluate(testSequence.TestOperation));
            TextTest.Text = testSequence.Event.ToString();
            TextTest.BackColor = EventColors[testSequence.Event];
            testPlanDefinition.TestSpace.Statistics.Update(testSequence.Event);
            StatusStatisticsUpdate(null, null);
            Logger.Stop(ref rtfResults);
        }

        private Boolean EventSet(Int32 aggregatedEvents, EVENTS events) { return ((aggregatedEvents & (Int32)events) == (Int32)events); }

        private EVENTS GroupEvaluate(TestGroup testGroup) {
            Int32 groupEvents = 0;
            foreach (Method method in testGroup.Methods) groupEvents |= (Int32)method.Event;
            foreach (EVENTS events in Enum.GetValues(typeof(EVENTS))) if (EventSet(groupEvents, events)) return events;

            throw new NotImplementedException(($"{nameof(testGroup.Classname)}: '{testGroup.Classname}', {nameof(testGroup.Description)} '{testGroup.Description}' doesn't contain valid {nameof(EVENTS)}."));
        }

        private EVENTS OperationEvaluate(TestOperation testOperation) {
            Int32 operationEvents = 0;
            foreach (TestGroup testGroup in testOperation.TestGroups) operationEvents |= (Int32)GroupEvaluate(testGroup);
            foreach (EVENTS events in Enum.GetValues(typeof(EVENTS))) if (EventSet(operationEvents, events)) return events;

            throw new NotImplementedException(($"{nameof(testOperation.NamespaceTrunk)}: '{testOperation.NamespaceTrunk}', {nameof(testOperation.Description)} '{testOperation.Description}' doesn't contain valid {nameof(EVENTS)}."));
        }
        #endregion Methods

        #region Status Strip methods.
        private void StatusTimeUpdate(Object source, ElapsedEventArgs e) { StatusTimeLabel.Text = testPlanDefinition.TestSpace.StatusTime(); }

        private void StatusStatisticsUpdate(Object source, ElapsedEventArgs e) { StatusStatisticsLabel.Text = testPlanDefinition.TestSpace.StatisticsStatus(); }

        private enum MODES { Resetting, Running, Cancelling, Emergency_Stopping, Waiting };

        private static readonly Dictionary<MODES, Color> ModeColors = new Dictionary<MODES, Color>() {
            { MODES.Resetting, EventColors[EVENTS.UNSET] },
            { MODES.Running, EventColors[EVENTS.PASS] },
            { MODES.Cancelling, EventColors[EVENTS.CANCEL] },
            { MODES.Emergency_Stopping, EventColors[EVENTS.EMERGENCY_STOP] },
            { MODES.Waiting, Color.Black }
        };

        private void StatusModeUpdate(MODES mode) {
            StatusModeLabel.Text = Enum.GetName(typeof(MODES), mode);
            StatusModeLabel.ForeColor = ModeColors[mode];
        }

        public void StatusCustomWrite(String Message, Color ForeColor) {
            _ = Invoke((Action)(() => StatusCustomLabel.Text = Message));
            _ = Invoke((Action)(() => StatusCustomLabel.ForeColor = ForeColor));
        }
        #endregion Status Strip methods.
    }
}
