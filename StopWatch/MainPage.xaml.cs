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
using System.ComponentModel;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using Microsoft.Phone.Tasks;
using System.Windows.Media;
using System.Globalization;
using Windows.ApplicationModel.Store;
using Common.IsolatedStoreage;
using Common.Utilities;
using Common.Licencing;

namespace StopWatch
{

    public partial class MainPage : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        bool _trialExpired = false;
        MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
        private const string RATED = "RATED";
        private const string NUMBEROFTIMESOPENED = "NUMBEROFTIMESOPENED";
        private bool _rated;
        private int _numberOfTimesOpened = 0;
        public static MainPage _mainPageInstance;

        // Constructor
        public MainPage()
        {
            _mainPageInstance = this;

            string hasAppBeenRated = string.Empty;

            if (Rate.HasAppBeenRated().ToUpper()=="YES")
            {
                _rated = true;
            }
            else
            {
                _rated = false;
            }

            InitializeComponent();

            //Initially show the msft control, if it fails, show google.
            AdvertisingVisibility = Visibility.Visible;
            GoogleAdVisibility = Visibility.Collapsed;

            MyAdControl.CountryOrRegion = RegionInfo.CurrentRegion.TwoLetterISORegionName;

            BuildLocalizedApplicationBar(_rated);

            SetLockScreenSetting();

            MyAdControl.ErrorOccurred += MyAdControl_ErrorOccurred;
            GoogleAdControl.FailedToReceiveAd += GoogleAdControl_FailedToReceiveAd;

            this.DataContext = this;

            Trial.SaveStartDateOfTrial();
            if (IS.GetSetting(RATED) != null)
            {
                if ((bool)IS.GetSetting(RATED))
                {
                    _rated = true;
                }
            }

            if (IS.GetSetting(NUMBEROFTIMESOPENED) == null)
            {
                IS.SaveSetting(NUMBEROFTIMESOPENED, 0);
            }
            else
            {
                IS.SaveSetting(NUMBEROFTIMESOPENED, (int)IS.GetSetting(NUMBEROFTIMESOPENED) + 1);
                _numberOfTimesOpened = (int)IS.GetSetting(NUMBEROFTIMESOPENED);
            }

            if (!(Application.Current as App).IsFreeVersion)
            {
                PaidAppInitialization();
            }
            else
            {
                FreeAppInitialIzation();
            }
        }

        void GoogleAdControl_FailedToReceiveAd(object sender, GoogleAds.AdErrorEventArgs e)
        {
            
        }

        private void FreeAppInitialIzation()
        {
            if (!_rated && _numberOfTimesOpened >= 2)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.AppMenuItemAddCountdown, AppResources.AppMenuItemAddCountdown, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    IS.SaveSetting(RATED, true);
                    _rated = true;
                    MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                    marketplaceReviewTask.Show();
                }
            }

            if (Rate.HasAppBeenRated().ToUpper() == "YES")
            {
                EnableCountdown();
            }
            else
            {
                DisableCountdown();
            }
        }

        private void PaidAppInitialization()
        {
            //always enable countdown
            EnableCountdown();

            //Hide both ad controls in the paid app.
            AdvertisingVisibility = Visibility.Collapsed;
            GoogleAdVisibility = Visibility.Collapsed;


            if ((Application.Current as App).IsTrial)
            {
                if (Trial.IsTrialExpired())
                {
                    EnableApp(false);
                    MessageBox.Show(AppResources.TrialExpired);
                    _marketPlaceDetailTask.Show();
                }
                else
                {
                    //App has already been rated
                    if (_rated || _numberOfTimesOpened < 2)
                    {
                        MessageBox.Show(AppResources.YouHave + Trial.GetDaysLeftInTrial() + AppResources.DaysLeftInTrial);
                    }
                    //app not rated, rate to add 10 days to trial
                    else if (!_rated && _numberOfTimesOpened >= 2)
                    {
                        MessageBoxResult result = MessageBox.Show(AppResources.Trial1, AppResources.Trial2, MessageBoxButton.OKCancel);

                        if (result == MessageBoxResult.OK)
                        {
                            Trial.Add10DaysToTrial();
                            IS.SaveSetting(RATED, true);
                            _rated = true;
                            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                            marketplaceReviewTask.Show();
                        }
                    }
                }
            }
            else
            {
                if (!_rated)
                {
                    //5th, 10th, 15th time prompt, 20th time ok only to rate, never prompt them again after they rate.
                    Rate.RateTheApp(AppResources.RateTheAppQuestion, AppResources.RateTheAppPrompt, AppResources.RateAppHeader);
                }
            }
        }

        void MyAdControl_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            //When the MSFT Ad Control Fails, Now turn on the Google AD!
            if ((Application.Current as App).IsFreeVersion)
            {
                AdvertisingVisibility = System.Windows.Visibility.Collapsed;
                GoogleAdVisibility = System.Windows.Visibility.Visible;
            }
            Console.WriteLine(e.Error);
        }

        #region "Properties"
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private Visibility _advertisingVisibility;
        public Visibility AdvertisingVisibility
        {
            get { return _advertisingVisibility; }
            set
            {
                _advertisingVisibility = value;
                NotifyPropertyChanged("AdvertisingVisibility");
            }
        }

        private Visibility _googleAdVisibility;
        public Visibility GoogleAdVisibility
        {
            get { return _googleAdVisibility; }
            set { _googleAdVisibility = value; NotifyPropertyChanged("GoogleAdVisibility"); }
        }
        

        private String _pivotName;
        public String PivotName
        {
            get
            {
                _pivotName = (pivotControl.SelectedItem as PivotItem).Header as string;
                return _pivotName.ToUpper();
            }
        }

        #endregion "Properties"

        #region "Events"

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Need to change the appbar and menu item captions depending on wich pivot
            //Options should either be Stopwatch options or Countdown options
            foreach (ApplicationBarMenuItem item in ApplicationBar.MenuItems)
            {
                if (item.Text.Contains("Options"))
                {
                    if (PivotName == "STOPWATCH")
                    {
                        item.Text = AppResources.StopWatchOptions;
                    }
                    else
                    {
                        item.Text = AppResources.CountdownOptions;
                    }
                }
            }

            foreach (ApplicationBarIconButton item in ApplicationBar.Buttons)
            {

            }
        }

        private void Email_Click(object sender, EventArgs e)
        {
            EmailComposeTask emailCompuser;
            string emailBody = string.Empty;

            switch (PivotName)
            {
                case "STOPWATCH":
                    emailBody = BuildEmailBodyStopwatch();
                    break;
                case "COUNTDOWN":
                    emailBody = string.Empty;
                    break;
            }

            emailCompuser = new EmailComposeTask();
            emailCompuser.Subject = AppResources.EmailSubject;
            emailCompuser.Body = emailBody;
            emailCompuser.Show();
        }

        private void DeleteLaps_Click(object sender, EventArgs e)
        {
            switch (PivotName)
            {
                case "STOPWATCH":
                    MessageBoxResult result = MessageBox.Show(AppResources.DeleteAllLapDataMessage, AppResources.DeleteLapsTitle, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.OK)
                    {
                        IS.RemoveSetting("Stopwatch-Laps");
                        IS.RemoveSetting("Stopwatch-Splits");
                        this.stopwatchControl.StopwatchTimesCollection.Clear();
                    }

                    break;
                case "COUNTDOWN":
                    MessageBoxResult result1 = MessageBox.Show(AppResources.DeleteAllRecordDataMessage, AppResources.DeleteRecordsTitle, MessageBoxButton.OKCancel);
                    if (result1 == MessageBoxResult.OK)
                    {
                        IS.RemoveSetting("Countdown-Laps");
                        IS.RemoveSetting("Countdown-Splits");
                        this.countdownControl.CountdownTimesCollection.Clear();
                    }

                    break;
            }
        }

        private void Options_Click(object sender, EventArgs e)
        {
            switch (PivotName)
            {
                case "STOPWATCH":
                    NavigationService.Navigate(new Uri("/StopwatchOptions.xaml", UriKind.Relative));
                    break;
                case "COUNTDOWN":
                    NavigationService.Navigate(new Uri("/CountdownOptions.xaml", UriKind.Relative));
                    break;
            }
        }

        private void About_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
        }

        private void Review_Click(object sender, EventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        private void MoreApps_Click(object sender, EventArgs e)
        {
            MarketplaceSearchTask marketplaceSearchTask = new MarketplaceSearchTask();

            marketplaceSearchTask.SearchTerms = "KLBCreations";
            marketplaceSearchTask.Show();
        }

        private void AddCountdown_Click(object sender, EventArgs e)
        {
            MessageBoxResult msgResult;
            string hasAppBeenRated = string.Empty;

            hasAppBeenRated = Rate.HasAppBeenRated();

            if (hasAppBeenRated.ToUpper() == "YES")
            {
                return;
            }

            msgResult = MessageBox.Show(AppResources.AddCountdownQuestion, AppResources.RateAppHeader, MessageBoxButton.OKCancel);
            if (msgResult == MessageBoxResult.OK)
            {
                MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                marketplaceReviewTask.Show();

                IS.SaveSetting("AppRated", "Yes");
                EnableCountdown();

                foreach (ApplicationBarMenuItem item in ApplicationBar.MenuItems)
                {
                    if (item.Text == AppResources.AppMenuItemAddCountdown)
                    {
                        //Disable it now that it is no longer needed, next time app is started it will not be even loaded as a menu option
                        item.IsEnabled = false;
                    }
                }

            }
            else
            {
                IS.SaveSetting("AppRated", "No");
            }
        }

        #endregion "Events"

        #region "Methods"

        private void SetLockScreenSetting()
        {
            string lockScreenValue = string.Empty;

            if (IS.GetSettingStringValue("Stopwatch-LockScreen") != string.Empty)
            {
                lockScreenValue = IS.GetSettingStringValue("Stopwatch-LockScreen");
                if (lockScreenValue == "Enabled")
                {
                    PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
                }
                else
                {
                    PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                }
            }
            else //if lockscreen setting not set then default to disabling lockscreen
            {
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
                IS.SaveSetting("Stopwatch-LockScreen", "Disabled");
            }
        }

        public string BuildEmailBodyStopwatch()
        {
            string returnString = string.Empty;

            try
            {
                returnString = AppResources.EmailBody + "\n";
                foreach (var item in this.stopwatchControl.StopwatchTimesCollection)
                {
                    returnString = returnString + item.ItemCount + '/' + item.SplitTime + '/' + item.LapTime + "\n";
                }
            }
            catch (Exception)
            {

                return returnString;
            }
            return returnString;
        }

        private void EnableCountdown()
        {
            this.pivotCountdown.IsEnabled = true;
            this.countdownControl.btnCountdownHowTo.Visibility = Visibility.Collapsed;
        }

        private void DisableCountdown()
        {
            this.pivotCountdown.IsEnabled = false;
            this.countdownControl.btnCountdownHowTo.Visibility = Visibility.Visible;
        }

        private void EnableApp(bool enabled)
        {
            this.pivotCountdown.IsEnabled = enabled;
            this.pivotStopwatch.IsEnabled = enabled;
            if (enabled)
            {
                this.countdownControl.btnCountdownHowTo.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.countdownControl.btnCountdownHowTo.Visibility = Visibility.Visible;
            }
        }

        #endregion "Methods"

        #region "Common Routines"

        private void BuildLocalizedApplicationBar(bool hasAppBeenRated)
        {
            // Set the page's ApplicationBar to a new instance of ApplicationBar.
            ApplicationBar = new ApplicationBar();

            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.Opacity = 1.0;
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;

            // Create a new button and set the text value to the localized string from AppResources.
            ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/feature.email.png", UriKind.Relative));
            appBarButton.Text = AppResources.AppBarEmailButton;
            ApplicationBar.Buttons.Add(appBarButton);
            appBarButton.Click += new EventHandler(Email_Click);

            ApplicationBarIconButton appBarButton2 = new ApplicationBarIconButton(new Uri("/Assets/cancel.png", UriKind.Relative));
            appBarButton2.Text = AppResources.AppBarClearLapsButton;
            ApplicationBar.Buttons.Add(appBarButton2);
            appBarButton2.Click += new EventHandler(DeleteLaps_Click);

            // Create a new menu item with the localized string from AppResources.
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppMenuItemOptions);
            ApplicationBar.MenuItems.Add(appBarMenuItem);
            appBarMenuItem.Click += new EventHandler(Options_Click);

            ApplicationBarMenuItem appBarMenuItem2 = new ApplicationBarMenuItem(AppResources.AppMenuItemAbout);
            ApplicationBar.MenuItems.Add(appBarMenuItem2);
            appBarMenuItem2.Click += new EventHandler(About_Click);

            ApplicationBarMenuItem appBarMenuItem4 = new ApplicationBarMenuItem(AppResources.AppMenuItemMoreApps);
            ApplicationBar.MenuItems.Add(appBarMenuItem4);
            appBarMenuItem4.Click += new EventHandler(MoreApps_Click);

            if (!hasAppBeenRated && (Application.Current as App).IsFreeVersion)
            {
                ApplicationBarMenuItem appBarMenuItem5 = new ApplicationBarMenuItem(AppResources.AppMenuItemAddCountdown);
                ApplicationBar.MenuItems.Add(appBarMenuItem5);
                appBarMenuItem5.Click += new EventHandler(AddCountdown_Click);
            }
        }

        private void PhoneOrientationChanged(object sender, OrientationChangedEventArgs e)
        {


            switch (PivotName)
            {
                case "STOPWATCH":
                    // Switch the placement of the buttons based on an orientation change.
                    if ((e.Orientation & PageOrientation.Portrait) == (PageOrientation.Portrait))
                    {
                        Grid.SetRow(MyAdControl, 0);
                        Grid.SetRow(this.stopwatchControl.ContentPanel, 1);
                        Grid.SetRowSpan(this.stopwatchControl.ContentPanel, 2);
                        Grid.SetRow(this.stopwatchControl.ButtonPanel, 3);
                        this.stopwatchControl.LapBorder.Visibility = Visibility.Visible;
                        this.stopwatchControl.LapGrid.Visibility = Visibility.Visible;
                    }
                    // If not in portrait, move buttonList content to visible row and column.
                    else
                    {
                        Grid.SetRow(MyAdControl, 0);
                        Grid.SetRow(this.stopwatchControl.ContentPanel, 1);
                        Grid.SetRowSpan(this.stopwatchControl.ContentPanel, 2);
                        Grid.SetRow(this.stopwatchControl.ButtonPanel, 4);
                        this.stopwatchControl.LapBorder.Visibility = Visibility.Collapsed;
                        this.stopwatchControl.LapGrid.Visibility = Visibility.Collapsed;
                    }
                    break;
                case "COUNTDOWN":
                    // Switch the placement of the buttons based on an orientation change.
                    if ((e.Orientation & PageOrientation.Portrait) == (PageOrientation.Portrait))
                    {
                        Grid.SetRow(MyAdControl, 0);
                        Grid.SetRow(this.countdownControl.ContentPanel, 1);
                        Grid.SetRowSpan(this.countdownControl.ContentPanel, 2);
                        Grid.SetRow(this.countdownControl.ButtonPanel, 3);
                        this.countdownControl.LapBorder.Visibility = Visibility.Visible;
                        this.countdownControl.LapGrid.Visibility = Visibility.Visible;
                    }
                    // If not in portrait, move buttonList content to visible row and column.
                    else
                    {
                        Grid.SetRow(MyAdControl, 0);
                        Grid.SetRow(this.countdownControl.ContentPanel, 1);
                        Grid.SetRowSpan(this.countdownControl.ContentPanel, 2);
                        Grid.SetRow(this.countdownControl.ButtonPanel, 4);
                        this.countdownControl.LapBorder.Visibility = Visibility.Collapsed;
                        this.countdownControl.LapGrid.Visibility = Visibility.Collapsed;
                    }
                    break;
            }
        }

        public void SaveStartDateOfTrial()
        {
            Trial.SaveStartDateOfTrial();
        }

        public DateTime GetStartDateOfTrial()
        {
            DateTime returnValue = DateTime.Today;

            returnValue = Trial.GetStartDateOfTrial();

            return returnValue;
        }

        public int GetDaysLeftInTrial()
        {
            int returnValue = 0;
            returnValue = Trial.GetDaysLeftInTrial();
            return returnValue;
        }

        private bool IsTrialExpired()
        {
            bool trialExpired = false;

            trialExpired = Trial.IsTrialExpired();

            return trialExpired;
        }

        #endregion "Common Routines"

        /// <summary>
        /// Called when the app comes back to life.  I put this in so when the trial is over, and the user is directed to the store, if 
        /// the user purchases the app it will be activated when he / she comes back.  If the app is not activated, and the user just presses
        /// the back button, we'll de-activate the app and go to the  "Purchase" screen.
        /// </summary>
        internal void AppActivated()
        {
            try
            {
                if ((Application.Current as App).IsTrial)
                {
                    if (Trial.IsTrialExpired())
                    //if (true)
                    {
                        try
                        {
                            Dispatcher.BeginInvoke(() =>
                                {
                                    NavigationService.Navigate(new Uri("/TrialExpired.xaml", UriKind.Relative));
                                });
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                else
                {
                    EnableApp(true);
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}