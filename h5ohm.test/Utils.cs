using System;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;

using Xunit;

using H5Ohm;

namespace TestUtils
{
    class Helpers
    {
        static string demodata = System.IO.Path.GetFullPath("../../demodata/");

        public static string GetDemodataDir()
        {
            Assert.True(System.IO.Directory.Exists(demodata));

            return demodata;
        }

        public static string GetTempDirectory(string dirName = "")
        {
            if (dirName == string.Empty)
                dirName = Path.GetRandomFileName();

            string path = Path.Combine(Path.GetTempPath(), dirName);
            Directory.CreateDirectory(path);

            return path;
        }

    }

    /// <summary>
    /// Creates a temporary H5File instance on disk, which is accessible by `.Content()`.
    /// </summary>
    public class TempH5FileContainer : IDisposable
    {
        H5File hf;
        public TempH5FileContainer(string filemode = "w")
        {
            string temp_path = System.IO.Path.GetTempFileName();
            hf = H5File.Open(temp_path, mode: "w");
            // close..
            hf.Dispose();
            // ..to reopen:
            hf = H5File.Open(temp_path, mode: filemode);
        }

        public H5File Content() { return hf; }

        public void Dispose()
        {
            string path = hf.Path;
            hf.Dispose();

            File.Delete(path);
        }
    }
}
