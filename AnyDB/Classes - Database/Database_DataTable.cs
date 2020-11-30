/********************************************************************************************************************
 * 
 * Database_DataTable.cs
 * 
 * Methods for returning the result of a SELECT statement in a DataTable. Although a DataTable can be awkward to use 
 * (lots of type conversions, casting or ToString() methods), it does have the advantage of being easy to bind to a 
 * GridView, DataGridView or Chart. It is also very reliable,  and less error prone than using a DataReader or rolling 
 * your own Command/Connect read sequence.
 */

using System;
using System.Data;
using System.Data.Common;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Returns the result of an SELECT statement in a DataTable. This is a quick and simple way to get data out 
        /// of a database. The next step is to bind it to some display object. 
        /// </summary>
        /// <param name="SelectStatement">
        /// An SQL SELECT statement, which can (and should) include parameter markers.
        /// </param>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement. You will need as many actual parameters as 
        /// your SQL statement has formal parameter markers, even if your SQL statement uses named markers.
        /// </param>
        /// <returns>A DataTable holding the results of your query.</returns>

        public DataTable GetDataTable(string SelectStatement, params object[] QueryParameters)
        {
            string sql = SelectStatement;
            DbConnection connect = null;

            try
            {
                sql = RewriteQuery(SelectStatement);          // text driver throws invalid table here

                using (var command = Driver.CreateCommand())
                {
                    BindParameters(command, ref sql, QueryParameters);

                    connect = CreateOrReuseConnection(ConnectionString);

                    using (var adapt = Driver.CreateDataAdapter())
                    {
                        adapt.SelectCommand = command;
                        adapt.SelectCommand.Connection = connect;
                        adapt.SelectCommand.CommandText = sql;
                        adapt.SelectCommand.Transaction = PossiblyUseTransaction();
                        try
                        {
                            DataTable dt = new DataTable();
                            adapt.Fill(dt);
                            if (Driver.QuirkPaddedStrings) TrimDataTableStrings(dt);
                            return dt;
                        }
                        finally
                        {
                            adapt.SelectCommand = null; // Workaround for SPlite-to-SQLite InvalidCastException
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw PossibleAnyDbException(sql, ex);
            }
            finally
            {
                DisposeTemporaryConnection(connect);
            }
        
        }

        DataTable TrimDataTableStrings(DataTable dt)
        {
            int ncols = dt.Columns.Count;
            foreach(DataRow dr in dt.Rows)
                for (int i=0; i<ncols; i++)
                    if (dr[i] is string) dr[i] = dr[i].ToString().TrimEnd();
            return dt;
        }

        DataTable DataTableSelect(DataTable dtOrig, string select, string sort)
        {
            DataTable dtNew = new DataTable();
            foreach (DataColumn dc in dtOrig.Columns) dtNew.Columns.Add(dc.ColumnName, dc.DataType);
            foreach (DataRow drOrig in dtOrig.Select(select,sort))
            {
                DataRow drNew = dtNew.NewRow();
                for (int i = 0; i < dtOrig.Columns.Count; i++) drNew[i] = drOrig[i];
                dtNew.Rows.Add(drNew);
            }
            return dtNew;
        }
    }
}