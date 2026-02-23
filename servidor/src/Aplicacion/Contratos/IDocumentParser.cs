using System.Text.Json;
using Servidor.Aplicacion.Dtos.DocumentosCompra;

namespace Servidor.Aplicacion.Contratos;

public interface IDocumentParser
{
    ParsedDocumentDto Parse(JsonElement input);
}


