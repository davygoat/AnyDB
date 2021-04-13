/*===================================================================================================================
 * 
 * Database_Singleton.cs
 * 
 * Methods for performing a singleton select, i.e. a query that returns exactly one row of data. If no data is found,
 * or if the query results in multiple records, an exception is thrown.
 */

using System.Data;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Performs a singleton select and returns the data as a DataRow. The query must return EXACTLY one row.
        /// </summary>
        /// <param name="SelectStatement">
        /// Parameterised SQL query. The query must return exactly one row. This is normally achieved by making use of
        /// the table's PRIMARY KEY or a UNIQUE INDEX, or by using LIMIT/TOP syntax with an ORDER BY to retrieve the 
        /// 'latest' record. If the query returns no data, a NoDataException is thrown. If the query returns multiple
        /// records, a MultipleDataException is thrown. You can also handle both types of error by catching the
        /// SingletonException.
        /// </param>
        /// <param name="QueryParameters">Parameters to bind to the SQL query.</param>
        /// <returns>A DataRow containing the single record.</returns>
        public DataRow GetSingleton(string SelectStatement, params object[] QueryParameters)
        {
            DataTable dt = GetDataTable(SelectStatement, QueryParameters);
            if (dt.Rows.Count < 1) throw new NoDataException(SelectStatement);
            if (dt.Rows.Count > 1) throw new MultipleDataException(SelectStatement);
            return dt.Rows[0];
        }

        /// <summary>
        /// Performs a singleton select, and returns the result in an object of type &lt;T&gt;. The query must return
        /// EXACTLY one row.
        /// </summary>
        /// <typeparam name="T">A class.</typeparam>
        /// <param name="SelectStatement">
        /// Parameterised SQL query. The query must return exactly one row. This is normally achieved by making use of
        /// the table's PRIMARY KEY or a UNIQUE INDEX, or by using LIMIT/TOP syntax with an ORDER BY to retrieve the 
        /// 'latest' record. If the query returns no data, a NoDataException is thrown. If the query returns multiple
        /// records, a MultipleDataException is thrown. You can also handle both types of error by catching the
        /// SingletonException.
        /// </param>
        /// <param name="QueryParameters">Parameters to bind to the SQL query.</param>
        /// <returns></returns>
        public T GetSingleton<T>(string SelectStatement, params object[] QueryParameters) where T : class
        {
            DataTable dt = GetDataTable(SelectStatement, QueryParameters);
            if (dt.Rows.Count < 1) throw new NoDataException(SelectStatement);
            if (dt.Rows.Count > 1) throw new MultipleDataException(SelectStatement);
            return DataTableToList<T>(dt)[0];
        }

        /// <summary>
        /// Performs a singleton select and returns the data as a DataRow. The query may return zero rows, in which
        /// null is returned.
        /// </summary>
        /// <param name="SelectStatement">
        /// Parameterised SQL query. The query must return one or zero rows. This is normally achieved by making use of
        /// the table's PRIMARY KEY or a UNIQUE INDEX, or by using LIMIT/TOP syntax with an ORDER BY to retrieve the 
        /// 'latest' record. If the query returns returns no data, null is returned. If multiple records, then a 
        /// MultipleDataException is thrown.
        /// </param>
        /// <param name="QueryParameters">Parameters to bind to the SQL query.</param>
        /// <returns>A DataRow containing the single record, or null if there is no data.</returns>
        public DataRow GetOptional(string SelectStatement, params object[] QueryParameters)
        {
            DataTable dt = GetDataTable(SelectStatement, QueryParameters);
            if (dt.Rows.Count < 1) return null;
            if (dt.Rows.Count > 1) throw new MultipleDataException(SelectStatement);
            return dt.Rows[0];
        }

        /// <summary>
        /// Performs a singleton select, and returns the result in an object of type &lt;T&gt;.
        /// </summary>
        /// <typeparam name="T">A class.</typeparam>
        /// <param name="SelectStatement">
        /// Parameterised SQL query. The query must return one or zero rows. This is normally achieved by making use of
        /// the table's PRIMARY KEY or a UNIQUE INDEX, or by using LIMIT/TOP syntax with an ORDER BY to retrieve the 
        /// 'latest' record. If the query returns no data, null is returned. If the query returns multiple records, a 
        /// MultipleDataException is thrown.
        /// </param>
        /// <param name="QueryParameters">Parameters to bind to the SQL query.</param>
        /// <returns></returns>
        public T GetOptional<T>(string SelectStatement, params object[] QueryParameters) where T : class
        {
            DataTable dt = GetDataTable(SelectStatement, QueryParameters);
            if (dt.Rows.Count < 1) return null;
            if (dt.Rows.Count > 1) throw new MultipleDataException(SelectStatement);
            return DataTableToList<T>(dt)[0];
        }
    }
}