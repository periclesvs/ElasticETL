using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces
{
    public interface IStaging<TStagingModel>
        where TStagingModel : class, new()
    {
        void InsertParallel(IExtractorDataQueue<TStagingModel> dataQueue);
        int UpdateForeignKey();
    }
}
