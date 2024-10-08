using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;

namespace ABT.Test.TestExecutive.Instruments {
    public static class Instrumentation {
        public enum IDN_FIELDS { Manufacturer, Model, SerialNumber, FirmwareRevision } // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".  
        public enum STATES { off = 0, ON = 1 }
        public enum PS { Amps, Volts }
        public enum SENSE_MODE { EXTernal, INTernal }
        private const Char IDENTITY_SEPARATOR = ',';
        // Consistent convention for lower-cased inactive states off/low/zero as 1st states in enums, UPPER-CASED active ON/HIGH/ONE as 2nd states.

        public static Dictionary<Alias, Object> Get() {
            Dictionary<Alias, Object> Instruments = new Dictionary<Alias, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("Instruments").Elements("Instrument")) {
                String ID = xe.Attribute("ID").Value;
                String Address = xe.Attribute("Address").Value;
                String ClassName = xe.Attribute("ClassName").Value;
                Instruments.Add(new Alias(ID), Activator.CreateInstance(Type.GetType(ClassName), new Object[] { Address }));
            }
            return Instruments;
        }

        public static (String Description, String Address, String ClassName) Get(String ID) {
            XElement Instrument = XElement.Load(TestExec.GlobalConfigurationFile).Element("Instruments").Elements("Instrument").FirstOrDefault(x => x.Element("ID").Value == ID);
            if (Instrument != null) return (Instrument.Element("Description").Value, Instrument.Element("Address").Value, Instrument.Element("ClassName").Value);
            throw new ArgumentException($"Instrument with ID '{ID}' not present in file '{TestExec.GlobalConfigurationFile}'.");
        }

        public sealed class Alias {
            public readonly String ID;

            public Alias(String name) { ID = name; }
            public override Boolean Equals(Object obj) {
                Alias a = obj as Alias;
                if (ReferenceEquals(this, a)) return true;
                return a != null && ID == a.ID;
            }

            public override Int32 GetHashCode() { return 3 * ID.GetHashCode(); }

            public override String ToString() { return ID; }
        }

        public static void Clear(Dictionary<Alias, Object> Instruments) {
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) { new AgSCPI99(Get(kvp.Key.ID).Address).SCPI.CLS.Command(); }
        }

        public static void Initialize(Dictionary<Alias, Object> Instruments) {
            Reset(Instruments);
            Clear(Instruments);
        }

        public static void Reset(Dictionary<Alias, Object> Instruments) {
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) { new AgSCPI99(Get(kvp.Key.ID).Address).SCPI.RST.Command(); }
        }

        public static String IdentityGet(String ID) {
            new AgSCPI99(Get(ID).Address).SCPI.IDN.Query(out String Identity);
            return Identity;
        }

        public static String IdentityGet(String ID, IDN_FIELDS idn_field) {
            return IdentityGet(ID).Split(IDENTITY_SEPARATOR)[(Int32)idn_field];
        }

        public static Int32 SelfTest(String ID) {
            new AgSCPI99(Get(ID).Address).SCPI.TST.Query(out Int32 selfTestResult);
            return selfTestResult; // 0 == passed, 1 == failed.
        }

        public static Int32 SelfTestFailures(Dictionary<Alias, Object> Instruments)  {
            Int32 selfTestFailures = 0;
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) selfTestFailures += SelfTest(Get(kvp.Key.ID).Address);
            return selfTestFailures;
        }

        public static Boolean SelfTestPassed(Form CurrentForm, String ID) {
            Int32 selfTestResult;
            try {
                selfTestResult = SelfTest(ID);
            } catch (Exception) {
                (String Description, String Address, String ClassName) = Get(ID);
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{ID}'{Environment.NewLine}" +
                    $"Address: '{Address}'{Environment.NewLine}" +
                    $"Description: '{Description}'{Environment.NewLine}" +
                    $"likely unpowered or not communicating.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // If unpowered, SelfTest throws a Keysight.CommandExpert.InstrumentAbstraction.CommunicationException exception,
                // which requires an apparently unavailable Keysight library to explicitly catch.
                return false;
            }
            if (selfTestResult == 1) {
                (String Description, String Address, String ClassName) = Get(ID);
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{ID}'{Environment.NewLine}" +
                    $"Address: '{Address}'{Environment.NewLine}" +
                    $"Description: '{Description}'{Environment.NewLine}" +
                    $"failed Self-Test.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true; // selfTestResult == 0.
        }

        public static Boolean SelfTestsPassed(Form CurrentForm, Dictionary<Alias, Object> Instruments) {
            Boolean selfTestsPassed = true;
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) selfTestsPassed &= SelfTestPassed(CurrentForm, kvp.Key.ID);
            return selfTestsPassed;
        }
    }
}
