namespace MiddlewareInfocert.Models;

public sealed class SignAutoPadesRequest
{
    public string ObjectId { get; set; } = string.Empty;   // id documento su DocsMarshal
    public string Alias { get; set; } = string.Empty;      // username/CF del medico
    public string Pin { get; set; } = string.Empty;        // pin di firma (non persistito)
    
    public string NomeMedico { get; set; } = string.Empty; // nome del medico, usato per la filigrana
}
