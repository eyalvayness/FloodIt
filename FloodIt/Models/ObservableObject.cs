using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace FloodIt.Models
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected bool SetProperty<T>(ref T backingStore, T value, Action ifTrue, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            ifTrue();
            return true;
        }

        protected bool SetProperty<T>(ref T backingStore, T value, bool checkEquals, [CallerMemberName] string propertyName = "")
        {
            if (checkEquals && EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "", object sender = null) => PropertyChanged?.Invoke(sender ?? this, new PropertyChangedEventArgs(propertyName));
        protected void OnPropertiesChanged(object sender = null, params string[] propertiesName) => Array.ForEach(propertiesName, prop => OnPropertyChanged(prop, sender));// propertyName => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
    }
}
