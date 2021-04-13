/*
 * SQL Server requires parameters to be named, but does at least expose the parameter names through a metadata 
 * collection. The ODBC version does not need parameter names because ODBC parameters are always positional.
 */

using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class SQLServer : DriverBase
    {
        [Ident(Providers.SQLServer, ProductName="Microsoft SQL Server", ProviderInvariantName="System.Data.Odbc")]
        public SQLServer()
        {
            TimespanFormat = "DATEADD({3}, {1}{2}, {0})";
            TimespanExpressions.Add(new Regex("DATEADD\\s*\\((?<U>"+UNIT+")S?\\s*,\\s*(?<S>[-+])?\\s*(?<N>[^\\s,]+)\\s*,\\s*(?<B>"+NT+")\\s*\\)", OPT));

            LimitFormat = "SELECT TOP {1} {0}";
            LimitExpressions.Add(new Regex("SELECT\\s+TOP\\s+(?<N>"+N+")(?<Q>[^;]+)", OPT));

            rePrimaryKeyException       = new Regex("PRIMARY KEY [^.]+.* duplicate .* '(?<NAME>.+?)'",    RegexOptions.IgnoreCase);
            reNotUniqueException        = new Regex("UNIQUE KEY [^.].* duplicate .* '(?<NAME>.+?)'",      RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex("invalid column name '(?<NAME>.*?)'",                 RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex("invalid object name '(?<NAME>.*?)'",                 RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex("could not find stored procedure '(?<NAME>.*?)'",     RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex("FOREIGN KEY constraint.*table \"(?<NAME>.+?)\"",     RegexOptions.IgnoreCase);
            reNotNullException          = new Regex("column '(?<NAME>.+?)'.*column does not allow nulls", RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex("constraint.*table \"(?<NAME>.+?)\"",                 RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex("permission was denied on the object '(?<NAME>.+?)'", RegexOptions.IgnoreCase);
        }

        [Ident(Providers.SQLServer, ProductName="Microsoft SQL Server", ProviderInvariantName="System.Data.SqlClient")]
        public SQLServer(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            DateTimeType = DbType.DateTime2;
            BackgroundProcParamsNeeded(ConnectionString, () =>
            {
                using (var con = Factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    con.Open();
                    ProcParamsCache[ConnectionString] = con.GetSchema("ProcedureParameters");
                }
            });
            MetaProcedureName   = "specific_name";
            MetaProcedureSchema = "specific_schema";
            MetaParameterName   = "parameter_name";
            MetaOrdinalPosition = "ordinal_position";
        }
    }
}