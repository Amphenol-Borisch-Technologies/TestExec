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

        public static Dictionary<String, Object> Get() {
            Dictionary<String, Object> Instruments = new Dictionary<String, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("Instruments").Elements("Instrument")) {
                String ID_System = xe.Attribute("ID_System").Value;
                String Detail = xe.Attribute("Detail").Value;
                String Address = xe.Attribute("Address").Value;
                String ClassName = xe.Attribute("ClassName").Value;
                Instruments.Add(ID_System, Activator.CreateInstance(Type.GetType(ClassName), new Object[] { ID_System, Address, Detail }));
            }
            return Instruments;
        }

        public static (String Detail, String Address, String ClassName) Get(String ID) {
            XElement Instrument = XElement.Load(TestExec.GlobalConfigurationFile).Element("Instruments").Elements("Instrument").FirstOrDefault(x => x.Element("ID_System").Value == ID);
            if (Instrument != null) return (Instrument.Element("Detail").Value, Instrument.Element("Address").Value, Instrument.Element("ClassName").Value);
            throw new ArgumentException($"Instrument with ID_System '{ID}' not present in file '{TestExec.GlobalConfigurationFile}'.");
        }

        public static void Clear(Dictionary<String, Object> Instruments) {
            foreach (KeyValuePair<String, Object> kvp in Instruments) { new AgSCPI99(Get(kvp.Key).Address).SCPI.CLS.Command(); }
        }

        public static void Initialize(Dictionary<String, Object> Instruments) {
            Reset(Instruments);
            Clear(Instruments);
        }

        public static void Reset(Dictionary<String, Object> Instruments) {
            foreach (KeyValuePair<String, Object> kvp in Instruments) { new AgSCPI99(Get(kvp.Key).Address).SCPI.RST.Command(); }
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

        public static Int32 SelfTestFailures(Dictionary<String, Object> Instruments)  {
            Int32 selfTestFailures = 0;
            foreach (KeyValuePair<String, Object> kvp in Instruments) selfTestFailures += SelfTest(kvp.Key);
            return selfTestFailures;
        }

        public static Boolean SelfTestPassed(Form CurrentForm, String ID) {
            Int32 selfTestResult;
            try {
                selfTestResult = SelfTest(ID);
            } catch (Exception) {
                (String Detail, String Address, String ClassName) = Get(ID);
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{ID}'{Environment.NewLine}" +
                    $"Address: '{Address}'{Environment.NewLine}" +
                    $"Detail:  '{Detail}'{Environment.NewLine}" +
                    $"likely unpowered or not communicating.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // If unpowered, SelfTest throws a Keysight.CommandExpert.InstrumentAbstraction.CommunicationException exception,
                // which requires an apparently unavailable Keysight library to explicitly catch.
                return false;
            }
            if (selfTestResult == 1) {
                (String Detail, String Address, String ClassName) = Get(ID);
                _ = MessageBox.Show(CurrentForm, $"Instrument:'{ID}'{Environment.NewLine}" +
                    $"Address: '{Address}'{Environment.NewLine}" +
                    $"Detail:  '{Detail}'{Environment.NewLine}" +
                    $"failed Self-Test.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true; // selfTestResult == 0.
        }

        public static Boolean SelfTestsPassed(Form CurrentForm, Dictionary<String, Object> Instruments) {
            Boolean selfTestsPassed = true;
            foreach (KeyValuePair<String, Object> kvp in Instruments) selfTestsPassed &= SelfTestPassed(CurrentForm, kvp.Key);
            return selfTestsPassed;
        }
    }
}
