using AutoMapper;
using AutoMapper.Execution;
using Common;
using Common.Interfaces;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using Serilog;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Oracle
{
    internal class OracleDatabaseContext : IDisposable
    {

        const string _connectionString = "";
        private readonly OracleConnection _connection;
        private readonly ILogger logger;
        private readonly EtlProcessWatcher etlProcessWatcher;

        public OracleDatabaseContext(ILogger logger, EtlProcessWatcher etlProcessWatcher)
        {
            _connection = new OracleConnection(_connectionString);
            this.logger = logger;
            this.etlProcessWatcher = etlProcessWatcher;
        }

        private OracleDataReader ExecuteReader(IEntitySqlExtractor extractor)
        {
            try
            {
                logger.Information("Retrieving Oracle Command");
                OracleCommand command = new(extractor.GetSqlQuery(), _connection);

                logger.Information("Opening Oracle Connection");
                _connection.Open();

                logger.Information("Executing Oracle Reader");
                OracleDataReader reader = command.ExecuteReader();

                return reader;
            }
            catch (Exception ex)
            {
                logger.Error("Error executing Oracle Reader: {0}", ex.ToString());
                throw;
            }
        }

        public void ExtractParallel<TExtractorModel, TStagingModel>(IEntitySqlExtractor extractor, IExtractorDataQueue<TStagingModel> queue, IMapper mapper)
            where TExtractorModel : class, new()
            where TStagingModel : class, new()
        {
            try
            {
                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Extracting;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                OracleDataReader reader = ExecuteReader(extractor);

                logger.Information("Starting reader process");
                queue.IsExtractorProcessing = true;

                ParseCollectionParallel<TExtractorModel, TStagingModel>(reader, queue, mapper);

                queue.IsExtractorProcessing = false;
                logger.Information("End of reader process");

                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Finished;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                _connection.Close();
            }
            catch (Exception ex)
            {
                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Error;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                logger.Error("Error in Parallel Extraction: {0}", ex.ToString());

                throw;
            }
        }

        private void ParseCollectionParallel<TExtractorModel, TStagingModel>(OracleDataReader reader, IExtractorDataQueue<TStagingModel> queue, IMapper mapper)
            where TExtractorModel : class, new()
            where TStagingModel : class, new()
        {
            if (queue == null)
            {
                logger.Error("Queue not initialized");
                throw new ArgumentNullException(nameof(queue));
            }

            try
            {
                logger.Information("Mapping Columns <> Properties");
                Dictionary<string, int> ordinals = ReadOrdinals<TExtractorModel>(reader);

                logger.Information("Reading data");
                int count = 0;
                bool processing = true;

                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Extracting;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                Thread counter = new(() =>
                {
                    while (processing)
                    {
                        etlProcessWatcher.ExtractionRowCount = count;
                        etlProcessWatcher.LastUpdatedAt = DateTime.Now;
                        Thread.Sleep(1000);
                    }
                });

                counter.Start();

                while (reader.Read())
                {
                    TExtractorModel? obj = ReadObject<TExtractorModel>(reader, ordinals);
                    if (obj != null)
                    {
                        TStagingModel model = mapper.Map<TStagingModel>(obj);

                        queue.Bag.Add(model);
                        count++;
                    }
                }

                processing = false;
                etlProcessWatcher.ExtractionRowCount = count;
                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Finished;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                logger.Information("End of reading data");
            }
            catch (Exception ex)
            {
                etlProcessWatcher.ParallelExtractionProcess = ProcessStep.Error;
                etlProcessWatcher.LastUpdatedAt = DateTime.Now;

                logger.Error("Error Parsing data reader in parallel process: {0}", ex.ToString());
                throw;
            }
        }

        private TExtractorModel? ReadObject<TExtractorModel>(OracleDataReader reader, Dictionary<string, int> ordinals) where TExtractorModel : class, new()
        {
            TExtractorModel? result = default;

            Type type = typeof(TExtractorModel);

            if (type != null && ordinals != null)
            {
                result = new TExtractorModel();

                foreach (PropertyInfo pi in type.GetProperties())
                {
                    if (ordinals.TryGetValue(pi.Name, out int value))
                    {
                        object? rawValue = null;

                        try
                        {
                            Type? propertyType = pi.PropertyType ?? throw new InvalidOperationException($"Problem reading property type in {type.Name}, property name: {pi.Name}");
                            bool isNullable = false;

                            if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                propertyType = Nullable.GetUnderlyingType(propertyType);
                                isNullable = true;
                            }

                            if (reader.IsDBNull(value))
                            {
                                if (!isNullable)
                                    throw new InvalidOperationException($"Property {pi.Name} is not null and the value to assign is null");

                                rawValue = null;
                            }
                            else if (propertyType == typeof(decimal))
                            {
                                if (reader is OracleDataReader oracleDataReader)
                                    rawValue = OracleDecimal.SetPrecision(oracleDataReader.GetOracleDecimal(value), 28);
                                else
                                    rawValue = reader.GetDecimal(value);
                            }
                            else if (propertyType == typeof(double))
                                rawValue = reader.GetDouble(value);
                            else
                                rawValue = reader.GetValue(value);

                            object? convertedValue = null;
                            if (rawValue != null)
                                convertedValue = ChangeType(propertyType, rawValue);

                            pi.SetValue(result, convertedValue);

                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                rawValue ??= reader.GetValue(value);
                            }
                            catch (Exception)
                            {
                                // Only to try to get the value to log
                            }
                            throw new InvalidOperationException($"Invalid extraction fo property {pi.Name} from type {type.Name}. Value received: {rawValue}. Error {ex}");
                        }
                    }
                }
            }

            if (result == null)
                logger.Warning($"An object of type {type?.Name} could not be created from the result set");

            return result;
        }

        private object? ChangeType(Type? propertyType, object rawValue)
        {
            object? convertedValue = null;

            if (propertyType == null)
                return convertedValue;

            if (rawValue != null)
            {
                string? val = rawValue.ToString();

                if (val == null)
                    convertedValue = null;
                else if (propertyType == typeof(string))
                    convertedValue = val;
                else if (propertyType == typeof(DateTime))
                    convertedValue = DateTime.Parse(val);
                else if (propertyType == typeof(decimal))
                    convertedValue = decimal.Parse(val);
                else if (propertyType == typeof(char))
                    convertedValue = char.Parse(val);
                else if (propertyType == typeof(bool))
                    convertedValue = bool.Parse(val);
                else if (propertyType == typeof(int))
                    convertedValue = int.Parse(val);
                else if (propertyType == typeof(long))
                    convertedValue = long.Parse(val);
                else if (propertyType == typeof(float))
                    convertedValue = float.Parse(val);
                else if (propertyType == typeof(double))
                    convertedValue = double.Parse(val);
                else if (propertyType == typeof(TimeSpan))
                    convertedValue = TimeSpan.Parse(val);
                else
                {
                    logger.Warning($"The type {propertyType.Name} has no created convert method. Using default type change method...");
                    convertedValue = Convert.ChangeType(rawValue, propertyType);
                }
            }
            return convertedValue;

        }

        private Dictionary<string, int> ReadOrdinals<TExtractorModel>(OracleDataReader reader) where TExtractorModel : class, new()
        {
            Dictionary<string, int> ordinals = new();

            Type type = typeof(TExtractorModel);
            ICustomTypeDescriptor? typeDescriptor = TypeDescriptor.GetProvider(type).GetTypeDescriptor(type);

            if (typeDescriptor != null)
            {
                foreach (PropertyDescriptor pi in typeDescriptor.GetProperties())
                {
                    try
                    {
                        int ordinal = reader.GetOrdinal(pi.Name);
                        if (ordinal >= 0)
                        {
                            ordinals.Add(pi.Name, ordinal);
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        logger.Warning("Column name '{0}' was not found in result ser for type '{1}'.", pi.Name, type.Name);
                    }
                }
            }

            return ordinals;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
