﻿using System;
using System.Collections.Generic;
using Agilent.CommandExpert.ScpiNet.Ag33500B_33600A_2_09;
using TestLibrary.AppConfig;
// All Agilent.CommandExpert.ScpiNet drivers are created by adding new SCPI VISA Instruments in Keysight's Command Expert app software.
//  - Command Expert literally downloads & installs Agilent.CommandExpert.ScpiNet drivers when new SVIs are added.
//  - The Agilent.CommandExpert.ScpiNet dirvers are installed into folder C:\ProgramData\Keysight\Command Expert\ScpiNetDrivers.
// https://www.keysight.com/us/en/lib/software-detail/computer-software/command-expert-downloads-2151326.html
//
// Recommend using Command Expert to generate SCPI commands, which are directly exportable as .Net statements.
//
namespace TestLibrary.SCPI_VISA_Instruments {
    public static class WG_33509B {
        public const String MODEL = "33509B";

        public static Boolean IsWG_33509(SCPI_VISA_Instrument SVI) { return (SVI.Instrument.GetType() == typeof(Ag33500B_33600A)); }

        public static void Clear(SCPI_VISA_Instrument SVI) { ((Ag33500B_33600A)SVI.Instrument).SCPI.CLS.Command(); }

        public static void ClearAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) if (IsWG_33509(kvp.Value)) Clear(kvp.Value); }

        public static void Reset(SCPI_VISA_Instrument SVI) { ((Ag33500B_33600A)SVI.Instrument).SCPI.RST.Command(); }

        public static void ResetAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) if (IsWG_33509(kvp.Value)) Reset(kvp.Value); }

        public static void SelfTest(SCPI_VISA_Instrument SVI) {
            ((Ag33500B_33600A)SVI.Instrument).SCPI.TST.Query(out Int32 selfTestResult);
            if (selfTestResult != 0) {
                ((Ag33500B_33600A)SVI.Instrument).SCPI.SYSTem.ERRor.Query(out Int32 errorNumber, out String errorMessage);
                throw new InvalidOperationException(SCPI_VISA.GetErrorMessage(SVI, errorMessage, errorNumber));
            }
        }

        public static void SelfTestAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) if (IsWG_33509(kvp.Value)) SelfTest(kvp.Value); }

        public static void Initialize(SCPI_VISA_Instrument SVI) {
            Reset(SVI); // Reset SVI to default power-on states.
            Clear(SVI); // Clear all event registers & the Status Byte register.
            SelfTest(SVI);
            ((Ag33500B_33600A)SVI.Instrument).SCPI.DISPlay.TEXT.CLEar.Command();
        }

        public static void InitializeAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) if (IsWG_33509(kvp.Value)) Initialize(kvp.Value); }
    }
}