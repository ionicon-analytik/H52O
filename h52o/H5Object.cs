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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Reflection;

using H52O.Extensions;


namespace H52O
{
    /// <summary>
    /// Base class for all persistent Objects.
    /// </summary>
    public abstract class H5Object : IDisposable
    {
        public ReadOnlyDictionary<string, H5Attribute> Attr;

        public H5Object(H5Group group)
        {
            Attr = new ReadOnlyDictionary<string, H5Attribute>(new Dictionary<string, H5Attribute>());

            Initialize(group);
        }

        ~H5Object()
        {
            Dispose();
        }

        /// <summary>
        /// Automagically fill data fields with their Hdf5 implementations.
        /// </summary> 
        private void Initialize(H5Group grp)
        {
            var collectedH5Attrs = new Dictionary<string, H5Attribute>();
            foreach (object attr in this.GetType().GetCustomAttributes(inherit: true))
            {
                if (attr.GetType() == typeof(AttributeAttribute))
                {
                    string name = ((AttributeAttribute)attr).name;
                    try
                    {
                        collectedH5Attrs.Add(name, grp.GetAttribute(name));
                    }
                    catch (KeyNotFoundException)
                    {
                        Type primitive = ((AttributeAttribute)attr).primitive;
                        object default_ = ((AttributeAttribute)attr).default_;
                        if (primitive == null && default_ == null)
                            continue;

                        collectedH5Attrs.Add(name, grp.SetAttribute(name, primitive, default_));
                    }
                }
            }

            var pendingAttrs = new List<Tuple<string, Type, object>>();
            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
            foreach (FieldInfo dsetInfo in this.GetType().GetFields(flags))
            {
                // skip fields that don't interest us here..
                if (!dsetInfo.FieldType.IsSubclassOf(typeof(H5DataSet)))
                    continue;

                // ..as well as those that have been already defined:
                if (!(dsetInfo.GetValue(this) == null))
                    continue;

                // set default dataset creation parameters..
                string key = dsetInfo.Name;
                bool readonli = false;
                Match match = Regex.Match(dsetInfo.FieldType.Name, @".*([1-3])d");
                int rank = Int32.Parse(match.Groups[1].Value);
                long[] dims = new long[rank];
                long[] maxdims = new long[rank];
                dims.Fill<long>(1L);
                maxdims.Fill<long>(-1L);  // use H5S.UNLIMITED on all dimensions

                // ..which may be overridden by Attributes:
                pendingAttrs.Clear();
                foreach (object attr in dsetInfo.GetCustomAttributes(inherit: false))
                {
                    if (attr.GetType() == typeof(LocationAttribute))
                        key = ((LocationAttribute)attr).key;
                    else if (attr.GetType() == typeof(ReadonlyAttribute))
                        readonli = true;
                    else if (attr.GetType() == typeof(ShapeAttribute))
                        dims = ((ShapeAttribute)attr).dims;
                    else if (attr.GetType() == typeof(MaximumShapeAttribute))
                        maxdims = ((MaximumShapeAttribute)attr).dims;
                    else if (attr.GetType() == typeof(AttributeAttribute))
                        pendingAttrs.Add(new Tuple<string, Type, object>(
                            ((AttributeAttribute)attr).name,
                            ((AttributeAttribute)attr).primitive,
                            ((AttributeAttribute)attr).default_
                            ));
                }

                // initialize the dataset..
                H5DataSet dset;
                if (H5Link.Exists(grp.ID, key))
                {
                    dset = grp[key];
                }
                else if (readonli)
                {
                    continue;
                }
                else
                {
                    Type dtype, field_type = dsetInfo.FieldType;
                    if (field_type.IsGenericType)
                        dtype = field_type.GenericTypeArguments[0];
                    else if (field_type == typeof(string1d))
                        dtype = typeof(string);
                    else if (field_type == typeof(string2d))
                        dtype = typeof(string);
                    else
                        throw new NotImplementedException(field_type.ToString());

                    dset = grp.CreateDataset(key, rank, dims, dtype, maxdims);
                }

                // add pending hdf5 attributes..
                foreach (var tup in pendingAttrs)
                {
                    string name = tup.Item1;
                    Type primitive = tup.Item2;
                    object default_ = tup.Item3;
                    try
                    {
                        collectedH5Attrs.Add(name, dset.GetAttribute(name));
                    }
                    catch (KeyNotFoundException)
                    {
                        if (primitive == null && default_ == null)
                            continue;

                        collectedH5Attrs.Add(name, dset.SetAttribute(name, primitive, default_));
                    }
                }

                // finally, set this instance's field to the new object.
                // Note that, even if `dset` is declared as `object`, it will be
                // of type `field.FieldType`:
                dsetInfo.SetValue(this, dset);
            }
            Attr = new ReadOnlyDictionary<string, H5Attribute>(collectedH5Attrs);
        }

        public virtual void Dispose()
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
            foreach (var field in this.GetType().GetFields(flags))
            {
                if (field.GetValue(this) is IDisposable h5obj)
                    h5obj?.Dispose();
            }
            foreach (IDisposable attr in Attr.Values)
                attr.Dispose();
        }
    }
}
