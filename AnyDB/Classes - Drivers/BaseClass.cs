using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace AnyDB
{
    #region Data

    public partial class DriverBase
    {
        internal DriverBase()
        {
        }

        /// <summary>
        /// This static field is set by AnyDB to defer the DLL load error.
        /// </summary>
        internal static string DLLNotFound = null;

        /// <summary>
        /// The data provider factory used for creating DbCommand objects.
        /// </summary>
        public DbProviderFactory Factory { get; internal set; }

        #region Regular expressions

        static Regex reInvalid = new Regex("^>><<$");

        /// <summary>
        /// The regular expression used to identify "Column not found" exceptions.
        /// </summary>
        internal Regex reInvalidColumnException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Table not found" exceptions.
        /// </summary>
        internal Regex reInvalidTableException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Procedure not found" exceptions.
        /// </summary>
        internal Regex reInvalidProcedureException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Permission denied" exceptions.
        /// </summary>
        internal Regex rePermissionDeniedException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Failed PRIMARY KEY check" exceptions.
        /// </summary>
        internal Regex rePrimaryKeyException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Failed FOREIGN KEY check" exceptions.
        /// </summary>
        internal Regex reForeignKeyException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Failed NOT UNIQUE check" exceptions.
        /// </summary>
        internal Regex reNotUniqueException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Failed NOT NULL check" exceptions.
        /// </summary>
        internal Regex reNotNullException = reInvalid;

        /// <summary>
        /// The regular expression used to identify "Failed CHECK constraint" exceptions.
        /// </summary>
        internal Regex reCheckConstraintException = reInvalid;

        #endregion

        #region Query rewriter and datetime parameters

        /// <summary>
        /// </summary>
        protected const string UNIT = "HOUR|MINUTE|SECOND|DAY|WEEK|MONTH|YEAR";  // unit, e.g. day, week.

        /// <summary>
        /// </summary>
        protected const string TOK = "[@:]?[A-Z][A-Z0-9_]*|{[0-9]+}";           // token, e.g. current_timestamp or parameter marker

        /// <summary>
        /// </summary>
        protected const string N = "[0-9]+";                                  // num

        /// <summary>
        /// </summary>
        protected const string NT = N + "|" + TOK;                             // num or token

        /// <summary>
        /// </summary>
        protected const string qNT = "'" + N + "'|" + TOK;                      // quoted num or token

        /// <summary>
        /// </summary>
        protected const RegexOptions OPT = RegexOptions.IgnoreCase | RegexOptions.Singleline;

        /// <summary>
        /// Preferred syntax for CURRENT_TIMESTAMP, e.g. now().
        /// </summary>
        internal string CurrentTimestamp = "CURRENT_TIMESTAMP";

        /// <summary>
        /// Regular expressions used to identify "timestamp +/- 1 HOUR" type syntax.
        /// </summary>
        internal List<Regex> TimespanExpressions = new List<Regex>();

        /// <summary>
        /// Syntax for representing CURRENT_TIMESTAMP - 1 HOUR type queries.
        /// </summary>
        internal string TimespanFormat = "{0} {1} INTERVAL '{2}' {3}";

        /// <summary>
        /// Regular expressions used to identify "SELECT FIRST n ROWS" type queries.
        /// </summary>
        internal List<Regex> LimitExpressions = new List<Regex>();

        /// <summary>
        /// Syntax for representing SELECT FIRST n ROWS type queries.
        /// </summary>
        internal string LimitFormat = "SELECT {0} LIMIT {1}";

        /// <summary>
        /// DbType to use for passing DateTime parameters at the highest possible resolution. SQL Server defaults
        /// to milliseconds unless DateTime2 is used.
        /// </summary>
        internal DbType DateTimeType = DbType.DateTime;

        /// <summary>
        /// .NET DateTime format string to use if the database truncates timestamp seconds.
        /// </summary>
        internal string DateTimeFormat;

        /// <summary>
        /// Default transaction isolation level. CUBRID rejectes Unspecified.
        /// </summary>
        internal IsolationLevel DefaultIsolationLevel = IsolationLevel.Unspecified;

        /// <summary>
        /// Microsoft Access, Excel and Text require the INNER keyword for ordinary joins. Other databases default
        /// to an inner join.
        /// </summary>
        internal string DefaultJoin = null;

        /// <summary>
        /// SQL statements to run on opening a connection. For example, with MySQL we will set SQL_MODE to enforce
        /// CHECK constraints. You can always add or remove statements if you need to.
        /// </summary>
        internal List<string> OnConnect = new List<string>();

        /// <summary>
        /// Set this field in the driver if the ParameterMarkerFormat cannot be derived from the DataSourceInformation
        /// collection.
        /// </summary>
        internal string ParameterMarkerFormat;

        /// <summary>
        /// A copy of the connection string.
        /// </summary>
        internal string ConnectionString;

        #endregion

        #region Features and quirks (assuming ideal RDBMS)

        /*
         * Assume all features enabled. Turn them off in the driver's constructor.
         */

        /// <summary>
        /// Does the database have an INSERT statement?
        /// </summary>
        public bool HasInsert { get; protected set; } = true;

        /// <summary>
        /// Does the database have a working UPDATE statement?
        /// </summary>
        public bool HasUpdate { get; protected set; } = true;

        /// <summary>
        /// Does the database allow DELETE statements?
        /// </summary>
        public bool HasDelete { get; protected set; } = true;

        /// <summary>
        /// Does the database support transactions and does it have a working ROLLBACK?
        /// </summary>
        public bool HasTransactions { get; protected set; } = true;

        /// <summary>
        /// Does the database correctly reply with a "Permission denied" message when you do not have access to an 
        /// existing table, or does it neither confirm nor deny with a "Table not found" evasion?
        /// </summary>
        public bool HasPermissionDeniedException { get; protected set; } = true;

        /// <summary>
        /// Does the database correctly reply with a "Table not found" message if the table does not exist, or does
        /// it diplomatically tell you to **** off with a "Permission denied".
        /// </summary>
        public bool HasInvalidTableException { get; protected set; } = true;

        /// <summary>
        /// Does the database explicitly state that the PRIMARY KEY is being violated, or does it refer to the
        /// record's UNIQUE-ness.
        /// </summary>
        public bool HasPrimaryKeyException { get; protected set; } = true;

        /// <summary>
        /// Does the database enforce referential integrity?
        /// </summary>
        public bool HasForeignKeyException { get; protected set; } = true;

        /// <summary>
        /// Does the database have a UNIQUE constraint?
        /// </summary>
        public bool HasUniqueException { get; protected set; } = true;

        /// <summary>
        /// Does the database have a NOT NULL constraint?
        /// </summary>
        public bool HasNotNullException { get; protected set; } = true;

        /// <summary>
        /// Does the database have a working CHECK constraint?
        /// </summary>
        public bool HasCheckException { get; protected set; } = true;

        /// <summary>
        /// Does the database have stored procedures?
        /// </summary>
        public bool HasStoredProcedures { get; protected set; } = true;

        /// <summary>
        /// Does the database have access controls, something like a GRANT statement?
        /// </summary>
        public bool HasAccessControl { get; protected set; } = true;

        /// <summary>
        /// Can a stored procedure return the results of more than one query?
        /// </summary>
        public bool HasMultipleCursors { get; protected set; } = true;

        /// <summary>
        /// Do the database and the .NET provider allow you to call procedures with output parameters?
        /// </summary>
        public bool HasOutputParameters { get; protected set; } = true;

        /*
         * Assume no quirks. Turn them on as required in the driver's constructor.
         */

        /// <summary>
        /// Firebird and PostgreSQL DataReader leak connections if you don't use CommandBehavior.CloseConnection.
        /// </summary>
        public bool QuirkDataReaderCloseConnection { get; protected set; } = false;

        /// <summary>
        /// True if the database throws a "Procedure not found" as a "Table not found". If so, we will catch the
        /// exception and re-throw it properly.
        /// </summary>
        public bool QuirkThrowsInvalidProcedureAsTable { get; protected set; } = false;

        /// <summary>
        /// True if the database reports "Column not found" exceptions as a missing parameter. MS Access does this
        /// because it treats the invalid query as a "parameterized query" (it does not use parameter markers).
        /// </summary>
        public bool QuirkThrowsInvalidColumnAsParameter { get; protected set; } = false;

        /// <summary>
        /// True if stored functions MUST have a parameter of direction InOut. If so, AnyDB will insert one for you.
        /// </summary>
        public bool QuirkFunctionsMustHaveReturnValue { get; protected set; } = false;

        /// <summary>
        /// True for DB2. We pick will up the CurrentSchema option from the query string and cross-apply it to stored
        /// procedures to make your life easier.
        /// </summary>
        public bool QuirkUseCurrentSchemaForProcedures { get; protected set; } = false;

        /// <summary>
        /// True for PostgreSQL. I don't want you wasting your valuable time on a inexplicable 'Error 34000: cursor 
        /// "&lt;unnamed portal x&gt;" does not exist' error.
        /// </summary>
        public bool QuirkRefCursorRequiresTransaction { get; protected set; } = false;

        /// <summary>
        /// Rdb pads its strings to the declared length, which makes comparisons a bit awkward. So I'll make sure
        /// your DataTable, DataSet, and Scalar, have their strings trimmed for convenience. This doesn't work
        /// for DataReader, in which case you have to do your own trimming.
        /// </summary>
        public bool QuirkPaddedStrings { get; protected set; } = false;

        /// <summary>
        /// True for Excel so we can use an ordinary table name to commonly refer to a worksheet.
        /// </summary>
        public bool QuirkTableNameDollar { get; protected set; } = false;

        /// <summary>
        /// True for the Microsoft Text driver so you do not have to add .csv or .txt to all your table names.
        /// </summary>
        public bool QuirkTableNameDotExtension { get; protected set; } = false;

        /// <summary>
        /// True for MySQL if the mysql.proc table is not readable. You can avoid this problem by GRANT'ing the
        /// user SELECT on mysql.proc (as long as that does not give you any security nightmares).
        /// </summary>
        public bool QuirkParameterNamesRequired { get; protected set; } = false;

        /*
         * A few more.
         */

        /// <summary>
        /// True if the database allows you to run multiple queries, separated by semicolons. Oracle does not allow
        /// any semicolons, not even on the end of your query. AnyDB always removes the last semicolon.
        /// </summary>
        public bool AllowSemicolon { get; protected set; } = true;

        /// <summary>
        /// True if the database allows neither INSERT, UPDATE or DELETE.
        /// </summary>
        public bool Readonly { get; protected set; } = false;

        #endregion

        #region Metadata collections

        /// <summary>
        /// Returns the driver's type name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.GetType().ToString();
            }
        }

        /// <summary>
        /// A DataTable containing all the stored PROCEDURES, to distinguish them from FUNCTIONS.
        /// </summary>
        internal DataTable dtProcedures;

        /// <summary>
        /// A DataTable containing all stored procedures/functions and their parameters.
        /// </summary>
        internal DataTable dtProcParams;

        /// <summary>
        /// Which column in dtProcParams refers to the procedure's name.
        /// </summary>
        internal string MetaProcedureName = "procedure_name";

        /// <summary>
        /// Which column in dtProcParams refers to the procedure's schema, if used.
        /// </summary>
        protected string MetaProcedureSchema = "procedure_schema";

        /// <summary>
        /// Which column in dtProcParams indicates the parameter's position in the procedure's formal parameter list.
        /// </summary>
        protected string MetaOrdinalPosition = "ordinal_position";

        /// <summary>
        /// Which column in dtProcParams refers to the parameter's name.
        /// </summary>
        protected string MetaParameterName = "parameter_name";

        /// <summary>
        /// Which column in dtProcParams identifies the parameter's data type.
        /// </summary>
        protected string MetaDataType = "data_type";

        /// <summary>
        /// Which column in dtProcParameter qualifies a parameter as having IN, OUT or INOUT direction.
        /// </summary>
        protected string MetaDirection = "direction";

        #endregion
    }

    #endregion

    #region Base methods

    /// <summary>
    /// Base class for AnyDB drivers.
    /// </summary>

    partial class DriverBase
    {
        /// <summary>
        /// Gets a new DbCommand from the Factory. The SQLite driver overrides this method so it can provider stored
        /// procedures through a SPliteCommand instead of a SQLiteCommand.
        /// </summary>
        /// <returns></returns>
        internal virtual DbCommand CreateCommand()
        {
            return Factory.CreateCommand();
        }

        /// <summary>
        /// Gets a new DbDataAdapter from the Factory. The new improved platform independent MySQL Connector/NET 6.10.6 
        /// always returns null, so the driver has to resort to instantiating a MySql.Data.MyClient.MySqlDataAdapter 
        /// the hard way. The old 32 bit version didn't have this problem.
        /// </summary>
        /// <returns></returns>
        internal virtual DbDataAdapter CreateDataAdapter()
        {
            return Factory.CreateDataAdapter();
        }

        /// <summary>
        /// Format a CURRENT_TIMESTAMP +/- n HOURS type query into the preferred syntax.
        /// </summary>
        /// <param name="start">Starting point, e.g. CURRENT_TIMESTAMP.</param>
        /// <param name="sign">Plus or minus.</param>
        /// <param name="num">Number of units.</param>
        /// <param name="unit">Unit, e.g. HOUR, MINUTE, DAY, MONTH.</param>
        /// <returns>Timespan expression.</returns>
        internal virtual string FormatTimespan(string start, string sign, string num, string unit)
        {
            return string.Format(TimespanFormat, start, sign, num, unit);
        }

        /// <summary>
        /// Format a SELECT n ROWS type query into the preferred syntax.
        /// </summary>
        /// <param name="select">The query part of the statement.</param>
        /// <param name="limit">The number of records part of the statement.</param>
        /// <returns></returns>
        internal string FormatLimit(string select, string limit)
        {
            return string.Format(LimitFormat, select, limit);
        }

        /// <summary>
        /// Use this method if you need to read system catalogs on initialisation, e.g. if your .NET data provider 
        /// factory does not have a working ProcedureParameters collection.
        /// </summary>
        /// <param name="sql">SQL query. MUST use correct vendor specific syntax, and MUST NOT use parameters.</param>
        /// <param name="connect">The System.Data.Common.DbConnection to use.</param>
        /// <returns></returns>
        protected DataTable GetDataTableUsingConnection(string sql, DbConnection connect)
        {
            using (DbCommand command = connect.CreateCommand())
            {
                command.CommandText = sql;
                using (DbDataReader reader = command.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    int ncols = reader.FieldCount;
                    for (int i = 0; i < ncols; i++)
                        dt.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
                    while (reader.Read())
                    {
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < ncols; i++) dr[i] = reader[i];
                        dt.Rows.Add(dr);
                    }
                    return dt;
                }
            }
        }

        /// <summary>
        /// Sets up the parameter list in preparation for calling a stored procedure.
        /// </summary>
        /// <param name="name">Procedure name.</param>
        /// <param name="args">Argument list.</param>
        /// <returns>Command type.</returns>
        internal virtual CommandType BindParametersForProcedure(ref string name, params IDbDataParameter[] args)
        {
            /*
             * If we're using ODBC, we'll need a list of question marks, and we'll blank out the parameter names.
             */

            if (ParameterMarkerFormat == "?")
            {
                string plist = "";
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0) plist += ",";
                    plist += "?";
                    args[i].ParameterName = "";
                }
                if (plist != "") plist = "(" + plist + ")";
                name = "{CALL " + name + plist + "}";
                return CommandType.StoredProcedure;
            }

            /*
             * MySQL requires parameters to be named, but doesn't actually give you a list of parameter names, which 
             * means you, the developer, need to know the names in advance. However, if the parameter list has only 
             * input parameters, you can use CALL and pretend it's a query. That doesn't work with OUT and INOUT 
             * parameters, but it's better than nothing. Note this is almost identical to the ODBC situation, except 
             * for the lack of curly brackets around the CALL command.
             */

            else if (QuirkParameterNamesRequired &&
                     Array.FindIndex(args, p => p.Direction != ParameterDirection.Input) < 0)
            {
                string plist = "";
                for (int i = 0; i < args.Length; i++)
                {
                    if (i > 0) plist += ",";
                    plist += "?";
                    args[i].ParameterName = "";
                }
                name = "CALL " + name + "(" + plist + ")";
                return CommandType.Text;
            }

            /*
             * For databases that require parameter names, i.e. SQL Server, or MySQL if it gave us a list, fill in the 
             * names.
             */

            else if (dtProcParams != null)
            {
                DataRow[] dr = GetParameters(name);
                int nargs = Math.Min(args.Length, dr.Length);
                for (int i = 0; i < nargs; i++)
                {
                    args[i].ParameterName = dr[i][MetaParameterName].ToString();
                }

                // Anticipate MySQL error
                if (QuirkParameterNamesRequired &&
                    Array.FindIndex(args, p => p.Direction != ParameterDirection.Input) >= 0 &&
                    nargs != args.Length)
                {
                    Regex reUID = new Regex("(User|UID)=([^ ;]+)", RegexOptions.IgnoreCase);
                    MatchCollection mc = reUID.Matches(ConnectionString);
                    if (mc.Count > 0)
                    {
                        string user = mc[mc.Count - 1].Groups[2].Value;
                        throw new AnyDbException("Cannot call procedure '" + name + "' with output parameters " +
                                                 "because neither information_schema.parameters nor mysql.proc are readable.\r\n" +
                                                 "\r\n" +
                                                 "Either:\r\n" +
                                                 "\r\n" +
                                                 "(1) Rewrite " + name + " to avoid output parameters.\r\n" +
                                                 "(2) Explicitly name the output parameters when calling " + name + ".\r\n" +
                                                 "(3) GRANT SELECT ON mysql.proc TO " + user + ".");
                    }
                }
                return CommandType.StoredProcedure;
            }

            /*
             * Anything else.
             */

            else return CommandType.StoredProcedure;
        }

        /// <summary>
        /// Get the formal parameter list for the procedure by filtering the dtProcParams collection.
        /// </summary>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        internal DataRow[] GetParameters(string procedureName)
        {
            string[] dot = procedureName.Split('.');
            string whereClause;
            if (dot.Length == 1)
                whereClause = MetaProcedureName + " = '" + procedureName + "' ";
            else
                whereClause = MetaProcedureName + " = '" + dot[1] + "' AND " +
                              MetaProcedureSchema + " = '" + dot[0] + "'";

            return dtProcParams.Select(whereClause,
                                       MetaOrdinalPosition + " ASC");
        }

        internal virtual IDbDataParameter[] PossiblyAddRefCursor(string procedureName, IDbDataParameter[] args)
        {
            return args;
        }

        #endregion
    }
}