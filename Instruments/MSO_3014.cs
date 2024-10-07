using System;
using Tektronix.Tkdpo2k3k4k.Interop;

namespace ABT.Test.TestExecutive.Instruments {
    public class MSO_3014 : Tkdpo2k3k4kClass {
        public readonly String ID;
        public readonly String Detail;
        public readonly String Address;

        public MSO_3014(String ID, String Detail, String Address) {
            Initialize(ResourceName: Address, IdQuery: false, Reset: true, OptionString: String.Empty);
            this.ID = ID;
            this.Detail = Detail;
            this.Address = Address;
        }
    }
}
