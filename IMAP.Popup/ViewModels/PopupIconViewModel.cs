﻿using Caliburn.Micro;
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
            
            _incomingMailPopupHandler = new Thread(HandleDisplayingOfIncomingMail)
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
	        while(_isApplicationActive)
            {
	            Email incomingMail;
	            while (_incomingMail.TryTake(out incomingMail) && incomingMail != null)
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
                        
            _taskbarIcon.Dispatcher.Invoke(() =>
            {
	            newMailBaloon = new NewMailBaloon();
	            newMailBaloon.BaloonClosing += IncomingEmail_Popup_Closed;
	            newMailBaloon.Dispatcher.Invoke(() =>
	            {
		            newMailBaloon.FromText = email.From;
		            newMailBaloon.SubjectText = email.Subject;
		            newMailBaloon.HighlightBrush = GetHighlightBrushFromRules(email, configuration.HighlightRules ?? new List<MailHighlightRule>());
	            });
            });

            _taskbarIcon.ShowCustomBalloon(newMailBaloon, PopupAnimation.Slide, configuration.PopupDelay);
        }

        private void IncomingEmail_Popup_Closed()
        {
			_incomingMailPopupClosedEvent.Set();
		}

        private static SolidColorBrush GetHighlightBrushFromRules(Email mail, IEnumerable<MailHighlightRule> mailHighlightRules)
        {
            var defaultBrush = new SolidColorBrush(Colors.Transparent);
	        var highlightRules = mailHighlightRules.ToList();
	        if(mailHighlightRules == null || highlightRules.Any() == false)
                return defaultBrush;

            foreach(var rule in highlightRules)
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
