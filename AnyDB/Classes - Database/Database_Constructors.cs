/********************************************************************************************************************
 * 
 * Database_Constructors.cs
 * 
 * Three constructors: the first two use an enum to choose the database type; the third uses a more flexible, but more 
 * typo-prone, "provider invariant string" that does, however, do the real work. The connection string can be passed 
 * either as a string or as a ConnectionStringSettings element.
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace AnyDB
{
    public partial class Database
    {
        private static Regex reWhite = new Regex(@"[\r\n\t ]+");
        private static short _Counter;
        internal readonly short Counter;

        #region First choice

        /// <summary>
        /// First choice: Initialises a new instance of the AnyDB.Database class using an element from the 
        /// ConnectionStrings section of your application or web configuration file. This leaves everything to the user 
        /// without you hard-coding anything. And with only a single parameter there is absolutely no risk of getting 
        /// your parameters mixed up.
        /// </summary>
        /// <param name="ConnectionStringObject">
        /// An entry from the ConnectionStrings section of your configuration file. The providerName attribute can be 
        /// either the short and simple name of a Providers enum, e.g. "SQLServer", or a fully spelled out provider 
        /// invariant name, e.g. "System.Data.SqlClient".
        /// </param>

        public Database(ConnectionStringSettings ConnectionStringObject)
            : this(ConnectionStringObject.ProviderName, ConnectionStringObject.ConnectionString) // third choice constructor
        {
        }

        #endregion
        
        #region Second choice

        /// <summary>
        /// Second choice: Initialises a new instance of the AnyDB.Database class using one of the predefined symbolic 
        /// names and an actual string ConnectionString. Use this constructor if you cannot avoid hard-coding your 
        /// connection string, but you do want to save yourself the inconvenience and human error of mistyping the 
        /// invariant name.
        /// </summary>
        /// <param name="Provider">
        /// The type of database or database provider that you want to use.
        /// </param>
        /// <param name="ConnectionString">
        /// Provider specific connection string.
        /// </param>

        public Database(Providers Provider, string ConnectionString)
            : this(Provider.ToString(), ConnectionString) // third choice constructor
        {
        }

        #endregion

        #region Third choice

        /// <summary>
        /// Third choice: Initialises a new instance of the AnyDB.Database class using the provider's 'invariant' name, 
        /// which is essentially a reference to its DLL. Use this constructor if the .NET provider you require is not 
        /// in the Providers or ProviderInvariantNames list, but it is registered through machine.config, web.config,
        /// or an app.config. If it is not in either of those places, then call the static AnyDB.Database.AddProvider() 
        /// method before using this constructor. Try and avoid using this method, though, because it is easy to pass 
        /// the two strings in the wrong order.
        /// </summary>
        /// <param name="ProviderInvariantName">
        /// Reference to the already registered DLL that handles this type of database, without the .DLL extension, e.g.
        /// "System.Data.SqlClient".
        /// </param>
        /// <param name="ConnectionString">
        /// Provider specific connection string.
        /// </param>

        public Database(string ProviderInvariantName, string ConnectionString)
        {
            /*
             * Accept non invariant (enum) name as an alternative to the actual (long) invariant name.
             */

            if (Enum.IsDefined(typeof(Providers), ProviderInvariantName))
            {
                var eprv = (Providers)Enum.Parse(typeof(Providers), ProviderInvariantName);
                ProviderInvariantName = ProviderInvariantNames.Invariants[eprv];
            }

            /*
             * Remember these in case we need them again. We'll definitely need the connection string.
             */

            this.Counter = unchecked(++_Counter);
            this.ProviderInvariantName = ProviderInvariantName;
            this.ConnectionString = ConnectionString = reWhite.Replace(ConnectionString, " ");

            Debug.WriteLineIf(Database.Trace, "Database #" + Counter + " (" + ProviderInvariantName + ")");

            /*
             * This is what we'll be using most here at the Phlogiston Factory. Most .NET provider factories are loaded
             * with the DbProviderFactories.GetFactory() method. But this doesn't work with CUBRID: we have to load the 
             * DLL of the disk instead.
             */

            try
            {
                if (AssemblyLoadedFactories.ContainsKey(this.ProviderInvariantName))
                {
                    var typ = AssemblyLoadedFactories[this.ProviderInvariantName];
                    this.Factory = (DbProviderFactory)typ.InvokeMember(null, BindingFlags.CreateInstance, null, null, null);
                }
                else
                    this.Factory = DbProviderFactories.GetFactory(this.ProviderInvariantName);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unable to find the requested .Net Framework Data Provider"))
                {
                    var bld = new StringBuilder();
                    bld.Append("For information, these provider invariants are present in some form:");
                    DataTable factories = DbProviderFactories.GetFactoryClasses();
                    foreach(DataRow dr in factories.Rows)
                    {
                        var invariant = dr["InvariantName"].ToString();
                        bld.Append($"\r\n- {invariant}");
                    }
                    var infoex = new Exception(bld.ToString());
                    var windex = new Exception(ex.Message, infoex);
                    var throwex = new ProviderNotFoundException(ConnectionString, this.ProviderInvariantName, windex);
                    throw throwex;
                }
                else
                    throw new ConstructorException(ConnectionString, this.ProviderInvariantName, "Cannot load '" + this.ProviderInvariantName + "' factory class.", ex);
            }

            /*
             * Some ODBC vendors like to use different driver names on 64 bit and 32 bit machines. If you use the wrong 
             * one, you'll end up scratching your head when it tells you the driver is not installed. This has to happen 
             * outside MakeDriverObject() because you won't be able to make a connection if you're using the wrong 
             * driver name. The following code lets you use a 'fuzzy' driver name.
             */

            if (ProviderInvariantName == ProviderInvariantNames.ODBC)
            {
                if (ConnectionString.Contains("Microsoft Access Driver") ||
                    ConnectionString.Contains("SQL Server Native Client") ||
                    ConnectionString.Contains("Microsoft Excel Driver") ||
                    ConnectionString.Contains("Oracle Rdb Driver"))
                {
                    ConnectionString = RewriteDriver(ConnectionString);
                }
                else if (ConnectionString.Contains("Microsoft") && ConnectionString.Contains("Text Driver"))
                {
                    ConnectionString = RewriteDriver(ConnectionString,
                                                     "Microsoft Access Text Driver (*.txt, *.csv)",
                                                     "Microsoft Text Driver (*.txt; *.csv)"); // note semi
                }
                this.ConnectionString = ConnectionString;
            }

            /*
             * Oracle and DB2 both create tables in a schema of the same name as the owner of the table. If Fred creates 
             * a table OINK, the table is actually called FRED.OINK, and you always have to specify the schema if you 
             * are not Fred. This is a right pain!
             * 
             * Luckily DB2 has a connection string option called CurrentSchema to set the default schema on connect. We 
             * also apply this option to procedures later on.
             * 
             * Oracle, on the other hand, does not have any such convenience. You have to run the ALTER SESSION SET 
             * CURRENT_SCHEMA command to change the default schema after you have made your connection.
             * 
             * So I'm going to bastardise Oracle's connection string by giving it a CurrentSchema option. But... Oracle 
             * won't like it! You have to remove the option before you make the connection. For DB2 you DO NOT remove it.
             * 
             * As an aside, I'm also going to allow Current Schema (two words), or Default Schema, because other databases 
             * also have these.
             */

            if (ProviderInvariantName == ProviderInvariantNames.DB2 ||
                ProviderInvariantName == ProviderInvariantNames.Oracle ||
                ProviderInvariantName == ProviderInvariantNames.OracleUnmanaged)
            {
                Regex reSchema = new Regex("(Current|Default) *Schema=(?<NAME>[^;]+);?");
                var match = reSchema.Match(ConnectionString);
                if (match.Success)
                {
                    // capture default schema for both
                    CurrentSchema = match.Groups["NAME"].Value;

                    // remove it if we're connecting to Oracle, possibly rewrite if DB2
                    if (ProviderInvariantName == ProviderInvariantNames.DB2)
                        ConnectionString = reSchema.Replace(ConnectionString, "CurrentSchema=" + CurrentSchema + ";");
                    else
                        ConnectionString = reSchema.Replace(ConnectionString, "");

                    this.ConnectionString = ConnectionString;
                }
            }

            /*
             * SQLite has a connection string quirk whereby a UNC path such as \\server\directory\filename.db has to 
             * start with four backslashes. That's going to bite me loads of times if I don't handle it.
             */

            if (ProviderInvariantName == ProviderInvariantNames.SQLite)
            {
                Regex reSQLiteFileName = new Regex("Data *Source=(?<FNAM>[^;]+)", RegexOptions.IgnoreCase);
                Match m = reSQLiteFileName.Match(ConnectionString);
                if (m.Success)
                {
                    // extract filename
                    string fnam = m.Groups["FNAM"].Value;

                    // double initial backslashes
                    if (fnam.StartsWith(@"\\") && !fnam.StartsWith(@"\\\\")) fnam = @"\\" + fnam;

                    // rewrite connect string
                    ConnectionString = reSQLiteFileName.Replace(ConnectionString, "Data Source=" + fnam);
                    this.ConnectionString = ConnectionString;
                }
            }

            /*
             * The connection string can have a RewriteFlags option to disable query rewriting. If so, parse it and
             * remove it.
             */

            Regex reRewrite = new Regex("RewriteFlags=([^;]+);?");

            if (reRewrite.IsMatch(ConnectionString))
            {
                Match m = reRewrite.Match(ConnectionString);
                foreach (var opt in m.Groups[1].Value.Split(','))
                {
                    var tag = opt.Trim().ToLower();
                         if (tag == "none") RewriteFlags = RewriteOptions.None;
                    else if (tag == "all") RewriteFlags = RewriteOptions.All;
                    else if (tag == "join") RewriteFlags |= RewriteOptions.Join;
                    else if (tag == "limit") RewriteFlags |= RewriteOptions.Limit;
                    else if (tag == "table") RewriteFlags |= RewriteOptions.Table;
                    else if (tag == "timespan") RewriteFlags |= RewriteOptions.Timespan;
                    else if (tag == "nojoin") RewriteFlags &= ~RewriteOptions.Join;
                    else if (tag == "nolimit") RewriteFlags &= ~RewriteOptions.Limit;
                    else if (tag == "notable") RewriteFlags &= ~RewriteOptions.Table;
                    else if (tag == "notimespan") RewriteFlags &= ~RewriteOptions.Timespan;
                }
                ConnectionString = reRewrite.Replace(ConnectionString, "");
                this.ConnectionString = ConnectionString;
            }

            /*
             * Open a temporary connection to check if the connection string works, and also to take note of its SQL 
             * preferences and cache some useful metadata.
             */

            using (var connect = Factory.CreateConnection())
            {
                try
                {
                    connect.ConnectionString = ConnectionString;
                    connect.Open();
                }
                catch (Exception ex)
                {
                    if (ProviderInvariantName == ProviderInvariantNames.ODBC)
                    {
                        Match m = reDriver.Match(ConnectionString);
                        throw new DriverNotFoundException(ConnectionString,
                                                              m.Success ? m.Groups["NAME"].Value : "(UNKNOWN)", 
                                                              ex);
                    }
                    throw new ConnectException(ConnectionString, ProviderInvariantName, ex);
                }
                EstablishParameterStyle(connect);
                Driver = MakeDriverObject(connect);
                Driver.Factory = Factory;
                Driver.ParameterMarkerFormat = ParameterMarkerFormat;
                Driver.ConnectionString = ConnectionString;
            }
        }

        static List<String> GetSystemDriverList()
        {
            RegistryKey reg = Registry.LocalMachine;
            foreach (string sub in @"Software\ODBC\ODBCINST.INI\ODBC Drivers".Split('\\'))
            {
                reg = reg.OpenSubKey(sub);
                if (reg == null) return null;
            }

            List<string> names = new List<string>();
            names = new List<string>(reg.GetValueNames());
            reg.Close();

            names.Sort(delegate(string a, string b) {
                return b.CompareTo(a);
            });
            return names;
        }

#endregion
    }
}