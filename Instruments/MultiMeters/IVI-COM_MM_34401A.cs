using System;
using Agilent.Agilent34401.Interop;

namespace ABT.Test.TestExecutive.Instruments.MultiMeters {
    internal class IVI_COM_MM_34401A : Agilent34401Class {
        public IVI_COM_MM_34401A(String Address) { Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty); }
    }
}