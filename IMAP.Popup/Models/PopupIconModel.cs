using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace IMAP.Popup.Models
{
    public class PopupIconModel : IDisposable
    {
        public event Action<Email> MailReceived;
        public event Action MailServerPolled;

        private const string LastFetchDateKey = "last-fetch-date";
        private const string UnreadMailCountKey = "unread-mail-count";
        private const string IsPollingActivatedKey = "is-polling-activated";
        
        private readonly Func<Configuration> GetConfiguration;
        private readonly PersistanceModel _persistanceModel;
        private bool _isPolling;
        private readonly Thread _pollingThread;


        public bool IsPollingActive
        {
            get
            {
                return _persistanceModel.Get<bool>(IsPollingActivatedKey);
            }
            set
            {
                _persistanceModel.Set<bool>(IsPollingActivatedKey, value);
            }
        }

        public int UnreadMailCount
        {
            get
            {
                return _persistanceModel.Get<int>(UnreadMailCountKey);
            }
            private set
            {
                _persistanceModel.Set<int>(UnreadMailCountKey,value);
            }
        }

        public PopupIconModel(PersistanceModel persistanceModel)
        {
            _isPolling = true;
            _persistanceModel = persistanceModel;
            GetConfiguration = () => persistanceModel.LoadConfiguration();
            _pollingThread = new Thread(DoMailPolling) { Name = "Mail Polling Thread", IsBackground = true };
            _pollingThread.Start();

            if (!_persistanceModel.Contains(LastFetchDateKey))
                _persistanceModel.Set(LastFetchDateKey, DateTime.UtcNow);            
        }

        public void ResetLastFetchDate()
        {
            _persistanceModel.Set(LastFetchDateKey, new DateTime(1982,1,1));
        }

        protected void OnMailServerPolled()
        {
            if (MailServerPolled != null)
                Task.Run(() => MailServerPolled());
        }

        protected void DoMailPolling(object threadState)
        {
            while(_isPolling)
            {
                if (IsPollingActive)
                {
                    var fetchedMails = FetchNewMails().ToList();
                    var lastFetchDate = _persistanceModel.Get<DateTime>(LastFetchDateKey);
                    var relevantFetchedMails = fetchedMails.Where(x => x.Date.UtcDateTime > lastFetchDate)
                                                           .ToList();
                    if (relevantFetchedMails.Any())
                    {
                        OnMailReceive(relevantFetchedMails);
                        if (relevantFetchedMails.Any())
                            _persistanceModel.Set(LastFetchDateKey, DateTime.UtcNow);

                    }
                }
                
                var configuration = _persistanceModel.LoadConfiguration();
                Thread.Sleep(configuration.PollingInterval);
            }
        }

        private void OnMailReceive(IReadOnlyList<MimeMessage> fetchedMails)
        {
            var mailReceivedEvent = MailReceived;
            if (mailReceivedEvent != null)
            {
                var eventThread = new Thread(() =>
                {
                    foreach (var mail in fetchedMails)
                        mailReceivedEvent(new Email
                        {
                            From = mail.From.First().ToString(),
                            To = mail.To.Select(x => x.ToString()).ToArray(),
                            Cc = mail.Cc.Select(x => x.ToString()).ToArray(),
                            Subject = mail.Subject,
                            WhenSent = mail.Date.UtcDateTime                            
                        });
                });

                eventThread.SetApartmentState(ApartmentState.STA);
                eventThread.Start();
            }
        }

        protected IEnumerable<MimeMessage> FetchNewMails()
        {
            var configuration = GetConfiguration();
            var credentials = new NetworkCredential(configuration.Username, configuration.Password);

            var uri = new Uri("imaps://" + configuration.ImapServer + ":" + configuration.ImapPort);
            using (var cancel = new CancellationTokenSource())
            {
                using (var mailClient = new ImapClient())
                {
                    mailClient.Connect(uri, cancel.Token);
                    mailClient.AuthenticationMechanisms.Remove("XOAUTH");

                    mailClient.Authenticate(credentials, cancel.Token);

                    var inbox = mailClient.Inbox;
                    inbox.Open(FolderAccess.ReadOnly, cancel.Token);

                    UniqueId[] recentMailUids = null;
                    try
                    {
                        var lastFetchDate = _persistanceModel.Get<DateTime>(LastFetchDateKey);
                        recentMailUids = inbox.Search(SearchQuery.NotSeen
                                                .And(SearchQuery.DeliveredAfter(lastFetchDate)), cancel.Token);

                        UnreadMailCount = inbox.Search(SearchQuery.NotSeen,cancel.Token).Length;
                    }
                    catch (Exception e)
                    {
                        //TODO : add logging
                    }

                    if (recentMailUids == null)
                        yield break;

                    foreach (var uid in recentMailUids)
                        yield return inbox.GetMessage(uid, cancel.Token);
                }
            }
        }

        public void Dispose()
        {
            _isPolling = false;
            Thread.Sleep(_persistanceModel.LoadConfiguration().PollingInterval + 10);
            if(_pollingThread.IsAlive)
                try
                {
                    _pollingThread.Abort();
                }
                catch { }
        }
    }
}
