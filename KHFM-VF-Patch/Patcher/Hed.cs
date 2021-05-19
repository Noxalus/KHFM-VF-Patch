using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xe.BinaryMapper;

namespace KHFM_VF_Patch
{
    public class Hed
    {
        public class Entry
        {
            // Filename hash
            [Data(Count = 16)] public byte[] MD5 { get; set; }
            // Offset of the header of the corresponding file in the PKG stream
            [Data] public long Offset { get; set; }
            // Original asset's compressed data length + all remastered asset's compressed data length
            [Data] public int DataLength { get; set; }
            // Original asset's decompressed data length
            [Data] public int ActualLength { get; set; }
        }

        public static IEnumerable<Entry> Read(Stream stream) => Enumerable
            .Range(0, (int)(stream.Length / 0x20))
            .Select(_ => BinaryMapping.ReadObject<Entry>(stream));
    }
}
