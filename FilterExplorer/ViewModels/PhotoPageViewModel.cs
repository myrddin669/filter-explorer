﻿using FilterExplorer.Commands;
using FilterExplorer.Models;
using FilterExplorer.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace FilterExplorer.ViewModels
{
    public class PhotoPageViewModel : INotifyPropertyChanged
    {
        public IDelegateCommand GoBackCommand { get; private set; }
        public IDelegateCommand OpenPhotoCommand { get; private set; }
        public IDelegateCommand SavePhotoCommand { get; private set; }
        public IDelegateCommand SharePhotoCommand { get; private set; }
        public IDelegateCommand AddFilterCommand { get; private set; }
        public IDelegateCommand RemoveFilterCommand { get; private set; }

        private PhotoViewModel _photo = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public PhotoViewModel Photo
        {
            get
            {
                return _photo;
            }

            private set
            {
                if (_photo != value)
                {
                    if (_photo != null)
                    {
                        _photo.Model.ModifiedChanged -= Photo_Model_ModifiedChanged;
                        _photo.Model.Filters.ItemsChanged -= Photo_Model_Filters_ItemsChanged;
                    }

                    _photo = value;

                    if (_photo != null)
                    {
                        _photo.Model.ModifiedChanged += Photo_Model_ModifiedChanged;
                        _photo.Model.Filters.ItemsChanged += Photo_Model_Filters_ItemsChanged;
                    }

                    SavePhotoCommand.RaiseCanExecuteChanged();
                    AddFilterCommand.RaiseCanExecuteChanged();
                    RemoveFilterCommand.RaiseCanExecuteChanged();

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Photo"));
                    }
                }
            }
        }

        public PhotoPageViewModel()
        {
            GoBackCommand = CommandFactory.CreateGoBackCommand();

            OpenPhotoCommand = new DelegateCommand(
                async (parameter) =>
                {
                    var file = await PhotoLibraryModel.PickPhotoFileAsync();

                    if (file != null)
                    {
                        var photo = new PhotoModel(file);

                        SessionModel.Instance.Photo = photo;

                        Photo = new PhotoViewModel(SessionModel.Instance.Photo);
                    }
                });

            SavePhotoCommand = new DelegateCommand(
                async (parameter) =>
                    {
                        await PhotoLibraryModel.SavePhotoAsync(Photo.Model);
                    },
                () =>
                    {
                        return Photo != null ? Photo.Model.Modified : false;
                    });

            SharePhotoCommand = new DelegateCommand(
                (parameter) =>
                    {
                        PhotoShareModel.SharePhotoAsync(Photo.Model);
                    },
                () =>
                    {
                        return PhotoShareModel.Available;
                    });

            AddFilterCommand = new DelegateCommand(
                (parameter) =>
                    {
                        var frame = (Frame)Window.Current.Content;
                        frame.Navigate(typeof(FilterPage));
                    });

            RemoveFilterCommand = new DelegateCommand(
                (parameter) =>
                    {
                        Photo.Model.Filters.RemoveLast();
                    },
                () =>
                    {
                        return Photo != null ? Photo.Model.Filters.Count > 0 : false;
                    });

            Photo = new PhotoViewModel(SessionModel.Instance.Photo);

            PhotoShareModel.AvailableChanged += PhotoShareModel_AvailableChanged;
        }

        ~PhotoPageViewModel()
        {
            if (Photo != null)
            {
                Photo.Model.ModifiedChanged -= Photo_Model_ModifiedChanged;
                Photo.Model.Filters.ItemsChanged -= Photo_Model_Filters_ItemsChanged;
            }

            PhotoShareModel.AvailableChanged -= PhotoShareModel_AvailableChanged;
        }

        private void Photo_Model_ModifiedChanged(object sender, EventArgs e)
        {
            SavePhotoCommand.RaiseCanExecuteChanged();
        }

        private void Photo_Model_Filters_ItemsChanged(object sender, EventArgs e)
        {
            RemoveFilterCommand.RaiseCanExecuteChanged();
        }

        private void PhotoShareModel_AvailableChanged(object sender, EventArgs e)
        {
            SharePhotoCommand.RaiseCanExecuteChanged();
        }
    }
}
