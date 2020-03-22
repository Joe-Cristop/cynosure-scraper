using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CynosureScraper
{
    public delegate void ItemAdded();

    public partial class Form1 : MaterialForm
    {
        int mItemCount;

        private readonly SynchronizationContext synchronizationContext;

        public Form1()
        {
            InitializeComponent();

            synchronizationContext = SynchronizationContext.Current;

            MaterialSkinManager materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;

            // Configure color schema
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Blue400, Primary.Blue500,
                Primary.Blue500, Accent.LightBlue200,
                TextShade.WHITE
            );
        }

        private void btnAllCheck_Click(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox)
                {
                    CheckBox cb = (CheckBox)c;
                    cb.Checked = true;
                }
            }
        }

        private void btnAllUncheck_Click(object sender, EventArgs e)
        {
            foreach (Control c in this.Controls)
            {
                if (c is CheckBox)
                {
                    CheckBox cb = (CheckBox)c;
                    cb.Checked = false;
                }
            }
        }

        private void btnSelFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            sfd.Filter = "CSV Format|*.csv";

            if (DialogResult.OK != sfd.ShowDialog())
                return;

            leOutputFile.Text = sfd.FileName;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            List<int> treatmentIds = new List<int>();
            string filePath = leOutputFile.Text;
            string inputFilePath = leInputFile.Text;

            if ("" == inputFilePath)
            {
                MessageBox.Show("Select zipcode file.");
                return;
            }

            if ("" == filePath)
            {
                MessageBox.Show("Select file to save.");
                return;
            }

            foreach (Control c in this.Controls)
            {
                if (c is CheckBox)
                {
                    CheckBox cb = (CheckBox)c;
                    if (!cb.Checked)
                        continue;
                    treatmentIds.Add(int.Parse(cb.Name.Split('_')[1]));
                }
            }

            if (treatmentIds.Count == 0)
            {
                MessageBox.Show("Select at least one treatment.");
                return;
            }

            btnStart.Enabled = false;
            btnStart.Text = "Processing";

            string country = rbCountryUSA.Checked ? "US" : "CA";

            await Task.Run(() =>
            {
                mItemCount = 0;
                Engine engine = new Engine();

                string[] zipCodes = File.ReadAllLines(inputFilePath);

                for (int i = 0; i < zipCodes.Length; ++ i)
                {
                    string zipcode = zipCodes[i].Trim();

                    synchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        leProcess.Text = "Zip Code: " + zipcode;
                    }), null);

                    engine.Process(zipcode, country, 0, treatmentIds.ToArray(), filePath, onNewItemAdded);
                }
            });

            btnStart.Enabled = true;
            btnStart.Text = "Start";

            MessageBox.Show( mItemCount + " Results Found");
        }

        private void onNewItemAdded()
        {
            synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                btnStart.Text = o + " Results Found";
            }), ++mItemCount);
        }

        private void btnSelInputFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog sfd = new OpenFileDialog();

            sfd.Filter = "Text Format|*.txt";

            if (DialogResult.OK != sfd.ShowDialog())
                return;

            leInputFile.Text = sfd.FileName;
        }
    }
}
