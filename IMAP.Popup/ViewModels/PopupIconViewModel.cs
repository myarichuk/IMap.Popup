using Caliburn.Micro;
using Hardcodet.Wpf.TaskbarNotification;
using IMAP.Popup.Models;
using IMAP.Popup.Views;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace IMAP.Popup.ViewModels
{
    public class PopupIconViewModel : Conductor<Screen>.Collection.OneActive,IDisposable
    {
        private readonly ConfigurationViewModel _configurationViewModel;
        private readonly IWindowManager _windowManager;
        private readonly ManualResetEventSlim _incomingMailPopupClosedEvent;
        private readonly PopupIconModel _model;
        private readonly TaskbarIcon _taskbarIcon;
        private readonly BlockingCollection<Email> _incomingMail;
        private bool _isApplicationActive;
        private readonly Thread _incomingMailPopupHandler;

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
            _isApplicationActive = true;
            _incomingMailPopupClosedEvent = new ManualResetEventSlim();
            _incomingMail = new BlockingCollection<Email>();
            
            _incomingMailPopupHandler = new Thread(() => HandleDisplayingOfIncomingMail())
            {
                Name = "IncomingMailPopupHandler",
                IsBackground = true
            };
            _incomingMailPopupHandler.Start();
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            NotifyOfPropertyChange(() => UnreadMailCount);
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
            _incomingMail.Add(receivedMail);
        }

        private void HandleDisplayingOfIncomingMail()
        {
            Email incomingMail;
            while(_isApplicationActive)
            {
                while (_incomingMail.TryTake(out incomingMail))
                {
                    _incomingMailPopupClosedEvent.Reset();
                    DisplayIncomingEmail(incomingMail);
                    _incomingMailPopupClosedEvent.Wait();
                }
                Thread.Sleep(500);
            }
        }

        private void DisplayIncomingEmail(Email email)
        {
            var configuration = _configurationViewModel.ConfigurationData;
            
            NewMailBaloon newMailBaloon = null;
                        
            _taskbarIcon.Dispatcher.Invoke(new System.Action(() =>
            {
                newMailBaloon = new NewMailBaloon();
                newMailBaloon.BaloonClosing += () => IncomingEmail_Popup_Closed();
                newMailBaloon.Dispatcher.Invoke(new System.Action(() =>
                {
                    newMailBaloon.FromText = email.From;
                    newMailBaloon.SubjectText = email.Subject;
                    newMailBaloon.HighlightBrush = GetHighlightBrushFromRules(email, configuration.HighlightRules);                     
                }));
            }));

            _taskbarIcon.ShowCustomBalloon(newMailBaloon, PopupAnimation.Slide, configuration.PopupDelay);
        }

        private void IncomingEmail_Popup_Closed()
        {
            Task.Run(() =>
            {
                Thread.Sleep(50);
                _incomingMailPopupClosedEvent.Set();
            });
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

        public void Dispose()
        {
            _isApplicationActive = false;
            _incomingMailPopupHandler.Join(510);
            if(_incomingMailPopupHandler.IsAlive)
            {
                try
                {
                    _incomingMailPopupHandler.Abort();
                }
                catch { }
            }
        }
    }
}
