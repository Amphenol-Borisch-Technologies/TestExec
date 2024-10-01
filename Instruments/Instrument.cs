using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Linq;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;

namespace ABT.Test.TestExecutive.Instruments {

    public static class Instrument {
        public enum IDN_FIELDS { Manufacturer, Model, SerialNumber, FirmwareRevision } // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".  
        public enum STATES { off=0, ON=1 }
        public enum PS { Amps, Volts }
        public enum SENSE_MODE { EXTernal, INTernal }
        public enum STATE { off, ON }
        // Consistent convention for lower-cased inactive states off/low/zero as 1st states in enums, UPPER-CASED active ON/HIGH/ONE as 2nd states.

        public static Dictionary<Alias, Object> Get() {
            Type type;
            Alias alias;
            Object obj;
            Dictionary<Alias, Object> Instruments =  new Dictionary<Alias, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("Instruments").Elements("Instrument")) {
                type = Type.GetType("ABT.Test.TestExecutive.Instruments." + xe.Element("ClassName").Value);
                alias = new Alias(xe.Element("ID").Value);
                obj = Activator.CreateInstance(type, new Object[] { alias, xe.Element("Description").Value, xe.Element("Address").Value, xe.Element("ClassName").Value });
                Instruments.Add(alias, obj);
            }
            return Instruments;
        }

        public static Int32 SelfTest(Object Instrument) {
            new AgSCPI99(Instrument.Address).SCPI.TST.Query(out Int32 selfTestResult);
            return selfTestResult; // 0 == passed, 1 == failed.
        }

        public static Int32 SelfTestFailures(Dictionary<Alias, Object> Instruments) {
            Int32 selfTestFailures = 0;
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) selfTestFailures += SelfTest(kvp.Value);
            return selfTestFailures;
        }

        public static Boolean SelfTestPassed(Form CurrentForm, Object Instrument) {
            Int32 selfTestResult;
            try {
                selfTestResult = SelfTest(Instrument);
            } catch (Exception) {
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{Instrument.Description}'{Environment.NewLine}Address: '{SVI.Address}'{Environment.NewLine}likely unpowered or not communicating.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // If unpowered, SelfTest throws a Keysight.CommandExpert.InstrumentAbstraction.CommunicationException exception,
                // which requires an apparently unavailable Keysight library to explicitly catch.
                return false;
            }
            if (selfTestResult == 1) {
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{SVI.Description}'{Environment.NewLine}Address: '{SVI.Address}'{Environment.NewLine}failed Self-Test.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true; // selfTestResult == 0.
        }

        public static Boolean SelfTestsPassed(Form CurrentForm, Dictionary<Alias, Object> Instruments) {
            Boolean selfTestsPassed = true;
            foreach (KeyValuePair<Alias, Object> kvp in Instruments) selfTestsPassed &= SelfTestPassed(CurrentForm, kvp.Value);
            return selfTestsPassed;
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
    }
}
