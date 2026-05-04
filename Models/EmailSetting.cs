namespace Change_order.Models
{
    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string FromAddress { get; set; } = "";
        public string DefaultBcc { get; set; } = "";
        public string BccAddress { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool TestMode { get; set; } = false;
        public string TestToAddress { get; set; } = "";
        public string Manager1Email { get; set; } = "";
        public string Manager2Email { get; set; } = "";
        public string AppBaseUrl { get; set; } = "";
    }
}