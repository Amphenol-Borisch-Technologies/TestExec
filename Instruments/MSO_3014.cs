using System;
using Tektronix.Tkdpo2k3k4k.Interop;

namespace ABT.Test.Instruments {
    public class MSO_3014 : Tkdpo2k3k4kClass {
        public readonly String Address;
        public MSO_3014(String Address) {
            this.Address = Address;
            Initialize(Address, false, true, "");
        }
    }
}
