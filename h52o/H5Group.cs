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
using System.Linq;
using System.Collections.Generic;       // KeyNotFoundException
using System.Runtime.InteropServices;   // Marshal.PtrToString

using HDF.PInvoke;


namespace H52O
{
    /// <summary>
    /// Contains the Hdf5 hierarchy of datasets and a sub-Groups.
    /// </summary>
    public class H5Group : H5Base
    {
        public readonly string Key;
        public readonly bool IsWritable;

        public string Name => Key.Split('/').Last();

        private H5Group(hid_t loc_id, string key, bool writable)
            : base(loc_id)
        {
            Key = key;
            IsWritable = writable;
        }

        internal static H5Group Open(hid_t loc_id, string key, bool writable)
        {
            return new H5Group(H5G.open(loc_id, key), key, writable);
        }

        private H5Group Create(hid_t loc_id, string key)
        {
            if (!IsWritable)
                throw new InvalidOperationException("trying to write a readonly file");

            return new H5Group(H5G.create(loc_id, key), key, IsWritable);
        }

        /// <summary>
        /// Retrieve the `H5DataSet` located by the `key` relative to this Group.
        /// The type of dataset returned depends on the actual datatype under
        /// the `key`. A non-existing `key` throws a `KeyNotFoundException`.
        /// </summary>
        /// <remarks>
        /// In order to use the object, an explicit cast to one of the generic 
        /// datasets is necessary. Beware of casting as it might throw an
        /// `InvalidCastException`.  
        /// </remarks>
        /// <example>
        /// <code>
        /// H5Group root = h5file.Root;
        /// double2d dset = (double2d)root["level1/doubleset"];
        /// string1d names = root["level1/names"] as string1d;
        /// if (names == null)
        ///     throw new ApplicationException("no such dataset: level1/names!");
        /// </code>
        /// </example>
        public H5DataSet this[string key]
        {
            get
            {
                if (key.StartsWith("/") || key.EndsWith("/") || !H5Link.Exists(ID, key))
                    throw new KeyNotFoundException(key);

                if (!H5Link.Exists(ID, key))
                    throw new KeyNotFoundException(key);

                return H5DataSet.FromID(H5DataSet.Open(ID, key));
            }
        }

        /// <summary>
        /// Create a new dataset with a given `name` under this group. 
        /// An attempt to write a readonly-file will throw an `InvalidOperationException`.
        /// </summary>
        public H5DataSet CreateDataset(string name, int rank, long[] dims, Type primitive, long[] maxdims = null)
        {
            if (!IsWritable)
                throw new InvalidOperationException("trying to write a readonly file");

            return H5DataSet.Create(ID, name, rank, dims, maxdims, primitive);
        }

        /// <summary>
        /// Access the sub-Group given by `key` relative to this Group.
        /// If the sub-Group does not exist and `create = false` (default),
        /// a `KeyNotFoundException` is thrown.
        /// An attempt to create multiple groups or to write a readonly-file 
        /// will throw an `InvalidOperationException`.
        /// </summary>
        public H5Group SubGroup(string key, bool create = false)
        {
            if (H5Link.Exists(ID, key))
                return H5Group.Open(ID, key, IsWritable);

            if (create)
                return CreateGroup(key);

            else
                throw new KeyNotFoundException(key);
        }

        /// <summary>
        /// Access all direct sub-groups of this group. This enumerates
        /// `H5Group` instances. Each instance will be opened just for the
        /// duration of the visit and disposed of, when the next item is
        /// requested.
        /// </summary>
        public IEnumerable<H5Group> SubGroups()
        {
            H5Group current;
            foreach (string name in CollectObjects(H5O.type_t.GROUP))
                using (current = SubGroup(name, create: false))
                    yield return current;
        }

        private string[] CollectObjects(H5O.type_t type)
        {
            const int CONTINUE = 0;
            int status;
            List<string> rv = new List<string>();

            // the callback function, called for each item in the iteration
#if HDF5_VER1_10
            H5L.iterate_t op_fun = (long loc, IntPtr name, ref H5L.info_t info, IntPtr op_data) =>
#else
            H5L.iterate_t op_fun = (int loc, IntPtr name, ref H5L.info_t info, IntPtr op_data) =>
#endif
            {
                H5O.info_t oinfo = new H5O.info_t();
                var bname = Marshal.PtrToStringAnsi(name);
                if ((status = H5O.get_info_by_name(loc, bname, ref oinfo, H5P.DEFAULT)) < 0)
                    return status;

                if (oinfo.type == type)
                    rv.Add(bname);

                return CONTINUE;
            };
            ulong tracking_index = 0;
            if ((status = H5L.iterate(ID, H5.index_t.NAME, H5.iter_order_t.NATIVE, ref tracking_index, op_fun, IntPtr.Zero)) < 0)
                throw new H5LibraryException($"H5Literate() returned {status}");

            return rv.ToArray();
        }

        /// <summary>
        /// Create a sub-Group with a given `key` relative to this Group.
        /// An attempt to create multiple groups or to write a readonly-file 
        /// will throw an `InvalidOperationException`.
        /// </summary>
        /// <remarks>
        /// Groups can only be created one at a time. To create a hierarchy
        /// like "/one/two/three" under the Root-Group in an empty file, call
        /// <code>
        /// H5Group grp = h5file.Root;
        /// grp = grp.CreateGroup("one");
        /// grp = grp.CreateGroup("two");
        /// grp = grp.CreateGroup("three");
        /// </code>
        /// Afterwards, the H5Group *three* is accessible through
        /// <code>
        /// H5Group three = h5file.Root.SubGroup("one/two/three");
        /// </code>
        /// </remarks>
        public H5Group CreateGroup(string key)
        {
            if (H5Link.Exists(ID, key) || key.Contains("/"))
                throw new InvalidOperationException($"Cannot create group at <{key}>");

            return Create(ID, key);
        }

        /// <summary>
        /// Remove the link to the sub-group specified by `key` from this group.
        /// Throws a `KeyNotFoundException` if the given `key` does not exist.
        /// </summary>
        public void DeleteGroup(string key)
        {
            H5Link.Delete(ID, key);
        }

        /// <summary>
        /// Return the hdf5 *attribute* by `name` of this group.
        /// </summary>
        public H5Attribute GetAttribute(string name)
        {
            if (!H5Attribute.Exists(ID, name))
                throw new KeyNotFoundException(name);

            return H5Attribute.FromID(H5Attribute.Open(ID, name));
        }

        /// <summary>
        /// Add a hdf5 *attribute* to this group. The `Type` argument is mandatory.
        /// If a `default_` value is given, it is written to the hdf5 file immediately.
        /// Note, that an existing *attribute* will not be overwritten.
        /// </summary>
        public H5Attribute SetAttribute(string name, Type primitive, object default_ = null)
        {
            if (H5Attribute.Exists(ID, name))
                throw new InvalidOperationException($"attribute exists ({name})");

            return H5Attribute.Create(ID, name, primitive, default_);
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5G.close(ID);
        }
    }
}
