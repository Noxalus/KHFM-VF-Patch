using System.IO;

namespace KHFM_VF_Patch
{
    public static class StreamExtensions
    {
        public static T FromBegin<T>(this T stream) where T : Stream => stream.SetPosition(0);

        public static T SetPosition<T>(this T stream, long position) where T : Stream
        {
            stream.Seek(position, SeekOrigin.Begin);
            return stream;
        }

        public static byte[] ReadBytes(this Stream stream) => stream.ReadBytes((int)(stream.Length - stream.Position));

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            var data = new byte[length];
            stream.Read(data, 0, length);
            return data;
        }

        public static byte[] ReadAllBytes(this Stream stream)
        {
            var data = stream.SetPosition(0).ReadBytes();
            stream.Position = 0;
            return data;
        }

        public static int Align(int offset, int alignment)
        {
            var misalignment = offset % alignment;
            return misalignment > 0 ? offset + alignment - misalignment : offset;
        }

        public static long Align(long offset, int alignment)
        {
            var misalignment = offset % alignment;
            return misalignment > 0 ? offset + alignment - misalignment : offset;
        }

        public static Stream AlignPosition(this Stream stream, int alignValue) => stream.SetPosition(Align(stream.Position, alignValue));

        public static void CopyTo(this Stream stream, Stream destination, long startOffset, int size)
        {
            stream.SetPosition(startOffset);
            destination.Write(stream.ReadBytes(size));
        }
    }
}
