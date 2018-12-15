using System;
using System.Windows.Input;

namespace ApiReviewList.ViewModels
{
    internal sealed class Command : ICommand
    {
        private readonly Action _handler;

        public Command(Action handler)
        {
            _handler = handler;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            _handler();
        }

        public event EventHandler CanExecuteChanged;
    }
}
