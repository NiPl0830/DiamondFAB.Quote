# Changelog

## [2.5.7.11] - 2025-01-13

### Added
- **Delete button** for each line item in the quote grid
- **Edit line item** functionality - double-click or edit button to modify existing items
- **Add Custom Line Item** button - manually add line items without XML import
- **Keyboard shortcuts** for all major actions:
  - Ctrl+I: Import XML
  - Ctrl+S: Save Quote
  - Ctrl+O: Load Saved Quote
  - Ctrl+P: Export PDF
  - Ctrl+Shift+E: Export CSV
  - Ctrl+N: New Quote
- **Valid Until date** display on PDF quotes (shown in red for urgency)
- **Summary metrics box** on PDF - displays total laser time (hours) and total material (lbs)
- **Integrated part-level breakdown** - part details now appear directly below laser time line items on PDF
- **Setting to toggle part breakdown** - show/hide part details in PDF exports (Preferences tab)
- **Customer "Bill To" section** now properly displays on PDF quotes

### Changed
- **PDF format completely redesigned**:
  - Part details integrated into Page 1 (no more separate Page 2)
  - Part details show as indented items under laser time line items
  - Part details display format: `↳ Part Name (qty: X) • X" cut length`
  - Removed square footage from part details, now shows only cut length in inches
  - Added highlighted summary metrics box with blue accent border
  - Quote meta box now includes Valid Until date when set
- **Customer information** properly falls back to Settings if not set on Quote
- **Company header vs Bill To** - "Your Company Information" (Preferences) shows in header, "Customer Information" (General) shows in Bill To section

### Fixed
- Customer Bill To section not appearing on PDF exports
- Quote.AppSettings reference properly maintained for PDF generation

### UI/UX Improvements
- Keyboard shortcuts added to button tooltips
- Line item management more intuitive with edit/delete per row
- PDF quotes now single-page format (cleaner, more professional)
- Part-level breakdown contextually integrated instead of separate appendix

---

## [1.3.5] - Previous Release
- Initial stable release with XML import functionality
- Basic PDF export with 2-page format
- Settings management for company info, rates, and charges
- Quote save/load functionality
