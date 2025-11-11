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
using ReverseRaffler.Services;
using Telerik.WinControls;
using Telerik.WinControls.UI;

namespace ReverseRaffler
{
    public partial class frmImport : RadForm
    {
        #region Internals

        private CsvReader csvReader;
        private Stream fileStream;
        private string fileName;
        private bool isExcelFile;

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
            fileName = filePathToImport;
            isExcelFile = Path.GetExtension(filePathToImport).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

            if (isExcelFile)
            {
                // Store file path for Excel import
                fileStream = new FileStream(filePathToImport, FileMode.Open, FileAccess.Read);
            }
            else
            {
                // CSV import
                TextReader tr = new StreamReader(filePathToImport);
                csvReader = new CsvReader(tr, CultureInfo.InvariantCulture);
            }
        }

        public frmImport(Stream inputStream, string originalFileName)
            : this()
        {
            fileName = originalFileName;
            fileStream = inputStream;
            isExcelFile = Path.GetExtension(originalFileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);

            if (!isExcelFile)
            {
                // CSV import - need to reset stream position
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                TextReader tr = new StreamReader(fileStream);
                csvReader = new CsvReader(tr, CultureInfo.InvariantCulture);
            }
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

                // Determine import method based on file type
                if (isExcelFile)
                {
                    // Import from Excel
                    importTickets = ImportFromExcel();
                }
                else
                {
                    // Import from CSV
                    importTickets = ImportFromCsv();
                }

                // Validate we have tickets to import
                if (importTickets == null || importTickets.Count == 0)
                {
                    RadMessageBox.Show("No tickets found in the file!", "NO DATA", MessageBoxButtons.OK, RadMessageIcon.Error);
                    Close();
                    return;
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
            }
            catch (Exception ex)
            {
                string errorMessage = isExcelFile
                    ? $"Your Excel file could not be imported!\r\n\r\nExpected format:\r\n" +
                      $"Column 1: Ticket Number\r\n" +
                      $"Column 2: Name 1\r\n" +
                      $"Column 3: Name 2 (optional)\r\n" +
                      $"Column 4: Name 3 (optional)\r\n" +
                      $"Column 5: Name 4 (optional)\r\n\r\n" +
                      $"Error: {ex.Message}"
                    : $"Your CSV is not properly formatted! First row MUST be a header!\r\n" +
                      $"Header should be id, name1, name2, name3 !!\r\n" +
                      $"Rows in the file should be:  <ticketnumber>,<name>,<name>,<name> (Max of 3 <names>)\r\n" +
                      $"Example: 1,joe,harry,mike  (per row)\r\n\r\n" +
                      $"Error: {ex.Message}";

                RadMessageBox.Show(errorMessage, "IMPORT ERROR", MessageBoxButtons.OK, RadMessageIcon.Error);
                Close();

                return;
            }
            finally
            {
                // Clean up resources
                csvReader?.Dispose();
                fileStream?.Dispose();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Imports tickets from CSV file
        /// </summary>
        private List<TicketHolder> ImportFromCsv()
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

            return importTickets;
        }

        /// <summary>
        /// Imports tickets from Excel file
        /// </summary>
        private List<TicketHolder> ImportFromExcel()
        {
            // Reset stream position if possible
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            return ExcelImportService.ImportFromExcel(fileStream);
        }

        #endregion
    }
}
