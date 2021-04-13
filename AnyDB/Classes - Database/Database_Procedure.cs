/********************************************************************************************************************
 * 
 * Database_Procedure.cs
 * 
 * Stored procedures are even more of a portability nightmare than parameterized queries. But at least the biggest 
 * differences are nicely contained on the server. That's the whole point of stored procedures: you don't need to know 
 * what they do, how they're written, where they get their data from... All you have to do is call the procedure, pass 
 * it a few parameters, and use the data it returns to you.
 * 
 * At least that would be the case if more than one database provider could agree between themselves how to call a 
 * procedure. Even in .NET, if you do it by the book, some of the differences will still show themselves in your code.
 * 
 * But I'm trying to hide those differences as much as possible. So I'll limit myself to three common "use cases":
 * 
 * 1. Call a stored procedure or function, taking any number of input parameters, and returning a scalar value. This is 
 *    pretty straightforward, because you can just pass numbers, strings, variables, and what have you, as ordinary C# 
 *    parameters; and I'll convert them to an array of IDbDataParameter without you being any the wiser. All that matters 
 *    is that your inputs go in, and the result comes out.
 *    
 * 2. Call a procedure or function that has output parameters. This would be really easy if C#'s variable argument lists 
 *    supported ref parameters, but sadly they don't. So you'll have to pass one or two IDbDataParameter of your own, 
 *    but I'll give you a Database.Out() method to make that a little bit easier. Of course, your input parameters can 
 *    still go as they are; you dont't have to mess with IDbDataParameter objects for inputs.
 *    
 * 3. Call a procedure or function that returns structured data. That's where .NET's DataTable, DataSet and DataReader 
 *    come in. Again, you just pass your input parameters on the method call, and I'll convert them to IDbDataParameter 
 *    for you. If you find yourself with a procedure that uses every method under the sun (input parameters, output 
 *    parameters, result sets and horrendous side effects), I'll try and make it reasonably possible for you to call. 
 *    But I can only be reasonable up to a point.
 *    
 * But if you've got really bizarre stored procedures you're probably locked into your vendor's ways of doing things, 
 * in which case you might be better off forgetting about portability.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace AnyDB
{
    public partial class Database
    {
        string CurrentSchema = null;

        #region Public

        /// <summary>
        /// Invokes a stored procedure, with as many arguments as necessary, and returns the procedure's single return 
        /// value if it has one. Ideally, all parameters will be input only, in which case you can pass them as-is. If 
        /// the stored procedure has output parameters, then you will need to pass these as IDbDataParameter objects. 
        /// Use the Database.Out() method to create these IDbDataParameter objects, but remember that the output 
        /// parameter's value is returned in the .Value field, which you will need to access manually.
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as the stored procedure requires. Output parameters should be passed as IDbDataParameter 
        /// objects.
        /// </param>
        /// <returns>
        /// The stored procedure's return value. This can be the result of an explicit RETURN or EXIT statement, the 
        /// scalar result of a singleton SELECT (if it is the last statement), or the number of rows affected by an 
        /// UPDATE or DELETE (if that was the last statement executed).
        /// </returns>

        public object ProcedureCall(string procedureName, params object[] args)
        {
            return ProcedureCall(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure, with as many arguments as necessary, and returns the procedure's single return 
        /// value, converted to the specified type &lt;T&gt;. All parameters are input only, unless passed as 
        /// IDbDataParameter objects, in which case they can be output parameters. DbNull or [C#] null will probably 
        /// throw an error.
        /// </summary>
        /// <typeparam name="T">The expected type of the procedure's return value.</typeparam>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as the stored procedure requires. Output parameters should be passed as IDbDataParameter 
        /// objects.
        /// </param>
        /// <returns>
        /// The stored procedure's return value. DbNull and "No Data" will throw, unless the return type &lt;T&gt; 
        /// allows those conditions to be returned as an empty string.
        /// </returns>

        public T ProcedureCall<T>(string procedureName, params object[] args)
        {
            return ConvertToTypeT<T>(ProcedureCall(procedureName, ParameterArray(procedureName, args)));
        }

        /// <summary>
        /// Invokes a stored procedure that performs a singleton select, and returns the data in a DataRow.
        /// </summary>
        /// <param name="procedureName">
        /// The procedure to call. The procedure must return exactly one row of data. If the query returns no data, a 
        /// NoDataException is thrown. If the query returns multiple records, a MultipleDataException is thrown. You 
        /// can also handle both types of error by catching the SingletonException.
        /// </param>
        /// <param name="args">Parameters to pass to the procedure.</param>
        /// <returns>A DataRow containing the single record.</returns>
        
        public DataRow ProcedureSingleton(string procedureName, params object[] args)
        {
            return ProcedureSingleton(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure that performs a singleton select, and returns the data in an object of type
        /// &lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">A class.</typeparam>
        /// <param name="procedureName">
        /// The procedure to call. The procedure must return exactly one row of data. If the query returns no data, a 
        /// NoDataException is thrown. If the query returns multiple records, a MultipleDataException is thrown. You 
        /// can also handle both types of error by catching the SingletonException.
        /// </param>
        /// <param name="args">Parameters to pass to the procedure.</param>
        /// <returns>An object of type &lt;T&gt;.</returns>

        public T ProcedureSingleton<T>(string procedureName, params object[] args) where T : class
        {
            return ProcedureSingleton<T>(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure that performs a singleton select, and returns the data in a DataRow. If the 
        /// query returns no data, then null is returned rather than throwing.
        /// </summary>
        /// <param name="procedureName">
        /// The procedure to call. The procedure must return no more than one row of data. If the query returns 
        /// multiple records, a MultipleDataException is thrown.
        /// </param>
        /// <param name="args">Parameters to pass to the procedure.</param>
        /// <returns>A DataRow containing the single record.</returns>
        
        public DataRow ProcedureOptional(string procedureName, params object[] args)
        {
            return ProcedureOptional(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure that performs a singleton select, and returns the data in an object of type
        /// &lt;T&gt;. If the query returns no data, then null is returned rather than throwing.
        /// </summary>
        /// <typeparam name="T">A class.</typeparam>
        /// <param name="procedureName">
        /// The procedure to call. The procedure must return no more than one row of data. If the query returns 
        /// multiple records, a MultipleDataException is thrown.
        /// </param>
        /// <param name="args">Parameters to pass to the procedure.</param>
        /// <returns>An object of type &lt;T&gt;.</returns>

        public T ProcedureOptional<T>(string procedureName, params object[] args) where T : class
        {
            return ProcedureOptional<T>(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure, with as many arguments as necessary, and returns the data in a DataTable. Use 
        /// this method if the stored procedure is a front for a complex or secured SELECT statement, and you want to 
        /// use the result in the same way as you would use a SELECT from a table. This method is also to be preferred 
        /// over a DataReader, because it will clean up after itself (the DataReader places the onus on you).
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as the stored procedure needs. Output parameters should be passed as IDbDataParameter 
        /// objects.
        /// </param>
        /// <returns>A DataTable, suitable for binding, iterating, and so on.</returns>

        public DataTable ProcedureDataTable(string procedureName, params object[] args)
        {
            return ProcedureDataTable(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure, with as many arguments as you need, and returns the data in a DataSet. 
        /// The DataSet.Tables collection can be used to access the multiple query results.
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as the stored procedure needs. Output parameters should be passed as IDbDataParameter 
        /// objects.
        /// </param>
        /// <returns>
        /// A DataSet. Use the Tables collection within to access the individual DataTable for each query.
        /// </returns>

        public DataSet ProcedureDataSet(string procedureName, params object[] args)
        {
            return ProcedureDataSet(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure, with as many parameters as you need, and returns a DataReader, with which to 
        /// 'cursor' through the data. Use this method if you do not want to use a DataTable (try it, it cleans up after 
        /// itself!), or if you are going to be using a cursor to selectively update the underlying database records.
        /// This method should be called in a using block to ensure that resources are freed up after use.
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as you need. Pass any output parameters as IDbDataParameter objects.
        /// </param>
        /// <returns></returns>

        public DbDataReader ProcedureDataReader(string procedureName, params object[] args)
        {
            return ProcedureDataReader(procedureName, ParameterArray(procedureName, args));
        }

        /// <summary>
        /// Invokes a stored procedure, with as many arguments as you need, and converts the resulting DataTable into 
        /// a generic list of type T. Named columns in the DataTable are mapped to (case blind) identically named fields 
        /// or properties in the T class. Use this method if you want a "strongly typed" result set rather than a 
        /// DataTable.
        /// </summary>
        /// <param name="procedureName">Name of the stored procedure to invoke.</param>
        /// <param name="args">
        /// As many parameters as the stored procedure needs. Output parameters should be passed as IDbDataParameter 
        /// objects.
        /// </param>
        /// <returns>
        /// A List&lt;T&gt;, suitable for iterating and so on. Tip: Classes with properties can also be used for 
        /// binding.)
        /// </returns>

        public List<T> ProcedureList<T>(string procedureName, params object[] args) where T : class
        {
            return DataTableToList<T>(ProcedureDataTable(procedureName, args));
        }

        /// <summary>
        /// Creates a new IDbDataParameter, and sets its Direction to Output. Use this method if you need to receive an 
        /// output parameter's value.
        /// </summary>
        /// <param name="dbType">
        /// Data type. Use the DbType enum for this (typeof will not work).
        /// </param>
        /// <param name="size">
        /// Optional output size. If the parameter is a string, you should specify a nonzero maximum size, unless you 
        /// are happy to accept a default of 1024.
        /// </param>
        /// <returns>IDbDataParameter</returns>

        public IDbDataParameter Out(DbType dbType = DbType.String, int size = 0)
        {
            return Out(null, dbType, size);
        }

        /// <summary>
        /// Create a new IDbDataParameter, with a name, and sets its Direction to Output. Use this method if Database
        /// .ParameterNamesRequired is true, which means *you* have to name parameters.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="dbType">
        /// Data type. Use the DbType enum for this (typeof will not work).
        /// </param>
        /// <param name="size">
        /// Optional output size. If the parameter is a string, you should specify a nonzero maximum size, unless you 
        /// are happy to accept a default of 1024.
        /// </param>
        /// <returns>IDbDataParameter</returns>

        public IDbDataParameter Out(string name = null, DbType dbType = DbType.String, int size = 0)
        {
            IDbDataParameter ret = Factory.CreateParameter();
            ret.ParameterName = name;
            ret.Direction = ParameterDirection.Output;
            ret.DbType = dbType;
            ret.Size = size;
            if (ret.Size == 0)
            {
                switch (dbType)
                {
                    case System.Data.DbType.AnsiString:
                    case System.Data.DbType.AnsiStringFixedLength:
                    case System.Data.DbType.String:
                    case System.Data.DbType.StringFixedLength:
                        ret.Size = 1024;
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Creates a new IDbDataParameter, sets its direction to Input, and gives it a Value. Use this method if you 
        /// are passing a mixture of input and output parameters and want to explicitly state that this parameter is 
        /// an input.
        /// </summary>
        /// <param name="value">The input parameter's value.</param>
        /// <returns>IDbDataParameter</returns>

        public IDbDataParameter In(object value)
        {
            return In(null, value);
        }

        /// <summary>
        /// Creates a new IDbDataParameter, sets its direction to Input, and gives it a Value. Use this method if you 
        /// are passing output parameters to a stored procedure and Database.ParameterNamesRequired is true, requiring 
        /// you to name all your parameters. Damn nuisance.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">The input parameter's value.</param>
        /// <returns>IDbDataParameter</returns>
        
        public IDbDataParameter In(string name, object value)
        {
            if (value == null) value = DBNull.Value;
            IDbDataParameter ret = Factory.CreateParameter();
            ret.ParameterName = name;
            ret.Value = value;
            ret.Direction = ParameterDirection.Input;
            ret.Size = InferSize(value, 0);
            return ret;
        }

        #endregion

        #region Private

        void PossiblyThrowProcedureNotFound(string name)
        {
            if (!Driver.HasStoredProcedures)
                throw new ProcedureNotFoundException(name, new NotImplementedException(ProductName + " does not have stored procedures."), name);
        }

        /*===========================================================================================================
         * 
         * This does the boring work of wrapping up value types, strings, and what have you, into an array of 
         * IDbDataParameter objects. Of course, you can still pass in the odd IDbDataParameter that you got from the 
         * db.Out() method, because C# won't let you pass a reference into a variable argument list.
         */

        private IDbDataParameter[] ParameterArray(string name, object[] args)
        {
            /*
             * SQLite doesn't do stored procedures, but we can sort of implement "stored queries". That just leaves
             * Excel and text files at this point.
             */

            PossiblyThrowProcedureNotFound(name);

            /*
             * Some of the data providers have unbelievably slow metadata collections, and even the good ones can take
             * a long time if we're using a database with thousands and thousands of stored procedures. If that's the
             * case, then the Driver class will do part of its constructor on a thread.
             */

            Driver.WaitForBackgroundProcParams();

            /*
             * Convert objects to IDbDataParameter.
             */

            List<IDbDataParameter> prms = new List<IDbDataParameter>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is IDbDataParameter) prms.Add((IDbDataParameter)args[i]);
                else
                {
                    prms.Add(In(args[i]));
                    if (args[i] is DateTime && Driver.DateTimeFormat != null) prms[prms.Count-1].Value = args[i] = ((DateTime)args[i]).ToString(Driver.DateTimeFormat);
                    prms[prms.Count - 1].DbType = InferDbType(args[i], prms[prms.Count - 1].DbType);// DbType.String);
                    prms[prms.Count-1].Size = InferSize(args[i], 0);
                    prms[prms.Count-1].Scale = InferScale(args[i], 0);
                }
            }

            /*
             * Oracle and DB2 have to be told whether to call a procedure or function. If it's a function, add a 
             * ReturnValue parameter to the start of the list.
             */

            if (Driver.QuirkFunctionsMustHaveReturnValue)
            {
                if (Driver.dtProcedures.Select(Driver.MetaProcedureName + " = '" + name + "'").Length <= 0)
                {
                    prms.Insert(0, Out(DbType.String));
                    prms[0].Direction = ParameterDirection.ReturnValue;
                }
            }

            /*
             * Convert list to array.
             */

            return prms.ToArray();
        }

        /*===========================================================================================================
         * 
         * Invokes a stored procedure, with as many IDbDataParameter elements as you need. This method is private 
         * because the "variable argument list of objects" overload is easier to use than an array of IDbDataParameter.
         * 
         * This and the following routines assume that ParameterArray() will have been called beforehand. The important 
         * work is done by the PrepareProcedure() method. Then we just call ExecuteScalar() to get the return value.
         */

        private object ProcedureCall(string procedureName, IDbDataParameter[] args)
        {
            DbConnection connect = null;
            try
            {
                DbCommand command;
                PrepareProcedure(out command, out connect, procedureName, args);
                var ob = command.ExecuteScalar();
                if (args.Length > 0 &&
                    args[0].Direction == ParameterDirection.ReturnValue)
                {
                    return args[0].Value;
                }
                else
                    return ob;
            }
            catch (Exception ex)
            {
                ex = PossibleAnyDbException(procedureName, ex);
                if (Driver.QuirkThrowsInvalidProcedureAsTable)
                    if (ex is TableNotFoundException)
                        ex = new ProcedureNotFoundException(procedureName, ex.InnerException, ((TableNotFoundException)ex).TableName);
                throw ex;
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        }

        /*===========================================================================================================
         *
         * Invokes a stored procedure, with as many parameters as it needs, and returns the result in a DataRow. If
         * the procedure does not return exactly one row of data, an exception is thrown.
         */

        private DataRow ProcedureSingleton(string procedureName, IDbDataParameter[] args)
        {
            DataTable dt = ProcedureDataTable(procedureName, args);
            if (dt.Rows.Count < 1) throw new NoDataException(procedureName);
            if (dt.Rows.Count > 1) throw new MultipleDataException(procedureName);
            return dt.Rows[0];
        }

        /*===========================================================================================================
         *
         * Invokes a stored procedure, with as many parameters as it needs, and returns the result in a DataRow. If
         * the procedure does not return exactly one row of data, an exception is thrown.
         */

        private T ProcedureSingleton<T>(string procedureName, IDbDataParameter[] args) where T : class
        {
            List<T> list = ProcedureList<T>(procedureName, args);
            if (list.Count < 1) throw new NoDataException(procedureName);
            if (list.Count > 1) throw new MultipleDataException(procedureName);
            return list[0];
        }

        /*===========================================================================================================
         *
         * Invokes a stored procedure, with as many parameters as it needs, and returns the result in a DataRow. If
         * the procedure returns more than one row, an exception is thrown. If it returns no rows, null is returned.
         */

        private DataRow ProcedureOptional(string procedureName, IDbDataParameter[] args)
        {
            DataTable dt = ProcedureDataTable(procedureName, args);
            if (dt.Rows.Count < 1) return null;
            if (dt.Rows.Count > 1) throw new MultipleDataException(procedureName);
            return dt.Rows[0];
        }

        /*===========================================================================================================
         *
         * Invokes a stored procedure, with as many parameters as it needs, and returns the result in a DataRow. If
         * the procedure returns more than one row, an exception is thrown. If it returns no rows, null is returned.
         */

        private T ProcedureOptional<T>(string procedureName, IDbDataParameter[] args) where T : class
        {
            List<T> list = ProcedureList<T>(procedureName, args);
            if (list.Count < 1) return null;
            if (list.Count > 1) throw new MultipleDataException(procedureName);
            return list[0];
        }

        /*===========================================================================================================
         * 
         * Invokes a stored procedure, with as many parameters, etc. and returns the result of a SELECT into a DataTable. 
         * Again, variable argument list of objects is easier than constructing a IDbDataParameter array.
         * 
         * This method uses a data adapter to load the data into a single DataTable. The call to PrepareProcedure() is 
         * preceded by a horrendous Oracle hack because it has a peculiar way of passing cursors back through output 
         * parameters.
         * 
         * I should also point out that a DataTable can only contain one set of results. If you want multiple result 
         * sets, use a DataSet or DataReader instead.
         */

        private DataTable ProcedureDataTable(string procedureName, IDbDataParameter[] args)
        {
            DbConnection connect = null;
            try
            {
                DbCommand command;
                args = Driver.PossiblyAddRefCursor(procedureName, args);
                PrepareProcedure(out command, out connect, procedureName, args);
                using (DbDataAdapter adapt = Driver.CreateDataAdapter())
                {
                    DataTable dt = new DataTable();
                    adapt.SelectCommand = command;
                    try
                    {
                        adapt.Fill(dt);
                    }
                    finally
                    {
                        adapt.SelectCommand = null; // Workaround for SPlite-to-SQLite InvalidCastException
                    }
                    return dt;
                }
            }
            catch (Exception ex)
            {
                ex = PossibleAnyDbException(procedureName, ex);
                if (Driver.QuirkThrowsInvalidProcedureAsTable)
                    if (ex is TableNotFoundException)
                        ex = new ProcedureNotFoundException(procedureName, ex.InnerException, ((TableNotFoundException)ex).TableName);
                throw ex;
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        }

        /*===========================================================================================================
         * 
         * Invokes a stored procedure with parameters, and returns the result(s) in a DataSet.
         * 
         * This is more or less identical to the DataTable version, except that we're using a DataSet instead of a 
         * DataTable. I would do this with a generic and a type constraint (where), but that doesn't work.
         * 
         * Through the magic of the DataSet, we'll be able to return multiple result sets to the client, which is 
         * really neat. But bear in mind that not all databases can handle multiple results.
         */

        private DataSet ProcedureDataSet(string procedureName, IDbDataParameter[] args)
        {
            DbConnection connect = null;
            try
            {
                DbCommand command;
                args = Driver.PossiblyAddRefCursor(procedureName, args);
                PrepareProcedure(out command, out connect, procedureName, args);
                using (DbDataAdapter adapt = Driver.CreateDataAdapter())
                {
                    DataSet ds = new DataSet();
                    adapt.SelectCommand = command;
                    try
                    {
                        adapt.Fill(ds);
                    }
                    finally
                    {
                        adapt.SelectCommand = null; // Workaround for SPlite-to-SQLite InvalidCastException
                    }
                    return ds;
                }
            }
            catch (Exception ex)
            {
                ex = PossibleAnyDbException(procedureName, ex);
                if (Driver.QuirkThrowsInvalidProcedureAsTable)
                    if (ex is TableNotFoundException)
                        ex = new ProcedureNotFoundException(procedureName, ex.InnerException, ((TableNotFoundException)ex).TableName);
                throw ex;
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        }

        /*===========================================================================================================
         * 
         * Invokes a stored procedure, with as many... blah, blah, blah, and gives the caller a data reader so they can 
         * cursor through the data. Again, this one is private because I want to save you from having to manually 
         * construct arrays of IDbDataParameter.
         * 
         * A DataReader can also return multiple result sets, but a DataSet is preferable because it cleans up after 
         * itself. If you're using the DataReader, it's up to you to call the Dispose() method, preferably by way of a
         * using block.
         * 
         * We do have to be careful, though, because the Dispose() may or may not be allowed to close the connection. 
         * We don't want to close the connection inside a transaction, because that would stop subsequent operations 
         * from working.
         */

        private DbDataReader ProcedureDataReader(string procedureName, IDbDataParameter[] args)
        {
            DbConnection connect = null;
            try
            {
                CommandBehavior cb = CommandBehavior.Default;

                if (Driver.QuirkDataReaderCloseConnection)
                {
                    if (Transaction == null) cb = CommandBehavior.CloseConnection;
                }

                DbCommand command;
                args = Driver.PossiblyAddRefCursor(procedureName, args);
                PrepareProcedure(out command, out connect, procedureName, args);
                return command.ExecuteReader(cb);
            }
            catch (Exception ex)
            {
                ex = PossibleAnyDbException(procedureName, ex);
                if (Driver.QuirkThrowsInvalidProcedureAsTable)
                    if (ex is TableNotFoundException)
                        ex = new ProcedureNotFoundException(procedureName, ex.InnerException, ((TableNotFoundException)ex).TableName);
                // Do Dispose(), there is NO DATA.
                DisposeTemporaryConnection(connect);
                throw ex;
            }
            finally
            {
                // Don't Dispose(), there's data.
            }
        }

        /*===========================================================================================================
         * 
         * Set up the preliminaries for calling a stored procedure. 
         */

        private void PrepareProcedure(out DbCommand command,
                                      out DbConnection connect,
                                      string name,
                                      IDbDataParameter[] args)
        {
            /*
             * Set up the command.
             */

            CommandType commandType = Driver.BindParametersForProcedure(ref name, args);

            /*
             * Make a connection, or re-use the already open connection if we're in a transaction. These are not in a 
             * using block, because the connection needs to stay open if we're using the DataReader method. The other 
             * methods will close the connection in a finally block (unless we're in a transaction, that is, because 
             * then we really do need to keep the connection open).
             */

            connect = CreateOrReuseConnection(ConnectionString);
            command = Driver.CreateCommand();

            command.Connection = connect;
            command.Transaction = PossiblyUseTransaction();

            /*
             * PostgreSQL hack:
             * 
             * If you run a stored procedure that returns a cursor, and you are not in a transaction, you end up with
             * an 'Error 34000: cursor "<unnamed portal x>" does not exist'. This happens because Postgres creates a
             * temporary autocommit transaction for you, and the cursor gets closed when the transaction ends.
             */

            if (Driver.QuirkRefCursorRequiresTransaction)
                if (command.Transaction == null) command.Transaction = connect.BeginTransaction();

            /*
             * DB2 hack:
             * 
             * Even if you have CurrentSchema=FRED in your connection string, you are still forced to prefix all your 
             * procedure calls with the FRED schema. According to the documentation there *is* a CurrentFunctionPath 
             * configuration keyword (p. 2-29), but it hasn't got a connection string equivalent. In fact, if you have
             * CurrentFunctionPath or CurrentPath in your connection string, you'll get an InvalidArgumentException. 
             * The official solution is to run the SET CURRENT PATH command on your connection. But we can just as
             * easily insert the schema name at this point.
             */

            if (Driver.QuirkUseCurrentSchemaForProcedures && !name.Contains(".")) name = CurrentSchema + "." + name;

            command.Parameters.AddRange(args);
            command.CommandText = name;
            command.CommandType = commandType;
        }

        #endregion
    }
}