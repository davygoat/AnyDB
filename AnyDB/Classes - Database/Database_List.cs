/********************************************************************************************************************
 * 
 * Database_List.cs
 * 
 * A method that calls GetDataTable() and converts the resulting data to a list of strongly typed objects. This method 
 * uses reflection to make DataTable columns to fields or properties. Once you have the data in a strongly typed list, 
 * you can run LINQ and lambdas over them to your heart's content.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;

namespace AnyDB
{
    public partial class Database
    {
        /// <summary>
        /// Returns the result of a SELECT statement in a generic List&lt;T&gt;
        /// </summary>
        /// <param name="SelectStatement">
        /// An SQL SELECT statement, which can (and should) include parameter markers.
        /// </param>
        /// <param name="QueryParameters">
        /// Parameters to bind to parameter markers in the SELECT statement. You will need as many actual parameters as 
        /// your SQL statement has formal parameter markers, even if your SQL  statement uses named markers.
        /// </param>
        /// <returns>A List&lt;T&gt; holding the results of your query.</returns>

        public List<T> GetList<T>(string SelectStatement, params object[] QueryParameters) where T : class
        {
            return DataTableToList<T>(GetDataTable(SelectStatement, QueryParameters));
        }

        /// <summary>
        /// Converts a DataTable to a generic list of classes of type T.
        /// </summary>
        /// <typeparam name="T">The destination type.</typeparam>
        /// <param name="dt">DataTable to convert to a List&lt;T&gt;.</param>
        /// <returns></returns>

        public static List<T> DataTableToList<T>(DataTable dt)
        {
            /*
             * Can't see this ever happening, but it's best to be prepared.
             */

            if (dt == null) return null;

            /*
             * Get the required field or property info. You never know whether you're looking at a field or a property, 
             * so you always have to check. We can also handle underscore to mixed case conversion, but we're not going 
             * to bother trying the reverse since that would be unlikely to see much use.
             */

            BindingFlags nocase = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;
            Dictionary<string, object> reflect = new Dictionary<string, object>();
            foreach (DataColumn dc in dt.Columns)
            {
                string fld = dc.ColumnName;
                for (int i = 0; i < 2; i++)
                {
                    // Check if it's a field (case blind).
                    var fi = typeof(T).GetField(fld, nocase);
                    if (fi != null)
                    {
                        reflect[dc.ColumnName] = fi;
                        break;
                    }
                    // No? Check if it's a property (case blind).
                    var pi = typeof(T).GetProperty(fld, nocase);
                    if (pi != null)
                    {
                        reflect[dc.ColumnName] = pi;
                        break;
                    }
                    // If that didn't work either, try it without underscores.
                    fld = fld.Replace("_", "");
                }
            }

            /*
             * Find the default parameterless constructor. We can't call new() without imposing a constraint on the
             * generic type T. If we impose a "where T : new()" constraint, then that causes problems if we want to
             * return a null later because the new() constraint implies not null.
             */

            object[] noparams = new object[0];
            Type[] notypes = new Type[0];
            ConstructorInfo construct = typeof(T).GetConstructor(notypes);
            
            /*
             * Now convert each DataRow to a T object, using Reflection to set fields/properties.
             */

            List<T> ret = new List<T>();
            foreach (DataRow dr in dt.Rows)
            {
                T elem = (T)construct.Invoke(noparams);
                foreach (DataColumn dc in dt.Columns)
                {
                    if (dr[dc.ColumnName] != DBNull.Value && reflect.ContainsKey(dc.ColumnName) == true)
                    {
                        var mi = reflect[dc.ColumnName];
                        if (mi is FieldInfo)
                            SetField(elem, (FieldInfo)mi, dr[dc.ColumnName]);
                        else
                            SetProperty(elem, (PropertyInfo)mi, dr[dc.ColumnName]);
                    }
                }
                ret.Add(elem);
            }
            return ret;
        }

        private static void SetField(object DestinationObject, FieldInfo fi, object Value)
        {
            object ob = Value;
            if (fi.FieldType != ob.GetType())
            {
                if (fi.FieldType.IsEnum && ob is string)
                {
                    if (ob != null && ob.ToString() != "") ob = Enum.Parse(fi.FieldType, ob.ToString());
                }
                else if (fi.FieldType == typeof(DateTime) && ob is string)
                {
                    if (ob != null && ob.ToString() != "") ob = DateTime.Parse(ob.ToString());
                    else return;
                }
                else
                {
                    Type tt = IsNullableType(fi.FieldType) ? Nullable.GetUnderlyingType(fi.FieldType) : fi.FieldType;
                    if (tt.IsEnum && ob != null && ob.ToString() != "") ob = Enum.Parse(tt, ob.ToString());
                    else ob = Convert.ChangeType(ob, tt);
                }
            }
            fi.SetValue(DestinationObject, ob);
        }

        private static void SetProperty(object DestinationObject, PropertyInfo pi, object Value)
        {
            object ob = Value;
            if (pi.PropertyType != ob.GetType())
            {
                if (pi.PropertyType.IsEnum && ob is string)
                {
                    if (ob != null && ob.ToString() != "") ob = Enum.Parse(pi.PropertyType, ob.ToString());
                }
                else if (pi.PropertyType == typeof(DateTime) && ob is string)
                {
                    if (ob != null && ob.ToString() != "") ob = DateTime.Parse(ob.ToString());
                    else return;
                }
                else
                {
                    Type tt = IsNullableType(pi.PropertyType) ? Nullable.GetUnderlyingType(pi.PropertyType) : pi.PropertyType;
                    if (tt.IsEnum && ob != null && ob.ToString() != "") ob = Enum.Parse(tt, ob.ToString());
                    else ob = Convert.ChangeType(ob, tt);
                }
            }
            pi.SetValue(DestinationObject, ob, null);
        }

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }
    }
}