using Windows.Media.MediaProperties;

namespace TestCamera
{
    public class RecordSizeInfo
    {
        public RecordSizeInfo(uint width, uint height, VideoEncodingQuality type)
        {
            Width = width;
            Height = height;
            SizeTag = width * height;
            SizeType = type;
        }

        public uint Width;

        public uint Height;

        public uint SizeTag;

        public VideoEncodingQuality SizeType;
    }
}