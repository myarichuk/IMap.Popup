using System;

namespace IMAP.Popup.Models
{
    public class Email
    {
        public string From { get; set; }
        public string[] To { get; set; }
        public string[] Cc { get; set; }

        public string Subject { get; set; }

        public DateTime WhenSent { get; set; }

        public uint MessageUid { get; set; }

        public bool HasAttachments { get; set; }

        public ContentType? MimeType { get; set; }

        public string Content { get; set; }

        public enum ContentType
        {
            PlainText,
            Html
        }

    }
}
