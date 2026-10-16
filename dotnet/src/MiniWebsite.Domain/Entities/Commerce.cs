using MiniWebsite.Domain.Common;

namespace MiniWebsite.Domain.Entities;

/// <summary>A plan that is sold; the price is the same for every role.</summary>
public class PricingPlan
{
    public int Id { get; set; }
    public string PlanName { get; set; } = "";
    /// <summary>One of <see cref="PlanCommissionValues.Categories"/>.</summary>
    public string PlanCategory { get; set; } = PlanCommissionValues.CategoryDbProfile;
    /// <summary>Null means lifetime validity.</summary>
    public int? ValidityMonths { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal OriginalPriceGst { get; set; }
    public decimal OfferedPrice { get; set; }
    public decimal OfferedPriceGst { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Discount and earning terms for one role on one pricing plan.</summary>
public class PlanCommission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public int PricingPlanId { get; set; }
    public PricingPlan PricingPlan { get; set; } = null!;
    /// <summary>One of <see cref="PlanCommissionValues.DiscountTypes"/>.</summary>
    public string DiscountType { get; set; } = PlanCommissionValues.DiscountFixed;
    /// <summary>Discount to the customer, without GST. Used only when the discount type is Fixed.</summary>
    public decimal DiscountAmount { get; set; }
    /// <summary>One of <see cref="PlanCommissionValues.EarningTypes"/>.</summary>
    public string EarningType { get; set; } = PlanCommissionValues.EarningFixed;
    /// <summary>Partner earning, without GST. Used only when the earning type is Fixed.</summary>
    public decimal EarningAmount { get; set; }
    /// <summary>Renewal price for this role on this plan, without GST. Null when not renewed.</summary>
    public decimal? RenewalPrice { get; set; }
    public decimal? RenewalPriceGst { get; set; }
    /// <summary>Percent of <see cref="RenewalPrice"/> earned on renewal. Null means no renewal earning.</summary>
    public decimal? RenewalEarningPercent { get; set; }
    /// <summary>The partner pays SB revenue plus GST from their wallet (Franchise Partner).</summary>
    public bool DeductFromWallet { get; set; }
    public string Status { get; set; } = PlanCommissionValues.StatusActive;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class DealCustomerMapping
{
    public int Id { get; set; }
    public int DealId { get; set; }
    public Coupon Coupon { get; set; } = null!;
    public string CustomerEmail { get; set; } = "";
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
}

public class JoiningDeal
{
    public int Id { get; set; }
    public string DealCode { get; set; } = "";
    public string DealName { get; set; } = "";
    public string DealType { get; set; } = "";
    public decimal Fees { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalFees { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int? MwDealId { get; set; }
    public int? FranchiseDealId { get; set; }
    public int UpgradeOrder { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UserJoiningDeal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string UserEmail { get; set; } = "";
    public int JoiningDealId { get; set; }
    public JoiningDeal JoiningDeal { get; set; } = null!;
    public string DealCode { get; set; } = "";
    public string MappingStatus { get; set; } = "ACTIVE";
    public string? MappedBy { get; set; }
    public DateTime? MappedDate { get; set; }
    public bool EmailSent { get; set; }
    public DateTime? EmailSentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? TransactionId { get; set; }
    public int? InvoiceId { get; set; }
    public string PaymentStatus { get; set; } = "PENDING";
    public DateTime? PaymentDate { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string UserEmail { get; set; } = "";
    public decimal Balance { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public int UserId { get; set; }
    public string Type { get; set; } = ""; // deposit|withdraw|adjust
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? OrderId { get; set; }
    public string? Message { get; set; }
    public string? Ip { get; set; }
    public DateTime CreatedAt { get; set; } = AppClock.Now;
}

public class Order
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = "";
    public int? CardId { get; set; }
    public Card? Card { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string UserEmail { get; set; } = "";
    public string? ProductId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerContact { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Pincode { get; set; }
    public string? PaymentType { get; set; }
    public decimal PaymentAmount { get; set; }
    public string OrderStatus { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
}

public class Invoice
{
    public int Id { get; set; }
    /// <summary>Public order ID (<c>ORD</c> + 8 chars); assigned automatically on insert.</summary>
    public string OrderCode { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
    public string? AdminComment { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int? CardId { get; set; }
    public Card? Card { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string UserEmail { get; set; } = "";
    public string UserName { get; set; } = "";
    public string UserContact { get; set; } = "";
    public string BillingName { get; set; } = "";
    public string BillingEmail { get; set; } = "";
    public string BillingContact { get; set; } = "";
    public string BillingAddress { get; set; } = "";
    public string BillingState { get; set; } = "";
    public string BillingCity { get; set; } = "";
    public string BillingPincode { get; set; } = "";
    public string? BillingGstNumber { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string? PromoCode { get; set; }
    public decimal PromoDiscount { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string RazorpaySignature { get; set; } = "";
    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaymentDate { get; set; }
    public string ServiceName { get; set; } = "";
    public string ServiceDescription { get; set; } = "";
    public string HsnSacCode { get; set; } = "998313";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal SubTotal { get; set; }
    public decimal IgstPercentage { get; set; } = 18m;
    public decimal IgstAmount { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentType { get; set; } = "Regular";
    public string ReferenceNumber { get; set; } = "";
    public string? PlanName { get; set; }
    public string? PlanValidity { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FranchisePayment
{
    public int Id { get; set; }
    public int? FranchiseUserId { get; set; }
    public User? FranchiseUser { get; set; }
    public string FranchiseEmail { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public string PaymentStatus { get; set; } = "Pending";
    public string PaymentMethod { get; set; } = "Razorpay";
    public DateTime PaymentDate { get; set; }
    public string? AdminNotes { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime? RefundDate { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
