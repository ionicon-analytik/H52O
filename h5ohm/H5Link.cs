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
using System.Collections.Generic;  // KeyNotFoundException

using HDF.PInvoke;


namespace H5Ohm
{
    public class H5Link
    {
        /// <summary>
        /// Convenience function to verify that a Hdf5 link exists. Fails on attributes.
        /// </summary>
        /// <param name="id">Hdf5 loc_id of the object.</param>
        /// <param name="key">Slashed path name relative to `id`.</param>
        public static bool Exists(hid_t id, string key)
        {
            if (!(id > 0))
                throw new InvalidOperationException($"invalid id ({id}");

            key = key.TrimStart('/');   // ignore the root-group (see note on behaviour change:
                                        // https://portal.hdfgroup.org/display/HDF5/H5L_EXISTS)
            int rv, slash = 0;
            string subkey;
            while (slash < key.Length)
            {
                slash = 1 + key.IndexOf('/', slash);
                if (slash == 0)
                    slash = key.Length;
                subkey = key.Substring(0, slash);
                if ((rv = H5L.exists(id, subkey)) < 0)
                    throw new H5LibraryException($"H5L.exists returned ({rv})");

                if (!(rv > 0))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Convenience function for deleting a hdf5 link. 
        /// </summary>
        /// <remarks>
        /// This may permanently remove the referenced hdf5 object from the
        /// file if the reference count reaches zero. See
        /// https://portal.hdfgroup.org/display/HDF5/H5L_DELETE for more info.
        /// </remarks>
        public static void Delete(hid_t id, string key)
        {
            if (!Exists(id, key))
                throw new KeyNotFoundException(key);

            H5L.delete(id, key, H5P.DEFAULT);
        }
    }
}
