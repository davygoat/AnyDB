/*===================================================================================================================
 *
 * CUBRID's ADO.NET factory classes seem to be only partially implemented. You cannot load it with the usual
 * DbProviderFactories.GetFactory() method, but I have found I can get it into memory with Assembly.LoadFrom().
 * 
 * I cannot find a definitive Provider Invariant Name, so I'm assuming it is the same as the "using" namespace. CUBRID 
 * does not follow the usual pattern, which puts it in the same league as SAP DB.
 * 
 * Connection string variables MUST be lowercase, and MUST NOT have spaces after semicolons. The database and password
 * are case sensitive, username and host are case blind. But anything to the left of an equals sign MUST be lowercase.
 * 
 * DATETIME gets truncated to seconds. For milliseconds you have to convert your parameters to strings.
 * 
 * It has YET ANOTHER timespan style.
 * 
 * Transactions can't use IsolationLevel.Unspecified, so there is now a DefaultIsolationLevel parameter. The default 
 * for data tables is ReadUncommitted.
 * 
 * The CHECK constraint is ignored. The stated reason for this is to allow data migration from other RDBMS.
 * 
 * Java stored procedures barely work. You can call scalars, but forget about returning result sets. Although there
 * is example code for returning java.sql.ResultSet, I haven't been able to get it to work with .NET or even in csql. 
 * This seems to be accepted "in a non-Java environment". Consequently, I'm disabling stored procedures for the time 
 * being, but you can always set HasStoredProcedures to true if you need to call voids or scalars.
 * 
 * http://www.cubrid.org/manual/91/en/sql/jsp.html
 */

using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    [RegisterProvider("CUBRID.Data.CUBRIDClient", DLL="CUBRID.Data.dll", FactoryClass="CUBRID.Data.CUBRIDClient.CUBRIDClientFactory")]
    class CUBRID : DriverBase
    {
        public CUBRID()
        {
            TimespanFormat = "ADDDATE({0}, INTERVAL {1}{2} {3})";
            TimespanExpressions.Add(new Regex("ADDDATE\\s*\\((?<B>"+TOK+")\\s*,\\s*INTERVAL\\s+(?<S>[-+])?\\s*(?<N>"+NT+")\\s+(?<U>"+UNIT+")S?\\s*\\)", OPT));

            LimitFormat = "SELECT {0} LIMIT {1}";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+(?<N>"+N+")", OPT));

            HasCheckException           = false; // parsed but ignored
            HasOutputParameters         = false; // yes but
            HasStoredProcedures         = false; // too many bugs!
            DateTimeFormat              = "yyyy-MM-dd HH:mm:ss.fff";
            DefaultIsolationLevel       = IsolationLevel.ReadUncommitted;
            rePermissionDeniedException = new Regex("Semantic: (.*) is not authorized on (?<NAME>.+)\\. ",   RegexOptions.IgnoreCase);
            rePrimaryKeyException       = new Regex("unique constraint.*INDEX pk_.*ON CLASS (?<NAME>[^(]+)", RegexOptions.IgnoreCase);
            reNotUniqueException        = new Regex("unique constraint.*INDEX .*ON CLASS (?<NAME>[^(]+)",    RegexOptions.IgnoreCase);
            reNotNullException          = new Regex("Attribute .(?<NAME>.+?). cannot be made NULL",          RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex("Unknown class \"(?<NAME>.+)\"",                         RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex("foreign key '(?<NAME>.+)' is invalid",                  RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex("Semantic: (?<NAME>.*?) is not defined",                 RegexOptions.IgnoreCase);
        }

        [Ident(Providers.CUBRID, ProviderInvariantName="CUBRID.Data.CUBRIDClient")]
        public CUBRID(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            if (HasStoredProcedures) // currently disabled anyway
            {
                Console.WriteLine("TODO - Test CUBRID fast constructor.");
                BackgroundProcParamsNeeded(ConnectionString, () =>
                {
                    using (var con = Factory.CreateConnection())
                    {
                        con.ConnectionString = ConnectionString;
                        con.Open();
                        ProceduresCache[ConnectionString] = con.GetSchema("Procedures");
                    }
                });
            }

            Regex reParams = new Regex(@"\((?<LIST>.*)\)");
            dtProcParams = new DataTable();
            dtProcParams.Columns.Add("procedure_name", typeof(string));
            dtProcParams.Columns.Add("parameter_name", typeof(string));
            dtProcParams.Columns.Add("data_type", typeof(string));
            dtProcParams.Columns.Add("ordinal_position", typeof(int));
            foreach (DataRow dr in dtProcedures.Rows)
            {
                string name = dr["PROCEDURE_NAME"].ToString();
                string type = dr["PROCEDURE_TYPE"].ToString();
                string target = dr["TARGET"].ToString();
                if (type == "PROCEDURE")
                {
                    dtProcedures.Rows.Add(name);
                }
                Match m = reParams.Match(target);
                if (m.Success)
                {
                    int ord = 0;
                    foreach (string prm in m.Groups["LIST"].Value.Split(','))
                    {
                        dtProcParams.Rows.Add(name, "p" + ord, prm.Trim(), ord);
                        ord++;
                    }
                }
            }
        }

        override internal CommandType BindParametersForProcedure(ref string name, params IDbDataParameter[] args)
        {
            /*
             * CUBRID uses question marks for parameters, but when it comes to procedures the parameters MUST be
             * named even though it doesn't supply provide you with any names and ignores the names anyway. But then 
             * it refuses to accept parameter markers on the procedure call itself, so we've got to use SQL injection. 
             * That makes it a bit like MonetDB.
             */

            string plist = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) plist += ",";
                object val = args[i].Value;
                if (val == null || val == DBNull.Value) plist += "NULL";
                else if (val is DateTime) plist += "'" + ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                else if (val is string) plist += "'" + val.ToString().Replace("'", "''") + "'";
                else plist += val.ToString();
                args[i].ParameterName = "?p" + i;
            }

            /*
             * Procedures and functions are called differently.
             */

            bool isFun = dtProcedures.Select("procedure_name = '" + name + "' AND procedure_type = 'FUNCTION'").Length != 0;

            if (isFun) name = "SELECT " + name + "(" + plist + ")";
            else name = "CALL " + name + "(" + plist + ")";

            return CommandType.Text;
        }
    }
}
