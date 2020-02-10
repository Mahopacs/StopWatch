using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using StopWatch.Resources;
using Common.IsolatedStoreage;

namespace StopWatch
{
    public partial class Options
    {
 

        public Options()
        {
           string lockScreenValue = string.Empty;

            InitializeComponent();

            if (IS.GetSettingStringValue("Stopwatch-LockScreen") == string.Empty)
            {
                toggleswitch.IsChecked = false;
                toggleswitch.Content = AppResources.Disabled;
            }
            else
            {
                lockScreenValue = IS.GetSettingStringValue("Stopwatch-LockScreen");
                if (lockScreenValue == "Enabled")
                {
                    toggleswitch.IsChecked = true;
                    toggleswitch.Content = AppResources.Enabled;
                }
                else
                {
                    toggleswitch.IsChecked = false;
                    toggleswitch.Content = AppResources.Disabled;
                }
            }
        }

        //OFF Lock screen is not disabled
        private void toggleswitch_Checked(object sender, RoutedEventArgs e)
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            IS.SaveSetting("Stopwatch-LockScreen", "Enabled");
            toggleswitch.Content = AppResources.Enabled;
        }

        //ON Lock screen is disabled
        private void toggleswitch_Unchecked(object sender, RoutedEventArgs e)
        {
            PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            IS.SaveSetting("Stopwatch-LockScreen", "Disabled");
            toggleswitch.Content = AppResources.Disabled;
        }
    }
}