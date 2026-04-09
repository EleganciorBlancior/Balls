/// <summary>
/// Przechowuje int XOR-owany z losowym kluczem.
/// Skanery pamięci szukające wartości np. 500 nie znajdą jej w RAM.
/// Działa jak zwykły int dzięki implicit conversion.
/// </summary>
[System.Serializable]
public struct ObfuscatedInt
{
    [UnityEngine.SerializeField] private int _encoded;
    [UnityEngine.SerializeField] private int _key;

    private static readonly System.Random _rng = new System.Random();

    private static int NextKey() => _rng.Next(int.MinValue, int.MaxValue);

    private ObfuscatedInt(int value)
    {
        _key     = NextKey();
        _encoded = value ^ _key;
    }

    public int Value
    {
        get => _encoded ^ _key;
        set
        {
            _key     = NextKey();
            _encoded = value ^ _key;
        }
    }

    public static implicit operator int(ObfuscatedInt o)  => o.Value;
    public static implicit operator ObfuscatedInt(int v)   => new ObfuscatedInt(v);

    public override string ToString() => Value.ToString();
    public override int  GetHashCode() => Value.GetHashCode();
    public override bool Equals(object obj) => obj is ObfuscatedInt o && o.Value == Value;
}
