using System;
using Tektronix.Tkdpo2k3k4k.Interop;

namespace ABT.Test.TestExecutive.Instruments.Oscilloscopes {
    public class MSO_3014 : Tkdpo2k3k4kClass {

        public MSO_3014(String Address) { Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty); }
    }
}
