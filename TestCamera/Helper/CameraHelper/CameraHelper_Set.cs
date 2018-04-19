using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;

namespace TestCamera
{
    /// <summary>
    /// 摄像头辅助者，设置相关
    /// </summary>
    public static partial class CameraHelper
    {

        #region 公开方法

        /// <summary>
        /// 设置缩放
        /// </summary>
        public static async void SetZoom(double zoomNumber, bool isInit = false)
        {
            try
            {
                if (IsSupportHWZoom == true)
                {
                    MainCamera.VideoDeviceController.Zoom.TrySetValue(zoomNumber);
                }
                else
                {
                    //if (AppHelper.OneWorkLock == true) { return; }
                    //AppHelper.OneWorkLock = true;
                    //try
                    //{
                    //    var file = await ApplicationData.Current.RoamingFolder.CreateFileAsync("ZoomParas.txt", CreationCollisionOption.OpenIfExists);
                    //    await FileIO.WriteTextAsync(file, zoomNumber.ToString());

                    //    //EngineWrapperHelper.HelperObj.ZoomSet.setZoomPercent((float)zoomNumber);

                    //    if (IsZoomEffectAdded == false)
                    //    {
                    //        await MainCamera.AddEffectAsync(MediaStreamType.VideoPreview, "ZoomScaleTransform.ZoomScaleEffect", null);
                    //        IsZoomEffectAdded = true;
                    //    }
                    //}
                    //catch (Exception ex) { LogHelper.AddError(ex); }
                    //finally { AppHelper.OneWorkLock = false; }
                }
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// 设置焦点
        /// </summary>
        public static async Task SetROI(double left, double top, double width, double height)
        {
            try
            {
                if (IsSupportROI == false) { return; }
                if (left < 0) { left = 0; }
                else if (left + width > CurrentPreviewW) { left = CurrentPreviewW - width; }
                if (top < 0) { top = 0; }
                else if (top + height > CurrentPreviewH) { top = CurrentPreviewH - height; }
                var roi = new RegionOfInterest();
                roi.AutoFocusEnabled = IsSetFocusAF;
                roi.AutoExposureEnabled = IsSetFocusAE;
                roi.AutoWhiteBalanceEnabled = false;
                roi.Bounds = new Rect(left, top, width, height);
                try
                {
                    await MainCamera.VideoDeviceController.RegionsOfInterestControl.SetRegionsAsync(new List<RegionOfInterest>() { roi });
                }
                catch (Exception ex)
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        public static async Task ClearROI()
        {
            try
            {
                await MainCamera.VideoDeviceController.RegionsOfInterestControl.ClearRegionsAsync();
            }
            catch(Exception ex)
            {
            }
        }

        public static async Task SetFocus(int newFocus)
        {
            try
            {
                await MainCamera.VideoDeviceController.FocusControl.SetPresetAsync(FocusPreset.Manual, true);
                await MainCamera.VideoDeviceController.FocusControl.SetValueAsync((uint)newFocus);
            }
            catch(Exception ex)
            {
            }
        }
        /// <summary>
        /// 对设置摄像头方向进行旋转
        /// </summary>
        public static void SetCameraRotation(DisplayOrientations orientations)
        {
            var cameraRotation = VideoRotation.None;
            switch (orientations)
            {
                case DisplayOrientations.Landscape:
                    cameraRotation = VideoRotation.None;
                    break;
                case DisplayOrientations.Portrait:
                    cameraRotation = ReversePreviewRotation == true ? VideoRotation.Clockwise270Degrees : VideoRotation.Clockwise90Degrees;
                    break;
                case DisplayOrientations.LandscapeFlipped:
                    cameraRotation = VideoRotation.Clockwise180Degrees;
                    break;
                case DisplayOrientations.PortraitFlipped:
                    cameraRotation = ReversePreviewRotation == true ? VideoRotation.Clockwise90Degrees : VideoRotation.Clockwise270Degrees;
                    break;
                default:
                    cameraRotation = VideoRotation.None;
                    break;
            }
            try
            {
                LogHelper.AddString(cameraRotation.ToString());
                MainCamera.SetPreviewRotation(cameraRotation);
            }
            catch (Exception ex) { }
            try
            {
                //MainCamera.SetRecordRotation(cameraRotation);
            }
            catch (Exception ex) { }
        }

        /// <summary>
        /// 设置曝光控制器
        /// </summary>
        public static async Task SetExposure(bool isAuto,TimeSpan timeSpan)
        {
            try
            {
                await MainCamera.VideoDeviceController.ExposureControl.SetAutoAsync(isAuto);
                if (isAuto == false && timeSpan != null)
                {
                    await MainCamera.VideoDeviceController.ExposureControl.SetValueAsync(timeSpan);
                }
            }
            catch(Exception ex)
            {
            }
        }

        /// <summary>
        /// 设置IsoSpeed
        /// </summary>
        public static async Task SetIsoSpeed(IsoSpeedPreset isoSpeed)
        {
            try
            {
                await MainCamera.VideoDeviceController.IsoSpeedControl.SetPresetAsync(isoSpeed);
            }
            catch(Exception ex)
            {
            }
        }

        public static async Task ClearEffect()
        {
            try
            {
                await MainCamera.ClearEffectsAsync(Windows.Media.Capture.MediaStreamType.VideoRecord);
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 设置分辨率
        /// </summary>
        private static async Task SetSize()
        {
            try
            {
                var width = ConfigHelper.Info.CameraPhotoSizeWidth;
                var height = ConfigHelper.Info.CameraPhotoSizeHeight;
                if (IsHavePhotoStream == true)
                {
                    //获得分辨率
                    CameraSizeInfo photoSize = null;
                    foreach (var item in CameraPhotoSizeList)
                    {
                        if (item.Width == width && item.Height == height)
                        {
                            photoSize = item;
                            break;
                        }
                    }
                    if (photoSize == null)
                    {
                        var maxTag = CameraPhotoSizeList.Max(li => li.SizeTag);
                        photoSize = CameraPhotoSizeList.FirstOrDefault(li => li.SizeTag == maxTag);
                    }
                    var photoRato = Convert.ToDouble(photoSize.Width) / photoSize.Height;
                    var tempPreviewList = CameraPreviewSizeList.Where(li => Convert.ToDouble(li.Width) / li.Height == photoRato);
                    CameraSizeInfo previewSize = null;
                    if (tempPreviewList.Count() > 0)
                    {
                        var maxTag = tempPreviewList.Max(li => li.SizeTag);
                        previewSize = tempPreviewList.FirstOrDefault(li => li.SizeTag == maxTag);
                    }
                    else
                    {
                        var maxTag = CameraPreviewSizeList.Max(li => li.SizeTag);
                        previewSize = CameraPreviewSizeList.FirstOrDefault(li => li.SizeTag == maxTag);
                    }
                    if (previewSize == null)
                    {
                        return;
                    }
                    //设置
                    await MainCamera.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, previewSize.Data);
                    await MainCamera.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, photoSize.Data);
                }
                else
                {
                    //获得分辨率
                    CameraSizeInfo nowSize = null;
                    foreach (var item in CameraPreviewSizeList)
                    {
                        if (item.Width == width && item.Height == height)
                        {
                            nowSize = item;
                            break;
                        }
                    }
                    if (nowSize == null)
                    {
                        var maxTag = CameraPreviewSizeList.Max(li => li.SizeTag);
                        nowSize = CameraPreviewSizeList.FirstOrDefault(li => li.SizeTag == maxTag);
                    }
                    //设置
                    await MainCamera.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview, nowSize.Data);
                }
                //获得设置分辨率
                var settingVideoRes = (VideoEncodingProperties)MainCamera.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
                CurrentPreviewW = settingVideoRes.Width;
                CurrentPreviewH = settingVideoRes.Height;
                //获得设置分辨率
                if (IsPhotoFromImagePropStream == true)
                {
                    var settingPhotoRes = (ImageEncodingProperties)MainCamera.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo);
                    CurrentPhotoW = settingPhotoRes.Width;
                    CurrentPhotoH = settingPhotoRes.Height;
                }
                else
                {
                    var settingPhotoRes = (VideoEncodingProperties)MainCamera.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.Photo);
                    CurrentPhotoW = settingPhotoRes.Width;
                    CurrentPhotoH = settingPhotoRes.Height;
                }
                //保存设置
                if (ConfigHelper.Info.CameraPhotoSizeWidth != CurrentPhotoW || ConfigHelper.Info.CameraPhotoSizeHeight != CurrentPhotoH)
                {
                    ConfigHelper.Info.CameraPhotoSizeWidth = CurrentPhotoW;
                    ConfigHelper.Info.CameraPhotoSizeHeight = CurrentPhotoH;
                    ConfigHelper.Save();
                }
            }
            catch (Exception ex)
            {
            }
        }

        #endregion

    }
}