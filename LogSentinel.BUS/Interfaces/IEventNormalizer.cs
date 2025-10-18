using System.Collections.Generic;
using System.Threading.Tasks;
using LogSentinel.BUS.Models;

namespace LogSentinel.BUS.Interfaces
{
    public interface IEventNormalizer
    {
        EventDto Normalize(string rawLogLine);
        EventDto NormalizeFromWindowsEvent(System.Diagnostics.Eventing.Reader.EventRecord eventRecord);
        EventDto NormalizeFromSysmonEvent(System.Diagnostics.Eventing.Reader.EventRecord eventRecord);
    }
}
