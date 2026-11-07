namespace lucho_sprintime.Models;

public enum EventStatus { Draft, Published, RegistrationOpen, RegistrationClosed, Live, Finished, Suspended }
public enum RegistrationStatus { PendingPayment, Confirmed, Cancelled, Refunded, Onsite }
public enum PaymentStatus { Pending, Approved, Rejected, Refunded, Cancelled }
public enum CheckInStatus { Pending, Accredited, Started, Finished, Dns, Dnf }
public enum TimingPointType { Start, Checkpoint, Finish }
public enum ResultStatus { Official, Provisional, Dns, Dnf, Disqualified }
