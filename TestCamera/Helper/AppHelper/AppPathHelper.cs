using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace TestCamera
{
    /// <summary>
    /// 关于文件地址的相关定义
    /// </summary>
    public static class AppPathHelper
    {
        /// <summary>
        /// 应用Roaming文件夹地址
        /// </summary>
        public static string AppRoamingFolderPath = "";
        /// <summary>
        /// 应用Roaming文件夹
        /// </summary>
        public static StorageFolder AppRoamingFolder = null;
        /// <summary>
        /// 本地文件夹
        /// </summary>
        public static StorageFolder AppLocatFolder = null;

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            AppRoamingFolderPath = ApplicationData.Current.RoamingFolder.Path + "\\";
            AppRoamingFolder = ApplicationData.Current.RoamingFolder;
            AppLocatFolder = ApplicationData.Current.LocalFolder;
        }

        #region 文件

        /// <summary>
        /// 获得Temp图片文件
        /// </summary>
        public static async Task<StorageFile> GetTempPhotoFile(int index = -1)
        {
            try
            {
                var name = string.Empty;
                if (index == -10)
                { name = AppDefaultHelper.TEMP_FIRST_PHOTO_FILE_NAME; }
                else
                { name = AppDefaultHelper.TEMP_PHOTO_FILE_NAME; }
                if (index > 0)
                { name = string.Format(name, index.ToString()); }
                else
                { name = string.Format(name, ""); }
                return await AppRoamingFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion
    }
}