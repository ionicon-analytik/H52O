using System;

using Xunit;

using H5Ohm;

// Running tests in parallel causes issues with the hdf5-library, 
// because it holds global file handles that prevent temporary
// files from being deleted.
// For now, disable all parallelism in xUnit:
[assembly: CollectionBehavior(DisableTestParallelization = true)]


namespace H5Ohm.Test
{
    public class TestLibrary
    {
        public class TestH5Object
        {
            class MyH5Object : H5Base  // access abstract class 'H5Object'..
            {
                public MyH5Object(hid_t id) : base(id)
                {
                }

                public override void Dispose()
                {
                    base.Dispose();

                    ID = 0;
                }
            }

            [Fact]
            public void RejectsInvalidID()
            {
                Assert.Throws<H5LibraryException>(() => new MyH5Object(-7));
            }

#if DEBUG
            [Theory]
            [InlineData(10_000)]
            public void IsDisposedOnFinalization(int n_objects)
            {
                GC.WaitForPendingFinalizers();

                Assert.Equal(0, H5Base.nObjects);

                // first test manual disposal ======================== 

                MyH5Object BAR = new MyH5Object(42);

                Assert.Equal(1, H5Base.nObjects);

                BAR.Dispose();

                Assert.Equal(0, H5Base.nObjects);

                // test using(..) construct ======================== 

                using (MyH5Object FOO = new MyH5Object(42))
                {
                    Assert.Equal((hid_t)42, FOO.ID);

                    Assert.Equal(1, H5Base.nObjects);
                }

                Assert.Equal(0, H5Base.nObjects);

                // test automatic garbage collection ======================== 

                Assert.Equal(0, H5Base.nObjects);

                MyH5Object mfo = null;

                // Create and release a large number of objects that require finalization..
                int counter = 0;
                for (int j = 0; j < n_objects; j++)
                {
                    mfo = new MyH5Object(j);
                    counter += 1;
                }

                // The garbage collector has already kicked in. The true number of mfos
                // is somewhere around 3000, but definitely below the maximum number..
                Assert.Equal(n_objects, counter);
                int lower = (int)(n_objects * 0.01);
                int upper = n_objects - 1;
                Assert.InRange(H5Base.nObjects, lower, upper);

                // This *would* release the last object created in the loop, but for some
                // reason the garbage collection does not recognize this..
                mfo = null;

                //Force garbage collection. Our short-lived mfos populate generation 1..
                GC.Collect(generation: 1);

                // Wait for all finalizers to complete before continuing.
                // Without this call to GC.WaitForPendingFinalizers,
                // the worker loop below might execute at the same time
                // as the finalizers.
                // With this call, the worker loop executes only after
                // all finalizers have been called.
                GC.WaitForPendingFinalizers();

                // by this point *almost all* of the mfos should have been finalized.
                // the exact number depends on the algorithms of the garbage collector, 
                // but it seems quite reliably to be 2 (the first and the last are not
                // yet collected):
                Assert.Equal(2, H5Base.nObjects);
            }
#endif
        }

        [Theory]
        [InlineData(typeof(string), 256)]
        [InlineData(typeof(float), 4)]
        [InlineData(typeof(double), 8)]
        [InlineData(typeof(int), 4)]
        [InlineData(typeof(byte), 1)]
        public void Test_H5Type(Type type, long size)
        {
            H5Type dtype = H5Type.Create(type);

            Assert.Equal(type, dtype.PrimitiveType);
            Assert.Equal(size, dtype.Size);

            // make sure, disposal works..
            Assert.True(dtype.ID > 0);

            dtype.Dispose();

            Assert.Equal((hid_t)0, dtype.ID);
        }

        [Theory]
        [InlineData(1, new long[] { 5 }, null)]
        [InlineData(2, new long[] { 5, 4}, null)]
        [InlineData(5, new long[] { 5, 4, 3, 3, 1 }, null)]
        [InlineData(2, new long[] { 3, 5 }, new long[] { 3, 5 })]
        [InlineData(2, new long[] { 3, 5 }, new long[] { 6, 30 })]
        public void Test_H5Space(int rank, long[] dims, long[] maxdims)
        {
            H5Space space = H5Space.Create(rank, dims, maxdims);

            Assert.Equal(rank, space.Rank);
            Assert.Equal(dims, space.Dims);

            // make sure, disposal works..
            Assert.True(space.ID > 0);

            space.Dispose();

            Assert.Equal((hid_t)0, space.ID);
        }

        static string demodata = System.IO.Path.GetFullPath("../../demodata/");

        [Theory]
        [InlineData("/")]                       // the root-Group always exists
        [InlineData("level1")]
        [InlineData("/level1")]
        [InlineData("level1/")]                 // trailing slash is allowed
        [InlineData("/level1/")]
        [InlineData("/level1/dset1/")]
        [InlineData("level1/dset1")]
        [InlineData("/level1/dset1")]
        [InlineData("level1/level2")]
        [InlineData("level1/level2/dset1_link")]
        public void Test_Link_Exists(string key)
        {
            using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
            {
                Assert.True(H5Link.Exists(hf.ID, key));
            }
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("foo/bar")]
        [InlineData("level1/attr1")]            // attributes are not checked!
        [InlineData("level1/level2/level3")]
        [InlineData("level1/level")]
        public void Test_Link_Dont_Exist(string key)
        {
            using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
            {
                Assert.False(H5Link.Exists(hf.ID, key));
            }
        }

        [Fact]
        public void Test_Attribute_Exist()
        {
            using (H5File hf = H5File.Open(demodata + "test_link_exists.h5", mode: "r"))
            {
                var grp = hf.Root.SubGroup("level1");

                Assert.True(H5Attribute.Exists(grp.ID, "attr1"));
                Assert.True(H5Attribute.Exists(grp["dset1"].ID, "dset_attr"));

                Assert.False(H5Attribute.Exists(grp.ID, "foo"));
            }
        }
    }
}
