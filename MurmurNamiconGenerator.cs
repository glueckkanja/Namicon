using System;
using System.Text;
using Murmur;

namespace Namicon
{
    public class MurmurNamiconGenerator : NamiconGenerator
    {
        private readonly uint _seed;

        public MurmurNamiconGenerator(int outputSize = 100, uint seed = 0) : base(outputSize)
        {
            _seed = seed;
        }

        public override uint Hasher(string text)
        {
            byte[] hash;
            byte[] input = Encoding.UTF8.GetBytes(text);

            using (var murmur = new Murmur32ManagedX86(_seed))
            {
                hash = murmur.ComputeHash(input);
            }

            return BitConverter.ToUInt32(hash, 0);
        }
    }
}