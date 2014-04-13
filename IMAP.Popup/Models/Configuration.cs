using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace IMAP.Popup
{
    public enum ImapAuthentication
    {
        NonEncrypted,
        TLS,
        SSL
    }

    public class MailHighlightRule
    {
        [DisplayName("From field Regex")]
        public string FromRegex { get; set; }

        [DisplayName("Subject field Regex")]
        public string SubjectRegex { get; set; }

        [DisplayName("Highlight Color")]
        public Color HighlightColor { get; set; }
    }

    public class Configuration
    {
        public string Username { get; set; }

        public string Password { get; set; }

        [DisplayName("IMAP Server Hostname")]
        public string ImapServer { get; set; }

        [DisplayName("IMAP Port")]
        public uint ImapPort { get; set; }

        private ImapAuthentication _authentication;
        [DisplayName("IMAP Authentication")]
        public ImapAuthentication Authentication 
        {
            get
            {
                return _authentication;
            }
            set
            {
                if (ImapPort == 0)
                    ImapPort = (uint) (value == ImapAuthentication.SSL ? 993 : 143);
                
				_authentication = value;
            }
        }

        [DisplayName("Highlighting Rules")]
        public List<MailHighlightRule> HighlightRules { get; set; }

        [DisplayName("Popup Delay (ms)")]
        public int PopupDelay { get; set; }

        [DisplayName("Polling Interval (ms)")]
        public int PollingInterval { get; set; }

        public Configuration()
        {
	        ImapServer = String.Empty;
            ImapPort = 143;
            PopupDelay = 4000; //default delay
            PollingInterval = 1000; //default polling latency
        }
    }
}
