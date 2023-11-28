using Common;
using Common.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer
{
    public class Staging<TStagingModel> : IStaging<TStagingModel> where TStagingModel : class, new()
    {
        private readonly ILogger logger;
        private readonly EtlProcessWatcher etlProcessWatcher;
        private readonly SqlDatabaseContext context;

        public Staging(ILogger logger, EtlProcessWatcher etlProcessWatcher)
        {
            this.logger = logger;
            this.etlProcessWatcher = etlProcessWatcher;
            context = new SqlDatabaseContext(logger, etlProcessWatcher);
        }


        public void InsertParallel(IExtractorDataQueue<TStagingModel> dataQueue)
        {
            try
            {
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.ProcessStep = ProcessStep.DeletingStaging;

                logger.Information("Cleaning staging database");
                context.DeleteAll<TStagingModel>();

                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.ProcessStep = ProcessStep.ParallelExecution;

                logger.Information("Starting Parallel Insert Process");
                context.BulkInsertParallel(dataQueue);
                logger.Information("End of Parallel Insert Process");
            }
            catch (Exception ex)
            {
                logger.Error("Error executing Insert Parallel: {0}", ex.ToString());
                throw;
            }
        }

        public int UpdateForeignKey()
        {
            try
            {
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.ParallelStagingProcess = ProcessStep.UpdatingForeignKeys;

                logger.Information("Updating Foreign Keys");
                int rowCount = context.UpdateForeignKeys<TStagingModel>();
                logger.Information("Update Foreign Keys finished");

                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.ParallelStagingProcess = ProcessStep.Finished;
                etlProcessWatcher.FkUpdatedRowCount = rowCount;

                return rowCount;
            }
            catch (Exception ex)
            {
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.ParallelStagingProcess= ProcessStep.Error;
                logger.Error("Error updating foreign keys: {0}", ex.ToString());
                throw;
            }
        }
    }
}
