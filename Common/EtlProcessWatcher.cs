using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class EtlProcessWatcher
    {
        public Guid Id { get; set; }
        public string? CustomInformation { get; set; }
        public int? ProcessThreadId { get; set; }
        public int? ReaderThreadingId { get; set; }
        public int? StagingThreadId { get; set; }
        public int ExtractionRowCount { get; set; }
        public int StagingRowCount { get; set; }
        public int FkUpdatedRowCount { get; set; }
        public int FinalRowCount { get; set; }
        public ProcessStep ProcessStep { get; set; }
        public ProcessStep ParallelStagingProcess { get; set; }
        public ProcessStep ParallelExtractionProcess { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
