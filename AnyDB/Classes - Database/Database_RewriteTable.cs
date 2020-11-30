using System.IO;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        /*
         * Regular expression for an SQL table name. I'm anticipating the following "use cases":
         * 
         * SELECT ... FROM <table> ...
         * INSERT INTO <table> VALUES ...
         * UPDATE <table> SET ...
         * DELETE FROM <table> WHERE ...
         * 
         * A SELECT statement can also have multiple tables if you use JOIN syntax rather than the traditional comma
         * and WHERE clause.
         * 
         * SELECT ... FROM <table1> t1 [INNER|LEFT|RIGHT|OUTER] JOIN <table2> t2 ...
         */

        static Regex reTable = new Regex(@"(FROM|INTO|UPDATE|JOIN)(\s+)([A-Z][A-Z0-9_]*)", RegexOptions.Singleline|RegexOptions.IgnoreCase);

        string RewriteTable(string sql)
        {
            if ((RewriteFlags & RewriteOptions.Table) != RewriteOptions.Table) return sql;

            /*
             * The Microsoft ODBC driver for ASCII text files insists that you have a file name extension on the end of 
             * the table name, as in SELECT * FROM fred.csv. There is a connection string option that lets you specify 
             * a list of possible extensions, but the driver does not take any notice. So I've got a quick hack that 
             * accepts a table name without an extension, and adds a suitable extension using the list as input. If no 
             * suitable file can be found, then we'll throw a more meaningful TableNotFoundException. If you want to 
             * specify the extension yourself, then make sure to enclose the file name in square brackets.
             */

            if (Driver.QuirkTableNameDotExtension)
            {
                var vend = Driver as Drivers.Text;
                MatchCollection mc = reTable.Matches(sql);
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
                        if (m.Groups.Count == 4)
                        {
                            string op  = m.Groups[1].Value;
                            string spc = m.Groups[2].Value;
                            string tbl = m.Groups[3].Value;
                            string nam = Path.Combine(vend.Directory, tbl);
                            bool found = false;
                            foreach(string ext in vend.Extensions.Split(','))
                            {
                                if (File.Exists(nam + "." + ext))
                                {
                                    tbl += "." + ext;
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                string tmsg = string.Format("Table '{0}' not found.", tbl);
                                string fmsg = string.Format("File '{0}.{1}' not found.", tbl, vend.Extensions.Split(',')[0]);
                                throw new TableNotFoundException(sql, new FileNotFoundException(tmsg + " " + fmsg), tbl, tmsg + " " + fmsg);
                            }
                            string rep = op + spc + "[" + tbl + "]";
                            sql = reTable.Replace(sql, rep.Replace(" ","¬"), 1);
                        }
                    }
                }
                return sql.Replace("¬", " ");
            }

            /*
             * The ODBC driver for Excel allows any valid Excel syntax as a "table name". However, for a table name to 
             * be valid, it must be in square brackets, and a named worksheet must be followed by a dollar sign to show 
             * the type of range meant. I've implemented another simple hack that lets you use a simple table name to 
             * mean a worksheet within the file. Other notations should still be possible, as long as you enclose them 
             * in square brackets and follow Excel's rules.
             */

            if (Driver.QuirkTableNameDollar)
            {
                MatchCollection mc = reTable.Matches(sql);
                if (mc.Count > 0)
                {
                    foreach (Match m in mc)
                    {
                        if (m.Groups.Count == 4)
                        {
                            string op  = m.Groups[1].Value;
                            string spc = m.Groups[2].Value;
                            string tbl = m.Groups[3].Value;
                            string rep = op + spc + "[" + tbl + "$]";
                            sql = reTable.Replace(sql, rep.Replace(" ","¬"), 1);
                        }
                    }
                }
                return sql.Replace("¬", " ");
            }

            return sql;
        }
    }
}