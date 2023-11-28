using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IExtractor<TExtractorModel, TStagingModel>
        where TExtractorModel : class, new()
        where TStagingModel : class, new()
    {
        void ExtractParallel(IExtractorDataQueue<TStagingModel> dataQueue);
    }
}
