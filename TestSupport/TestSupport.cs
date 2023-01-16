﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using TestLibrary.Config;

namespace TestLibrary.TestSupport {
    public class TestCancellationException : Exception {
        public TestCancellationException() { }
        public TestCancellationException(String message) : base(message) { }
        public TestCancellationException(String message, Exception inner) : base(message, inner) { }
    }

    public static class EventCodes {
        public const String CANCEL = "CANCEL";
        public const String ERROR = "ERROR";
        public const String FAIL = "FAIL";
        public const String PASS = "PASS";
        public const String UNSET = "UNSET";

        public static Color GetColor(String EventCode) {
            Dictionary<String, Color> CodesToColors = new Dictionary<String, Color>() {
                {EventCodes.CANCEL, Color.Yellow },
                {EventCodes.ERROR, Color.Aqua },
                {EventCodes.FAIL, Color.Red },
                {EventCodes.PASS, Color.Green },
                {EventCodes.UNSET, Color.Gray }
            };
            return CodesToColors[EventCode];
        }
    }

    public static class TestTasks {
        public static String EvaluateTestResult(Test test) {
            // NOTE: Sequence of below if blocks interdependent.  That is, if reordered, they may fail.
            switch (test.Type) {
                case TestCustomized.Type:
                    return test.Measurement;
                case TestProgrammed.Type:
                    if (String.Equals(((TestProgrammed)test.TestClass).FirmwareCRC, test.Measurement)) return EventCodes.PASS;
                    else return EventCodes.FAIL;
                case TestRanged.Type:
                    if (!Double.TryParse(test.Measurement, NumberStyles.Float, CultureInfo.CurrentCulture, out Double dMeasurement)) throw new InvalidOperationException($"TestElement ID '{test.ID}' Measurement '{test.Measurement}' ≠ System.Double.");
                    TestRanged t = (TestRanged)test.TestClass;
                    if ((t.Low <= dMeasurement) && (dMeasurement <= t.High)) return EventCodes.PASS;
                    else return EventCodes.FAIL;
                default:
                    throw new NotImplementedException($"TestElement ID '{test.ID}' with Type '{test.Type}' not implemented.");
            }
        }

        public static String EvaluateUUTResult(ConfigTest configTest) {
            if (!configTest.Group.Required) return EventCodes.UNSET;
            // 0th priority evaluation that precedes all others.
            if (GetResultCount(configTest.Tests, EventCodes.PASS) == configTest.Tests.Count) return EventCodes.PASS;
            // 1st priority evaluation (or could also be last, but we're irrationally optimistic.)
            // All test results are PASS, so overall UUT result is PASS.
            if (GetResultCount(configTest.Tests, EventCodes.ERROR) > 0) return EventCodes.ERROR;
            // 2nd priority evaluation:
            // - If any test result is ERROR, overall UUT result is ERROR.
            if (GetResultCount(configTest.Tests, EventCodes.CANCEL) > 0) return EventCodes.CANCEL;
            // 3rd priority evaluation:
            // - If any test result is CANCEL, and none were ERROR, overall UUT result is CANCEL.
            if (GetResultCount(configTest.Tests, EventCodes.UNSET) > 0) throw new InvalidOperationException("One or more Tests didn't execute!");
            // 4th priority evaluation:
            // - If any test result is UNSET, and there are no explicit ERROR or CANCEL results, it implies the test didn't complete
            //   without erroring or cancelling, which shouldn't occur, but...
            if (GetResultCount(configTest.Tests, EventCodes.FAIL) > 0) return EventCodes.FAIL;
            // 5th priority evaluation:
            // - If there are no ERROR, CANCEL or UNSET results, but there is a FAIL result, UUT result is FAIL.

            // Else, handle oopsies!
            String validEvents = String.Empty, invalidTests = String.Empty;
            foreach (FieldInfo fi in typeof(EventCodes).GetFields()) validEvents += ((String)fi.GetValue(null), String.Empty);
            foreach (KeyValuePair<String, Test> t in configTest.Tests) {
                if (!validEvents.Contains(t.Value.Result)) invalidTests += $"ID: '{t.Key}' Result: '{t.Value.Result}'.{Environment.NewLine}";
            }
            if (!String.Equals(invalidTests, String.Empty)) throw new NotImplementedException($"Invalid Test ID(s) to Result(s):{Environment.NewLine}{invalidTests}");
            else throw new NotImplementedException("Sigh...unexpected Error.  Am clueless why.");
            // EventCodes was modified without updating EvaluateUUTResult; take Exception to that.
        }

        private static Int32 GetResultCount(Dictionary<String, Test> tests, String eventCode) {
            return (from t in tests where String.Equals(t.Value.Result, eventCode) select t).Count();
        }
    }
}
