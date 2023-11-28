using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum ProcessStep
    {
        None,
        Extracting,
        DeletingStaging,
        InsertingStaging,
        UpdatingForeignKeys,
        InsertingFinal,

        ParallelExecution,

        Finished,
        Cancelled,
        Error
    }
}
