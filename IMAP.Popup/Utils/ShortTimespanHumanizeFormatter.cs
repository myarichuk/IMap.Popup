using Humanizer.Localisation;
using Humanizer.Localisation.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMAP.Popup.Utils
{
    //this a dirty hack, just to make the project work
    // TODO : rewrite this as PR to Humanizer project --> make short time descriptions a convention
    public class ShortTimespanHumanizeFormatter : DefaultFormatter
    {
        public override string TimeSpanHumanize(TimeUnit timeUnit, int unit)
        {
            return GetResourceForTimeSpan(timeUnit, unit);
        }

        private string GetResourceForTimeSpan(TimeUnit unit, int count)
        {
            string resourceKey = ResourceKeys.TimeSpanHumanize.GetResourceKey(unit, count);
            return ShortenValue((count == 1 ? Format(resourceKey) : Format(resourceKey, count)), unit);
        }

        private string ShortenValue(string value, TimeUnit timeUnit)
        {
            var valueAsParts = value.Split(' ');
            if (valueAsParts.Length != 2)
                throw new InvalidOperationException("something strange in humanized form of time span...could not parse");

            var timeValue = valueAsParts[0];
            var timeDescription = valueAsParts[1];

            switch(timeUnit)
            {
                case TimeUnit.Millisecond:
                    return String.Format("{0} ms", timeValue);
                case TimeUnit.Minute:
                case TimeUnit.Second:
                    return String.Format("{0} {1}", timeValue, timeDescription.Substring(0, 3));
                default:
                    return String.Format("{0} {1}",timeValue,timeDescription.Substring(0,1));
            }
        }
    }
}
