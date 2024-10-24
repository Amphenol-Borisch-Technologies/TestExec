﻿using System;
using System.Windows.Forms;
using Tektronix.Tkdpo2k3k4k.Interop;
using ABT.Test.Exec.InstrumentDrivers.Interfaces;

namespace ABT.Test.Exec.InstrumentDrivers.Oscilloscopes {
    public class MSO_3014_IVI_COM : Tkdpo2k3k4kClass, IInstruments {
        public String Address { get; }
        public String Detail { get; }
        public INSTRUMENT_TYPES InstrumentType { get; }

        public void ResetClear() {
            Utility.Reset();
        }

        public DIAGNOSTICS_RESULTS Diagnostics() {
            Int32 TestResult = 0;
            String TestMessage = String.Empty;
            try {
                UtilityEx.SelfTest(ref TestResult, ref TestMessage);
            } catch (Exception) {
                _ = MessageBox.Show($"Instrument with driver {GetType().Name} likely unpowered or not communicating:{Environment.NewLine}" + 
                    $"Type:      {InstrumentType}{Environment.NewLine}" +
                    $"Detail:    {Detail}{Environment.NewLine}" +
                    $"Address:   {Address}"
                    , "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // If unpowered or not communicating (comms cable possibly disconnected) SelfTest throws a
                // Keysight.CommandExpert.InstrumentAbstraction.CommunicationException exception,
                // which requires an apparently unavailable Keysight library to explicitly catch.
                return DIAGNOSTICS_RESULTS.FAIL;
            }
            return (DIAGNOSTICS_RESULTS)TestResult; // Tkdpo2k3k4kClass returns 0 for passed, 1 for fail.
        }

        public MSO_3014_IVI_COM(String Address, String Detail) {
            this.Address = Address;
            this.Detail = Detail;
            InstrumentType = INSTRUMENT_TYPES.OSCILLOSCOPE;
            Initialize(ResourceName: Address, IdQuery: false, Reset: false, OptionString: String.Empty);
        }
    }
}