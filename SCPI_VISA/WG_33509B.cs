﻿using System;
using Agilent.CommandExpert.ScpiNet.Ag33500B_33600A_2_09;
// All Agilent.CommandExpert.ScpiNet drivers are created by adding new instruments in Keysight's Command Expert app software.
//  - Command Expert literally downloads & installs Agilent.CommandExpert.ScpiNet drivers when new instruments are added.
//  - The Agilent.CommandExpert.ScpiNet dirvers are installed into folder C:\ProgramData\Keysight\Command Expert\ScpiNetDrivers.
// https://www.keysight.com/us/en/lib/software-detail/computer-software/command-expert-downloads-2151326.html
//
// Recommend using Command Expert to generate SCPI commands, which are directly exportable as .Net statements.
//
namespace TestLibrary.SCPI_VISA {
    public static class WG_33509B {
        public static void SpecificInitialization(Instrument instrument) {
            SCPI99.SelfTest(instrument); // SCPI99.SelfTest() issues a Factory Reset (*RST) command after its *TST completes.
            SCPI99.Clear(instrument);    // SCPI99.Clear() issues SCPI *CLS.
            ((Ag33500B_33600A)instrument.Instance).SCPI.DISPlay.TEXT.CLEar.Command();
        }
    }
}