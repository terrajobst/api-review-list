using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApiReviewList.ViewModels
{
    internal abstract class ViewModel : INotifyPropertyChanged
    {
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var args = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(args);
        }

        protected void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
