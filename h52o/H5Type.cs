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

using HDF.PInvoke;


namespace H52O
{
    public class H5Type : H5Base
    {
        internal H5Type(hid_t tid) : base(tid) { }

        /// <summary>
        /// Create a new H5Type from a native .NET primitive type.
        /// </summary>
        /// <param name="primitive_type">A primitive type such as `typeof(float)`.</param>
        /// <param name="size">(for `typeof(string)` only) The size of the allocated string.</param>
        public static H5Type Create(Type primitive_type, long size = 256)
        {
            if (primitive_type == typeof(System.Double))
                return new H5Type(H5T.copy(H5T.NATIVE_DOUBLE));

            else if (primitive_type == typeof(System.Single))
                return new H5Type(H5T.copy(H5T.NATIVE_FLOAT));

            else if (primitive_type == typeof(System.Byte))
                return new H5Type(H5T.copy(H5T.NATIVE_INT8));

            else if (primitive_type == typeof(System.Int32))
                return new H5Type(H5T.copy(H5T.NATIVE_INT32));

            else if (primitive_type == typeof(System.Int64))
                return new H5Type(H5T.copy(H5T.NATIVE_INT64));

            else if (primitive_type == typeof(System.String))
                return new H5Type(H5T.create(H5T.class_t.STRING, (IntPtr)size));

            else
                throw new NotImplementedException($"can't create H5Type with primitive type {primitive_type}");
        }

        /// <summary>
        /// Read the appropriate H5Type from a *dataset* with ID `dset_id`.
        /// </summary>
        public static H5Type FromDataset(hid_t dset_id)
        {
            return new H5Type(H5D.get_type(dset_id));
        }

        /// <summary>
        /// Read the appropriate H5Type from an *attribute* with ID `attr_id`.
        /// </summary>
        public static H5Type FromAttribute(hid_t attr_id)
        {
            return new H5Type(H5A.get_type(attr_id));
        }

        public long Size => (long)H5T.get_size(ID);

        public Type PrimitiveType
        {
            get
            {
                H5T.class_t class_;
                var size = (System.Int64)H5T.get_size(ID);  // size of atomic type in bytes
                if ((class_ = H5T.get_class(ID)) < 0)
                    throw new H5LibraryException($"H5T.get_class() returned ({class_})");

                if (class_ == H5T.class_t.STRING)
                    return typeof(System.String);

                else if (class_ == H5T.class_t.INTEGER)
                {
                    if (size == 8)
                        return typeof(System.Int64);

                    if (size == 4)
                        return typeof(System.Int32);

                    if (size == 1)
                        return typeof(System.Byte);
                }
                else if (class_ == H5T.class_t.FLOAT)
                {
                    if (size == 4)
                        return typeof(System.Single);

                    if (size == 8)
                        return typeof(System.Double);
                }
                throw new H5LibraryException($"can't interpret type with class-id ({H5T.get_class(ID)}) / size ({(System.Int64)H5T.get_size(ID)})");
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5T.close(ID);
        }
    }
}
