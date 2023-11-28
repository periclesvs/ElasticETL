using Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YourMixedFarm.Services.QueryExtractors
{
    public class PersonExtractor : IEntitySqlExtractor
    {
        private readonly string _query;

        private const string MAIN_QUERY = @"SELECT 
                                                 Id
                                                ,CdArea
                                                ,Name
                                                ,Active
                                                ,LastUpdate
                                            FROM Person";


        public PersonExtractor()
        {
            _query = MAIN_QUERY;
        }

        public PersonExtractor(int cdArea)
        {
            _query = MAIN_QUERY +
            Environment.NewLine +
            "WHERE CdArea = " + cdArea;
        }

        public string GetSqlQuery()
        {
            return _query;
        }


    }
}
