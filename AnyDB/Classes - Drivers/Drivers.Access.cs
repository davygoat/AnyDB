/*
 * Microsoft Access has a DATEADD function that differs from Microsoft SQL Server. With the office-type products you 
 * cannot use a plain JOIN, you have to rewrite the query to say INNER JOIN. Stored procedures are now possible, but 
 * documentation is rather scarce. There is no support for output parameters, and the procedures can only have one 
 * statement (hence, DataSets are not much use with Access).
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class Access : DriverBase
    {
        [Ident(Providers.Access, ProductName="ACCESS")]
        public Access()
        {
            CurrentTimestamp = "NOW";
            TimespanFormat = "DATEADD('{3}', {1}{2}, {0})";
            TimespanExpressions.Add(new Regex(">>FIXME<<"));

            LimitFormat = "SELECT TOP {1} {0}";
            LimitExpressions.Add(new Regex("SELECT\\s+TOP\\s+(?<N>"+N+")(?<Q>[^;]+)", OPT));
            
            DefaultJoin                  = "INNER";
            AllowSemicolon               = false;
            HasAccessControl             = false;
            HasPermissionDeniedException = false;
            HasPrimaryKeyException       = false;
            HasNotNullException          = false;
            HasCheckException            = false;
            HasMultipleCursors           = false;
            HasOutputParameters          = false;
            reNotUniqueException         = new Regex("would create duplicate values",                      RegexOptions.IgnoreCase);
            reInvalidColumnException     = new Regex("unknown field name: '(?<NAME>.*?)'",                 RegexOptions.IgnoreCase);
            reInvalidTableException      = new Regex("(?:could not|cannot) find.*table.*?'(?<NAME>.*?)'",  RegexOptions.IgnoreCase);
            reInvalidProcedureException  = new Regex(">>> TABLE NOT FOUND <<<",                            RegexOptions.IgnoreCase);
            reNotNullException           = new Regex("must enter a value in the '(?<NAME>.+)' field",      RegexOptions.IgnoreCase);
            reForeignKeyException        = new Regex("related record is required in table '(?<NAME>.+?)'", RegexOptions.IgnoreCase);
            reCheckConstraintException   = new Regex("prohibited.*set for '(?<NAME>.+)'. Enter a value",   RegexOptions.IgnoreCase);
            rePrimaryKeyException        = reNotUniqueException;
            Readonly                     = !HasInsert && !HasUpdate && !HasDelete;

            QuirkThrowsInvalidProcedureAsTable = true;
            QuirkThrowsInvalidColumnAsParameter = true;
        }

        internal static Dictionary<string, string> MicrosoftAccessUnits = new Dictionary<string, string>()
        {
            { "YEAR",   "yyyy" },
            { "MONTH",  "m"    },
            { "WEEK",   "ww"   },
            { "DAY",    "d"    },
            { "HOUR",   "h"    },
            { "MINUTE", "n"    },
            { "SECOND", "s"    }
        };

        override internal string FormatTimespan(string start, string sign, string num, string unit)
        {
            if (Access.MicrosoftAccessUnits.ContainsKey(unit)) unit = Access.MicrosoftAccessUnits[unit];
            return base.FormatTimespan(start, sign, num, unit);
        }
    }
}