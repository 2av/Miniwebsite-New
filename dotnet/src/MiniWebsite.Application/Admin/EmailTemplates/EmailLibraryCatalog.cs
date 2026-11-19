using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using MiniWebsite.Domain.Common;
using MiniWebsite.Domain.Entities;

namespace MiniWebsite.Application.Admin.EmailTemplates;

/// <summary>
/// Approved ShareBizz email library. Startup inserts any missing row.
/// Existing admin edits are left in place after the first content update.
/// </summary>
public static class EmailLibraryCatalog
{
    public const string RegistrationInvitation = "ce_01_registration_invitation";
    public const string EmailVerificationOtp = "registration_otp";
    public const string RegistrationWelcome = "customer_welcome";
    public const string AccountCredentials = "ce_04_account_credentials";
    public const string PasswordResetOtp = "password_reset";
    public const string PasswordChanged = "ce_06_password_changed";
    public const string DocumentsReceived = "ce_07_documents_received";
    public const string DocumentsVerified = "ce_08_documents_verified";
    public const string DocumentResubmission = "ce_09_document_resubmission";
    public const string PaymentSuccessful = "ce_10_payment_successful";
    public const string PaymentFailed = "ce_11_payment_failed";
    public const string InvoiceAvailable = "ce_12_invoice_available";
    public const string ReferralStatement = "ce_13_referral_statement";
    public const string ReferralQueryResolved = "ce_14_referral_query_resolved";
    public const string ReferralApproved = "ce_15_referral_approved";
    public const string ReferralPayout = "ce_16_referral_payout";
    public const string MembershipExpiry = "ce_17_membership_expiry";
    public const string RenewalSuccessful = "ce_18_renewal_successful";
    public const string ComplimentaryBenefit = "ce_19_complimentary_benefit";
    public const string FstAppointment = "ce_20_fst_appointment";
    public const string FstOnboarding = "ce_21_fst_onboarding";
    public const string FstPerformanceReport = "ce_22_fst_performance_report";
    public const string FstSalaryStatement = "ce_23_fst_salary_statement";
    public const string FstSalarySlip = "ce_24_fst_salary_slip";
    public const string FstSalaryPayment = "ce_25_fst_salary_payment";
    public const string FstTargetUpdate = "ce_26_fst_target_update";
    public const string ReferralActivation = "se_01_referral_activation";
    public const string ProspectOtp = "se_02_prospect_otp";
    public const string ProspectLoginPayment = "se_03_prospect_login_payment";
    public const string FstComplimentaryDecision = "se_04_fst_complimentary_decision";

    public static readonly EmailLibraryDefinition[] All =
    [
        Def(RegistrationInvitation, "CE-01 — Registration Invitation", "auth",
            "You're invited to join ShareBizz as {{user_type}}",
            Html(
                "Dear {{first_name}},",
                "You are invited to register with ShareBizz as a {{user_type}}.",
                "ShareBizz helps businesses build a professional digital presence, showcase their products or services, connect with customers and manage customer enquiries.",
                "To get started, please complete your registration using the link below:",
                "<a href=\"{{registration_link}}\">{{registration_link}}</a>",
                "{{invitation_context}}",
                "If you need help with the registration process, please contact us at {{support_email}}.",
                "Regards,<br>{{sender_name}}<br>ShareBizz<br>{{website}}"),
            "first_name,user_type,registration_link,invitation_context,support_email,sender_name,website",
            "Sent when an admin invites someone to register, or sends a Franchise Partner registration link.",
            sent: true),

        Def(EmailVerificationOtp, "CE-02 — Email Verification OTP", "auth",
            "Your ShareBizz verification code",
            Html(
                "Dear {{first_name}},",
                "Use the following one-time password (OTP) to verify your email address for your ShareBizz account:",
                "<strong>{{otp}}</strong>",
                "This OTP is valid for {{otp_validity}}. Please do not share it with anyone.",
                "If you did not initiate this request, you can ignore this email. If you believe someone is trying to access your account, contact {{support_email}}.",
                "Regards,<br>ShareBizz<br>This is an automated email. Please do not reply."),
            "first_name,otp,otp_validity,support_email",
            "Sent when a ShareBizz user or Franchise Partner requests a registration OTP. Validity is 10 minutes.",
            sent: true),

        Def(RegistrationWelcome, "CE-03 — Registration Successful / Welcome", "welcome",
            "Welcome to ShareBizz, {{first_name}}!",
            Html(
                "Dear {{first_name}},",
                "Welcome to ShareBizz!",
                "Your registration as a {{user_type}} has been completed successfully.",
                "{{next_steps_block}}",
                "Your login details:",
                "Login ID: {{login_id}}<br>Password: {{password}}",
                "You can access your account here:",
                "<a href=\"{{login_link}}\">{{login_link}}</a>",
                "{{payment_block}}",
                "We recommend keeping your account details secure and completing any pending onboarding steps shown in your Dashboard.",
                "If you need assistance, contact {{support_email}}.",
                "We're glad to have you with ShareBizz.",
                "Regards,<br>{{sender_name}}<br>ShareBizz<br>{{website}}"),
            "first_name,user_type,next_steps_block,login_id,password,login_link,payment_block,support_email,sender_name,website",
            "Sent after registration OTP is verified. The password is included. The payment paragraph is included only when a payment link applies.",
            sent: true),

        Def(AccountCredentials, "CE-04 — Account Credentials", "auth",
            "Your ShareBizz account access details",
            Html(
                "Dear {{first_name}},",
                "Your ShareBizz {{user_type}} account is ready.",
                "Login ID: {{login_id}}<br>Password: {{password}}",
                "{{payment_block}}",
                "You can sign in here:",
                "<a href=\"{{login_link}}\">{{login_link}}</a>",
                "For your security, do not share your password or login details with anyone.",
                "If you did not expect this account setup email, contact {{support_email}}.",
                "Regards,<br>ShareBizz<br>This is an automated email. Please do not reply."),
            "first_name,user_type,login_id,password,payment_block,login_link,support_email",
            "Sent when an invited partner finishes registration, or when an admin creates an account with a password. The password is included. The payment paragraph is included only when a fee applies.",
            sent: true),

        Def(PasswordResetOtp, "CE-05 — Password Reset OTP", "auth",
            "Your ShareBizz password reset OTP",
            Html(
                "Dear {{first_name}},",
                "We received a request to reset the password for your ShareBizz account.",
                "Use this OTP to verify the request:",
                "<strong>{{otp}}</strong>",
                "The OTP is valid for {{otp_validity}}. Do not share it with anyone.",
                "If you did not request a password reset, do not share the OTP and contact {{support_email}} if you notice any unusual account activity.",
                "Regards,<br>ShareBizz<br>This is an automated email. Please do not reply."),
            "first_name,otp,otp_validity,support_email",
            "Sent for forgot-password. The code is the existing reset code and is valid for 30 minutes.",
            sent: true),

        Def(PasswordChanged, "CE-06 — Password Changed / Security Alert", "auth",
            "Your ShareBizz password has been changed",
            Html(
                "Dear {{first_name}},",
                "The password for your ShareBizz account was changed successfully.",
                "Change date: {{change_date}}<br>Change time: {{change_time}}",
                "{{device_block}}",
                "If you made this change, no action is required.",
                "If you did not make this change, contact {{support_email}} immediately so that the team can help secure your account.",
                "Regards,<br>ShareBizz<br>This is an automated security notification."),
            "first_name,change_date,change_time,device_block,support_email",
            "Sent after a successful password reset, password change, or admin password reset. Device and location are omitted because they are not stored.",
            sent: true),

        Def(DocumentsReceived, "CE-07 — Document Submission Received", "documents",
            "We have received your ShareBizz verification documents",
            Html(
                "Dear {{first_name}},",
                "We have received the following documents/details submitted through your ShareBizz Dashboard:",
                "{{document_list}}",
                "Submission date: {{submission_date}}<br>Reference ID: {{reference_id}}<br>Current status: {{verification_status}}",
                "Our team will review the submitted information. You can check the latest status from the Document Verification section of your Dashboard:",
                "<a href=\"{{dashboard_link}}\">{{dashboard_link}}</a>",
                "If any additional information is required, we will contact you.",
                "For assistance, write to {{support_email}}.",
                "Regards,<br>ShareBizz Support"),
            "first_name,document_list,submission_date,reference_id,verification_status,dashboard_link,support_email",
            "Sent when document verification is submitted and the status becomes Pending Review.",
            sent: true),

        Def(DocumentsVerified, "CE-08 — Documents Verified Successfully", "documents",
            "Your ShareBizz documents have been verified",
            Html(
                "Dear {{first_name}},",
                "Your submitted documents have been reviewed and verified successfully.",
                "Reference ID: {{reference_id}}<br>Verification date: {{approval_date}}",
                "You can sign in to your ShareBizz Dashboard to continue with the applicable onboarding or account activities:",
                "<a href=\"{{dashboard_link}}\">{{dashboard_link}}</a>",
                "Thank you for completing the verification process.",
                "If you need assistance, contact {{support_email}}.",
                "Regards,<br>ShareBizz Support"),
            "first_name,reference_id,approval_date,dashboard_link,support_email",
            "Sent when document review is Approved.",
            sent: true),

        Def(DocumentResubmission, "CE-09 — Document Resubmission Required", "documents",
            "Action required: please update your ShareBizz documents",
            Html(
                "Dear {{first_name}},",
                "We reviewed the information submitted for your ShareBizz account. One or more items need correction or additional information before verification can be completed.",
                "Document / field requiring attention: {{document_name}}<br>Reason: {{reason}}<br>Action required: {{required_correction}}",
                "Please sign in to your Dashboard and submit the corrected information in the Document Verification section:",
                "<a href=\"{{dashboard_link}}\">{{dashboard_link}}</a>",
                "If you need help understanding the requested correction, contact {{support_email}} and mention reference {{reference_id}}.",
                "Regards,<br>ShareBizz Support"),
            "first_name,document_name,reason,required_correction,dashboard_link,support_email,reference_id",
            "Sent when document review is Resubmission Required. A Rejected decision does not send this email.",
            sent: true),

        Def(PaymentSuccessful, "CE-10 — Payment Successful", "billing",
            "Your ShareBizz payment was successful",
            Html(
                "Dear {{first_name}},",
                "We have successfully received your payment for {{plan_name}}.",
                "Payment summary:<br>Plan / product: {{plan_name}}<br>Base amount: {{base_amount}}<br>Tax: {{tax_amount}}<br>Total paid: {{total_amount}}<br>Payment date: {{payment_date}}<br>Transaction ID: {{transaction_id}}",
                "{{activation_block}}",
                "You can view your payment record here:",
                "<a href=\"{{receipt_link}}\">{{receipt_link}}</a>",
                "If you have any questions about this transaction, contact {{billing_email}}.",
                "Thank you for choosing ShareBizz.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,plan_name,base_amount,tax_amount,total_amount,payment_date,transaction_id,activation_block,receipt_link,billing_email",
            "Sent when a membership or Digital Business Profile payment is confirmed. Reply-To is billing. The invoice email is not sent separately.",
            sent: true),

        Def(PaymentFailed, "CE-11 — Payment Failed / Action Required", "billing",
            "Action required: your ShareBizz payment could not be confirmed",
            Html(
                "Dear {{first_name}},",
                "We could not confirm the payment for {{plan_name}}.",
                "Payment status: {{payment_status}}<br>Transaction reference: {{transaction_reference}}<br>Attempt date: {{attempt_date}}",
                "If you would like to try again, use the secure payment link below:",
                "<a href=\"{{payment_link}}\">{{payment_link}}</a>",
                "Important: If the amount has already been debited from your bank account or payment app, please do not make another payment immediately. Contact {{billing_email}} and share the transaction reference so that the team can check the status.",
                "For other payment assistance, contact {{billing_email}}.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,plan_name,payment_status,transaction_reference,attempt_date,payment_link,billing_email",
            "Sent only when payment signature verification fails. Reply-To is billing.",
            sent: true),

        Def(InvoiceAvailable, "CE-12 — Invoice / Receipt Available", "billing",
            "Your ShareBizz payment document is available",
            Html(
                "Dear {{first_name}},",
                "Your payment document is now available.",
                "Document type: {{document_type}}<br>Document number: {{document_number}}<br>Document date: {{document_date}}<br>Plan / product: {{plan_name}}<br>Total amount: {{total_amount}}",
                "You can view or download the document securely here:",
                "<a href=\"{{document_link}}\">{{document_link}}</a>",
                "Please review the details and contact {{billing_email}} if you notice any discrepancy.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,document_type,document_number,document_date,plan_name,total_amount,document_link,billing_email",
            "Stored for editing. Not sent yet: the receipt is already included in the payment-success email.",
            sent: false),

        Def(ReferralStatement, "CE-13 — Referral Earnings Statement / Review Request", "referral",
            "Your ShareBizz referral earnings statement is ready for review",
            Html(
                "Dear {{first_name}},",
                "Your referral earnings statement for {{earnings_period}} is ready.",
                "Statement summary:<br>{{summary_block}}",
                "Net amount for review: {{net_amount}}",
                "{{review_note}}",
                "View your statement:",
                "<a href=\"{{statement_link}}\">{{statement_link}}</a>",
                "The amount shown is for review and is not a confirmation of payout. Final eligibility and payable amount are subject to the applicable approved terms and verification.",
                "For assistance, contact {{billing_email}}.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,earnings_period,summary_block,net_amount,review_note,statement_link,billing_email",
            "Sent when a referral payout invoice is created for review. Sales splits that are not stored on the earning are omitted. Reply-To is billing.",
            sent: true),

        Def(ReferralQueryResolved, "CE-14 — Referral Earnings Query Resolved", "referral",
            "Update on your ShareBizz referral earnings query",
            Html(
                "Dear {{first_name}},",
                "We have reviewed your query regarding your referral earnings.",
                "Query reference: {{query_reference}}<br>Earnings period: {{earnings_period}}<br>Our review outcome: {{resolution_summary}}",
                "{{updated_amount_block}}",
                "{{updated_statement_block}}",
                "You can view the relevant statement or supporting details here:",
                "<a href=\"{{statement_link}}\">{{statement_link}}</a>",
                "If you believe the issue has not been resolved, reply to this email with the query reference and the specific point that still needs review.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,query_reference,earnings_period,resolution_summary,updated_amount_block,updated_statement_block,statement_link,billing_email",
            "Stored for editing. Not sent yet: there is no referral earnings query workflow.",
            sent: false),

        Def(ReferralApproved, "CE-15 — Referral Earnings Approved / Payout Scheduled", "referral",
            "Your ShareBizz referral earnings have been approved",
            Html(
                "Dear {{first_name}},",
                "Your referral earnings for {{earnings_period}} have been reviewed and approved for payout.",
                "Approved net amount: {{approved_net_amount}}<br>Scheduled payout date: {{payout_date}}<br>Payout reference: {{payout_reference}}",
                "The payout will be processed to the verified payment destination associated with your account, subject to the applicable payout checks.",
                "You can view the approved statement here:",
                "<a href=\"{{statement_link}}\">{{statement_link}}</a>",
                "If your verified bank or UPI details have changed, contact {{billing_email}} before the scheduled payout date. Do not send sensitive banking information in an unsecured reply.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,earnings_period,approved_net_amount,payout_date,payout_reference,statement_link,billing_email",
            "Stored for editing. Not sent yet: payout is recorded immediately and there is no scheduled-payout state.",
            sent: false),

        Def(ReferralPayout, "CE-16 — Referral Payout Processed", "referral",
            "Your ShareBizz referral payout has been processed",
            Html(
                "Dear {{first_name}},",
                "Your ShareBizz referral payout for {{earnings_period}} has been processed.",
                "Net amount paid: {{net_payout}}<br>Processing date: {{payment_date}}<br>Payment reference / UTR: {{utr}}<br>Destination: {{destination}}",
                "Please allow the usual processing time of your bank or payment provider for the amount to appear in your account.",
                "If the amount is not reflected after the applicable bank processing period, contact {{billing_email}} and quote the payment reference above.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,earnings_period,net_payout,payment_date,utr,destination,billing_email",
            "Sent when an admin records a referral payout that completes the earning. Accepting an invoice in the dashboard does not send this email. Reply-To is billing.",
            sent: true),

        Def(MembershipExpiry, "CE-17 — Membership / Subscription Expiry Reminder", "billing",
            "Reminder: your {{plan_name}} is due to expire on {{expiry_date}}",
            Html(
                "Dear {{first_name}},",
                "This is a reminder that your {{plan_name}} is scheduled to expire on {{expiry_date}}.",
                "Renewal details:<br>Plan: {{plan_name}}<br>Current validity ends: {{expiry_date}}<br>Applicable renewal amount: {{renewal_amount}}<br>Renewal link: <a href=\"{{renewal_link}}\">{{renewal_link}}</a>",
                "{{renewal_note}}",
                "Please review the plan details and complete any required renewal before the expiry date if you wish to continue the applicable paid membership or subscription.",
                "If you have already renewed, please ignore this reminder. For billing assistance, contact {{billing_email}}.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,plan_name,expiry_date,renewal_amount,renewal_link,renewal_note,billing_email",
            "Stored for editing. Not sent yet: membership has no end date and there is no renewal scheduler.",
            sent: false),

        Def(RenewalSuccessful, "CE-18 — Membership / Subscription Renewal Successful", "billing",
            "Your ShareBizz renewal was successful",
            Html(
                "Dear {{first_name}},",
                "Your renewal for {{plan_name}} has been confirmed.",
                "Renewal summary:<br>Plan: {{plan_name}}<br>Amount paid: {{total_amount}}<br>Transaction ID: {{transaction_id}}<br>Payment date: {{payment_date}}<br>New validity: {{start_date}} to {{end_date}}",
                "{{benefit_block}}",
                "Your payment document is available here:",
                "<a href=\"{{receipt_link}}\">{{receipt_link}}</a>",
                "Thank you for continuing with ShareBizz.",
                "For assistance, contact {{billing_email}}.",
                "Regards,<br>ShareBizz Billing Team"),
            "first_name,plan_name,total_amount,transaction_id,payment_date,start_date,end_date,benefit_block,receipt_link,billing_email",
            "Stored for editing. Not sent yet: there is no renewal checkout.",
            sent: false),

        Def(ComplimentaryBenefit, "CE-19 — Complimentary DB Profile Benefit Status", "hr",
            "Update on your complimentary ShareBizz DB Profile benefit",
            Html(
                "Dear {{first_name}},",
                "This email is to update you about the complimentary ShareBizz Digital Business Profile (DB Profile) benefit associated with your {{user_type}} status.",
                "Current benefit status: {{benefit_status}}<br>Current access end date: {{current_end_date}}",
                "{{benefit_policy_block}}",
                "If any action is required from you, the details are provided below:",
                "{{required_action_block}}",
                "For clarification or assistance, contact {{support_email}}. For employee-related benefit decisions, contact {{hr_email}}.",
                "Regards,<br>{{signoff}}"),
            "first_name,user_type,benefit_status,current_end_date,benefit_policy_block,required_action_block,support_email,hr_email,signoff",
            "Stored for editing. Not sent yet: complimentary access is only a Yes/No card flag.",
            sent: false),

        Def(FstAppointment, "CE-20 — FST Appointment Letter / Employment Offer", "hr",
            "ShareBizz — Field Sales Team appointment letter",
            Html(
                "Dear {{first_name}},",
                "We are pleased to share your appointment details for the position of {{designation}} with {{employer_name}}.",
                "Employee ID: {{employee_id}}<br>Designation: {{designation}}<br>Proposed joining date: {{joining_date}}<br>Reporting to: {{reporting_manager}}",
                "Please review the appointment letter and the terms stated in it:",
                "<a href=\"{{appointment_letter_link}}\">{{appointment_letter_link}}</a>",
                "Your employment terms, compensation, responsibilities and applicable conditions will be governed by the approved appointment letter and related company policies.",
                "Please complete the acceptance and joining formalities described in the letter. If you have questions, contact {{hr_email}}.",
                "Regards,<br>ShareBizz HR Team<br>{{employer_name}}"),
            "first_name,designation,employer_name,employee_id,joining_date,reporting_manager,appointment_letter_link,hr_email",
            "Stored for editing. Not sent yet: uploading an appointment letter file does not issue an offer.",
            sent: false),

        Def(FstOnboarding, "CE-21 — FST Employee Onboarding / Welcome", "hr",
            "Welcome to the ShareBizz Field Sales Team",
            Html(
                "Dear {{first_name}},",
                "Welcome to the ShareBizz Field Sales Team!",
                "We are pleased to have you join us.",
                "Your employee details:<br>Employee ID: {{employee_id}}<br>Designation: {{designation}}<br>Joining date: {{joining_date}}<br>Reporting manager: {{reporting_manager}}",
                "Please complete the following onboarding steps:",
                "{{onboarding_steps}}",
                "Access your assigned system or dashboard here:",
                "<a href=\"{{login_link}}\">{{login_link}}</a>",
                "Your role responsibilities, salary and incentive terms will be as stated in your appointment letter and applicable company policies.",
                "For onboarding assistance, contact {{hr_email}}.",
                "Wishing you a successful journey with ShareBizz.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,employee_id,designation,joining_date,reporting_manager,onboarding_steps,login_link,hr_email",
            "Stored for editing. Not sent yet: Field Sales account mail uses the registration invitation and account credentials emails. Employee id, manager, and joining date are not a separate HR record.",
            sent: false),

        Def(FstPerformanceReport, "CE-22 — FST Monthly Sales & Performance Report", "hr",
            "Your ShareBizz monthly sales and performance report — {{month_year}}",
            Html(
                "Dear {{first_name}},",
                "Your sales and performance report for {{reporting_period}} is ready.",
                "Performance summary:<br>Assigned target: {{target}}<br>Achieved sales: {{achieved_sales}}<br>Target achievement: {{achievement_percentage}}<br>DB Profile sales: {{db_profile_sales}}<br>SB Franchise sales: {{sb_franchise_sales}}",
                "You can view the detailed report here:",
                "<a href=\"{{report_link}}\">{{report_link}}</a>",
                "Please review the figures and contact {{manager_contact}} if you notice any discrepancy or need clarification.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,month_year,reporting_period,target,achieved_sales,achievement_percentage,db_profile_sales,sb_franchise_sales,report_link,manager_contact",
            "Stored for editing. Not sent yet: monthly performance is shown on screen and is not issued as a report.",
            sent: false),

        Def(FstSalaryStatement, "CE-23 — FST Monthly Salary Statement", "hr",
            "Your ShareBizz salary statement — {{month_year}}",
            Html(
                "Dear {{first_name}},",
                "Please find the salary calculation summary for {{salary_period}}.",
                "Salary summary:<br>Fixed salary: {{fixed_salary}}<br>Eligible incentive: {{eligible_incentive}}<br>Allowances: {{allowances}}<br>Adjustments / deductions: {{adjustments}}<br>Net payable: {{net_payable}}",
                "View the detailed salary statement securely:",
                "<a href=\"{{statement_link}}\">{{statement_link}}</a>",
                "This is a salary calculation statement for the stated period. The final payable amount will be reflected in the approved payroll records and salary slip.",
                "For any discrepancy, contact {{hr_email}}.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,month_year,salary_period,fixed_salary,eligible_incentive,allowances,adjustments,net_payable,statement_link,hr_email",
            "Stored for editing. Not sent yet: salary statements are not stored.",
            sent: false),

        Def(FstSalarySlip, "CE-24 — FST Salary Slip Generated", "hr",
            "Your ShareBizz salary slip for {{month_year}} is available",
            Html(
                "Dear {{first_name}},",
                "Your salary slip for {{salary_month}} has been generated.",
                "You can securely view or download it here:",
                "<a href=\"{{salary_slip_link}}\">{{salary_slip_link}}</a>",
                "For questions about the salary slip, please contact {{hr_email}}.",
                "Please keep your salary documents confidential and do not share the secure document link with others.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,month_year,salary_month,salary_slip_link,hr_email",
            "Stored for editing. Not sent yet: salary slips are not generated.",
            sent: false),

        Def(FstSalaryPayment, "CE-25 — FST Salary Payment Processed", "hr",
            "Your ShareBizz salary payment has been processed",
            Html(
                "Dear {{first_name}},",
                "Your salary payment for {{salary_period}} has been processed.",
                "Net amount: {{net_amount}}<br>Payment date: {{payment_date}}<br>Payment reference / UTR: {{utr}}<br>Destination: {{destination}}",
                "If the amount is not reflected in your account after the applicable bank processing period, please contact {{hr_email}} with the payment reference.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,salary_period,net_amount,payment_date,utr,destination,hr_email",
            "Stored for editing. Not sent yet: salary payments are not recorded.",
            sent: false),

        Def(FstTargetUpdate, "CE-26 — FST Sales Target / Performance Update", "hr",
            "Your ShareBizz sales target and performance update",
            Html(
                "Dear {{first_name}},",
                "Here is your sales performance update for {{review_period}}.",
                "Assigned target: {{assigned_target}}<br>Sales achieved: {{achieved_sales}}<br>Target achievement: {{achievement_percentage}}<br>Remaining target: {{remaining_target}}",
                "{{manager_feedback_block}}",
                "Please review the figures and connect with {{reporting_manager}} if you need clarification or would like to discuss your action plan.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,review_period,assigned_target,achieved_sales,achievement_percentage,remaining_target,manager_feedback_block,reporting_manager",
            "Stored for editing. Not sent yet: sales targets are not issued as a message.",
            sent: false),

        Def(ReferralActivation, "SE-01 — SBU 45-Day Referral Feature Activation", "referral",
            "Your ShareBizz referral features are now available",
            Html(
                "Dear {{first_name}},",
                "Your ShareBizz account is now eligible for access to the referral-related features in your Dashboard.",
                "You can now explore the available referral tools, review the applicable programme information and share the relevant ShareBizz referral links with prospective customers.",
                "Open your Dashboard:",
                "<a href=\"{{dashboard_link}}\">{{dashboard_link}}</a>",
                "Read the referral programme details:",
                "<a href=\"{{programme_link}}\">{{programme_link}}</a>",
                "Please review the applicable programme terms before sharing any referral link. Referral earnings, where applicable, are subject to the programme rules and verification.",
                "For assistance, contact {{support_email}}.",
                "Regards,<br>ShareBizz Support"),
            "first_name,dashboard_link,programme_link,support_email",
            "Sent once, on the next portal visit after the 45-day referral eligibility date, for a ShareBizz user.",
            sent: true),

        Def(ProspectOtp, "SE-02 — Prospect Email OTP for FST Registration", "auth",
            "Verify your email address for ShareBizz registration",
            Html(
                "Dear {{first_name}},",
                "{{fst_name}}, a ShareBizz Field Sales Team employee, has initiated your {{registration_type}} registration as part of the employee's on-ground sales and customer onboarding work for ShareBizz.",
                "To verify that you own this email address, enter the following OTP:",
                "<strong>{{otp}}</strong>",
                "This OTP is valid for {{otp_validity}}. Do not share it with anyone.",
                "If you requested this registration, enter the OTP in the registration process to continue.",
                "If you did not request or authorise this registration, do not share the OTP and contact {{support_email}}.",
                "Regards,<br>ShareBizz<br>This is an automated verification email. Please do not reply."),
            "first_name,fst_name,registration_type,otp,otp_validity,support_email",
            "Stored for editing. Not sent yet: Field Sales print-shop registration does not verify the prospect with an OTP. This is not an FRP or FSP referral workflow.",
            sent: false),

        Def(ProspectLoginPayment, "SE-03 — Prospect Login Details and Payment Link", "auth",
            "Your ShareBizz registration details and payment link",
            Html(
                "Dear {{first_name}},",
                "Your {{registration_type}} registration, initiated by {{fst_name}}, a ShareBizz Field Sales Team employee, has been recorded.",
                "Your account details:",
                "Login ID: {{login_id}}<br>Password: {{password}}",
                "Login page: <a href=\"{{login_link}}\">{{login_link}}</a>",
                "To complete the applicable purchase, please use the payment link below:",
                "<a href=\"{{payment_link}}\">{{payment_link}}</a>",
                "Plan / product: {{plan_name}}",
                "{{amount_block}}",
                "Please complete the remaining onboarding steps shown in your account after payment.",
                "If you have questions about registration or payment, contact {{support_email}} or {{billing_email}}, as applicable.",
                "Regards,<br>ShareBizz Team"),
            "first_name,registration_type,fst_name,login_id,password,login_link,payment_link,plan_name,amount_block,support_email,billing_email",
            "Sent when a Field Sales Team member registers a Print Shop Partner. The password is included. The payable amount is included only when billing has confirmed it. This is not an FRP or FSP referral workflow.",
            sent: true),

        Def(FstComplimentaryDecision, "SE-04 — FST Complimentary DB Profile Continuation", "hr",
            "Update on your complimentary ShareBizz DB Profile access",
            Html(
                "Dear {{first_name}},",
                "Your initial complimentary ShareBizz Digital Business Profile (DB Profile) access is scheduled to end on {{end_date}}.",
                "Following the applicable internal review, the decision regarding continuation is:",
                "{{hr_decision}}",
                "{{continued_block}}",
                "{{not_continued_block}}",
                "{{under_review_block}}",
                "For questions, please contact {{hr_email}}.",
                "Regards,<br>ShareBizz HR Team"),
            "first_name,end_date,hr_decision,continued_block,not_continued_block,under_review_block,hr_email",
            "Stored for editing. Not sent yet: there is no complimentary continuation decision. Include only the paragraph that matches the decision.",
            sent: false),
    ];

    public static readonly (string Key, string Description)[] Superseded =
    [
        ("franchisee_welcome", "Superseded by CE-03 (customer_welcome). The app sends CE-03 instead."),
        ("franchisee_welcome_no_payment", "Superseded by CE-03 (customer_welcome). The app sends CE-03 instead."),
        ("franchisee_admin_created", "Superseded by CE-04 (ce_04_account_credentials). The app sends CE-04 instead."),
        ("team_member_welcome", "Superseded by CE-04 (ce_04_account_credentials). The app sends CE-04 instead."),
    ];

    private static readonly (string Key, string Name, string Subject)[] LegacyContent =
    [
        (EmailVerificationOtp, "Registration OTP", "{{host}} - Your OTP for Registration"),
        (RegistrationWelcome, "Customer Welcome", "Welcome to {{host}}"),
        (PasswordResetOtp, "Password Reset OTP", "Password reset code"),
    ];

    public static EmailLibraryDefinition? Find(string templateKey) =>
        All.FirstOrDefault(d => string.Equals(d.Key, templateKey, StringComparison.OrdinalIgnoreCase));

    public static bool ShouldReplaceLegacy(string templateKey, string name, string subject)
    {
        foreach (var legacy in LegacyContent)
        {
            if (!string.Equals(legacy.Key, templateKey, StringComparison.OrdinalIgnoreCase)) continue;
            return string.Equals(name, legacy.Name, StringComparison.Ordinal)
                || string.Equals(subject, legacy.Subject, StringComparison.Ordinal);
        }
        return false;
    }

    private static EmailLibraryDefinition Def(
        string key, string name, string category, string subject, string body, string variables, string description, bool sent) =>
        new(key, name, category, subject, body, variables, description, sent);

    private static string Html(params string[] paragraphs) =>
        string.Concat(paragraphs.Select(p =>
            p.StartsWith("{{", StringComparison.Ordinal) && p.EndsWith("}}", StringComparison.Ordinal)
                ? p
                : "<p>" + p + "</p>"));
}

public sealed record EmailLibraryDefinition(
    string Key,
    string Name,
    string Category,
    string Subject,
    string BodyHtml,
    string Variables,
    string Description,
    bool Sent);

public static class EmailLibraryMessages
{
    private static readonly CultureInfo India = CultureInfo.GetCultureInfo("en-IN");
    private static readonly Regex Placeholder = new(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}", RegexOptions.Compiled);

    public static readonly HashSet<string> RawHtmlKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "payment_block", "next_steps_block", "invitation_context", "activation_block", "device_block",
        "summary_block", "amount_block", "document_list", "updated_amount_block", "updated_statement_block",
        "required_action_block", "benefit_policy_block", "manager_feedback_block", "onboarding_steps",
        "benefit_block", "renewal_note", "continued_block", "not_continued_block", "under_review_block",
    };

    public static string Inr(decimal value) => "₹" + value.ToString("N2", India);

    public static string Date(DateTime value) => value.ToString("dd MMM yyyy", India);

    public static string Time(DateTime value) => value.ToString("HH:mm", India);

    public static string FirstWord(string? name)
    {
        var text = (name ?? "").Trim();
        if (text.Length == 0) return "there";
        var space = text.IndexOf(' ');
        return space < 0 ? text : text[..space];
    }

    public static string Paragraph(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return "<p>" + WebUtility.HtmlEncode(text.Trim()) + "</p>";
    }

    public static string PaymentBlock(string? url, string? label = null, string? amount = null)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        var safeUrl = WebUtility.HtmlEncode(url.Trim());
        var intro = string.IsNullOrWhiteSpace(label)
            ? "To complete the applicable payment, use this secure link:"
            : "To pay " + WebUtility.HtmlEncode(label.Trim()) + ", use this secure link:";
        var amountLine = string.IsNullOrWhiteSpace(amount)
            ? ""
            : "<br>Amount payable: " + WebUtility.HtmlEncode(amount.Trim());
        return "<p>" + intro + amountLine + "</p><p><a href=\"" + safeUrl + "\">" + safeUrl + "</a></p>";
    }

    public static string MaskDestination(string? accountNumber, string? upiId)
    {
        var digits = new string((accountNumber ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length >= 4) return "Bank account ending " + digits[^4..];
        var upi = (upiId ?? "").Trim();
        if (upi.Length >= 4) return "UPI ending " + upi[^4..];
        return "the verified payment destination on your account";
    }

    public static Dictionary<string, string?> PaymentSuccess(
        string? firstName, Invoice invoice, string receiptLink, string activationHtml)
    {
        var when = invoice.PaymentDate ?? AppClock.Now;
        var transaction = string.IsNullOrWhiteSpace(invoice.RazorpayPaymentId)
            ? invoice.InvoiceNumber
            : invoice.RazorpayPaymentId;
        return new Dictionary<string, string?>
        {
            ["first_name"] = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName,
            ["plan_name"] = string.IsNullOrWhiteSpace(invoice.PlanName) ? invoice.ServiceName : invoice.PlanName,
            ["base_amount"] = Inr(invoice.SubTotal),
            ["tax_amount"] = Inr(invoice.CgstAmount + invoice.SgstAmount + invoice.IgstAmount),
            ["total_amount"] = Inr(invoice.TotalAmount),
            ["payment_date"] = Date(when),
            ["transaction_id"] = transaction,
            ["receipt_link"] = receiptLink,
            ["activation_block"] = activationHtml,
        };
    }

    public static Dictionary<string, string?> PaymentFailed(
        string? firstName, Invoice invoice, string paymentLink, DateTime attempt)
    {
        var reference = string.IsNullOrWhiteSpace(invoice.RazorpayPaymentId)
            ? invoice.RazorpayOrderId
            : invoice.RazorpayPaymentId;
        if (string.IsNullOrWhiteSpace(reference)) reference = invoice.InvoiceNumber;
        return new Dictionary<string, string?>
        {
            ["first_name"] = string.IsNullOrWhiteSpace(firstName) ? "there" : firstName,
            ["plan_name"] = string.IsNullOrWhiteSpace(invoice.PlanName) ? invoice.ServiceName : invoice.PlanName,
            ["payment_status"] = "Could not be confirmed",
            ["transaction_reference"] = reference,
            ["attempt_date"] = Date(attempt),
            ["payment_link"] = paymentLink,
        };
    }

    public static IReadOnlyCollection<string> Placeholders(string template) =>
        Placeholder.Matches(template ?? "").Select(m => m.Groups[1].Value).ToArray();
}
