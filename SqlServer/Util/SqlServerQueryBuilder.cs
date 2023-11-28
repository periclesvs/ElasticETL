using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Util
{
    public class SqlServerQueryBuilder
    {
        public static string GenerateMerge<TSource, TTarget>() where TSource : class where TTarget : class
        {
            StringBuilder builder = new();

            Type sourceType = typeof(TSource);
            Type targetType = typeof(TTarget);

            string sourceClassName = sourceType.Name;
            string targetClassName = targetType.Name;

            List<PropertyInfo> sourcePropList = sourceType.GetProperties().ToList();
            List<PropertyInfo> targetPropList = targetType.GetProperties().ToList();

            List<PropertyInfo> targerRequiredProps = targetPropList.Where(x => x.GetCustomAttribute<RequiredAttribute>() != null).ToList();

            List<PropertyInfo> keys = sourcePropList.Where(x => x.GetCustomAttribute<KeyAttribute>() != null && targetPropList.Any(t => x.Name.Equals(t.Name))).ToList();
            keys.AddRange(targetPropList.Where(x => x.GetCustomAttribute<KeyAttribute>() != null && targetPropList.Any(t => x.Name.Equals(t.Name))).ToList());

            keys = keys.Distinct().ToList();

            List<PropertyInfo> commonProperties = sourcePropList.Where(x => targetPropList.Any(y => y.Name.Equals(x.Name))).ToList();
            List<PropertyInfo> commonPropertiesWithoutKey = commonProperties.Where(x => !keys.Any(y => y.Name.Equals(x.Name))).ToList();

            if (keys.Count == 0)
                throw new MissingPrimaryKeyException();

            bool firstProperty = true;

            builder.AppendLine("MERGE " + targetClassName + " AS tgt");
            builder.AppendLine(" USING (SELECT");

            foreach (PropertyInfo sourceProp in commonProperties)
            {
                builder.AppendLine((firstProperty ? string.Empty : ",") + sourceProp.Name);
                firstProperty = false;
            }

            builder.AppendLine("FROM " + sourceClassName);

            if (targerRequiredProps.Any())
            {
                builder.AppendLine("WHERE");
                firstProperty = true;

                foreach (PropertyInfo item in targerRequiredProps)
                {
                    builder.AppendLine((firstProperty ? string.Empty : "AND ") + item.Name + " IS NOT NULL");
                    firstProperty = false;
                }
            }

            builder.AppendLine(") AS src");

            firstProperty = true;
            foreach (PropertyInfo item in keys)
            {
                builder.AppendLine((firstProperty ? "ON (" : "AND ") + "tgt." + item.Name + " = src." + item.Name);
                firstProperty = false;
            }

            builder.AppendLine(")");
            builder.AppendLine("WHEN MATCHED THEN");
            builder.AppendLine("UPDATE SET");

            firstProperty = true;
            foreach (PropertyInfo item in commonPropertiesWithoutKey)
            {
                builder.AppendLine((firstProperty ? string.Empty : ",") + item.Name + " = src." + item.Name);
                firstProperty = false;
            }

            builder.AppendLine("WHEN NOT MATCHED THEN");
            builder.AppendLine("INSERT (");

            firstProperty = true;
            foreach (PropertyInfo item in commonProperties)
            {
                builder.AppendLine((firstProperty ? string.Empty : ",") + item.Name);
                firstProperty = false;
            }

            builder.AppendLine(") VALUES (");

            firstProperty = true;
            foreach (PropertyInfo item in commonProperties)
            {
                builder.AppendLine((firstProperty ? string.Empty : ",") + "src." + item.Name);
                firstProperty = false;
            }

            builder.AppendLine(");");
            builder.AppendLine("select @@ROWCOUNT");

            return builder.ToString();
        }

        public static string GenerateUpdateFK<T>() where T : class
        {
            StringBuilder builder = new();

            Type type = typeof(T);

            List<PropertyInfo> fksProperties = type.GetProperties().Where(x => x.GetCustomAttributes<FillMeForeignKeyAttribute>().Any()).ToList();

            if (fksProperties == null || fksProperties.Count == 0)
            {
                throw new MissingFieldException("Necessary to have at least one property marked with FillMeForeignKeyAttribute Attribute");
            }

            foreach (PropertyInfo property in fksProperties)
            {
                string fk = property.Name;
                FillMeForeignKeyAttribute? attribute = property.GetCustomAttribute<FillMeForeignKeyAttribute>();

                if (attribute == null) continue;

                if (!string.IsNullOrWhiteSpace(attribute.StoredProcedure))
                {
                    builder.AppendLine("EXEC " + attribute.StoredProcedure);
                    builder.AppendLine();
                    continue;
                }

                builder.AppendLine("UPDATE tgt SET");
                builder.AppendLine(fk + " = src." + attribute.PkColumnName);
                builder.AppendLine("FROM " + type.Name + "tgt");
                builder.AppendLine("JOIN " + attribute.ToTable + " src");
                builder.AppendLine("ON");

                bool firstColumn = true;
                if (attribute.SourceColumns != null)
                    foreach (string column in attribute.SourceColumns)
                    {
                        builder.AppendLine((firstColumn ? string.Empty : "AND ") + "tgt." + column + " = src." + column);
                        firstColumn = false;
                    }

                builder.AppendLine();
                builder.AppendLine("select @@ROWCOUNT");


            }

            return builder.ToString();

        }
    }
}
