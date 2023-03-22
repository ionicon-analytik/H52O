/* Copyright (c) 2020 lefi7z
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Security;
using HDF.PInvoke;


namespace H52O
{
    /// <summary>
    /// Represents a Hdf5 ID to identify Hdf5 objects.
    /// </summary>
    public struct hid_t
    {
#if HDF5_VER1_8
        private System.Int32 id;
#else
        private System.Int64 id;
#endif

#if HDF5_VER1_8
        public static implicit operator Int32(hid_t id)
#else
        public static implicit operator Int64(hid_t id)
#endif
        {
            return id.id;
        }

#if HDF5_VER1_8
        public static implicit operator hid_t(Int32 id)
#else
        public static implicit operator hid_t(Int64 id)
#endif
        {
            return new hid_t() { id = id };
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    /// <summary>
    /// Represents a C-like size type.
    /// </summary>
    public struct size_t
    {
        private System.Int64 val;

        public static implicit operator Int64(size_t s)
        {
            return s.val;
        }

        public static implicit operator IntPtr(size_t s)
        {
            return (IntPtr)s.val;
        }

        public static implicit operator size_t(Int64 val)
        {
            return new size_t() { val = val };
        }

        public override string ToString()
        {
            return val.ToString();
        }
    }

    /// <summary>
    /// The exception that is thrown on any Hdf5 library error.
    /// </summary>
    public class H5LibraryException : ApplicationException
    {
        public H5LibraryException(string msg) : base(msg) { }
    }

    public class H5Library
    {
        /// <summary>
        /// Return the Hdf5 library version of the loaded hdf5.dll as a string.
        /// </summary>
        public static string LibVersion
        {
            get
            {
                uint major = 0, minor = 0, patch = 0;
                H5.get_libversion(ref major, ref minor, ref patch);
                
                return $"{major}.{minor}.{patch}";
            }
        }

        /// <summary>
        /// Return whether the HDF5 library was built with thread-safety enabled.
        /// </summary>
        /// <remarks>
        /// This function is not available for hdf5 version < 1.8.16 !
        /// </remarks>
        public static bool IsThreadSafe
        {
            get
            {
                uint major = 0, minor = 0, patch = 0;
                H5.get_libversion(ref major, ref minor, ref patch);
                if (minor <= 8 && patch < 16)
                    throw new NotImplementedException("H5.is_library_threadsafe()");

                uint rv = 0;
                H5.is_library_threadsafe(ref rv);

                return (rv == 1) ? true : false;
            }
        }

        public static string Info
        {
            get
            {
                string thread_safe;
                uint major = 0, minor = 0, patch = 0;
                H5.get_libversion(ref major, ref minor, ref patch);
                if (minor <= 8 && patch < 16)
                    thread_safe = "unknown";
                else
                    thread_safe = IsThreadSafe.ToString();

                return $"hdf5 v{LibVersion}, thread_safety_enabled = {thread_safe}";
            }
        }

        /// <summary>
        /// Close the library, flush all buffers, close all file handles.
        /// </summary>
        public static void Close()
        {
            H5.close();
#if DEBUG
            H5Base.nObjects = 0;
#endif
            Debug.WriteLine("closing hdf5 library... goodbye.");
        }

        // Note: The following is copy'n'pasted from the HDF.Pinvoke library, because
        //  calling this function there seems to have no effect. Adds ./bin64 to PATH:

        internal static void ResolvePathToExternalDependencies()
        {
            GetDllPathFromAssembly(out string NativeDllPath);

            AddPathStringToEnvironment(NativeDllPath);
        }

        private static string GetAssemblyName()
        {
            string myPath = new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            myPath = Uri.UnescapeDataString(myPath);
            return myPath;
        }

        private static void GetDllPathFromAssembly(out string aPath)
        {
            switch (IntPtr.Size)
            {
                case 8:
                    aPath = Path.Combine(Path.GetDirectoryName(GetAssemblyName()), "bin64");
                    break;
                case 4:
                    aPath = Path.Combine(Path.GetDirectoryName(GetAssemblyName()), "bin32");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private static void AddPathStringToEnvironment(string aPath)
        {
            try
            {
                string EnvPath = Environment.GetEnvironmentVariable("PATH");
                if (EnvPath.Contains(aPath))
                    return;

                Environment.SetEnvironmentVariable("PATH", aPath + ";" + EnvPath);
                Trace.WriteLine(string.Format("{0} added to Path.", aPath));
            }
            catch (SecurityException)
            {
                Trace.TraceError("Changing PATH not allowed");
            }
        }

    }
}
