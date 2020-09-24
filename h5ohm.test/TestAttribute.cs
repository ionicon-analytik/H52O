using System;
using System.Collections.Generic;  // KeyNotFoundException

using Xunit;

using TestUtils;

using H5Ohm;


namespace H5Ohm.Test
{
    public class TestAttribute
    {
        static string demodata = System.IO.Path.GetFullPath("../../demodata/");

        [Fact]
        public void Read()
        {
            using (H5File hf = H5File.Open(demodata + "test_generic_read.h5", mode: "r"))
            {
                H5Group grp = hf.Root.SubGroup("my_group");

                H5Attribute attr = grp.GetAttribute("group_attr");
                Assert.NotNull(attr);
                Assert.Equal(42, attr.Read<int>());

                attr = grp["float_1D"].GetAttribute("dataset_attr");
                Assert.NotNull(attr);
                Assert.Equal("a foo that bars", attr.Reads());
            }
        }

        [Fact]
        public void ReadFails()
        {
            using (H5File hf = H5File.Open(demodata + "test_generic_read.h5", mode: "r"))
            {
                H5Group grp = hf.Root.SubGroup("my_group");

                Assert.Throws<KeyNotFoundException>(() => grp.GetAttribute("asdfawieryan"));
                Assert.Throws<KeyNotFoundException>(() => grp["float_1D"].GetAttribute("awxryan"));

                H5Attribute attr = grp.GetAttribute("group_attr");

                Assert.Throws<InvalidCastException>(() => attr.Read<float>());
                Assert.Throws<InvalidCastException>(() => attr.Read<string>());
                Assert.Throws<InvalidCastException>(() => attr.Read<long>());
            }
        }

        [Attribute("group_attr")]
        [Attribute("not_existing")]
        class MyObject : H5Object
        {
            [Readonly]
            [Attribute("dataset_attr")]
            public dset1d<float> float_1D;

            public MyObject(H5Group grp) : base(grp)
            {
            }
        }

        [Fact]
        public void ReadFromMapping()
        {
            using (H5File hf = H5File.Open(demodata + "test_generic_read.h5", mode: "r"))
            {
                MyObject myo;
                using (H5Group grp = hf.Root.SubGroup("my_group"))
                {
                    myo = new MyObject(grp);
                }

                Assert.Equal(42, myo.Attr["group_attr"].Read());
                Assert.Equal("a foo that bars", myo.Attr["dataset_attr"].Read());

                Assert.Throws<KeyNotFoundException>(() => myo.Attr["not_existing"]);

                myo.Dispose();
            }
        }

        [Attribute("not_existing")]
        [Attribute("please_create", typeof(float))]
        class MyNewObject : H5Object
        {
            [Attribute("dataset_attr", typeof(string))]
            public dset1d<float> dset;

            public MyNewObject(H5Group grp) : base(grp)
            {
            }
        }

        [Fact]
        public void WriteToMapping()
        {
            using (TempH5FileContainer container = new TempH5FileContainer())
            {
                H5File hf = container.Content();
                Assert.True(hf.Root.IsWritable);

                MyNewObject myo = new MyNewObject(hf.Root);

                Assert.Throws<KeyNotFoundException>(() => myo.Attr["not_existing"]);

                Assert.NotNull(myo.Attr["please_create"]);
                Assert.NotNull(myo.Attr["dataset_attr"]);

                myo.Attr["dataset_attr"].Writes("foo");
                myo.Attr["please_create"].Write(3.14f);
                Assert.Throws<InvalidCastException>(() => myo.Attr["please_create"].Write(3.14));

                Assert.Equal("foo", myo.Attr["dataset_attr"].Reads());
                Assert.Equal(3.14f, myo.Attr["please_create"].Read<float>());
                Assert.Equal(3.14f, myo.Attr["please_create"].Read());

                myo.Dispose();
            }
        }

    }
}
