using Servidor.Aplicacion.Dtos.Precios;
using Servidor.Dominio.Enums;

namespace Servidor.Aplicacion.CasosDeUso.Precios;

public interface IPromoStrategy
{
    PromocionTipo Tipo { get; }

    PromoApplyResult Apply(PromocionAplicableDto promo, PricingContext context);
}


