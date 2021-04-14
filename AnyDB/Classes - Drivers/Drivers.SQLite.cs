/*
 * SQLite is similar to MySQL and SQL Server in may ways. It main limitations are the lack of stored procedures and 
 * of access controls. Both of these are design tradeoffs because SQLite's primary focus is on leanness and running
 * in embedded-type environments.
 * 
 * It does, however, have a unique combination of features that we can use to implement "stored queries". So most of 
 * the code in this file is actually an SPliteCommand to act as a proxy for SQLiteCommand.
 * 
 * Dependencies :-
 * 
 * - System.Data.SQLite.dll
 * - SQLite.Interop.dll      (depending on System.Data.SQLite.dll version)
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("SPlite, PublicKey=0024000004800000940000000602000000240000525341310004000001000100f9fef02c4d43f5ad0ffbfbb75694855c52bfa59d4f21568a1a387eb3e7b78ce7f4123c805708cb8b0b5f93efe04898a080a4a12f533171bf044cf56d534dbc4b1376fe0a420fba6702ced8bbfb1113b21d8ac8a9979434a5e121bf4cd0ac30139dc0b61e186608a2cd3fee5315149b38334382b4a9905e998f2a4edcc9d7c8f4")]

namespace AnyDB.Drivers
{
    [RegisterProvider("System.Data.SQLite")]
    internal class SQLite : DriverBase
    {
        public SQLite()
        {
            TimespanFormat = "DATETIME({0}, '{1}{2} {3}')";
            TimespanExpressions.Add(new Regex("DATETIME\\s*\\((?<B>"+TOK+")\\s*,\\s*'\\s*(?<S>[-+])?\\s*(?<N>"+NT+")\\s+(?<U>"+UNIT+")S?\\s*'\\s*\\)", OPT));

            LimitFormat = "SELECT {0} LIMIT {1}";
            LimitExpressions.Add(new Regex("SELECT(?<Q>.+?)LIMIT\\s+(?<N>"+N+")", OPT));

            HasStoredProcedures          = false;
            HasOutputParameters          = false;
            HasPrimaryKeyException       = false;
            HasAccessControl             = false;
            HasPermissionDeniedException = false;
            reNotUniqueException         = new Regex("unique constraint failed: (?<NAME>.+)",                       RegexOptions.IgnoreCase);
            reInvalidColumnException     = new Regex("has no column named (?<NAME>.*)|no such column: (?<NAME>.*)", RegexOptions.IgnoreCase);
            reInvalidTableException      = new Regex("no such table: (?<NAME>.*)",                                  RegexOptions.IgnoreCase);
            reInvalidProcedureException  = new Regex("Procedure '(?<NAME>.+)' not found",                           RegexOptions.IgnoreCase);
            reForeignKeyException        = new Regex("FOREIGN KEY constraint failed",                               RegexOptions.IgnoreCase);
            reNotNullException           = new Regex("NOT NULL constraint failed: (?<NAME>.+)",                     RegexOptions.IgnoreCase);
            reCheckConstraintException   = new Regex("CHECK constraint failed: (?<NAME>.+)",                        RegexOptions.IgnoreCase);
            rePrimaryKeyException        = reNotUniqueException;
        }

        [Ident(Providers.SQLite, ProductName="SQLite")]
        public SQLite(DbProviderFactory Factory, string ConnectionString)
            : this()
        {
            this.Factory = Factory;
            OnConnect.Add("PRAGMA FOREIGN_KEYS=\"ON\"");
            try
            {
                using (var con = Factory.CreateConnection())
                {
                    con.ConnectionString = ConnectionString;
                    con.Open();
                    var dt = GetDataTableUsingConnection(@"SELECT *
                                                           FROM   sqlite_master
                                                           WHERE  type = 'table'
                                                           AND    name = 'splite_procs'",
                                                         con);
                    this.HasStoredProcedures = dt.Rows.Count == 1;
                }
            }
            catch
            {
                throw;
            }
        }

        override internal DbCommand CreateCommand()
        {
            return new SPliteCommand(Factory);
        }

#region SPliteCommand

        internal class SPliteCommand : DbCommand
        {
            DbCommand   _Command;     // encapsulate SQLiteCommand, but don't impose System.Data.SQLite reference
            CommandType _CommandType; // not allowed to assign _Command.CommandType = CommandType.StoredProcedure

#region Constructors

            internal SPliteCommand(DbProviderFactory Factory)
            {
                _Command = Factory.CreateCommand();
                _CommandType = _Command.CommandType;
            }

            internal SPliteCommand(DbConnection connection)
            {
                _Command = connection.CreateCommand();
                _CommandType = _Command.CommandType;
            }

#endregion

#region The bits I tend to use a lot

            override public string CommandText 
            {
                get
                {
                    return _Command.CommandText;
                }
                set
                {
                    _Command.CommandText = value;
                }
            }

            override public CommandType CommandType
            {
                get
                {
                    return _CommandType;
                }
                set
                {
                    _CommandType = value;
                    if (value != System.Data.CommandType.StoredProcedure) _Command.CommandType = value;
                }
            }

            new public void Dispose()
            {
                if (_Command != null)
                {
                    _Command.Dispose();
                    _Command = null;
                }
            }

            override public int ExecuteNonQuery()
            {
                PossiblyGetProcedureBody();
                return _Command.ExecuteNonQuery();
            }

            override public object ExecuteScalar()
            {
                PossiblyGetProcedureBody();
                return _Command.ExecuteScalar();
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                PossiblyGetProcedureBody();
                return _Command.ExecuteReader(behavior);
            }

            new public DbDataReader ExecuteReader()
            {
                PossiblyGetProcedureBody();
                return _Command.ExecuteReader();
            }

            protected override DbParameter CreateDbParameter()
            {
                return _Command.CreateParameter();
            }

            new public DbParameterCollection Parameters
            {
                get
                {
                    return _Command.Parameters;
                }
            }

#endregion

#region The rest of the DbCommand interface

            override public void Cancel()
            {
                _Command.Cancel();
            }

            override public int CommandTimeout
            {
                get
                {
                    return _Command.CommandTimeout;
                }
                set
                {
                    _Command.CommandTimeout = value;
                }
            }

            new public DbConnection Connection
            {
                get
                {
                    return _Command.Connection;
                }
                set
                {
                    _Command.Connection = value;
                }
            }

            override protected DbConnection DbConnection
            {
                get
                {
                    return _Command.Connection;
                }
                set
                {
                    _Command.Connection = value;
                }
            }

            override protected DbTransaction DbTransaction
            {
                get
                {
                    return _Command.Transaction;
                }
                set
                {
                    _Command.Transaction = value;
                }
            }

            override protected DbParameterCollection DbParameterCollection
            {
                get
                {
                    return _Command.Parameters;
                }
            }

            override public bool DesignTimeVisible
            {
                get;
                set;
            }

            override public UpdateRowSource UpdatedRowSource
            {
                get
                {
                    return _Command.UpdatedRowSource;
                }
                set
                {
                    _Command.UpdatedRowSource = value;
                }
            }

            override public void Prepare()
            {
                _Command.Prepare();
            }

            new public DbTransaction Transaction
            {
                get
                {
                    return _Command.Transaction;
                }
                set
                {
                    _Command.Transaction = value;
                }
            }

#endregion

#region The naughty bits (private parts)

            private void PossiblyGetProcedureBody()
            {
                if (_CommandType == System.Data.CommandType.StoredProcedure)
                {
                    using (DbCommand temp = _Command.Connection.CreateCommand())
                    {
                        string procnam = _Command.CommandText;
                        _Command.CommandText = GetProcedureBody(temp, procnam);
                        _Command.CommandType = System.Data.CommandType.Text;

                        string key = temp.Connection.ConnectionString + "¬" + procnam;
                        TrialRun cache = ProcedureCache[key];

                        if (cache.Parameters != null && cache.Parameters.Count == _Command.Parameters.Count)
                        {
                            for (int i=0; i<cache.Parameters.Count; i++)
                            {
                                if (_Command.Parameters[i].ParameterName == null)
                                {
                                    _Command.Parameters[i].ParameterName = cache.Parameters[i];
                                }
                            }
                        }
                    }
                }
            }

            /*
             * I'm using Microsoft-style SP syntax to extract:
             * 
             * (1) The CREATE PROCEDURE keywords (no OR REPLACE and IF NOT EXISTS right now).
             * (2) The procedure name.
             * (3) The parameters, all the way up to the keyword AS.
             * (4) The procedure body.
             * 
             * The procedure body is optionally enclosed in a BEGIN/END code block.
             * 
             * The syntax actually looks very similar to SQLite trigger syntax.
             */

            static Regex reSproc = new Regex(@"^\s*" +
                                             @"CREATE\s+PROCEDURE\s*" +
                                             @"(?<IFNO>IF\s+NOT\+EXISTS\s*)?" +
                                             @"(?<NAME>[A-Za-z][A-Za-z0-9_]*) *" +
                                             @"(?<PARAMS>.*)\s+AS\s+" +
                                             @"(?<BODY>.+)$",
                                             RegexOptions.IgnoreCase|RegexOptions.Singleline);

            static Regex reCodeBlock = new Regex(@"^\s*" +
                                                 @"BEGIN\s+" +
                                                 @"(?<STATEMENTS>.+)\s+" +
                                                 @"END\s*;?\s*$", 
                                                 RegexOptions.IgnoreCase|RegexOptions.Singleline);

            static Regex reParam = new Regex(@"([@:][A-Za-z][A-Za-z0-9_]*)");

            static char[] whitespace =  " \t\r\n\f".ToCharArray();
            static char[] semiwhite  = "; \t\r\n\f".ToCharArray();

            internal class TrialRun
            {
                public string ProcedureName;
                public string SQL;
                public string Body;
                public bool Fail;
                public string Message;
                public List<string> Parameters;

                public TrialRun(string name, string sql)
                {
                    this.ProcedureName = name;
                    this.SQL = sql;
                    this.Fail = true;
                }
            }

            static Dictionary<string, TrialRun> ProcedureCache = new Dictionary<string, TrialRun>();

            private string GetProcedureBody(DbCommand command, string ProcedureName)
            {
                string sql = null;

                /*
                 * Look up the procedure definition in splite_procs. If we can't find it, throw an exception.
                 */

                command.CommandText = @"SELECT sql FROM splite_procs WHERE name = @nam";
                command.CommandType = System.Data.CommandType.Text;

                DbParameter dbp = command.CreateParameter();
                dbp.ParameterName = "nam";
                dbp.Value = ProcedureName;
                dbp.Direction = ParameterDirection.Input;
                command.Parameters.Add(dbp);

                sql = command.ExecuteScalar()?.ToString();
                if (sql == null)
                    throw new SPliteException("Procedure '" + ProcedureName + "' not found.");

                /*
                 * We've just done a trip to the database engine. Further down we'll be doing some really nasty stuff
                 * like making trial runs on every statement in the query body. That has the potential for lots of 
                 * activity that can hinder performance. So we'll try and minimise the impact of all that nonsense by 
                 * maintaining a cache of the procedure definitions (valid or invalid). Then we'll only have to go 
                 * through the whole shebang if the definition actually changed since we last checked.
                 */

                string key = command.Connection.ConnectionString + "¬" + ProcedureName;

                if (ProcedureCache.ContainsKey(key))
                {
                    TrialRun cache = ProcedureCache[key];
                    if (cache.SQL == sql)
                    {
                        if (cache.Fail) throw new SPliteException(cache.Message);
                        else return cache.Body;
                    }
                    else
                        Debug.WriteLine("Procedure '" + ProcedureName + "' updated.");
                }

                /*
                 * This is where we do the nasty stuff. It's in a separate function that we can also make available to 
                 * the SPlite program.
                 */

                TrialRun test = ProcedureCache[key] = new TrialRun(ProcedureName, sql);
                CheckSyntax(test, command);
                return test.Body;
            }

            static internal string GetProcedureName(string sql)
            {
                return reSproc.Match(sql).Groups["NAME"].Value;
            }

            static internal void CheckSyntax(TrialRun test, DbCommand command)
            {
                string sql = test.SQL;
                string ProcedureName = test.ProcedureName;

                /*
                 * The procedure definition MUST have CREATE PROCEDURE keywords, a NAME, a PARAMETER LIST (possibly
                 * empty), and a BODY. At this point we have to start checking syntax because we haven't got a lemon 
                 * parser to guarantee that invalid procedures never make it into the system catalog.
                 */

                Match m = reSproc.Match(sql);
                if (m.Groups.Count < 4)
                {
                    test.Message = "Procedure '" + ProcedureName + "' not a valid CREATE PROCEDURE statement.\r\n" + sql;
                    throw new SPliteException(test.Message);
                }

                string name    = m.Groups["NAME"].Value;
                string ifno    = m.Groups["IFNO"] != null ? m.Groups["IFNO"].Value : "";
                string prmlist = m.Groups["PARAMS"].Value.Trim();
                string body    = m.Groups["BODY"].Value.Trim(semiwhite);

                if (name.ToUpper() != ProcedureName.ToUpper())
                {
                    test.Message = "Procedure '" + ProcedureName +
                                                         "' splite_procs name differs from CREATE PROCEDURE " + name + ".";
                    throw new SPliteException(test.Message);
                }

                /*
                 * The parameter list is optionally enclosed in parentheses, and parentheses must be balanced.
                 */

                if (prmlist.StartsWith("(") || prmlist.EndsWith(")"))
                {
                    if (prmlist.StartsWith("(") && prmlist.EndsWith(")"))
                        prmlist = prmlist.Substring(1, prmlist.Length-2).Trim(semiwhite);
                    else
                    {
                        test.Message = "Procedure '" + ProcedureName + "' unbalanced parentheses in argument list.";
                        throw new SPliteException(test.Message);
                    }
                }

                /*
                 * Extract the names and types, stick 'em into a dictionary so we can check for undeclared parameters.
                 */

                Dictionary<string, string> ParameterList = new Dictionary<string, string>();

                if (prmlist != "")
                {
                    test.Parameters = new List<string>();
                    foreach (string param in prmlist.Split(','))
                    {
                        string[] tok = param.Trim().Split(whitespace, StringSplitOptions.RemoveEmptyEntries);

                        string nam = tok[0];
                        if (tok.Length < 2)
                        {
                            test.Message = "Procedure '" + ProcedureName + "' missing data type after parameter '" + nam + "'.";
                            throw new SPliteException(test.Message);
                        }
                        if (!nam.StartsWith("@") && !nam.StartsWith(":"))
                        {
                            test.Message = "Procedure '" + ProcedureName + "' expected parameter marker at '" + nam + "'.";
                            throw new SPliteException(test.Message);
                        }
                        if (ParameterList.ContainsKey(nam.ToUpper()))
                        {
                            test.Message = "Procedure '" + ProcedureName + "' parameter '" + nam + "' declared twice.";
                            throw new SPliteException(test.Message);
                        }
                        ParameterList[nam.ToUpper()] = tok[1].ToUpper();
                        test.Parameters.Add(nam);
                    }
                }

                /*
                 * Multiple statements are enclosed in a BEGIN/END construct, which has to be properly opened and closed.
                 * 
                 * However, if we pass the whole code block to SQLite, it'll complain about finding a command where it 
                 * expects to see a semicolon or a TRANSACTION keyword. If we insert a semicolon, it'll throw if we're 
                 * already in a transaction. So we'll just have to strip off the BEGIN/END bit. I'm sure the lemon parser 
                 * can handle this because it already has multi statement triggers.
                 */

                var BODY = body.ToUpper();
                if (BODY.StartsWith("BEGIN") || BODY.EndsWith("END"))
                {
                    m = reCodeBlock.Match(body);
                    if (!BODY.StartsWith("BEGIN") || !BODY.EndsWith("END"))
                    {
                        test.Message = "Procedure '" + ProcedureName + "' BEGIN/END mismatch.";
                        throw new SPliteException(test.Message);
                    }
                    body = m.Groups["STATEMENTS"].Value;
                }

                /*
                 * Scan the body for parameter markers, checking for any undeclared parameters.
                 */

                foreach (Match prm in reParam.Matches(body))
                {
                    string mark = prm.Captures[0].Value;
                    if (!ParameterList.ContainsKey(mark.ToUpper()))
                    {
                        test.Message = "Procedure '" + ProcedureName + "' undeclared parameter '" + mark + "'.";
                        throw new SPliteException(test.Message);
                    }
                }

                /*
                 * Now we have a parameterized semicolon delimited sequence of SQL statements that we can run as a plain
                 * DbCommand.Text. But first I want to make sure that the statements actually parse, otherwise there is 
                 * the risk of, say, a successful DELETE, followed by a failed INSERT, leaving you with an incomplete 
                 * result. So we'll see if the EXPLAIN command throws anything back at us. Unfortunately that means we 
                 * have to run each individual SQL statement separately, HOPING THAT WE HAVEN'T BROKEN THE SEQUENCE ON 
                 * THE WRONG SEMICOLON...  I trust nobody in their right mind will have string literals with semicolons 
                 * in them?
                 */

                foreach (string cmd in body.Split(';'))
                {
                    if (cmd.Trim() != "")
                    {
                        try
                        {
                            // Don't try and explain explain.
                            if (!cmd.Trim().ToUpper().StartsWith("EXPLAIN"))
                                command.CommandText = "EXPLAIN QUERY PLAN " + cmd;
                            else 
                                command.CommandText = cmd;
                            command.ExecuteNonQuery();
                        }
                        catch(Exception ex)
                        {
                            // Expect this to also error on "Insufficient parameters".
                            if (ex.Message.Contains("SQL logic error"))
                            {
                                command.CommandText = ProcedureName;
                                test.Message = "Procedure '" + ProcedureName + "' " + ex.Message;
                                throw new SPliteException(test.Message);
                            }
                        }
                    }
                }

                /*
                 * Right. We got this far, so we can tentatively assume that there is nothing blatantly wrong with the 
                 * SQL we're about to run. Unless you got a false positive, that is... In that case: Tough. Have a lemon!
                 */

                test.Body = body;
                test.Fail = false;
            }

#endregion
        }

        internal class SPliteException : Exception
        {
            internal SPliteException(string message)
                : base(message)
            {
            }
        }

#endregion
    }
}