using Caliburn.Micro;
using IMAP.Popup.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Popup.ViewModels
{
    public class EmailViewModel : Screen
    {
        private Email _email;
        private readonly EmailModel _emailModel;
        private bool _isViewLoaded;

        public event System.Action EmailViewClosing;

        public EmailViewModel(EmailModel emailModel)
        {
            _emailModel = emailModel;
            DisplayName = "Email";
        }

        public bool IsViewLoaded
        {
            get
            {
                return _isViewLoaded;
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            _isViewLoaded = true;
        }

        public override void CanClose(Action<bool> callback)
        {
            base.CanClose(callback);
            _isViewLoaded = false;
            var emailViewClosing = EmailViewClosing;
            if (emailViewClosing != null)
                emailViewClosing();
        }

        public void Set(Email email)
        {
            _email = email;
            NotifyOfPropertyChange(() => From);
            NotifyOfPropertyChange(() => To);
            NotifyOfPropertyChange(() => Cc);
            NotifyOfPropertyChange(() => Subject);
            NotifyOfPropertyChange(() => EmailContent);
        }

        public string From
        {
            get
            {
                return _email.From;
            }
        }

        public string To
        {
            get
            {
                var toEmails = _email.To.Aggregate(String.Empty, (acc, to) => acc += (to + ";"));
                return toEmails.EndsWith(";") ? toEmails.Substring(0, toEmails.Length - 1) : toEmails;
            }
        }

        public string Cc
        {
            get
            {
                var ccEmails = _email.Cc.Aggregate(String.Empty, (acc, cc) => acc += (cc + ";"));
                return ccEmails.EndsWith(";") ? ccEmails.Substring(0, ccEmails.Length - 1) : ccEmails;

            }
        }

        public string Subject
        {
            get
            {
                return _email.Subject;
            }
        }

        public string EmailContent
        {
            get
            {
                return _email.Content;
            }
        }

        public void MarkAsRead()
        {
            Task.Run(() =>
                {
                    var uid = _email.MessageUid;
                    _emailModel.MarkAsRead(uid);
                });
            TryClose();
        }

    }
}
