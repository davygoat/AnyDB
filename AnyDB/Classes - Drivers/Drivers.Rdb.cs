/*
 * Rdb has serious problems with its factory classes. GetSchema("Procedures") blows up with a memory allocation 
 * failure! So we'll just treat this one as ODBC for now.
 */

using System.Data;
using System.Text.RegularExpressions;
namespace AnyDB.Drivers
{
    class Rdb : DriverBase
    {
        [Ident(Providers.Rdb, ProductName="Rdb")]
        public Rdb()
        {
            TimespanFormat = "{0} {1} INTERVAL '{2}' {3}";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*(?<N>"+qNT+")\\s+(?<U>"+UNIT+")S?", OPT));

            LimitFormat = "SELECT {0} LIMIT TO {1} ROWS";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+TO\\s+(?<N>" + N + ")\\s+ROWS?", OPT));

            QuirkPaddedStrings = true;
        }

        override internal CommandType BindParametersForProcedure(ref string name, params IDbDataParameter[] args)
        {
            string plist = "";
            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0) plist += ",";
                plist += "?";
                args[i].ParameterName = "";
            }
            plist = "(" + plist + ")";
            name = "SELECT " + name + plist + " FROM rdb$database LIMIT TO 1 ROW";
            return CommandType.StoredProcedure;
        }
    }
}