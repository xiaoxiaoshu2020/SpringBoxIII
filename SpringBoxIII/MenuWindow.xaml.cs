﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpringBoxIII
{
    /// <summary>
    /// MenuWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MenuWindow : Window
    {
        public event Action<string>? MessageSent;

        public MenuWindow()
        {
            InitializeComponent();

        }

        private void BtnFeed_Click(object sender, RoutedEventArgs e)
        {
            MessageSent?.Invoke("Feed");
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnClearCheese_Click(object sender, RoutedEventArgs e)
        {
            MessageSent?.Invoke("ClearAllCheese");
        }
    }
}
