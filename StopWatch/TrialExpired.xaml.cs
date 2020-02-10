using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace StopWatch
{
    public partial class TrialExpired : PhoneApplicationPage
    {
        public TrialExpired()
        {
            InitializeComponent();
        }

        private void PurchaseClicked(object sender, RoutedEventArgs e)
        {
            MarketplaceDetailTask _marketPlaceDetailTask = new MarketplaceDetailTask();
            _marketPlaceDetailTask.Show();
        }
    }
}