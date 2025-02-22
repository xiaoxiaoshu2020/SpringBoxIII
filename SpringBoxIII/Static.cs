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
            AudioPath = config["Path:AudioPath"] + "";
        }
        private const string iniFilePath = "./config.ini";

        public static string AudioPath = "";
    }
}
