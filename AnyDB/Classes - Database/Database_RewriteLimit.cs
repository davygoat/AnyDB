/********************************************************************************************************************
 * 
 * Database_RewriteLimit.cs
 * 
 * Code to automatically convert simple "get the first so many rows" type queries into the correct vendor-specific 
 * syntax.
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        static List<Regex> LimitExpressions = new List<Regex>();

        class LimitQueries
        {
            public string query;
            public string limit;

            public LimitQueries(string query, string limit)
            {
                this.query = query;
                this.limit = limit;
            }
        }

        string RewriteLimit(string sql)
        {
            if ((RewriteFlags & RewriteOptions.Limit) != RewriteOptions.Limit) return sql;

            Debug.WriteLineIf(Database.Trace, "RewriteQueryLimit()");

            List<LimitQueries> lims = new List<LimitQueries>();

            Regex reMatch = null;

            /*
             * Look for limit expression(s). Only one really.
             */

            foreach (Regex re in LimitExpressions)
            {
                MatchCollection mc = re.Matches(sql);
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
                        if (m.Groups.Count == 3)
                        {
                            lims.Add(new LimitQueries(m.Groups["Q"].Value,
                                                      m.Groups["N"].Value));
                        }
                    }
                    reMatch = re;
                    break;
                }
            }

            /*
             * Rewrite into the database's preferred syntax.
             */

            if (reMatch != null && reMatch.ToString() != Driver.LimitExpressions.ToString())
            {
                string orig = sql;
                foreach (var lim in lims)
                {
                    string native = Driver.FormatLimit(lim.query, lim.limit);
                    sql = reMatch.Replace(sql, native.Replace(' ','¬'), 1);
                }
                sql = sql.Replace("¬", " ");
            }

            return sql;
        }
    }
}