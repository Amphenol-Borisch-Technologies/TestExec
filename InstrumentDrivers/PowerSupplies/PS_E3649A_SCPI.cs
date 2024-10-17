﻿using System;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;

namespace ABT.Test.TestExecutive.InstrumentDrivers.PowerSupplies {
    public class PS_E3649A_SCPI : AgE364xD, IInstrumentDrivers, IPowerSupplyE3649A {
        public String Address { get; }
        public String Detail { get; }

        public void Reinitialize() {
            SCPI.RST.Command();
            SCPI.CLS.Command();
        }

        public PS_E3649A_SCPI(String Address, String Detail) : base(Address) {
            this.Address = Address;
            this.Detail = Detail;
        }

        public OUTPUTS2 Selected() {
            SCPI.INSTrument.SELect.Query(out String select);
            return select == "OUTP1" ? OUTPUTS2.OUTput1 : OUTPUTS2.OUTput2;
        }

        public void Select(OUTPUTS2 Output) { SCPI.INSTrument.SELect.Command($"{Output}"); }

        public (Double AmpsDC, Double VoltsDC) Get(OUTPUTS2 Output, DC DC) {
            Select(Output);
            SCPI.MEASure.SCALar.CURRent.DC.Query(out Double AmpsDC);
            SCPI.MEASure.SCALar.VOLTage.DC.Query(out Double VoltsDC);
            return (AmpsDC, VoltsDC);
        }

        public void Set(OUTPUTS2 Output, Double Volts, Double Amps, Double OVP, STATES State) {
            Select(Output);
            SCPI.OUTPut.STATe.Command(false);
            SCPI.SOURce.VOLTage.PROTection.CLEar.Command();
            SCPI.SOURce.VOLTage.PROTection.LEVel.Command($"{MMD.MAXimum}");
            SCPI.SOURce.VOLTage.LEVel.IMMediate.AMPLitude.Command($"{Volts}");
            SCPI.SOURce.CURRent.LEVel.IMMediate.AMPLitude.Command($"{Amps}");
            SCPI.SOURce.VOLTage.PROTection.LEVel.Command($"{OVP}");
            SCPI.OUTPut.STATe.Command(State == STATES.ON);
        }

        public STATES StateGet(OUTPUTS2 Output) {
            Select(Output);
            SCPI.OUTPut.STATe.Query(out Boolean state);
            return state ? STATES.ON : STATES.off;
        }

        public void StateSet(STATES State) {
            // NOTE: Most multi-output supplies like the E3649A permit individual control of outputs,
            // but the E3649A does not; all supplies are set to the same STATE, off or ON.
            SCPI.OUTPut.STATe.Command(State == STATES.ON);
        }
    }
}
