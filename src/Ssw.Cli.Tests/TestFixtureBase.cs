using System;
using System.IO;
using NUnit.Framework;

namespace Ssw.Cli.Tests
{
    [TestFixture()]
    public abstract class TestFixtureBase
    {
        private static Lazy<string> TestDirectoryFetcher = new Lazy<string>(() => Path.GetDirectoryName((new Uri(typeof(TestFixtureBase).Assembly.CodeBase)).LocalPath));
        protected static string CurrentDir => TestDirectoryFetcher.Value;
    }
}