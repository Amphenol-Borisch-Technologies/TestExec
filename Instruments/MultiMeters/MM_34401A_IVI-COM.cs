using System;
using ABT.Test.TestExecutive.Instruments.Generic;
using Agilent.Agilent34401.Interop;

namespace ABT.Test.TestExecutive.Instruments.MultiMeters {
    internal class MM_34401A_IVI_COM : Agilent34401Class, IInstruments {
        public String Address { get; }
        public String Detail { get; }

        public void Reinitialize() {
            Utility.Reset();
            Clear();
        }

        public MM_34401A_IVI_COM(String Address, String Detail) {
            this.Address = Address;
            this.Detail = Detail;
            Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty);
        }
    }
}