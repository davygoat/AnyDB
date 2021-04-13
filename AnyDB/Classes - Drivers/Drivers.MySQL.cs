/*
 * Like SQL Server, MySQL insists that parameters be named.
 * 
 * But MySQL has an added complication in that, by default, it does not expose the procedures and parameters through
 * the "Procedure Parameters" collection because by default the mysql.proc table is not readable. That being the case, 
 * you end up with an empty metadata collection. You can, however, query the information_schema.parameters table to
 * get the same information.
 * 
 * At some point the data factory's CreateDataAdapter() method seems to have stopped working - it always returns null.
 * The assembly also has to be registered with an "unusual" class namespace.
 * 
 * Dependencies :-
 * 
 * - MySql.Data.dll
 */

using System;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    [RegisterProvider("MySql.Data.MySqlClient", FactoryClass="MySql.Data.MySqlClient.MySqlClientFactory", ClassNamespace="MySql.Data")]
    class MySQL : DriverBase
    {
        [Ident(Providers.MySQL, ProductName="MySQL", ProviderInvariantName="System.Data.Odbc")]
        public MySQL()
        {
            TimespanFormat = "{0} {1} INTERVAL '{2}' {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*(?<N>"+qNT+")\\s+(?<U>"+UNIT+")S?", OPT));

            LimitFormat = "SELECT {0} LIMIT {1}";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+(?<N>"+N+")", OPT));

            HasCheckException           = false;
            HasInvalidTableException    = false;

            rePrimaryKeyException       = new Regex("Duplicate entry '.+?' for key '(?<NAME>PRIMARY)'",        RegexOptions.IgnoreCase);
            reNotUniqueException        = new Regex("Duplicate entry '.+?' for key '(?<NAME>.+?)'",            RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex("Unknown column '(?<NAME>.+?)'",                           RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex("Procedure or function '`(?<NAME>.+?)`' cannot be found",  RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex(@"foreign key constraint .*?\.`(?<NAME>.+?)`, CONSTRAINT", RegexOptions.IgnoreCase);
            reNotNullException          = new Regex("Column '(?<NAME>.+?)' cannot be null",                    RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex("command denied .* for .* '(?<NAME>.+)'",                  RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex(">>> CHECK CONSTRAINTS IGNORED <<<");
            reInvalidTableException     = new Regex(">>> PRESENTS AS ACCESS DENIED <<<");

            OnConnect.Add("SET SQL_MODE='STRICT_TRANS_TABLES'"); // for transactions
        }

        [Ident(Providers.MySQL, ProductName="MySQL", ProviderInvariantName="MySql.Data.MySqlClient")]
        public MySQL(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            var asm = Assembly.GetAssembly(Factory.GetType());
            DataAdapterType = asm.GetType("MySql.Data.MySqlClient.MySqlDataAdapter");

            MetaProcedureName = "specific_name";
            MetaParameterName = "parameter_name";
            MetaOrdinalPosition = "ordinal_position";

            QuirkParameterNamesRequired = dtProcParams == null || dtProcParams.Rows.Count == 0;
            FactoryDataAdapterIsBroken = Factory.CreateDataAdapter() == null;

            BackgroundProcParamsNeeded(ConnectionString, () =>
            {
                try
                {
                    using (var con = Factory.CreateConnection())
                    {
                        con.ConnectionString = ConnectionString;
                        con.Open();
                        dtProcParams = con.GetSchema("Procedure Parameters");
                        if (dtProcParams.Rows.Count == 0)
                        {
                            dtProcParams = GetDataTableUsingConnection(@"SELECT   *
                                                                         FROM     information_schema.parameters
                                                                         ORDER BY specific_schema, specific_name, ordinal_position",
                                                                         con);
                        }
                        ProcParamsCache[ConnectionString] = dtProcParams;
                    }
                    QuirkParameterNamesRequired = dtProcParams.Rows.Count == 0;
                }
                catch(Exception ex)
                {
                    Console.WriteLine("MySQL: {0}", ex.Message);
                    dtProcParams = null;
                }
            });
        }

        private Type DataAdapterType;
        private bool FactoryDataAdapterIsBroken = false;

        override internal DbDataAdapter CreateDataAdapter()
        {
            if (FactoryDataAdapterIsBroken)
                return DataAdapterType.InvokeMember(null, BindingFlags.CreateInstance, null, null, null) as DbDataAdapter;
            else
                return Factory.CreateDataAdapter();
        }
    }
}