#undef VERBOSE
using System;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Serilog; // Install Serilog via NuGet Package Manager.  Site is https://serilog.net/.
using ABT.Test.TestLib;
using ABT.Test.TestLib.TestConfiguration;

// TODO:  Eventually; persist measurement data into Microsoft SQL Server Express; write all full Operation TestMeasurement output therein.
// - Stop writing TestMeasurement output to RichTextBoxSink when testing full Operations; only write TestGroups output to RichTextBoxSink.
// - Continue writing TestMeasurement output to RichTextBoxSink when only testing Groups.
// - Stop saving RichTextBoxSink as RTF files, except allow manual export for Troubleshooting.
// - This will resolve the RichTextBox scroll issue, wherein TestGroups output are scrolled up & away as TestMeasurements are appended.
// - Only SQL Server Express persisted measurement data is legitimate; all RichTextBoxSink is Troubleshooting only.
// - Create a Microsoft C# front-end exporting/reporting app for persisted SQL Server Express TestMeasurement full Operation measurement data.
// - Export in CSV, report in PDF.
//

namespace ABT.Test.TestExec.Logging {
    public static class Logger {
        public const String LOGGER_TEMPLATE = "{Message}{NewLine}";
        public const String SPACES_21 = "                     ";
        private const String MESSAGE_STOP = "Stop              : ";
        private const String MESSAGE_TEST_EVENT = "Test Event";
        private const String MESSAGE_UUT_EVENT = MESSAGE_TEST_EVENT + "        : ";

        #region Public Methods
        public static String FormatMessage(String Label, String Message) { return $"  {Label}".PadRight(SPACES_21.Length) + $" : {Message}"; }

        public static String FormatNumeric(MethodInterval methodInterval) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("High Limit", $"{methodInterval.High:G}"));
            sb.AppendLine(FormatMessage("Measured", $"{Math.Round(Double.Parse((String)methodInterval.Value), (Int32)methodInterval.FractionalDigits, MidpointRounding.ToEven)}"));
            sb.AppendLine(FormatMessage("Low Limit", $"{methodInterval.Low:G}"));
            String units = $"{Enum.GetName(typeof(MI_Units), methodInterval.Units)}";
            if (methodInterval.UnitSuffix != MI_UnitSuffix.NONE) units += $" {Enum.GetName(typeof(MI_UnitSuffix), methodInterval.UnitSuffix)}";
            sb.Append(FormatMessage("Units", units));
            return sb.ToString();
        }

        public static String FormatProcess(MethodProcess methodProcess) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("Expected", methodProcess.Expected));
            sb.Append(FormatMessage("Actual", (String)methodProcess.Value));
            return sb.ToString();
        }

        public static String FormatTextual(MethodTextual methodTextual) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("Expected", methodTextual.Text));
            sb.Append(FormatMessage("Actual", (String)methodTextual.Value));
            return sb.ToString();
        }

        public static void LogError(String logMessage) { Log.Error(logMessage); }

        public static void LogMessage(String Message) { Log.Information(Message); }

        public static void LogTest(Boolean isOperation, Method method, ref RichTextBox rtfResults) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage("Method", method.Name));
            stringBuilder.AppendLine(FormatMessage("Cancel Not Passed", method.CancelNotPassed.ToString()));
            stringBuilder.AppendLine(FormatMessage("Description", method.Description));

            if (method is MethodCustom) { } // NOTE: Call LogMessage from Tests project to log any MethodCustom desired detail.
            else if (method is MethodInterval methodInterval) stringBuilder.AppendLine(FormatNumeric(methodInterval));
            else if (method is MethodProcess methodProcess) stringBuilder.AppendLine(FormatProcess(methodProcess));
            else if (method is MethodTextual methodTextual) stringBuilder.AppendLine(FormatTextual(methodTextual));
            else throw new NotImplementedException($"Method '{method.Name}', description '{method.Description}', with classname '{nameof(method)}' not implemented.");
            stringBuilder.AppendLine(FormatMessage(MESSAGE_TEST_EVENT, method.Event.ToString()));
            stringBuilder.Append(method.Log.ToString());
            Log.Information(stringBuilder.ToString());
            if (isOperation) SetBackColor(ref rtfResults, 0, method.Name, TestLib.TestLib.EventColors[method.Event]);
        }

        public static void Start(TestExec testExec, ref RichTextBox rtfResults) {
            if (TestSelection.IsGroup()) {
                // When TestGroups are executed, measurement data is never saved as Rich Text.
                // RichTextBox only. 
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
                Log.Information($"Note: following measurement results invalid for UUT production testing, only troubleshooting.");
                Log.Information(FormatMessage($"UUT Serial Number", $"{TestLib.TestLib.testDefinition.TestSpace.SerialNumber}"));
                Log.Information(FormatMessage($"UUT Number", $"{TestLib.TestLib.testDefinition.UUT.Number}"));
                Log.Information(FormatMessage($"UUT Revision", $"{TestLib.TestLib.testDefinition.UUT.Revision}"));
                Log.Information(FormatMessage($"TestGroup ", $"{TestSelection.TestGroup.Class}"));
                Log.Information(FormatMessage($"Description", $"{TestSelection.TestGroup.Description}"));
                Log.Information(FormatMessage($"Start", $"{DateTime.Now}\n"));
                return;
                // Log Header isn't written to Console when TestGroups are executed, further emphasizing measurements are invalid for pass verdict/$hip disposition, only troubleshooting failures.
            }

            if (TestLib.TestLib.testDefinition.TestData.Item is TextFiles textFiles) {
                // When TestOperations are executed, measurement data is always & automatically saved as Rich Text.
                // RichTextBox + File.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            } else if (TestLib.TestLib.testDefinition.TestData.Item is SQLDB sqlDB) {
                // TODO:  Eventually; RichTextBox + SQL.
                SQLStart();
            } else {
                // RichTextBox only; customer doesn't require saved measurement data, unusual for Functional testing, but potentially not for other testing methodologies.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            }
            Log.Information($"UUT:");
            Log.Information($"\t{MESSAGE_UUT_EVENT}");
            Log.Information($"\tSerial Number     : {TestLib.TestLib.testDefinition.TestSpace.SerialNumber}");
            Log.Information($"\tNumber            : {TestLib.TestLib.testDefinition.UUT.Number}");
            Log.Information($"\tRevision          : {TestLib.TestLib.testDefinition.UUT.Revision}");
            Log.Information($"\tDescription       : {TestLib.TestLib.testDefinition.UUT.Description}");
            Log.Information($"\tType              : {TestLib.TestLib.testDefinition.UUT.Category}");
            Log.Information($"\tCustomer          : {TestLib.TestLib.testDefinition.UUT.Customer}\n");

            Log.Information($"TestOperation:");
            Log.Information($"\tStart             : {DateTime.Now}");
            Log.Information($"\t{MESSAGE_STOP}");
            Log.Information($"\tUserPrincipal     : {UserPrincipal.Current.DisplayName}");
            // NOTE:  UserPrincipal.Current.DisplayName requires a connected/active Domain session for Active Directory PCs.
            Log.Information($"\tMachineName       : {Environment.MachineName}");
            Log.Information($"\tExec              : {Assembly.GetExecutingAssembly().GetName().Name}, {Assembly.GetExecutingAssembly().GetName().Version}, {BuildDate(Assembly.GetExecutingAssembly().GetName().Version)}");
            Log.Information($"\tTest              : {Assembly.GetEntryAssembly().GetName().Name}, {Assembly.GetEntryAssembly().GetName().Version} {BuildDate(Assembly.GetEntryAssembly().GetName().Version)}");
            Log.Information($"\tSpecification     : {TestLib.TestLib.testDefinition.UUT.TestSpecification}");
            Log.Information($"\tID                : {TestSelection.TestOperation.NamespaceTrunk}");
            Log.Information($"\tDescription       : {TestSelection.TestOperation.Description}\n");

            StringBuilder sb = new StringBuilder();
            foreach (TestGroup testGroup in TestSelection.TestOperation.TestGroups) {
                sb.Append(String.Format("\t{0,-" + testGroup.FormattingLengthGroupID + "} : {1}\n", testGroup.Class, testGroup.Description));
                foreach (Method method in testGroup.Methods) sb.Append(String.Format("\t\t{0,-" + testGroup.FormattingLengthMeasurementID + "} : {1}\n", method.Name, method.Description));
            }
            Log.Information($"TestMeasurements:\n{sb}");
        }

        public static String BuildDate(Version version) { 
            DateTime Y2K = new DateTime(year: 2000, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Local);
            return $"{Y2K + new TimeSpan(days: version.Build, hours: 0, minutes: 0, seconds: 2 * version.Revision):g}";
        }

        public static void Stop(TestExec testExec, ref RichTextBox rtfResults) {
            if (TestSelection.IsGroup()) Log.CloseAndFlush();
            // Log Trailer isn't written when not a TestOperation, further emphasizing measurement results aren't valid for passing & $hipping, only troubleshooting failures.
            else {
                ReplaceText(ref rtfResults, 0, MESSAGE_UUT_EVENT, MESSAGE_UUT_EVENT + TestLib.TestLib.testDefinition.TestSpace.Event.ToString());
                SetBackColor(ref rtfResults, 0, TestLib.TestLib.testDefinition.TestSpace.Event.ToString(), TestLib.TestLib.EventColors[TestLib.TestLib.testDefinition.TestSpace.Event]);
                ReplaceText(ref rtfResults, 0, MESSAGE_STOP, MESSAGE_STOP + DateTime.Now);
                Log.CloseAndFlush();
                if (TestLib.TestLib.testDefinition.TestSpace.Event != EVENTS.IGNORE) { // Don't save test data who's overall result is IGNORE.
                    if (TestLib.TestLib.testDefinition.TestData.Item is TextFiles textFiles) FileStop(testExec, ref rtfResults);
                    else if (TestLib.TestLib.testDefinition.TestData.Item is SQLDB sqlDB) SQLStop();
                }
            }
        }
#endregion Public Methods

        #region Private Methods
        private static void FileStop(TestExec testExec, ref RichTextBox rtfResults) {
            TextFiles textFiles = (TextFiles)TestLib.TestLib.testDefinition.TestData.Item;
            String textFilesFolder = $"{textFiles.Folder}{TestSelection.TestOperation.NamespaceTrunk}\\";
            String fileName = $"{TestLib.TestLib.testDefinition.UUT.Number}_{TestLib.TestLib.testDefinition.TestSpace.SerialNumber}_{TestSelection.TestOperation.NamespaceTrunk}";
            String[] files = Directory.GetFiles(textFilesFolder, $"{fileName}_*.rtf", SearchOption.TopDirectoryOnly);
            // Will fail if invalid path.  Don't catch resulting Exception though; this has to be fixed in App.config.
            // Otherwise, files is the set of all files like config.configUUT.Number_Config.configUUT.SerialNumber_TestSelection.TestOperation.NamespaceLeaf_*.rtf.
            Int32 maxNumber = 0; String s;
            foreach (String f in files) {
                s = f;
                s = s.Replace($"{textFilesFolder}{fileName}", String.Empty);
                s = s.Replace(".rtf", String.Empty);
                s = s.Replace("_", String.Empty);

                foreach (EVENTS Event in Enum.GetValues(typeof(EVENTS))) s = s.Replace(Event.ToString(), String.Empty);
                if (Int32.Parse(s) > maxNumber) maxNumber = Int32.Parse(s);
                // Example for final (3rd) iteration of foreach (String f in files):
                //   FileName            : 'UUTNumber_TestElementID_SerialNumber'
                //   Initially           : 'P:\Test\TDR\D4522142-1\T-30\UUTNumber_TestElementID_SerialNumber_3_PASS.rtf'
                //   FilePath + fileName : '_3_PASS.rtf'
                //   .txt                : '_3_PASS'
                //   _                   : '3PASS'
                //   foreach (FieldInfo  : '3'
                //   maxNumber           : '3'
            }
            fileName += $"_{++maxNumber}_{TestLib.TestLib.testDefinition.TestSpace.Event}.rtf";
            rtfResults.SaveFile($"{textFilesFolder}{fileName}");
        }
        
        private static void ReplaceText(ref RichTextBox richTextBox, Int32 startFind, String originalText, String replacementText) {
            Int32 selectionStart = richTextBox.Find(originalText, startFind, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
            if (selectionStart == -1) Log.Error($"Rich Text '{originalText}' not found after character '{startFind}', cannot replace with '{replacementText}'.");
            else {
                richTextBox.SelectionStart = selectionStart;
                richTextBox.SelectionLength = originalText.Length;
                richTextBox.SelectedText = replacementText;
            }
        }

        private static void SetBackColor(ref RichTextBox richTextBox, Int32 startFind, String findText, Color backColor) {
            Int32 selectionStart = richTextBox.Find(findText, startFind, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
            if (selectionStart == -1) Log.Error($"Rich Text '{findText}' not found after character '{startFind}', cannot highlight with '{backColor.Name}'.");
            else {
                richTextBox.SelectionStart = selectionStart;
                richTextBox.SelectionLength = findText.Length;
                richTextBox.SelectionBackColor = backColor;
            }
        }

        private static void SQLStart() {
            // TODO:  Eventually; SQL Server Express: SQLStart.
        }

        private static void SQLStop() {
            // TODO:  Eventually; SQL Server Express: SQLStop.
        }
        #endregion Private
    }
}