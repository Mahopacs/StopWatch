using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Threading;
using System.ComponentModel;
using StopWatch.Resources;
using Common.IsolatedStoreage;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Phone.Scheduler;
using System.Threading;
using System.Threading.Tasks;

namespace StopWatch
{
    public partial class CountdownUserControl : INotifyPropertyChanged
    {

        DispatcherTimer dispatcherTimer;
        public event PropertyChangedEventHandler PropertyChanged;
        string isRunning = "No";
        TimeSpan _lastSplitTime = new TimeSpan(0, 0, 0);
        string lastCountdownValue = string.Empty;
        public static Alarm alarm;
        string originalCountdownTime = string.Empty;

        public CountdownUserControl()
        {
            InitializeComponent();

            isRunning = IsCountdownRunning();
            CountdownTimesCollection = new ObservableCollection<StopwatchTimes>();

            LoadLapAndSplitData();

            App.gDefaultCountdown = GetCountdownDefaultTime();
            App.gAlarmSetting = GetCountdownAlarmSetting();

            lastCountdownValue = GetLastCountdownValue();

            if (lastCountdownValue == string.Empty || lastCountdownValue == "00:00:00")
            {
                ClockValue = App.gDefaultCountdown;
            }
            else
            {
                ClockValue = TimeSpan.Parse(lastCountdownValue);
            }

            ClockValueString = ClockValue.ToString(@"hh\:mm\:ss");

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            if (isRunning.ToUpper() == "YES")
            {
                Mode = AppResources.StartText;
                Start.Background = new SolidColorBrush(Colors.Green);
                StartCountdown();
            }
            else
            {
                if (lastCountdownValue == string.Empty || lastCountdownValue == "00:00:00" || lastCountdownValue == App.gDefaultCountdown.ToString())
                {
                    Mode = AppResources.StartText;
                    Start.Background = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    Mode = AppResources.ResumeText;
                    Start.Background = new SolidColorBrush(Colors.Green);
                }

            }
            this.DataContext = this;
        }

        #region "Properties"

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public TimeSpan TimeSpan1Min { get { return TimeSpan.FromMinutes(1); } }

        private ObservableCollection<StopwatchTimes> _countdownTimesCollection;
        public ObservableCollection<StopwatchTimes> CountdownTimesCollection
        {
            get { return _countdownTimesCollection; }
            set
            {
                _countdownTimesCollection = value;
                NotifyPropertyChanged("CountdownTimesCollection");
            }
        }

        private TimeSpan _clockValue;
        public TimeSpan ClockValue
        {
            get { return _clockValue; }
            set
            {
                _clockValue = value;
                NotifyPropertyChanged("ClockValue");
            }
        }

        private String _clockValueString;
        public String ClockValueString
        {
            get { return _clockValueString; }
            set
            {
                _clockValueString = value;
                NotifyPropertyChanged("ClockValueString");
            }
        }

        private string _mode;
        public string Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                NotifyPropertyChanged("Mode");

                if (_mode == AppResources.StartText)
                {
                    this.Start.IsEnabled = true;
                    this.Reset.IsEnabled = false;
                    this.Record.IsEnabled = false;
                    IS.SaveSetting("Countdown-IsRunning", "No");
                }
                else if (_mode == AppResources.PauseText)
                {
                    this.Start.IsEnabled = true;
                    this.Reset.IsEnabled = true;
                    this.Record.IsEnabled = true;
                    IS.SaveSetting("Countdown-IsRunning", "Yes");
                }
                else if (_mode == AppResources.ResumeText)
                {
                    this.Start.IsEnabled = true;
                    this.Reset.IsEnabled = true;
                    this.Record.IsEnabled = false;
                    IS.SaveSetting("Countdown-IsRunning", "No");
                };
            }
        }

        private string _sound;
        public string Sound
        {
            get { return _sound; }
            set { _sound = value; NotifyPropertyChanged("Sound"); }
        }

        #endregion "Properties"

        #region "Events"
        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Task.Factory.StartNew(TimerClickAsync);
        }

        private void TimerClickAsync()
        {
            try
            {
                Dispatcher.BeginInvoke(() =>
                {
                    ClockValue = ClockValue - new TimeSpan(0, 0, 1);
                    ClockValueString = ClockValue.ToString(@"hh\:mm\:ss");

                    if (ClockValue <= new TimeSpan(0, 0, 5) && ClockValue > new TimeSpan(0, 0, 0))
                    {
                        btnCountdownDisplay.Foreground = new SolidColorBrush(Colors.Red);
                        PlaySound("Assets/alarm.wav");
                    }
                });

                if (ClockValue <= new TimeSpan(0, 0, 0))
                {
                    Dispatcher.BeginInvoke(() =>
                        {
                            PlaySound("Assets/beep.wav");

                            MessageBoxResult result = MessageBox.Show(AppResources.ElapsedTime + " " + originalCountdownTime, AppResources.CountdownFinished, MessageBoxButton.OK);
                            if (result == MessageBoxResult.OK)
                            {
                                ResetCountdown(true);
                            }
                        });
                }

                IS.SaveSetting("Countdown-LastValue", ClockValue.ToString());

            }
            catch (Exception)
            {
            }
        }

        private void Countdown_Start_Click(object sender, EventArgs e)
        {
            StartCountdown();
        }

        private void Countdown_Reset_Click(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            MessageBoxResult result = MessageBox.Show(AppResources.ResetMessage, AppResources.ResetText, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                ResetCountdown(true);
                _lastSplitTime = new TimeSpan();
                CountdownTimesCollection.Clear();
                IS.RemoveSetting("Countdown-Laps");
                IS.RemoveSetting("Countdown-Splits");
            }
            else
            {
                dispatcherTimer.Stop();
                Mode = AppResources.ResumeText;
                Start.Background = new SolidColorBrush(Colors.Green);
            }
        }

        private void Countdown_Record_Click(object sender, EventArgs e)
        {
            string saveLaps = string.Empty;
            string saveSplits = string.Empty;
            StopwatchTimes stopwatchTimes = new StopwatchTimes();

            stopwatchTimes.ItemCount = CountdownTimesCollection.Count + 1;
            stopwatchTimes.SplitTime = ClockValueString;
            stopwatchTimes.LapTime = (ClockValue - _lastSplitTime).ToString(@"hh\:mm\:ss\.ff");
            CountdownTimesCollection.Insert(0, stopwatchTimes);
            _lastSplitTime = ClockValue;

            saveSplits = GetSplitData(",");
            IS.SaveSetting("Countdown-Splits", saveSplits);

            saveLaps = GetLapData(",");
            IS.SaveSetting("Countdown-Laps", saveLaps);
        }

        private void Display_TimeSpanPickerControl(object sender, RoutedEventArgs e)
        {
            isRunning = IsCountdownRunning();

            if (isRunning.ToUpper() == "NO")
            {
                //TJY I do not want to set the timespanpicker value here because if I do that triggers the value changed event below and resetscountdown,
                //which is fine if the user picks a time and clicks ok/check, BUT if they cancel then I do not/did not want to resetcountdown, and just wanted what was there still
                //up so by not setting the value here, I will only be resetting if they click ok/cancel.
                // ctlCountdownTime.Value = ClockValue;
                ctlCountdownTime.DialogTitle = "Choose Duration";

                ctlCountdownTime.OpenPicker();
            }
        }

        private void countdownTime_ValueChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            ClockValue = TimeSpan.Parse(ctlCountdownTime.Value.ToString());
            ClockValueString = ClockValue.ToString(@"hh\:mm\:ss");
            ResetCountdown(false);
        }

        #endregion "Events"

        #region "Methods"

        public string IsCountdownRunning()
        {
            string returnValue = string.Empty;

            try
            {
                if (IS.GetSettingStringValue("Countdown-IsRunning") != string.Empty)
                {
                    returnValue = IS.GetSettingStringValue("Countdown-IsRunning");
                }
                else
                {
                    returnValue = "No";
                }
            }
            catch (Exception)
            {
                return "No";
            }
            return returnValue;
        }

        private void ResetCountdown(bool setClockValueToDefault)
        {
            SolidColorBrush mySolidColorBrush = new SolidColorBrush();

            dispatcherTimer.Stop();

            if (setClockValueToDefault == true)
            {
                ClockValue = App.gDefaultCountdown;
            }


            // Describes the brush's color using RGB values.  Each value has a range of 0-255.
            mySolidColorBrush.Color = System.Windows.Media.Color.FromArgb(255, 50, 205, 50);

            ClockValueString = ClockValue.ToString(@"hh\:mm\:ss");
            Mode = AppResources.StartText;
            Start.Background = new SolidColorBrush(Colors.Green);
            btnCountdownDisplay.Foreground = new SolidColorBrush(mySolidColorBrush.Color);
        }

        public void StartCountdown()
        {
            if (Mode == AppResources.StartText)
            {
                dispatcherTimer.Start();
                Mode = AppResources.PauseText;
                Start.Background = new SolidColorBrush(Colors.Red);
                originalCountdownTime = ClockValueString;
            }
            else if (Mode == AppResources.PauseText)
            {
                dispatcherTimer.Stop();
                Mode = AppResources.ResumeText;
                Start.Background = new SolidColorBrush(Colors.Green);
            }
            else if (Mode == AppResources.ResumeText)
            {
                dispatcherTimer.Start();
                Mode = AppResources.PauseText;
                Start.Background = new SolidColorBrush(Colors.Red);
            };
        }

        private string GetLastCountdownValue()
        {
            string returnValue = string.Empty;

            if (IS.GetSettingStringValue("Countdown-LastValue") == string.Empty)
            {
                returnValue = string.Empty;
            }
            else
            {
                returnValue = IS.GetSettingStringValue("Countdown-LastValue");
            }

            return returnValue;
        }

        public String GetSplitData(String delimiter)
        {
            String returnString = String.Empty;

            try
            {
                foreach (var item in CountdownTimesCollection)
                {
                    returnString = returnString + item.SplitTime + delimiter;
                }
            }
            catch (Exception)
            {

                return returnString;
            }
            return returnString;
        }

        public String GetLapData(String delimiter)
        {
            String returnString = String.Empty;

            try
            {
                foreach (var item in CountdownTimesCollection)
                {
                    returnString = returnString + item.LapTime + delimiter;
                }
            }
            catch (Exception)
            {

                return returnString;
            }
            return returnString;
        }

        public void LoadLapAndSplitData()
        {
            string[] laps;
            string[] splits;

            laps = IS.GetSettingStringValue("Countdown-Laps").Split(',');
            splits = IS.GetSettingStringValue("Countdown-Splits").Split(',');

            for (int i = laps.Count() - 1; i >= 0; i--)
            {
                if (laps[i] != string.Empty)
                {
                    StopwatchTimes stopwatchTimes = new StopwatchTimes();
                    stopwatchTimes.ItemCount = laps.Count() - i - 1;
                    stopwatchTimes.LapTime = laps[i];
                    stopwatchTimes.SplitTime = splits[i];
                    CountdownTimesCollection.Insert(0, stopwatchTimes);

                    _lastSplitTime = TimeSpan.Parse(stopwatchTimes.SplitTime);
                }
            }
        }

        private TimeSpan GetCountdownDefaultTime()
        {
            TimeSpan returnValue;

            if (IS.GetSettingStringValue("Countdown-DefaultTime") == string.Empty)
            {
                returnValue = new TimeSpan(0, 1, 0);
            }
            else
            {
                returnValue = TimeSpan.Parse(IS.GetSettingStringValue("Countdown-DefaultTime"));
            }

            return returnValue;
        }

        private string GetCountdownAlarmSetting()
        {
            string returnValue = string.Empty;

            if (IS.GetSettingStringValue("Countdown-Alarm") == string.Empty)
            {
                returnValue = "Enabled";
                IS.SaveSetting("Countdown-Alarm", "Enabled");
            }
            else
            {
                returnValue = IS.GetSettingStringValue("Countdown-Alarm");
            }

            return returnValue;
        }

        //media element allows for pausing sound, Soundeffect does not allow for pause/stop so NOT good for background music
        //Note no slash before the Assets folder, and it's a WAV file!
        private void PlaySound(string soundFile)
        {
            try
            {
                if (App.gAlarmSetting.ToUpper() == "ENABLED")
                {
                    Stream stream = TitleContainer.OpenStream(soundFile);
                    SoundEffect effect = SoundEffect.FromStream(stream);
                    FrameworkDispatcher.Update();
                    effect.Play();
                }
            }
            catch (Exception ex)
            {
            }
        }

        //media element allows for pausing sound, Soundeffect does not allow for pause/stop so NOT good for background music
        private void PlaySound2(string soundFile)
        {
            try
            {
                Sound = soundFile;

                myBoundSound.Play();

                //if (myBoundSound.CurrentState == System.Windows.Media.MediaElementState.Playing)
                //    myBoundSound.Pause();
                //else
                //    myBoundSound.Play();
            }
            catch (Exception ex)
            {
            }
        }

        #endregion "Methods"

    }
}
