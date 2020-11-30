/********************************************************************************************************************
 *
 * Database_Parameters.cs
 *
 * Parameterized queries are a database portability minefield. If you do it by the book on one database, there's a 
 * distinct possibility it won't work on another.
 *
 * Plus, binding parameters to your carefully crafted SQL statement is a right pain, when you're in a hell of a hurry 
 * to get that emergency page out, and the phlogiston factory is struggling to keep its earth/air/water/fire balance 
 * under control. And that doesn't even come close to the mayhem the BCS caused, when the noodle extrusion line got 
 * clogged by that batch of wheat that Raw Materials imported from Italy. The guys on shift said they'd never seen that 
 * much gluten in one strand! Instead of ramyeon, we got rubber bands, and one hell of a tangle at the pulling end.
 * What a waste! Think of the software engineers we could have got with that batch. That would never have happened if 
 * the BCS inspectors hadn't issued us with an improvement notice, ordering the immediate decommissioning that goto 
 * statement. I mean, they didn't even give us time to do a Management of Change! So much for the British Cakemakers' 
 * Society being the competent body for cold drawn dough products.
 *
 * Anyway... You know very well you should be using parameter markers in your SQL, but when you're faced with the 
 * imminent threat of an antimatter breakout... well, let's just concatenate it this once...
 *
 * https://xkcd.com/327/
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        internal string ParameterMarker;
        internal string ParameterMarkerFormat;
        internal string ParameterMarkerPattern;

        /*===========================================================================================================
         * 
         * EstablishParameterStyle()
         * 
         * Use the connection's DataSourceInformation collection to determine the provider's native parameter formatting 
         * style. Microsoft, following Sybase, prefixes parameters with at (@) signs, but other databases tend to use 
         * colons, and question marks are also not unheard of. And then ODBC uses question marks without naming the 
         * parameters, which throws yet another spanner into the works. (As we speak, contractors are being given very 
         * stern toolbox talks about the safety implications of throwing hex wrenches into live machinery without proper
         * isolation.)
         */

        void EstablishParameterStyle(DbConnection connect)
        {
            /*
             * Get the ParameterMarkerFormat and ParameterMarkerPattern columns from the connection's 
             * DataSourceInformation collection. We'll need both of these. The database identifying information will 
             * also be useful for reference.
             */

            var dsi = connect.GetSchema("DataSourceInformation");

            if (dsi != null)
            {
                var dr = dsi.Rows[0];

                ProductName            = dr["DataSourceProductName"].ToString();
                ProductVersion         = dr["DataSourceProductVersion"].ToString();
                ParameterMarkerFormat  = dr["ParameterMarkerFormat"].ToString();
                ParameterMarkerPattern = dr["ParameterMarkerPattern"].ToString();
            }
            else
            {
                ProductName            = ProviderInvariantName;
                ProductVersion         = "unknown";
                ParameterMarkerFormat  = "?";
            }

            /*
             * If ParameterMarkerFormat is a question mark, we're probably using ODBC. The suffix ~Format hints at the 
             * fact that this is a String.Format format string. When used with a parameter name, the resulting string 
             * will be a question mark plain and simple, which is what ODBC expects to see.
             */

            if (ParameterMarkerFormat == "?")
            {
                ParameterMarker = "";
                return;
            }

            /*
             * If ParameterMarkerFormat is "{0}", we're looking at a SQL Server style .NET provider.
             * 
             * Whereas the ODBC question mark format string results in a single question mark, the "{0}" results in the 
             * parameter name, which would be exactly as nature intended, were it not for the glaring omission of the 
             * all-important colon or curlycue. This, poses one observant internet user, is quite disgusting. I'm 
             * rather inclined to agree with the chap, though I'm not quite sure whether this repulsive object was 
             * ejected from a vulpine sigmoid, or a strygine proventriculus.
             * 
             * Google: sql server parametermarkerformat disgusting
             * 
             * To find out the identity of the elusive parameter marker, we have to untangle the eponymous 
             * ParameterMarkerPattern. This, as its suffix would suggest, is a regular expression, destined for the 
             * lofty task of identifying parameter markers. Again, another observant internet user points out that the 
             * first character of the ParameterMarkerPattern is the required "at" sign. All well and good, until you 
             * discover that other databases have slightly more exotic regular expressions...
             * 
             * "If one hit doesn't do the job, hit it again. And if that doesn't work, get a bigger hammer." (A 
             * Scaffold Monkey)
             */

            if (ParameterMarkerFormat == "{0}")
            {
                // Hit it once, hit it twice...
                string marker = ParameterMarkerPattern.Substring(0, 1);
                if (marker == "(") marker = ParameterMarkerPattern.Substring(1, 1);

                // It's for your own good, much safer this way.
                if (marker != "@" && marker != ":" && marker != "?") 
                    throw new Wobbler("Don't know how to handle:\r\n" +
                                      "ParameterMarkerFormat " + ParameterMarkerFormat + "\r\n" +
                                      "ParameterMarkerPattern " + ParameterMarkerPattern);

                ParameterMarker = marker;
                return;
            }

            /*
             * One last case. If it's not a question mark (ODBC), and it's not a broken Microsoft excuse of a format 
             * string, this must be one of those really traditional IBM or Oracle DBMS that don't need to conform 
             * themselves to Microsoft's bugs. Curiously, we end up doing the same as we did for ODBC, though the end 
             * result will be very different. In case you're wondering: Oracle's is ":{0}", DB2's is "@{0}".
             */

            {
                ParameterMarker = "";
                return;
            }
        }

        /*===========================================================================================================
         * 
         * BindParameters()
         * 
         * The AnyDB methods accept, in addition to a formatted SQL statement, a variable number of additional 
         * parameters that are to be bound to the markers in the SQL statement. Because parameter markers vary 
         * considerably between databases, and even between drivers for the same database, we'll try and accommodate 
         * any style that might be thrown at us.
         * 
         * Generally speaking, there are three parameter styles in common use:
         * 
         * - Named parameters, prefixed with a colon or at sign, as used by Microsoft and other databases. The open 
         *   source databases tend to allow both prefixes for the obvious portability benefits (no reason to avoid 
         *   locking in customers). Named parameters can be repeated, and could, in theory, be bound in any order; 
         *   though in practice this will not be possible, because of the next common style.
         *   
         * - Question marks, without names, as is the requirement on ODBC drivers. Because ODBC parameters are not 
         *   named, they have to be bound in the correct number and order.
         *   
         * - Parameter names only, without any prefix. This goes against SQL92, and poses far too many difficulties 
         *   for me to even attempt to support.
         *   
         * To these three, I would like to add a fourth, perhaps simpler, option:
         * 
         * - A number, enclosed in curly brackets. This style will be familiar to .NET programmers because it's the good 
         *   old String.Format approach.
         *   
         * Whichever method you prefer is entirely up to yourself, but the third option is, as I said, not really 
         * possible to implement, and the out of sequence variation on the first would be asking for trouble. The fourth 
         * option is there for you to consider, because it is inherently database neutral, and there's no risk of 
         * catching a question mark in the wrong place. It's also pretty easy to understand when you've been using .NET 
         * for a while. What's more, in that case you *can* repeat or reorder your parameters.
         * 
         * Either way, you should always parameterize your queries, never concatenate (he says, knowing very well what 
         * a bad example he's always set...)
         */

        Regex reParmo = new Regex(@"({[0-9]+}|[:@?][A-Za-z][A-Za-z0-9_]*|\?)");
        Regex reFormo = new Regex(@"{([0-9]+)}");

        internal void BindParameters(DbCommand command, ref string sql, params object[] args)
        {
            command.Parameters.Clear();

            /*
             * Look for parameter markers, which may be "{xxx}", ":yyy", "@zzz" or "?".
             */

            List<string> markers = new List<string>();
            {
                MatchCollection mc = reParmo.Matches(sql);
                for (int i = 0; i < mc.Count; i++) markers.Add(mc[i].Value);
            }

            /*
             * Special case for .NET format string style parameter markers. If all of them are numbered, we can accept 
             * repeated or out of sequence parameters.
             */

            if (DotNetParameters(command, markers, ref sql, args)) return;

            /*
             * Otherwise, treat all parameters as positional.
             */

            StandardParameters(command, markers, ref sql, args);
        }

        /*===========================================================================================================
         * 
         * DotNetParameters()
         * 
         * If we're using .NET string.Format() style parameter markers, i.e. {0}, {1}, etc. Then we can accept repeated 
         * or out of sequence markers. That saves having to repeat a parameter when it is used multiple times within 
         * the query. It should also make .NET programmers' lives easier.
         */

        bool DotNetParameters(DbCommand command, List<string> markers, ref string sql, params object[] args)
        {
            /*
             * No parameters? No problem.
             */

            if (markers.Count == 0) return true;

            /*
             * Check if all parameters are curly bracketed numbers, e.g. {0}. If we find even a single one that is not, 
             * then the whole lot is decidely *not* .NET style.
             */

            Dictionary<string, int> dic = new Dictionary<string, int>();
            int min = int.MaxValue;
            int max = int.MinValue;

            foreach (string marker in markers)
            {
                if (!reFormo.IsMatch(marker)) return false; // we know it's *not* .NET

                int num = int.Parse(marker.Substring(1, marker.Length - 2));
                if (num < min) min = num;
                if (num > max) max = num;
                dic[marker] = num;
            }

            /*
             * A slight caveat for ODBC. Its parameters are always positional, no matter what. If that's the case, 
             * we'll have to renumber the parameter markers and re-jig the argument list. We can't use string.Replace() 
             * for this because it replaces all instances of the search string, not just the first. So we'll have to 
             * use a regular expression instead and make sure we escape the curly bracket because it's not a quantifier.
             */

            if (string.Format(ParameterMarker + ParameterMarkerFormat, "ODBC") == "?")
            {
                object[] newargs = new object[markers.Count];
                string[] newmarks = new string[markers.Count];
                for (int i=0; i<markers.Count; i++)
                {
                    string marker = markers[i];
                    int num = dic[marker];
                    newargs[i] = args[num];
                    newmarks[i] = "@pood" + (i+1) + "booq";
                    sql = new Regex("\\" + marker).Replace(sql, newmarks[i], 1);
                    dic[newmarks[i]] = i;
                }
                args = newargs;
                markers = new List<string>(newmarks);
            }
            else
                markers = new List<string>(dic.Keys);

            /*
             * Check the parameter count. Think of the parameters as an array, and the markers as array indices. It 
             * doesn't matter if we have fewer or more parameters than markers. As long as all the indices are in 
             * range, we're ok. If we're using ODBC, then we'll have exactly the right number at this point.
             */

            if (min < 0 || max >= args.Length)
                throw new FormatException(".NET style parameter markers: index (zero based)" +
                                          " must be greater than or equal to zero" +
                                          " and less than the size of the argument list.");

            /*
             * Modify the SQL string to conform to the provider's parameter style, which will be anything but .NET 
             * format string. In fact, if we're on ODBC, even the SQL won't be .NET format anymore because we've just 
             * scrunged it into SQL Server style.
             */

            foreach (string marker in markers)
            {
                int num = dic[marker];
                string oldMarker = marker;
                string newMarker = string.Format(ParameterMarker + ParameterMarkerFormat, "pood" + (num+1) + "booq");
                sql = sql.Replace(oldMarker, newMarker);
            }

            /*
             * Now bind the parameters to the markers. In this case we'll be accessing the args array in essentially 
             * random order based on the unique parameter marker numbers.
             */

            foreach (string marker in markers)
            {
                int num = dic[marker];
                IDbDataParameter param = Factory.CreateParameter();
                param.ParameterName = string.Format(ParameterMarker + ParameterMarkerFormat, "pood" + (num+1) + "booq");
                param.Value = args[num];
                if (param.Value is DateTime && Driver.DateTimeFormat != null) param.Value = ((DateTime)param.Value).ToString(Driver.DateTimeFormat);
                param.DbType = InferDbType(param.Value, param.DbType);
                param.Size = InferSize(param.Value, 0);
                param.Scale = InferScale(param.Value, 0);
                command.Parameters.Add(param);
            }

            /*
             * We've done it!
             */

            return true;
        }

        /*===========================================================================================================
         * 
         * StandardParameters()
         * 
         * Here standard means anything but .NET style. In other words, the parameters markers could have colons, at 
         * signs, or question marks. We're going to expect the argument list to have exactly the right number of elements, 
         * exactly the same number as the SQL has parameter markers.
         */

        List<string> CheckRepeats(List<string> markers)
        {
            Dictionary<string, bool> dic = new Dictionary<string, bool>();
            List<string> newlist = new List<string>();
            foreach(string marker in markers)
            {
                if (marker == "?") return markers;
                if (!dic.ContainsKey(marker))
                {
                    dic[marker] = true;
                    newlist.Add(marker);
                }
            }
            return newlist;
        }

        void StandardParameters(DbCommand command, List<string> markers, ref string sql, params object[] args)
        {
            /*
             * We're going to treat the parameters as positional. That means no omissions and no repeats. In other 
             * words, there must be as many parameters as there are markers - no fewer, no more.
             */

            if (markers.Count != args.Length)
            {
                markers = CheckRepeats(markers);
                if (markers.Count != args.Length)
                    throw new Exception("The number of query parameters does not match the number of parameter markers");
            }

            /*
             * Special case: if all parameters are DbParameters with names, then don't rename and booqend them, but just 
             * accept them as they are. This is for SQLite "stored procedures".
             */

            bool allNamed = true;
            foreach (object arg in args)
            {
                if (!(arg is DbParameter) || ((DbParameter)arg).ParameterName == "")
                {
                    allNamed = false;
                    break;
                }
            }
            if (allNamed)
            {
                command.Parameters.AddRange(args);
                return;
            }

            /*
             * Modify the SQL string to conform to the provider's parameter style. ParameterMarker is either a colon, 
             * an at sign, or nothing at all. ParameterMarkerFormat results either in a parameter name, or a single 
             * question mark. We'll number the parameters and booqend them to make sure we don't end up replacing the 
             * wrong ones. Shlemiel the painter also plays a cameo role in this section.
             */

            for (int i = 0; i < markers.Count; i++)
            {
                string mark = markers[i].Replace("?", "\\?");
                Regex re = new Regex($"{mark}(?<DELIM>[^A-Za-z0-9_]|$)");
                string newmark = string.Format(ParameterMarker + ParameterMarkerFormat, "pood" + (i + 1) + "booq");
                sql = re.Replace(sql, newmark + "${DELIM}");
            }

            /*
             * Now bind the parameters to the markers. Pay attention to ParameterMarker -- a prefix may be required, 
             * tolerated or reprobated, depending on the driver. In this case we'll be accessing the args array 
             * sequentially in parallel with the marker array.
             */

            for (int i = 0; i < markers.Count; i++)
            {
                IDbDataParameter param = Factory.CreateParameter();
                param.ParameterName = string.Format(ParameterMarker + ParameterMarkerFormat, "pood" + (i + 1) + "booq");
                param.Value = args[i] ?? DBNull.Value;
                if (param.Value is DateTime && Driver.DateTimeFormat != null) param.Value = ((DateTime)param.Value).ToString(Driver.DateTimeFormat);
                param.DbType = InferDbType(param.Value, param.DbType);
                param.Size = InferSize(param.Value, 0);
                param.Scale = InferScale(param.Value, 0);
                command.Parameters.Add(param);
            }
        }

        DbType InferDbType(object ob, DbType def)
        {
            //if (ob == null) return DbType.Object;
            if (ob is DateTime || ob is DateTime?) return Driver.DateTimeType;
            if (ob is int || ob is int?) return DbType.Int32;
            if (ob is short || ob is short?) return DbType.Int16;
            if (ob is long || ob is long?) return DbType.Int64;
            if (ob is float || ob is float?) return DbType.Single;
            if (ob is double || ob is double?) return DbType.Double;
            if (ob is bool || ob is bool?) return DbType.Boolean;
            if (ob is string) return DbType.String;
            if (ob != null && ob.GetType().IsEnum) return DbType.String;
            return def;
        }

        int InferSize(object ob, int def)
        {
            if (ob is DateTime) return 8;
            if (ob is int || ob is int?) return sizeof(int);
            if (ob is short || ob is short?) return sizeof(short);
            if (ob is long || ob is long?) return sizeof(long);
            if (ob is float || ob is float?) return sizeof(float);
            if (ob is double || ob is double?) return sizeof(double);
            if (ob is string) return ob != null ? ob.ToString().Length : 0;
            return def;
        }

        byte InferScale(object ob, byte def)
        {
            if (ob is DateTime || ob is DateTime?) return 7;
            return def;
        }
    }

    /// <summary>
    /// Something very peculiar happened, and we don't know what to do about it.
    /// </summary>
    public class Wobbler : Exception
    {
        /// <summary>
        /// Throws a wobbler with an error message of your choice.
        /// </summary>
        /// <param name="msg"></param>
        public Wobbler(string msg)
            : base(msg)
        {
        }
    }
}