using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DiamondFAB.Quote.Models
{
    public class Quote : INotifyPropertyChanged
    {
        public string QuoteNumber { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string CustomerName { get; set; }

        private List<LineItem> _lineItems = new();
        public List<LineItem> LineItems
        {
            get => _lineItems;
            set
            {
                _lineItems = value;
                OnPropertyChanged(nameof(LineItems));
                NotifyTotalsChanged();
            }
        }

        public double TaxRate { get; set; } = 0.0;

        public double Subtotal => LineItems.Sum(x => x.Total);
        public double Tax => Subtotal * (TaxRate / 100.0);
        public double GrandTotal => Subtotal + Tax;

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(GrandTotal));
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}