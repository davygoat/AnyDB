/*
 * Informix is reasonably well behaved. It is a lot like DB2 and can use the same .NET provider. But like all SQL
 * databases, it has its own variations on the standard.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class Informix : DriverBase
    {
        public Informix()
        {
            CurrentTimestamp = "CURRENT";
            TimespanFormat = "{0} {1} INTERVAL({2}) {3} TO {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*\\((?<N>"+qNT+")\\)\\s*(?<U>"+UNIT+")\\s+TO\\s+(\\3)", OPT));

            LimitFormat = "SELECT FIRST {1} {0}";
            LimitExpressions.Add(new Regex("SELECT\\s+FIRST\\s+(?<N>"+N+")(?<Q>[^;]+)", OPT));

            HasMultipleCursors          = false;
            HasPrimaryKeyException      = false;
            reNotUniqueException        = new Regex(@"Unique constraint \((?<NAME>.*?)\) violated.*duplicate",      RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex(@"Column \((?<NAME>.*?)\) not found",                           RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex(@"The specified table \((?<NAME>.*?)\) is not in the database", RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex(@"Routine \((?<NAME>.*?)\) can not be resolved",                RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex(@"table for referential constraint \((?<NAME>[^)]+)\)",         RegexOptions.IgnoreCase);
            reNotNullException          = new Regex(@"Cannot insert a null into column \((?<NAME>[^)]+)\)",         RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex(@"Check constraint \((?<NAME>[^)]+)\) failed",                  RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex(@"No .+ permission for (?<NAME>.+)\.",                          RegexOptions.IgnoreCase);
            rePrimaryKeyException       = reNotUniqueException;
        }

        [Ident(Providers.Informix, ProductNameStartsWith="IDS/")]
        public Informix(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            Console.WriteLine("TODO - Test Informix fast constructor.");
            BackgroundProcParamsNeeded(ConnectionString, () =>
            {
                using (var con = Factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    con.Open();
                    ProceduresCache[ConnectionString] = con.GetSchema("Procedures");
                }
            });
            MetaProcedureName = "procedure_name";
            MetaParameterName = "column_name";
        }
    }
}