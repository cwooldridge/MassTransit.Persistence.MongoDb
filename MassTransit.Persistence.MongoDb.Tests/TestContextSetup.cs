using System;
using System.Reflection;

using Serilog;

namespace MassTransit.Persistence.MongoDb.Tests
{
    using System.Diagnostics;
    using System.IO;

    using NUnit.Framework;

    [SetUpFixture]
    public class TestContextSetup
    {
        [SetUp]
        public void SetupLogging()
        {
            var logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Assembly.GetExecutingAssembly().GetName().Name + ".log");

            Trace.WriteLine("Serilog Logging to File: " + logFile);

            Log.Logger =
                new LoggerConfiguration().MinimumLevel.Verbose()
                    .WriteTo.Trace()
                    .WriteTo.ColoredConsole()
                    .WriteTo.RollingFile(logFile)
                    .CreateLogger();
        }
    }
}