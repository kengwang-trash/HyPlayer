using Windows.UI.Xaml.Controls;
using HyPlayer.Contracts.ViewModels;
using Windows.UI.Xaml;
using HyPlayer;

namespace Flarum.Desktop.Dialogs
{
   public abstract class DialogBase<TViewModel> : ContentDialog
   where TViewModel : class, IViewModel
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(TViewModel), typeof(TViewModel),
                                        new PropertyMetadata(default));

        public TViewModel ViewModel
        {
            get => (TViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        protected DialogBase()
        {
            ViewModel = Locator.Instance.GetService<TViewModel>();
            DataContext = ViewModel;
        }
    }
}
