﻿#undef VERBOSE
using System;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Serilog; // Install Serilog via NuGet Package Manager.  Site is https://serilog.net/.
using ABT.Test.Exec.AppConfig;
using System.Diagnostics;

// TODO:  Eventually; persist measurement data into Microsoft SQL Server Express; write all full Operation TestMeasurement output therein.
// - Stop writing TestMeasurement output to RichTextBoxSink when testing full Operations; only write TestGroups output to RichTextBoxSink.
// - Continue writing TestMeasurement output to RichTextBoxSink when only testing Groups.
// - Stop saving RichTextBoxSink as RTF files, except allow manual export for Troubleshooting.
// - This will resolve the RichTextBox scroll issue, wherein TestGroups output are scrolled up & away as TestMeasurements are appended.
// - Only SQL Server Express persisted measurement data is legitimate; all RichTextBoxSink is Troubleshooting only.
// - Create a Microsoft C# front-end exporting/reporting app for persisted SQL Server Express TestMeasurement full Operation measurement data.
// - Export in CSV, report in PDF.
//

namespace ABT.Test.Exec.Logging {
    public static class Logger {
        public const String LOGGER_TEMPLATE = "{Message}{NewLine}";
        public const String SPACES_21 = "                     ";
        private const String MESSAGE_STOP = "Stop              : ";
        private const String MESSAGE_TEST_EVENT = "Test Event";
        private const String MESSAGE_UUT_EVENT = MESSAGE_TEST_EVENT + "        : ";

        #region Public Methods
        public static String FormatMessage(String Label, String Message) { return $"  {Label}".PadRight(SPACES_21.Length) + $" : {Message}"; }

        public static String FormatNumeric(MeasurementNumeric MN, Double Value) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("High Limit", $"{MN.High:G}"));
            sb.AppendLine(FormatMessage("Measured", $"{Value:G}"));
            sb.AppendLine(FormatMessage("Low Limit", $"{MN.Low:G}"));
            String units_si = $"{Enum.GetName(typeof(UNITS_SI), MN.Units_SI)}";
            if (MN.Units_SI_Modifier != UNITS_SI_MODIFIER.NotApplicable) units_si += $" {Enum.GetName(typeof(UNITS_SI_MODIFIER), MN.Units_SI_Modifier)}";
            sb.Append(FormatMessage("SI Units", units_si));
            return sb.ToString();
        }

        public static String FormatProcess(MeasurementProcess MP, String Value) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("Expected", MP.ProcessExpected));
            sb.Append(FormatMessage("Actual", Value));
            return sb.ToString();
        }

        public static String FormatTextual(MeasurementTextual MT, String Value) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("Expected", MT.Text));
            sb.Append(FormatMessage("Actual", Value));
            return sb.ToString();
        }
        #endregion Public Methods

        #region Internal Methods
        internal static void LogError(String logMessage) { Log.Error(logMessage); }

        internal static void LogMessage(String Message) { Log.Information(Message); }

        internal static void LogTest(Boolean isOperation, Measurement measurement, ref RichTextBox rtfResults) {
            StringBuilder message = new StringBuilder();
            message.AppendLine(FormatMessage("TestMeasurement ID", measurement.ID));
#if VERBOSE
            message.AppendLine(FormatMessage("Revision", measurement.Revision));
            message.AppendLine(FormatMessage("Measurement Type", measurement.ClassObject.GetType().Name));
            message.AppendLine(FormatMessage("Cancel Not Passed", measurement.CancelNotPassed.ToString()));
#endif
            message.AppendLine(FormatMessage("Description", measurement.Description));
            switch (measurement.ClassObject) {
                case MeasurementCustom _:
                    message.AppendLine(measurement.Value);
                    break;
                case MeasurementNumeric _:
                    message.AppendLine(FormatNumeric((MeasurementNumeric)measurement.ClassObject, Double.Parse(measurement.Value)));
                    break;
                case MeasurementProcess _:
                    message.AppendLine(FormatProcess((MeasurementProcess)measurement.ClassObject, measurement.Value));
                    break;
                case MeasurementTextual _:
                    message.AppendLine(FormatTextual((MeasurementTextual)measurement.ClassObject, measurement.Value));
                    break;
                default:
                    throw new NotImplementedException($"TestMeasurement ID '{measurement.ID}' with ClassName '{measurement.ClassObject.GetType().Name}' not implemented.");
            }
            message.AppendLine(FormatMessage(MESSAGE_TEST_EVENT, measurement.TestEvent));
            message.Append(measurement.Message.ToString());
            Log.Information(message.ToString());
            if (isOperation) SetBackColor(ref rtfResults, 0, measurement.ID, TestEvents.GetColor(measurement.TestEvent));
        }

        internal static void Start(TestExec testExec, ref RichTextBox rtfResults) {
            if (!testExec.ConfigTest.IsOperation) {
                // When TestGroups are executed, measurement data is never saved as Rich Text.
                // RichTextBox only. 
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
                Log.Information($"Note: following measurement results invalid for UUT production testing, only troubleshooting.");
                Log.Information(FormatMessage($"UUT Serial Number", $"{TestExec.ConfigUUT.SerialNumber}"));
                Log.Information(FormatMessage($"UUT Number", $"{TestExec.ConfigUUT.Number}"));
                Log.Information(FormatMessage($"UUT Revision", $"{TestExec.ConfigUUT.Revision}"));
                Log.Information(FormatMessage($"TestGroup ID", $"{testExec.ConfigTest.TestElementID}"));
                Log.Information(FormatMessage($"Description", $"{testExec.ConfigTest.TestElementDescription}"));
                Log.Information(FormatMessage($"Start", $"{DateTime.Now}\n"));
                return;
                // Log Header isn't written to Console when TestGroups are executed, further emphasizing measurements are invalid for pass verdict/$hip disposition, only troubleshooting failures.
            }

            if (testExec.ConfigLogger.FileEnabled && !testExec.ConfigLogger.SQLEnabled) {
                // When TestOperations are executed, measurement data is always & automatically saved as Rich Text.
                // RichTextBox + File.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            } else if (!testExec.ConfigLogger.FileEnabled && testExec.ConfigLogger.SQLEnabled) {
                // TODO:  Eventually; RichTextBox + SQL.
                SQLStart(testExec);
            } else if (testExec.ConfigLogger.FileEnabled && testExec.ConfigLogger.SQLEnabled) {
                // TODO:  Eventually; RichTextBox + File + SQL.
                SQLStart(testExec);
            } else {
                // RichTextBox only; customer doesn't require saved measurement data, unusual for Functional testing, but potentially not for other testing methodologies.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            }
            Log.Information($"UUT:");
            Log.Information($"\t{MESSAGE_UUT_EVENT}");
            Log.Information($"\tSerial Number     : {TestExec.ConfigUUT.SerialNumber}");
            Log.Information($"\tNumber            : {TestExec.ConfigUUT.Number}");
            Log.Information($"\tRevision          : {TestExec.ConfigUUT.Revision}");
            Log.Information($"\tDescription       : {TestExec.ConfigUUT.Description}");
            Log.Information($"\tType              : {TestExec.ConfigUUT.Type}");
            Log.Information($"\tCustomer          : {TestExec.ConfigUUT.Customer}\n");

            Log.Information($"TestOperation:");
            Log.Information($"\tStart             : {DateTime.Now}");
            Log.Information($"\t{MESSAGE_STOP}");
            Log.Information($"\tUserPrincipal     : {UserPrincipal.Current.DisplayName}");
            // NOTE:  UserPrincipal.Current.DisplayName requires a connected/active Domain session for Active Directory PCs.
            Log.Information($"\tMachineName       : {Environment.MachineName}");
            Log.Information($"\tTestExec          : {Assembly.GetExecutingAssembly().GetName().Name}, {Assembly.GetExecutingAssembly().GetName().Version}, {BuildDate(Assembly.GetExecutingAssembly().GetName().Version)}");
            Log.Information($"\tTestPlan          : {Assembly.GetEntryAssembly().GetName().Name}, {Assembly.GetEntryAssembly().GetName().Version} {BuildDate(Assembly.GetEntryAssembly().GetName().Version)}");
            Log.Information($"\tSpecification     : {TestExec.ConfigUUT.TestSpecification}");
            Log.Information($"\tID                : {testExec.ConfigTest.TestElementID}");
#if VERBOSE
            Log.Information($"\tRevision          : {testExec.ConfigTest.TestElementRevision}");
#endif
            Log.Information($"\tDescription       : {testExec.ConfigTest.TestElementDescription}\n");

            StringBuilder sb = new StringBuilder();
            foreach (String groupID in testExec.ConfigTest.GroupIDsSequence) {
                sb.Append(String.Format("\t{0,-" + testExec.ConfigTest.FormattingLengthGroupID + "} : {1}\n", groupID, testExec.ConfigTest.Groups[groupID].Description));
                foreach (String measurementID in testExec.ConfigTest.GroupIDsToMeasurementIDs[groupID]) sb.Append(String.Format("\t\t{0,-" + testExec.ConfigTest.FormattingLengthMeasurementID + "} : {1}\n", measurementID, testExec.ConfigTest.Measurements[measurementID].Description));
            }
            Log.Information($"TestMeasurements:\n{sb}");
        }

        internal static String BuildDate(Version version) { 
            DateTime Y2K = new DateTime(year: 2000, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Local);
            return $"{Y2K + new TimeSpan(days: version.Build, hours: 0, minutes: 0, seconds: 2 * version.Revision):g}";
        }

        internal static void Stop(TestExec testExec, ref RichTextBox rtfResults) {
            if (!testExec.ConfigTest.IsOperation) Log.CloseAndFlush();
            // Log Trailer isn't written when not a TestOperation, further emphasizing measurement results aren't valid for passing & $hipping, only troubleshooting failures.
            else {
                ReplaceText(ref rtfResults, 0, MESSAGE_UUT_EVENT, MESSAGE_UUT_EVENT + TestExec.ConfigUUT.TestEvent);
                SetBackColor(ref rtfResults, 0, TestExec.ConfigUUT.TestEvent, TestEvents.GetColor(TestExec.ConfigUUT.TestEvent));
                ReplaceText(ref rtfResults, 0, MESSAGE_STOP, MESSAGE_STOP + DateTime.Now);               
                Log.CloseAndFlush();
                if (testExec.ConfigLogger.FileEnabled) FileStop(testExec, ref rtfResults);
                if (testExec.ConfigLogger.SQLEnabled) SQLStop(testExec);
            }
        }
#endregion Internal Methods

        #region Private Methods
        private static void FileStop(TestExec testExec, ref RichTextBox rtfResults) {
            String fileName = $"{TestExec.ConfigUUT.Number}_{TestExec.ConfigUUT.SerialNumber}_{testExec.ConfigTest.TestElementID}";
            String[] files = Directory.GetFiles(GetFilePath(testExec), $"{fileName}_*.rtf", SearchOption.TopDirectoryOnly);
            // Will fail if invalid path.  Don't catch resulting Exception though; this has to be fixed in App.config.
            // Otherwise, files is the set of all files like config.configUUT.Number_Config.configUUT.SerialNumber_configTest.TestElementID_*.rtf.
            Int32 maxNumber = 0; String s;
            foreach (String f in files) {
                s = f;
                s = s.Replace($"{GetFilePath(testExec)}{fileName}", String.Empty);
                s = s.Replace(".rtf", String.Empty);
                s = s.Replace("_", String.Empty);
                foreach (FieldInfo fi in typeof(TestEvents).GetFields()) s = s.Replace((String)fi.GetValue(null), String.Empty);
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
            fileName += $"_{++maxNumber}_{TestExec.ConfigUUT.TestEvent}.rtf";
            rtfResults.SaveFile($"{GetFilePath(testExec)}{fileName}");
        }
        
        private static String GetFilePath(TestExec testExec) { return $"{testExec.ConfigLogger.FilePath}{testExec.ConfigTest.TestElementID}\\"; }

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

        private static void SQLStart(TestExec testExec) {
            // TODO:  Eventually; SQL Server Express: SQLStart.
        }

        private static void SQLStop(TestExec testExec) {
            // TODO:  Eventually; SQL Server Express: SQLStop.
        }
        #endregion Private
    }
}