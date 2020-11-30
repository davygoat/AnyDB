#!/bin/sh

# vim:set ts=4:

mkdir cubrid/
cd cubrid/

cubrid createdb AnyDB
cubrid server start AnyDB
cubrid service start

csql -u dba AnyDB <<-EOD

	CREATE USER test PASSWORD 'lemming';

	CREATE TABLE AnyDB_numbers
	(
	   number_text VARCHAR(80) NOT NULL PRIMARY KEY
	);

	INSERT INTO AnyDB_numbers VALUES ('One');
	INSERT INTO AnyDB_numbers VALUES ('Two');
	INSERT INTO AnyDB_numbers VALUES ('Three');
	INSERT INTO AnyDB_numbers VALUES ('Four');
	INSERT INTO AnyDB_numbers VALUES ('Five');
	INSERT INTO AnyDB_numbers VALUES ('OneOneOne');

	GRANT SELECT ON AnyDB_numbers TO PUBLIC;

EOD

cd ..

cat >AnyDB.java <<-EOD

	import java.sql.*;
	import cubrid.jdbc.driver.*;
	
	public class AnyDB
	{
	   public static double AnyDB_testProcedure1 (int iKey,
	                                              double fAdd)
	   {
	      double ret = -1;
	      try
	      { 
	         String sql = "SELECT column_four + " + fAdd + " " +
	                      "FROM   AnyDB_test " +
	                      "WHERE  column_one = " + iKey;
	         Class.forName ("cubrid.jdbc.driver.CUBRIDDriver");
	         Connection con = DriverManager.getConnection("jdbc:default:connection");
	         Statement stmt = con.createStatement();
	         ResultSet rs = stmt.executeQuery(sql);
	         while (rs.next())
	         {
	            ret = rs.getDouble(1);
	            break;
	         }
	         stmt.close();
	         con.close();
	      }
	      catch(Exception e)
	      {
	         e.printStackTrace();
	         System.err.println("Exception: " + e.getMessage());
	      }
	      return ret;
	   } 

	   /*
	    * This does not work:
	    *
	    * "ERROR: Invalid call: it can not return ResultSet."
	    *
	    * The manual mentions that "An error also occurs when calling a 
	    * function that returns ResultSet in a non-Java environment."
	    *
	    * So I'm not even going to attempt AnyDB_testProcedure4().
	    */

	   public static ResultSet AnyDB_testProcedure3()
	   {
	      try
	      { 
	         String sql = "SELECT * FROM AnyDB_test";
	         Class.forName ("cubrid.jdbc.driver.CUBRIDDriver");
	         Connection con = DriverManager.getConnection("jdbc:default:connection");
	         Statement stmt = con.createStatement();
	         ResultSet rs = stmt.executeQuery(sql);
	         ((CUBRIDResultSet)rs).setReturnable();
	         return rs;
	      }
	      catch(Exception e)
	      {
	         e.printStackTrace();
	         System.err.println("SQLException: " + e.getMessage());
	      }
	      return null;
	   }
	}
EOD

CLASSPATH=cubrid/CUBRID/jdbc/cubrid_jdbc.jar javac AnyDB.java
loadjava AnyDB AnyDB.class 

csql AnyDB <<-EOD

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER     NOT NULL PRIMARY KEY,
	   column_two   VARCHAR(80) NOT NULL UNIQUE,
	   column_three DATETIME    NOT NULL,
	   column_four  DOUBLE PRECISION,
	   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers,
	   CHECK       (column_four > 0)
	);

	GRANT SELECT,INSERT,UPDATE,DELETE ON AnyDB_test TO test;

	CREATE FUNCTION AnyDB_testProcedure1 (iKey INTEGER,
	                                      fAdd DOUBLE PRECISION)
	RETURN DOUBLE
	AS LANGUAGE JAVA
	NAME 'AnyDB.AnyDB_testProcedure1(int, double) returns double';

	CREATE FUNCTION AnyDB_testProcedure3 ()
	RETURN CURSOR
	AS LANGUAGE JAVA
	NAME 'AnyDB.AnyDB_testProcedure3() returns java.sql.ResultSet';

EOD

csql -u dba AnyDB <<-EOD

	REVOKE SELECT ON AnyDB_numbers FROM PUBLIC;

EOD
