using System;
namespace TestCamera.Helper.CodeHelper
 
{
    public static class CodeExpandHelper
    {
        /// <summary>
        /// (BuildGuid)生成标识
        /// </summary>
        public static string BG(this string item) { return Guid.NewGuid().ToString(item); }
    }
}
