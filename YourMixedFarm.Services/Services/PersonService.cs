using AutoMapper;
using Common;
using Common.Interfaces;
using Oracle;
using Process;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YourMixedFarm.Models.Extraction;
using YourMixedFarm.Models.Final;
using YourMixedFarm.Models.Staging;
using YourMixedFarm.Services.QueryExtractors;

namespace YourMixedFarm.Services.Services
{
    public class PersonService : BaseService<PersonStaging, Person>
    {
        public PersonService(ILogger logger, IMapper mapper, EtlProcessWatcher etlProcessWatcher, IFinal final) : base(logger, mapper, etlProcessWatcher, final)
        {
        }

        public void ProcessPersonParallel()
        {
            try
            {
                DoProcessParallel(new PersonExtractor());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ProcessPersonParallel(int cdArea)
        {
            try
            {
                DoProcessParallel(new PersonExtractor(cdArea));
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void DoProcessParallel(PersonExtractor personExtractor)
        {
            try
            {
                IExtractor<PersonExtraction, PersonStaging> extractor = new Extractor<PersonExtraction, PersonStaging>(logger, mapper, etlProcessWatcher, personExtractor);

                ParallelService<PersonExtraction, PersonStaging, Person> service = new(logger, extractor, staging, final, new ExtractorDataQueue<PersonStaging>(), etlProcessWatcher);

                service.ProcessParallel();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
