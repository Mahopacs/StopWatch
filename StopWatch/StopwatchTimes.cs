using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StopWatch
{
    public class StopwatchTimes : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;


        #region "Properties"
        
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private int _itemCount;
        public int ItemCount
        {
            get { return _itemCount; }
            set
            {
                _itemCount = value;
                NotifyPropertyChanged("ItemCount");
            }
        }

        private string _lapTime;
        public string LapTime
        {
            get { return _lapTime; }
            set
            {
                _lapTime = value;
                NotifyPropertyChanged("LapTime");
            }
        }

        private string _splitTime;
        public string SplitTime
        {
            get { return _splitTime; }
            set
            {
                _splitTime = value;
                NotifyPropertyChanged("SplitTime");
            }
        }
        #endregion "Properties"

    }
}
