/*
 * Comma delimited text: you have to have a file name extension on the table name, so we're going for a rewrite feature. 
 * Files are mostly readonly, but the ODBC driver does allow INSERT (the OLEDB driver, on the other hand, doesn't). 
 * DELETE and UPDATE are not available, and transactions and stored procedures are also out the window. Still, despite 
 * those limitations, "Comedy Limited" (Phil Factor) still has plenty of uses that make it well worth supporting.
 */

using System.IO;
using System.Text.RegularExpressions;

namespace AnyDB.Drivers
{
    class Text : DriverBase
    {
        static Regex reExt = new Regex(@"(Extensions)=([^;]+)");
        static Regex reDir = new Regex(@"(Data Source|DBQ)=([^;]+)");

        internal string Directory;
        internal string Extensions = "csv,txt";

        public Text()
        {
            CurrentTimestamp = "NOW";
            TimespanFormat = "DATEADD('{3}', {1}{2}, {0})";
            TimespanExpressions.Add(new Regex(">>FIXME<<"));

            LimitFormat = "SELECT TOP {1} {0}";
            LimitExpressions.Add(new Regex("SELECT\\s+TOP\\s+("+N+")([^;]+)", OPT));
            
            DefaultJoin                  = "INNER";
            AllowSemicolon               = false;
            HasAccessControl             = false;
            HasPermissionDeniedException = false;
            HasTransactions              = false;
            HasStoredProcedures          = false;
            HasOutputParameters          = false;
            HasPrimaryKeyException       = false;
            HasForeignKeyException       = false;
            HasUniqueException           = false;
            HasNotNullException          = false;
            HasCheckException            = false;
            reInvalidColumnException     = new Regex("unknown field name: '(?<NAME>.*?)'", RegexOptions.IgnoreCase);
            reInvalidTableException      = new Regex("table '(?<NAME>.*?)' not found",     RegexOptions.IgnoreCase);

            QuirkThrowsInvalidColumnAsParameter = true;
            QuirkTableNameDotExtension = true;
        }

        [Ident(Providers.Text, ProviderInvariantName="System.Data.Odbc", ConnectionStringContains="Text Driver")]
        [Ident(Providers.Text, ProviderInvariantName="System.Data.OleDb", ConnectionStringRegularExpression="Extended Properties='[^']*Text,?")]
        public Text(string ProviderInvariantName, string ConnectionString)
            : this()
        {
            Match m = reDir.Match(ConnectionString);
            if (m.Groups.Count == 3)
            {
                string dir = m.Groups[2].Value;
                if (dir.StartsWith("'") || dir.StartsWith("\"")) dir = dir.Substring(1, dir.Length - 2);
                Directory = dir;
            }
            else
                throw new DirectoryNotFoundException("Microsoft Text Driver requires a \"DBQ\" or \"Data Source\" " +
                                                     "query string option that specifies the directory in which to " +
                                                     "look for files.");

            m = reExt.Match(ConnectionString);
            if (m.Groups.Count == 3)
            {
                string ext = m.Groups[2].Value;
                if (ext.StartsWith("'") || ext.StartsWith("\"")) ext = ext.Substring(1, ext.Length - 2);
                Extensions = ext;
            }
            HasInsert = ProviderInvariantName == ProviderInvariantNames.ODBC;
            HasUpdate = false;
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