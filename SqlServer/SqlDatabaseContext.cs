using Common;
using Common.Interfaces;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.SqlClient;
using Serilog;
using SqlServer.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Z.Dapper.Plus;

namespace SqlServer
{
    internal class SqlDatabaseContext
    {
        private static readonly string strConnection = "";

        private readonly EtlProcessWatcher etlProcessWatcher;
        private readonly ILogger log;

        public SqlDatabaseContext(ILogger logger, EtlProcessWatcher etlProcessWatcher)
        {
            this.etlProcessWatcher = etlProcessWatcher;
            this.log = logger;
        }

        internal static SqlConnection GetConnection()
        {
            return new SqlConnection(strConnection);
        }

        internal void DeleteAll<T>() where T : class, new()
        {
            try
            {
                log.Information("Getting Database Connection");
                using var connection = GetConnection();
                log.Information("Delete all started");
                connection.DeleteAll<T>();
                log.Information("Delete All finished");
            }
            catch (Exception ex)
            {
                log.Error("Error in Delete All: {0}", ex.ToString());
                throw;
            }
        }

        internal void BulkInsertParallel<T>(IExtractorDataQueue<T> queue) where T : class, new()
        {
            if (queue == null)
            {
                log.Error("The shared queue is null");
                throw new ArgumentNullException(nameof(queue));
            }

            try
            {
                while (!queue.IsExtractorProcessing.HasValue)
                    Thread.Sleep(50);

                queue.IsStaginProcessing = true;
                log.Information("Staging Process started");

                etlProcessWatcher.ParallelStagingProcess = ProcessStep.InsertingStaging;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                T? model;
                log.Information("Getting Database Connection");
                using var connection = GetConnection();

                while (queue.IsExtractorProcessing.Value || !queue.Bag.IsEmpty)
                {
                    List<T> list = new();

                    while (list.Count <= 10000 && !queue.Bag.IsEmpty)
                    {
                        while (!queue.Bag.TryTake(out model))
                            Thread.Sleep(5);

                        if (model != null)
                            list.Add(model);
                    }

                    if (list.Any())
                    {
                        log.Verbose("Bulk Insert -> lines: {0}", list.Count);
                        connection.BulkInsert(list);
                        etlProcessWatcher.StagingRowCount += list.Count;
                        etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                        Log.Verbose("End of bulk insert");
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                queue.IsStaginProcessing = false;
                log.Information("Staging Process ended");
            }
            catch (Exception ex)
            {
                log.Error("Error in Bulk Insert Parallel: {0}", ex.ToString());
                throw;
            }
        }

        internal int UpdateForeignKeys<T>() where T : class, new()
        {
            try
            {
                log.Information("Building update FK query");
                string query;

                try
                {
                    query = SqlServerQueryBuilder.GenerateUpdateFK<T>();
                }
                catch (MissingFieldException)
                {
                    log.Information("No Foreign Key in model to update");
                    return 0;
                }
                catch (Exception)
                {
                    throw;
                }

                log.Information("Getting Database Connection");
                using var connection = GetConnection();

                log.Information("Executing update foreign keys");
                int rowCount = connection.Execute(query);
                log.Information("Update foreign key executed - rows affected: {0}", rowCount);

                return rowCount;
            }
            catch (Exception ex)
            {
                log.Error("Error updating foreign keys in staging: {0}", ex.ToString());
                throw;
            }
        }

        internal int StagingToFinal<TStaging, TFinal>()
            where TStaging : class, new()
            where TFinal : class, new()
        {
            try
            {
                log.Information("Building the merge query");
                string query = SqlServerQueryBuilder.GenerateMerge<TStaging, TFinal>();

                log.Information("Getting database connection");
                using var connection = GetConnection();

                log.Information("Executing merge query");
                int rowCount = connection.Execute(query, commandTimeout: 0);
                log.Information("Merge query executed -> rows affected: {0}", rowCount);

                return rowCount;
            }
            catch (Exception ex)
            {
                log.Error("Error in Staging to Final: {0}", ex.ToString());
                throw;
            }
        }
    }
}
