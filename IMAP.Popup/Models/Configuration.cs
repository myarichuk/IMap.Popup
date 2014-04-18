using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace IMAP.Popup.Models
{
    public enum ImapAuthentication
    {
        NonEncrypted,
        TLS,
        SSL
    }

    public class MailHighlightRule
    {
        [DisplayName(@"From field Regex")]
        public string FromRegex { get; set; }

        [DisplayName(@"Subject field Regex")]
        public string SubjectRegex { get; set; }

        [DisplayName(@"Highlight Color")]
        public Color HighlightColor { get; set; }
    }

    public class Configuration
    {
        [Category("IMAP Server Configuration")]        
        public string Username { get; set; }

        [Category("IMAP Server Configuration")]
        [PasswordPropertyText(true)]
        public string Password { get; set; }

        [DisplayName(@"IMAP Server Hostname")]
        [Category("IMAP Server Configuration")]
        public string ImapServer { get; set; }

        [DisplayName(@"IMAP Port")]
        [DefaultValue(993)]
        [Category("IMAP Server Configuration")]
        public uint ImapPort { get; set; }

        private ImapAuthentication _authentication;

        [DisplayName(@"IMAP Authentication")]
        [Category("IMAP Server Configuration")]
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


        [DisplayName(@"Polling Interval (ms)")]
        [Description("Polling interval for checking new mail")]
        [Category("IMAP Server Configuration")]
        [DefaultValue(1000)]
        public int PollingInterval { get; set; }

        [DisplayName(@"Popup Delay (ms)")]
        [Description("How much time the email popup will remain visible")]
        [DefaultValue(4000)]
        [Category("Appearance")]
        public int PopupDelay { get; set; }

        [DisplayName(@"Highlighting Rules")]
        [Category("Appearance")]
        public List<MailHighlightRule> HighlightRules { get; set; }

        [DisplayName("Remind later delays")]
        [Description("Possible intervals for the mail reminder to appear")]
        [Category("General Configuration")]
        public List<long> RemindMeTimespans { get; set; }
    }
}
