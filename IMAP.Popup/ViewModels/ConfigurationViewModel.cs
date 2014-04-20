using Caliburn.Micro;
using IMAP.Popup.Models;

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