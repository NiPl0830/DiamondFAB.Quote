using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DiamondFAB.Quote.Models
{
    public class Quote : INotifyPropertyChanged
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string? CustomerName { get; set; }

        // Formatted string like "Q-000123"
        public string? QuoteNumber { get; set; }

        private List<LineItem> _lineItems = new();
        public List<LineItem> LineItems
        {
            get => _lineItems;
            set
            {
                _lineItems = value ?? new List<LineItem>();
                OnPropertyChanged(nameof(LineItems));
                NotifyTotalsChanged();
            }
        }

        // Part-level details used on page 2 of the PDF
        public List<PartDetail> PartDetails { get; set; } = new();

        // Optional: settings snapshot used by exporter
        public Settings? AppSettings { get; set; }

        // Percent values, e.g., 8.25 for 8.25%
        public double TaxRate { get; set; } = 0.0;

        // --- Discount (% of Subtotal, applied before tax) ---
        private double _discountPercent = 0.0;
        public double DiscountPercent
        {
            get => _discountPercent;
            set
            {
                if (Math.Abs(_discountPercent - value) > double.Epsilon)
                {
                    _discountPercent = value;
                    OnPropertyChanged(nameof(DiscountPercent));
                    NotifyTotalsChanged();
                }
            }
        }

        // --- Totals ---
        public double Subtotal => LineItems.Sum(x => x.Total);

        public double DiscountAmount => Subtotal * (DiscountPercent / 100.0);

        // Ensure we don’t dip below zero due to an extreme discount
        public double SubtotalAfterDiscount => Math.Max(0, Subtotal - DiscountAmount);

        // Tax is calculated on (Subtotal - Discount)
        public double Tax => SubtotalAfterDiscount * (TaxRate / 100.0);

        public double GrandTotal => SubtotalAfterDiscount + Tax;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(DiscountPercent));
            OnPropertyChanged(nameof(DiscountAmount));
            OnPropertyChanged(nameof(Tax));
            OnPropertyChanged(nameof(GrandTotal));
        }

        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}