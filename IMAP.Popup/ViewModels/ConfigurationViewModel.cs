using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using System;
using System.Windows;
namespace IMAP.Popup.ViewModels
{

    public class ConfigurationViewModel : Screen
    {
        private PersistanceModel _model;

        public ConfigurationViewModel(PersistanceModel model)
        {
            _model = model;
            DisplayName = "Configuration";
        }

        public Configuration ConfigurationData
        {
            get
            {
                var configurationData = _model.LoadConfiguration() ?? new Configuration();
                return configurationData;
            }
            set
            {
                _model.SaveConfiguration(value);
                NotifyOfPropertyChange(() => ConfigurationData);
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