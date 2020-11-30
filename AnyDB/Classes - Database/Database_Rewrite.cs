using System;
using System.Diagnostics;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Controls the extent to which queries may be rewritten by AnyDB to accommodate different SQL dialects.
        /// </summary>
        public RewriteOptions RewriteFlags = RewriteOptions.All;

        /// <summary>
        /// Individual query rewrite options.
        /// </summary>
        [Flags]
        public enum RewriteOptions
        {
            /// <summary>
            /// No rewriting.
            /// </summary>
            None = 0,

            /// <summary>
            /// Rewrite JOIN syntax (Excel, Access, Text).
            /// </summary>
            Join = 1,

            /// <summary>
            /// Rewrite LIMIT, TOP n, FETCH FIRST n ROWS syntax.
            /// </summary>
            Limit = 2,

            /// <summary>
            /// Rewrite table names (Excel, Text).
            /// </summary>
            Table = 4,

            /// <summary>
            /// Rewrite CURRENT_TIMESTAMP and timestamp +/- interval syntax.
            /// </summary>
            Timespan = 8,

            /// <summary>
            /// Rewrite queries as required.
            /// </summary>
            All = Join | Limit | Table | Timespan
        }

        string RewriteQuery(string sql)
        {
            string orig = sql;
            sql = RewriteTimespan(sql);
            sql = RewriteLimit(sql);
            sql = RewriteJoin(sql);
            sql = RewriteTable(sql);
            if (sql != orig && Database.Trace == true)
            {
                Debug.WriteLine("Original SQL");
                Debug.WriteLine(orig);
                Debug.WriteLine("Modified SQL");
                Debug.WriteLine(sql);
            }
            return sql.TrimEnd(' ', ';');
        }
    }
}