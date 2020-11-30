/*
 * Oracle and DB2 treat functions and procedures differently. A function MUST be called with a DbParameter of 
 * ReturnValue direction, a procedure MUST NOT be called with a ReturnValue. We'll use the Procedures collection to 
 * decide which method to use.
 */

using System;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class DB2 : DriverBase
    {
        public DB2()
        {
            TimespanFormat = "{0} {1} {2} {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*(?<N>"+NT+")\\s*(?<U>"+UNIT+")S?", OPT));

            LimitFormat = "SELECT {0} FETCH FIRST {1} ROWS ONLY";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)FETCH\\s+FIRST\\s+(?<N>"+N+")\\sROWS?\\sONLY", OPT));

            HasPrimaryKeyException      = false;
            reNotUniqueException        = new Regex("primary .* unique .* constrains table \"(?<NAME>.+?)\"",            RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex("\"(?<NAME>.*?)\" is not valid in the context",                      RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex("\"(?<NAME>.*?)\" is an undefined name",                             RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex("routine named \"(?<NAME>.*?)\" of type \"(?:PROCEDURE|FUNCTION)\"", RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex(@"FOREIGN KEY ""(?<NAME>[^.]+\.[^.]+)\.[^""]+"" is not equal",       RegexOptions.IgnoreCase);
            reNotNullException          = new Regex(@"NOT NULL column .+ (?<NAME>COLNO=[0-9]+)"" is not allowed",        RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex(@"check constraint ""(?<NAME>[^.]+\.[^.]+)\.[^""]+""\.",             RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex(@"authorization or privilege .+ Object: ""(?<NAME>.+?)""",           RegexOptions.IgnoreCase);
            rePrimaryKeyException       = reNotUniqueException;
        }

        [Ident(Providers.DB2, ProductNameStartsWith="DB2/")]
        public DB2(string ConnectionString, DbProviderFactory Factory)
            : this()
        {
            Console.WriteLine("TODO - Test DB2 fast constructor");

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
            QuirkFunctionsMustHaveReturnValue = true;
            QuirkUseCurrentSchemaForProcedures = ConnectionString.Contains("CurrentSchema=");
        }
    }
}