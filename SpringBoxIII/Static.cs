using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace SpringBoxIII
{
    class Static
    {
        public static void ReadConfig()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddIniFile(iniFilePath, optional: true, reloadOnChange: true);
            var config = builder.Build();
            // 读取配置文件
            if (config["Path:AudioPath"] != null)
                AudioPath = config["Path:AudioPath"] + "";
            if (config["Path:ImgPath0"] != null)
                ImgPath[0] = config["Path:ImgPath0"] + "";
            if (config["Path:ImgPath1"] != null)
                ImgPath[1] = config["Path:ImgPath1"] + "";
        }
        private const string iniFilePath = "./config.ini";

        public static string AudioPath = "./Rat.wav";
        public static string[] ImgPath = new string[2] { "./Rat.png", "./Rat1.png" };
    }
}
