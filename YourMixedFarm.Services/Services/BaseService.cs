using Common.Interfaces;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using AutoMapper;
using SqlServer;

namespace YourMixedFarm.Services.Services
{
    public abstract class BaseService<TStagingModel, TFinalModel>
        where TStagingModel : class, new() 
        where TFinalModel : class, new()
    {
        protected readonly ILogger logger;
        protected readonly IMapper mapper;

        protected readonly EtlProcessWatcher etlProcessWatcher;
        protected readonly IStaging<TStagingModel> staging;
        protected readonly IFinal final;

        protected BaseService(ILogger logger, IMapper mapper, EtlProcessWatcher etlProcessWatcher, IFinal final)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.etlProcessWatcher = etlProcessWatcher;
            this.staging = new Staging<TStagingModel>(logger, etlProcessWatcher);
            this.final = final;
        }
    }
}
