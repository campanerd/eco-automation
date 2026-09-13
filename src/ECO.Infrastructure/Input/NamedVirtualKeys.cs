namespace ECO.Infrastructure.Input;

// Teclas "nomeadas" — sem representação de caractere visível — que o WindowsInputRecorder
// reconhece como KeyPressStep (em vez de acumular no buffer de texto) e que o
// WindowsInputPlayer sabe reproduzir a partir do nome. Uma tabela só, usada nas duas direções,
// pra recorder e player nunca ficarem com listas de teclas divergentes.
internal static class NamedVirtualKeys
{
    private static readonly (string Name, ushort Code)[] Entries =
    [
        ("Enter", 0x0D),
        ("Tab", 0x09),
        ("Escape", 0x1B),
        ("Backspace", 0x08),
        ("Delete", 0x2E),
        ("Up", 0x26),
        ("Down", 0x28),
        ("Left", 0x25),
        ("Right", 0x27),
        ("Home", 0x24),
        ("End", 0x23),
    ];

    public static readonly IReadOnlyDictionary<string, ushort> ByName =
        Entries.ToDictionary(e => e.Name, e => e.Code, StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyDictionary<ushort, string> ByCode =
        Entries.ToDictionary(e => e.Code, e => e.Name);
}
