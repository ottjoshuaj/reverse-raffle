using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using ReverseRaffler.Data;

namespace ReverseRaffler.Services
{
    /// <summary>
    /// Service for importing raffle tickets from Excel (XLSX) files
    /// </summary>
    public class ExcelImportService
    {
        /// <summary>
        /// Imports tickets from an Excel file stream
        /// Format: Column 1 = Ticket Number, Columns 2+ = Names on ticket
        /// </summary>
        /// <param name="fileStream">Excel file stream to read</param>
        /// <returns>List of TicketHolder objects</returns>
        public static List<TicketHolder> ImportFromExcel(Stream fileStream)
        {
            var tickets = new List<TicketHolder>();

            using (var package = new ExcelPackage(fileStream))
            {
                // Get the first worksheet
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    throw new Exception("Excel file contains no worksheets");
                }

                // Determine if first row is a header by checking if it's numeric
                int startRow = 1;
                var firstCellValue = worksheet.Cells[1, 1].Value;

                // If first row, first column is not a number, assume it's a header
                if (firstCellValue != null && !IsNumeric(firstCellValue.ToString()))
                {
                    startRow = 2; // Skip header row
                }

                // Get the used range to know how many rows to process
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount == 0)
                {
                    throw new Exception("Excel file is empty");
                }

                // Process each row
                for (int row = startRow; row <= rowCount; row++)
                {
                    // First column is ticket number
                    var ticketNumberValue = worksheet.Cells[row, 1].Value;
                    if (ticketNumberValue == null)
                        continue; // Skip empty rows

                    // Try to parse ticket number
                    if (!int.TryParse(ticketNumberValue.ToString(), out int ticketNumber))
                    {
                        // Skip rows where ticket number isn't valid
                        continue;
                    }

                    // Create ticket holder
                    var ticket = new TicketHolder
                    {
                        TicketNumber = ticketNumber,
                        TicketHolder1 = GetCellValueAsString(worksheet, row, 2),
                        TicketHolder2 = GetCellValueAsString(worksheet, row, 3),
                        TicketHolder3 = GetCellValueAsString(worksheet, row, 4),
                        TicketHolder4 = GetCellValueAsString(worksheet, row, 5)
                    };

                    tickets.Add(ticket);
                }
            }

            return tickets;
        }

        /// <summary>
        /// Imports tickets from an Excel file path
        /// </summary>
        /// <param name="filePath">Path to Excel file</param>
        /// <returns>List of TicketHolder objects</returns>
        public static List<TicketHolder> ImportFromExcelFile(string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return ImportFromExcel(fileStream);
            }
        }

        /// <summary>
        /// Gets cell value as string, handling nulls and different data types
        /// </summary>
        private static string GetCellValueAsString(ExcelWorksheet worksheet, int row, int column)
        {
            var cellValue = worksheet.Cells[row, column].Value;
            if (cellValue == null)
                return string.Empty;

            return cellValue.ToString().Trim();
        }

        /// <summary>
        /// Checks if a string represents a numeric value
        /// </summary>
        private static bool IsNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return double.TryParse(value, out _);
        }
    }
}
