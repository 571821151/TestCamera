using Windows.Devices.Enumeration;

namespace TestCamera
{
    public class CameraInfoViewModel
    {
        public string Id;

        public string Name { get; set; }

        public DeviceInformation Data;
    }
}