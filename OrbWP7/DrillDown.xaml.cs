using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace OrbWP7
{
    public partial class DrillDown : PhoneApplicationPage
    {

        ItemViewModel model;
        public DrillDown()
        {
            this.model = App.ViewModel.Peek();
            InitializeComponent();
            DataContext = model;


            if (model.IsMedia)
            {
                if (model.MediaOpened) return;
                Debug.WriteLine("Is Media");
                ContentPanel.Visibility = System.Windows.Visibility.Collapsed;
                TitlePanel.Visibility = System.Windows.Visibility.Collapsed;

                // if we're navigating back immediately
                
                Debug.WriteLine("Didn't jump back");
                Loaded += (_, __) =>
                    {
                        switch (model.MediaType)
                        {
                            case "video":
                                progress.IsIndeterminate = true;
                                model.GetStreamURL(url =>
                                {
                                    Dispatcher.BeginInvoke(() =>
                                    {
                                        progress.IsIndeterminate = false;
                                        model.MediaOpened = true;
                                        MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                                        mediaPlayerLauncher.Media = new Uri(url, UriKind.Absolute);
                                        mediaPlayerLauncher.Show();
                                    });
                                },
                                error =>
                                {
                                    Dispatcher.BeginInvoke(() =>
                                    {
                                        progress.IsIndeterminate = false;
                                        MessageBox.Show("Could not download media stream: " + error);
                                        NavigationService.GoBack();
                                        model.MediaOpened = false;
                                    });
                                });
                                break;
                            case "photo":
                                ContentPanel.Visibility = Visibility;
                                lstItems.Visibility = Visibility.Collapsed;
                                scroller.Visibility = System.Windows.Visibility.Visible;

                                App.ViewModel.GetImage(model, bitmap =>
                                    {
                                        Dispatcher.BeginInvoke(() =>
                                        {
                                            progress.IsIndeterminate = false;
                                            img.Source = bitmap;
                                        });
                                    }, error =>
                                    {
                                        Dispatcher.BeginInvoke(() =>
                                        {
                                            progress.IsIndeterminate = false;
                                            MessageBox.Show("Could not download media stream: " + error);
                                            NavigationService.GoBack();
                                            model.MediaOpened = false;
                                        });
                                    });


                                img.Source = new BitmapImage(new Uri(model.GetImageURL()));
                                break;
                            case "document":
                                WebBrowserTask wbt = new WebBrowserTask();
                                wbt.URL = Uri.EscapeDataString(model.GetDownloadURL());
                                wbt.Show();
                                break;
                            case "audio":
                                progress.IsIndeterminate = true;
                                model.GetStreamURL(url =>
                                {
                                    Dispatcher.BeginInvoke(() =>
                                    {
                                        progress.IsIndeterminate = false;
                                        model.MediaOpened = true;
                                        MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                                        mediaPlayerLauncher.Media = new Uri(url, UriKind.Absolute);
                                        mediaPlayerLauncher.Show();
                                    });
                                },
                                error =>
                                {
                                    Dispatcher.BeginInvoke(() =>
                                    {
                                        progress.IsIndeterminate = false;
                                        MessageBox.Show("Could not download media stream: " + error);
                                        NavigationService.GoBack();
                                        model.MediaOpened = false;
                                    });
                                });
                                break;
                            default:
                                MessageBox.Show("This kind of media is not supported: " + model.MediaType);
                                NavigationService.GoBack();
                                break;
                        }
                    };
            }
            else
            {
                Loaded += (_, __) =>
                    {
                        Debug.WriteLine("loading children");
                        model.LoadChildren();
                    };
            }
        }

        private void lstItems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (sender as ListBox);
            if (box.SelectedIndex == -1) return;
            App.ViewModel.Push(box.SelectedItem as ItemViewModel);
            NavigationService.Navigate(new Uri("/DrillDown.xaml?q=" + model.SearchID, UriKind.Relative));
            box.SelectedIndex = -1;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // kill this page when coming back from the media player.
            if (model.MediaOpened)
            {
                NavigationService.GoBack();
            }
            base.OnNavigatedTo(e);
            model.MediaOpened = false;
        }

        double initialAngle, initialScale;

        private void OnPinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            initialAngle = transform.Rotation;
            initialScale = transform.ScaleX;
        }

        private void OnPinchDelta(object sender, PinchGestureEventArgs e)
        {
            transform.Rotation = initialAngle + e.TotalAngleDelta;
            transform.ScaleX = initialScale * e.DistanceRatio;
            transform.ScaleY = initialScale * e.DistanceRatio;
        }
    }
}