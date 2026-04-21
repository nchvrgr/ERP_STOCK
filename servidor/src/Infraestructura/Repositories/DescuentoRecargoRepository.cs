using Microsoft.EntityFrameworkCore;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.DescuentosRecargos;
using Servidor.Dominio.Entities;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;
using Servidor.Infraestructura.Persistence;

namespace Servidor.Infraestructura.Repositories;

public sealed class DescuentoRecargoRepository : IDescuentoRecargoRepository
{
    private readonly PosDbContext _dbContext;

    public DescuentoRecargoRepository(PosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<DescuentoRecargoDto>> SearchAsync(
        Guid tenantId,
        DescuentoRecargoTipo? tipo,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<DescuentoRecargo>().AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (tipo.HasValue)
        {
            query = query.Where(x => x.Tipo == tipo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var pattern = $"%{term}%";
            query = _dbContext.Database.IsNpgsql()
                ? query.Where(x => EF.Functions.ILike(x.Name, pattern))
                : query.Where(x => EF.Functions.Like(x.Name, pattern));
        }

        return await query
            .OrderBy(x => x.Tipo)
            .ThenBy(x => x.Name)
            .Select(x => new DescuentoRecargoDto(
                x.Id,
                x.Name,
                x.Porcentaje,
                x.Tipo == DescuentoRecargoTipo.Descuento ? "DESCUENTO" : "RECARGO"))
            .ToListAsync(cancellationToken);
    }

    public async Task<DescuentoRecargoDto?> GetByIdAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<DescuentoRecargo>().AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == descuentoRecargoId)
            .Select(x => new DescuentoRecargoDto(
                x.Id,
                x.Name,
                x.Porcentaje,
                x.Tipo == DescuentoRecargoTipo.Descuento ? "DESCUENTO" : "RECARGO"))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        DescuentoRecargoCreateDto request,
        DescuentoRecargoTipo tipo,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();
        var exists = await _dbContext.Set<DescuentoRecargo>().AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId
                    && x.Tipo == tipo
                    && x.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException("Ya existe un descuento/recargo con ese nombre.");
        }

        var entity = new DescuentoRecargo(
            Guid.NewGuid(),
            tenantId,
            normalizedName,
            request.Porcentaje ?? 0,
            tipo,
            nowUtc);

        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> UpdateAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        DescuentoRecargoUpdateDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<DescuentoRecargo>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == descuentoRecargoId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        var newName = request.Name?.Trim() ?? entity.Name;
        var newPorcentaje = request.Porcentaje ?? entity.Porcentaje;

        var exists = await _dbContext.Set<DescuentoRecargo>().AsNoTracking()
            .AnyAsync(
                x => x.TenantId == tenantId
                    && x.Tipo == entity.Tipo
                    && x.Id != descuentoRecargoId
                    && x.Name.ToLower() == newName.ToLower(),
                cancellationToken);

        if (exists)
        {
            throw new ConflictException("Ya existe un descuento/recargo con ese nombre.");
        }

        entity.Update(newName, newPorcentaje, nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<DescuentoRecargo>()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == descuentoRecargoId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
