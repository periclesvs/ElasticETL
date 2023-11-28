using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IExtractorDataQueue<T> where T : class, new()
    {
        ConcurrentBag<T> Bag { get; set; }
        bool? IsExtractorProcessing { get; set; }
        bool? IsStaginProcessing { get; set; }
    }
}
