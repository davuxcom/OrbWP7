using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO.IsolatedStorage;

namespace OrbWP7
{
    public class ItemViewModel : INotifyPropertyChanged
    {

        private bool _LoadedChildren = false;
        public ObservableCollection<ItemViewModel> Children { get; private set; }

        private string _Title;
        public string Title
        {
            get
            {
                return _Title;
            }
            set
            {
                if (value != _Title)
                {
                    _Title = value;
                    NotifyPropertyChanged("Title");
                }
            }
        }

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

        private string _Description;
        public string Description
        {
            get
            {
                return _Description;
            }
            set
            {
                if (value != _Description)
                {
                    _Description = value;
                    NotifyPropertyChanged("Description");
                }
            }
        }        

        private BitmapImage _Thumbnail = null;
        public BitmapImage Thumbnail
        {
            get
            {
                if (_Thumbnail == null)
                {
                    LoadThumbnail();
                }
                return _Thumbnail;
            }
            set
            {
                if (value != _Thumbnail)
                {
                    _Thumbnail = value;
                    NotifyPropertyChanged("Thumbnail");
                }
            }
        }

        private void LoadThumbnail()
        {
            if (!string.IsNullOrEmpty(this.ThumbnailID))
            {
                App.ViewModel.GetThumbnail(this, image =>
                    {

                        _Thumbnail = image;
                        NotifyPropertyChanged("Thumbnail");
                    }, error => { });
            }
        }        


        public event PropertyChangedEventHandler PropertyChanged;
        private OrbAPI.ResultItemBase resultItemBase;

        public ItemViewModel(OrbAPI.ResultItemBase resultItemBase)
        {
            Children = new ObservableCollection<ItemViewModel>();
            this.resultItemBase = resultItemBase;
            this.Title = resultItemBase.Title;
            this.SearchID = resultItemBase.SearchID;
            this.MediaType = resultItemBase.MediaType;

            var item = resultItemBase as OrbAPI.Item;
            if (item != null)
            {
                this.ThumbnailID = item.ThumbnailID;
                this.IsMedia = true;
                this.Description = formatBytes(item.Size);
            }
            var group = resultItemBase as OrbAPI.Group;
            if (group != null)
            {
                this.Description = group.Count + " items";
            }
        }
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string formatBytes(float bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = 0;
            for (i = 0; (int)(bytes / 1024) > 0; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
            return String.Format("{0:0.00} {1}", dblSByte, Suffix[i]);
        }

        public void LoadChildren()
        {
            if (!_LoadedChildren)
            {
                IsLoading = true;
                App.ViewModel.LoadChildren(this, child =>
                    {
                        Children.Add(child);
                        IsLoading = false;
                    });
                _LoadedChildren = true;
            }
        }

        public void GetStreamURL(Action<string> UrlFound, Action<string> ErrorCallback)
        {
            IsLoading = true;
            App.ViewModel.GetStream(this, url =>
            {
                IsLoading = false;
                UrlFound(url);
            }, ErrorCallback);
        }

        public string GetDownloadURL()
        {
            var url = string.Format("https://api.orb.com/orb/data/download?sid={0}&mediumId={1}&open=1", App.ViewModel.SessionID, SearchID);
            Debug.WriteLine("Download Url: " + url);
            return url;
        }

        public string GetImageURL()
        {
            var url = string.Format("https://api.orb.com/orb/data/image?mediumId={0}&sid={1}", SearchID, App.ViewModel.SessionID);
            Debug.WriteLine("Image Url: " + url);
            return url;
        }

        public string SearchID { get; set; }

        public string ThumbnailID { get; set; }

        public bool IsMedia { get; set; }

        public string MediaType { get; set; }

        public bool MediaOpened { get; set; }
    }
}