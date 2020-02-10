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
using System.Reflection;

namespace StopWatch
{
    public partial class About : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        public About()
        {
            InitializeComponent();

            AssemblyName assemblyName = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            
            Author = "Author: KLB Creations";
            VersionString = "Version: " +  assemblyName.Version.ToString();
            Support = "Support/Feedback: KLBCreation01@yahoo.com";
            PicCredits = "Pic Credits:http://www.freedigitalphotos.net/;http://animal-backgrounds.com";
            SoundCredits = "Sound Credits:http://www.animal-sounds.org/;http://www.findsounds.com/";

            this.DataContext = this;
        }

        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private String _versionString;
        public String VersionString
        {
            get { return _versionString; }
            set
            {
                _versionString = value;
                NotifyPropertyChanged("VersionString");
            }
        }

        private String _author;
        public String Author
        {
            get { return _author; }
            set
            {
                _author = value;
                NotifyPropertyChanged("Author");
            }
        }

        private String _support;
        public String Support
        {
            get { return _support; }
            set
            {
                _support = value;
                NotifyPropertyChanged("Support");
            }
        }

        private String _picCredits;
        public String PicCredits
        {
            get { return _picCredits; }
            set
            {
                _picCredits = value;
                NotifyPropertyChanged("PicCredits");
            }
        }

        private String _soundCredits;
        public String SoundCredits
        {
            get { return _soundCredits; }
            set
            {
                _soundCredits = value;
                NotifyPropertyChanged("SoundCredits");
            }
        }
    }
}