using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ABT.Test.TestExecutive.Instruments {

    public static class Instrumentation {
        public enum IDN_FIELDS { Manufacturer, Model, SerialNumber, FirmwareRevision } // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".  
        public enum STATES { off=0, ON=1 }
        public enum PS { Amps, Volts }
        public enum SENSE_MODE { EXTernal, INTernal }
        public enum STATE { off, ON }
        // Consistent convention for lower-cased inactive states off/low/zero as 1st states in enums, UPPER-CASED active ON/HIGH/ONE as 2nd states.

        public static Dictionary<Alias, Instrument> Get() {
            Dictionary<Alias, Instrument> Instruments =  new Dictionary<Alias, Instrument>();
            foreach (XElement instrumentElement in XElement.Load(TestExec.GlobalConfigurationFile).Elements("Instruments").Elements("Instrument")) {
                Instruments.Add(new Alias(instrumentElement.Element("ID").Value), new Instrument(instrumentElement));
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

        public sealed class Instrument {
            public readonly String ID;
            public readonly String Description;
            public readonly String Address;
            public readonly String AssemblyQualifiedName;
            public readonly Object Instance;

            internal Instrument(XElement instrumentElement) {
                // NOTE: Utilized by ABT.Test.TestExecutive.Instruments.Instrumentation.Get() method to add all permanent instruments.
                // Permanent instruments are those that are never removed from the test system; they'll always be available to the system.
                ID = instrumentElement.Attribute("ID").Value;
                Description = instrumentElement.Attribute("Description").Value;
                Address = instrumentElement.Attribute("Address").Value;
                AssemblyQualifiedName = instrumentElement.Attribute("AssemblyQualifiedName").Value;
                Instance = Activator.CreateInstance(Type.GetType(AssemblyQualifiedName), new Object[] { Address });
            }

            public Instrument(String ID, String Description, String Address, String AssemblyQualifiedName, Object Instance) {
                // NOTE: Utilized by ABT.Test.TestPlan to add temporary instruments needed only by specific TestPlans.
                // Temporary instruments are added/removed as needed to test specific UUTs.
                // - We likely have few to 1 of such temporay instruments at ABT, due to high expen$e and/or low usage.
                // - Such can move from production test stations to troubleshooting benches to lab use as needed.
                this.ID = ID;
                this.Description = Description;
                this.Address = Address;
                this.AssemblyQualifiedName = AssemblyQualifiedName;
                this.Instance = Instance;
            }
        }
    }
}
