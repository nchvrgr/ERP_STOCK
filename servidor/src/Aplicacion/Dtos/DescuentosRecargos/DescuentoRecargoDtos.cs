namespace Servidor.Aplicacion.Dtos.DescuentosRecargos;

public sealed record DescuentoRecargoDto(
    Guid Id,
    string Name,
    decimal Porcentaje,
    string Tipo);

public sealed record DescuentoRecargoCreateDto(
    string Name,
    decimal? Porcentaje,
    string Tipo);

public sealed record DescuentoRecargoUpdateDto(
    string? Name,
    decimal? Porcentaje);
