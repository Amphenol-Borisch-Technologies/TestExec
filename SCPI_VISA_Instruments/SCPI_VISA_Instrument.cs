using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using NationalInstruments.Visa;
using ABT.Test.TestExecutive.Logging;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments {
    // NOTE:  https://forums.ni.com/t5/Instrument-Control-GPIB-Serial/IVI-Drivers-Pros-and-Cons/td-p/4165671.

    public abstract class SCPI_VISA_InstrumentOld {
        public enum COMMANDS { CLS, ESE, OPC, RST, SRE, WAI }       // SCPI Mandated Commands & Queries; required for all SCPI implementations.
        public enum QUERIES { ESE, ESR, IDN, OPC, SRE, STB, TST }   // https://www.ivifoundation.org/downloads/SCPI/scpi-99.pdf
        public enum IDN_FIELDS { Manufacturer, Model, SerialNumber, FirmwareRevision } // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".  
        public enum STATES { off=0, ON=1 }
        public readonly Alias ID;
        public readonly String Description;
        public readonly String Address;
        public readonly String ClassName;
        public readonly MessageBasedSession Session;
        public abstract String MODEL { get; }
        public const String LF = "\n"; // Line Feed.
        public Boolean Is() { return String.Equals(IDN(IDN_FIELDS.Model), MODEL); }
        public StringBuilder SCPI = new();

        public SCPI_VISA_InstrumentOld(Alias id, String description, String address, String className) {
            ID = id;
            Description = description;
            Address = address;
            ClassName = className;
            using ResourceManager rm = new();
            // NOTE:  Must copy C:\Program Files (x86)\National Instruments\Measurement Studio\DotNET\v4.0\AnyCPU\NationalInstruments.Common 19.0.40\NationalInstruments.Common.dll to the output directory.
            // Referenced in TestExec project, and implicitly copied locally into output directory.
            // https://forums.ni.com/t5/Measurement-Studio-for-NET/Could-not-load-NationalInstruments-common/td-p/3265218
            // Could also invoke Assembly.LoadFrom(@"C:\Program Files (x86)\National Instruments\Measurement Studio\DotNET\v4.0\AnyCPU\NationalInstruments.Common 19.0.40\NationalInstruments.Common.dll");
            // https://forums.ni.com/t5/Measurement-Studio-for-NET/Could-not-load-file-or-assembly-NationalInstruments-Common/td-p/4279770
            Session = (MessageBasedSession)rm.Open(Address);
        }

        private String Read() {
            TestExec.CT_EmergencyStop.ThrowIfCancellationRequested();
            return Session.RawIO.ReadString().Replace("\r\n", "").Replace(LF, "");
        }

        private void Write(String C) {
            C = C.Replace("\\r\\n", LF).Replace("\\n", LF);
            if (!String.Equals(C.Replace(LF, ""), $"*{COMMANDS.CLS}")) TestExec.CT_EmergencyStop.ThrowIfCancellationRequested();
            // NOTE:  Mustn't invoke TestExec.CT_EmergencyStop.ThrowIfCancellationRequested(); on *CLS.
            if (!C.EndsWith(LF)) C += LF;
            Session.RawIO.Write(C);
        }

        internal static Boolean IsCloseEnough(Double D1, Double D2, Double Delta) { return Math.Abs(D1 - D2) <= Delta; }
        // Close is good enough for horseshoes, hand grenades, nuclear weapons, and Doubles!  Shamelessly plagiarized from the Internet!

        public void Command(String C) { Write(C); }

        public void Command(ref StringBuilder C) {
            Write(C.ToString());
            C.Clear();
        }

        public void Command(COMMANDS C) { Write($"*{C}"); }

        public static void Command(Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs, String C) { foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) kvp.Value.Command(C); }

        public static void Command(Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs, COMMANDS C) { foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) kvp.Value.Command(C); }

        public String Query(String Q) {
            Write(Q);
            return Read();
        }

        public String Query(ref StringBuilder Q) {
            Write(Q.ToString());
            Q.Clear();
            return Read();
        }

        public String Query(QUERIES Q) { return Query($"*{Q}?"); }

        public static Dictionary<Alias, String> Query(Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs, String Q) {
            Dictionary<Alias, String> q = [];
            foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) q.Add(kvp.Key, kvp.Value.Query(Q));
            return q;
        }

        public static Dictionary<Alias, String> Query(Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs, QUERIES Q) {
            Dictionary<Alias, String> q = [];
            foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) q.Add(kvp.Key, kvp.Value.Query(Q));
            return q;
        }

        public void Initialize() {
            Command(COMMANDS.RST);
            Command(COMMANDS.CLS);
        }

        public static void Initialize(Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs) { foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) kvp.Value.Initialize(); }

        public static Dictionary<Alias, SCPI_VISA_InstrumentOld> Get() {
            Type type;
            Alias alias;
            SCPI_VISA_InstrumentOld svi;
            Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs = [];
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("SCPI_VISA_Instruments").Elements("SVI")) {
                type = Type.GetType("ABT.Test.TestExecutive.SCPI_VISA_Instruments." + xe.Element("ClassName").Value);
                alias = new Alias(xe.Element("ID").Value);
                svi = (SCPI_VISA_InstrumentOld)Activator.CreateInstance(type, [alias, xe.Element("Description").Value, xe.Element("Address").Value, xe.Element("ClassName").Value]);
                SVIs.Add(alias, svi);
            }
            return SVIs;
        }

        public String GetInfo(String optionalHeader = "") {
            String info = String.Equals(optionalHeader, "") ? optionalHeader : optionalHeader += Environment.NewLine;
            foreach (PropertyInfo pi in this.GetType().GetProperties()) info += $"{pi.Name.PadLeft(Logger.SPACES_21.Length)}: '{pi.GetValue(this)}'{Environment.NewLine}";
            return info;
        }

        internal String ErrorMessageGet() { return GetInfo($"SCPI VISA Instrument Address '{this.Address}' failed.{Environment.NewLine}"); }

        internal String ErrorMessageGet(String errorMessage) { return $"{ErrorMessageGet()}{errorMessage}{Environment.NewLine}"; }

        public String IDN(IDN_FIELDS IDN_Field) { return Query(QUERIES.IDN).Split(',')[(Int32)IDN_Field]; }

        public Boolean SelfTestPassed() { return Query(QUERIES.TST) == "0"; }

        public Boolean SelfTestPassed(Form CurrentForm) {
            Boolean selfTestPassed;
            try {
                selfTestPassed = SelfTestPassed();
            } catch (Exception) {
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{Description}'{Environment.NewLine}Address: '{Address}'{Environment.NewLine}likely unpowered or not communicating.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!selfTestPassed) {
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{Description}'{Environment.NewLine}Address: '{Address}'{Environment.NewLine}failed Self-Test.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        public static Boolean SelfTestsPassed(Form CurrentForm, Dictionary<Alias, SCPI_VISA_InstrumentOld> SVIs) {
            Boolean selfTestsPassed = true;
            foreach (KeyValuePair<Alias, SCPI_VISA_InstrumentOld> kvp in SVIs) selfTestsPassed &= kvp.Value.SelfTestPassed(CurrentForm);
            return selfTestsPassed;
        }

        public class Alias(String name) {
            public readonly String ID = name;

            public override Boolean Equals(Object obj) {
                Alias a = obj as Alias;
                if (ReferenceEquals(this, a)) return true;
                return a != null && ID == a.ID;
            }

            public override Int32 GetHashCode() { return 3 * ID.GetHashCode(); }

            public override String ToString() { return ID; }
        }
    }
}
