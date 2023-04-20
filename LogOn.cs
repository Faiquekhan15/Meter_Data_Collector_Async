using System;
using System.Data;
using System.Windows.Forms;

namespace MDC_Server
{
    public partial class LogOn : Form
    {
        public LogOn()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                DBGetSet DB = new DBGetSet();
                DB.Query = "select username from meter.users where username = '" + textBox1.Text + "' and `password` = '" + textBox2.Text + "' and typ = 'Admin';";
                DataTable dt = DB.ExecuteReader();

                if (dt.Rows.Count < 1)
                {
                    MessageBox.Show("Invalid Credentials", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.Hide();
                Form1 Main = new Form1();
                Main.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("An Error Occured. Please Check if the Authentication Server is running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
