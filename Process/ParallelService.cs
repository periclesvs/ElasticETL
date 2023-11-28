using Common;
using Common.Interfaces;
using Serilog;

namespace Process
{
    public class ParallelService<TExtractorModel, TStagingModel, TFinalModel>
        where TExtractorModel : class, new()
        where TStagingModel : class, new()
        where TFinalModel : class, new()
    {
        protected readonly ILogger logger;
        private readonly IExtractor<TExtractorModel, TStagingModel> extractor;
        private readonly IStaging<TStagingModel> staging;
        private readonly IFinal final;
        private readonly IExtractorDataQueue<TStagingModel> dataQueue;
        private readonly EtlProcessWatcher etlProcessWatcher;

        public ParallelService(
            ILogger logger,
            IExtractor<TExtractorModel, TStagingModel> extractor,
            IStaging<TStagingModel> staging,
            IFinal final,
            IExtractorDataQueue<TStagingModel> dataQueue,
            EtlProcessWatcher etlProcessWatcher
        )
        {
            this.logger = logger;
            this.extractor = extractor;
            this.staging = staging;
            this.final = final;
            this.dataQueue = dataQueue;
            this.etlProcessWatcher = etlProcessWatcher;
        }

        public virtual void ProcessParallel()
        {
            try
            {
                etlProcessWatcher.StartedAt = DateTime.Now;

                logger.Information($"Parallel Process for Extractor: {typeof(TExtractorModel).Name}, Staging: {typeof(TStagingModel).Name}, Final: {typeof(TFinalModel).Name}");

                Thread threadReader = new(() => { extractor.ExtractParallel(dataQueue); });
                logger.Information("Extraction Thread created");

                Thread threadStaging = new(() => { staging.InsertParallel(dataQueue); });
                logger.Information("Staging Thread created");

                threadReader.Start();
                logger.Information("Extraction Thread started");
                threadStaging.Start();
                logger.Information("Staging Thread started");

                etlProcessWatcher.ReaderThreadingId = threadReader.ManagedThreadId;
                etlProcessWatcher.StagingThreadId = threadStaging.ManagedThreadId;

                while (!dataQueue.IsStaginProcessing.HasValue)
                {
                    Thread.Sleep(10);
                }

                while (dataQueue.IsStaginProcessing.Value)
                {
                    Thread.Sleep(10);
                }

                UpdateStagingAndMoveFinal();

                etlProcessWatcher.ProcessStep = ProcessStep.Finished;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                etlProcessWatcher.FinishedAt = DateTime.Now;
                logger.Information("Process Finished");

            }
            catch (Exception ex)
            {
                logger.Error($"Error executing Parallel Process: {ex}");
                throw;
            }
        }

        private void UpdateStagingAndMoveFinal()
        {
            try
            {
                logger.Information("FK Process");
                UpdateForeignKeysStaging();

                logger.Information("Staging to Final Process");
                StagingToFinal();
            }
            catch (Exception)
            {
                throw;
            }
        }

        private int StagingToFinal()
        {
            try
            {
                logger.Information("Moving data to Final started");
                int rowCount = final.StagingToFinal();
                logger.Information("Moving data to Final finished");
                return rowCount;
            }
            catch (Exception ex)
            {
                logger.Error("Error moving to final: " + ex.ToString());
                throw;
            }
        }

        private int UpdateForeignKeysStaging()
        {
            try
            {
                logger.Information("Foreign Key update Started");
                int rowCount = staging.UpdateForeignKey();
                logger.Information("Foreign keys updated");
                return rowCount;
            }
            catch (Exception ex)
            {
                logger.Error("Error updating Foreign Keys: " + ex.ToString());
                throw;
            }
        }
    }
}