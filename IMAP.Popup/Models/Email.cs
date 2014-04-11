using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
