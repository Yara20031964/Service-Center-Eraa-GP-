using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace KHDMA.API.Swagger;

/// <summary>
/// The Swagger section names, in the order they should appear: every ADMIN group,
/// then CUSTOMER, then PROVIDER, then the COMMON groups that serve more than one
/// role, then the anonymous PUBLIC catalogue.
/// </summary>
/// <remarks>
/// Swagger UI has no concept of a "role", so the ordering has to be carried in the
/// tag name itself - hence the numeric prefix. Sections are attached with
/// <c>[Tags(ApiTags.X)]</c> on a controller, or on a single action when one
/// controller serves two audiences (BookingController does: the customer creates a
/// booking, the provider accepts it, the admin exports them all).
/// </remarks>
public static class ApiTags
{
    // ---- Admin -------------------------------------------------------------
    public const string AdminDashboard    = "01 ADMIN - Dashboard";
    public const string AdminAdmins       = "02 ADMIN - Admins & Roles";
    public const string AdminCustomers    = "03 ADMIN - Customers";
    public const string AdminProviders    = "04 ADMIN - Providers";
    public const string AdminCatalog      = "05 ADMIN - Catalog (Categories & Services)";
    public const string AdminBookings     = "06 ADMIN - Bookings";
    public const string AdminFinance      = "07 ADMIN - Payments, Commission & Reports";
    public const string AdminContent      = "08 ADMIN - Content, Policy & Payouts";
    public const string AdminModeration   = "09 ADMIN - Reviews & Moderation";
    public const string AdminNotifications = "10 ADMIN - Notifications";
    public const string AdminChat         = "11 ADMIN - Chat Oversight";

    // ---- Customer ----------------------------------------------------------
    public const string CustomerBookings  = "12 CUSTOMER - Bookings";
    public const string CustomerPayments  = "13 CUSTOMER - Payments";
    public const string CustomerFavorites = "14 CUSTOMER - Favorites";
    public const string CustomerReviews   = "15 CUSTOMER - Reviews";

    // ---- Provider ----------------------------------------------------------
    public const string ProviderJobs      = "16 PROVIDER - Jobs (dispatch & lifecycle)";
    public const string ProviderLocation  = "17 PROVIDER - Live Location";
    public const string ProviderEarnings  = "18 PROVIDER - Earnings, Wallet & Payouts";
    public const string ProviderReviews   = "19 PROVIDER - Review Replies";

    // ---- Common (more than one role calls the same route) -------------------
    public const string CommonAuth        = "20 COMMON - Authentication";
    public const string CommonProfile     = "21 COMMON - Profile & Addresses";
    public const string CommonBookings    = "22 COMMON - Booking history";
    public const string CommonChat        = "23 COMMON - Chat";
    public const string CommonNotifications = "24 COMMON - Notifications";
    public const string CommonFiles       = "25 COMMON - Files";

    // ---- Public ------------------------------------------------------------
    public const string PublicCatalog     = "26 PUBLIC - Catalog (no token required)";

    /// <summary>
    /// Section order plus the blurb Swagger UI prints under each heading. This is
    /// the only place the order is defined; <see cref="TagOrderDocumentFilter"/>
    /// reads it.
    /// </summary>
    public static readonly (string Tag, string Description)[] Ordered =
    {
        (AdminDashboard,    "**Admin only.** Platform-wide counters for the admin home screen."),
        (AdminAdmins,       "**Admin only.** Create and deactivate admin accounts."),
        (AdminCustomers,    "**Admin only.** Browse, suspend, ban and restore customers."),
        (AdminProviders,    "**Admin only.** Approve or reject applicants, then moderate active providers."),
        (AdminCatalog,      "**Admin only.** The categories and services customers can book. Public read-only copies of these live in section 25."),
        (AdminBookings,     "**Admin only.** Every booking on the platform, plus force-cancel, status history and CSV/Excel export. Customers see only their own in section 12."),
        (AdminFinance,      "**Admin only.** Payments, refunds, commission rate and revenue reports."),
        (AdminContent,      "**Admin only.** Banners, the cancellation policy, and approving provider payout requests."),
        (AdminModeration,   "**Admin only.** Hide or delete reviews, edit notification templates, bulk provider actions."),
        (AdminNotifications,"**Admin only.** Broadcast to everyone, or push to one user."),
        (AdminChat,         "**Admin only.** Read a booking's chat transcript for a dispute."),

        (CustomerBookings,  "**Customer.** Create a booking (dispatch or direct), cancel it, retry a failed payment, and watch it via the ETA endpoint. The provider's side of the same booking is section 16."),
        (CustomerPayments,  "**Customer.** Start a payment, confirm it, or request a refund. `POST /api/payments/webhook` is called by the payment gateway, not by the app."),
        (CustomerFavorites, "**Customer.** Save providers for re-booking."),
        (CustomerReviews,   "**Customer.** Rate a completed booking, or edit a rating already left. The provider replies in section 19."),

        (ProviderJobs,      "**Provider.** The dispatch flow: accept or reject a broadcast job card, then walk it En Route -> Arrived -> In Progress -> Completed. `accept` is first-come-first-served: exactly one caller gets 200, the rest get 409 and should dismiss the card."),
        (ProviderLocation,  "**Provider.** Push the current position while travelling. Cached ~5 min, never stored as a trail (SRS 7.2)."),
        (ProviderEarnings,  "**Provider.** Earnings summary, wallet balance and payout requests. Admin approves them in section 08."),
        (ProviderReviews,   "**Provider.** Reply once to a review left on your work."),

        (CommonAuth,        "**Common - everyone.** Register, log in, refresh, log out. Note the two login routes: customers and providers use `POST /api/auth/login`, admins must use `POST /api/auth/login/admin`."),
        (CommonProfile,     "**Common - Customer + Provider.** One profile controller serves both. Addresses are used by customers; certificates and portfolio images are only meaningful for providers."),
        (CommonBookings,    "**Common - Customer + Provider.** One route, scoped by the token: a customer gets the bookings they raised, a provider the ones they were assigned."),
        (CommonChat,        "**Common - Customer + Provider.** Both parties of a booking use the identical routes; the server decides which side you are from the token. Locked automatically once the booking is Completed or Cancelled."),
        (CommonNotifications,"**Common - everyone.** The in-app inbox and device token registration."),
        (CommonFiles,       "**Common - Customer + Provider.** Shared upload endpoint."),

        (PublicCatalog,     "**Public - no token.** Categories, services and provider profiles for the browse-before-signup screens."),
    };

    /// <summary>
    /// Resolves the Swagger section for one endpoint: the action's own
    /// <c>[Tags]</c> wins over the controller's, and a controller with neither
    /// falls back to its name so a newly added one is still visible (the document
    /// filter then flags it as untagged).
    /// </summary>
    public static IList<string> SelectFor(ApiDescription api)
    {
        if (api.ActionDescriptor is ControllerActionDescriptor action)
        {
            var onAction = action.MethodInfo.GetCustomAttribute<TagsAttribute>(inherit: false);
            if (onAction is not null) return onAction.Tags.ToList();

            var onController = action.ControllerTypeInfo.GetCustomAttribute<TagsAttribute>(inherit: true);
            if (onController is not null) return onController.Tags.ToList();

            return new[] { action.ControllerName };
        }

        api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name);
        return new[] { name ?? "Other" };
    }
}
