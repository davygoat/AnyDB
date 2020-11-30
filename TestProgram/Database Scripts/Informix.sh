#!/bin/sh

set -e

# vim:set ts=4:

# Informix ######################################################################
#
# If you can't connect to the server, check that DRDA is configured.
#
# $ grep dr_informix /etc/services ==> it's port 9089 in my VirtualBox
# $ cat $INFORMIXSQLHOSTS          ==> dr_informix1210 MUST listen on ip 0.0.0.0
# # onmode -k ; oninit
#

dbaccess <<EOD

	CREATE DATABASE AnyDB;

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

	REVOKE SELECT ON AnyDB_numbers FROM PUBLIC;

	CREATE TABLE AnyDB_test
	(
	   column_one   INTEGER                      NOT NULL,
	   column_two   VARCHAR(80)                  NOT NULL,
	   column_three DATETIME YEAR TO FRACTION(5) NOT NULL,
	   column_four  DOUBLE PRECISION             NOT NULL,
	   PRIMARY KEY (column_one),
	   FOREIGN KEY (column_two) REFERENCES AnyDB_numbers,
	   UNIQUE      (column_two),
	   CHECK       (column_four > 0)
	);

	CREATE PROCEDURE AnyDB_testProcedure1 (iKey INTEGER,
	                                       fAdd DOUBLE PRECISION)
	   RETURNING DOUBLE PRECISION;

	   DEFINE ret DOUBLE PRECISION;

	   SELECT  column_four + fAdd
	   INTO    ret
	   FROM    AnyDB_test
	   WHERE   column_one = iKey;

	   RETURN ret;

	END PROCEDURE;

	CREATE PROCEDURE AnyDB_testProcedure2 (    iInput       INTEGER,
                                           OUT sOutputStr   VARCHAR(80),
                                           OUT sOutputFloat REAL)

	   SELECT column_two,
			  column_four
	   INTO   sOutputStr,
			  sOutputFloat
	   FROM   AnyDB_test
	   WHERE  column_one = iInput;

	END PROCEDURE;

	CREATE PROCEDURE AnyDB_testProcedure3()

	   RETURNING INTEGER                      AS column_one,
	             VARCHAR(80)                  AS column_two,
	             DATETIME YEAR TO FRACTION(5) AS column_three,
	             DOUBLE PRECISION             AS column_four;

	   DEFINE ret_one   INTEGER;
	   DEFINE ret_two   VARCHAR(80);
	   DEFINE ret_three DATETIME YEAR TO FRACTION(5);
	   DEFINE ret_four  DOUBLE PRECISION;

	   FOREACH SELECT  *
	           INTO    ret_one,
	                   ret_two,
	                   ret_three,
	                   ret_four
	           FROM    AnyDB_test

	      RETURN ret_one,
	             ret_two,
	             ret_three,
	             ret_four
	      WITH RESUME;

	   END FOREACH;

	END PROCEDURE;

	CREATE PROCEDURE AnyDB_testProcedure4()

	   -- Informix cannot return multiple result sets

	   RETURNING INTEGER                      AS column_one,
	             VARCHAR(80)                  AS column_two,
	             DATETIME YEAR TO FRACTION(5) AS column_three,
	             DOUBLE PRECISION             AS column_four;

	   DEFINE ret_one   INTEGER;
	   DEFINE ret_two   VARCHAR(80);
	   DEFINE ret_three DATETIME YEAR TO FRACTION(5);
	   DEFINE ret_four  DOUBLE PRECISION;

	   FOREACH SELECT   *
	           INTO     ret_one,
	                    ret_two,
	                    ret_three,
	                    ret_four
	           FROM     AnyDB_test
	           ORDER BY column_one

	      RETURN ret_one,
	             ret_two,
	             ret_three,
	             ret_four
	      WITH RESUME;

	   END FOREACH;

	END PROCEDURE;

	GRANT CONNECT TO test;

EOD

#
# Enable transactions.
#

echo
echo "We need to enable logging for transactions to work."
echo "This requires root privilege."
echo

sudo su <<EOD
	. /opt/ibm/informix/ol_informix1210.ksh
	ondblog buf AnyDB
	ontape -s -B AnyDB
EOD
