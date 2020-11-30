/*
 * Firebird has a ProcedureParameters collection very similar to like SQL Server's, but with one or two small column 
 * name differences. But it manages perfectly well without named parameters, so we needn't waste any effort on that 
 * front. But Firebird's procedures can only return one result set at a time. 
 */

using System;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    [RegisterProvider("FirebirdSql.Data.FirebirdClient")]
    class Firebird : DriverBase
    {
        [Ident(Providers.Firebird, ProviderInvariantName="FirebirdSql.Data.FirebirdClient")]
        public Firebird()
        {
            TimespanFormat = "DATEADD({3}, {1}{2}, {0})";
            TimespanExpressions.Add(new Regex("DATEADD\\s*\\((?<U>"+UNIT+")S?\\s*,\\s*(?<S>[-+])?\\s*(?<N>[^\\s,]+)\\s*,\\s*(?<B>"+NT+")\\s*\\)", OPT));

            LimitFormat = "SELECT FIRST {1} {0}";
            LimitExpressions.Add(new Regex("SELECT\\s+FIRST\\s+(?<N>"+N+")(?<Q>[^;]+)", OPT));

            AllowSemicolon              = false;
            HasMultipleCursors          = false;
            HasPrimaryKeyException      = false;
            reNotUniqueException        = new Regex(@"PRIMARY or UNIQUE KEY constraint [^(]+\(""(?<NAME>.*?)"" =",      RegexOptions.IgnoreCase);
            reInvalidColumnException    = new Regex(@"Column unknown\s+(?<NAME>[^\s]+)",                                RegexOptions.IgnoreCase|RegexOptions.Singleline);
            reInvalidTableException     = new Regex(@"Table unknown\s+(?<NAME>[^\s]+)",                                 RegexOptions.IgnoreCase|RegexOptions.Singleline);
            reInvalidProcedureException = new Regex(@"Procedure unknown\s+(?<NAME>[^\s]+)",                             RegexOptions.IgnoreCase|RegexOptions.Singleline);
            reForeignKeyException       = new Regex(@"FOREIGN KEY .* value is \(""(?<NAME>.*?)""",                      RegexOptions.IgnoreCase|RegexOptions.Singleline);
            reNotNullException          = new Regex(@"validation error for column (?<NAME>""[^,]+""), value .... null", RegexOptions.IgnoreCase);
            reCheckConstraintException  = new Regex(@"CHECK constraint.* view or table (?<NAME>[^ ]+)",                 RegexOptions.IgnoreCase);
            rePermissionDeniedException = new Regex(@"no permission .+ TABLE (?<NAME>.+)",                              RegexOptions.IgnoreCase);
            rePrimaryKeyException       = reNotUniqueException;

            QuirkThrowsInvalidProcedureAsTable = true;
            QuirkDataReaderCloseConnection = true;
        }
    }
}