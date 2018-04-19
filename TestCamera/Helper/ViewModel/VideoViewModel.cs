using System;
using System.Threading.Tasks;
using TestCamera.Helper.CodeHelper;
using Windows.Graphics.Display;

namespace TestCamera.Helper.ViewModel
{
    public class VideoViewModel
    {
        public VideoViewModel()
        {

        }

        /// <summary>
        /// 当前验证方向执行标识
        /// </summary>
        private string NowValidationRotationGuid = "";

        /// <summary>
        /// 原始方向
        /// </summary>
        private DisplayOrientations OldOrientations = DisplayOrientations.None;

        /// <summary>
        /// 改变全景状态的面板
        /// </summary>
        public event Action OnChangeVideoPageState;

        /// <summary>
        /// 验证方向
        /// </summary>
        public void ValidationRotation(DisplayOrientations nowOrientations)
        {
            Task.Run(async () =>
            {
                try
                {
                    var nowGuid = "N".BG();
                    NowValidationRotationGuid = nowGuid;
                    int i = 0;
                    for (i = 0; i < 10; i++)
                    {
                        LogHelper.AddString(nowOrientations.ToString());

                        //if (NowValidationRotationGuid != nowGuid) { break; }
                        await Task.Delay(500);
                        //if (OldOrientations != nowOrientations)
                       // {
                            LogHelper.AddString("ExecuteValidationRotation");
                            SetRotation(nowOrientations);
                         //   break;
                        //}
                    }
                }
                catch (Exception ex)
                {
                   //LogHelper.AddError(ex);
                }
            });
        }


        /// <summary>
        /// 设置方向
        /// </summary>
        public void SetRotation(DisplayOrientations orientations)
        {
            if (CameraHelper.IsLoading)
            {
                LogHelper.AddString("CameraHelper.IsLoading == True, Leave:" + orientations.ToString());
                return;
            }

            OldOrientations = orientations;
            CameraHelper.SetCameraRotation(orientations);
        }



    }


}
