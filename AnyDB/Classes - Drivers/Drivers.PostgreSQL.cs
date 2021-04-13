/*
 * PostgreSQL supports stored procedures (functions), but doesn't expose functions and their parameters through the 
 * metadata collections (at least Npgsql doesn't). Luckily, Npgsql gets by just fine without having to name them.
 * 
 * Dependencies :-
 * 
 * - Npgsql.dll
 * - Mono.Security.dll
 */

using System;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    [RegisterProvider("Npgsql")]
    class PostgreSQL : DriverBase
    {
        [Ident(Providers.PostgreSQL, ProviderInvariantName="Npgsql")]
        public PostgreSQL()
        {
            TimespanFormat = "{0} {1} INTERVAL '{2} {3}'";
            TimespanExpressions.Add(new Regex("(?<B>"+TOK+")\\s*(?<S>[-+])\\s*INTERVAL\\s*'(?<N>"+NT+")\\s+(?<U>"+UNIT+")S?'", OPT));

            LimitFormat = "SELECT {0} LIMIT {1}";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+(?<N>"+N+")", OPT));

            reInvalidColumnException    = new Regex("column \"(?<NAME>.*?)\".*does not exist",        RegexOptions.IgnoreCase);
            reInvalidTableException     = new Regex("relation \"(?<NAME>.*?)\" does not exist",       RegexOptions.IgnoreCase);
            reInvalidProcedureException = new Regex("function (?<NAME>.*?)\\(.* does not exist",      RegexOptions.IgnoreCase);
            rePrimaryKeyException       = new Regex("duplicate key.*constraint \"(?<NAME>.*?pkey)\"", RegexOptions.IgnoreCase);
            reNotUniqueException        = new Regex("duplicate key.*constraint \"(?<NAME>.*?)\"",     RegexOptions.IgnoreCase);
            reForeignKeyException       = new Regex("\"(?<NAME>.+?)\" violates foreign key",          RegexOptions.IgnoreCase);
            reNotNullException          = new Regex("\"(?<NAME>.+?)\" violates not-null constraint",  RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex("\"(?<NAME>.+?)\" violates check constraint",     RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex("permission denied for relation (?<NAME>.+)",     RegexOptions.IgnoreCase);

            QuirkRefCursorRequiresTransaction = true;
            QuirkDataReaderCloseConnection = true;
        }
    }
}
