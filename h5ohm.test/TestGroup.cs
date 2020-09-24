using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;  // KeyNotFoundException

using Xunit;

using TestUtils;

using H5Ohm;


namespace H5Ohm.Test
{
    class MyObject : H5Object          // outline a test-object with some fields..
    {
#pragma warning disable 0649

        public dset1d<int> intset;
        public dset1d<long> longset;
        public dset1d<byte> byteset;

        public string1d stringset;
        public string2d stringset2;

        public dset2d<double> doubleset;
        public dset2d<float> floatset;

        public dset3d<double> doubleset3;

        [Location("alternative_path")]
        [Shape(3, 5)]
        [MaximumShape(6, -1)]
        public dset2d<float> advancedset;

        dset1d<float> privateset;

#pragma warning restore 0649

        public MyObject(H5Group grp) : base(grp)
        {
        }

        public dset1d<float> Privateset => privateset;
    }


    public class TestGroup
    {
        static string demodata = Helpers.GetDemodataDir();

        // A Group cannot explicitly create any `datasets`. 
        // The specific structure is only created by linking
        // an `H5Object` to a `File`. 
        // This ensures that links remain valid across multiple
        // sessions, as long as the program has not changed. 
        public class TestDependencyInjection
        {
            [Fact]
            public void TestBasicInjection()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    using (MyObject myo = new MyObject(hf.Root))
                    {

                        Assert.NotNull(myo.intset);
                        Assert.NotNull(myo.longset);
                        Assert.NotNull(myo.byteset);
                        Assert.NotNull(myo.stringset);
                        Assert.NotNull(myo.doubleset);
                        Assert.NotNull(myo.floatset);
                        Assert.NotNull(myo.Privateset);
                        Assert.NotNull(myo.doubleset3);

                        Assert.Equal(1, myo.intset.Rank);
                        Assert.Equal(1, myo.longset.Rank);
                        Assert.Equal(1, myo.byteset.Rank);
                        Assert.Equal(1, myo.stringset.Rank);
                        Assert.Equal(2, myo.doubleset.Rank);
                        Assert.Equal(2, myo.floatset.Rank);
                        Assert.Equal(1, myo.Privateset.Rank);
                        Assert.Equal(3, myo.doubleset3.Rank);

                        Assert.Equal(1L, myo.intset.Length);
                        Assert.Equal(1L, myo.longset.Length);
                        Assert.Equal(1L, myo.byteset.Length);
                        Assert.Equal(1L, myo.stringset.Length);
                        Assert.Equal(1L, myo.doubleset.Length);
                        Assert.Equal(1L, myo.floatset.Length);
                        Assert.Equal(1L, myo.Privateset.Length);
                        Assert.Equal(1L, myo.doubleset3.Length);

                        Assert.Equal(typeof(int), myo.intset.PrimitiveType);
                        Assert.Equal(typeof(long), myo.longset.PrimitiveType);
                        Assert.Equal(typeof(byte), myo.byteset.PrimitiveType);
                        Assert.Equal(typeof(string), myo.stringset.PrimitiveType);
                        Assert.Equal(typeof(double), myo.doubleset.PrimitiveType);
                        Assert.Equal(typeof(float), myo.floatset.PrimitiveType);
                        Assert.Equal(typeof(float), myo.Privateset.PrimitiveType);
                        Assert.Equal(typeof(double), myo.doubleset3.PrimitiveType);
                    }
                }
            }

            class ReadOnlyObject : H5Object          // outline a readonly-object..
            {
                [Readonly]
                public dset2d<byte> readonli;

                [Readonly]
                public dset1d<int> existing;

                public ReadOnlyObject(H5Group grp) : base(grp)
                {
                }
            }

            [Fact]
            public void InjectReadonly()
            {
                string path = Path.Combine(demodata, "test_inject_readonly.h5");

                if (!File.Exists(path))
                    using (H5File hf = H5File.Open(path, "x"))
                    {
                        var dset = hf.Root.CreateDataset("existing", 1, new long[] { 1 }, typeof(int));
                        // make sure, the file is closed properly:
                        dset.Dispose();
                    }

                // first try: readonly file mode..
                using (var hf = H5File.Open(path, "r"))
                {
                    using (ReadOnlyObject myo = new ReadOnlyObject(hf.Root))
                    {

                        // ..dataset has been skipped:
                        Assert.Null(myo.readonli);

                        Assert.NotNull(myo.existing);

                    }
                }
            }

            [Fact]
            public void TestAdvancedInjection()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    using (MyObject myo = new MyObject(hf.Root))
                    {

                        Assert.True(H5Link.Exists(hf.Root.ID, "alternative_path"));

                        dset2d<float> dset = myo.advancedset as dset2d<float>;
                        Assert.NotNull(dset);

                        Assert.Equal(2, dset.Rank);
                        Assert.Equal(new long[] { 3, 5 }, dset.Dims);
                        Assert.Equal(15L, dset.Length);
                        Assert.Equal(new float[5], dset[2]);

                        if (H5Library.LibVersion == "1.8.12")
                        {
                            Assert.Throws<NotImplementedException>(() => dset.Resize(new long[] { 6, 9 }));
                        }
                        else
                        {
                            dset.Resize(new long[] { 6, 5 });

                            Assert.Equal(30L, dset.Length);
                            dset[5] = new float[5] { 1, 2, 3, 4, 5 };

                            dset.Resize(new long[] { 6, 9 });

                            dset[3] = new float[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                        }
                    }
                }
            }

            [Fact]
            public void TestDataPersistance()
            {
                string testfile = demodata + "test_data_persistance.h5";
                try
                {
                    using (H5File hf = H5File.Open(testfile, "w"))
                    {
                        using (MyObject myo = new MyObject(hf.Root))
                        {

                            Assert.Equal(0, myo.floatset[0, 0]);
                            Assert.Equal(0, myo.doubleset[0, 0]);
                            Assert.Equal("", myo.stringset[0]);
                            Assert.Equal("", myo.stringset2[0, 0]);
                            Assert.Equal(0, myo.intset[0]);
                            Assert.Equal(0, myo.doubleset3[0, 0, 0]);

                            myo.floatset[0, 0] = 42.0f;
                            myo.doubleset[0] = new double[] { 42.0 };
                            myo.stringset[0] = "foo";
                            myo.stringset2[0] = new string[] { "zoom" };
                            myo.intset[0] = 42;
                            myo.doubleset3[0, 0, 0] = 3.14;
                        }
                    }

                    using (H5File hf = H5File.Open(testfile, "r"))
                    {
                        using (MyObject myo = new MyObject(hf.Root))
                        {

                            Assert.Equal(1L, myo.doubleset.Length);
                            Assert.Equal(new long[] { 1, 1 }, myo.doubleset.Dims);

                            Assert.Equal(42.0f, myo.floatset[0, 0]);
                            Assert.Equal(new double[] { 42.0 }, myo.doubleset[0]);
                            Assert.Equal("foo", myo.stringset[0]);
                            Assert.Equal(new string[] { "zoom" }, myo.stringset2[0]);
                            Assert.Equal(42, myo.intset[0]);
                            Assert.Equal(new double[,] { { 3.14 } }, myo.doubleset3[0]);
                        }
                    }
                }
                finally
                {
                    System.IO.File.Delete(testfile);
                }
            }
        }

        public class TestDatasetAccessByIndex
        {
            [Fact]
            public void LocateDataset()
            {
                using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
                {
                    H5DataSet dset = hf.Root["level1/dset1"];
                    Assert.NotNull(dset);

                    H5DataSet dset_clone = hf.Root.SubGroup("level1")["dset1"];
                    Assert.NotNull(dset_clone);

                    H5DataSet dset_link = hf.Root["level1/level2/dset1_link"];
                    Assert.NotNull(dset_link);
                }
            }

            [Theory]
            [InlineData("datasets/double1d")]
            [InlineData("datasets/string1d")]
            [InlineData("datasets/string1d_32")]
            [InlineData("datasets/float1d")]
            [InlineData("datasets/int1d")]
            public void LocateDatasetWith1DProperties(string key)
            {
                using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
                {
                    H5DataSet dset = hf.Root[key];

                    Assert.Equal(1, dset.Rank);
                    Assert.Equal(7L, dset.Length);
                    Assert.Equal(new long[] { 7 }, dset.Dims);
                }
            }

            [Theory]
            [InlineData("datasets/double2d")]
            [InlineData("datasets/float2d")]
            [InlineData("datasets/int2d")]
            public void LocateDatasetWith2DProperties(string key)
            {
                using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
                {
                    H5DataSet dset = hf.Root[key];

                    Assert.Equal(2, dset.Rank);
                    Assert.Equal(49L, dset.Length);
                    Assert.Equal(new long[] { 7, 7 }, dset.Dims);
                }
            }

            [Fact]
            public void NonexistingLookupFails()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    Assert.Throws<KeyNotFoundException>(() => hf.Root["zoom"]);
                    Assert.Throws<KeyNotFoundException>(() => hf.Root["foo/bar"]);
                }
            }

            [Fact]
            public void InvalidPathLookupFails()
            {
                using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
                {
                    Assert.True(H5Link.Exists(hf.ID, "level1/dset1"));

                    // absolute paths are not allowed, as those undermine the local-ness of a group
                    Assert.Throws<KeyNotFoundException>(() => hf.Root["/level1/dset1"]);

                    // dataset cannot have trailing slash, because this would look like a group
                    Assert.Throws<KeyNotFoundException>(() => hf.Root["level1/dset1/"]);
                }
            }
        }

        public class TestSubGroupAccess
        {
            [Fact]
            public void CreateSubGroup()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    Assert.False(H5Link.Exists(hf.ID, "foo"));
                    Assert.Throws<KeyNotFoundException>(() => hf.Root.SubGroup("foo"));

                    using (var GRP = hf.Root.CreateGroup("foo"))
                    {
                        Assert.True(H5Link.Exists(hf.ID, "foo"));
                        using (hf.Root.SubGroup("foo"))
                        {
                        }  // dispose immediately

                        Assert.False(H5Link.Exists(hf.ID, "bar"));
                        Assert.Throws<KeyNotFoundException>(() => hf.Root.SubGroup("bar"));

                        // create on-the-fly..
                        using (hf.Root.SubGroup("bar", create: true))
                        {
                        }
                        Assert.True(H5Link.Exists(hf.ID, "bar"));
                    }
                }
            }

            [Fact]
            public void DeleteSubGroup()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    Assert.False(H5Link.Exists(hf.ID, "foo"));
                    Assert.False(H5Link.Exists(hf.ID, "bar"));

                    using (hf.Root.CreateGroup("foo"))
                    {
                    }
                    using (hf.Root.CreateGroup("bar"))
                    {
                    }

                    Assert.True(H5Link.Exists(hf.ID, "foo"));
                    Assert.True(H5Link.Exists(hf.ID, "bar"));

                    hf.Root.DeleteGroup("bar");

                    Assert.True(H5Link.Exists(hf.ID, "foo"));
                    Assert.False(H5Link.Exists(hf.ID, "bar"));

                    hf.Root.DeleteGroup("foo");

                    Assert.False(H5Link.Exists(hf.ID, "foo"));
                    Assert.False(H5Link.Exists(hf.ID, "bar"));
                }
            }

            [Fact]
            public void CreateNestedSubGroups()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    Assert.False(H5Link.Exists(hf.ID, "zoom"));
                    Assert.False(H5Link.Exists(hf.ID, "zoom/zoom"));
                    Assert.False(H5Link.Exists(hf.ID, "zoom/zoom/zoom"));

                    H5Group GRP = hf.Root;

                    for (int i = 0; i < 3; i++)
                    {
                        GRP = GRP.CreateGroup("zoom");
                    }

                    GRP.Dispose();

                    Assert.True(H5Link.Exists(hf.ID, "zoom/zoom/zoom"));
                }
            }

            [Fact]
            public void CannotCreateMultipleGroups()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    Assert.False(H5Link.Exists(hf.ID, "grok/"));
                    Assert.False(H5Link.Exists(hf.ID, "grok/fitz"));

                    Assert.Throws<InvalidOperationException>(() => hf.Root.CreateGroup("grok/fitz"));
                }
            }

            [Fact]
            public void CanAccessAbsolutePaths()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

                    using (H5Group GRP = hf.Root.CreateGroup("foo"))
                    {
                        H5Group SUBGRP = GRP.SubGroup("bar", create: true);
                        SUBGRP.Dispose();

                        Assert.True(H5Link.Exists(hf.ID, "/foo/"));
                        Assert.True(H5Link.Exists(hf.ID, "/foo/bar/"));

                        SUBGRP = hf.Root.SubGroup("/foo");
                        Assert.NotNull(GRP);
                        SUBGRP.Dispose();

                        SUBGRP = hf.Root.SubGroup("/foo/bar");
                        Assert.NotNull(GRP);
                        SUBGRP.Dispose();

                        SUBGRP = hf.Root.SubGroup("foo/bar");
                        Assert.NotNull(SUBGRP);
                        SUBGRP.Dispose();
                    }
                }
            }

            [Fact]
            public void FindsAllSubgroups()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5File hf = container.Content();

#if DEBUG
                    int nObjectsInitially = H5Base.nObjects;
#endif

                    H5Group SUT = hf.Root;

                    var testnames = new List<string>
                    {
                        "foo",
                        "bar",
                        "zoom",
                        "grok",
                    };
                    // create a group hierarchy..
                    foreach (string name in testnames)
                        using (var subgroup = SUT.CreateGroup(name))
                        {
                            using (subgroup.CreateGroup("tarnkappenzwerg"))
                            {
                                // just create and dispose..
                            }
                        }
                    // ..as well as some confusion..
                    using (SUT.CreateDataset("tarnkappenbomber", 1, new long[] { 7 }, typeof(float)))
                    {
                    }

                    var actual = SUT.SubGroups().Select(g => g.Name);

                    // a) check for set equality..
                    var testnames_sorted = new SortedSet<string>(testnames);

                    Assert.True(testnames_sorted.SetEquals(actual));

                    // b) check the default behaviour of sorting the SubGroups() alphabetically..
                    Assert.Equal(testnames_sorted.ToList(), actual);

                    // c) check that all allocated SubGroups() have been disposed of properly..
#if DEBUG
                    Assert.Equal(nObjectsInitially, H5Base.nObjects);
#endif
                }
            }
        }
    }
}
