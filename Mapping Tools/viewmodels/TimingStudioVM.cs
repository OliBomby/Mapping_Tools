﻿using Mapping_Tools.Classes.TimingStudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Mapping_Tools.Viewmodels
{
    public class TimingStudioVM : INotifyPropertyChanged
    {
        private int _currentTime;
        private string _baseBeatmap;
        private ObservableCollection<StudioTimingPoint> _timingPoints;

        public string baseBeatmap
        {
            get => _baseBeatmap; set
            {
                if (_baseBeatmap != value)
                {
                    _baseBeatmap = value;
                    NotifyPropertyChanged("baseBeatmap");
                }
            }
        }

        public ObservableCollection<StudioTimingPoint> timingPoints
        {
            get => _timingPoints;
            set
            {
                if (_timingPoints != value)
                {
                    _timingPoints = value;
                    NotifyPropertyChanged("timingPoints");
                }
            }
        }

        public int currentTime
        {
            get => _currentTime; set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    NotifyPropertyChanged("currentTime");
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
