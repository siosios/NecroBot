﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using PoGo.NecroBot.Logic.Model.Settings;

namespace RocketBot2.Forms
{
    public partial class AuthAPIForm : Form
    {
        public APIConfig Config
        {
            get
            {
                return new APIConfig()
                {
                    UseLegacyAPI = radLegacy.Checked,
                    UsePogoDevAPI = radHashServer.Checked,
                    AuthAPIKey = txtAPIKey.Text.Trim()
                };
            }
            set
            {
                radHashServer.Checked = value.UsePogoDevAPI;
                radLegacy.Checked = value.UseLegacyAPI;
            }
        }

        private bool forceInput;

        public AuthAPIForm(bool forceInput)
        {
            InitializeComponent();

            if (forceInput)
            {
                this.forceInput = forceInput;
                ControlBox = false;
                btnCancel.Visible = false;
            }
        }

        private const int CP_NOCLOSE_BUTTON = 0x200;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams myCp = base.CreateParams;
                if (forceInput)
                {
                    myCp.ClassStyle = myCp.ClassStyle | CP_NOCLOSE_BUTTON;
                }

                return myCp;
            }
        }

        private void LnkBuy_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://talk.pogodev.org/d/51-api-hashing-service-by-pokefarmer");
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (radHashServer.Checked && string.IsNullOrEmpty(txtAPIKey.Text))
            {
                MessageBox.Show("Please enter API Key", "Missing API key", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!radHashServer.Checked && !radLegacy.Checked)
            {
                MessageBox.Show("Please select an API method", "Config error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void AuthAPIForm_Load(object sender, EventArgs e)
        {
        }
    }
}