//#define AVOID_H5LIBRARY_ERRORS

using System;
using System.IO;

using Xunit;

using TestUtils;

using H5Ohm;



namespace H5Ohm.Test
{
    public class TestFile
    {
        public class OpenFile
        {
            static string demodata = System.IO.Path.GetFullPath("../../demodata/");

            // opening a file, there are 4 possibilities..
            //
            //     path exists  |  create   |  effect
            // -------------------------------
            // 1)    no         |   true    |  -> create file
            // 2)    no         |   false   |  ! FileNotFoundException
            // 3)    yes        |   false   |  -> open file
            // 4)    yes        |   true    |  ! ApplicationException (File Exists)

            [Fact]
            public void CreateAndRead()
            {
                string path = Path.GetTempFileName();
                File.Delete(path);
                Assert.False(File.Exists(path));

                H5File FILE = null;

                // 1) create..
                FILE = H5File.Open(path, mode: "x");

                Assert.NotNull(FILE.Root);
                Assert.True(FILE.Root.IsWritable);
                
                FILE.Dispose();

                // 1a) truncate..
                FILE = H5File.Open(path, mode: "w");
                
                Assert.NotNull(FILE.Root);
                Assert.True(FILE.Root.IsWritable);
                
                FILE.Dispose();

                // 2a) ..reopen in r mode..
                FILE = H5File.Open(path, mode: "r");
                
                Assert.NotNull(FILE.Root);
                Assert.False(FILE.Root.IsWritable);

                FILE.Dispose();

                // 2b) ..and in r+ mode..
                FILE = H5File.Open(path, mode: "r+");
                
                Assert.NotNull(FILE.Root);
                Assert.True(FILE.Root.IsWritable);

                FILE.Dispose();

                // cleanup..
                File.Delete(path);
            }

            [Fact]
            public void OpenNonExistingFails()
            {
                string path = Path.Combine(demodata, "aeroiyu359hnfna.soie");

                Assert.False(File.Exists(path));

                Assert.Throws<FileNotFoundException>(() => H5File.Open(path, mode: "r"));
                Assert.Throws<FileNotFoundException>(() => H5File.Open(path, mode: "r+"));
            }

            [Fact]
            public void OpenExisting()
            {
                string path = Path.GetTempFileName();

                H5File FILE0 = H5File.Open(path, mode: "w");

                FILE0.Dispose();

                H5File FILE1 = H5File.Open(path, mode: "r");

                FILE1.Dispose();

                H5File FILE2 = H5File.Open(path, mode: "r+");

                FILE2.Dispose();

                File.Delete(path);
            }

#if AVOID_H5LIBRARY_ERRORS
            [Fact(Skip="avoiding test-cases that provoke hdf5 library errors to get a clean result")]
#else
            [Fact]
#endif
            public void ReOpenFails()
            {
                string path = Path.GetTempFileName();

                H5File FILE0 = H5File.Open(path, mode: "w");

                FILE0.Dispose();

                H5File FILE = H5File.Open(path, mode: "r");

                Assert.Throws<H5LibraryException>(() => H5File.Open(path, mode: "r+"));
                Assert.Throws<H5LibraryException>(() => H5File.Open(path, mode: "w"));

                FILE.Dispose();

                File.Delete(path);
            }

            [Fact]
            public void OverwriteExisting()
            {
                string path = Path.GetTempFileName();

                Assert.True(File.Exists(path));

                // A) truncate file works..
                H5File hf = H5File.Open(path, mode: "w");

                hf.Dispose();

                // B) ..but x-mode will fail on existing file:
                Assert.Throws<ApplicationException>(() => H5File.Open(path, mode: "x"));

                File.Delete(path);
            }
        }

        public class CloseFile
        {
            // this is not as trivial as it may sound:
            //  - - - - - - - - - - - - - - - - - - - -  
            // "If this is the last file identifier open for the file and no other
            // access identifier is open (e.g., a dataset identifier, group
            // identifier, or shared datatype identifier), the file will be fully
            // closed and access will end.
            //
            // Delayed close: 
            // Note the following deviation from the above-described behavior. If H5F_CLOSE is
            // called for a file but one or more objects within the file remain open, those
            // objects will remain accessible until they are individually closed. Thus, if the
            // dataset data_sample is open when H5F_CLOSE is called for the file containing
            // it, data_sample will remain open and accessible (including writable) until it
            // is explicitly closed.The file will be automatically closed once all objects in
            // the file have been closed. "
            // (see: https://portal.hdfgroup.org/display/HDF5/H5F_CLOSE)

            [Fact]
            public void CloseEmptyFile()
            {
                string path = Path.GetTempFileName();

                H5File FILE = H5File.Open(path, mode: "w");

                Assert.True(FILE.ID > 0);

                FILE.Dispose();

                Assert.Equal((hid_t)0, FILE.ID);

                // trying to re-open is a surefire way to check if FILE was closed correctly:
                H5File FILE2 = H5File.Open(path, mode: "w");
                FILE2.Dispose();
                
                File.Delete(path);

                Assert.False(File.Exists(FILE.Path));
            }
        }
    }
}
