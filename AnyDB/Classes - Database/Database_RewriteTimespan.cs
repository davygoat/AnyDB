/********************************************************************************************************************
 * 
 * Database_RewriteTimespan.cs
 * 
 * Code to automatically convert simple "timestamp plus or minus x days" type queries into the correct vendor-specific 
 * syntax. 
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        static List<Regex> TimespanExpressions = new List<Regex>();

        class TimespanQueries
        {
            public string start;
            public string plusMinus;
            public string num;
            public string unit;
            public string match;

            public TimespanQueries(string start, string plusMinus, string num, string unit, string match)
            {
                this.start = start;
                this.plusMinus = plusMinus;
                this.num = num;
                this.unit = unit.ToUpper();
                this.match = match;
            }
        }

        string RewriteTimespan(string sql)
        {
            if ((RewriteFlags & RewriteOptions.Timespan) != RewriteOptions.Timespan) return sql;

            Debug.WriteLineIf(Database.Trace, "RewriteQueryTimestamp()");

            Regex reCTS = new Regex("(CURRENT_TIMESTAMP)", RegexOptions.IgnoreCase|RegexOptions.Singleline);
            List<TimespanQueries> spans = new List<TimespanQueries>();

            /*
             * Look for timespan expressions.
             */

            Regex reMatch = null;

            foreach (Regex re in TimespanExpressions)
            {
                MatchCollection mc = re.Matches(sql);
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
                        if (m.Groups.Count == 5)
                        {
                            spans.Add(new TimespanQueries(m.Groups["B"].Value,
                                                          m.Groups["S"].Value,
                                                          m.Groups["N"].Value.Replace("'", ""),
                                                          m.Groups["U"].Value,
                                                          m.Groups[0].Value));
                        }
                    }
                    reMatch = re;
                    break;
                }
            }

            /*
             * Translate them into the database's preferred syntax.
             */

            if (reMatch != null && reMatch.ToString() != Driver.TimespanExpressions.ToString())
            {
                string orig = sql;
                foreach (var span in spans)
                {
                    string native = Driver.FormatTimespan(span.start, span.plusMinus, span.num, span.unit);
                    sql = reMatch.Replace(sql, native.Replace(' ','¬'), 1);
                }
                sql = sql.Replace("¬", " ");
            }
            sql = reCTS.Replace(sql, Driver.CurrentTimestamp);

            return sql;
        }
    }
}