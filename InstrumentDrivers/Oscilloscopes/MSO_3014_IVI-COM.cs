using ABT.Test.TestExecutive.InstrumentDrivers.Generic;
using System;
using Tektronix.Tkdpo2k3k4k.Interop;

namespace ABT.Test.TestExecutive.InstrumentDrivers.Oscilloscopes {
    public class MSO_3014_IVI_COM : Tkdpo2k3k4kClass, IInstrumentDrivers {
        public String Address { get; }
        public String Detail { get; }

        public void Reinitialize() {
            Utility.Reset();
        }

        public MSO_3014_IVI_COM(String Address, String Detail) {
            this.Address = Address;
            this.Detail = Detail;
            Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty);
        }
    }
}
