using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MastercardHost
{
    public partial class FormDialogBox : Form
    {
        public string InputResult {  get; private set; }

        public FormDialogBox()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.InputResult = this.textBox1.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
