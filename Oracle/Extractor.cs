using AutoMapper;
using Common;
using Common.Interfaces;
using Serilog;

namespace Oracle
{
    public class Extractor<TExtractorModel, TStagingModel> : IExtractor<TExtractorModel, TStagingModel>
        where TExtractorModel : class, new()
        where TStagingModel : class, new()
    {
        private readonly ILogger logger;
        private readonly IMapper mapper;

        private readonly EtlProcessWatcher etlProcessWatcher;
        private readonly IEntitySqlExtractor entitySqlExtractor;

        public Extractor(ILogger logger, IMapper mapper, EtlProcessWatcher etlProcessWatcher, IEntitySqlExtractor entitySqlExtractor)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.etlProcessWatcher = etlProcessWatcher;
            this.entitySqlExtractor = entitySqlExtractor;
        }

        public void ExtractParallel(IExtractorDataQueue<TStagingModel> dataQueue)
        {
            using OracleDatabaseContext context = new(logger, etlProcessWatcher);
            context.ExtractParallel<TExtractorModel, TStagingModel>(entitySqlExtractor, dataQueue, mapper);
        }
    }
}