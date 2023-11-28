using Common;
using Common.Interfaces;
using Serilog;

namespace SqlServer
{
    public class Final<TStaging, TFinal> : IFinal
        where TStaging : class, new()
        where TFinal : class, new()
    {
        private readonly ILogger logger;
        private readonly EtlProcessWatcher etlProcessWatcher;

        public Final(ILogger logger, EtlProcessWatcher etlProcessWatcher)
        {
            this.logger = logger;
            this.etlProcessWatcher = etlProcessWatcher;
        }


        public int StagingToFinal()
        {
            try
            {
                etlProcessWatcher.ProcessStep = ProcessStep.InsertingStaging;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                int rowCount = new SqlDatabaseContext(logger, etlProcessWatcher).StagingToFinal<TStaging, TFinal>();

                etlProcessWatcher.FinalRowCount = rowCount;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                return rowCount;
            }
            catch (Exception ex)
            {
                logger.Error("Error in Staging to Final: {0}", ex.ToString());
                throw;
            }
        }
    }
}