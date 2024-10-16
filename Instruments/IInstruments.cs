using System;

namespace ABT.Test.TestExecutive.Instruments {
    public enum STATES { off = 0, ON = 1 }
    public interface IInstruments {
        String Address { get; }
        String Detail { get; }
        void Reinitialize();
    }
}
