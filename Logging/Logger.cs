#undef VERBOSE
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using Serilog; // Install Serilog via NuGet Package Manager.  Site is https://serilog.net/.
using ABT.Test.TestLib;
using ABT.Test.TestLib.TestConfiguration;
using static ABT.Test.TestLib.Data;

// TODO:  Eventually; persist test data into PostgreSQL on IS server:
// Only XML and PostgreSQL persisted test data is legitimate.
//   - RichTextBoxSink test data are failures only, for trouble-shooting.
//     - Can be exported as RTF if desired, then printed.
//
// Local PostgreSQL installations on all TestExec PCs in case LAN, IS Server or PostgreSQL server are down.
//   - Local PostgreSQL exports test data periodically via Microsoft Task Scheduler to main PostgreSQL RBDMS, then deletes its local data.
//
// Create Microsoft Access app for querying & reporting PostgreSQL RBDMS:
//   - PostgreSQL test data accessible as:
//     - Read-only for all ABT personnel.
//     - Read-write for Test Engineers & Technicians.
//
// PostgreSQL can integrate with Microsoft Active Directory for user access.
//   - Include standard queries & reports.
//     - Queries exportable as .CSV files for Excel.
//     - Reports exportable solely as .PDF files for Acrobat Reader.
//   - Custom querying/reporting permitted & encouraged.

namespace ABT.Test.TestExec.Logging {
    public static class Logger {
        public const String LOGGER_TEMPLATE = "{Message}{NewLine}";
        public const String SPACES_2 = "  ";
        public const String SPACES_21 = "                     ";
        private const String MESSAGE_TEST_EVENT = "Test Event";
        private const String MESSAGE_UUT_EVENT = MESSAGE_TEST_EVENT + "        : ";

        #region Public Methods
        public static String FormatMessage(String Label, String Message) { return $"{SPACES_2}{Label}".PadRight(SPACES_21.Length) + $" : {Message}"; }

        public static StringBuilder FormatNumeric(MethodInterval methodInterval) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage("High Limit", $"{methodInterval.High:G}"));
            stringBuilder.AppendLine(FormatMessage("Measured", $"{Math.Round(Double.Parse((String)methodInterval.Value), (Int32)methodInterval.FractionalDigits, MidpointRounding.ToEven)}"));
            stringBuilder.AppendLine(FormatMessage("Low Limit", $"{methodInterval.Low:G}"));
            String units = String.Empty;
            if (methodInterval.UnitPrefix != MI_UnitPrefix.NONE) units += $"{Enum.GetName(typeof(MI_UnitPrefix), methodInterval.UnitPrefix)}";
            units += $"{Enum.GetName(typeof(MI_Units), methodInterval.Units)}";
            if (methodInterval.UnitSuffix != MI_UnitSuffix.NONE) units += $" {Enum.GetName(typeof(MI_UnitSuffix), methodInterval.UnitSuffix)}";
            stringBuilder.Append(FormatMessage("Units", units));
            return stringBuilder;
        }

        public static StringBuilder FormatProcess(MethodProcess methodProcess) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage("Expected", methodProcess.Expected));
            stringBuilder.Append(FormatMessage("Actual", (String)methodProcess.Value));
            return stringBuilder;
        }

        public static StringBuilder FormatTextual(MethodTextual methodTextual) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage("Expected", methodTextual.Text));
            stringBuilder.AppendLine(FormatMessage("Actual", (String)methodTextual.Value));
            return stringBuilder;
        }

        public static void LogError(String logMessage) { Log.Error(logMessage); }

        public static void LogMessageAppend(String Message) { Log.Information(Message); }

        public static void LogMessageAppendLine(String Message) { Log.Information($"{Message}{Environment.NewLine}"); }

        public static void LogMethod(ref RichTextBox rtfResults, Method method) {
            SetBackColor(ref rtfResults, 0, method.Name, EventColors[method.Event]);
            if (method.Event is EVENTS.PASS) return;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage("Method", method.Name));
            stringBuilder.AppendLine(FormatMessage("Cancel Not Passed", method.CancelNotPassed.ToString()));
            stringBuilder.AppendLine(FormatMessage("Description", method.Description));

            if (method is MethodCustom) { } // NOTE: Call Method.LogMessage() from Tests project to log desire Method detail.
            else if (method is MethodInterval methodInterval) stringBuilder.Append(FormatNumeric(methodInterval));
            else if (method is MethodProcess methodProcess) stringBuilder.Append(FormatProcess(methodProcess));
            else if (method is MethodTextual methodTextual) stringBuilder.Append(FormatTextual(methodTextual));
            else throw new NotImplementedException($"Method '{method.Name}', description '{method.Description}', with classname '{nameof(method)}' not implemented.");
            stringBuilder.AppendLine(FormatMessage(MESSAGE_TEST_EVENT, method.Event.ToString()));
            stringBuilder.Append($"{SPACES_2}{method.Log}");
            Log.Information(stringBuilder.ToString());
        }

        public static void Start(ref RichTextBox rtfResults) {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                .CreateLogger();

            Log.Information($"UUT:");
            Log.Information($"\t{MESSAGE_UUT_EVENT}");
            Log.Information($"\tSerial Number     : {testSequence.SerialNumber}");
            Log.Information($"\tNumber            : {testSequence.UUT.Number}");
            Log.Information($"\tRevision          : {testSequence.UUT.Revision}");
            Log.Information($"\tDescription       : {testSequence.UUT.Description}");
            Log.Information($"\tType              : {testSequence.UUT.Category}");
            Log.Information($"\tCustomer          : {testSequence.UUT.Customer.Name}\n");

            StringBuilder stringBuilder = new StringBuilder();
            foreach (TestGroup testGroup in testSequence.TestOperation.TestGroups) {
                stringBuilder.Append(String.Format("\t{0,-" + testGroup.FormattingLengthGroupID + "} : {1}\n", testGroup.Classname, testGroup.Description));
                foreach (Method method in testGroup.Methods) stringBuilder.Append(String.Format("\t\t{0,-" + testGroup.FormattingLengthMethodID + "} : {1}\n", method.Name, method.Description));
            }
            Log.Information($"TestMethods:\n{stringBuilder}");
        }

        public static void Stop(ref RichTextBox rtfResults) {
            ReplaceText(ref rtfResults, 0, MESSAGE_UUT_EVENT, MESSAGE_UUT_EVENT + testSequence.Event.ToString());
            SetBackColor(ref rtfResults, 0, testSequence.Event.ToString(), EventColors[testSequence.Event]);
            Log.CloseAndFlush();
            if (testSequence.IsOperation) {
                if (testDefinition.TestData.Item is XML) StopXML();
                else if (testDefinition.TestData.Item is SQL) StopSQL();
                else throw new ArgumentException($"Unknown TestData Item '{testDefinition.TestData.Item}'.");
            }
        }
        #endregion Public Methods

        #region Private Methods
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

        private static void StopSQL() {
            // TODO:  Eventually; PostgreSQL.
        }

        private static void StopXML() {
            XML xml = (XML)testDefinition.TestData.Item;
            String xmlFolder = $"{xml.Folder}\\{testSequence.TestOperation.NamespaceTrunk}";
            String xmlBaseName = $"{testSequence.UUT.Number}_{testSequence.SerialNumber}_{testSequence.TestOperation.NamespaceTrunk}";
            String[] xmlFileNames = Directory.GetFiles(xmlFolder, $"{xmlBaseName}_*.xml", SearchOption.TopDirectoryOnly);
            // NOTE:  Will fail if invalid path.  Don't catch resulting Exception though; this has to be fixed in TestDefinitionXML.
            Int32 maxNumber = 0; String s;
            foreach (String xmlFileName in xmlFileNames) {
                s = xmlFileName;
                foreach (EVENTS Event in Enum.GetValues(typeof(EVENTS))) s = s.Replace(Event.ToString(), String.Empty);
                s = s.Replace($"{xmlFolder}\\{xmlBaseName}", String.Empty);
                s = s.Replace(".xml", String.Empty);
                s = s.Replace("_", String.Empty);

                if (Int32.Parse(s) > maxNumber) maxNumber = Int32.Parse(s);
            }

            using (FileStream fileStream = new FileStream($"{xmlFolder}\\{xmlBaseName}_{++maxNumber}_{testSequence.Event}.xml", FileMode.CreateNew)) {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(fileStream, new UTF8Encoding(true))) {
                    xmlTextWriter.Formatting = Formatting.Indented;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(TestSequence));
                    xmlSerializer.Serialize(xmlTextWriter, testSequence);
                }
            }
        }
        #endregion Private
    }
}