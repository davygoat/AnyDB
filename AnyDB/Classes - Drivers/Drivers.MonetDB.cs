/*
 * MonetDB *has* stored procedures and functions, but with major headaches:
 * 
 * - The ODBC driver cannot handle parameter markers when calling stored functions. If you try to use any parameter 
 *   markers, you will be confronted with a "Cannot have a parameter (?) on both sides of an expression" error. So 
 *   we'll have to use "SQL Injection for Fun and Profit" to get round this one.
 *   
 * - GetSchema("Procedures") blows up with "no such operator 'env'". If you grant public to the env() procedure (this 
 *   also affects the product version), then it complains about p.sql. Without a procedures collection there's no real 
 *   way of knowing whether we should be calling a procedure or a function. So we'll have to roll our own dtProcedures 
 *   collection by querying the system catalog ourselves.
 *   
 * - Output parameters are not supported even though MonetDB does have them.
 * 
 * - Table valued functions cannot return more than one table. In other words, you can't return multiple result sets 
 *   in a DataSet or a DataReader. But MonetDB is not alone in having this limitation.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class MonetDB : DriverBase
    {
        static Dictionary<string,List<string>> MonetProcedureNames = new Dictionary<string, List<string>>();

        public MonetDB()
        {
            TimespanFormat = "{0} {1} INTERVAL '{2}' {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*(?<N>"+qNT+")\\s+(?<U>"+UNIT+")S?", OPT));

            LimitFormat = "SELECT {0} LIMIT {1}";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+(?<N>"+N+")", OPT));

            HasMultipleCursors  = false;
            HasOutputParameters = false;
            HasCheckException   = false;

            rePrimaryKeyException       = new Regex("PRIMARY KEY constraint '(?<NAME>.*?)' violated",                  RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex("FOREIGN KEY constraint '(?<NAME>.*?)' violated",                  RegexOptions.IgnoreCase);
            reNotUniqueException        = new Regex("UNIQUE constraint '(?<NAME>.*?)' violated",                       RegexOptions.IgnoreCase);
            reNotNullException          = new Regex("NOT NULL constraint violated for column '(?<NAME>.*?)'",          RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex("no such column '(?<NAME>.*?)'|identifier '(?<NAME>.*?)' unknown", RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex("no such table '(?<NAME>.*?)'",                                    RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex("no such operator '(?<NAME>.*?)'",                                 RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex("access denied .+ table '(?<NAME>.+)'",                            RegexOptions.IgnoreCase);
        }

        [Ident(Providers.MonetDB, ProductName="MonetDB")]
        public MonetDB(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            Console.WriteLine("TODO - Test MonetDB fast constructor.");

            /*
             * Build our own dtProcParams.
             */

            BackgroundProcParamsNeeded(ConnectionString, () =>
            {
                using (var con = Factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    con.Open();
                    ProcParamsCache[ConnectionString] = GetDataTableUsingConnection(
                            @"SELECT   CASE fun.type
                                      WHEN 1 THEN 'Scalar'
                                      WHEN 2 THEN 'Procedure'
                                      WHEN 3 THEN 'Aggregate'
                                      WHEN 4 THEN 'Filter'
                                      WHEN 5 THEN 'Table'
                                      ELSE        CAST(fun.type AS VARCHAR(10))
                                   END         AS function_type,
                                   fun.name    AS procedure_name,
                                   sch.name    AS schema_name,
                                   prm.name    AS argument_name,
                                   prm.inout   AS inout,
                                   prm.number  AS ordinal_position
                          FROM     sys.functions fun
                                   LEFT JOIN sys.args    prm ON prm.func_id   = fun.id
                                   JOIN      sys.schemas sch ON fun.schema_id = sch.id
                                   JOIN      sys.types   typ ON fun.type      = typ.id
                          WHERE    fun.mod = 'user'
                          ORDER BY sch.name, fun.name, prm.number",
                          con
                    );
                }
                var list = new List<string>();
                foreach (DataRow dr in ProcParamsCache[ConnectionString].Select("function_type = 'Procedure'"))
                {
                    var str = dr["procedure_name"].ToString();
                    if (!list.Contains(str)) list.Add(str);
                }
                MonetProcedureNames[ConnectionString] = list;
            });
            MetaProcedureName = "procedure_name";
            MetaParameterName = "argument_name";
            MetaOrdinalPosition = "ordinal_position";
        }

        override internal CommandType BindParametersForProcedure(ref string name, params IDbDataParameter[] args)
        {
            /*
             * MonetDB doesn't implement parameter markers on stored procedures and it has some very unusual invocation 
             * methods.
             */

            var ProcedureNames = MonetProcedureNames[ConnectionString];

            // cannot use bound parameters, so we have to use injection
            string plist = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) plist += ",";
                object val = args[i].Value;
                if (val == null || val == DBNull.Value) plist += "NULL";
                else if (val is DateTime) plist += "'" + ((DateTime)val).ToString("yyyy-MM-dd HH:mm:ss.fff") + "'";
                else if (val is string) plist += "'" + val.ToString().Replace("'", "''") + "'";
                else plist += val.ToString();
            }

            // three different calling mechanisms
            bool isProc = ProcedureNames.Contains(name.ToLower());
            bool isTable = dtProcParams.Select("procedure_name = '" + name.ToLower() +
                                                                    "' AND function_type = 'Table'").Length > 0;
            if (isProc) name = "CALL " + name + "(" + plist + ")";                // void
            else if (isTable) name = "SELECT * FROM " + name + "(" + plist + ")"; // table valued
            else name = "SELECT " + name + "(" + plist + ")";                     // scalar

            // and neither of them is a stored procedure
            return CommandType.Text;
        }
    }
}