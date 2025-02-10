#region

using AsyncAwaitBestPractices;
using HyPlayer.Classes;
using HyPlayer.Contract.Views;
using HyPlayer.Controls;
using HyPlayer.HyPlayControl;
using HyPlayer.NeteaseApi.ApiContracts;
using HyPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

#endregion

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace HyPlayer.Pages;

/// <summary>
///     可用于自身或导航至 Frame 内部的空白页。
/// </summary>
public sealed partial class Home : HomePageBase, IDisposable
{
    private static List<string> RandomSlogen = new()
    {
        "用音乐开启新的一天吧",
        "戴上耳机 享受新的一天吧"
    };
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private CancellationToken _cancellationToken;

    private bool disposedValue = false;

    public Home()
    {
        InitializeComponent();
        _cancellationToken = _cancellationTokenSource.Token;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewModel.GetDataAsync().SafeFireAndForget();
    }

   

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        HyPlayList.OnLoginDone -= ViewModel.LoadLoginedContent;
        try
        {
            _cancellationTokenSource.Cancel();
        }
        catch
        {
            //Ignore
        }
        Dispose();
    }


    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        PersonalFM.InitPersonalFM();
    }



    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                MainContainer.Children.Clear();
                _cancellationTokenSource.Dispose();
            }
            HyPlayList.OnLoginDone -= ViewModel.LoadLoginedContent;
            disposedValue = true;
        }
    }

    ~Home()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class HomePageBase : AppPageBase<HomeViewModel>
{

}