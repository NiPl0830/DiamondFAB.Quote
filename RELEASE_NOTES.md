# DiamondFAB Quote v1.4.11.11 Release Notes

## 🎉 Major Update: Enhanced PDF Exports & Line Item Management

This release brings significant improvements to quote management and PDF generation, making DiamondFAB Quote faster and more professional.

---

## 🚀 What's New

### Professional PDF Exports
- **Redesigned single-page PDF format** - All information now on one clean, professional page
- **Summary metrics at-a-glance** - Total laser time and material weight displayed prominently
- **Integrated part breakdown** - Part details appear contextually below their line items (no more confusing Page 2)
- **Valid Until date** - Create urgency with expiration dates shown in red
- **Proper customer information display** - "Bill To" section now reliably shows customer details

### Powerful Line Item Management
- **Edit any line item** - Double-click to modify description, quantity, or price
- **Delete individual items** - Remove unwanted line items with one click
- **Add custom line items** - Manually add items without needing XML import
- **Keyboard shortcuts** for power users:
  - `Ctrl+I` Import XML
  - `Ctrl+S` Save Quote
  - `Ctrl+O` Load Quote
  - `Ctrl+P` Export PDF
  - `Ctrl+Shift+E` Export CSV
  - `Ctrl+N` New Quote

### Customizable PDF Output
- **Toggle part-level breakdown** - Show or hide detailed part information per customer preference
- **Cut length only display** - Part details now show cut length in inches (removed confusing square footage)
- **Settings persist** - Your PDF preferences save between sessions

---

## 🔧 Technical Improvements

- Fixed customer information not appearing in "Bill To" section
- Improved Settings → Quote data flow for reliable PDF generation
- Enhanced MVVM architecture for better line item editing
- Optimized PDF generation (QuestPDF) for faster exports

---

## 📦 Installation

1. Download `DiamondFAB.Quote.Installer.msi` from this release
2. Run the installer (requires .NET 8.0 Runtime - included)
3. Launch DiamondFAB Quote from Start Menu or Desktop shortcut

**Upgrading from 1.3.5?** Your settings and saved quotes will be preserved automatically.

---

## 💡 Tips for Using v1.4.11.11

1. **Set up both company profiles** in Settings:
   - **Preferences → Your Company Information** (appears in PDF header)
   - **General → Customer Information** (appears in "Bill To" section)

2. **Customize PDF output** in Settings → Preferences:
   - Enable/disable part-level breakdown based on customer needs
   - Saves you time when generating quotes for different audiences

3. **Use keyboard shortcuts** for faster workflow:
   - Import multiple XML files with `Ctrl+I`
   - Save frequently with `Ctrl+S`
   - Export PDFs instantly with `Ctrl+P`

4. **Edit line items on the fly**:
   - Double-click any item to modify
   - Add custom charges or adjustments without re-importing
   - Delete mistakes immediately

---

## 🐛 Known Issues

- None reported in this release

---

## 📞 Support

For issues, feature requests, or questions:
- Open an issue on GitHub
- Contact: [Your Contact Info]

---

## 🙏 Changelog Summary

**Added:**
- Line item editing, deletion, and custom addition
- Keyboard shortcuts for all major actions
- PDF summary metrics box (laser hours + material weight)
- Valid Until date on PDF quotes
- Integrated part-level breakdown in PDF
- Toggle setting for part breakdown visibility

**Changed:**
- PDF format from 2-page to single-page
- Part details now show cut length only (removed sq ft)
- Customer info properly displays in "Bill To" section

**Fixed:**
- Customer information missing from PDF exports
- Quote settings reference for proper PDF generation

---

**Full Changelog:** [View CHANGELOG.md](CHANGELOG.md)

**Previous Release:** v1.3.5
