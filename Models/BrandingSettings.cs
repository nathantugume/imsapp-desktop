using System.Text.Json.Serialization;

namespace imsapp_desktop.Models;

public class BrandingSettings
{
    [JsonPropertyName("business_name")]
    public string BusinessName { get; set; } = "Mini Price Hardware";

    [JsonPropertyName("business_name_short")]
    public string BusinessNameShort { get; set; } = "Mini Price";

    [JsonPropertyName("business_tagline")]
    public string BusinessTagline { get; set; } = "Quality Hardware at Affordable Prices";

    [JsonPropertyName("business_address")]
    public string BusinessAddress { get; set; } = "Kampala, Uganda";

    [JsonPropertyName("business_phone")]
    public string BusinessPhone { get; set; } = "+256 XXX XXXXXX";

    [JsonPropertyName("business_email")]
    public string BusinessEmail { get; set; } = "info@minipricehardware.com";

    [JsonPropertyName("currency_symbol")]
    public string CurrencySymbol { get; set; } = "ugx";

    [JsonPropertyName("low_stock_threshold")]
    public int LowStockThreshold { get; set; } = 30;

    [JsonPropertyName("critical_stock_threshold")]
    public int CriticalStockThreshold { get; set; } = 10;

    [JsonPropertyName("expiry_warning_days")]
    public int ExpiryWarningDays { get; set; } = 90;

    [JsonPropertyName("expiry_critical_days")]
    public int ExpiryCriticalDays { get; set; } = 30;

    public string FormatCurrency(decimal value) => $"{CurrencySymbol} {value:N2}";
    public string FormatCurrency(double value) => $"{CurrencySymbol} {value:N2}";
}
