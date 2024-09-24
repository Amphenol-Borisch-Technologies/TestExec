using System;
using System.ComponentModel;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.PowerSupplies.Keysight {

    public class PS_E3649A(SCPI_VISA_InstrumentOld.Alias id, String description, String address, String className) : PS_E36XXA(id, description, address, className) {
        public override String MODEL { get { return "E3649A"; } }  // NOTE: E3649A is a dual supply, but only has a single on/off.  Both turn on or off together, inseparably.
        public enum OUTPUTS { OUTPut1, OUTPut2 };

        public OUTPUTS InstrumentGet() { return Query(":INSTrument:SELect?") == "OUTP1" ? OUTPUTS.OUTPut1 : OUTPUTS.OUTPut2; }

        public void InstrumentSet(OUTPUTS Output) { Command($":INSTrument:SELect {Enum.GetName(typeof(OUTPUTS), Output)}"); }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Set(OUTPUTS Output, Single Volts, Single Amps, Single OVP, STATES State)", true)]
        public override void Set(Single Volts, Single Amps, Single OVP, STATES State) { }

        public void Set(OUTPUTS Output, Single Volts, Single Amps, Single OVP, STATES State) {
            InstrumentSet(Output);
            base.Set(Volts, Amps, OVP, State);
        }
    }
}