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
using System.Collections.Generic;       // KeyNotFoundException
using System.Runtime.InteropServices;   // GCHandle

using HDF.PInvoke;


namespace H5Ohm
{
    /// <summary>
    /// A small annotation to a hdf5 group or dataset.
    /// </summary>
    public class H5Attribute : H5Base
    {
        /// <summary>
        /// Load an existing hdf5 attribute by `attr_id`.
        /// </summary>
        /// <remarks>
        /// Creates and returns an `H5Attribute` of the appropriate `space` and `dtype`.
        /// Throws `H5LibraryException` if `attr_id` is invalid.
        /// </remarks>
        internal static H5Attribute FromID(hid_t attr_id)
        {
            return new H5Attribute(attr_id);
        }

        protected H5Attribute(hid_t attr_id) : base(attr_id) { }

        internal static hid_t Open(hid_t loc_id, string name)
        {
            return H5A.open(loc_id, name, H5P.DEFAULT);
        }

        internal static H5Attribute Create(hid_t loc_id, string name, Type primitive, object default_ = null)
        {
            if (primitive == null)
                primitive = default_.GetType();  // may throw NullReferenceException, which is informational enough

            int rank = 1;
            long[] dims = new long[1] { 1 };
            hid_t id;
            using (H5Space space = H5Space.Create(rank, dims))
            using (H5Type dtype = H5Type.Create(primitive))
            {
                if ((id = H5A.create(loc_id, name, dtype.ID, space.ID, H5P.DEFAULT, H5P.DEFAULT)) < 0)
                    throw new H5LibraryException($"H5A.create() returned ({id})");
            }
            H5Attribute rv = FromID(id);
            if (default_ != null)
                rv.Write(default_);

            return rv;
        }

        public static bool Exists(hid_t loc_id, string name)
        {
            int rv;
            if ((rv = H5A.exists(loc_id, name)) < 0)
                throw new H5LibraryException($"H5L.exists returned ({rv})");

            return (rv > 0);
        }

        H5Type GetDType() { return H5Type.FromAttribute(ID); }

        /// <summary>
        /// The primitive Type (e.g. `float`) of the stored data.
        /// </summary>
        public Type PrimitiveType  { get { using (var dtype = GetDType()) { return dtype.PrimitiveType; } } }

        /// <summary>
        /// Read the string value stored in this *attribute*.
        /// </summary>
        public string Reads()
        {
            using (H5Type dtype = GetDType())
            {
                if (typeof(System.String) != dtype.PrimitiveType)
                    throw new InvalidCastException(dtype.PrimitiveType.ToString());

                long slen = dtype.Size;
                byte[] buf = new byte[slen];
                GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                int status = H5A.read(ID, dtype.ID, pinnedArray.AddrOfPinnedObject());
                pinnedArray.Free();
                if (status < 0)
                    throw new H5LibraryException($"H5A.read() returned ({status})");

                string decoded = System.Text.Encoding.ASCII.GetString(buf);

                return decoded.TrimEnd('\0');
            }
        }

        /// <summary>
        /// Store a string in this *attribute*.
        /// </summary>
        public void Writes(string value)
        {
            using (H5Type dtype = GetDType())
            {
                if (typeof(System.String) != dtype.PrimitiveType)
                    throw new InvalidCastException(dtype.PrimitiveType.ToString());

                long slen = dtype.Size;
                if (value.Length > slen - 1)
                    throw new IndexOutOfRangeException($"string longer than ({slen})");

                byte[] buf = new byte[slen];
                Array.Copy(string1d.Enc.GetBytes(value), buf, value.Length);
                GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                int status = H5A.write(ID, dtype.ID, pinnedArray.AddrOfPinnedObject());
                pinnedArray.Free();
                if (status < 0)
                    throw new H5LibraryException($"H5A.write() returned ({status})");
            }
        }

        /// <summary>
        /// Read the numeric value stored in this *attribute* specified by `T`.
        /// </summary>
        /// <remarks>
        /// For reading a `string`, use `Reads()` instead!
        /// </remarks>
        public T Read<T>()
        {
            using (H5Type dtype = GetDType())
            {
                if (typeof(T) != dtype.PrimitiveType)
                    throw new InvalidCastException(dtype.PrimitiveType.ToString());

                T[] buf = new T[1];
                GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                int status = H5A.read(ID, dtype.ID, pinnedArray.AddrOfPinnedObject());
                pinnedArray.Free();
                if (status < 0)
                    throw new H5LibraryException($"H5A.read() returned ({status})");

                return buf[0];
            }
        }

        /// <summary>
        /// Store a numeric value in this *attribute*.
        /// </summary>
        /// <remarks>
        /// For writing a `string`, use `Writes()` instead!
        /// </remarks>
        public void Write<T>(T value)
        {
            using (H5Type dtype = GetDType())
            {
                if (typeof(T) != dtype.PrimitiveType)
                    throw new InvalidCastException(dtype.PrimitiveType.ToString());

                T[] buf = new T[1] { value };
                GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                int status = H5A.write(ID, dtype.ID, pinnedArray.AddrOfPinnedObject());
                pinnedArray.Free();
                if (status < 0)
                    throw new H5LibraryException($"H5A.write() returned ({status})");
            }
        }

        /// <summary>
        /// Read the value stored in this *attribute* and box it as an `object`.
        /// </summary>
        public object Read()
        {
            using (H5Type dtype = GetDType())
            {
                if (dtype.PrimitiveType == typeof(System.String)) return Reads();
                if (dtype.PrimitiveType == typeof(System.Double)) return Read<double>();
                if (dtype.PrimitiveType == typeof(System.Single)) return Read<float>();
                if (dtype.PrimitiveType == typeof(System.Byte)) return Read<byte>();
                if (dtype.PrimitiveType == typeof(System.Int64)) return Read<long>();
                if (dtype.PrimitiveType == typeof(System.Int32)) return Read<int>();

                throw new NotImplementedException(dtype.PrimitiveType.ToString());
            }
        }

        /// <summary>
        /// Store a `value` in this *attribute*. The `object` is unboxed to this attribute's
        /// `.PrimitiveType`. This may throw an `InvalidCastException`.
        /// </summary>
        public void Write(object value)
        {
            using (H5Type dtype = GetDType())
            {
                if (dtype.PrimitiveType == typeof(System.String)) Writes((string)value);
                else if (dtype.PrimitiveType == typeof(System.Double)) Write((double)value);
                else if (dtype.PrimitiveType == typeof(System.Single)) Write((float)value);
                else if (dtype.PrimitiveType == typeof(System.Byte)) Write((byte)value);
                else if (dtype.PrimitiveType == typeof(System.Int64)) Write((long)value);
                else if (dtype.PrimitiveType == typeof(System.Int32)) Write((int)value);
                else
                    throw new NotImplementedException(dtype.PrimitiveType.ToString());
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5A.close(ID);
        }
    }
}
