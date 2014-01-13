using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using OrbAPI;
using System.Text.RegularExpressions;
using System.IO.IsolatedStorage;
using DavuxLibSL.Extensions;
using System.Threading;

namespace OrbWP7
{
    public class MainViewModel : INotifyPropertyChanged
    {

        private Stack<ItemViewModel> Models = new Stack<ItemViewModel>();

        public void Push(ItemViewModel model)
        {
            Models.Push(model);
        }

        public ItemViewModel Pop()
        {
            return Models.Pop();
        }

        public ItemViewModel Peek()
        {
            return Models.Peek();
        }

        public MainViewModel()
        {
            this.VideoItems = new ObservableCollection<ItemViewModel>();
            this.PhotoItems = new ObservableCollection<ItemViewModel>();
            this.DocumentItems = new ObservableCollection<ItemViewModel>();
            this.MusicItems = new ObservableCollection<ItemViewModel>();
        }

        public ObservableCollection<ItemViewModel> VideoItems { get; private set; }
        public ObservableCollection<ItemViewModel> PhotoItems { get; private set; }
        public ObservableCollection<ItemViewModel> DocumentItems { get; private set; }
        public ObservableCollection<ItemViewModel> MusicItems { get; private set; }

        public bool IsDataLoaded { get; private set; }

        private Orb orb = null;

        private bool _IsLoading;
        public bool IsLoading
        {
            get
            {
                return _IsLoading;
            }
            set
            {
                if (value != _IsLoading)
                {
                    _IsLoading = value;
                    NotifyPropertyChanged("IsLoading");
                }
            }
        }        


        public int StreamingSpeed
        {
            get
            {
                return DavuxLibSL.Settings.Get("StreamingSpeed", 512);
            }
            set
            {
                if (value != StreamingSpeed)
                {
                    DavuxLibSL.Settings.Set("StreamingSpeed", value);

                    NotifyPropertyChanged("StreamingSpeed");
                    NotifyPropertyChanged("StreamingSpeedText");
                }
            }
        }        

        public string StreamingSpeedText
        {
            get
            {
                int speed = StreamingSpeed;
                string text;
                if (speed > 3000)
                {
                    text = "Fast WiFi";
                }
                else if (speed >= 1024)
                {
                    text = "WiFi";
                }
                else if (speed > 384)
                {
                    text = "3G";
                }
                else if (speed > 64)
                {
                    text = "EDGE";
                }
                else
                {
                    text = "GPRS";
                }
                return string.Format("{0}kbps, suitable for {1} streaming", speed, text);
            }
        }        

        public void Connect(Action SuccessCallback, Action<string> LoginErrorCallback, Action<string> ErrorCallback)
        {
            IsLoading = true;
            orb = new Orb(DavuxLibSL.Settings.Get("UserName"), DavuxLibSL.Settings.Get("Password"), () =>
            {
                // success

                const int cMaxResponses = 4;
                int cResponses = 0;

                orb.Search("video", "", results =>
                    {
                        IsLoading = (++cResponses) < cMaxResponses;
                        results.Groups.ForEach(g => VideoItems.Add(new ItemViewModel(g as ResultItemBase)));
                        results.Items.ForEach(g => VideoItems.Add(new ItemViewModel(g as ResultItemBase)));
                    }, 
                    error =>
                    {
                        IsLoading = (++cResponses) < cMaxResponses;
                        ErrorCallback(error);
                    });

                orb.Search("photo", "", results =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    results.Groups.ForEach(g => PhotoItems.Add(new ItemViewModel(g as ResultItemBase)));
                    results.Items.ForEach(g => PhotoItems.Add(new ItemViewModel(g as ResultItemBase)));
                },
                error =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    ErrorCallback(error);
                });

                orb.Search("document", "", results =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    results.Groups.ForEach(g => DocumentItems.Add(new ItemViewModel(g as ResultItemBase)));
                    results.Items.ForEach(g => DocumentItems.Add(new ItemViewModel(g as ResultItemBase)));
                },
                error =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    ErrorCallback(error);
                });

                orb.Search("audio", "", results =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    results.Groups.ForEach(g => MusicItems.Add(new ItemViewModel(g as ResultItemBase)));
                    results.Items.ForEach(g => MusicItems.Add(new ItemViewModel(g as ResultItemBase)));
                },
                error =>
                {
                    IsLoading = (++cResponses) < cMaxResponses;
                    ErrorCallback(error);
                });

                IsDataLoaded = true;
                SuccessCallback();
            },
            error =>
            {
                IsLoading = false;
                LoginErrorCallback(error);
            });
        }

        public void LoadChildren(ItemViewModel model, Action<ItemViewModel> ChildFound)
        {
            orb.Search(model.MediaType, model.SearchID, results =>
            {
                results.Groups.ForEach(g => ChildFound(new ItemViewModel(g as ResultItemBase)));
                results.Items.ForEach(g => ChildFound(new ItemViewModel(g as ResultItemBase)));
            },
            error =>
            {

            });
        }

        public void GetStream(ItemViewModel model, Action<string> UrlFound, Action<string> ErrorCallback)
        {
            orb.GetStream(model.SearchID, DavuxLibSL.Settings.Get("StreamingSpeed", 512),
                content =>
                {
                    // <REF HREF="http://192.168.1.30:81/74491BCB93C23FB1/1/E0/live.wmv"/>
                    var m = Regex.Match(content, "HREF=\"(.*?)\"");
                    if (m.Success)
                    {
                        UrlFound(m.Groups[1].Value);
                    }
                    else
                    {
                        ErrorCallback("Could not read URL from ASX file");
                    }
                    Debug.WriteLine("asx: " + UrlFound);
                },
                ErrorCallback);
        }

        static IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication();

        public void GetImage(ItemViewModel model, Action<BitmapImage> Callback, Action<string> ErrorCallback)
        {
            // NOTE:  can't access RootVisual off UI thread, so we'll need a ref to the dispatcher
            var dispatcher = App.Current.RootVisual.Dispatcher;

            Action<byte[]> ReturnThumbnail = bytes =>
            {
                dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        BitmapImage bm = new BitmapImage();
                        bm.SetSource(new System.IO.MemoryStream(bytes, false));
                        Callback(bm);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Can't set image: " + ex.Message);
                    }
                });
            };


            if (isf.FileExists(model.SearchID + "_img"))
            {
                ReturnThumbnail(isf.ReadAllBytes(model.SearchID + "_img"));
            }
            else
            {
                orb.DownloadImage(model.SearchID, bytes =>
                {
                    // save the thumbnail first
                    isf.WriteAllBytes(model.SearchID + "_img", bytes);
                    ReturnThumbnail(bytes);
                }, ErrorCallback);
            }
        }

        public void GetThumbnail(ItemViewModel model, Action<BitmapImage> Callback, Action<string> ErrorCallback)
        {
            // NOTE:  can't access RootVisual off UI thread, so we'll need a ref to the dispatcher
            var dispatcher = App.Current.RootVisual.Dispatcher;

            Action<byte[]> ReturnThumbnail = bytes =>
                {
                    dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            BitmapImage bm = new BitmapImage();
                            bm.SetSource(new System.IO.MemoryStream(bytes, false));
                            Callback(bm);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Can't set image: " + ex.Message);
                        }
                    });
                };

            ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (isf.FileExists(model.ThumbnailID))
                    {
                        ReturnThumbnail(isf.ReadAllBytes(model.ThumbnailID));
                    }
                    else
                    {
                        orb.DownloadThumbnail(model.ThumbnailID, bytes =>
                        {
                            // save the thumbnail first
                            isf.WriteAllBytes(model.ThumbnailID, bytes);
                            ReturnThumbnail(bytes);
                        }, ErrorCallback);
                    }
                });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string SessionID
        {
            get
            {
                return orb.SessionID;
            }
        }
    }
}