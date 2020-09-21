using System;

using H5Ohm;


namespace Examples
{
#pragma warning disable 0649

    class MyObject : H5Object
    {
        [Shape(200, 5)]
        public dset2d<double> temperatureReading;

        [Shape(200)]
        public dset1d<long> timestamps;

        [Shape(5)]
        public string1d cityNames;

        public MyObject(H5Group location)
            : base(location)
        {
            cityNames.Values = new string[] {
                "Chicago", "New York", "San Francisco",
                "Springfield, Nebraska", "New Berlin",
            };
        }
    }

#pragma warning restore 0649

    class Program
    {
        static void Main(string[] args)
        {
            using (var hf = H5File.Open("example.h5", "w"))
            {
                using (new MyObject(hf.Root))
                {
                    // ... work, work, work ...
                }
            }
        }
    }
}
