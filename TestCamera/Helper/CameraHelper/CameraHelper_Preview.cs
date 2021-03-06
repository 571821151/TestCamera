﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;

namespace TestCamera
{
    /// <summary>
    /// 摄像头辅助者，预览相关
    /// </summary>
    public static partial class CameraHelper
    {

        #region 公开方法
        public static event Action CameraLoadedEvent;

        public static bool bExposureAuto = true;
        public static bool bFocus = true; 
        public static bool bBacklightCompensation = false;
        public static bool bWhiteBalance = false;

        /// <summary>
        /// 初始化摄像头并启动预览
        /// </summary>
        public static async void InitCamera(bool isNewInit = true, bool isMinZoom = false)
        {
            try
            {
                AppPathHelper.Init();
                ConfigHelper.Init();
                if (isNewInit == true && IsLoading == true)
                {
                    TriggerInitCameraOver(ErrorMessageType.Camera_Start_IsLoading);
                    if (CameraLoadedEvent != null)
                    {
                        CameraLoadedEvent();
                    }
                    return;
                }
                IsLoading = true;
                if (isNewInit == true)
                {
                    FailedIndex = 1;
                    IsZoomEffectAdded = false;
                }
                //使用UI线程进行初始化，因为需要一些事件中进行调用或重试，这些事件可能在子线程中。
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    IsCanSetCamera = false;
                    IsCanUseCamera = false;
                });
                try
                {
                    await StopPreview();
                    await LoadDevice();
                    if (CurrentCamDevice == null)
                    {
                        TriggerInitCameraOver(ErrorMessageType.Camera_Start_NoDevice);
                        if (CameraLoadedEvent != null)
                        {
                            CameraLoadedEvent();
                        }
                        return;
                    }
                    var useRes = IsCanUseCurrentDevice();
                    if (useRes != ErrorMessageType.None)
                    {
                        TriggerInitCameraOver(useRes);
                        if (CameraLoadedEvent != null)
                        {
                            CameraLoadedEvent();
                        }
                        return;
                    }
                    var res = await InitCamera();
                    if (!res)
                    {
                        TriggerInitCameraOver(ErrorMessageType.Camera_Start_UserDenied);
                        if (CameraLoadedEvent != null)
                        {
                            CameraLoadedEvent();
                        }
                        return;
                    }
                    if (isMinZoom)
                    { AppHelper.CameraZoomNumber = MinZoomSetting; }
                    SetZoom(AppHelper.CameraZoomNumber, true);
                    GetPreviewSize();
                    GetPhotoSize();
                    if (CameraPhotoSizeList != null)
                    {
                        IsHavePhotoStream = CameraPhotoSizeList.Count > 0;
                    }
                    else
                    {
                        IsHavePhotoStream = false;
                    }
                    //获取录像分辨率，在摄像头初始化的时候判断是否支持两种录像的分辨率。
                    var recordSize = MainCamera.VideoDeviceController
                        .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)
                        .Where(li => li is VideoEncodingProperties)
                        .Select(li => li as VideoEncodingProperties);
                    var cameraSize = CameraHelper.GetSizeList();
                    var oneRecordSize = RecordSizeList[0];
                    var twoRecordSize = RecordSizeList[1];
                    IsCanRecord =
                        recordSize.Count(li => li.Width == oneRecordSize.Width && li.Height == oneRecordSize.Height) > 0 &&
                        cameraSize.Count(li =>
                            (li.Width == oneRecordSize.Width && li.Height == oneRecordSize.Height) ||
                            (li.Width == twoRecordSize.Width && li.Height == twoRecordSize.Height)) > 0;
                    //设置分辨率
                    await SetSize();
                    MainCamera.Failed += async (ds, de) =>
                    {
                        try
                        {
                            await StopPreview();
                        }
                        catch (Exception ex)
                        {
                        }
                        switch (de.Code)
                        {
                            case 0xC00D3EA3:
                            case 0xC00D3704:
                                TriggerInitCameraOver(ErrorMessageType.Camera_Start_AppLocked);
                                break;
                            case 0xC00D3E82:
                                if (FailedIndex <= 2)
                                {
                                    FailedIndex++;
                                    await Task.Delay(500);
                                    InitCamera(false);
                                }
                                else
                                {
                                    TriggerInitCameraOver(ErrorMessageType.Camera_Start_AppLocked);
                                }
                                break;
                            case 0x80070326:
                                TriggerInitCameraOver(ErrorMessageType.Camera_Start_NoAccessRight);
                                break;
                        }
                    };
                    if (OnNeedSetControls != null) { await OnNeedSetControls(MainCamera); }
                    await MainCamera.StartPreviewAsync();
                    //await PrepareLowLagPhoto();
                    //调用结束事件
                    TriggerInitCameraOver(ErrorMessageType.None);
                }
                catch (Exception ex)
                {
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                IsLoading = false;
                if (CameraLoadedEvent != null)
                {
                    CameraLoadedEvent();
                }
            }
        }
        /// <summary>
        /// 停止预览
        /// </summary>
        public static async Task StopPreview()
        {
            try
            {
                if (CameraPreviewSizeList != null)
                {
                    CameraPreviewSizeList.Clear();
                }
                CameraPreviewSizeList = null;
                if (CameraPhotoSizeList != null)
                {
                    CameraPhotoSizeList.Clear();
                }
                await CloseLowLagPhoto();
                CameraPhotoSizeList = null;
                IsSupportROI = false;
                IsSupportContinuousCapture = false;
                IsSupportHWZoom = false;
                if (MainCamera != null)
                {
                    try
                    {
                        await MainCamera.StopPreviewAsync();
                    }
                    catch (Exception ex)
                    {
                    }
                    try
                    {
                        MainCamera.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }
                }
                MainCamera = null;
                CurrentCamDevice = null;
                FailedIndex = 1;
                IsCanUseCamera = false;
            }
            catch (Exception ex)
            {
            }
        }

        public static async Task PrepareLowLagPhoto()
        {
            try
            {
                if (LowLagPhoto == null)
                {
                    DateTime start = DateTime.Now;
                    LowLagPhoto = await MainCamera.PrepareLowLagPhotoCaptureAsync(ImageEncodingProperties.CreateJpeg());
                    //SetZoom(CameraHelper.MinZoomSetting, true);
                    DateTime end = DateTime.Now;
                    LogHelper.AddString("PrepareLowLagPhoto time:" + (end - start).TotalMilliseconds.ToString());
                }
            }
            catch (Exception ex)
            {
                LogHelper.AddString(ex.Message);
            }
        }

        public static async Task CloseLowLagPhoto()
        {
            try
            {
                if (LowLagPhoto != null)
                {
                    await LowLagPhoto.FinishAsync();
                    LowLagPhoto = null;
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static List<CameraSizeInfo> GetSizeList()
        {
            if (CameraHelper.IsHavePhotoStream == true)
            {
                return CameraPhotoSizeList;
            }
            else
            {
                return CameraPreviewSizeList;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 触发初始化结束
        /// </summary>
        private static void TriggerInitCameraOver(ErrorMessageType type)
        {
            if (OnInitOver == null) { return; }
            OnInitOver(type);
        }
        /// <summary>
        /// 加载设备
        /// </summary>
        private static async Task LoadDevice()
        {
            try
            {
                ReversePreviewRotation = true;
                DeviceInformationCollection DevCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
                if (CameraDevicesList != null) { CameraDevicesList.Clear(); }
                CameraDevicesList = new List<CameraInfo>();
                CurrentCamDevice = null;
                foreach (var item in DevCollection)
                {
                    CameraDevicesList.Add(new CameraInfo() { Id = item.Id, Name = item.Name, Obj = item });
                    if (item.Id == SelectedCameraId)
                    {
                        CurrentCamDevice = item;
                        continue;
                    }
                }
                if (CurrentCamDevice == null && CameraDevicesList.Count > 0)
                {
                    CurrentCamDevice = CameraDevicesList.LastOrDefault().Obj;
                    SelectedCameraId = CurrentCamDevice.Id;
                }
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// 可以使用当前设备
        /// </summary>
        private static ErrorMessageType IsCanUseCurrentDevice()
        {
            try
            {
                var deviceInfo = DeviceAccessInformation.CreateFromId(CurrentCamDevice.Id);
                if (deviceInfo.CurrentStatus == DeviceAccessStatus.DeniedByUser)
                {
                    return ErrorMessageType.Camera_Start_UserDenied;
                }
                else if (deviceInfo.CurrentStatus == DeviceAccessStatus.DeniedBySystem)
                {
                    return ErrorMessageType.Camera_Start_SystemDenied;
                }
            }
            catch (Exception ex)
            {
            }
            return ErrorMessageType.None;
        }
        /// <summary>
        /// 初始化摄像头
        /// </summary>
        private static async Task<bool> InitCamera()
        {
            try
            {
                MainCamera = new MediaCapture();
                var setting = new MediaCaptureInitializationSettings();
                setting.VideoDeviceId = CurrentCamDevice.Id;
                setting.StreamingCaptureMode = StreamingCaptureMode.Video;
                setting.AudioDeviceId = "";
                var isOk = false;
                try
                {
                    setting.PhotoCaptureSource = PhotoCaptureSource.Auto;
                    await MainCamera.InitializeAsync(setting);
                    isOk = true;
                }
                catch (Exception ex)
                {
                }
                if (isOk == false)
                {
                    try
                    {
                        setting.PhotoCaptureSource = PhotoCaptureSource.VideoPreview;
                        await MainCamera.InitializeAsync(setting);
                        isOk = true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
                if (isOk == false) { return false; }
                //判断模式
                var vdc = MainCamera.MediaCaptureSettings.VideoDeviceCharacteristic;
                if (vdc != VideoDeviceCharacteristic.AllStreamsIdentical &&
                    vdc != VideoDeviceCharacteristic.PreviewPhotoStreamsIdentical &&
                    vdc != VideoDeviceCharacteristic.RecordPhotoStreamsIdentical)
                {
                    PhotoSource = PhotoCaptureSource.Photo;
                }
                else
                {
                    PhotoSource = PhotoCaptureSource.VideoPreview;
                }

                isConcurrentRecordAndPhotoSupport = MainCamera.MediaCaptureSettings.ConcurrentRecordAndPhotoSequenceSupported | MainCamera.MediaCaptureSettings.ConcurrentRecordAndPhotoSupported;

                //判断设备支持属性
                var videoDevice = MainCamera.VideoDeviceController;
                if (videoDevice == null) { return true; }
                if (videoDevice.Exposure != null && videoDevice.Exposure.Capabilities.Supported == true)
                {
                    videoDevice.Exposure.TrySetAuto(bExposureAuto);
                }
                if (videoDevice.Focus != null && videoDevice.Focus.Capabilities.Supported == true)
                {
                    videoDevice.Focus.TrySetAuto(bFocus);
                }
                if (videoDevice.BacklightCompensation != null && videoDevice.BacklightCompensation.Capabilities.Supported == true)
                {
                    videoDevice.BacklightCompensation.TrySetAuto(bBacklightCompensation);
                }
                if (videoDevice.WhiteBalance != null && videoDevice.WhiteBalance.Capabilities.Supported == true)
                {
                    videoDevice.WhiteBalance.TrySetAuto(bWhiteBalance);
                }

                if (videoDevice.Zoom != null && videoDevice.Zoom.Capabilities.Supported == true)
                { IsSupportHWZoom = true; }
                else
                { IsSupportHWZoom = false; }
                if (videoDevice.LowLagPhotoSequence != null && videoDevice.LowLagPhotoSequence.Supported == true)
                { IsSupportContinuousCapture = true; }
                else
                { IsSupportContinuousCapture = false; }
                IsSetFocusAF = videoDevice.Focus.Capabilities.Supported == true && videoDevice.Focus.Capabilities.Max - videoDevice.Focus.Capabilities.Min > 1.0;
                IsSetFocusAE = videoDevice.Brightness.Capabilities.Supported == true || videoDevice.Exposure.Capabilities.Supported == true;
                //如果支持缩放，则获取缩放范围
                if (IsSupportHWZoom == true)
                {
                    var zoomObj = videoDevice.Zoom.Capabilities;
                    MinZoomSetting = zoomObj.Min;
                    MaxZoomSetting = zoomObj.Max;
                    StepZoomSeting = zoomObj.Step;
                }
                else
                {
                    MinZoomSetting = 100;
                    MaxZoomSetting = 400;
                    StepZoomSeting = 1;
                }
                //判断是否支持区域焦点
                IsSupportROI = false;
                try
                {
                    //videoDevice.GetDeviceProperty(AppDefaultHelper.CAMERA_ROI_GUID);
                    var tempRes = videoDevice.RegionsOfInterestControl.MaxRegions;
                    if (tempRes != 0) { IsSupportROI = true; }
                }
                catch (Exception ex)
                {
                }
                SetDevInfo();
            }
            catch (Exception ex)
            {
            }
            return true;
        }
        /// <summary>
        /// 获得预览分辨率
        /// </summary>
        private static void GetPreviewSize()
        {
            var previewSizeList = MainCamera.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
            var sizeList = new List<VideoEncodingProperties>();
            var listCount = previewSizeList.Count;
            foreach (var item in previewSizeList)
            {
                var sizeItem = item as VideoEncodingProperties;
                if (sizeItem == null) { continue; }
                if (sizeItem.Width < AppDefaultHelper.CAMERA_VGA_WIDTH || sizeItem.Height < AppDefaultHelper.CAMERA_VGA_HEIGHT) { continue; }
                var oldItem = sizeList.FirstOrDefault(li => (li.Width == sizeItem.Width && li.Height == sizeItem.Height));
                if (oldItem != null)
                {
                    uint oldRate = oldItem.FrameRate.Numerator / oldItem.FrameRate.Denominator;
                    uint rate = sizeItem.FrameRate.Numerator / sizeItem.FrameRate.Denominator;
                    if (oldRate < rate)
                    {
                        sizeList.Remove(oldItem);
                    }
                    else { continue; }
                }
                sizeList.Add(sizeItem);
            }
            if (CameraPreviewSizeList == null)
            { CameraPreviewSizeList = new List<CameraSizeInfo>(); }
            else
            { CameraPreviewSizeList.Clear(); }
            foreach (var item in sizeList.OrderBy(li => li.Width))
            {
                CameraPreviewSizeList.Add(new CameraSizeInfo(item, item.Width, item.Height));
            }
        }
        /// <summary>
        /// 获得照片分辨率
        /// </summary>
        private static void GetPhotoSize()
        {
            if (PhotoSource == PhotoCaptureSource.VideoPreview) { return; }
            var photoSizeList = MainCamera.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo);
            var imageSizeList = new List<ImageEncodingProperties>();
            var videoSizeList = new List<VideoEncodingProperties>();
            foreach (var item in photoSizeList)
            {
                if (item.Type == "Image")
                {
                    var imageSize = (ImageEncodingProperties)item;
                    if (imageSize.Width < AppDefaultHelper.CAMERA_VGA_WIDTH || imageSize.Height < AppDefaultHelper.CAMERA_VGA_HEIGHT) { continue; }
                    imageSizeList.Add(imageSize);
                }
                else
                {
                    var videoSize = (VideoEncodingProperties)item;
                    if (videoSize.Width < AppDefaultHelper.CAMERA_VGA_WIDTH || videoSize.Height < AppDefaultHelper.CAMERA_VGA_HEIGHT) { continue; }
                    var oldItem = videoSizeList.FirstOrDefault(li => li.Width == videoSize.Width && li.Height == videoSize.Height);
                    if (oldItem == null) { videoSizeList.Add(videoSize); }
                }
            }
            if (CameraPhotoSizeList == null)
            { CameraPhotoSizeList = new List<CameraSizeInfo>(); }
            else
            { CameraPhotoSizeList.Clear(); }
            if (imageSizeList.Count > 0)
            {
                imageSizeList.Sort((Comparison<ImageEncodingProperties>)((p1, p2) =>
                {
                    if (p1 == null || p2 == null) { return 0; }
                    uint pixels0 = p1.Height * p1.Width;
                    uint pixels1 = p2.Height * p2.Width;
                    if (pixels0 > pixels1) { return 1; }
                    return (int)pixels0 == (int)pixels1 ? 0 : -1;
                }));
                foreach (var item in imageSizeList)
                {
                    CameraPhotoSizeList.Add(new CameraSizeInfo(item, item.Width, item.Height));
                }
                IsPhotoFromImagePropStream = true;
            }
            else if (videoSizeList.Count > 0)
            {
                videoSizeList.Sort((Comparison<VideoEncodingProperties>)((p1, p2) =>
                {
                    if (p1 == null || p2 == null) { return 0; }
                    uint pixels0 = p1.Height * p1.Width;
                    uint pixels1 = p2.Height * p2.Width;
                    if (pixels0 > pixels1) { return 1; }
                    return (int)pixels0 == (int)pixels1 ? 0 : -1;
                }));
                foreach (var item in videoSizeList)
                {
                    CameraPhotoSizeList.Add(new CameraSizeInfo(item, item.Width, item.Height));
                }
                IsPhotoFromImagePropStream = false;
            }
        }

        private static void SetDevInfo()
        {
            //组织设备信息
            try
            {
                CameraShowInfo = string.Empty;
                var vdc = MainCamera.VideoDeviceController;

                string strVideoDeviceCharacteristic = "";

                switch (MainCamera.MediaCaptureSettings.VideoDeviceCharacteristic)
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
                try
                {
                    CameraShowInfo += "SceneModeControl:\r\n";
                    foreach (var item in vdc.SceneModeControl.SupportedModes)
                    {
                        CameraShowInfo += item.ToString() + "\r\n";
                    }
                }
                catch { CameraShowInfo += "SceneModeControl:False\r\n"; }
            }
            catch (Exception ex)
            {
                CameraShowInfo = ex.ToString();
            }
        }

        #endregion

    }
}