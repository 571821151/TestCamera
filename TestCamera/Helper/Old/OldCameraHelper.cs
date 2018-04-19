using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace TestCamera
{
    /// <summary>
    /// 摄像头辅助者
    /// </summary>
    public static class OldCameraHelper
    {
        /// <summary>
        /// 分辨率列表
        /// </summary>
        public static List<CameraSizeInfo> PreviewSizeList;

        public static List<CameraSizeInfo> VideoRecordSizeList;

        public static List<CameraSizeInfo> PhotoSizeList;
        /// <summary>
        /// 主摄像头
        /// </summary>
        public static MediaCapture MainCamera;
        /// <summary>
        /// 摄像头信息列表
        /// </summary>
        public static List<CameraInfoViewModel> CameraInfoList;

        public static string OldCameraId = "";

        public static uint OldVideoSizeWidth = 0;

        public static uint OldVideoSizeHeight = 0;

        public static uint OldPhotoSizeWidth = 0;

        public static uint OldPhotoSizeHeight = 0;

        public static double ZoomMin = 0;

        public static double ZoomMax = 0;

        public static double ZoomStep = 0;
        /// <summary>
        /// 当前选择摄像头
        /// </summary>
        public static DeviceInformation NowCamera;

        public static bool IsLoad = true;

        public static LowLagPhotoCapture LowLag = null;

        public static string CameraShowInfo = string.Empty;

        /// <summary>
        /// 初始化
        /// </summary>
        public static async Task Init(CaptureElement element, Action overAction)
        {
            if (IsLoad == false) { return; }
            IsLoad = false;
            var res = await GetCameraList();
            var res2 = await StartPreviewt(element);
            IsLoad = true;
            overAction();
            //await GetCameraList().ContinueWith(async (t1) =>
            //{
            //    await StartPreviewt(element).ContinueWith((t2) =>
            //    {
            //        IsLoad = true;
            //        overAction();
            //    });
            //});
        }
        /// <summary>
        /// 停止预览
        /// </summary>
        public static void StopPreview()
        {
            if (MainCamera == null) { return; }
            MainCamera.Dispose();
            MainCamera = null;
        }
        /// <summary>
        /// 设置分辨率
        /// </summary>
        public static async void SetPhotoSize(MediaStreamType type, bool isSetOld, CameraSizeInfo size)
        {
            if (isSetOld == false)
            {
                if (type == MediaStreamType.Photo)
                {
                    OldPhotoSizeWidth = size.Width;
                    OldPhotoSizeHeight = size.Height;
                }
                else if (type == MediaStreamType.VideoPreview)
                {
                    OldVideoSizeWidth = size.Width;
                    OldVideoSizeHeight = size.Height;
                }
            }
            try
            {
                await MainCamera.VideoDeviceController.SetMediaStreamPropertiesAsync(type, size.Data);
            }
            catch (Exception ex) { }
        }

        public static bool SetZoom(double zoomValue)
        {
            if (MainCamera != null && MainCamera.VideoDeviceController != null && MainCamera.VideoDeviceController.Zoom != null)
            {
                if (MainCamera.VideoDeviceController.Zoom.Capabilities.Supported == false)
                {
                    return false;
                }
                if (MainCamera.VideoDeviceController.Zoom.TrySetValue(zoomValue) == false)
                {
                    return false;
                }
            }
            else { return false; }
            return true;
        }

        public static async void CaptureMorePhoto(Action<uint, uint, PhotoCapturedEventArgs> executeAction, Action overAction)
        {
            if (MainCamera.VideoDeviceController.LowLagPhotoSequence.Supported == false)
            {
                overAction();
                return;
            }
            var captureTime = DateTime.Now;
            var pastFrame = 10u;
            var maxFrame = MainCamera.VideoDeviceController.LowLagPhotoSequence.MaxPastPhotos;
            if (pastFrame > maxFrame) { pastFrame = maxFrame; }
            MainCamera.VideoDeviceController.LowLagPhotoSequence.ThumbnailEnabled = true;
            MainCamera.VideoDeviceController.LowLagPhotoSequence.DesiredThumbnailSize = 300;
            MainCamera.VideoDeviceController.LowLagPhotoSequence.PhotosPerSecondLimit = 4;
            MainCamera.VideoDeviceController.LowLagPhotoSequence.PastPhotoLimit = pastFrame;
            var photoCapture = await MainCamera.PrepareLowLagPhotoSequenceCaptureAsync(ImageEncodingProperties.CreateJpeg());
            var nowIndex = 1u;
            photoCapture.PhotoCaptured += new Windows.Foundation.TypedEventHandler<LowLagPhotoSequenceCapture, PhotoCapturedEventArgs>(async (ds, de) =>
            {
                if (nowIndex > pastFrame) { return; }
                executeAction(nowIndex, pastFrame, de);
                //处理索引
                if (nowIndex == pastFrame)
                {
                    await ds.StopAsync();
                    await ds.FinishAsync();
                    overAction();
                }
                nowIndex++;
            });
            await photoCapture.StartAsync();
        }
        public static async void CaptureOnePhoto(int type, Action<IRandomAccessStream> overAction)
        {
            var captureTime = DateTime.Now;
            if (type == 0)
            {
                using (IRandomAccessStream s = new InMemoryRandomAccessStream())
                {
                    await MainCamera.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), s);
                    overAction(s);
                }
            }
            else if (type == 1)
            {
                if (LowLag == null)
                {
                    return;
                }
                var photoData = await LowLag.CaptureAsync();
                using (var stream = photoData.Frame.CloneStream())
                {
                    overAction(stream);
                }
            }
        }
        /// <summary>
        /// 开始预览
        /// </summary>
        private static async Task<bool> StartPreviewt(CaptureElement element)
        {
            CameraShowInfo = string.Empty;
            if (NowCamera == null) { return false; }
            StopPreview();
            MainCamera = new MediaCapture();
            var settings = new MediaCaptureInitializationSettings();
            settings.VideoDeviceId = NowCamera.Id;
            settings.PhotoCaptureSource = PhotoCaptureSource.Auto;
            settings.StreamingCaptureMode = StreamingCaptureMode.Video;
            settings.AudioDeviceId = "";
            await MainCamera.InitializeAsync(settings);
            if (PreviewSizeList == null) { PreviewSizeList = new List<CameraSizeInfo>(); }
            try
            {
                GetPhotoSize(MediaStreamType.VideoPreview, PreviewSizeList);
            }
            catch (Exception ex) { }
            if (PhotoSizeList == null) { PhotoSizeList = new List<CameraSizeInfo>(); }
            try
            {
                GetPhotoSize(MediaStreamType.Photo, PhotoSizeList);
            }
            catch (Exception ex) { }
            if (OldCameraHelper.PhotoSizeList == null) { return false; }

            if (VideoRecordSizeList == null) { VideoRecordSizeList = new List<CameraSizeInfo>(); }
            try
            {
                GetPhotoSize(MediaStreamType.VideoRecord, VideoRecordSizeList);
            }
            catch (Exception ex) { }


            element.Source = OldCameraHelper.MainCamera;
            await MainCamera.StartPreviewAsync();
            //获取ZOOM范围
            if (MainCamera != null && MainCamera.VideoDeviceController != null && MainCamera.VideoDeviceController.Zoom != null)
            {
                ZoomMin = MainCamera.VideoDeviceController.Zoom.Capabilities.Min;
                ZoomMax = MainCamera.VideoDeviceController.Zoom.Capabilities.Max;
                ZoomStep = MainCamera.VideoDeviceController.Zoom.Capabilities.Step;
            }
            else
            {
                ZoomMin = 0;
                ZoomMax = 0;
                ZoomStep = 0;
            }

            /*
            //     获取或设置照相机的曝光时间。!!!!!
            public MediaDeviceControl Exposure { get; }*****
            //     获取此视频设备的曝光控件。!!!!!
            public ExposureControl ExposureControl { get; }*****
            //     获取或设置照相机的焦点设置。!!!!!
            public MediaDeviceControl Focus { get; }*****
            //     获取此视频设备的焦点控件。!!!!!
            public FocusControl FocusControl { get; }*****
            //     获取此视频设备的 ISO 感光度控件。!!!!!
            public IsoSpeedControl IsoSpeedControl { get; }*****
            //     获取此视频设备的低快门延迟照片控件。!!!!!
            public LowLagPhotoControl LowLagPhoto { get; }*****
            //     获取此视频设备的低快门延迟照片序列控件。!!!!!
            public LowLagPhotoSequenceControl LowLagPhotoSequence { get; }*****
            //     获取此视频设备的相关区域控件。!!!!!
            public RegionsOfInterestControl RegionsOfInterestControl { get; }*****
            //     获取和设置照相机的缩放设置。!!!!!
            public MediaDeviceControl Zoom { get; }*****
            //     指定是否在照相机上启用背光补偿。
            public MediaDeviceControl BacklightCompensation { get; }*****
            //     获取或设置照相机的亮度级别。
            public MediaDeviceControl Brightness { get; }*****
            //     获取或设置照相机的对比度。
            public MediaDeviceControl Contrast { get; }*****
            //     获取此视频设备的曝光补偿控件。
            public ExposureCompensationControl ExposureCompensationControl { get; }*****
            //     获取此视频设备的闪光控件。
            public FlashControl FlashControl { get; }*****
            //     获取或设置照相机的色调设置。
            public MediaDeviceControl Hue { get; }*****
            //     获取或设置照相机的全景设置。
            public MediaDeviceControl Pan { get; }*****
            //     获取或设置设备的主要用途。
            public CaptureUse PrimaryUse { get; set; }*****
            //     获取或设置照相机的滚拍设置。
            public MediaDeviceControl Roll { get; }*****
            //     获取此视频设备的场景模式控件。
            public SceneModeControl SceneModeControl { get; }*****
            //     获取或设置照相机的倾斜设置。
            public MediaDeviceControl Tilt { get; }*****
            //     获取此视频设备的闪光灯控件。
            public TorchControl TorchControl { get; }*****
            //     获取或设置照相机的白平衡。
            public MediaDeviceControl WhiteBalance { get; }*****
            //     获取此视频设备的白平衡控件。
            public WhiteBalanceControl WhiteBalanceControl { get; }
             */
            //组织设备信息
            try
            {
                var vdc = MainCamera.VideoDeviceController;
                CameraShowInfo += "Id:" + NowCamera.Id + "\r\n";
                CameraShowInfo += "PreviewSizeList:" + string.Join(",", PreviewSizeList.Select(li => li.Width + "*" + li.Height)) + "\r\n";
                CameraShowInfo += "PhotoSizeList:" + string.Join(",", PhotoSizeList.Select(li => li.Width + "*" + li.Height)) + "\r\n";
                CameraShowInfo += "VideoRecordSizeList:" + string.Join(",", VideoRecordSizeList.Select(li => li.Width + "*" + li.Height)) + "\r\n";


                string strVideoDeviceCharacteristic = "";
                
                switch( MainCamera.MediaCaptureSettings.VideoDeviceCharacteristic)
                {
                    case VideoDeviceCharacteristic.AllStreamsIndependent:

                        strVideoDeviceCharacteristic = "AllStreamsIndependent";
                        break;
                    case VideoDeviceCharacteristic.PreviewRecordStreamsIdentical:
                        strVideoDeviceCharacteristic = "PreviewRecordStreamsIdentical";
                        break;
                    case VideoDeviceCharacteristic.PreviewPhotoStreamsIdentical:
                        strVideoDeviceCharacteristic = "PreviewPhotoStreamsIdentical";
                        break;
                    case VideoDeviceCharacteristic.RecordPhotoStreamsIdentical:
                        strVideoDeviceCharacteristic = "RecordPhotoStreamsIdentical";
                        break;
                    case VideoDeviceCharacteristic.AllStreamsIdentical:
                        strVideoDeviceCharacteristic = "AllStreamsIdentical";
                        break;
                    default:

                        break;
                }

                CameraShowInfo += "VideoDeviceCharacteristic:" + strVideoDeviceCharacteristic + "\r\n";

                //// 摘要: 
                ////     所有流都是独立的。
                //[SupportedOn(100794368, Platform.Windows)]
                //[SupportedOn(100859904, Platform.WindowsPhone)]
                //AllStreamsIndependent = 0,
                ////
                //// 摘要: 
                ////     预览视频流是相同的。
                //[SupportedOn(100794368, Platform.Windows)]
                //[SupportedOn(100859904, Platform.WindowsPhone)]
                //PreviewRecordStreamsIdentical = 1,
                ////
                //// 摘要: 
                ////     预览图片流是相同的。
                //[SupportedOn(100794368, Platform.Windows)]
                //[SupportedOn(100859904, Platform.WindowsPhone)]
                //PreviewPhotoStreamsIdentical = 2,
                ////
                //// 摘要: 
                ////     视频和图片流是相同的。
                //[SupportedOn(100794368, Platform.Windows)]
                //[SupportedOn(100859904, Platform.WindowsPhone)]
                //RecordPhotoStreamsIdentical = 3,
                ////
                //// 摘要: 
                ////     所有流都是相同的。
                //[SupportedOn(100794368, Platform.Windows)]
                //[SupportedOn(100859904, Platform.WindowsPhone)]
                //AllStreamsIdentical = 4,

                try
                { CameraShowInfo += "Exposure:" + vdc.Exposure.Capabilities.Supported + ",AutoModeSupported:" + vdc.Exposure.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Exposure:False\r\n"; }
                try
                { CameraShowInfo += "ExposureControl:" + vdc.ExposureControl.Supported + ",Auto" + vdc.ExposureControl.Auto + "\r\n"; }
                catch { CameraShowInfo += "ExposureControl:False\r\n"; }
                try
                { CameraShowInfo += "Focus:" + vdc.Focus.Capabilities.Supported + ",AutoModeSupported" + vdc.Focus.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Focus:False\r\n"; }
                try
                { CameraShowInfo += "FocusControl:" + vdc.FocusControl.Supported + "\r\n"; }
                catch { CameraShowInfo += "FocusControl:False\r\n"; }
                try
                { CameraShowInfo += "IsoSpeedControl:" + vdc.IsoSpeedControl.Supported + "\r\n"; }
                catch { CameraShowInfo += "IsoSpeedControl:False\r\n"; }
                try
                { CameraShowInfo += "LowLagPhoto-HardwareAcceleratedThumbnailSupported:" + vdc.LowLagPhoto.HardwareAcceleratedThumbnailSupported + "\r\n"; }
                catch { CameraShowInfo += "LowLagPhoto:False\r\n"; }
                try
                { CameraShowInfo += "LowLagPhotoSequence:" + vdc.LowLagPhotoSequence.Supported + ",HardwareAcceleratedThumbnailSupported:" + vdc.LowLagPhotoSequence.HardwareAcceleratedThumbnailSupported + "\r\n"; }
                catch { CameraShowInfo += "LowLagPhotoSequence:False\r\n"; }
                try
                { CameraShowInfo += "RegionsOfInterestControl-MaxRegions:" + vdc.RegionsOfInterestControl.MaxRegions + ",AutoExposureSupported:" + vdc.RegionsOfInterestControl.AutoExposureSupported + ",AutoFocusSupported:" + vdc.RegionsOfInterestControl.AutoFocusSupported + ",AutoWhiteBalanceSupported:" + vdc.RegionsOfInterestControl.AutoWhiteBalanceSupported + "\r\n"; }
                catch { CameraShowInfo += "RegionsOfInterestControl:False\r\n"; }
                try
                { CameraShowInfo += "Zoom:" + vdc.Zoom.Capabilities.Supported + ",AutoModeSupported:" + vdc.Zoom.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Zoom:False\r\n"; }
                try
                { CameraShowInfo += "BacklightCompensation:" + vdc.BacklightCompensation.Capabilities.Supported + ",AutoModeSupported:" + vdc.BacklightCompensation.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "BacklightCompensation:False\r\n"; }
                try
                { CameraShowInfo += "Brightness:" + vdc.Brightness.Capabilities.Supported + ",AutoModeSupported:" + vdc.Brightness.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Brightness:False\r\n"; }
                try
                { CameraShowInfo += "Contrast:" + vdc.Contrast.Capabilities.Supported + ",AutoModeSupported:" + vdc.Contrast.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Contrast:False\r\n"; }
                try
                { CameraShowInfo += "ExposureCompensationControl:" + vdc.ExposureCompensationControl.Supported + "\r\n"; }
                catch { CameraShowInfo += "ExposureCompensationControl:False\r\n"; }
                try
                { CameraShowInfo += "FlashControl:" + vdc.FlashControl.Supported + ",Auto:" + vdc.FlashControl.Auto + ",Enabled:" + vdc.FlashControl.Enabled + ",PowerPercent:" + vdc.FlashControl.PowerPercent + ",PowerSupported:" + vdc.FlashControl.PowerSupported + ",RedEyeReduction:" + vdc.FlashControl.RedEyeReduction + ",RedEyeReductionSupported:" + vdc.FlashControl.RedEyeReductionSupported + "\r\n"; }
                catch { CameraShowInfo += "FlashControl:False\r\n"; }
                try
                { CameraShowInfo += "Hue:" + vdc.Hue.Capabilities.Supported + ",AutoModeSupported:" + vdc.Hue.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Hue:False\r\n"; }
                try
                { CameraShowInfo += "Pan:" + vdc.Pan.Capabilities.Supported + ",AutoModeSupported:" + vdc.Pan.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Pan:False\r\n"; }
                try
                { CameraShowInfo += "Roll:" + vdc.Roll.Capabilities.Supported + ",AutoModeSupported:" + vdc.Roll.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Roll:False\r\n"; }
                try
                { CameraShowInfo += "Tilt:" + vdc.Tilt.Capabilities.Supported + ",AutoModeSupported:" + vdc.Tilt.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "Tilt:False\r\n"; }
                try
                { CameraShowInfo += "TorchControl:" + vdc.TorchControl.Supported + ",Enabled:" + vdc.TorchControl.Enabled + ",PowerPercent:" + vdc.TorchControl.PowerPercent + ",PowerSupported:" + vdc.TorchControl.PowerSupported + "\r\n"; }
                catch { CameraShowInfo += "TorchControl:False\r\n"; }
                try
                { CameraShowInfo += "WhiteBalance:" + vdc.WhiteBalance.Capabilities.Supported + ",AutoModeSupported:" + vdc.WhiteBalance.Capabilities.AutoModeSupported + "\r\n"; }
                catch { CameraShowInfo += "WhiteBalance:False\r\n"; }
                try
                { CameraShowInfo += "WhiteBalanceControl:" + vdc.WhiteBalanceControl.Supported + "\r\n"; }
                catch { CameraShowInfo += "WhiteBalanceControl:False\r\n"; }
            }
            catch (Exception ex)
            {
                CameraShowInfo = ex.ToString();
            }
            return true;
        }
        /// <summary>
        /// 获得摄像头列表
        /// </summary>
        private static async Task<bool> GetCameraList()
        {
            //获得摄像头列表
            if (CameraInfoList == null) { CameraInfoList = new List<CameraInfoViewModel>(); }
            CameraInfoList.Clear();
            foreach (var item in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
            {
                CameraInfoList.Add(new CameraInfoViewModel()
                {
                    Id = item.Id,
                    Name = item.Name,
                    Data = item,
                });
                if (item.EnclosureLocation == null) { continue; }
                if (string.IsNullOrEmpty(OldCameraId) == true)
                {
                    if (item.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Back)
                    {
                        NowCamera = item;
                        return true;
                    }
                }
                else
                {
                    if (OldCameraId == item.Id)
                    {
                        NowCamera = item;
                        return true;
                    }
                }
            }
            OldCameraId = string.Empty;
            if (CameraInfoList.Count > 0) { NowCamera = CameraInfoList[0].Data; }
            return true;
        }
        /// <summary>
        /// 获得分辨率
        /// </summary>
        private static void GetPhotoSize(MediaStreamType type, List<CameraSizeInfo> list)
        {
            var pList = MainCamera.VideoDeviceController.GetAvailableMediaStreamProperties(type);
            var infoList = new Dictionary<double, CameraSizeInfo>();
            foreach (var item in pList)
            {
                CameraSizeInfo psivm = null;
                var imageSize = item as ImageEncodingProperties;
                if (imageSize == null)
                {
                    var videoSize = item as VideoEncodingProperties;
                    if (videoSize != null)
                    {
                        psivm = new CameraSizeInfo(videoSize, videoSize.Width, videoSize.Height);
                    }
                }
                else
                {
                    psivm = new CameraSizeInfo(imageSize, imageSize.Width, imageSize.Height);
                }
                if (psivm == null) { continue; }
                if (infoList.ContainsKey(psivm.SizeTag) == true) { continue; }
                infoList.Add(psivm.SizeTag, psivm);
            }
            if (list != null) { list.Clear(); }
            foreach (var item in infoList.Values.OrderBy(li => li.SizeTag))
            {
                list.Add(item);
            }
        }
        /// <summary>
        /// 设置焦点
        /// </summary>
        public static async Task SetROI(double left, double top, double width, double height)
        {
            //LogHelper.AddLog("SetROI", "Enter");
            //try
            //{
            //    if (IsSupportROI == false) { return; }
            //    if (left < 0) { left = 0; }
            //    else if (left + width > CurrentPreviewW) { left = CurrentPreviewW - width; }
            //    if (top < 0) { top = 0; }
            //    else if (top + height > CurrentPreviewH) { top = CurrentPreviewH - height; }
            //    var roi = new RegionOfInterest();
            //    roi.AutoFocusEnabled = IsSetFocusAF;
            //    roi.AutoExposureEnabled = IsSetFocusAE;
            //    roi.AutoWhiteBalanceEnabled = false;
            //    roi.Bounds = new Rect(left, top, width, height);
            //    try
            //    {
            //        await MainCamera.VideoDeviceController.RegionsOfInterestControl.SetRegionsAsync(new List<RegionOfInterest>() { roi });
            //    }
            //    catch (Exception ex)
            //    {
            //        LogHelper.AddLog("SetROI", ex.ToString());
            //    }
            //}
            //catch (Exception ex)
            //{
            //    LogHelper.AddLog("SetROI",ex.ToString());
            //}
            //LogHelper.AddLog("SetROI", "Leave");
        }

    }
}