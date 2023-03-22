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
using System.IO;

using HDF.PInvoke;


namespace H52O
{
    /// <summary>
    /// Represents a Hdf5 file on disk, which contains the Root-Group.
    /// </summary>
    public class H5File : H5Base
    {
        public readonly string Path;
        public readonly string Mode;

        /// <summary>
        /// The Root-Group represents the head node of a .h5-file.
        /// All other Groups are created underneath this unique Group.
        /// </summary>
        public readonly H5Group Root;

        /// <summary>
        /// Check if the file at `path` is a readable HDF5 file.
        /// </summary>
        /// <remarks>
        /// Throws a `FileNotFoundException` if file does not exist.
        /// <remarks>
        public static bool IsReadable(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            return (H5F.is_hdf5(path) > 0);
        }

        /// <summary>
        /// Open a `.h5`-file, possibly creating one if it does not exist.
        /// </summary>
        /// <param name="mode">
        /// One of "r", "r+", "w", "x" for opening file in readonly-, read-write-, 
        /// write- or create-mode, respectively.
        /// When using HDF5 v1.10 and later, the modes "sw" and "mr" are also
        /// supported for single-write- and multi-read-mode, respectively.
        /// </param>
        /// <remarks>
        /// Trying to create an existing file with "x" will fail. An existing
        /// file opened with "w" will be overwritten.
        /// </remarks>
        public static H5File Open(string path, string mode)
        {
            H5Library.ResolvePathToExternalDependencies();

            if (!File.Exists(path))
            {
                if (mode == "r" || mode == "r+" || mode == "sw" || mode == "mr")
                    throw new FileNotFoundException(path);
            }
            else if (mode == "x" && File.Exists(path))
                throw new ApplicationException($"file exists: path");

            hid_t fid;
            hid_t fapl;
#if HDF5_VER1_8
            fapl = H5P.DEFAULT;
#else
            // set the library version bounds conservatively when using SWMR..
            fapl = H5P.create(H5P.FILE_ACCESS);
            H5P.set_libver_bounds(fapl, H5F.libver_t.LATEST, H5F.libver_t.LATEST);
#endif
            switch (mode)
            {
                case "r":
                    fid = H5F.open(path, H5F.ACC_RDONLY);
                    break;
                case "r+":
                    fid = H5F.open(path, H5F.ACC_RDWR);
                    break;
#if !HDF5_VER1_8
                case "mr":
                    fid = H5F.open(path, H5F.ACC_RDONLY | H5F.ACC_SWMR_READ);
                    break;
                case "sw":
                    fid = H5F.open(path, H5F.ACC_RDWR | H5F.ACC_SWMR_WRITE);
                    break;
#endif
                case "w":
                    fid = H5F.create(path, H5F.ACC_TRUNC, H5P.DEFAULT, fapl);
                    break;
                case "x":
                    fid = H5F.create(path, H5F.ACC_EXCL, H5P.DEFAULT, fapl);
                    break;
                default:
                    throw new ArgumentException($"unknown mode ({mode})");
            }
            if (fid < 0)
                throw new H5LibraryException($"H5F.open() returned ({fid})");

            return new H5File(fid, path, mode);
        }

        protected H5File(hid_t fid, string path, string mode)
            : base(fid)
        {
            Path = path;
            Mode = mode;
            Root = H5Group.Open(ID, key: "/", writable: mode != "r");
        }

        /// <summary>
        /// Open a H5Group at `location` relative to the hdf5 root group, 
        /// creating subgroups as necessary.
        /// </summary>
        public H5Group MakeGroups(string location)
        {
            location = location.Trim('/');

            H5Group tmp, node = null;
            foreach (string subloc in location.Split('/'))
            {
                if (node == null)
                    node = Root.SubGroup(subloc, create: true);
                else
                {
                    // note: if we could rely on the GC, we
                    // wouldn't need a temporary variable..
                    tmp = node.SubGroup(subloc, create: true);
                    node.Dispose();
                    node = tmp;
                }
            }
            // re-open the group for its '.Key' set to our 'location'..
            node.Dispose();
            node = Root.SubGroup(location);

            return node;
        }

        /// <summary>
        /// Close the file to release the file handle.
        /// </summary>
        public override void Dispose()
        {
            Root.Dispose();

            // force garbage collection..
            GC.Collect();
            // ..and wait for all finalizers to complete before continuing.
            // this helps to prevent a delayed closing of the hdf5-file by
            // calling the .close() method on all dangling H5Objects. 
            GC.WaitForPendingFinalizers();

            base.Dispose();

            if (ID > 0)
                ID = H5F.close(ID);
        }
    }
}
