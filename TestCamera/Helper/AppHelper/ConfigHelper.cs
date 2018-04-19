using System;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace TestCamera
{
    /// <summary>
    /// 配置辅助者
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// 配置信息
        /// </summary>
        public static ConfigInfo Info;
        /// <summary>
        /// 加载配置
        /// </summary>
        public static void Init()
        {
            //读取文件
            var configFileTask = AppPathHelper.AppLocatFolder.CreateFileAsync(AppDefaultHelper.CONFIG_FILE_NAME, CreationCollisionOption.OpenIfExists).AsTask();
            configFileTask.Wait();
            var configFile = configFileTask.Result;
            var isError = false;
            var xmlStr = string.Empty;
            Exception exItem = null;
            try
            {
                //序列化XML内容
                var xmlStrTask = FileIO.ReadTextAsync(configFile).AsTask();
                xmlStrTask.Wait();
                xmlStr = xmlStrTask.Result;
                if (string.IsNullOrEmpty(xmlStr) == false)
                {
                    using (StringReader sr = new StringReader(xmlStr))
                    {
                        XmlSerializer xmldes = new XmlSerializer(typeof(ConfigInfo));
                        Info = xmldes.Deserialize(sr) as ConfigInfo;
                    }
                }
                else { isError = true; }
            }
            catch (Exception ex)
            {
                exItem = ex;
            }
            if (exItem != null || isError == true)
            {
                Info = new ConfigInfo();
                Save();
                if (exItem != null)
                {
                }
                if (string.IsNullOrEmpty(xmlStr) == false)
                {
                }
            }
        }
        /// <summary>
        /// 保存配置
        /// </summary>
        public static void Save()
        {
            if (Info == null) { return; }
            using (MemoryStream Stream = new MemoryStream())
            {
                XmlSerializer xml = new XmlSerializer(typeof(ConfigInfo));
                try
                {
                    //序列化对象
                    xml.Serialize(Stream, Info);
                }
                catch { }
                Stream.Position = 0;
                using (StreamReader sr = new StreamReader(Stream))
                {
                    var configFileTask = AppPathHelper.AppLocatFolder.CreateFileAsync(AppDefaultHelper.CONFIG_FILE_NAME, CreationCollisionOption.OpenIfExists).AsTask();
                    configFileTask.Wait();
                    var configFile = configFileTask.Result;
                    try
                    {
                        var writeTask = FileIO.WriteTextAsync(configFile, sr.ReadToEnd()).AsTask();
                        writeTask.Wait();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}