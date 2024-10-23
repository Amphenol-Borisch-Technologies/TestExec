using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Serilog.Core; // Install Serilog via NuGet Package Manager.  Site is https://serilog.net/.
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace ABT.Test.Exec.Logging {
    public class RichTextBoxSink : ILogEventSink {
        private readonly RichTextBox richTextBox;
        private readonly ITextFormatter formatter;

        public RichTextBoxSink(ref RichTextBox richTextBox, String outputTemplate = Logger.LOGGER_TEMPLATE) {
            this.richTextBox = richTextBox;
            formatter = new MessageTemplateTextFormatter(outputTemplate);
        }

        public void Emit(LogEvent logEvent) {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            StringWriter stringWriter = new StringWriter();
            formatter.Format(logEvent, stringWriter);
            Int32 startFind = richTextBox.TextLength;
            String logMessage = stringWriter.ToString();
            richTextBox.InvokeIfRequired(() => richTextBox.AppendText(logMessage));

            Int32 selectionStart; String testEvent;
            foreach (FieldInfo fi in typeof(TestEvents).GetFields()) {
                testEvent = (String)fi.GetValue(null);
                if (logMessage.Contains(testEvent)) {
                    selectionStart = richTextBox.Find(testEvent, startFind, RichTextBoxFinds.MatchCase | RichTextBoxFinds.WholeWord);
                    richTextBox.SelectionStart = selectionStart;
                    richTextBox.SelectionLength = testEvent.Length;
                    richTextBox.SelectionBackColor = TestEvents.GetColor(testEvent);
                }
            }
        }
    }

    public static class WinFormsControlExtensions {
        public static void InvokeIfRequired(this Control c, MethodInvoker action) {
            if (c.InvokeRequired) c.Invoke(action);
            else action();
        }
    }
}
