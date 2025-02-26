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
using System.Data.SqlClient;

// TODO:  Eventually; persist test data into Microsoft SQL Server Standard on IS server:
// Only XML and SQL Server persisted test data is legitimate.
//   - RichTextBoxSink test data are failures only, for trouble-shooting.
//     - Can be exported as RTF if desired, then printed.
//
// Local SQL Server Express installations on all TestExec PCs in case LAN, IS Server or SQL Server Standard are down.
//   - Local SQL Server Express  exports test data periodically via Microsoft Task Scheduler to main SQL Server Standard RBDMS, then deletes its local data.
//
// Create Microsoft Access app for querying & reporting SQL Server Standard RBDMS:
//   - SQL Server Standard test data accessible as:
//     - Read-only for all ABT personnel.
//     - Read-write for Test Engineers & Technicians.
//
// SQL Server Standard can integrate with Microsoft Active Directory for user access.
//   - Include standard queries & reports.
//     - Queries exportable as .CSV files for Excel.
//     - Reports exportable solely as .PDF files for Acrobat Reader.
//   - Custom querying/reporting permitted & encouraged.

namespace ABT.Test.TestExec.Logging {
    public static class Logger {
        public const String LOGGER_TEMPLATE = "{Message}{NewLine}";
        public const String SPACES_2 = "  ";
        public const String SPACES_4 = SPACES_2 + SPACES_2; // Embedded tabs in strings (\t) seem to cause method ReplaceText() issues.
        private const String MESSAGE_TEST_EVENT = "Test Event";
        private const Int32 PR = 21;
        private static readonly String MESSAGE_UUT_EVENT = (SPACES_2 + MESSAGE_TEST_EVENT).PadRight(PR) + ": ";
        private const String EXPECTED = "Expected";
        private const String ACTUAL = "Actual";

        #region Public Methods
        public static String FormatMessage(String Label, String Message) { return $"{SPACES_2}{Label}".PadRight(PR) + $": {Message}"; }

        public static StringBuilder FormatNumeric(MethodInterval methodInterval) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage(nameof(MethodInterval.High), $"{methodInterval.High:G}"));
            stringBuilder.AppendLine(FormatMessage(nameof(MethodInterval.Value), $"{Math.Round(Double.Parse(methodInterval.Value), (Int32)methodInterval.FractionalDigits, MidpointRounding.ToEven)}"));
            stringBuilder.AppendLine(FormatMessage(nameof(MethodInterval.Low), $"{methodInterval.Low:G}"));
            String units = String.Empty;
            if (methodInterval.UnitPrefix != MI_UnitPrefix.NONE) units += $"{Enum.GetName(typeof(MI_UnitPrefix), methodInterval.UnitPrefix)}";
            units += $"{Enum.GetName(typeof(MI_Units), methodInterval.Units)}";
            if (methodInterval.UnitSuffix != MI_UnitSuffix.NONE) units += $" {Enum.GetName(typeof(MI_UnitSuffix), methodInterval.UnitSuffix)}";
            stringBuilder.AppendLine(FormatMessage(nameof(MethodInterval.Units), units));
            return stringBuilder;
        }

        public static StringBuilder FormatProcess(MethodProcess methodProcess) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage(EXPECTED, methodProcess.Expected));
            stringBuilder.AppendLine(FormatMessage(ACTUAL, methodProcess.Value));
            return stringBuilder;
        }

        public static StringBuilder FormatTextual(MethodTextual methodTextual) {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage(EXPECTED, methodTextual.Text));
            stringBuilder.AppendLine(FormatMessage(ACTUAL, methodTextual.Value));
            return stringBuilder;
        }

        public static void LogError(String logMessage) { Log.Error(logMessage); }

        public static void LogMessageAppend(String Message) { Log.Information(Message); }

        public static void LogMessageAppendLine(String Message) { Log.Information($"{Message}{Environment.NewLine}"); }

        public static void LogMethod(ref RichTextBox rtfResults, Method method) {
            SetBackColor(ref rtfResults, 0, method.Name, EventColors[method.Event]);
            if (method.Event is EVENTS.PASS) return;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(FormatMessage($"{nameof(Method)}", method.Name));
            stringBuilder.AppendLine(FormatMessage($"{nameof(Method.CancelNotPassed)}", method.CancelNotPassed.ToString()));
            stringBuilder.AppendLine(FormatMessage($"{nameof(Method.Description)}", method.Description));

            if (method is MethodCustom) { } // NOTE: Call Method.LogMessage() from Tests project to log desire Method detail.
            else if (method is MethodInterval methodInterval) stringBuilder.Append(FormatNumeric(methodInterval));
            else if (method is MethodProcess methodProcess) stringBuilder.Append(FormatProcess(methodProcess));
            else if (method is MethodTextual methodTextual) stringBuilder.Append(FormatTextual(methodTextual));
            else throw new NotImplementedException($"{nameof(Method)} '{method.Name}', {nameof(Method.Description)} '{method.Description}', of type '{nameof(method)}' not implemented.");
            stringBuilder.AppendLine(FormatMessage(MESSAGE_TEST_EVENT, method.Event.ToString()));
            stringBuilder.Append($"{SPACES_2}{method.Log}");
            Log.Information(stringBuilder.ToString());
        }

        public static void Start(ref RichTextBox rtfResults) {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Sink(new RichTextBoxSink(richTextBox: ref rtfResults, outputTemplate: LOGGER_TEMPLATE))
                .CreateLogger();

            Log.Information($"{nameof(UUT)}:");
            Log.Information($"{MESSAGE_UUT_EVENT}");
            Log.Information($"{SPACES_2}{nameof(TestSequence.SerialNumber)}".PadRight(PR) + $": {testSequence.SerialNumber}");
            Log.Information($"{SPACES_2}{nameof(UUT.Number)}".PadRight(PR) + $": {testSequence.UUT.Number}");
            Log.Information($"{SPACES_2}{nameof(UUT.Revision)}".PadRight(PR) + $": {testSequence.UUT.Revision}");
            Log.Information($"{SPACES_2}{nameof(UUT.Description)}".PadRight(PR) + $": {testSequence.UUT.Description}");
            Log.Information($"{SPACES_2}{nameof(UUT.Category)}".PadRight(PR) + $": {testSequence.UUT.Category}");
            Log.Information($"{SPACES_2}{nameof(UUT.Customer)}".PadRight(PR) + $": {testSequence.UUT.Customer.Name}\n");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{nameof(TestGroup.Methods)}:");
            foreach (TestGroup testGroup in testSequence.TestOperation.TestGroups) {
                stringBuilder.AppendLine($"{SPACES_2}{testGroup.Classname}, {testGroup.Description}");
                foreach (Method method in testGroup.Methods) stringBuilder.AppendLine($"{SPACES_4}{method.Name}".PadRight(PR + SPACES_4.Length) + $": {method.Description}");
            }
            Log.Information(stringBuilder.ToString());
        }

        public static void Stop(ref RichTextBox rtfResults) {
            ReplaceText(ref rtfResults, 0, $"{MESSAGE_UUT_EVENT}", $"{MESSAGE_UUT_EVENT}{testSequence.Event}");
            SetBackColor(ref rtfResults, 0, testSequence.Event.ToString(), EventColors[testSequence.Event]);
            Log.CloseAndFlush();
            if (testSequence.IsOperation) {
                if (testDefinition.TestData.Item is XML) StopXML();
                else if (testDefinition.TestData.Item is SQL) StopSQL();
                else throw new ArgumentException($"Unknown {nameof(TestData)} item '{testDefinition.TestData.Item}'.");
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
            using (StringWriter stringWriter = new StringWriter()) {
                using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings { Encoding = new UTF8Encoding(true), Indent = true })) {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(TestSequence), GetOverrides());
                    xmlSerializer.Serialize(xmlWriter, testSequence);
                    xmlWriter.Flush();

                    using (SqlConnection sqlConnection = new SqlConnection(((SQL)testDefinition.TestData.Item).ConnectionString)) {
                        using (SqlCommand sqlCommand = new SqlCommand("INSERT INTO Sequences (Sequence) VALUES (@XML)", sqlConnection)) {
                            sqlCommand.Parameters.AddWithValue("@XML", stringWriter.ToString());
                            sqlConnection.Open();
                            sqlCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        private static void StopXML() {
            const String _xml = ".xml";
            XML xml = (XML)testDefinition.TestData.Item;
            String xmlFolder = $"{xml.Folder}\\{testSequence.TestOperation.NamespaceTrunk}";
            String xmlBaseName = $"{testSequence.UUT.Number}_{testSequence.SerialNumber}_{testSequence.TestOperation.NamespaceTrunk}";
            String[] xmlFileNames = Directory.GetFiles(xmlFolder, $"{xmlBaseName}_*{_xml}", SearchOption.TopDirectoryOnly);
            // NOTE:  Will fail if invalid path.  Don't catch resulting Exception though; this has to be fixed in TestDefinitionXML.
            Int32 maxNumber = 0; String s;
            foreach (String xmlFileName in xmlFileNames) {
                s = xmlFileName;
                foreach (EVENTS Event in Enum.GetValues(typeof(EVENTS))) s = s.Replace(Event.ToString(), String.Empty);
                s = s.Replace($"{xmlFolder}\\{xmlBaseName}", String.Empty);
                s = s.Replace(_xml, String.Empty);
                s = s.Replace("_", String.Empty);

                if (Int32.Parse(s) > maxNumber) maxNumber = Int32.Parse(s);
            }

            using (FileStream fileStream = new FileStream($"{xmlFolder}\\{xmlBaseName}_{++maxNumber}_{testSequence.Event}{_xml}", FileMode.CreateNew)) {
                using (XmlTextWriter xmlTextWriter = new XmlTextWriter(fileStream, new UTF8Encoding(true))) {
                    xmlTextWriter.Formatting = Formatting.Indented;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(TestSequence), GetOverrides());
                    xmlSerializer.Serialize(xmlTextWriter, testSequence);
                }
            }
        }

        private static XmlAttributeOverrides GetOverrides() {
            XmlAttributes xmlAttributes;
            XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
            xmlAttributes = new XmlAttributes { XmlIgnore = true };
            xmlAttributeOverrides.Add(typeof(UUT), nameof(UUT.Documentation), xmlAttributes);
            xmlAttributes = new XmlAttributes { XmlIgnore = true };
            xmlAttributeOverrides.Add(typeof(TestOperation), nameof(TestOperation.ProductionTest), xmlAttributes);
            xmlAttributes = new XmlAttributes { XmlIgnore = true };
            xmlAttributeOverrides.Add(typeof(Method), nameof(Method.CancelNotPassed), xmlAttributes);
            xmlAttributes = new XmlAttributes { XmlIgnore = true };
            xmlAttributeOverrides.Add(typeof(TestGroup), nameof(TestGroup.CancelNotPassed), xmlAttributes);
            xmlAttributes = new XmlAttributes { XmlIgnore = true };
            xmlAttributeOverrides.Add(typeof(TestGroup), nameof(TestGroup.Independent), xmlAttributes);
            return xmlAttributeOverrides;
        }
        #endregion Private
    }
}