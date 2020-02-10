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
    public partial class CountdownOptions : PhoneApplicationPage
    {
        public CountdownOptions()
        {

            InitializeComponent();

            GetLockScreenSetting();
            GetCountdownAlarmSetting();
            GetCountdownDefaultTime();
        }

        #region "Events"

        //OFF Lock screen is not disabled
        private void toggleLockScreen_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
                IS.SaveSetting("Stopwatch-LockScreen", "Enabled");
                toggleLockScreen.Content = AppResources.Enabled;

            }
            catch (Exception)
            {
            }
        }

        //ON Lock screen is disabled
        private void toggleLockScreen_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                IS.SaveSetting("Stopwatch-LockScreen", "Disabled");
                toggleLockScreen.Content = AppResources.Disabled;
            }
            catch (Exception)
            {
            }
        }

        private void togglePlayAlarm_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                IS.SaveSetting("Countdown-Alarm", "Enabled");
                togglePlayAlarm.Content = AppResources.Enabled;
                App.gAlarmSetting = "Enabled";
            }
            catch (Exception)
            {
            }
        }

        private void togglePlayAlarm_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                IS.SaveSetting("Countdown-Alarm", "Disabled");
                togglePlayAlarm.Content = AppResources.Disabled;
                App.gAlarmSetting = "Disabled";
            }
            catch (Exception)
            {
            }
        }

        private void defaultCountdownTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            try
            {
                IS.SaveSetting("Countdown-DefaultTime", ctlDefaultCountdownTime.Value.ToString());
                App.gDefaultCountdown = TimeSpan.Parse(ctlDefaultCountdownTime.Value.ToString());
            }
            catch (Exception)
            {
            }
        }

        #endregion "Events"

        #region "Methods"

        private void GetLockScreenSetting()
        {
            string lockScreenValue = string.Empty;

            try
            {
                if (IS.GetSettingStringValue("Stopwatch-LockScreen") == string.Empty)
                {
                    toggleLockScreen.IsChecked = false;
                    toggleLockScreen.Content = AppResources.Disabled;
                }
                else
                {
                    lockScreenValue = IS.GetSettingStringValue("Stopwatch-LockScreen");
                    if (lockScreenValue == "Enabled")
                    {
                        toggleLockScreen.IsChecked = true;
                        toggleLockScreen.Content = AppResources.Enabled;
                    }
                    else
                    {
                        toggleLockScreen.IsChecked = false;
                        toggleLockScreen.Content = AppResources.Disabled;
                    }
                }

            }
            catch (Exception)
            {
            }
        }

        private void GetCountdownAlarmSetting()
        {
            string countdownAlarmValue = string.Empty;

            try
            {
                if (IS.GetSettingStringValue("Countdown-Alarm") == string.Empty)
                {
                    togglePlayAlarm.IsChecked = false;
                    togglePlayAlarm.Content = AppResources.Disabled;
                }
                else
                {
                    countdownAlarmValue = IS.GetSettingStringValue("Countdown-Alarm");
                    if (countdownAlarmValue == "Enabled")
                    {
                        togglePlayAlarm.IsChecked = true;
                        togglePlayAlarm.Content = AppResources.Enabled;
                    }
                    else
                    {
                        togglePlayAlarm.IsChecked = false;
                        togglePlayAlarm.Content = AppResources.Disabled;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void GetCountdownDefaultTime()
        {
            string countdownAlarmValue = string.Empty;

            try
            {
                if (IS.GetSettingStringValue("Countdown-DefaultTime") == string.Empty)
                {
                    ctlDefaultCountdownTime.Value = new TimeSpan(0, 1, 0);
                }
                else
                {
                    countdownAlarmValue = IS.GetSettingStringValue("Countdown-DefaultTime");
                    ctlDefaultCountdownTime.Value = TimeSpan.Parse(countdownAlarmValue);
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion "Methods"

    }
}