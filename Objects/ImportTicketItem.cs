using CsvHelper.Configuration.Attributes;

namespace ReverseRaffler.Objects
{
    internal class ImportTicketItem
    {
        [Name("id")]
        public int Id { get; set; }

        [Name("name1")]
        public string Name1 { get; set; }

        [Name("name2")]
        public string Name2 { get; set; }

        [Name("name3")]
        public string Name3 { get; set; }
    }
}
