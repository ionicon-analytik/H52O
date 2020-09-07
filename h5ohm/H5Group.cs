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


namespace H5Ohm
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

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5G.close(ID);
        }
    }
}
