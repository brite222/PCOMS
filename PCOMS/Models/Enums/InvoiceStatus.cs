public enum InvoiceStatus
{
    Draft,          // Being created
    Sent,           // Sent to client
    Viewed,         // Client has viewed
    PartiallyPaid,  // Some payment received
    Paid,           // Fully paid
    Overdue,        // Past due date
    Cancelled,      // Cancelled
    Refunded        // Refunded
}