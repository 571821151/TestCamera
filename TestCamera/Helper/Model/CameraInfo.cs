using Windows.Devices.Enumeration;

namespace TestCamera
{
    public class CameraInfo
    {
        public string Id;
        public string Name { get; set; }
        public DeviceInformation Obj;
    }
}