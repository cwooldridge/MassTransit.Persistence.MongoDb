namespace MassTransit.Persistence.MongoDb.Tests
{
    using System.Diagnostics;
    using System.IO;

    using log4net.Config;

    using NUnit.Framework;

    [SetUpFixture]
    public class ContextSetup
    {
        [SetUp]
        public void Before_any()
        {
            Trace.WriteLine("Setting Up Log4net");

            string path = Path.GetDirectoryName(@"C:\\temp");

            string file = Path.Combine(path, "test.log4net.xml");

            XmlConfigurator.Configure(new FileInfo(file));
        }
    }
}