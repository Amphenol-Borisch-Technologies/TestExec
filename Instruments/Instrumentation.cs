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
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("InstrumentsSystem").Elements("Instrument")) {
                String ID = xe.Attribute("ID").Value;
                String Detail = xe.Attribute("Detail").Value;
                String Address = xe.Attribute("Address").Value;
                String ClassName = xe.Attribute("ClassName").Value;
                Instruments.Add(ID, Activator.CreateInstance(Type.GetType(ClassName), new Object[] { ID, Address, Detail }));
            }
            return Instruments;
        }

        public static (String Detail, String Address, String ClassName) Get(String ID) {
            XElement Instrument = XElement.Load(TestExec.GlobalConfigurationFile).Element("Instruments").Elements("Instrument").FirstOrDefault(x => x.Element("ID").Value == ID);
            if (Instrument != null) return (Instrument.Element("Detail").Value, Instrument.Element("Address").Value, Instrument.Element("ClassName").Value);
            throw new ArgumentException($"Instrument with ID '{ID}' not present in file '{TestExec.GlobalConfigurationFile}'.");
        }

        public static void Reinitialize(Dictionary<String, Object> Instruments) {
            foreach (KeyValuePair<String, Object> kvp in Instruments) {
                switch (kvp.Value) {
                    case Generic.SCPI i:
                        ((Generic.SCPI)kvp.Value).Reinitialize();
                        break;
                    case Multifunction.MF_34980A_SCPI i:
                        ((Multifunction.MF_34980A_SCPI)kvp.Value).Reinitialize();
                        break;
                    case MultiMeters.MM_34401A_IVI_COM i:
                        ((MultiMeters.MM_34401A_IVI_COM)kvp.Value).Reinitialize();
                        break;
                    case MultiMeters.MM_34401A_SCPI i:
                        ((MultiMeters.MM_34401A_SCPI)kvp.Value).Reinitialize();
                        break;
                    case Oscilloscopes.MSO_3014_IVI_COM i:
                        ((Oscilloscopes.MSO_3014_IVI_COM)kvp.Value).Reinitialize();
                        break;
                    case PowerSupplies.PS_E3634A_SCPI i:
                        ((PowerSupplies.PS_E3634A_SCPI)kvp.Value).Reinitialize();
                        break;
                    case PowerSupplies.PS_E3649A_SCPI i:
                        ((PowerSupplies.PS_E3649A_SCPI)kvp.Value).Reinitialize();
                        break;
                    default:
                        throw new ArgumentException($"Unknow Instrument: Alias '{kvp.Key}', Type '{kvp.Value.GetType()}'.");
                }
            }
        }
    }
}
