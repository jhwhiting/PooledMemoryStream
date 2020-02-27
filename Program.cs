using System;
using System.Buffers;
using System.IO;
using System.Linq;

namespace PooledMemoryStreamDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] loremBytes = File.ReadAllBytes("lorem.txt");

            string mode = args.FirstOrDefault() ?? string.Empty;

            if (mode.Equals("--pooled", StringComparison.InvariantCultureIgnoreCase))
            {
                TestPooledMemoryStream(loremBytes);
            }
            else
            {
                TestMemoryStream(loremBytes);
            }
        }

        private static void TestMemoryStream(byte[] loremBytes)
        {
            for (int i = 0; i < 10000; i++)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(loremBytes, 0, loremBytes.Length);
                }
            }
        }

        private static void TestPooledMemoryStream(byte[] loremBytes)
        {
            for (int i = 0; i < 10000; i++)
            {
                using (PooledMemoryStream pooledStream = new PooledMemoryStream(loremBytes, ArrayPool<byte>.Shared))
                {
                    //using (FileStream fs = new FileStream("loremOutput.txt", FileMode.Create))
                    //{
                    //    pooledStream.CopyTo(fs, 256);
                    //}
                }
            }
        }
    }
}
