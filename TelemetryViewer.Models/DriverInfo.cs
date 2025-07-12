public class DriverInfo
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
}
