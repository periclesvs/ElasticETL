using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FillMeForeignKeyAttribute: Attribute
    {
        private readonly string? storedProcedure;
        private readonly string[]? sourceColumns;
        private readonly string? toTable;
        private readonly string? pkColumnName;
                               
        public FillMeForeignKeyAttribute(string storedProcedure)
        {
            this.storedProcedure = storedProcedure;
        }

        public FillMeForeignKeyAttribute(string[] sourceColumns, string toTable, string pkColumnName)
        {
            this.sourceColumns = sourceColumns;
            this.toTable = toTable;
            this.pkColumnName = pkColumnName;
        }

        public string? StoredProcedure => storedProcedure;
        public string[]? SourceColumns => sourceColumns;
        public string? ToTable => toTable;
        public string? PkColumnName => pkColumnName;
    }
}
