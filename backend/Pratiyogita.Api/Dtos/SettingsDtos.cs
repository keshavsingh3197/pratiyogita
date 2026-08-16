namespace Pratiyogita.Api.Dtos;

public sealed record UpdatePaymentSettingsRequest(string UpiVpa, string PayeeName);

public sealed record PaymentSettingsDto(bool Configured, string? UpiVpa, string PayeeName);
