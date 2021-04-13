using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        static Regex reJoin = new Regex(@"\s([A-Z0-9]+)\s+JOIN\s", RegexOptions.IgnoreCase|RegexOptions.Singleline);

        static List<string> JoinTypes = new List<string>
        {
            "LEFT",
            "RIGHT",
            "INNER",
            "OUTER"
        };

        string RewriteJoin(string sql)
        {
            if ((RewriteFlags & RewriteOptions.Join) != RewriteOptions.Join) return sql;
            if (Driver.DefaultJoin == null) return sql;

            MatchCollection mc = reJoin.Matches(sql);
            if (mc.Count > 0)
            {
                foreach (Match m in mc)
                {
                    if (m.Groups.Count == 2)
                    {
                        string type = m.Groups[1].Value;
                        if (!JoinTypes.Contains(type.ToUpper()))
                        {
                            string rep = " " + type + " " + Driver.DefaultJoin + " JOIN ";
                            sql = reJoin.Replace(sql, rep.Replace(" ", "¬"), 1);
                        }
                    }
                }
                return sql.Replace("¬", " ");
            }
            return sql;
        }
    }
}