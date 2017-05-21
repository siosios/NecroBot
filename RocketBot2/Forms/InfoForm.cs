﻿using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace RocketBot2.Forms
{
    public partial class InfoForm : Form
    {
        public InfoForm()
        {
            InitializeComponent();
        }

        private void InfoForm_Load(object sender, EventArgs e)
        {
            Text = "RocketBot2 "
                        + FileVersionInfo
                            .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                            .ProductVersion;
        }


        private void Link_click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(((LinkLabel) sender).Text);
        }

        private int count = 60;

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (count-- <= 0)
            {
                Close();
            }
        }

        private void PictureBox1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/TheUnnamedOrganisation/RocketBot/releases");
        }
    }
}