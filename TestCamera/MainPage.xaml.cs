using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media;
using Windows.Media.Devices;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

using TestCamera.Helper.ViewModel;
using Windows.Graphics.Display;

namespace TestCamera
{
    public sealed partial class MainPage : Page
    {
        private VideoViewModel VM;
        public MainPage()
        {
            this.InitializeComponent();
            VM = new VideoViewModel();
            DataContext = VM;
        }
        
        private bool IsSetCamera = false;

        private DateTime CaptureTime = DateTime.Now;

        private DateTime PrepareLowLagCaptureTime = DateTime.Now;

        #region 页面事件

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            //监听事件
            DisplayInformation.DisplayContentsInvalidated += DisplayInformation_DisplayContentsInvalidated;
            //改变页面布局
            DisplayInformation.GetForCurrentView().OrientationChanged += currentDisplay_OrientationChanged;

            SystemMediaTransportControls.GetForCurrentView().PropertyChanged += VideoPage_PropertyChanged;
            CameraHelper.OnInitOver += CameraHelper_OnInitOver;
            CameraHelper.OnNeedSetControls += CameraHelper_OnNeedSetControls;
            CameraHelper.OnCaptureOver += CameraHelper_OnCaptureOver;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
             
            DisplayInformation.DisplayContentsInvalidated -= DisplayInformation_DisplayContentsInvalidated;

            SystemMediaTransportControls.GetForCurrentView().PropertyChanged -= VideoPage_PropertyChanged;
            CameraHelper.OnInitOver -= CameraHelper_OnInitOver;
            CameraHelper.OnNeedSetControls -= CameraHelper_OnNeedSetControls;
            CameraHelper.OnCaptureOver -= CameraHelper_OnCaptureOver;
        }

        private async void VideoPage_PropertyChanged(SystemMediaTransportControls ds, SystemMediaTransportControlsPropertyChangedEventArgs de)
        {
            if (ds.SoundLevel == SoundLevel.Muted)
            {
                await CameraHelper.StopPreview();
            }
            else
            {
                CameraHelper.InitCamera();
                //await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { BottomAppBar.IsEnabled = false; });
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LogHelper.SetControl(txtLog);
            CameraHelper.InitCamera();
            BottomAppBar.IsEnabled = false;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void cbxCameraList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSetCamera == true) { return; }
            var info = cbxCameraList.SelectedItem as CameraInfo;
            if (info == null) { return; }
            CameraHelper.SelectedCameraId = info.Id;
            CameraHelper.InitCamera();
            BottomAppBar.IsEnabled = false;
        }

        private void cbxSizeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsSetCamera == true) { return; }
            var size = cbxSizeList.SelectedItem as CameraSizeInfo;
            if (size == null) { return; }
            ConfigHelper.Info.CameraPhotoSizeWidth = size.Width;
            ConfigHelper.Info.CameraPhotoSizeHeight = size.Height;
            ConfigHelper.Save();
            CameraHelper.InitCamera();
            BottomAppBar.IsEnabled = false;
        }

        private void sldZoom_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            CameraHelper.SetZoom(sldZoom.Value);
        }

        private void btnShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (txtLog.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                txtLog.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                txtLog.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            LogHelper.Clear();
        }

        private void btnClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            gvwImageList.Items.Clear();
        }

        private void btnShowCameraInfo_Click(object sender, RoutedEventArgs e)
        {
            tblCameraInfo.Text = CameraHelper.CameraShowInfo;
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            var dp = new DataPackage();
            dp.SetText(OldCameraHelper.CameraShowInfo.Replace("\r\n", "\r").Replace("\r", "\r\n"));
            Clipboard.SetContent(dp);
        }

        private void btnCaptureMorePhoto_Click(object sender, RoutedEventArgs e)
        {
            CaptureTime = DateTime.Now;
            BottomAppBar.IsEnabled = false;
            CameraHelper.ContinuousCapture();
        }

        private void btnNormalCapture_Click(object sender, RoutedEventArgs e)
        {
            CaptureTime = DateTime.Now;
            BottomAppBar.IsEnabled = false;
            CameraHelper.SingleCapture(false);
        }

        private async void btnMoire_Click(object sender, RoutedEventArgs e)
        {
            CaptureTime = DateTime.Now;
            BottomAppBar.IsEnabled = false;
            var allLength = Math.Min(CameraHelper.CurrentPreviewW, CameraHelper.CurrentPreviewH);
            var heafLength = allLength * 0.16;
            var sizeX = CameraHelper.CurrentPreviewW / 2;
            var sizeY = CameraHelper.CurrentPreviewH / 2;
            await CameraHelper.SetROI(sizeX - heafLength, sizeY - heafLength, heafLength * 2, heafLength * 2);
            var ts = TimeSpan.FromMilliseconds(33.3333333);
            await CameraHelper.SetExposure(false, ts);
            await CameraHelper.SetIsoSpeed(IsoSpeedPreset.Auto);
            await CameraHelper.Focus();
            var sf = await CameraHelper.FirstCapture(true);
            await CameraHelper.SetExposure(false, ts);
            uint min = 0, max = 0;
            var focusValue = (int)CameraHelper.GetFocusValue(ref min, ref max);
            if (focusValue < min) { focusValue = (int)min; }
            if (focusValue > max) { focusValue = (int)max; }
            await CameraHelper.ClearROI();
            await CameraHelper.SetFocus(focusValue);
            await Task.Delay(TimeSpan.FromMilliseconds(2000));
            CameraHelper.SingleCapture(false);
            CameraHelper_OnCaptureOver(new List<StorageFile>() { sf }, CaptureOverResType.Error);
        }

        #endregion

        #region 事件

        private async void CameraHelper_OnInitOver(ErrorMessageType obj)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    IsSetCamera = true;
                    cbxCameraList.Items.Clear();
                    CameraInfo selectedCamera = null;
                    foreach (var item in CameraHelper.CameraDevicesList)
                    {
                        cbxCameraList.Items.Add(item);
                        if (CameraHelper.SelectedCameraId == item.Id)
                        {
                            selectedCamera = item;
                        }
                    }
                    cbxCameraList.SelectedItem = selectedCamera;
                    cbxSizeList.Items.Clear();
                    var sizeList = CameraHelper.GetSizeList();
                    CameraSizeInfo selectedItem = null;
                    foreach (var item in sizeList)
                    {
                        cbxSizeList.Items.Add(item);
                        if (item.Width == CameraHelper.CurrentPhotoW && item.Height == CameraHelper.CurrentPhotoH)
                        {
                            selectedItem = item;
                        }
                    }
                    if (selectedItem == null && sizeList.Count > 0)
                    {
                        selectedItem = sizeList.First(li => li.SizeTag == sizeList.Max(li2 => li2.SizeTag));
                    }
                    cbxSizeList.SelectedItem = selectedItem;
                    sldZoom.Minimum = CameraHelper.MinZoomSetting;
                    sldZoom.Maximum = CameraHelper.MaxZoomSetting;
                    sldZoom.StepFrequency = CameraHelper.StepZoomSeting;
                    sldZoom.IsEnabled = CameraHelper.IsSupportHWZoom == true;
                    btnCaptureMorePhoto.IsEnabled = CameraHelper.IsSupportContinuousCapture == true;


                    AutoFocus.IsChecked = CameraHelper.bFocus;
                    AutoExposure.IsChecked = CameraHelper.bExposureAuto;
                    AutoBacklightCompensation.IsChecked = CameraHelper.bBacklightCompensation;
                    AutoWhiteBalance.IsChecked = CameraHelper.bWhiteBalance;

                }
                finally
                {
                    IsSetCamera = false;
                    BottomAppBar.IsEnabled = true;
                }
            });
        }

        private async Task CameraHelper_OnNeedSetControls(Windows.Media.Capture.MediaCapture media)
        {
            //cpeMain.Width  = ConfigHelper.Info.CameraPhotoSizeWidth;
            //cpeMain.Height = ConfigHelper.Info.CameraPhotoSizeHeight;
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { cpeMain.Source = media; });
        }

        private async void CameraHelper_OnCaptureOver(List<StorageFile> files, CaptureOverResType overType)
        {
            if (overType != CaptureOverResType.Error)
            {
                LogHelper.AddString("CaptureTime:" + (DateTime.Now - CaptureTime).TotalMilliseconds.ToString());
            }
            await  Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    foreach (var item in files)
                    {
                        using (var s = await item.OpenReadAsync())
                        {
                            var bitmap = new BitmapImage();
                            bitmap.SetSource(s);
                            var image = new Image();
                            image.Width = 128;
                            image.Height = 72;
                            image.Source = bitmap;
                            var thumbnailItem = new Windows.UI.Xaml.Controls.GridViewItem();
                            thumbnailItem.Content = image;
                            gvwImageList.Items.Add(thumbnailItem);
                        }

                        await Task.Delay(200);

                    }
                }
                catch( Exception ex)
                {
                    LogHelper.AddString(ex.Message);
                }
                finally
                {
                        BottomAppBar.IsEnabled = true;
                }
            });
        }

        private async void cbEnableLowLagCapture(object sender, RoutedEventArgs e)
        {
            CheckBox cbEnable = sender as CheckBox;

            if( cbEnable.IsChecked == true)
            {
                await CameraHelper.PrepareLowLagPhoto();

                btnLowLagCapture.IsEnabled = true;
                btnNormalCapture.IsEnabled = false;
            }
            else
            {
                await CameraHelper.CloseLowLagPhoto();
                
                btnLowLagCapture.IsEnabled = false;
                btnNormalCapture.IsEnabled = true;
            }
        }

        private async void btnClickLowLagCapture(object sender, RoutedEventArgs e)
        {
            CaptureTime = DateTime.Now;

            CameraHelper.LowLagCapture(false);
        }

        #endregion
        #region 屏幕旋转方法
        #region 屏幕旋转方法

        /// <summary>
        /// 需重新绘制，屏幕旋转
        /// </summary>
        private void DisplayInformation_DisplayContentsInvalidated(DisplayInformation sender, object args)
        {
            try
            {
                VM.ValidationRotation(DisplayInformation.GetForCurrentView().CurrentOrientation);
            }
            catch (Exception ex)
            {
                //LogHelper.AddError(ex);
            }
        }

        #endregion





        /// <summary>
        /// 应用新的页面大小，主要设置拍摄面板的宽高。
        /// </summary>
        private void ChangeVideoSize()
        {

            try
            {
                double pageWidth = ActualWidth, pageHeight = ActualHeight;
                var orientation = DisplayInformation.GetForCurrentView().CurrentOrientation;
                VM.SetRotation(orientation);
                var dataWidth = 0.0;
                var dataHeight = 0.0;
                if (orientation == DisplayOrientations.Portrait || orientation == DisplayOrientations.PortraitFlipped)
                {
                    dataWidth = CameraHelper.CurrentPreviewH;
                    dataHeight = CameraHelper.CurrentPreviewW;
                    pageHeight = ActualHeight ;
                }
                else
                {
                    dataWidth = CameraHelper.CurrentPreviewW;
                    dataHeight = CameraHelper.CurrentPreviewH;
                    pageWidth = ActualWidth;


                }
                if (dataWidth != 0 && dataHeight != 0)
                {
                    var valueWidth = 0;
                    var valueHeight = 0;

                    if (dataWidth / pageWidth > dataHeight / pageHeight)
                    {
                        valueWidth = Convert.ToInt32(pageWidth);
                        valueHeight = Convert.ToInt32(dataHeight * (pageWidth / dataWidth));
                    }
                    //竖
                    else
                    {
                        valueWidth = Convert.ToInt32(dataWidth * (pageHeight / dataHeight));
                        valueHeight = Convert.ToInt32(pageHeight);

                    }


                    cpeMain.Width = valueWidth;
                    cpeMain.Height = valueHeight;

                }

            }
            catch (Exception ex) { }

        }
         

        private void currentDisplay_OrientationChanged(DisplayInformation sender, object args)
        {
            ChangeVideoSize();
        }
		
        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (CameraHelper.MainCamera.VideoDeviceController.Focus != null && CameraHelper.MainCamera.VideoDeviceController.Focus.Capabilities.Supported == true)
                {
                    CameraHelper.bFocus = true;
                    AutoFocus.IsEnabled = true;
                    AutoFocus.IsChecked = true;
                }
                else
                {
                    CameraHelper.bFocus = false;
                    AutoFocus.IsEnabled = false;
                    AutoFocus.IsChecked = false;
                }

                if (CameraHelper.MainCamera.VideoDeviceController.Exposure != null && CameraHelper.MainCamera.VideoDeviceController.Exposure.Capabilities.Supported == true)
                {
                    AutoExposure.IsChecked = true;
                    AutoExposure.IsEnabled = true;
                    CameraHelper.bExposureAuto = true;
                }
                else
                {
                    AutoExposure.IsChecked = false;
                    AutoExposure.IsEnabled = false;
                    CameraHelper.bExposureAuto = false;
                }

                if (CameraHelper.MainCamera.VideoDeviceController.BacklightCompensation != null && CameraHelper.MainCamera.VideoDeviceController.BacklightCompensation.Capabilities.Supported == true)
                {
                    AutoBacklightCompensation.IsChecked = true;
                    AutoBacklightCompensation.IsEnabled = true;
                    CameraHelper.bBacklightCompensation = true;
                }
                else
                {
                    AutoBacklightCompensation.IsChecked = false;
                    AutoBacklightCompensation.IsEnabled = false;
                    CameraHelper.bBacklightCompensation = false;
                }

                if (CameraHelper.MainCamera.VideoDeviceController.WhiteBalance != null && CameraHelper.MainCamera.VideoDeviceController.WhiteBalance.Capabilities.Supported == true)
                {
                    AutoWhiteBalance.IsChecked = true;
                    AutoWhiteBalance.IsEnabled = true;
                    CameraHelper.bWhiteBalance = true;
                }
                else
                {
                    AutoWhiteBalance.IsChecked = false;
                    AutoWhiteBalance.IsEnabled = false;
                    CameraHelper.bWhiteBalance = false;
                }

            }
            catch (Exception ex)
            {

            }

        }

        private void cbAutoFocus(object sender, RoutedEventArgs e)
        {
            CameraHelper.bFocus = !CameraHelper.bFocus;
            AutoFocus.IsChecked = CameraHelper.bFocus;

            try
            {
                if (CameraHelper.MainCamera.VideoDeviceController.Focus != null && CameraHelper.MainCamera.VideoDeviceController.Focus.Capabilities.Supported == true)
                {
                    CameraHelper.MainCamera.VideoDeviceController.Focus.TrySetAuto(CameraHelper.bFocus);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void cbAutoExposure(object sender, RoutedEventArgs e)
        {
            CameraHelper.bExposureAuto = !CameraHelper.bExposureAuto;
            AutoExposure.IsChecked = CameraHelper.bExposureAuto;


            try
            {
                if (CameraHelper.MainCamera.VideoDeviceController.Exposure != null && CameraHelper.MainCamera.VideoDeviceController.Exposure.Capabilities.Supported == true)
                {
                    CameraHelper.MainCamera.VideoDeviceController.Exposure.TrySetAuto(CameraHelper.bExposureAuto);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void cbBacklightCompensation(object sender, RoutedEventArgs e)
        {
            CameraHelper.bBacklightCompensation = !CameraHelper.bBacklightCompensation;
            AutoBacklightCompensation.IsChecked = CameraHelper.bBacklightCompensation;

            try
            {
                if (CameraHelper.MainCamera.VideoDeviceController.BacklightCompensation != null && CameraHelper.MainCamera.VideoDeviceController.BacklightCompensation.Capabilities.Supported == true)
                {
                    CameraHelper.MainCamera.VideoDeviceController.BacklightCompensation.TrySetAuto(CameraHelper.bBacklightCompensation);
                }
            }
            catch (Exception ex)
            {
                ;
            }
        }

        private void cbWhiteBalance(object sender, RoutedEventArgs e)
        {
            CameraHelper.bWhiteBalance = !CameraHelper.bWhiteBalance;
            AutoWhiteBalance.IsChecked = CameraHelper.bWhiteBalance;

            try
            {
                if (CameraHelper.MainCamera.VideoDeviceController.WhiteBalance != null && CameraHelper.MainCamera.VideoDeviceController.WhiteBalance.Capabilities.Supported == true)
                {
                    CameraHelper.MainCamera.VideoDeviceController.WhiteBalance.TrySetAuto(CameraHelper.bWhiteBalance);
                }
            }
            catch (Exception ex)
            {
                ;
            }
        }

        #endregion

        #region 方法

        #endregion

        private async void btn_Click(object sender, RoutedEventArgs e)
        {
             CameraHelper.InitCamera();
             await Task.Delay(2000);
             CameraHelper.InitCamera();
        }
    }
}