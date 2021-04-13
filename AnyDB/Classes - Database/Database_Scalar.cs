/********************************************************************************************************************
 * 
 * Database_Scalar.cs
 * 
 * Methods for getting a single value out of the database.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Returns a single value from the database using your SQL query. DbNull.Value means the data is NULL (column 
        /// has no data), C# null means there is no data (row does not exist).
        /// </summary>
        /// <param name="SelectStatement">
        /// An SQL SELECT statement, which can (and should) include parameter markers.
        /// </param>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement. You will need as many actual parameters 
        /// as your SQL statement has formal parameter markers, even if your SQL statement uses named markers.
        /// </param>
        /// <returns>
        /// The first column of the first row in the result set returned by the query. All other rows and columns are 
        /// ignored. [SQL] NULL is returned as DbNull.Value. "No Data" is returned as [C#] null.
        /// </returns>

        public object GetScalar(string SelectStatement, params object[] QueryParameters)
        {
            string sql = SelectStatement;
            DbConnection connect = null;
            try
            {
                sql = RewriteQuery(sql);

                using (var command = Driver.CreateCommand())
                {
                    BindParameters(command, ref sql, QueryParameters);

                    connect = CreateOrReuseConnection(ConnectionString);
                    command.CommandText = sql;
                    command.Connection = connect;
                    command.Transaction = PossiblyUseTransaction();

                    object ob = command.ExecuteScalar();
                    if (Driver.QuirkPaddedStrings && ob is string) ob = ob.ToString().TrimEnd();
                    return ob;
                }
            }
            catch (Exception ex)
            {
                if (Driver.QuirkThrowsInvalidColumnAsParameter)
                {
                    var i = ex.Message.IndexOf("Too few parameters. Expected");
                    if (i < 0) i = ex.Message.IndexOf("No value given for one or more required parameters");
                    if (i >= 0)
                        throw new ColumnNotFoundException(sql, ex, null, ex.Message.Substring(i));
                }
                throw PossibleAnyDbException(sql, ex);
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        }

        /// <summary>
        /// Returns a single value from the database using your SQL query, and converts it to the specified type 
        /// &lt;T&gt;. [SQL] NULL and [C#] null will throw, unless the return type &lt;T&gt; is string, in which case 
        /// an empty string is returned instead.
        /// </summary>
        /// <typeparam name="T">Data type of the data to return.</typeparam>
        /// <param name="selectStatement">
        /// An SQL SELECT statement, which can (and should) include parameter markers.
        /// </param>
        /// <param name="queryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement. You will need as many actual parameters as 
        /// your SQL statement has formal parameter markers, even if your SQL statement uses named markers.
        /// </param>
        /// <returns>
        /// The first column of the first row in the result set returned by the query. All otherrows and columns will 
        /// be ignored.
        /// </returns>
        /// <exception cref="InvalidCastException">The data is NULL, or cannot be converted.</exception>
        /// <exception cref="KeyNotFoundException">The query returned no data.</exception>

        public T GetScalar<T>(string selectStatement, params object[] queryParameters)
        {
            try
            {
                return ConvertToTypeT<T>(GetScalar(selectStatement, queryParameters));
            }
            catch(NoDataException)
            {
                throw new NoDataException(selectStatement);
            }
        }

        /*===========================================================================================================
         * 
         * Converts an object to type T. A simple cast very often doesn't work, for example float to int is annoyingly 
         * stupid. And the TypeConverter object cannot even convert a DateTime to a DateTime.
         */

        internal T ConvertToTypeT<T>(object ob)
        {
            /*
             * If the generic <T> is <string>, then return NULL and No Data as an empty string.
             */

            if (ob == DBNull.Value || ob == null)
            {
                if ("" is T)
                {
                    ob = "";
                    return (T)ob;
                }
                if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    return default(T);
                }
            }

            /*
             * Is type conversion required? Don't try to use TypeConverter if it's already the right type because it'll 
             * fail miserably! NULL and null cannot be converted to a value type, so throw an error if that happens.
             */

            if (!(ob is T))
            {
                if (ob == null) throw new NoDataException("The query did not return any data.");
                bool isDateTime = typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?);
                if (isDateTime && ob is string)
                {
                    ob = DateTime.Parse(ob.ToString());
                }
                else if (typeof(T).IsEnum && ob is string)
                {
                    ob = Enum.Parse(typeof(T), ob.ToString());
                }
                else
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
                    ob = tc.ConvertTo(ob, typeof(T));
                }
            }

            /*
             * The ob variable is an *object* of type T. The cast is required because the compiler insists it's an 
             * *object*, not a T. Life's so much simpler in C.
             */

            return (T)ob;
        }
    }
}