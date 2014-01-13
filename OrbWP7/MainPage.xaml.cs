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
using OrbAPI;
using System.Diagnostics;
using DavuxLibSL.Extensions;
using System.Threading;

namespace OrbWP7
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();


            txtUsername.IsEnabled = txtPassword.IsEnabled = btnLogin.IsEnabled = string.IsNullOrEmpty(DavuxLibSL.Settings.Get("UserName"));
            
            DataContext = App.ViewModel;
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);
        }

        // Load data for the ViewModel Items
        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (App.ViewModel.IsDataLoaded) return;

            LoginPanel.Visibility = System.Windows.Visibility.Visible;
            PivotPanel.Visibility = System.Windows.Visibility.Collapsed;


            if (!string.IsNullOrEmpty( DavuxLibSL.Settings.Get("UserName", null)))
            {
                txtUsername.Text = DavuxLibSL.Settings.Get("UserName");
                txtPassword.Password = DavuxLibSL.Settings.Get("Password");
                // auto-login
                Login();
            }
        }

        private void Login()
        {
            lblLoginError.Text = "";
            txtUsername.IsEnabled = txtPassword.IsEnabled = btnLogin.IsEnabled = false;

            bool shownError = false;

            App.ViewModel.Connect(() =>
            {
                // successful login
                LoginPanel.Visibility = System.Windows.Visibility.Collapsed;
                PivotPanel.Visibility = System.Windows.Visibility.Visible;
            }, error =>
                {
                    // unable to login
                    lblLoginError.Text = error;
                    txtUsername.IsEnabled = txtPassword.IsEnabled = btnLogin.IsEnabled = true;
                },
                error =>
                {
                    // since we fetch 4 feeds at once, we should only show the first error
                    // yeah, this is a hack
                    if (shownError) return;
                    shownError = true;

                    // unable to do something AFTER login
                    MessageBox.Show(error, "Orb Communication Error", MessageBoxButton.OK);
                    
                });
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var box = (sender as ListBox);
            if (box.SelectedIndex == -1) return;
            App.ViewModel.Push(box.SelectedItem as ItemViewModel);
            NavigationService.Navigate(new Uri("/DrillDown.xaml", UriKind.Relative));
            box.SelectedIndex = -1;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            DavuxLibSL.Settings.Set("UserName", txtUsername.Text);
            DavuxLibSL.Settings.Set("Password", txtPassword.Password);
            Login();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            DavuxLibSL.Settings.Set("UserName", "");
            DavuxLibSL.Settings.Set("Password", "");
            App.Quit();
        }

        private void btnClearCache_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.IsLoading = true;
            new Thread(() =>
                {
                    var isf = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication();

                    var files = isf.GetFileNames();

                    foreach (var file in files)
                    {
                        isf.DeleteFile(file);
                    }

                    Dispatcher.BeginInvoke(() => App.ViewModel.IsLoading = false);
                }).Start();
        }

        private void btnAbout_Click(object sender, RoutedEventArgs e)
        {
            DavuxLibSL.App.Assembly = System.Reflection.Assembly.GetExecutingAssembly();
            NavigationService.Navigate(new Uri("/DavuxLibSL;component/About.xaml", UriKind.RelativeOrAbsolute));
        }

        private void btnSpeed_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            switch (btn.Content as string)
            {
                case "EDGE":
                    App.ViewModel.StreamingSpeed = 128;
                    break;
                case "3G":
                    App.ViewModel.StreamingSpeed = 512;
                    break;
                case "WiFi":
                    App.ViewModel.StreamingSpeed = 1024;
                    break;
                case "Low":
                    App.ViewModel.StreamingSpeed = 32;
                    break;
            }
        }

    }
}