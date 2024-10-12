using System;
using Tektronix.Tkdpo2k3k4k.Interop;

namespace ABT.Test.TestExecutive.Instruments.Oscilloscopes {
    public class IVI_COM_MSO_3014 : Tkdpo2k3k4kClass {

        public IVI_COM_MSO_3014(String Address) { Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty); }
    }
}
