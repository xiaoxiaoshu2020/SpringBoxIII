using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Windows;

namespace SpringBoxIII
{
    class Static
    {
        public static void ReadConfig()
        {
            if (!File.Exists(iniFilePath))
            {
                MessageBox.Show("配置文件:\""+iniFilePath+"\"不存在,已自动创建");
                File.WriteAllText(iniFilePath, "[Path]\n" +
                    "AudioPath0=\n" +
                    "AudioPath1=\n" +
                    "ImgPath0=\n" +
                    "ImgPath1=\n" +
                    "[ConstValue]\n" +
                    "MaxRatCount=");
            }
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddIniFile(iniFilePath, optional: true, reloadOnChange: true);
            var config = builder.Build();
            // 读取配置文件
            if (!string.IsNullOrEmpty(config["Path:AudioPath0"]))
                AudioPath[0] = config["Path:AudioPath0"] + "";
            if (!string.IsNullOrEmpty(config["Path:AudioPath1"]))
                AudioPath[1] = config["Path:AudioPath1"] + "";
            if (!string.IsNullOrEmpty(config["Path:ImgPath0"]))
                ImgPath[0] = config["Path:ImgPath0"] + "";
            if (!string.IsNullOrEmpty(config["Path:ImgPath1"]))
                ImgPath[1] = config["Path:ImgPath1"] + "";
            if (!string.IsNullOrEmpty(config["ConstValue:MaxRatCount"]))
                MaxRatCount = int.Parse(config["ConstValue:MaxRatCount"] + "");
        }
        private const string iniFilePath = "./config.ini";

        public static string[]  AudioPath = ["./Rat.wav","./Rat1.wav"];
        public static string[] ImgPath = ["./Rat.png", "./Rat1.png"];
        public static int MaxRatCount = 10;
    }
}
