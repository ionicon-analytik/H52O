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
using System.Runtime.InteropServices;

using HDF.PInvoke;


namespace H52O
{
    public class H5Space : H5Base
    {
        public const long Unlimited = -1;

        private H5Space(hid_t sid) : base(sid) { }

        /// <summary>
        /// Load a Dataspace from a hdf5 `dataset` by `hid_t`. 
        /// </summary>
        public static H5Space FromDataset(hid_t dset_id)
        {
            return new H5Space(H5D.get_space(dset_id));
        }

        /// <summary>
        /// Create a new Dataspace from scratch. 
        /// </summary>
        /// <param name="rank">The Rank, i.e. number of dimensions.</param>
        /// <param name="dims">The initial dimensions.</param>
        /// <param name="maxdims">The maximum dimensions this space can be resized to. </param>
        /// <remarks>
        /// The `maxdims` parameter may be `null`, which sets `maxdims = dims`. 
        /// Otherwise, it is an array of length equal to `rank` giving the extent
        /// in each dimension. If a negative value is used, the corresponding
        /// dimension is set to unlimited extent.
        /// </remarks>
        public static H5Space Create(int rank, long[] dims, long[] maxdims = null)
        {
            if (dims.Length != rank)
                throw new ArgumentException($"dimensions must be of length ({rank})");

            ulong[] udims = new ulong[rank];
            for (int i = 0; i < rank; i++)
                udims[i] = (ulong)dims[i];
            ulong[] umaxdims = null;
            if (!(maxdims == null)) {
                if (maxdims.Length != rank)
                    throw new ArgumentException($"dimensions must be of length ({rank})");
    
                umaxdims = new ulong[rank];
                for (int i = 0; i < rank; i++) {
                    if (maxdims[i] < 0)
                        umaxdims[i] = H5S.UNLIMITED;
                    else
                        umaxdims[i] = (ulong)maxdims[i];
                }
            }
            return new H5Space(H5S.create_simple(rank, udims, umaxdims));
        }

        public void SelectHyperslab(ulong[] start, ulong[] stride, ulong[] count, ulong[] block)
        {
            if (H5S.select_hyperslab(ID, H5S.seloper_t.SET, start, stride, count, block) < 0)
                throw new H5LibraryException($"H5S.select_hyperslab() returned an ERROR");
        }

        public long SelectedPoints => H5S.get_select_npoints(ID);

        public int Rank => H5S.get_simple_extent_ndims(ID);

        public long[] Dims
        {
            get
            {
                ulong[] dims = new ulong[Rank];
                ulong[] maxdims = new ulong[Rank];
                H5S.get_simple_extent_dims(ID, dims, maxdims);

                // cast Array to `ulong[]` explicitly (Array.Copy() can't do that)..
                long[] rv = new long[Rank];
                for (int i = 0; i < Rank; i++)
                    rv[i] = (long)dims[i];

                return rv;
            }
        }

        public long[] MaxDims
        {
            get
            {
                ulong[] dims = new ulong[Rank];
                ulong[] maxdims = new ulong[Rank];
                H5S.get_simple_extent_dims(ID, dims, maxdims);

                // cast Array to `ulong[]` explicitly and check for UNLIMITED value..
                long[] rv = new long[Rank];
                for (int i = 0; i < Rank; i++)
                {
                    if (maxdims[i] == H5S.UNLIMITED)
                        rv[i] = Unlimited;
                    else
                        rv[i] = (long)maxdims[i];
                }

                return rv;
            }
        }

        public long Length => (long)H5S.get_simple_extent_npoints(ID);

        public override void Dispose()
        {
            base.Dispose();

            if (ID > 0)
                ID = H5S.close(ID);
        }
    }
}
