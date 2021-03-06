﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TestCamera
{
    /// <summary>
    /// 摄像头辅助者，拍照相关
    /// </summary>
    public static partial class CameraHelper
    {

        #region 公开方法

        /// <summary>
        /// 直接拍摄
        /// </summary>
        public static async void SingleCapture(bool isFirst)
        {
            try
            {
                var file = await FirstCapture(isFirst);
                if (file == null) { TriggerCaptureOver(null, CaptureOverResType.Error); }
                else
                { TriggerCaptureOver(new List<StorageFile>() { file }, CaptureOverResType.Normal); }
            }
            catch (Exception ex)
            {
                TriggerCaptureOver(null, CaptureOverResType.Error);
            }
        }
        /// <summary>
        /// 高级多张拍摄
        /// </summary>
        public static async void ContinuousCapture()
        {
            try
            {
                await CloseLowLagPhoto();
                //设置拍摄速度
                var maxCount = 5U;
                var tempCount = MainCamera.VideoDeviceController.LowLagPhotoSequence.MaxPastPhotos;
                if (maxCount > tempCount) { maxCount = tempCount; }
                MainCamera.VideoDeviceController.LowLagPhotoSequence.PastPhotoLimit = maxCount;
                var tempPerCount = MainCamera.VideoDeviceController.LowLagPhotoSequence.MaxPhotosPerSecond;
                if (maxCount < tempPerCount) { tempPerCount = maxCount; }
                MainCamera.VideoDeviceController.LowLagPhotoSequence.PhotosPerSecondLimit = tempPerCount;
                //开始拍摄
                var rotation = GetBitmapRotation();
                var directSave = true;
                //判断处理状态
                if (rotation == BitmapRotation.None)
                {
                    if (IsHavePhotoStream == true && IsSupportHWZoom == false)
                    { directSave = false; }
                    else
                    { directSave = true; }
                }
                else { directSave = false; }
                var captureObj = await MainCamera.PrepareLowLagPhotoSequenceCaptureAsync(ImageEncodingProperties.CreateJpeg());
                var nowCount = 0;
                var frameList = new List<CapturedFrame>();
                captureObj.PhotoCaptured += async (ds, de) =>
                {
                    nowCount++;
                    if (maxCount < nowCount) { return; }
                    else if (maxCount > nowCount) { frameList.Add(de.Frame); }
                    else if (maxCount == nowCount)
                    {
                        //当到达最后的一张的时候，进入处理阶段
                        frameList.Add(de.Frame);
                        //停止拍摄
                        try
                        {
                            await captureObj.StopAsync();
                        }
                        catch (Exception ex)
                        {
                        }
                        try
                        {
                            await captureObj.FinishAsync();
                        }
                        catch (Exception ex)
                        {
                        }
                        try
                        {
                            var fileList = new List<StorageFile>();
                            //保存所有图片
                            var listCount = frameList.Count;
                            for (int i = 0; i < listCount; i++)
                            {
                                var frame = frameList[i];
                                if (frame == null) { continue; }
                                using (var stream = frame.CloneStream())
                                {
                                    if (stream == null) { continue; }
                                    if (stream.Size < 1)
                                    {
                                        TriggerCaptureOver(null, CaptureOverResType.Error);
                                        break;
                                    }
                                    var file = await AppPathHelper.GetTempPhotoFile(i + 1);
                                    fileList.Add(file);
                                    //if (directSave == true)
                                    {
                                        using (var saveStream = await file.OpenStreamForWriteAsync())
                                        {
                                            await RandomAccessStream.CopyAndCloseAsync(stream, saveStream.AsOutputStream());
                                        }
                                    }
                                    //else
                                    //{
                                    //    await RotationZoomImage(stream, file, rotation,(int)AppHelper.CameraZoomNumber);
                                    //}
                                }
                            }
                            //await PrepareLowLagPhoto();
                            TriggerCaptureOver(fileList, CaptureOverResType.BurstCapture_Over);
                        }
                        catch (Exception ex)
                        {
                            TriggerCaptureOver(null, CaptureOverResType.Error);
                        }
                    }
                };
                await captureObj.StartAsync();
            }
            catch (Exception ex)
            {
                LogHelper.AddString(ex.Message);
                TriggerCaptureOver(null, CaptureOverResType.Error);
            }
        }

        public static async void LowLagCapture(bool isFirst)
        {
            try
            {
                var file = await LowLagCapturePhoto(isFirst);
                if (file == null) { TriggerCaptureOver(null, CaptureOverResType.Error); }
                else
                { TriggerCaptureOver(new List<StorageFile>() { file }, CaptureOverResType.Normal); }
            }
            catch (Exception ex)
            {
                TriggerCaptureOver(null, CaptureOverResType.Error);
            }
        }

        /// <summary>
        /// //////////////////////////////////////////////////////
        /// </summary>
        /// <param name="isFirst"></param>
        /// <returns></returns>
        public static async Task<StorageFile> LowLagCapturePhoto(bool isFirst)
        {
            var file = await AppPathHelper.GetTempPhotoFile(isFirst == true ? -10 : 0);
            if (file == null) { return null; }
            try
            {
                var rotation = GetBitmapRotation();
                var directSave = true;
                if (rotation == BitmapRotation.None)
                {
                    if (IsHavePhotoStream == true && IsSupportHWZoom == false)
                    { directSave = false; }
                    else
                    { directSave = true; }
                }
                else { directSave = false; }
                var photoData = await LowLagPhoto.CaptureAsync();
                var photoFrame = photoData.Frame;
                if (photoFrame.Size < 1) { return null; }
                using (var stream = photoFrame.CloneStream())
                {
                    //if (directSave == true)
                    {
                        using (var saveStream = await file.OpenStreamForWriteAsync())
                        {
                            await RandomAccessStream.CopyAndCloseAsync(stream, saveStream.AsOutputStream());
                        }
                    }
                   // else
                    {
                   //     await RotationZoomImage(stream, file, rotation, (int)AppHelper.CameraZoomNumber);
                    }
                }

                await Task.Delay(200);
            }
            catch (Exception ex)
            {
            }
            return file;
        }
        public static async Task<StorageFile> FirstCapture(bool isFirst)
        {
            //await Task.Delay(2000);//TODO//TEST//延迟拍摄，用于测试

            var file = await AppPathHelper.GetTempPhotoFile(isFirst == true ? -10 : 0);
            if (file == null) { return null; }
            try
            {
                var rotation = GetBitmapRotation();
                var directSave = true;
                if (rotation == BitmapRotation.None)
                {
                    if (IsHavePhotoStream == true && IsSupportHWZoom == false)
                    { directSave = false; }
                    else
                    { directSave = true; }
                }
                else { directSave = false; }

                await MainCamera.CapturePhotoToStorageFileAsync(ImageEncodingProperties.CreateJpeg(), file);

                await Task.Delay(200);
                //using (var photoFrame = new InMemoryRandomAccessStream())
                //{
                //    await MainCamera.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(),photoFrame);

                //    //var photoData = await LowLagPhoto.CaptureAsync();
                //    //var photoFrame = photoData.Frame;
                //    if (photoFrame.Size < 1) { return null; }
                //    using (var stream = photoFrame.CloneStream())
                //    {
                //        if (directSave == true)
                //        {
                //            using (var saveStream = await file.OpenStreamForWriteAsync())
                //            {
                //                await RandomAccessStream.CopyAndCloseAsync(stream, saveStream.AsOutputStream());
                //            }
                //        }
                //        else
                //        {
                //            await RotationZoomImage(stream, file, rotation, (int)AppHelper.CameraZoomNumber);
                //        }
                //    }
                //}

            }
            catch (Exception ex)
            {
            }
            return file;
        }

        public static async Task Focus()
        {
            try
            {
                await MainCamera.VideoDeviceController.FocusControl.FocusAsync();
            }
            catch(Exception ex)
            {
            }
        }

        public static uint GetFocusValue(ref uint min, ref uint max)
        {
            try
            {
                min = MainCamera.VideoDeviceController.FocusControl.Min;
                max = MainCamera.VideoDeviceController.FocusControl.Max;

                return MainCamera.VideoDeviceController.FocusControl.Value;
            }
            catch(Exception ex)
            {
            }

            return 0;
        }

        //public static async Task<StorageFile> BeginRecordVideo()
        //{
        //    try
        //    {
        //        await CloseLowLagPhoto();
        //        if (CameraHelper.IsCanRecord == false) { return null; }
        //        var oneRecordSize = CameraHelper.RecordSizeList[0];
        //        //创建MP4
        //        MediaEncodingProfile recordProfile = MediaEncodingProfile.CreateMp4(oneRecordSize.SizeType);
        //        //添加录制效果
        //        await MainCamera.AddEffectAsync(MediaStreamType.VideoRecord, "DummyEffectTransform.DummyEffect", null);
        //        //添加录制分辨率
        //        var recordSize = MainCamera.VideoDeviceController
        //            .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)
        //            .Where(li => li is VideoEncodingProperties)
        //            .Select(li => li as VideoEncodingProperties)
        //            .FirstOrDefault(li => li.Width == oneRecordSize.Width && li.Height == oneRecordSize.Height);
        //        await MainCamera.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord, recordSize);
        //        //创建文件
        //        var file = await KnownFolders.VideosLibrary.CreateFileAsync("StitchingVideo.mp4", CreationCollisionOption.ReplaceExisting);
        //        //var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("StitchingVideo.mp4", CreationCollisionOption.ReplaceExisting);
        //        //开始录制
        //        await MainCamera.StartRecordToStorageFileAsync(recordProfile, file);
        //        return file;
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //    return null;
        //}

        //public static async Task StopRecordVideo()
        //{
        //    await MainCamera.StopRecordAsync();
        //    await PrepareLowLagPhoto();
        //}

        #endregion

        #region 私有方法

        /// <summary>
        /// 获得摄像头方向
        /// </summary>
        public static BitmapRotation GetBitmapRotation()
        {
            switch (DisplayInformation.GetForCurrentView().CurrentOrientation)
            {
                case DisplayOrientations.Landscape: return BitmapRotation.None;
                case DisplayOrientations.Portrait: return ReversePreviewRotation != true ? BitmapRotation.Clockwise90Degrees : BitmapRotation.Clockwise270Degrees;
                case DisplayOrientations.LandscapeFlipped: return BitmapRotation.Clockwise180Degrees;
                case DisplayOrientations.PortraitFlipped: return ReversePreviewRotation != true ? BitmapRotation.Clockwise270Degrees : BitmapRotation.Clockwise90Degrees;
                default: return BitmapRotation.None;
            }
        }

        /// <summary>
        /// 旋转缩放图片
        /// </summary>
        private static async Task RotationZoomImage(IRandomAccessStream photoStream, StorageFile saveFile, BitmapRotation rotation, int zoomNumber)
        {
            try
            {
                //var decoder = await BitmapDecoder.CreateAsync(photoStream);
                //var transform = new BitmapTransform();
                //transform.Rotation = rotation;
                //transform.ScaledWidth = (uint)decoder.PixelWidth;
                //transform.ScaledHeight = (uint)decoder.PixelHeight;
                //var pixelData = await decoder.GetPixelDataAsync(
                //    BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform,
                //    ExifOrientationMode.RespectExifOrientation, ColorManagementMode.ColorManageToSRgb);
                //using (var outStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
                //{
                //    outStream.Size = 0UL;
                //    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outStream);
                //    var newWidth = 0U;
                //    var newHeight = 0U;
                //    if (rotation == BitmapRotation.Clockwise270Degrees || rotation == BitmapRotation.Clockwise90Degrees)
                //    {
                //        newWidth = transform.ScaledHeight;
                //        newHeight = transform.ScaledWidth;
                //    }
                //    else
                //    {
                //        newWidth = transform.ScaledWidth;
                //        newHeight = transform.ScaledHeight;
                //    }
                //    var pixels = pixelData.DetachPixelData();
                //    if (IsHavePhotoStream == true && IsSupportHWZoom == false)
                //    {
                //        try
                //        {
                //            int depth = 4;
                //            var newPixelData = new byte[(int)(newWidth * newHeight * depth)];
                //            float offset = (float)((0.5 * zoomNumber - 50) / zoomNumber);
                //            //unsafe
                //            //{
                //            //    fixed (byte* pbSrc = pixels)
                //            //    fixed (byte* pbDst = newPixelData)
                //            //    {
                //            //        IntPtr ptrSrc = (IntPtr)pbSrc;
                //            //        IntPtr ptrDst = (IntPtr)pbDst;
                //            //        EngineWrapperHelper.HelperObj.Engine.ZoomInImage_RGB32(
                //            //            ptrSrc.ToInt32(),
                //            //            (int)(newWidth * depth),
                //            //            ptrDst.ToInt32(),
                //            //            (int)(newWidth * depth),
                //            //            (int)newWidth,
                //            //            (int)newHeight,
                //            //            depth,
                //            //            offset,
                //            //            offset,
                //            //            (float)zoomNumber);
                //            //    }
                //            //}
                //        }
                //        catch (Exception ex)
                //        {
                //            LogHelper.AddError(ex);
                //        }
                //    }
                //    encoder.SetPixelData(
                //        BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
                //        newWidth, newHeight, decoder.DpiX, decoder.DpiY, pixels);
                //    await encoder.FlushAsync();
                //}
            }
            catch (Exception ex)
            {
            }
        }
        /// <summary>
        /// 触发拍照结束
        /// </summary>
        private static void TriggerCaptureOver(List<StorageFile> fileList, CaptureOverResType overType)
        {
            //if (OnCaptureOver == null) { return; }
            try
            {
                OnCaptureOver(fileList, overType);
            }
            catch(Exception ex)
            {
            }
        }

        #endregion

    }
}