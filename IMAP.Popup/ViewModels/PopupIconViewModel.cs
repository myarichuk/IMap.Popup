using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using IMAP.Popup.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace IMAP.Popup.ViewModels
{
    public class PopupIconViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly IWindowManager _windowManager;
        private readonly PopupIconModel _model;
        private readonly TaskbarIcon _taskbarIcon;

        public PopupIconViewModel(IWindowManager windowManager,                                  
                                  ConfigurationViewModel configurationViewModel,
                                  PopupIconModel model,
                                  TaskbarIcon taskbarIcon)
        {
            _windowManager = windowManager;
            _model = model;
            _configurationViewModel = configurationViewModel;
            _model.MailServerPolled += OnMailServerPolled;
            _model.MailReceived += OnMailReceived;
            _taskbarIcon = taskbarIcon;
        }

        public void ShowConfiguration()
        {
            _windowManager.ShowDialog(_configurationViewModel);
        }

       public bool IsChecked
        {
            get
            {
                return _model.IsPollingActive;
            }
            set
            {
                _model.IsPollingActive = value;
            }
        }

       public int UnreadMailCount
       {
           get
           {
               return _model.UnreadMailCount;
           }
       }

        public void ExitApplication()
        {            
            Application.Current.Shutdown();
        }

        public void ResetLastFetchDate()
        {
            _model.ResetLastFetchDate();
        }

        private void OnMailServerPolled()
        {
            NotifyOfPropertyChange(() => UnreadMailCount);
        }

        private void OnMailReceived(Email receivedMail)
        {
            var configuration = _configurationViewModel.ConfigurationData;
            
            NewMailBaloon newMailBaloon = null;
            _taskbarIcon.Dispatcher.Invoke(new System.Action(() =>
            {
                newMailBaloon = new NewMailBaloon();
                newMailBaloon.Dispatcher.Invoke(new System.Action(() =>
                {
                    newMailBaloon.FromText = receivedMail.From;
                    newMailBaloon.SubjectText = receivedMail.Subject;
                    newMailBaloon.HighlightBrush = GetHighlightBrushFromRules(receivedMail, configuration.HighlightRules); 
                }));
            }));

            _taskbarIcon.ShowCustomBalloon(newMailBaloon, PopupAnimation.Slide, configuration.PopupDelay);
        }

        private SolidColorBrush GetHighlightBrushFromRules(Email mail, IEnumerable<MailHighlightRule> mailHighlightRules)
        {
            var defaultBrush = new SolidColorBrush(Colors.Transparent);
            if(mailHighlightRules == null || mailHighlightRules.Any() == false)
                return defaultBrush;

            foreach(var rule in mailHighlightRules)
            {
                if ((!String.IsNullOrWhiteSpace(rule.FromRegex) && mail.From.RegexContains(rule.FromRegex)) ||
                   (!String.IsNullOrWhiteSpace(rule.SubjectRegex) && mail.Subject.RegexContains(rule.SubjectRegex)))
                    return new SolidColorBrush(rule.HighlightColor);
            }

            return defaultBrush;
        }
    }
}
