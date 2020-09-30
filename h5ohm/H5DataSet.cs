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
using System.Text;          // Encoding
using System.Linq;          // Any
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;  // GCHandle
using System.Threading.Tasks;

using HDF.PInvoke;

using H5Ohm.Extensions;


namespace H5Ohm
{
    /// <summary>
    /// Container for multidimensional generic Data. 
    /// </summary>
    /// <remarks>
    /// The abstract `H5DataSet` specializes in the number of dimensions
    /// (its `Rank`) as well as its atomic datatype, which is given as
    /// a template parameter in acute braces. There is a subclass for
    /// each number of dimensions, because these behave differently in
    /// regard to how the data can be accessed and iterated over. For 
    /// example, the `dset2d<T>` supports multidimensional indexing, 
    /// while `dset1d<T>` does not.
    /// </remarks>
    public abstract class H5DataSet : H5Attributable
    {
        /// <summary>
        /// The number of dimensions. 
        /// </summary>
        public int Rank { get { using (var space = GetSpace()) { return space.Rank; } } }

        /// <summary>
        /// The extent in each dimension. 
        /// </summary>
        public long[] Dims { get { using (var space = GetSpace()) { return space.Dims; } } }

        /// <summary>
        /// The maximum resizable extent in each dimension. 
        /// </summary>
        public long[] MaxDims { get { using (var space = GetSpace()) { return space.MaxDims; } } }

        /// <summary>
        /// The total number of items. 
        /// </summary>
        public long Length { get { using (var space = GetSpace()) { return space.Length; } } }

        /// <summary>
        /// The primitive Type (e.g. `float`) of the stored data.
        /// </summary>
        public Type PrimitiveType  { get { using (var dtype = GetDType()) { return dtype.PrimitiveType; } } }

        // beware of changes in the hdf5 ABI..
        //  Die hdf5.dll forciert einen Versions-Check, der nur dieselbe
        //  Patch-Version zulaesst. Durch setzen der Umgebungsvariable set
        //  HDF5_DISABLE_VERSION_CHECK = 1 kann der Check auf eigene Gefahr
        //  (data corruption!) ausgeschaltet werden.
        //
        //  Es gibt einen ABI-Tracker mit einer Vergleichstabelle. Demnach ist
        //  1.8.21 zu 98% mit 1.8.12 kompatibel.Die Unterschiede betreffen nur
        //  Symbole wie H5P_CLS_ATTRIBUTE_CREATE_g ~>
        //  H5P_CLS_ATTRIBUTE_CREATE_ID_g.
        //
        //  Das Ausfuehren der Unit-Tests von HDF.PInvoke mit der
        //  untergeschobenen hdf5-1.8.12.dll geht dennoch unglaublich schief:
        //
        //   System.Exception: The export with name "H5P_CLS_ROOT_ID_g" doesn't
        //   exist..
        //
        //  Die Versionsaenderung betrifft saemtliche Konstanten in
        //  HDF.PInvoke/HDF5/H5Pglobal.cs, aber nicht in
        //  HDF.PInvoke/HDF5/H5Ppublic.cs. Letztere definiert H5P.DEFAULT,
        //  sodass die meisten Operationen, die keine fortgeschrittenen
        //  property-lists benoetigen, auch mit der hdf5-1.8.12.dll
        //  funktionieren sollten.
        static bool HasH5Pcreate
        {
            get
            {
                uint major = 0, minor = 0, patch = 0;
                H5.get_libversion(ref major, ref minor, ref patch);

                return !(minor <= 8 && patch < 16);
            }
        }

        /// <summary>
        /// Change the dimensions of the dataset to `new_dims`.
        /// </summary>
        public void Resize(params long[] new_dims)
        {
            if (!HasH5Pcreate)
                throw new NotImplementedException($"cannot resize using hdf5 v{H5Library.LibVersion}");

            using (H5Space space = GetSpace())
            {
                if (new_dims.Length != space.Rank)
                    throw new RankException($"{new_dims.Length} != {space.Rank}");

                ulong[] extent = new ulong[space.Rank];
                long[] maxdims = space.MaxDims;
                for (int i = 0; i < new_dims.Length; i++)
                {
                    if (!(0 <= new_dims[i] && (new_dims[i] <= maxdims[i] || maxdims[i] == H5Space.Unlimited)))
                        throw new IndexOutOfRangeException($"{new_dims[i]} > {maxdims[i]}");

                    extent[i] = (ulong)new_dims[i];
                }
                int status;
                if ((status = H5D.set_extent(ID, extent)) < 0)
                    throw new H5LibraryException($"H5D.set_extent() returned ({status})");
            }
        }

        internal static hid_t Open(hid_t loc_id, string key)
        {
            return H5D.open(loc_id, key, H5P.DEFAULT);
        }

        /// <summary>
        /// Create a new hdf5 `H5DataSet` at `loc_id`.
        /// </summary>
        /// <remarks>
        /// `maxdims` may be `null` in which case it is set to `dims`.
        /// </remarks>
        internal static H5DataSet Create(hid_t loc_id, string key, int rank, long[] dims, long[] maxdims, Type primitive_type)
        {
            hid_t dcpl;  // the 'dataset creation property list' controls chunking..
            if (maxdims == null || dims.SequenceEqual(maxdims))
            {
                dcpl = H5P.DEFAULT;
            }
            else if (HasH5Pcreate)
            {
                // ..which is needed for later resizing:
                var chunk = new ulong[rank];
                // the chunk is of size 1 in each 'unlimited' dimension and of size 'maxdims'
                // for all other dimensions (just like the 'SPECdata/Intensities' dataset):
                for (int i = 0; i < rank; i++)
                {
                    if (maxdims[i] == H5Space.Unlimited)
                        chunk[i] = 1UL;
                    else if (maxdims[i] > 0)
                        checked { chunk[i] = (ulong)maxdims[i]; }
                    else
                        throw new ArgumentException($"invalid value in parameter 'maxdims'");
                }
                dcpl = H5P.create(H5P.DATASET_CREATE);
                H5P.set_chunk(dcpl, rank, chunk);
            }
            else
            {
                maxdims = dims;
                dcpl = H5P.DEFAULT;
            }
            hid_t id;
            using (H5Space space = H5Space.Create(rank, dims, maxdims))
            using (H5Type dtype = H5Type.Create(primitive_type))
            {
                if ((id = H5D.create(loc_id, key, dtype.ID, space.ID, H5P.DEFAULT, dcpl, H5P.DEFAULT)) < 0)
                    throw new H5LibraryException($"H5D.create() returned ({id})");
            }
            return FromID(id);
        }

        /// <summary>
        /// Load an existing dataset by `dset_id`.
        /// </summary>
        /// <remarks>
        /// Creates and returns an `H5DataSet` of the appropriate `space` and `dtype`.
        /// Throws `H5LibraryException` if `dset_id` is invalid.
        /// </remarks>
        internal static H5DataSet FromID(hid_t dset_id)
        {
            using (H5Space space = H5Space.FromDataset(dset_id)) {
                using (H5Type dtype = H5Type.FromDataset(dset_id)) {
                    if (space.Rank == 1)
                    {
                        if (dtype.PrimitiveType == typeof(System.Double)) return new dset1d<double>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Single)) return new dset1d<float>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Byte)) return new dset1d<byte>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int64)) return new dset1d<long>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int32)) return new dset1d<int>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.String)) return new string1d(dset_id);
                    }
                    if (space.Rank == 2)
                    {
                        if (dtype.PrimitiveType == typeof(System.Double)) return new dset2d<double>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Single)) return new dset2d<float>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Byte)) return new dset2d<byte>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int64)) return new dset2d<long>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int32)) return new dset2d<int>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.String)) return new string2d(dset_id);
                    }
                    if (space.Rank == 3)
                    {
                        if (dtype.PrimitiveType == typeof(System.Double)) return new dset3d<double>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Single)) return new dset3d<float>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Byte)) return new dset3d<byte>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int64)) return new dset3d<long>(dset_id);
                        if (dtype.PrimitiveType == typeof(System.Int32)) return new dset3d<int>(dset_id);
                    }
                    throw new NotImplementedException($"dataset<{dtype.PrimitiveType}> of rank ({space.Rank})");
                }
            }
        }

        protected H5DataSet(hid_t hid) : base(hid) { }

        protected H5Space GetSpace()
        {
            if (!(ID > 0))
                throw new InvalidOperationException("operation on closed dataset");

            return H5Space.FromDataset(ID);
        }

        protected H5Type GetDType()
        {
            if (!(ID > 0))
                throw new InvalidOperationException("operation on closed dataset");

            return H5Type.FromDataset(ID);
        }

        /// <summary>
        /// Get the value at `index`.
        /// </summary>
        /// <remark>
        /// The number of indices must match this dataset's rank.
        /// </remark>
        public T Get<T>(params long[] index)
        {
            using (H5Space file_space = SelectPoint(index)) {
                using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                    using (H5Type dtype = GetDType()) {
                        T[] buf = new T[1];
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                              H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.read() returned ({status})");

                        return buf[0];
                    }
                }
            }
        }

        /// <summary>
        /// Get the value at `index`.
        /// </summary>
        /// <remark>
        /// The number of indices must match this dataset's rank.
        /// </remark>
        public Task<T> GetAsync<T>(params long[] index)
        {
            return Task.Run(() => Get<T>(index));
        }

        /// <summary>
        /// Set the `value` at `index`.
        /// </summary>
        /// <remark>
        /// The number of indices must match this dataset's rank.
        /// </remark>
        public void Set<T>(T value, params long[] index)
        {
            using (H5Space file_space = SelectPoint(index)) {
                using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                    using (H5Type dtype = GetDType()) {
                        T[] buf = new T[1];
                        buf[0] = value;
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                               H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.write() returned ({status})");
                    }
                }
            }
        }

        /// <summary>
        /// Create an `H5Space` with exactly 1 point selected by `index`.
        /// </summary>
        /// <remarks>
        /// The number of indices must match the rank of the dataset.
        /// </remarks>
        protected H5Space SelectPoint(params long[] index)
        {
            var space = GetSpace();
            if (index.Length != space.Rank)
                throw new ArgumentException("dimension mismatch");

            for (int i = 0; i < space.Rank; i++)
                if (index[i] < 0 || index[i] >= space.Dims[i])
                    throw new IndexOutOfRangeException("index was outside the bounds of the dataset");

            var coord = new ulong[space.Rank];
            for (int i = 0; i < coord.Length; i++)
                coord[i] = (ulong)index[i];

            H5S.select_elements(space.ID, H5S.seloper_t.SET, (IntPtr)1L, coord);

            return space;
        }

        /// <summary>
        /// Create an `H5Space` with a slice selection on one axis.
        /// </summary>
        /// <remarks>
        /// The selection ranges from `start` inclusively to `stop` exclusively, 
        /// possibly with an additional `step` size. The selected axis is by
        /// default the first axis, i.e. for a 2D dataset entire rows are selected. 
        /// </remarks>
        protected H5Space SelectSlice(long start, long stop, long step = 1, int axis = 0)
        {
            if (stop <= start)
                throw new ArgumentException("stop <= start");

            if (step < 1)
                throw new ArgumentException("step can't be negative");

            if (!(axis < Rank))
                throw new ArgumentException("invalid axis selected");

            if (start < 0 || stop < 0 || stop > Dims[axis])
                throw new IndexOutOfRangeException("index was outside the bounds of the dataset");

            var space = GetSpace();
            var rank = space.Rank;
            // prepare hyperslab selection..
            ulong[] start_ = new ulong[rank];
            ulong[] stride = new ulong[rank];
            ulong[] count = new ulong[rank];
            ulong[] block = null;  // defaults to a single element in each dimension
            for (int i = 0; i < rank; i++) {
                start_[i] = 0UL;
                stride[i] = 1UL;
                // select entire axes other than the selected:
                count[i] = (ulong)space.Dims[i];
            }
            start_[axis] = (ulong)start;
            stride[axis] = (ulong)step;
            count[axis] = (ulong)((stop - start)/step);

            space.SelectHyperslab(start_, stride, count, block);

            return space;
        }

        public void Flush()
        {
#if HDF5_VER1_10
            H5D.flush(ID);
#endif
        }

        public void Refresh()
        {
#if HDF5_VER1_10
            H5D.refresh(ID);
#endif
        }

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5D.close(ID);
        }
    }

    /* -------- Dataset implementations -------- */

    public class string1d : H5DataSet, IEnumerable<string>
    {
        public string1d(hid_t dset_id) : base(dset_id) { }
    
        public static Encoding Enc = Encoding.GetEncoding("iso-8859-1");

        public string this[long m]
        {
            get
            {
                using (H5Space file_space = SelectPoint(m)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                        using (H5Type dtype = GetDType()) {
                            long slen = dtype.Size;
                            byte[] buf = new byte[slen];
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                  H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");

                            string decoded = string1d.Enc.GetString(buf);

                            return decoded.TrimEnd('\0');
                        }
                    }
                }
            }
            set
            {
                using (H5Space file_space = SelectPoint(m)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                        using (H5Type dtype = GetDType()) {
                            long slen = dtype.Size;
                            if (value.Length > slen - 1)
                                throw new IndexOutOfRangeException($"string longer than ({slen})");

                            byte[] buf = new byte[slen];
                            Array.Copy(string1d.Enc.GetBytes(value), buf, value.Length);
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                   H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");
                        }
                    }
                }
            }
        }

        public string[] Values
        {
            get
            {
                long m = Dims[0];
                string[] buf = new string[m];

                for (int i = 0; i < m; i++)
                    buf[i] = this[i];

                return buf;
            }
            set
            {
                for (int i = 0; i < Rank; i++)
                    if (value.GetLength(i) != Dims[i])
                        throw new InvalidOperationException("shape mismatch");

                long m = Dims[0];
                for (int i = 0; i < m; i++)
                    this[i] = value[i];
            }
        }

        public IEnumerable<string> Elements()
        {
            return new index_Enumerable<string>((i) => this[i], Dims[0]);
        }

        [Obsolete("use string1d.Elements() instead", error: false)]
        public IEnumerator<string> GetEnumerator()
        {
            return new index_Enumerator<string>((i) => this[i], Dims[0]);
        }

        [Obsolete("use string1d.Elements() instead", error: false)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class string2d : H5DataSet, IEnumerable<string[]>
    {
        public string2d(hid_t dset_id) : base(dset_id) { }
    
        public string[] this[long m]
        {
            get
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { Dims[1] })) {
                        using (H5Type dtype = GetDType()) {
                            int slen = (int)dtype.Size;
                            byte[] buf = new byte[slen * mem_space.Length];
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                  H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");

                            string decoded = string1d.Enc.GetString(buf);

                            var rv = new string[mem_space.Length];
                            for (int i = 0; i < rv.Length; i++)
                                rv[i] = decoded.Substring(i * slen, slen).TrimEnd('\0');

                            return rv;
                        }
                    }
                }
            }
            set
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { Dims[1] })) {
                        using (H5Type dtype = GetDType()) {
                            if (value.Length > mem_space.Length)
                                throw new IndexOutOfRangeException($"can't hold array of length ({value.Length})");

                            int slen = (int)dtype.Size;
                            if (value.Any(s => s.Length > slen - 1))
                                throw new IndexOutOfRangeException($"string longer than ({slen})");

                            byte[] buf = new byte[slen * mem_space.Length];
                            for (int i = 0; i < value.Length; i++)
                            {
                                var bytes = string1d.Enc.GetBytes(value[i]);
                                Array.Copy(bytes, 0, buf, i * slen, bytes.Length);
                            }
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                   H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.write() returned ({status})");
                        }
                    }
                }
            }
        }

        public string this[long m, long n]
        {
            get
            {
                using (H5Space file_space = SelectPoint(m, n)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                        using (H5Type dtype = GetDType()) {
                            long slen = dtype.Size;
                            byte[] buf = new byte[slen];
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                  H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");

                            string decoded = string1d.Enc.GetString(buf);

                            return decoded.TrimEnd('\0');
                        }
                    }
                }
            }
            set
            {
                using (H5Space file_space = SelectPoint(m, n)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { 1 })) {
                        using (H5Type dtype = GetDType()) {
                            long slen = dtype.Size;
                            if (value.Length > slen - 1)
                                throw new IndexOutOfRangeException($"string longer than ({slen})");

                            byte[] buf = new byte[slen];
                            Array.Copy(string1d.Enc.GetBytes(value), buf, value.Length);
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                   H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");
                        }
                    }
                }
            }
        }

        public string[] Column(long n)
        {
            using (H5Space file_space = SelectSlice(n, n+1, axis: 1)) {
                using (H5Space mem_space = H5Space.Create(1, new long[] { Dims[0] })) {
                    using (H5Type dtype = GetDType()) {
                        int slen = (int)dtype.Size;
                        byte[] buf = new byte[slen * mem_space.Length];
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                              H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.read() returned ({status})");

                        string decoded = string1d.Enc.GetString(buf);

                        var rv = new string[mem_space.Length];
                        for (int i = 0; i < rv.Length; i++)
                            rv[i] = decoded.Substring(i * slen, slen).TrimEnd('\0');

                        return rv;
                    }
                }
            }
        }

        public IEnumerable<string[]> Columns()
        {
            return new index_Enumerable<string[]>((j) => Column(j), Dims[1]);
        }

        public string[] Row(long m)
        {
            return this[m];
        }

        public IEnumerable<string[]> Rows()
        {
            return new index_Enumerable<string[]>((i) => this[i], Dims[0]);
        }

        public string[,] Values
        {
            get
            {
                long m = Dims[0];
                long n = Dims[1];
                string[,] buf = new string[m, n];

                for (int i = 0; i < m; i++)
                    for (int j = 0; j < n; j++)
                        buf[i, j] = this[i, j];

                return buf;
            }
            set
            {
                for (int i = 0; i < Rank; i++)
                    if (value.GetLength(i) != Dims[i])
                        throw new InvalidOperationException("shape mismatch");

                long m = Dims[0];
                long n = Dims[1];
                string[,] buf = new string[m, n];

                for (int i = 0; i < m; i++)
                    for (int j = 0; j < n; j++)
                        this[i, j] = value[i, j];
            }
        }

        public IEnumerable<string> Elements()
        {
            foreach (string[] row in Rows())
                foreach (string elm in row)
                    yield return elm;
        }

        [Obsolete("use dset2d<string[]>.Rows() instead", error: false)]
        public IEnumerator<string[]> GetEnumerator()
        {
            return Rows().GetEnumerator();
        }

        [Obsolete("use dset2d<string>.Rows() instead", error: false)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class dset1d<T> : H5DataSet, IEnumerable<T>
    {
        public dset1d(hid_t dset_id) : base(dset_id) { }

        public T this[long m]
        {
            get => Get<T>(m);
            set => Set<T>(value, m);
        }

        public T[] Values
        {
            get
            {
                using (H5Space space = GetSpace()) {
                    using (H5Type dtype = GetDType()) {
                        T[] buf = new T[space.Length];
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.read(ID, dtype.ID, space.ID, space.ID,
                                              H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.read() returned ({status})");

                        return buf;
                    }
                }
            }
            set
            {
                for (int i = 0; i < Rank; i++)
                    if (value.GetLength(i) != Dims[i])
                        throw new InvalidOperationException("shape mismatch");

                using (H5Space space = GetSpace()) {
                    using (H5Type dtype = GetDType()) {
                        GCHandle pinnedArray = GCHandle.Alloc(value, GCHandleType.Pinned);
                        int status = H5D.write(ID, dtype.ID, space.ID, space.ID,
                                               H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.write() returned ({status})");
                    }
                }
            }
        }

        public IEnumerable<T> Elements()
        {
            return new index_Enumerable<T>((i) => this[i], Dims[0]);
        }

        [Obsolete("use dset1d<T>.Elements() instead", error: false)]
        public IEnumerator<T> GetEnumerator()
        {
            return new index_Enumerator<T>((i) => this[i], Dims[0]);
        }

        [Obsolete("use dset1d<T>.Elements() instead", error: false)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class dset2d<T> : H5DataSet, IEnumerable<T[]>
    {
        public dset2d(hid_t dset_id) : base(dset_id) { }

        public T[] this[long m]
        {
            get
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { Dims[1] })) {
                        using (H5Type dtype = GetDType()) {
                            T[] buf = new T[mem_space.Length];
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                  H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");

                            return buf;
                        }
                    }
                }
            }
            set
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(1, new long[] { file_space.Dims[1] })) {
                        using (H5Type dtype = GetDType()) {
                            if (value.Length > mem_space.Length)
                                throw new IndexOutOfRangeException($"can't hold array of length ({value.Length})");

                            T[] buf = value;
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                   H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");
                        }
                    }
                }
            }
        }

        public T this[long m, long n]
        {
            get => Get<T>(m, n);
            set => Set<T>(value, m, n);
        }

        public T[] Column(long n)
        {
            using (H5Space file_space = SelectSlice(n, n+1, axis: 1)) {
                using (H5Space mem_space = H5Space.Create(1, new long[] { Dims[0] })) {
                    using (H5Type dtype = GetDType()) {
                        T[] buf = new T[mem_space.Length];
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                              H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.read() returned ({status})");

                        return buf;
                    }
                }
            }
        }

        public IEnumerable<T[]> Columns()
        {
            return new index_Enumerable<T[]>((j) => Column(j), Dims[1]);
        }

        public T[] Row(long m)
        {
            return this[m];
        }

        public IEnumerable<T[]> Rows()
        {
            return new index_Enumerable<T[]>((i) => this[i], Dims[0]);
        }

        public T[,] Values
        {
            get
            {
                using (H5Space space = GetSpace()) {
                    using (H5Type dtype = GetDType()) {
                        T[,] buf = new T[space.Dims[0], space.Dims[1]];
                        GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                        int status = H5D.read(ID, dtype.ID, space.ID, space.ID,
                                              H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.read() returned ({status})");

                        return buf;
                    }
                }
            }
            set
            {
                for (int i = 0; i < Rank; i++)
                    if (value.GetLength(i) != Dims[i])
                        throw new InvalidOperationException("shape mismatch");

                using (H5Space space = GetSpace()) {
                    using (H5Type dtype = GetDType()) {
                        GCHandle pinnedArray = GCHandle.Alloc(value, GCHandleType.Pinned);
                        int status = H5D.write(ID, dtype.ID, space.ID, space.ID,
                                               H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                        pinnedArray.Free();
                        if (status < 0)
                            throw new H5LibraryException($"H5D.write() returned ({status})");
                    }
                }
            }
        }

#if HDF5_VER1_10
        public void Append(T[] values)
        {
            uint axis = 0;
            IntPtr num_elem = (IntPtr)1;
            using (H5Type dtype = GetDType())
            {
                GCHandle pinnedArray = GCHandle.Alloc(values, GCHandleType.Pinned);
                IntPtr buf = pinnedArray.AddrOfPinnedObject();
                int status = H5DO.append(ID, H5P.DEFAULT, axis, num_elem, dtype.ID, buf);
                pinnedArray.Free();
                if (status < 0)
                    throw new H5LibraryException($"H5DO.append() returned ({status})");
            }
        }
#endif

        public IEnumerable<T> Elements()
        {
            foreach (T[] row in Rows())
                foreach (T elm in row)
                    yield return elm;
        }

        [Obsolete("use dset2d<T[]>.Rows() instead", error: false)]
        public IEnumerator<T[]> GetEnumerator()
        {
            return Rows().GetEnumerator();
        }

        [Obsolete("use dset2d<T[]>.Rows() instead", error: false)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    
    public class dset3d<T> : H5DataSet
    {
        public dset3d(hid_t dset_id) : base(dset_id) { }

        public T[,] this[long m]
        {
            get
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(Rank - 1, new long[] { Dims[1], Dims[2] })) {
                        using (H5Type dtype = GetDType()) {
                            T[] buf = new T[mem_space.Length];
                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.read(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                  H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");

                            // the buffer needs to be reshaped into a 2-dimensional
                            // array (constructing a 2D buffer is not predictable):
                            long dims0 = mem_space.Dims[0];
                            long dims1 = mem_space.Dims[1];
                            T[,] rv = new T[dims0, dims1];
                            for (int i = 0; i < dims0; i++)
                                for (int j = 0; j < dims1; j++)
                                    rv[i, j] = buf[i * dims1 + j];

                            return rv;
                        }
                    }
                }
            }
            set
            {
                using (H5Space file_space = SelectSlice(m, m+1)) {
                    using (H5Space mem_space = H5Space.Create(Rank - 1, new long[] { Dims[1], Dims[2] })) {
                        using (H5Type dtype = GetDType()) {
                            if (value.Length > mem_space.Length)
                                throw new IndexOutOfRangeException($"can't hold array of length ({value.Length})");

                            // the buffer needs to be reshaped into a 2-dimensional
                            // array (constructing a 2D buffer is not predictable):
                            long dims0 = mem_space.Dims[0];
                            long dims1 = mem_space.Dims[1];
                            T[] buf = new T[mem_space.Length];
                            for (int i = 0; i < dims0; i++)
                                for (int j = 0; j < dims1; j++)
                                    buf[i * dims1 + j] = value[i, j];

                            GCHandle pinnedArray = GCHandle.Alloc(buf, GCHandleType.Pinned);
                            int status = H5D.write(ID, dtype.ID, mem_space.ID, file_space.ID,
                                                   H5P.DEFAULT, pinnedArray.AddrOfPinnedObject());
                            pinnedArray.Free();
                            if (status < 0)
                                throw new H5LibraryException($"H5D.read() returned ({status})");
                        }
                    }
                }
            }
        }

        public T this[long m, long n, long k]
        {
            get => Get<T>(m, n, k);
            set => Set<T>(value, m, n, k);
        }
    }

    /* ---------- Enumerator classes ---------- */

    class index_Enumerable<T> : IEnumerable<T>
    {
        Func<long, T> indexer;
        long stop;

        public index_Enumerable(Func<long, T> indexer, long stop) { this.indexer = indexer; this.stop = stop; }
        public IEnumerator<T> GetEnumerator() { return new index_Enumerator<T>(indexer, stop); }
        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }

    class index_Enumerator<T> : IEnumerator<T>
    {
        Func<long, T> indexer;
        long stop, index = -1;

        public index_Enumerator(Func<long, T> indexer, long stop) { this.indexer = indexer; this.stop = stop; }

        public T Current => indexer(index);

        object IEnumerator.Current => this.Current;

        public bool MoveNext() { return ((index += 1) < stop); }

        public void Reset() { index = -1; }

        public void Dispose() { }
    }
}
