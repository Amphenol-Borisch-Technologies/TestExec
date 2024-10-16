using System;

namespace ABT.Test.TestExecutive.Instruments {
    public interface IInstruments {
        String Address { get; }
        String Detail { get; }
        void Reinitialize();
    }
}
