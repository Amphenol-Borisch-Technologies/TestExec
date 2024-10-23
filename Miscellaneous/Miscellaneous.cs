using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ABT.Test.Exec {
    public class TimerMilliSeconds {
        private readonly Int32 _milliSeconds;
        private readonly DateTime _start;
        public TimerMilliSeconds(Int32 MilliSeconds) { _milliSeconds = MilliSeconds; _start = DateTime.Now; }

        public DateTime GetStart() { return _start; }
        public Int32 GetMilliSecondsTotal() { return _milliSeconds; }
        public Int32 GetMilliSecondsElapsed() { return (Int32)Math.Round((DateTime.Now - _start).TotalMilliseconds); }
        public Int32 GetMilliSecondsRemaining() { return Expired() ? 0 : _milliSeconds - GetMilliSecondsElapsed(); }
        public Boolean Expired() { return GetMilliSecondsElapsed() >= _milliSeconds; }
        public Boolean NotExpired() { return !Expired(); }
    }

    public class TimerSeconds {
        private readonly Double _seconds;
        private readonly DateTime _start;
        public TimerSeconds(Double Seconds) { _seconds = Seconds; _start = DateTime.Now; }

        public DateTime GetStart() { return _start; }
        public Double GetSecondsTotal() { return _seconds; }
        public Double GetSecondsElapsed() { return Math.Round((DateTime.Now - _start).TotalSeconds, 2); }
        public Double GetSecondsRemaining() { return Expired() ? 0 : _seconds - GetSecondsElapsed(); }
        public Boolean Expired() { return GetSecondsElapsed() >= _seconds; }
        public Boolean NotExpired() { return !Expired(); }
    }

    public static class TestEvents {
        public const String CANCEL = "CANCEL";
        public const String EMERGENCY_STOP = "EMERGENCY STOP";
        public const String ERROR = "ERROR";
        public const String FAIL = "FAIL";
        public const String PASS = "PASS";
        public const String UNSET = "UNSET";

        public static Color GetColor(String Event) {
            Dictionary<String, Color> codesToColors = new Dictionary<String, Color>() {
                { CANCEL, Color.Yellow },
                { EMERGENCY_STOP, Color.Firebrick },
                { ERROR, Color.Aqua },
                { FAIL, Color.Red },
                { PASS, Color.Green },
                { UNSET, Color.Gray }
            };
            return codesToColors[Event];
        }
    }

    public static class Ext { public static Boolean In<T>(this T value, params T[] values) where T : struct { return values.Contains(value); } }
}