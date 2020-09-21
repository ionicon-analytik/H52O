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


namespace H5Ohm
{
    /// <summary>
    /// Set an alternative key for this dataset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class LocationAttribute : Attribute
    {
        public string key;
        public LocationAttribute(string key) { this.key = key; }
    }

    /// <summary>
    /// Make this dataset readonly. No dataset will be created. 
    /// The field may not be filled and must be checked agains `null`!
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadonlyAttribute : Attribute
    {
        public ReadonlyAttribute() { }
    }

    /// <summary>
    /// Adds an hdf5 attribute to the `H5Object`. As a class Attribute, it is attached to
    /// the hdf5 group, as a `DataSet` Attribute, it is attached to the hdf5 dataset.
    /// The collected attributes appear in the `H5Object.Attr` dictionary.
    /// If no `createPrimitive` is given, this attribute is readonly and may be misssing!
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public class AttributeAttribute : Attribute
    {
        public string name;
        public Type primitive;
        public AttributeAttribute(string location, Type createPrimitive = null) { name = location; primitive = createPrimitive; }
    }

    /// <summary>
    /// Set an initial shape other than 1 x 1 x 1 ... for this dataset.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ShapeAttribute : Attribute
    {
        public long[] dims;
        public ShapeAttribute(params long[] dims) { this.dims = dims; }
    }

    /// <summary>
    /// Set a maximum shape this dataset can be resized to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class MaximumShapeAttribute : Attribute
    {
        public long[] dims;
        public MaximumShapeAttribute(params long[] dims) { this.dims = dims; }
    }
}
