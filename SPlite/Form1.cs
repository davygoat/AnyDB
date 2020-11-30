using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SPlite
{
    public partial class Form1 : Form
    {
        private const string NewProcedure = "(New Procedure)";

        private string FileName = null;
        private string ProcedureName = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            lbProcedures.BackColor = this.BackColor;
            tbCreateProcedure.BackColor = this.BackColor;
            lblFileName.TextAlign = ContentAlignment.MiddleRight;
        }

        /*=================================================================================================
         * 
         * File/Open...
         * 
         * Open an SQLite database and display its "stored procedures". Preselect the first one.
         */

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dlgFileOpen.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string fnam = dlgFileOpen.FileName;
                    DataTable dtProcs = null;
                    SPliteProcs.DoTransaction(fnam, tx =>
                    {
                        SPliteProcs.PossiblyCreateTable(tx);
                        SPliteProcs.PossiblyCreateTriggers(tx);
                        dtProcs = SPliteProcs.ReadAllProcedures(tx);
                        dtProcs.Rows.Add(NewProcedure);
                        tx.Commit();
                    });
                    this.FileName = fnam;
                    lblFileName.Text = fnam;
                    ShowProcedureList(dtProcs);
                }
                catch(Exception ex)
                {
                    PopupException(ex);
                }
            }
        }

        /*=================================================================================================
         * 
         * ListBox.SelectedIndexChanged event.
         * 
         * If we selected the name of a stored procedure, show the procedure's definition for editing. If 
         * we selected "New Procedure", produce a template to start off with.
         */

        private void lbProcedures_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var drv = lbProcedures.SelectedItem as DataRowView;
                var row = drv.Row;
                if (row != null)
                {
                    var name = row["name"].ToString();
                    string sql = null;
                    if (name == NewProcedure)
                    {
                        ProcedureName = null;
                        sql = SPliteProcs.DummyCreateProcedure();
                        btnDelete.Enabled = false;
                    }
                    else
                    {
                        SPliteProcs.DoTransaction(FileName, tx =>
                        {
                            sql = SPliteProcs.GetProcedureText(tx, name);
                            tx.Rollback();
                        });
                        ProcedureName = name;
                        btnDelete.Enabled = true;
                    }
                    tbCreateProcedure.Text = TextBoxIfy(sql);
                    panel1.Visible = true;
                    btnSave.Enabled = false;
                }
                else
                {
                    panel1.Visible = false;
                }
            }
            catch(Exception ex)
            {
                PopupException(ex);
            }
        }

        /*=================================================================================================
         * 
         * Save button.
         * 
         * Do a syntax check, and write to the database.
         */

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = tbCreateProcedure.Text.Trim();
                string name = AnyDB.Drivers.SQLite.SPliteCommand.GetProcedureName(sql);
                DataTable dtProcs = null;
                SPliteProcs.DoTransaction(FileName, tx =>
                {
                    SPliteProcs.CheckSyntax(tx, name, sql);
                    try
                    {
                        SPliteProcs.PossiblyDropTriggers(tx);
                        SPliteProcs.CreateOrReplaceProcedure(tx, name, sql, ProcedureName);
                    }
                    finally
                    {
                        SPliteProcs.PossiblyCreateTriggers(tx);
                    }
                    dtProcs = SPliteProcs.ReadAllProcedures(tx);
                    dtProcs.Rows.Add(NewProcedure);
                    tx.Commit();
                });
                ShowProcedureList(dtProcs, ProcedureName = name);
            }
            catch(Exception ex)
            {
                PopupException(ex);
            }
        }

        /*=================================================================================================
         * 
         * Cancel button.
         * 
         * Undo unsaved edits.
         */

        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dtProcs = lbProcedures.DataSource as DataTable;
                ShowProcedureList(dtProcs, ProcedureName);
            }
            catch(Exception ex)
            {
                PopupException(ex);
            }
        }

        /*=================================================================================================
         * 
         * Delete button.
         * 
         * Delete the stored procedure.
         */

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                                $"Do you really want to drop the '{ProcedureName}' procedure?",
                                "Are you sure?",
                                MessageBoxButtons.YesNoCancel,
                                MessageBoxIcon.Question,
                                MessageBoxDefaultButton.Button2) == DialogResult.Yes)
            {
                try
                {
                    DataTable dtProcs = null;
                    SPliteProcs.DoTransaction(FileName, tx =>
                    {
                        try
                        {
                            SPliteProcs.PossiblyDropTriggers(tx);
                            SPliteProcs.DropProcedure(tx, ProcedureName);
                        }
                        finally
                        {
                            SPliteProcs.PossiblyCreateTriggers(tx);
                        }
                        dtProcs = SPliteProcs.ReadAllProcedures(tx);
                        dtProcs.Rows.Add(NewProcedure);
                        tx.Commit();
                    });
                    ShowProcedureList(dtProcs);
                }
                catch(Exception ex)
                {
                    PopupException(ex);
                }
            }
        }

        /*=================================================================================================
         * 
         * File/Exit
         * 
         * Does what it says on the tin.
         */

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /*=================================================================================================
         * 
         * TextBox.TextChanged event.
         * 
         * Whenever we modify the CREATE TABLE statement, trigger a syntax check so we don't end up putting 
         * invalid code into the database. Also enable/disable the Save button for the same reason.
         */

        private void tbCreateProcedure_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string sql = tbCreateProcedure.Text.Trim();
                string name = AnyDB.Drivers.SQLite.SPliteCommand.GetProcedureName(sql);
                SPliteProcs.DoTransaction(FileName, tx =>
                {
                    SPliteProcs.CheckSyntax(tx, name, sql);
                    tx.Rollback();
                });
                ShowOK();
                btnSave.Enabled = true;
            }
            catch(Exception ex)
            {
                // show red error message while typing
                ShowError(ex.Message);
                btnSave.Enabled = false;
            }
        }

        /*=================================================================================================
         * 
         * ShowProcedureList()
         * 
         * Populate the list, and select the current procedure. This one does a bit of jiggery-pokery to
         * make sure the SelectedIndexChanged doesn't misfire. 
         */

        private void ShowProcedureList(DataTable dtProcs, string name=null)
        {
            lbProcedures.SelectedIndexChanged -= lbProcedures_SelectedIndexChanged;
            lbProcedures.DataSource = dtProcs;
            lbProcedures.SelectedIndex = -1;
            lbProcedures.SelectedIndexChanged += lbProcedures_SelectedIndexChanged;
            if (name != null)
            {
                foreach (DataRow dr in dtProcs.Select($"name = '{name}'"))
                {
                    lbProcedures.SelectedIndex = dtProcs.Rows.IndexOf(dr);
                    break;
                }
            }
            else
                lbProcedures.SelectedIndex = 0;
        }

        /*
         * Conveniences.
         */

        private void ShowOK()
        {
            lblError.Text = "OK";
            lblError.ForeColor = this.ForeColor;
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.ForeColor = Color.Red;
        }

        private Regex reNewline = new Regex(@"\r?\n");

        private string TextBoxIfy(string str)
        {
            if (str == null) return "";
            return reNewline.Replace(str.Trim(), "\r\n");
        }

        private void PopupException(Exception ex)
        {
            MessageBox.Show(this, ex.Message);
        }
    }
}