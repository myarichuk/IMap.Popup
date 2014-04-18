using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

namespace IMAP.Popup.Models
{
	public class PopupIconModel : IDisposable
	{
		private const string LastFetchDateKey = "last-fetch-date";
		private const string UnreadMailCountKey = "unread-mail-count";
		private const string IsPollingActivatedKey = "is-polling-activated";

		private readonly Func<Configuration> GetConfiguration;
		private readonly PersistanceModel _persistanceModel;
		private readonly Thread _pollingThread;
		private bool _isPolling;

		public PopupIconModel(PersistanceModel persistanceModel)
		{
			_isPolling = true;
			_persistanceModel = persistanceModel;
			GetConfiguration = persistanceModel.LoadConfiguration;
			_pollingThread = new Thread(DoMailPolling) {Name = "Mail Polling Thread", IsBackground = true};
			_pollingThread.Start();

			if (!_persistanceModel.Contains(LastFetchDateKey))
				_persistanceModel.Set(LastFetchDateKey, DateTime.UtcNow);
		}


		public bool IsPollingActive
		{
			get { return _persistanceModel.Get<bool>(IsPollingActivatedKey); }
			set { _persistanceModel.Set(IsPollingActivatedKey, value); }
		}

		public int UnreadMailCount
		{
			get { return _persistanceModel.Get<int>(UnreadMailCountKey); }
			private set { _persistanceModel.Set(UnreadMailCountKey, value); }
		}

		public void Dispose()
		{
			_isPolling = false;
			int pollingInterval = _persistanceModel.LoadConfiguration().PollingInterval;
			_pollingThread.Join(pollingInterval + 10);
			if (_pollingThread.IsAlive)
				try
				{
					_pollingThread.Abort();
				}
				catch
				{
				}
		}

		public event Action<Email> MailReceived;
		public event Action MailServerPolled;
        public event Action MailPollingDisabled;

		public void ResetLastFetchDate()
		{
			_persistanceModel.Set(LastFetchDateKey, new DateTime(1982, 1, 1));
		}

		protected void OnMailServerPolled()
		{
			Action mailServerPolled = MailServerPolled;
			if (mailServerPolled != null)
				mailServerPolled();
		}

		protected void DoMailPolling(object threadState)
		{
			while (_isPolling)
			{
				if (IsPollingActive)
				{
					var fetchedMails = FetchNewMails().ToList();
					
                    var lastFetchDate = _persistanceModel.Get<DateTime>(LastFetchDateKey);
					var relevantFetchedMails = fetchedMails.Where(x => x.Item2.Date.UtcDateTime >= lastFetchDate).ToList();
					if (relevantFetchedMails.Any())
					{
						OnMailReceive(relevantFetchedMails);
						_persistanceModel.Set(LastFetchDateKey, DateTime.UtcNow);
					}
				}

                var configuration = GetConfiguration();
				Thread.Sleep(configuration.PollingInterval);
			}
		}

		private void OnMailReceive(IEnumerable<Tuple<UniqueId, MimeMessage>> fetchedMailsWithUids)
		{
			Action<Email> mailReceivedEvent = MailReceived;
			if (mailReceivedEvent != null)
			{
                foreach (var mailWithUid in fetchedMailsWithUids)
                {
                    var newMail = new Email
                    {
                        From = mailWithUid.Item2.From.First().ToString(),
                        To = mailWithUid.Item2.To.Select(x => x.ToString()).ToArray(),
                        Cc = mailWithUid.Item2.Cc.Select(x => x.ToString()).ToArray(),
                        Subject = mailWithUid.Item2.Subject,
                        WhenSent = mailWithUid.Item2.Date.UtcDateTime,
                        MessageUid = mailWithUid.Item1.Id,
                        HasAttachments = mailWithUid.Item2.BodyParts.Any(x => x.IsAttachment),
                    };

                    var textParts = mailWithUid.Item2.BodyParts.Where(x => x is TextPart).ToList();
                    if(textParts.Count > 0)
                    {
                        if (String.Equals(textParts.First().ContentType.MediaType, "text", StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (textParts.All(part => String.Equals(part.ContentType.MediaSubtype, "plain")))
                                newMail.MimeType = Email.ContentType.PlainText;
                            else if (textParts.Any(part => String.Equals(part.ContentType.MediaSubtype, "html")))
                                newMail.MimeType = Email.ContentType.Html;
                        }

                        if (newMail.MimeType.HasValue)
                        {   
                         
                            if(newMail.MimeType == Email.ContentType.PlainText)
                                newMail.Content = 
                                    textParts.Aggregate(String.Empty, (seed, part) => seed += ((TextPart)part).Text);
                            else if (newMail.MimeType == Email.ContentType.Html)
                                newMail.Content =
                                    textParts.Where(part => part.ContentType.MediaSubtype == "html")
                                             .Aggregate(String.Empty, (seed, part) => seed += ((TextPart)part).Text);
                        }
                    }

                    mailReceivedEvent(newMail);
                }
			}
		}

		protected IEnumerable<Tuple<UniqueId, MimeMessage>> FetchNewMails()
		{
			Configuration configuration = GetConfiguration();
			var credentials = new NetworkCredential(configuration.Username, configuration.Password);
			var results = new List<Tuple<UniqueId, MimeMessage>>();
			try
			{
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
						var unreadMailUids = inbox.Search(SearchQuery.NotSeen, cancel.Token);

                        UnreadMailCount = unreadMailUids.Length;
						OnMailServerPolled();

						if (unreadMailUids != null)
							results.AddRange(unreadMailUids.Select(uid =>
								Tuple.Create(uid, inbox.GetMessage(uid, cancel.Token))));
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show(
					"Mail polling failed. Reason: " + e.Message + ". It will be disabled. Correct the error and re-enable mail polling",
					"Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				IsPollingActive = false;
                if (MailPollingDisabled != null)
                    MailPollingDisabled();
                
                UnreadMailCount = 0;
                OnMailServerPolled();                
			}

			return results;
		}
	}
}