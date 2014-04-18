using MailKit;
using MailKit.Net.Imap;
using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Windows;

namespace IMAP.Popup.Models
{
    public class EmailModel
    {
        private readonly PersistanceModel _persistanceModel;

        public EmailModel(PersistanceModel persistanceModel)
        {
            _persistanceModel = persistanceModel;
        }

        public void MarkAsRead(uint emailUid)
        {
            var configuration = _persistanceModel.LoadConfiguration();
			var credentials = new NetworkCredential(configuration.Username, configuration.Password);
            var uid = new UniqueId(emailUid);
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

                        inbox.Open(FolderAccess.ReadWrite, cancel.Token);
                        
                        var messagesSummary = inbox.Fetch(new UniqueId[]{ uid },MessageSummaryItems.Fast,cancel.Token);
                        if (messagesSummary != null && messagesSummary.Any())
                            inbox.SetFlags(new UniqueId[] { uid }, MessageFlags.Seen, true, cancel.Token);
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to mark email as read. Reason: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
