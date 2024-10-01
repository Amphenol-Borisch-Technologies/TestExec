using System;
using System.Collections.Generic;
using System.Xml.Linq;

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
            Dictionary<Alias, Object> Instruments =  new Dictionary<Alias, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("Instruments").Elements("Instrument")) {
                type = Type.GetType(xe.Element("AssemblyQualifiedName").Value);
                Instruments.Add(new Alias(xe.Element("ID").Value), Activator.CreateInstance(type, new Object[] { xe.Element("Address").Value }));
            }
            return Instruments;
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
