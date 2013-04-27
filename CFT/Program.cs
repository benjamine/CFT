using System;
using System.Diagnostics;
using CLAP;

namespace BlogTalkRadio.Tools.CFT
{
    class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            // log to console by default
            Trace.AutoFlush = true;
            var traceListener = new ConsoleTraceListener();
            traceListener.TraceOutputOptions = TraceOptions.None;
            Trace.Listeners.Add(traceListener);

            Parser.RunConsole<App>(args);
        }
    }
}
