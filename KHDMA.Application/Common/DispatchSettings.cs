namespace KHDMA.Application.Common
{
    /// <summary>
    /// Bound from the <c>Dispatch</c> configuration section via IOptions.
    /// The defaults here are the SRS values, so an empty config still behaves correctly.
    /// </summary>
    public class DispatchSettings
    {
        public const string SectionName = "Dispatch";

        /// <summary>Radius for the first broadcast round.</summary>
        public double InitialRadiusKm { get; set; } = 10;

        /// <summary>Rounds stop expanding past this.</summary>
        public double MaxRadiusKm { get; set; } = 30;

        /// <summary>Added to the radius on each subsequent round.</summary>
        public double RadiusIncrementKm { get; set; } = 10;

        /// <summary>How long a provider has to accept before the round expires.</summary>
        public int AcceptTimeoutSeconds { get; set; } = 60;

        /// <summary>After this many rounds without an accept, the booking becomes NoProviderFound.</summary>
        public int MaxRounds { get; set; } = 3;

        /// <summary>Cap on how many providers one round notifies, nearest first.</summary>
        public int MaxProvidersPerRound { get; set; } = 20;

        /// <summary>
        /// How old a provider's position may be and still be dispatchable.
        /// </summary>
        /// <remarks>
        /// An idle Online provider refreshes only on the app's heartbeat, so this
        /// must stay comfortably above that interval or genuinely available
        /// providers get filtered out. Rows written before this column existed have
        /// a null timestamp and are treated as fresh, so enabling the filter cannot
        /// silently empty the candidate pool on deploy.
        /// </remarks>
        public int MaxLocationAgeMinutes { get; set; } = 30;
    }

    /// <summary>Bound from the <c>Vat</c> section.</summary>
    public class VatSettings
    {
        public const string SectionName = "Vat";

        /// <summary>0.10 renders as the "VAT (10%)" line in the app.</summary>
        public decimal Rate { get; set; } = 0.10m;

        public string Currency { get; set; } = "EGP";
    }

    /// <summary>Bound from the <c>FileUpload</c> section. SRS 10.3.</summary>
    public class FileUploadSettings
    {
        public const string SectionName = "FileUpload";

        public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024;

        public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".pdf"];
    }
}
