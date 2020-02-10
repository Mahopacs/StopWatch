using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using StopWatch.Resources;
using System.Collections.ObjectModel;
using Common.IsolatedStoreage;
using Common.Utilities;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;
using Microsoft.Phone.Tasks;

namespace StopWatch
{
    public partial class StopWatchUserControl : INotifyPropertyChanged
    {
        DispatcherTimer _timer;
        DateTime _dateTimeLastStart;
        TimeSpan _adjustment = new TimeSpan(0, 0, 0);
        TimeSpan _lastSplitTime = new TimeSpan(0, 0, 0);
        string _isRunning = "No";

        public event PropertyChangedEventHandler PropertyChanged;

        public StopWatchUserControl()
        {
            InitializeComponent();
          
            App.gStopWatch = new Stopwatch();
            _isRunning = IsStopWatchRunning();
            StopwatchTimesCollection = new ObservableCollection<StopwatchTimes>();

            LoadLapAndSplitData();

            //Need to determine what/if any adjustment should be made to clock
            //For example, if clock was paused previously we want to start at last clock value with clock paused
            //For example, if clock was running previously we want to add time that has accrued since app was shot down
            if ((IS.GetSettingStringValue("Stopwatch-DateTimeLastStart") == string.Empty) || (_isRunning.ToUpper() == "NO"))
            {
                if (IS.GetSettingStringValue("Stopwatch-DateTimeLastStart") == string.Empty)
                {
                    _adjustment = new TimeSpan();
                }
                else
                {
                    if (IS.GetSettingStringValue("Stopwatch-LastValue") == string.Empty)
                    {
                        _adjustment = new TimeSpan();
                    }
                    else
                    {
                        _adjustment = TimeSpan.Parse(IS.GetSettingStringValue("Stopwatch-LastValue"));
                    }
                }
            }
            else  //clock was running when it was last opened, so add accrued time
            {
                _dateTimeLastStart = DateTime.Parse(IS.GetSettingStringValue("Stopwatch-DateTimeLastStart"));
                _adjustment = TimeSpan.Parse(IS.GetSettingStringValue("Stopwatch-LastValue")) + (DateTime.Now - _dateTimeLastStart);
            }

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(Timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 0);
            _timer.Start();

            if (_isRunning.ToUpper() == "YES")
            {
                Mode = AppResources.StartText;
                Start.Background = new SolidColorBrush(Colors.Green);
                StartTimer();
            }
            else
            {
                if (_adjustment == new TimeSpan(0, 0, 0))
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

        private ObservableCollection<StopwatchTimes> _stopwatchTimesCollection;
        public ObservableCollection<StopwatchTimes> StopwatchTimesCollection
        {
            get { return _stopwatchTimesCollection; }
            set
            {
                _stopwatchTimesCollection = value;
                NotifyPropertyChanged("StopwatchTimesCollection");
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
                    this.Lap.IsEnabled = false;
                    IS.SaveSetting("Stopwatch-IsRunning", "No");
                }
                else if (_mode == AppResources.PauseText)
                {
                    this.Start.IsEnabled = true;
                    this.Reset.IsEnabled = true;
                    this.Lap.IsEnabled = true;
                    IS.SaveSetting("Stopwatch-IsRunning", "Yes");
                }
                else if (_mode == AppResources.ResumeText)
                {
                    this.Start.IsEnabled = true;
                    this.Reset.IsEnabled = true;
                    this.Lap.IsEnabled = false;
                    IS.SaveSetting("Stopwatch-IsRunning", "No");
                }
                else if (_mode == AppResources.ExceedText)
                {
                    App.gStopWatch.Stop();
                    ClockValue = new TimeSpan(0, 0, 0);
                    this.Start.IsEnabled = false;
                    this.Reset.IsEnabled = true;
                    this.Lap.IsEnabled = false;
                    IS.SaveSetting("Stopwatch-IsRunning", "No");
                };
            }
        }

        #endregion "Properties"

        #region "Events"

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (ClockValue >= new TimeSpan(9, 0, 0))
            {
                if (Mode != AppResources.ExceedText)
                {
                    MessageBox.Show(AppResources.MaxTimeExceededMessage);
                    Mode = AppResources.ExceedText;
                }
            }
            else if (Mode != AppResources.ExceedText)
            {
                ClockValue = App.gStopWatch.Elapsed + _adjustment;
                ClockValueString = ClockValue.ToString(@"hh\:mm\:ss\.ff");
                IS.SaveSetting("Stopwatch-LastValue", ClockValue.ToString());
            }
        }

        private void StopWatch_Start_Click(object sender, EventArgs e)
        {
            StartTimer();
        }

        private void StopWatch_Reset_Click(object sender, EventArgs e)
        {
            App.gStopWatch.Stop();
            MessageBoxResult result = MessageBox.Show(AppResources.ResetMessage, AppResources.ResetText, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {

                App.gStopWatch.Reset();
                _adjustment = new TimeSpan();
                _lastSplitTime = new TimeSpan();
                StopwatchTimesCollection.Clear();
                IS.RemoveSetting("Stopwatch-Laps");
                IS.RemoveSetting("Stopwatch-Splits");
                Mode = AppResources.StartText;
                Start.Background = new SolidColorBrush(Colors.Green);
            }
            else
            {
                App.gStopWatch.Stop();
                Mode = AppResources.ResumeText;
                Start.Background = new SolidColorBrush(Colors.Green);
            }
        }

        private void StopWatch_Lap_Click(object sender, EventArgs e)
        {
            string saveLaps = string.Empty;
            string saveSplits = string.Empty;
            StopwatchTimes stopwatchTimes = new StopwatchTimes();

            stopwatchTimes.ItemCount = StopwatchTimesCollection.Count + 1;
            stopwatchTimes.SplitTime = ClockValueString;
            stopwatchTimes.LapTime = (ClockValue - _lastSplitTime).ToString(@"hh\:mm\:ss\.ff");
            StopwatchTimesCollection.Insert(0, stopwatchTimes);
            _lastSplitTime = ClockValue;

            saveSplits = GetSplitData(",");
            IS.SaveSetting("Stopwatch-Splits", saveSplits);

            saveLaps = GetLapData(",");
            IS.SaveSetting("Stopwatch-Laps", saveLaps);
        }

        #endregion "Events"

        #region "Methods"

        public void StartTimer()
        {
            if (Mode == AppResources.StartText)
            {
                App.gStopWatch.Start();
                Mode = AppResources.PauseText;
                Start.Background = new SolidColorBrush(Colors.Red);
            }
            else if (Mode == AppResources.PauseText)
            {
                App.gStopWatch.Stop();
                Mode = AppResources.ResumeText;
                Start.Background = new SolidColorBrush(Colors.Green);
            }
            else if (Mode == AppResources.ResumeText)
            {
                App.gStopWatch.Start();
                Mode = AppResources.PauseText;
                Start.Background = new SolidColorBrush(Colors.Red);
            };

            ClockValue = App.gStopWatch.Elapsed;
        }

        public String GetLapData(String delimiter)
        {
            String returnString = String.Empty;

            try
            {
                foreach (var item in StopwatchTimesCollection)
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

        public String GetSplitData(String delimiter)
        {
            String returnString = String.Empty;

            try
            {
                foreach (var item in StopwatchTimesCollection)
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

        public string IsStopWatchRunning()
        {
            string returnValue = string.Empty;

            try
            {
                if (IS.GetSettingStringValue("Stopwatch-IsRunning") != string.Empty)
                {
                    returnValue = IS.GetSettingStringValue("Stopwatch-IsRunning");
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

        public void LoadLapAndSplitData()
        {
            string[] laps;
            string[] splits;

            laps = IS.GetSettingStringValue("Stopwatch-Laps").Split(',');
            splits = IS.GetSettingStringValue("Stopwatch-Splits").Split(',');

            for (int i = laps.Count() - 1; i >= 0; i--)
            {
                if (laps[i] != string.Empty)
                {
                    StopwatchTimes stopwatchTimes = new StopwatchTimes();
                    stopwatchTimes.ItemCount = laps.Count() - i - 1;
                    stopwatchTimes.LapTime = laps[i];
                    stopwatchTimes.SplitTime = splits[i];
                    StopwatchTimesCollection.Insert(0, stopwatchTimes);

                    _lastSplitTime = TimeSpan.Parse(stopwatchTimes.SplitTime);
                }
            }
        }

        #endregion "Methods"

       

        #region "Common Routines"

        

        #endregion "Common Routines"
    }
}
