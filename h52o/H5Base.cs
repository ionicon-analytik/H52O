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
using System.Diagnostics;


namespace H52O
{
    /// <summary>
    /// Base class from which all H5-objects are derived.
    /// </summary>
    public abstract class H5Base : IDisposable
    {
        public hid_t ID { get; protected set; }

#if DEBUG
        public static int nObjects = 0;
#endif

        protected H5Base(hid_t hid)
        {
            H5Library.ResolvePathToExternalDependencies();

            if ((ID = hid) < 0)
                throw new H5LibraryException($"{this.GetType()} :: invalid hid_t ({ID})");

#if DEBUG
            System.Threading.Interlocked.Increment(ref nObjects);
            int indent = Math.Max(0, nObjects);
            Debug.WriteLine("".PadLeft(indent) + $"[{nObjects}] + ({ID}) :: ({this.GetType().Name})");
#endif
        }

        ~H5Base()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);

#if DEBUG
            if (ID > 0)
            {
                System.Threading.Interlocked.Decrement(ref nObjects);
                int indent = Math.Max(0, nObjects);
                Debug.WriteLine("".PadLeft(indent) + $"[{nObjects}] - ({ID}) :: ({this.GetType().Name})");
            }
#endif
        }
    }
}
