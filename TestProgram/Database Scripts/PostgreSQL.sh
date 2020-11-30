#!/bin/sh

# vim:set ts=4:

# PostgreSQL #############################################################
#
# grep listen_addr /etc/postgresql/9.4/main/postgresql.conf # must be '*'
#

sudo su - postgres <<EOD
psql -c "CREATE USER david;"
psql -c "CREATE USER test WITH PASSWORD 'lemming';"
psql -c "CREATE DATABASE anydb WITH OWNER david;"
EOD


psql anydb <<EOD

BEGIN TRANSACTION;

   CREATE TABLE anydb_numbers
   (
      number_text  VARCHAR(80) NOT NULL,
      PRIMARY KEY (number_text)
   );

   INSERT INTO AnyDB_numbers VALUES ('One');
   INSERT INTO AnyDB_numbers VALUES ('Two');
   INSERT INTO AnyDB_numbers VALUES ('Three');
   INSERT INTO AnyDB_numbers VALUES ('Four');
   INSERT INTO AnyDB_numbers VALUES ('Five');
   INSERT INTO AnyDB_numbers VALUES ('Six');
   INSERT INTO AnyDB_numbers VALUES ('Seven');
   INSERT INTO AnyDB_numbers VALUES ('Eight');
   INSERT INTO AnyDB_numbers VALUES ('Nine');
   INSERT INTO AnyDB_numbers VALUES ('Ten');
   INSERT INTO AnyDB_numbers VALUES ('OneOneOne');

   CREATE TABLE anydb_test
   (
      column_one   INTEGER          NOT NULL,
      column_two   VARCHAR(80)      NOT NULL,
      column_three TIMESTAMP        NOT NULL,
      column_four  DOUBLE PRECISION NOT NULL,
      PRIMARY KEY (column_one),
      FOREIGN KEY (column_two) REFERENCES anydb_numbers,
	  UNIQUE      (column_two),
      CHECK       (column_four > 0)
   );

   CREATE FUNCTION anydb_testprocedure1 (iKey INTEGER,
                                         fAdd FLOAT)
   RETURNS FLOAT AS
   \$\$
   DECLARE retVal FLOAT;
   BEGIN

      SELECT column_four + fAdd
      INTO   retVal
      FROM   anydb_test
      WHERE  column_one = iKey;

      RETURN retVal;

   END
   \$\$ LANGUAGE plpgsql;


   CREATE FUNCTION anydb_testprocedure2 (iInput INTEGER,
                                         sOutputStr OUT VARCHAR(80),
                                         fOutputFloat OUT FLOAT) AS
   \$\$
   BEGIN

      SELECT column_two,
             column_four
      INTO   sOutputStr,
             fOutputFloat
      FROM   anydb_test
      WHERE  column_one = iInput;

   END
   \$\$ LANGUAGE plpgsql;


   CREATE FUNCTION anydb_testprocedure3 ()
   RETURNS REFCURSOR AS 
   \$\$
   DECLARE cur REFCURSOR;
   BEGIN

      OPEN cur FOR
      SELECT *
      FROM   anydb_test;

      RETURN cur;

   END
   \$\$ LANGUAGE plpgsql;

   CREATE FUNCTION anydb_testprocedure4 ()
   RETURNS SETOF REFCURSOR AS
   \$\$
   DECLARE
      cur1 REFCURSOR;
      cur2 REFCURSOR;
   BEGIN

      OPEN cur1 FOR
      SELECT   *
      FROM     anydb_test
      ORDER BY column_one ASC;

      RETURN NEXT cur1;

      OPEN cur2 FOR
      SELECT   *
      FROM     anydb_test
      ORDER BY column_one DESC;

      RETURN NEXT cur2;

   END
   \$\$ LANGUAGE plpgsql;

   GRANT SELECT,INSERT,UPDATE,DELETE ON anydb_test TO test;
   --GRANT EXECUTE ON FUNCTION anydb_testprocedure1 TO test;
   --GRANT EXECUTE ON FUNCTION anydb_testprocedure2 TO test;
   --GRANT EXECUTE ON FUNCTION anydb_testprocedure3 TO test;
   --GRANT EXECUTE ON FUNCTION anydb_testprocedure4 TO test;

COMMIT;

EOD
