using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

using Xunit;

using TestUtils;

using H5Ohm;
using H5Ohm.Extensions;


namespace H5Ohm.Test
{
    public partial class TestDataset
    {
        static string demodata = Path.GetFullPath("../../demodata/");
        static string testfile = demodata + "test_link_exists.h5";

        static long Edge = 7;
        static long Square = 49;

        public class Basic
        {
            [Fact]
            public void Properties()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    H5DataSet DSET = hf.Root["datasets/float2d"];

                    Assert.NotNull(DSET);

                    Assert.Equal(2, DSET.Rank);
                    Assert.Equal(Edge, DSET.Dims[0]);
                    Assert.Equal(Edge, DSET.Dims[1]);
                    Assert.Equal(Edge, DSET.MaxDims[0]);
                    Assert.Equal(Edge, DSET.MaxDims[1]);
                    Assert.Equal(Square, DSET.Length);
                    Assert.Equal(typeof(float), DSET.PrimitiveType);

                    DSET.Dispose();
                }
            }

#if DEBUG
            [Fact]
            public void DisposeOfProperly()
            {
                int N = H5Base.nObjects;

                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    Assert.Equal(N + 2, H5Base.nObjects);  // +file +root

                    H5DataSet DSET = hf.Root["datasets/float2d"];

                    Assert.Equal(N + 3, H5Base.nObjects);  // +dataset

                    Assert.NotNull(DSET);

                    Assert.Equal(2, DSET.Rank);
                    Assert.Equal(Edge, DSET.Dims[0]);
                    Assert.Equal(Edge, DSET.Dims[1]);
                    Assert.Equal(Edge, DSET.MaxDims[0]);
                    Assert.Equal(Edge, DSET.MaxDims[1]);
                    Assert.Equal(Square, DSET.Length);
                    Assert.Equal(typeof(float), DSET.PrimitiveType);

                    // check, that the object count is stable..
                    Assert.Equal(N + 3, H5Base.nObjects);  // +/- 0

                    DSET.Dispose();

                    Assert.Equal(N + 2, H5Base.nObjects);  // -dataset
                }
                Assert.Equal(N + 0, H5Base.nObjects);  // -root -file
            }
#endif
        }

        public class Read
        {
            [Fact]
            public void float1d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset1d<float> DSET = hf.Root["datasets/float1d"] as dset1d<float>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Edge, DSET.Length);
                    Assert.Equal(typeof(float), DSET.PrimitiveType);

                    float[] expected = new float[] { 0, 1, 2, 3, 4, 5, 6 };

                    float[] actual = new float[Edge];
                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void float2d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<float> DSET = hf.Root["datasets/float2d"] as dset2d<float>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);
                    Assert.Equal(typeof(float), DSET.PrimitiveType);

                    float[] expected = new float[] { 0, 1, 4, 9, 16, 25, 36 };

                    float[] actual = new float[Edge];
                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i, i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void float2d_Arrays()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<float> DSET = hf.Root["datasets/float2d"] as dset2d<float>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);

                    Assert.Equal(new float[] { 0, 0, 4, 0, 0,  0,  0 }, DSET.Row(2));
                    Assert.Equal(new float[] { 0, 0, 0, 9, 0,  0,  0 }, DSET.Row(3));
                    Assert.Equal(new float[] { 0, 0, 0, 0, 0, 25,  0 }, DSET.Row(5));
                    Assert.Equal(new float[] { 0, 1, 4, 9,16, 25, 36 }, DSET.Row(6));

                    Assert.Equal(new float[] { 0, 0, 4, 0, 0,  0,  4 }, DSET.Column(2));
                    Assert.Equal(new float[] { 0, 0, 0, 9, 0,  0,  9 }, DSET.Column(3));
                    Assert.Equal(new float[] { 0, 0, 0, 0, 0, 25, 25 }, DSET.Column(5));
                    Assert.Equal(new float[] { 0, 0, 0, 0, 0,  0, 36 }, DSET.Column(6));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(-1));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(-1));
                }
            }
    
            [Fact]
            public void int1d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset1d<int> DSET = hf.Root["datasets/int1d"] as dset1d<int>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Edge, DSET.Length);
                    Assert.Equal(typeof(int), DSET.PrimitiveType);

                    int[] expected = new int[] { 0, 1, 2, 3, 4, 5, 6 };

                    int[] actual = new int[Edge];
                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void int2d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<int> DSET = hf.Root["datasets/int2d"] as dset2d<int>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);
                    Assert.Equal(typeof(int), DSET.PrimitiveType);

                    int[] expected = new int[] { 0, 1, 4, 9, 16, 25, 36 };

                    int[] actual = new int[Edge];
                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i, i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void int2d_Arrays()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<int> DSET = hf.Root["datasets/int2d"] as dset2d<int>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);

                    Assert.Equal(new int[] { 0, 0, 4, 0, 0,  0,  0 }, DSET.Row(2));
                    Assert.Equal(new int[] { 0, 0, 0, 9, 0,  0,  0 }, DSET.Row(3));
                    Assert.Equal(new int[] { 0, 0, 0, 0, 0, 25,  0 }, DSET.Row(5));
                    Assert.Equal(new int[] { 0, 1, 4, 9,16, 25, 36 }, DSET.Row(6));

                    Assert.Equal(new int[] { 0, 0, 4, 0, 0,  0,  4 }, DSET.Column(2));
                    Assert.Equal(new int[] { 0, 0, 0, 9, 0,  0,  9 }, DSET.Column(3));
                    Assert.Equal(new int[] { 0, 0, 0, 0, 0, 25, 25 }, DSET.Column(5));
                    Assert.Equal(new int[] { 0, 0, 0, 0, 0,  0, 36 }, DSET.Column(6));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(-1));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(-1));
                }
            }

            [Fact]
            public void double1d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset1d<double> DSET = hf.Root["datasets/double1d"] as dset1d<double>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Edge, DSET.Length);
                    Assert.Equal(typeof(double), DSET.PrimitiveType);

                    double[] expected = new double[] { 0, 1, 2, 3, 4, 5, 6 };

                    double[] actual = new double[Edge];
                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void double2d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<double> DSET = hf.Root["datasets/double2d"] as dset2d<double>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);
                    Assert.Equal(typeof(double), DSET.PrimitiveType);

                    double[] expected = new double[] { 0, 1, 4, 9, 16, 25, 36 };
                    double[] actual = new double[Edge];

                    for (long i = 0; i < DSET.Dims[0]; i++)
                        actual[i] = DSET[i, i];

                    Assert.Equal(expected, actual);
                }
            }

            [Fact]
            public void double2d_Arrays()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<double> DSET = hf.Root["datasets/double2d"] as dset2d<double>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);

                    Assert.Equal(new double[] { 0, 0, 4, 0, 0,  0,  0 }, DSET.Row(2));
                    Assert.Equal(new double[] { 0, 0, 0, 9, 0,  0,  0 }, DSET.Row(3));
                    Assert.Equal(new double[] { 0, 0, 0, 0, 0, 25,  0 }, DSET.Row(5));
                    Assert.Equal(new double[] { 0, 1, 4, 9,16, 25, 36 }, DSET.Row(6));

                    Assert.Equal(new double[] { 0, 0, 4, 0, 0,  0,  4 }, DSET.Column(2));
                    Assert.Equal(new double[] { 0, 0, 0, 9, 0,  0,  9 }, DSET.Column(3));
                    Assert.Equal(new double[] { 0, 0, 0, 0, 0, 25, 25 }, DSET.Column(5));
                    Assert.Equal(new double[] { 0, 0, 0, 0, 0,  0, 36 }, DSET.Column(6));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Row(-1));

                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(7));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(17));
                    Assert.Throws<IndexOutOfRangeException>(() => DSET.Column(-1));
                }
            }

            [Fact]
            public void double3d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset3d<double> DSET = hf.Root["datasets/double3d"] as dset3d<double>;

                    Assert.NotNull(DSET);
                    Assert.Equal(1 * 2 * 3, DSET.Length);
                    Assert.Equal(typeof(double), DSET.PrimitiveType);

                    Assert.Equal(0.0, DSET[0, 0, 0]);
                    Assert.Equal(1.1, DSET[0, 1, 1]);
                    Assert.Equal(0.2, DSET[0, 0, 2]);
                    Assert.Equal(1.0, DSET[0, 1, 0]);
                    Assert.Equal(1.2, DSET[0, 1, 2]);
                }
            }

            [Fact]
            public void double3d_Array()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset3d<double> DSET = hf.Root["datasets/double3d"] as dset3d<double>;

                    Assert.NotNull(DSET);
                    Assert.Equal(1 * 2 * 3, DSET.Length);
                    Assert.Equal(typeof(double), DSET.PrimitiveType);

                    double[,] expect = new double[,] {
                        { 0.0, 0.1, 0.2 },
                        { 1.0, 1.1, 1.2 },
                    };
                    Assert.Equal(expect, DSET[0]);
                }
            }

            [Theory]
            [InlineData("datasets/string1d")]           // 256 byte string length
            [InlineData("datasets/string1d_32")]        //  32 byte string length
            public void string1d(string key)
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    string1d DSET = hf.Root[key] as string1d;

                    Assert.NotNull(DSET);
                    Assert.Equal(Edge, DSET.Length);
                    Assert.Equal(typeof(string), DSET.PrimitiveType);

                    string[] expect = { "foo", "bar", "zoom", "grok", "fitz", "roy", "baz" };

                    for (long i = 0; i < DSET.Dims[0]; i++)
                    {
                        Assert.Equal(expect[i], DSET[i]);
                    }
                }
            }

            [Theory]
            [InlineData("datasets/string2d")]           // 256 byte string length
            [InlineData("datasets/string2d_32")]        //  32 byte string length
            public void string2d(string key)
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    string2d DSET = hf.Root[key] as string2d;

                    Assert.NotNull(DSET);
                    Assert.Equal(25, DSET.Length);
                    Assert.Equal(typeof(string), DSET.PrimitiveType);

                    string[] expect = { "foo", "bar", "zoom", "grok", "yom" };

                    Assert.Equal(expect, DSET[4]);

                    for (long i = 0; i < DSET.Dims[0]; i++)
                    {
                        Assert.Equal(expect[i], DSET[i, i]);
                    }
                }
            }
        }

        public class Values
        {
            [Fact]
            public void float2d_To2dArray()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<float> DSET = hf.Root["datasets/float2d"] as dset2d<float>;

                    Assert.NotNull(DSET);
                    Assert.Equal(Square, DSET.Length);

                    var expect = new float[7,7] {
                        { 0, 0, 0, 0,  0,  0,  0 },
                        { 0, 1, 0, 0,  0,  0,  0 },
                        { 0, 0, 4, 0,  0,  0,  0 },
                        { 0, 0, 0, 9,  0,  0,  0 },
                        { 0, 0, 0, 0, 16,  0,  0 },
                        { 0, 0, 0, 0,  0, 25,  0 },
                        { 0, 1, 4, 9, 16, 25, 36 },
                    };

                    var actual = DSET.Values;

                    for (int i = 0; i < 7; i++)
                        for (int j = 0; j < 7; j++)
                            Assert.Equal(expect[i,j], actual[i,j]);
                }
            }
    
            [Fact]
            public void string2d_To2dArray()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    string2d DSET = hf.Root["datasets/string2d"] as string2d;

                    Assert.NotNull(DSET);
                    Assert.Equal(5 * 5, DSET.Length);

                    var expect = new string[5,5] {
                        { "foo", ""   , ""    , ""    ,  "" , },
                        { ""   , "bar", ""    , ""    ,  "" , },
                        { ""   , ""   , "zoom", ""    ,  "" , },
                        { ""   , ""   , ""    , "grok",  "" , },
                        { "foo", "bar", "zoom", "grok", "yom" },
                    };

                    var actual = DSET.Values;

                    for (int i = 0; i < 5; i++)
                        for (int j = 0; j < 5; j++)
                            Assert.Equal(expect[i,j], actual[i,j]);
                }
            }
        }

        public class Iterate
        {
            static string demodata = System.IO.Path.GetFullPath("../../demodata/");
            static string testfile = demodata + "test_link_exists.h5";

            [Fact]
            public void Elements()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    using (dset1d<int> DSET = hf.Root["datasets/int1d"] as dset1d<int>)
                    {
                        Assert.NotNull(DSET);

                        var actual = DSET.Elements().ToArray();

                        var expected = new int[] { 0, 1, 2, 3, 4, 5, 6 };

                        Assert.Equal(expected, actual);
                    }

                    using (dset2d<int> DSET = hf.Root["datasets/int2d"] as dset2d<int>)
                    {
                        Assert.NotNull(DSET);

                        var actual = DSET.Elements().Skip(5 * 7 + 4).Take(7).ToArray();

                        var expected = new int[7] { 0, 25, 0, 0, 1, 4, 9 };

                        Assert.Equal(expected, actual);
                    }
                }
            }

            [Fact]
            public void Rows()
            {
                int[][] expected = {
                    new int[] { 0, 0, 0, 0, 0, 0, 0 },
                    new int[] { 0, 1, 0, 0, 0, 0, 0 },
                    new int[] { 0, 0, 4, 0, 0, 0, 0 },
                    new int[] { 0, 0, 0, 9, 0, 0, 0 },
                    new int[] { 0, 0, 0, 0,16, 0, 0 },
                    new int[] { 0, 0, 0, 0, 0,25, 0 },
                    new int[] { 0, 1, 4, 9,16,25,36 },
                };

                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<int> DSET = hf.Root["datasets/int2d"] as dset2d<int>;

                    Assert.NotNull(DSET);

                    int index = 0;
                    foreach (int[] actual in DSET.Rows()) {
                        Assert.Equal(expected[index], actual);
                        index += 1;
                    }

                    Assert.Equal(7, index);

                    // manual testing..
                    
                    var iterator = DSET.Rows().GetEnumerator();

                    Assert.True(iterator.MoveNext());  // init ~> 0
                    Assert.True(iterator.MoveNext());  // 1
                    Assert.True(iterator.MoveNext());  // 2

                    Assert.Equal(expected[2], iterator.Current);

                    iterator.Reset();
                    Assert.True(iterator.MoveNext());  // 0

                    Assert.Equal(expected[0], iterator.Current);

                    iterator.Dispose();
                }
            }

            [Fact]
            public void Cols()
            {
                int[][] expected = {
                    new int[] { 0, 0, 0, 0, 0, 0, 0 },
                    new int[] { 0, 1, 0, 0, 0, 0, 1 },
                    new int[] { 0, 0, 4, 0, 0, 0, 4 },
                    new int[] { 0, 0, 0, 9, 0, 0, 9 },
                    new int[] { 0, 0, 0, 0,16, 0,16 },
                    new int[] { 0, 0, 0, 0, 0,25,25 },
                    new int[] { 0, 0, 0, 0, 0, 0,36 },
                };

                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    dset2d<int> DSET = hf.Root["datasets/int2d"] as dset2d<int>;

                    Assert.NotNull(DSET);

                    int index = 0;
                    foreach (int[] actual in DSET.Columns()) {
                        Assert.Equal(expected[index], actual);
                        index += 1;
                    }

                    Assert.Equal(7, index);

                    // manual testing..
                    
                    var iterator = DSET.Columns().GetEnumerator();

                    Assert.True(iterator.MoveNext());  // init ~> 0
                    Assert.True(iterator.MoveNext());  // 1
                    Assert.True(iterator.MoveNext());  // 2

                    Assert.Equal(expected[2], iterator.Current);

                    iterator.Reset();
                    Assert.True(iterator.MoveNext());  // 0

                    Assert.Equal(expected[0], iterator.Current);

                    iterator.Dispose();
                }
            }

            [Fact]
            public void string1d()
            {
                using (H5File hf = H5File.Open(testfile, mode: "r"))
                {
                    string1d DSET = hf.Root["datasets/string1d"] as string1d;

                    Assert.NotNull(DSET);

                    string[] expected = new string[] {
                        "foo", "bar", "zoom", "grok", "fitz", "roy", "baz"
                    };
                    int index = 0;

                    foreach (string actual in DSET.Elements()) {
                        Assert.Equal(expected[index], actual);
                        index += 1;
                    }
                }
            }
        }

        public class Write
        {
            public class WriteBeyondExtent_Fails
            {
                [Fact]
                public void double1d()
                {
                    using (var container = new TempH5FileContainer())
                    {
                        H5Group ROOT = container.Content().Root;

                        int rank = 1;
                        var dims = new long[] { 3 };
                        using (dset1d<double> DSET = ROOT.CreateDataset("dataset", rank, dims, typeof(double)) as dset1d<double>)
                        {
                            Assert.NotNull(DSET);
                            Assert.Equal(dims, DSET.Dims);

                            DSET[0] = 3.14;
                            DSET[1] = 3.14;
                            DSET[2] = 3.14;

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3] = 3.14);

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1] = 3.14);
                        }
                    }
                }

                [Fact]
                public void double2d()
                {
                    using (var container = new TempH5FileContainer())
                    {
                        H5Group ROOT = container.Content().Root;

                        int rank = 2;
                        var dims = new long[] { 3, 2 };

                        using (dset2d<double> DSET = ROOT.CreateDataset("dataset", rank, dims, typeof(double)) as dset2d<double>)
                        {
                            Assert.NotNull(DSET);
                            Assert.Equal(dims, DSET.Dims);

                            DSET[0, 0] = 3.14;
                            DSET[2, 1] = 3.14;
                            DSET[1] = new double[] { 1.2, 3.4 };

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 0]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 0] = 3.14);

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 2]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 2] = 3.14);

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, 7]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, 7] = 3.14);

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, -3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, -3] = 3.14);

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3] = new double[] { 1.0, 2.0 });

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1] = new double[] { 1.0, 2.0 });

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[0] = new double[] { 1.0, 2.0, 3.0 });
                        }
                    }
                }

                [Fact]
                public void string1d()
                {
                    using (var container = new TempH5FileContainer())
                    {
                        H5Group ROOT = container.Content().Root;

                        int rank = 1;
                        var dims = new long[] { 3 };

                        using (string1d DSET = ROOT.CreateDataset("dataset", rank, dims, typeof(string)) as string1d)
                        {
                            Assert.NotNull(DSET);
                            Assert.Equal(dims, DSET.Dims);

                            DSET[0] = "foo";
                            DSET[1] = "bar";
                            DSET[2] = "yom";

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3] = "grok");

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1] = "grok");
                        }
                    }
                }

                [Fact]
                public void string2d()
                {
                    using (var container = new TempH5FileContainer())
                    {
                        H5Group ROOT = container.Content().Root;

                        int rank = 2;
                        var dims = new long[] { 3, 2 };

                        using (string2d DSET = ROOT.CreateDataset("dataset", rank, dims, typeof(string)) as string2d)
                        {
                            Assert.NotNull(DSET);
                            Assert.Equal(dims, DSET.Dims);

                            DSET[0, 0] = "foo";
                            DSET[2, 1] = "bar";
                            DSET[1] = new string[] { "zoom", "grok" };

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 0]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 0] = "foo");

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 2]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3, 2] = "foo");

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, 7]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, 7] = "foo");

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, -3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[1, -3] = "foo");

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[3] = new string[] { "foo", "bar" });

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1]);
                            Assert.Throws<IndexOutOfRangeException>(() => DSET[-1] = new string[] { "foo", "bar" });

                            Assert.Throws<IndexOutOfRangeException>(() => DSET[0] = new string[] { "foo", "bar", "zoom" });
                        }
                    }
                }
            }

            public class Values
            {
                [Fact]
                public void float1d()
                {
                    using (var Container = new TempH5FileContainer())
                    {
                        var hf = Container.Content();

                        var dims = new long[] { 7L };

                        dset1d<float> DSET = hf.Root.CreateDataset("tempdata", 1, dims, typeof(float)) as dset1d<float>;

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        var expect = new float[7] { 1, 0, 2, 9, 5, 3, 1 };

                        DSET.Values = expect;

                        var actual = DSET.Values;

                        Assert.Equal(expect, actual);

                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new float[1]);
                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new float[9]);

                        DSET.Dispose();
                    }
                }

                [Fact]
                public void string1d()
                {
                    using (var Container = new TempH5FileContainer())
                    {
                        var hf = Container.Content();

                        var dims = new long[] { 3L };

                        string1d DSET = hf.Root.CreateDataset("tempdata", 1, dims, typeof(string)) as string1d;

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        var expect = new string[3] { "foo", "bar", "zoom" };

                        DSET.Values = expect;

                        var actual = DSET.Values;

                        Assert.Equal(expect, actual);

                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new string[1]);
                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new string[9]);

                        DSET.Dispose();
                    }
                }

                [Fact]
                public void float2d()
                {
                    using (var Container = new TempH5FileContainer())
                    {
                        var hf = Container.Content();

                        var dims = new long[] { 3L, 5L };

                        dset2d<float> DSET = hf.Root.CreateDataset("tempdata", 2, dims, typeof(float)) as dset2d<float>;

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        var expect = new float[3,5] {
                            { 0, 0,  2,  0,  5 },
                            { 1, 3,  0,  8,  9 },
                            { 4, 0,  7,  0,  2 },
                        };

                        DSET.Values = expect;

                        var actual = DSET.Values;

                        for (int i = 0; i < 3; i++)
                            for (int j = 0; j < 5; j++)
                                Assert.Equal(expect[i,j], actual[i,j]);

                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new float[1, 1]);
                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new float[1, 5]);

                        DSET.Dispose();
                    }
                }

                [Fact]
                public void string2d()
                {
                    using (var Container = new TempH5FileContainer())
                    {
                        var hf = Container.Content();

                        var dims = new long[] { 3L, 2L };

                        string2d DSET = hf.Root.CreateDataset("tempdata", 2, dims, typeof(string)) as string2d;

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        var expect = new string[3, 2] {
                            { "foo", "bar" },
                            { "zoom", "grok" },
                            { "", "a;seoliruo345uta;gojasd;gljasdlkfjas;eitj;lefgj" },
                        };

                        DSET.Values = expect;

                        var actual = DSET.Values;

                        for (int i = 0; i < 3; i++)
                            for (int j = 0; j < 2; j++)
                                Assert.Equal(expect[i,j], actual[i,j]);

                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new string[1, 1]);
                        Assert.Throws<InvalidOperationException>(() => DSET.Values = new string[1, 5]);

                        DSET.Dispose();
                    }
                }
            }
        }

        public class Resize
        {
            [Fact]
            public void CreateChunkedDataset()
            {
                using (var container = new TempH5FileContainer())
                {
                    H5Group ROOT = container.Content().Root;

                    int rank = 2;
                    var dims = new long[] { 2, 3 };
                    var maxdims = new long[] { 5, 6 };

                    var trace = new StringWriter();
                    //TextWriterTraceListener listener = new TextWriterTraceListener(trace);
                    //Trace.Listeners.Add(listener);

                    using (var DSET = ROOT.CreateDataset("chunked", rank, dims, typeof(int), maxdims))
                    {
                        // chunking is not available in hdf5 v1.8.12 and we want to know about it:
                        //if (H5Library.LibVersion == "1.8.12")
                        //    Assert.Contains("WARNING", trace.ToString());
                        //Trace.Listeners.Remove(listener);

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);
                    }
                }
            }

            [Theory]
            [InlineData(new long[] { 7 }, new long[] { 7 })]
            [InlineData(new long[] { 7 }, new long[] { 0 })]
            [InlineData(new long[] { 7 }, new long[] { 5 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 5, 6 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 1, 6 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 5, 1 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 2, 3 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 0, 6 })]
            [InlineData(new long[] { 5, 6 }, new long[] { 0, 0 })]
            public void ResizeDataset(long[] max_dims, long[] new_dims)
            {
                using (var container = new TempH5FileContainer())
                {
                    H5Group ROOT = container.Content().Root;

                    int rank = max_dims.Length;
                    var dims = new long[rank];
                    dims.Fill(1L);

                    using (H5DataSet DSET = ROOT.CreateDataset("resizable", rank, dims, typeof(double), max_dims))
                    {

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        if (H5Library.LibVersion == "1.8.12")
                        {
                            Assert.NotEqual(max_dims, DSET.MaxDims);
                            Assert.Equal(dims, DSET.MaxDims);
                            Assert.Throws<NotImplementedException>(() => DSET.Resize(max_dims));
                        }
                        else
                        {
                            DSET.Resize(new_dims);

                            Assert.Equal(new_dims, DSET.Dims);
                        }
                    }
                }
            }

            [Theory]
            [InlineData(new long[] { 6, 6 })]
            [InlineData(new long[] { 5, 7 })]
            [InlineData(new long[] { 12, 17 })]
            [InlineData(new long[] { 0, 7 })]
            public void ResizeDatasetChecksBounds(long[] new_dims)
            {
                using (var container = new TempH5FileContainer())
                {
                    H5Group ROOT = container.Content().Root;

                    int rank = 2;
                    var dims = new long[] { 2, 3 };
                    var maxdims = new long[] { 5, 6 };

                    using (H5DataSet DSET = ROOT.CreateDataset("resizable", rank, dims, typeof(double), maxdims))
                    {

                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        if (H5Library.LibVersion == "1.8.12")
                        {
                            Assert.NotEqual(maxdims, DSET.MaxDims);
                            Assert.Equal(dims, DSET.MaxDims);
                            Assert.Throws<NotImplementedException>(() => DSET.Resize(maxdims));
                        }
                        else
                        {
                            Assert.Throws<IndexOutOfRangeException>(() => DSET.Resize(new_dims));
                        }
                    }
                }
            }

            [Theory]
            [InlineData(new long[] { 2, 7, 3 })]
            [InlineData(new long[] { 1 })]
            [InlineData(new long[] { })]
            public void ResizeDatasetChecksRank(long[] new_dims)
            {
                using (var container = new TempH5FileContainer())
                {
                    H5Group ROOT = container.Content().Root;
                    
                    int rank = 2;
                    var dims = new long[] { 2, 3 };
                    var maxdims = new long[] { 5, 6 };

                    using (H5DataSet DSET = ROOT.CreateDataset("resizable", rank, dims, typeof(double), maxdims))
                    {
                        Assert.NotNull(DSET);
                        Assert.Equal(dims, DSET.Dims);

                        if (H5Library.LibVersion == "1.8.12")
                        {
                            Assert.NotEqual(maxdims, DSET.MaxDims);
                            Assert.Equal(dims, DSET.MaxDims);
                            Assert.Throws<NotImplementedException>(() => DSET.Resize(maxdims));
                        }
                        else
                        {
                            Assert.Throws<RankException>(() => DSET.Resize(new_dims));
                        }
                    }
                }
            }

            [Theory]
            [InlineData(new long[] { 2, 3 })]
            [InlineData(new long[] { 12, 17 })]
            [InlineData(new long[] { 7, 1 })]
            [InlineData(new long[] { 1, 7 })]
            [InlineData(new long[] { 1, 1 })]  // shrink..
            [InlineData(new long[] { 121, 333 })]
            [InlineData(new long[] { 0, 5 })]
            [InlineData(new long[] { 0, 0 })]
            public void ResizeUnlimited(long[] new_dims)
            {
                using (var container = new TempH5FileContainer())
                {
                    H5Group ROOT = container.Content().Root;

                    int rank = 2;
                    var dims = new long[] { 2, 3 };
                    var maxdims = new long[] { -1, -1 };

                    using (var DSET = ROOT.CreateDataset("resizable", rank, dims, typeof(double), maxdims) as dset2d<double>)
                    {
                        Assert.NotNull(DSET);

                        Assert.Equal(dims, DSET.Dims);

                        if (H5Library.LibVersion == "1.8.12")
                        {
                            Assert.Throws<NotImplementedException>(() => DSET.Resize(new long[] { 6, 9 }));
                        }
                        else
                        {
                            DSET.Resize(new_dims);
                            Assert.Equal(new_dims, DSET.Dims);
                        }
                    }
                }
            }
        }
    }
}
