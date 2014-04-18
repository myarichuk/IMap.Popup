using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using System;
using System.Windows;
namespace IMAP.Popup.ViewModels
{

    public class ConfigurationViewModel : Screen
    {
        private readonly PersistanceModel _model;

        public ConfigurationViewModel(PersistanceModel model)
        {
            _model = model;
            DisplayName = "Configuration";
        }

        public Configuration ConfigurationData
        {
            get
            {
                var configurationData = _model.LoadConfiguration();
                return configurationData;
            }            
        }

        public void SaveConfiguration(Configuration configurationData)
        {
            _model.SaveConfiguration(configurationData);
            NotifyOfPropertyChange(() => ConfigurationData);
            TryClose();
        }
    }
}