﻿#undef VERBOSE
using System;
using System.DirectoryServices.AccountManagement;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Serilog; // Install Serilog via NuGet Package Manager.  Site is https://serilog.net/.
using ABT.TestSpace.TestExec.AppConfig;

// TODO:  Eventually; persist measurement data into Microsoft SQL Server Express; write all full Operation TestMeasurement results therein.
// - Stop writing TestMeasurement results to RichTextBoxSink when testing full Operations; only write TestGroups results to RichTextBoxSink.
// - Continue writing TestMeasurement results to RichTextBoxSink when only testing Groups.
// - Stop saving RichTextBoxSink as RTF files, except allow manual export for Troubleshooting.
// - This will resolve the RichTextBox scroll issue, wherein TestGroups results are scrolled up & away as TestMeasurements are appended.
// - Only SQL Server Express persisted measurement data is legitimate; all RichTextBoxSink is Troubleshooting only.
// - Create a Microsoft C# front-end exporting/reporting app for persisted SQL Server Express TestMeasurement full Operation measurement data.
// - Export in CSV, report in PDF.
//

namespace ABT.TestSpace.TestExec.Logging {
    public static class Logger {
        public const String LOGGER_TEMPLATE = "{Message}{NewLine}";
        public const String SPACES_21 = "                     ";
        private const String MESSAGE_STOP = "STOP              : ";
        private const String MESSAGE_UUT_RESULT = "Result            : ";

        #region Public Methods
        public static String FormatMessage(String Label, String Message) { return $"  {Label}".PadRight(SPACES_21.Length) + $" : {Message}"; }

        public static String FormatNumeric(MeasurementNumeric MN, Double Value) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(FormatMessage("High Limit", $"{MN.High:G}"));
            sb.AppendLine(FormatMessage("Measured", $"{Value:G}"));
            sb.AppendLine(FormatMessage("Low Limit", $"{MN.Low:G}"));
            String si_units = $"{Enum.GetName(typeof(SI_UNITS), MN.SI_Units)}";
            if (MN.SI_Units_Modifier != SI_UNITS_MODIFIER.NotApplicable) si_units += $" {Enum.GetName(typeof(SI_UNITS_MODIFIER), MN.SI_Units_Modifier)}";
            sb.Append(FormatMessage("SI Units", si_units));
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

        public static void LogError(String logMessage) { Log.Error(logMessage); }

        public static void LogMessage(String Message) { Log.Information(Message); }

        public static void LogTest(Boolean isOperation, Measurement measurement, ref RichTextBox rtfResults) {
            StringBuilder message = new StringBuilder();
            message.AppendLine(FormatMessage("TestMeasurement ID", measurement.ID));
#if VERBOSE
            message.AppendLine(FormatMessage("Revision", measurement.Revision));
            message.AppendLine(FormatMessage("Measurement Type", measurement.ClassName));
            message.AppendLine(FormatMessage("Cancel Not Passed", measurement.CancelNotPassed.ToString()));
#endif
            message.AppendLine(FormatMessage("Description", measurement.Description));
            switch (measurement.ClassName) {
                case MeasurementCustom.ClassName:
                    message.AppendLine(measurement.Value);
                    break;
                case MeasurementNumeric.ClassName:
                    message.AppendLine(FormatNumeric((MeasurementNumeric)measurement.ClassObject, Double.Parse(measurement.Value)));
                    break;
                case MeasurementProcess.ClassName:
                    message.AppendLine(FormatProcess((MeasurementProcess)measurement.ClassObject, measurement.Value));
                    break;
                case MeasurementTextual.ClassName:
                    message.AppendLine(FormatTextual((MeasurementTextual)measurement.ClassObject, measurement.Value));
                    break;
                default:
                    throw new NotImplementedException($"TestMeasurement ID '{measurement.ID}' with ClassName '{measurement.ClassName}' not implemented.");
            }
            message.AppendLine(FormatMessage("Result", measurement.Result));
            if (!String.Equals(measurement.Message, String.Empty)) message.Append(measurement.Message);
            Log.Information(message.ToString());
            if (isOperation) SetBackColor(ref rtfResults, 0, measurement.ID, EventCodes.GetColor(measurement.Result));
        }

        public static void Start(TestExecutive testExecutive, ref RichTextBox rtfResults) {
            if (!testExecutive.ConfigTest.IsOperation) {
                // When TestGroups are executed, measurement data is never saved as Rich Text.
                // RichTextBox only. 
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
                Log.Information($"Note: following measurement results invalid for UUT production testing, only troubleshooting.");
                Log.Information(FormatMessage($"UUT Serial Number", $"{testExecutive.ConfigUUT.SerialNumber}"));
                Log.Information(FormatMessage($"UUT Number", $"{testExecutive.ConfigUUT.Number}"));
                Log.Information(FormatMessage($"UUT Revision", $"{testExecutive.ConfigUUT.Revision}"));
                Log.Information(FormatMessage($"TestGroup ID", $"{testExecutive.ConfigTest.TestElementID}"));
                Log.Information(FormatMessage($"Description", $"{testExecutive.ConfigTest.TestElementDescription}"));
                Log.Information(FormatMessage($"START", $"{DateTime.Now}\n"));
                return;
                // Log Header isn't written to Console when TestGroups are executed, further emphasizing measurement results are invalid for pass verdict/$hip disposition, only troubleshooting failures.
            }

            if (testExecutive.ConfigLogger.FileEnabled && !testExecutive.ConfigLogger.SQLEnabled) {
                // When TestOperations are executed, measurement data is always & automatically saved as Rich Text.
                // RichTextBox + File.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            } else if (!testExecutive.ConfigLogger.FileEnabled && testExecutive.ConfigLogger.SQLEnabled) {
                // TODO:  Eventually; RichTextBox + SQL.
                SQLStart(testExecutive);
            } else if (testExecutive.ConfigLogger.FileEnabled && testExecutive.ConfigLogger.SQLEnabled) {
                // TODO:  Eventually; RichTextBox + File + SQL.
                SQLStart(testExecutive);
            } else {
                // RichTextBox only; customer doesn't require saved measurement data, unusual for Functional testing, but potentially not for other testing methodologies.
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                    .CreateLogger();
            }
            Log.Information($"UUT:");
            Log.Information($"\t{MESSAGE_UUT_RESULT}");
            Log.Information($"\tSerial Number     : {testExecutive.ConfigUUT.SerialNumber}");
            Log.Information($"\tNumber            : {testExecutive.ConfigUUT.Number}");
            Log.Information($"\tRevision          : {testExecutive.ConfigUUT.Revision}");
            Log.Information($"\tDescription       : {testExecutive.ConfigUUT.Description}");
            Log.Information($"\tType              : {testExecutive.ConfigUUT.Type}");
            Log.Information($"\tCustomer          : {testExecutive.ConfigUUT.Customer}\n");

            Log.Information($"TestOperation:");
            Log.Information($"\tSTART             : {DateTime.Now}");
            Log.Information($"\t{MESSAGE_STOP}");
            Log.Information($"\tUserPrincipal     : {UserPrincipal.Current.DisplayName}"); // NOTE:  UserPrincipal.Current.DisplayName requires a connected/active Domain session for Active Directory PCs.
            Log.Information($"\tMachineName       : {Environment.MachineName}");
            Log.Information($"\tTestExecutive     : {Assembly.GetExecutingAssembly().GetName().Name}, {Assembly.GetExecutingAssembly().GetName().Version}");
            Log.Information($"\tTestExecutor      : {Assembly.GetEntryAssembly().GetName().Name}, {Assembly.GetEntryAssembly().GetName().Version}");
            Log.Information($"\tSpecification     : {testExecutive.ConfigUUT.TestSpecification}");
            Log.Information($"\tID                : {testExecutive.ConfigTest.TestElementID}");
            Log.Information($"\tRevision          : {testExecutive.ConfigTest.TestElementRevision}");
            Log.Information($"\tDescription       : {testExecutive.ConfigTest.TestElementDescription}\n");

            StringBuilder sb = new StringBuilder();
            foreach (String groupID in testExecutive.ConfigTest.GroupIDsSequence) {
                sb.Append(String.Format("\t{0,-" + testExecutive.ConfigTest.FormattingLengthGroupID + "} : {1}\n", groupID, testExecutive.ConfigTest.Groups[groupID].Description));
                foreach (String measurementID in testExecutive.ConfigTest.GroupIDsToMeasurementIDs[groupID]) sb.Append(String.Format("\t\t{0,-" + testExecutive.ConfigTest.FormattingLengthMeasurementID + "} : {1}\n", measurementID, testExecutive.ConfigTest.Measurements[measurementID].Description));
            }
            Log.Information($"TestMeasurements:\n{sb}");
        }

        public static void Stop(TestExecutive testExecutive, ref RichTextBox rtfResults) {
            if (!testExecutive.ConfigTest.IsOperation) Log.CloseAndFlush();
            // Log Trailer isn't written when not a TestOperation, further emphasizing measurement results aren't valid for passing & $hipping, only troubleshooting failures.
            else {
                ReplaceText(ref rtfResults, 0, MESSAGE_UUT_RESULT, MESSAGE_UUT_RESULT + testExecutive.ConfigUUT.EventCode);
                SetBackColor(ref rtfResults, 0, testExecutive.ConfigUUT.EventCode, EventCodes.GetColor(testExecutive.ConfigUUT.EventCode));
                ReplaceText(ref rtfResults, 0, MESSAGE_STOP, MESSAGE_STOP + DateTime.Now);               
                Log.CloseAndFlush();
                if (testExecutive.ConfigLogger.FileEnabled) FileStop(testExecutive, ref rtfResults);
                if (testExecutive.ConfigLogger.SQLEnabled) SQLStop(testExecutive);
                if (testExecutive.ConfigLogger.TestEventsEnabled) TestEvents(testExecutive.ConfigUUT);
            }
        }
        #endregion Public Methods

        #region Non-Public Methods
        internal static String GetFilePath(TestExecutive testExecutive) { return $"{testExecutive.ConfigLogger.FilePath}{testExecutive.ConfigTest.TestElementID}\\"; }

        private static void FileStop(TestExecutive testExecutive, ref RichTextBox rtfResults) {
            String fileName = $"{testExecutive.ConfigUUT.Number}_{testExecutive.ConfigUUT.SerialNumber}_{testExecutive.ConfigTest.TestElementID}";
            String[] files = Directory.GetFiles(GetFilePath(testExecutive), $"{fileName}_*.rtf", SearchOption.TopDirectoryOnly);
            // Will fail if invalid path.  Don't catch resulting Exception though; this has to be fixed in App.config.
            // Otherwise, files is the set of all files like config.configUUT.Number_Config.configUUT.SerialNumber_configTest.TestElementID_*.rtf.
            Int32 maxNumber = 0; String s;
            foreach (String f in files) {
                s = f;
                s = s.Replace($"{GetFilePath(testExecutive)}{fileName}", String.Empty);
                s = s.Replace(".rtf", String.Empty);
                s = s.Replace("_", String.Empty);
                foreach (FieldInfo fi in typeof(EventCodes).GetFields()) s = s.Replace((String)fi.GetValue(null), String.Empty);
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
            fileName += $"_{++maxNumber}_{testExecutive.ConfigUUT.EventCode}.rtf";
            rtfResults.SaveFile($"{GetFilePath(testExecutive)}{fileName}");
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

        private static void SQLStart(TestExecutive testExecutive) {
            // TODO:  Eventually; SQL Server Express: SQLStart.
        }

        private static void SQLStop(TestExecutive testExecutive) {
            // TODO:  Eventually; SQL Server Express: SQLStop.
        }

        private static void TestEvents(AppConfigUUT uut) {
            String eventCode;
            switch (uut.EventCode) {
                case EventCodes.CANCEL:
                    eventCode = "A";
                    break;
                case EventCodes.ERROR:
                    eventCode = "E";
                    break;
                case EventCodes.FAIL:
                    eventCode = "F";
                    break;
                case EventCodes.PASS:
                    eventCode = "P";
                    break;
                case EventCodes.UNSET:
                    eventCode = "U";
                    break;
                default:
                    throw new NotImplementedException($"Unrecognized EventCode '{uut.EventCode}'.");
            }
            // TODO:  Eventually; invoke TestEvents with $"{uut.Number} {uut.SerialNumber} {uut.eventCode}";
        }
        #endregion Non-Public
    }
}