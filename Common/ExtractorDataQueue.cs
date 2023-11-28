using Common.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ExtractorDataQueue<T> : IExtractorDataQueue<T> where T : class, new()
    {
        public ConcurrentBag<T> Bag { get; set; } = new ConcurrentBag<T>();
        public bool? IsExtractorProcessing { get; set; }
        public bool? IsStaginProcessing { get; set; }
    }
}
