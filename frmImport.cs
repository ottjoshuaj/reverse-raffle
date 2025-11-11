using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CsvHelper;
using CsvHelper.Configuration;
using ReverseRaffler.Data;
using ReverseRaffler.Objects;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

namespace ReverseRaffler
{
    public partial class frmImport : RadForm
    {
        #region Internals

        private CsvReader csvReader;

        #endregion

        #region Constructor(s)

        public frmImport()
        {
            InitializeComponent();

            Shown += frmImport_Shown;
        }

        public frmImport(string filePathToImport)
            : this()
        {
            TextReader tr = new StreamReader(filePathToImport);
            csvReader = new CsvReader(tr, CultureInfo.InvariantCulture);
        }

        public frmImport(Stream fileStream)
            : this()
        {
            TextReader tr = new StreamReader(fileStream);
            csvReader = new CsvReader(tr, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Form Events

        private void frmImport_Load(object sender, EventArgs e)
        {

        }

        void frmImport_Shown(object sender, EventArgs e)
        {
            try
            {
                var importTickets = new List<TicketHolder>();
                var csvRecords = csvReader.GetRecords<ImportTicketItem>();

                csvReader.Read();
                csvReader.ReadHeader();

                while (csvReader.Read())
                {
                    var record = csvReader.GetRecord<ImportTicketItem>();

                    importTickets.Add(new TicketHolder
                    {
                        TicketNumber = record.Id,
                        TicketHolder1 = record.Name1,
                        TicketHolder2 = record.Name2,
                        TicketHolder3 = record.Name3
                    });
                }

                //Now actually import the file in to the database
                progressImportStatus.Minimum = 1;
                progressImportStatus.Maximum = importTickets.Count + 1;
                progressImportStatus.Value1 = 1;

                using (var dbModel = new RevRaffleModelEntities())
                {
                    //Clear out the database
                    foreach (var item in dbModel.TicketHolders.ToList())
                        dbModel.TicketHolders.Remove(item);

                    dbModel.SaveChanges();

                    //Import the tickets
                    foreach (var resultItem in importTickets)
                    {
                        progressImportStatus.Text = $"Importing Ticket # {resultItem.TicketNumber}";

                        //Make sure this ticket number does NOT exist
                        if (!dbModel.TicketHolders.Any(x => x.TicketNumber == resultItem.TicketNumber))
                        {
                            dbModel.TicketHolders.Add(resultItem);

                            //Save this item to dB
                            dbModel.SaveChanges();
                        }

                        progressImportStatus.Value1++;
                    }

                }

                Close();
            } catch
            {
                RadMessageBox.Show("Your CSV is not properly formatted! First row MUST be a header!\r\nHeader should be id, name1, name2, name3 !!\r\nRows in the file should be:  <ticketnumber>,<name>,<name>" +
                                   ",<name> (Max of 3 <names>)\r\nExample: 1,joe,harry,mike  (per row)", "BAD FORMAT", MessageBoxButtons.OK, RadMessageIcon.Error);
                Close();

                return;
            }
        }

        #endregion
    }
}
