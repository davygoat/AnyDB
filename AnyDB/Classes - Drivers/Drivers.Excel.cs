/*
 * Excel is similar to Microsoft Access, but wants a dollar sign after the table name, which the query rewriter will 
 * do for you. INSERT and UPDATE are possible if the connection string has "Readonly=False". DELETE is not supported; 
 * neither are transactions or stored procedures.
 * 
 * https://www.simple-talk.com/sql/database-administration/getting-data-between-excel-and-sql-server-using-odbc--/
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class Excel : DriverBase
    {
        public Excel()
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
            HasTransactions              = false;
            HasStoredProcedures          = false;
            HasMultipleCursors           = false;
            HasOutputParameters          = false;
            HasPrimaryKeyException       = false;
            HasForeignKeyException       = false;
            HasUniqueException           = false;
            HasNotNullException          = false;
            HasCheckException            = false;
            reInvalidColumnException     = new Regex("unknown field name: '(?<NAME>.*?)'", RegexOptions.IgnoreCase);
            reInvalidTableException      = new Regex("'(?<NAME>.*?)' is not a valid name", RegexOptions.IgnoreCase);

            QuirkThrowsInvalidColumnAsParameter = true;
            QuirkTableNameDollar = true;
        }

        [Ident(Providers.Excel, ProductName="EXCEL")]
        public Excel(string ConnectionString)
            : this()
        {
            string cs = ConnectionString.ToLower();
            HasInsert = cs.Contains("readonly=false") || cs.Contains("readonly=0");
            HasUpdate = HasInsert;
            HasDelete = false;
            Readonly  = !HasInsert && !HasUpdate && !HasDelete;
        }

        override internal string FormatTimespan(string start, string sign, string num, string unit)
        {
            if (Access.MicrosoftAccessUnits.ContainsKey(unit)) unit = Access.MicrosoftAccessUnits[unit];
            return base.FormatTimespan(start, sign, num, unit);
        }
    }
}