﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShareFile.Api.Powershell.Browser
{
    public partial class BasicAuthDialog : Form
    {
        public string Username { get { return textBoxUsername.Text;  } }

        public string Password { get { return textBoxPassword.Text;  } }

        public BasicAuthDialog(string domain)
        {
            InitializeComponent();
            labelDomainName.Text = domain;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
