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
                MessageBox.Show("配置文件:\"" + iniFilePath + "\"不存在,已自动创建");
                File.WriteAllText(iniFilePath, "[Path]\n" +
                    "Path=\n" +
                    "[ConstValue]\n" +
                    "MaxRatCount=");
            }
            var builder = new ConfigurationBuilder()
                .SetBasePath(System.AppDomain.CurrentDomain.BaseDirectory)
                .AddIniFile(iniFilePath, optional: true, reloadOnChange: true);
            var config = builder.Build();
            // 读取配置文件
            if (!string.IsNullOrEmpty(config["Path:Path"]))
            {
                AudioPath[0] = "./Audio/" + config["Path:Path"] + "0" + ".wav";
                AudioPath[1] = "./Audio/" + config["Path:Path"] + "1" + ".wav";
                ImgPath[0] = "./Image/" + config["Path:Path"] + "0" + ".png";
                ImgPath[1] = "./Image/" + config["Path:Path"] + "1" + ".png";
            }
            if (!string.IsNullOrEmpty(config["ConstValue:MaxRatCount"]))
                MaxRatCount = int.Parse(config["ConstValue:MaxRatCount"] + "");
        }

        private const string iniFilePath = "./config.ini";

        public static string[] AudioPath = ["./Audio/Rat0.wav", "./Audio/Rat1.wav"];
        public static string[] ImgPath = ["./Image/Rat0.png", "./Image/Rat1.png"];
        public static int MaxRatCount = 10;
    }
}
