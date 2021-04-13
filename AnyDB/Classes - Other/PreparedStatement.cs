using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace AnyDB
{
    /// <summary>
    /// This class lets you run a parameterised SQL statement multiple times with less overhead.
    /// </summary>
    public class PreparedStatement : IDisposable
    {
        internal Database db;
        internal DbCommand command;
        internal string original;

        internal PreparedStatement(Database db, DbCommand command, string original)
        {
            var con = command.Connection.State;
            this.db = db;
            this.command = command;
            this.original = original;
            this.command.Prepare();
        }

        /// <summary>
        /// Executes the prepared statement using the supplied parameters as input.
        /// </summary>
        /// <param name="QueryParameters">
        /// Parameters to pass to the SQL statement. The number and type of parameters must match exactly
        /// those of the previously prepared statement.
        /// </param>
        /// <returns>Typically the number of rows affected by the UPDATE statement.</returns>

        public int ExecuteNonQuery(params object[] QueryParameters)
        {
            if (QueryParameters.Length != command.Parameters.Count)
                throw new Exception("The number of query parameters does not correspond with the prepared statement.");
            string sql = original;
            db.BindParameters(command, ref sql, QueryParameters);
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Returns the result of the prepared statement in a DataTable.
        /// </summary>
        /// <param name="QueryParameters">
        /// Parameters to pass to the SQL statement. The number and type of parameters must match exactly
        /// those of the previously prepared statement.
        /// </param>
        /// <returns>A DataTable holding the results of your query.</returns>
        
        public DataTable GetDataTable(params object[] QueryParameters)
        {
            if (QueryParameters.Length != command.Parameters.Count)
                throw new Exception("The number of query parameters does not correspond with the prepared statement.");
            string sql = original;
            db.BindParameters(command, ref sql, QueryParameters);
            try
            {
                using (var adapt = db.Driver.CreateDataAdapter())
                {
                    adapt.SelectCommand = command;
                    adapt.SelectCommand.Connection = command.Connection;
                    DataTable dt = new DataTable();
                    adapt.Fill(dt);
                    return dt;
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Returns the result of the prepared statement in a generic list of type T.
        /// </summary>
        /// <param name="QueryParameters">
        /// Parameters to pass to the SQL statement. The number and type of parameters must match exactly
        /// those of the previously prepared statement.
        /// </param>
        /// <returns>A List&lt;T&gt; holding the results of your query.</returns>
        
        public List<T> GetList<T>(params object[] QueryParameters) where T : class
        {
            DataTable dt = GetDataTable(QueryParameters);
            return Database.DataTableToList<T>(dt);
        }

        /// <summary>
        /// Returns a single value from your prepared statement.
        /// </summary>
        /// <param name="QueryParameters">
        /// Parameters to pass to the SQL statement. The number and type of parameters must match exactly
        /// those of the previously prepared statement.
        /// </param>
        /// <returns>
        /// The first column of the first row in the result set returned by the query. All other 
        /// rows and columns are ignored. [SQL] NULL is returned as DbNull.Value. "No Data" is
        /// returned as [C#] null.
        /// </returns>
        
        public object GetScalar(params object[] QueryParameters)
        {
            if (QueryParameters.Length != command.Parameters.Count)
                throw new Exception("The number of query parameters does not correspond with the prepared statement.");
            string sql = original;
            db.BindParameters(command, ref sql, QueryParameters);
            return command.ExecuteScalar();
        }

        /// <summary>
        /// Returns a single value from the database using your SQL query, and converts it to
        /// the specified type &lt;T&gt;. [SQL] NULL and [C#] null will throw, unless the return
        /// type &lt;T&gt; is string, in which case an empty string will be returned instead.
        /// </summary>
        /// <typeparam name="T">Data type of the data to return.</typeparam>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement. You will need as many
        /// actual parameters as your SQL statement has formal parameter markers, even if your SQL 
        /// statement uses named markers.
        /// </param>
        /// <returns>
        /// The first column of the first row in the result set returned by the query. All other
        /// rows and columns will be ignored.
        /// </returns>
        /// <exception cref="InvalidCastException">The data is NULL, or cannot be converted.</exception>
        /// <exception cref="KeyNotFoundException">The query returned no data.</exception>
        
        public T GetScalar<T>(params object[] QueryParameters)
        {
            return db.ConvertToTypeT<T>(GetScalar(QueryParameters));
        }

        /// <summary>
        /// Disposes the prepared statement, closing the connection and releasing associated memory.
        /// </summary>
        
        public void Dispose()
        {
            if (command != null)
            {
                command.Connection.Close();
                command.Connection = null;
                command = null;
                db = null;
                original = null;
            }
        }
    }
}