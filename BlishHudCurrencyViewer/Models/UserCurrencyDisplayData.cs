using Blish_HUD.Controls;

namespace BlishHudCurrencyViewer.Models
{
    internal class UserCurrencyDisplayData
    { 
        public int CurrencyId { get; set; }
        public string CurrencyDisplayName { get; set; }
        public Label Name { get; set; }
        public Label Quantity { get; set; }
    }
}
